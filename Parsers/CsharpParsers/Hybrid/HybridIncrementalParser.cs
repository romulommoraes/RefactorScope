using System;
using System.Collections.Generic;
using System.Diagnostics;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;

namespace RefactorScope.Parsers.CsharpParsers.Hybrid
{
    /// <summary>
    /// Hybrid Incremental Parser
    ///
    /// Estratégia:
    ///
    /// 1. Executa o parser primário (tipicamente Regex)
    /// 2. Avalia se o modelo primário já é estruturalmente suficiente
    /// 3. Se for suficiente:
    ///       retorna imediatamente
    ///
    /// 4. Caso contrário:
    ///       executa o parser secundário
    ///       e realiza merge dos modelos
    ///
    /// Objetivo:
    /// reduzir custo computacional em cenários onde o parser primário
    /// já entrega cobertura estrutural satisfatória.
    ///
    /// Regra atual de suficiência
    /// --------------------------
    /// A decisão de skip do parser secundário usa critérios simples,
    /// porém mais grounded do que um threshold fixo arbitrário:
    ///
    /// - o modelo primário precisa existir
    /// - o resultado precisa ser plausível
    /// - o número de tipos detectados precisa ultrapassar um mínimo
    /// - a densidade relacional (referências por tipo) precisa atingir
    ///   um patamar mínimo
    ///
    /// Isso torna a decisão proporcional ao tamanho/estrutura do projeto,
    /// em vez de depender de um número absoluto de referências.
    ///
    /// Limitações atuais
    /// -----------------
    /// - a heurística ainda é simples e baseada apenas no resultado do parser primário
    /// - não considera distribuição modular das referências
    /// - não considera cobertura por arquivo nem variância estrutural
    /// - não compara ganho potencial do parser secundário
    ///
    /// Melhorias futuras sugeridas
    /// ---------------------------
    /// - incorporar cobertura por arquivo (tipos/arquivo, refs/arquivo)
    /// - usar métricas de plausibilidade mais ricas
    /// - calibrar threshold por corpus/projeto
    /// - estimar ganho marginal esperado do parser secundário
    /// - transformar a decisão em uma função estatística/configurável
    /// </summary>
    public class HybridIncrementalParser : IParserCodigo
    {
        private readonly IParserCodigo primary;
        private readonly IParserCodigo secondary;
        private readonly Action<string>? warn;

        public string Name => "HybridParser (Incremental)";

        public HybridIncrementalParser(
            IParserCodigo primaryParser,
            IParserCodigo secondaryParser,
            Action<string>? warn = null)
        {
            primary = primaryParser;
            secondary = secondaryParser;
            this.warn = warn;
        }

        public IParserResult Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            var stopwatch = Stopwatch.StartNew();

            warn?.Invoke("[HybridIncremental] Executando parser primário...");

            var primaryResult = primary.Parse(rootPath, include, exclude);

            if (IsPrimaryModelSufficient(primaryResult))
            {
                stopwatch.Stop();

                warn?.Invoke(
                    "[HybridIncremental] Modelo primário suficiente. Skip do parser secundário.");

                return new ParserResult(
                    Status: primaryResult.Status,
                    IsPlausible: primaryResult.IsPlausible,
                    Confidence: primaryResult.Confidence,
                    ParserName: "HybridParser (Incremental → Regex)",
                    Model: primaryResult.Model,
                    UsedFallback: false,
                    Stats: primaryResult.Stats ?? new ParserExecutionStats(
                        stopwatch.Elapsed,
                        0,
                        !primaryResult.IsPlausible),
                    Error: primaryResult.Error
                );
            }

            warn?.Invoke(
                "[HybridIncremental] Modelo primário insuficiente. Executando parser secundário.");

            var secondaryResult = secondary.Parse(rootPath, include, exclude);

            // -------------------------------------------------
            // Tratamento seguro dos cenários parciais
            // -------------------------------------------------
            if (primaryResult.Model == null && secondaryResult.Model == null)
            {
                stopwatch.Stop();

                return new ParserResult(
                    Status: ParseStatus.Failed,
                    IsPlausible: false,
                    Confidence: 0,
                    ParserName: Name,
                    Model: null,
                    UsedFallback: false,
                    Stats: new ParserExecutionStats(
                        stopwatch.Elapsed,
                        0,
                        true),
                    Error: primaryResult.Error ?? secondaryResult.Error
                );
            }

            if (primaryResult.Model == null && secondaryResult.Model != null)
            {
                stopwatch.Stop();

                warn?.Invoke(
                    "[HybridIncremental] Parser primário falhou. Retornando parser secundário como fallback.");

                return new ParserResult(
                    Status: ParseStatus.FallbackTriggered,
                    IsPlausible: secondaryResult.IsPlausible,
                    Confidence: secondaryResult.Confidence,
                    ParserName: $"{Name} → Fallback({secondaryResult.ParserName})",
                    Model: secondaryResult.Model,
                    UsedFallback: true,
                    Stats: BuildCombinedStats(
                        stopwatch.Elapsed,
                        primaryResult,
                        secondaryResult,
                        !secondaryResult.IsPlausible),
                    Error: primaryResult.Error ?? secondaryResult.Error
                );
            }

            if (primaryResult.Model != null && secondaryResult.Model == null)
            {
                stopwatch.Stop();

                warn?.Invoke(
                    "[HybridIncremental] Parser secundário indisponível. Retornando modelo primário.");

                return new ParserResult(
                    Status: primaryResult.Status,
                    IsPlausible: primaryResult.IsPlausible,
                    Confidence: primaryResult.Confidence,
                    ParserName: $"{Name} → PrimaryOnly",
                    Model: primaryResult.Model,
                    UsedFallback: false,
                    Stats: BuildCombinedStats(
                        stopwatch.Elapsed,
                        primaryResult,
                        secondaryResult,
                        !primaryResult.IsPlausible),
                    Error: primaryResult.Error ?? secondaryResult.Error
                );
            }

            // A partir daqui, ambos os modelos existem
            var merged = ModeloMerger.Merge(
                primaryResult.Model!,
                secondaryResult.Model!);

            stopwatch.Stop();

            bool plausible = PlausibilityEvaluator.Evaluate(merged);

            warn?.Invoke("[HybridIncremental] Merge incremental concluído.");

            return new ParserResult(
                Status: plausible
                    ? ParseStatus.Success
                    : ParseStatus.PlausibilityWarning,

                IsPlausible: plausible,

                Confidence: Math.Max(
                    primaryResult.Confidence,
                    secondaryResult.Confidence),

                ParserName: Name,

                Model: merged,

                UsedFallback: false,

                Stats: BuildCombinedStats(
                    stopwatch.Elapsed,
                    primaryResult,
                    secondaryResult,
                    !plausible),

                Error: primaryResult.Error ?? secondaryResult.Error
            );
        }

        /// <summary>
        /// Avalia se o modelo primário é suficientemente bom para evitar
        /// a execução do parser secundário.
        ///
        /// Heurística atual:
        /// - modelo não nulo
        /// - plausibilidade positiva
        /// - mínimo de tipos detectados
        /// - densidade mínima de referências por tipo
        ///
        /// A densidade relacional é mais robusta do que um número fixo
        /// absoluto, porque escala com o tamanho do projeto.
        /// </summary>
        private static bool IsPrimaryModelSufficient(IParserResult primaryResult)
        {
            if (primaryResult.Model == null)
                return false;

            if (!primaryResult.IsPlausible)
                return false;

            var typeCount = primaryResult.Model.Tipos.Count;
            var referenceCount = primaryResult.Model.Referencias.Count;

            if (typeCount < 15)
                return false;

            var referencesPerType =
                typeCount == 0
                    ? 0
                    : referenceCount / (double)typeCount;

            return referencesPerType >= 3.0;
        }

        /// <summary>
        /// Consolida estatísticas observáveis da execução incremental.
        ///
        /// Observação:
        /// o consumo de memória permanece estimado por soma simples,
        /// o que é suficiente para observabilidade básica, mas ainda
        /// pode ser refinado futuramente.
        /// </summary>
        private static ParserExecutionStats BuildCombinedStats(
            TimeSpan elapsed,
            IParserResult? primaryResult,
            IParserResult? secondaryResult,
            bool anomalyDetected)
        {
            var estimatedMemory =
                (primaryResult?.Stats?.EstimatedMemoryBytes ?? 0) +
                (secondaryResult?.Stats?.EstimatedMemoryBytes ?? 0);

            return new ParserExecutionStats(
                elapsed,
                estimatedMemory,
                anomalyDetected);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;

namespace RefactorScope.Parsers.CsharpParsers.Hybrid
{

    /// <summary>
    /// Hybrid Adaptive Parser
    ///
    /// Estratégia:
    ///
    /// 1. Executa parser primário (Regex)
    /// 2. Avalia plausibilidade estrutural
    /// 3. Caso estrutura esteja fraca:
    ///       parser secundário recupera TIPOS
    /// 4. Caso estrutura esteja boa:
    ///       parser secundário extrai apenas dependências
    ///
    /// Objetivo:
    /// tornar o merge resiliente a falhas do Regex.
    ///
    /// Este modo é ideal para repositórios
    /// com código parcialmente inválido
    /// ou geração dinâmica de código.
    /// </summary>
    public class HybridAdaptiveParser : IParserCodigo
    {
        private readonly IParserCodigo primary;
        private readonly IParserCodigo secondary;
        private readonly Action<string>? warn;

        public string Name => "HybridParser (Adaptive)";

        public HybridAdaptiveParser(
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

            warn?.Invoke("[HybridAdaptive] Executando parser primário...");

            var primaryResult =
                primary.Parse(rootPath, include, exclude);

            bool weakStructure =
                primaryResult.Model == null ||
                primaryResult.Model.Tipos.Count < 3;

            if (weakStructure)
            {
                warn?.Invoke(
                    "[HybridAdaptive] Estrutura fraca detectada. Recuperando tipos via parser secundário.");
            }
            else
            {
                warn?.Invoke(
                    "[HybridAdaptive] Estrutura válida. Extraindo dependências adicionais.");
            }

            var secondaryResult =
                secondary.Parse(rootPath, include, exclude);

            var merged =
                ModeloMerger.Merge(
                    primaryResult.Model!,
                    secondaryResult.Model!);

            stopwatch.Stop();

            bool plausible =
                PlausibilityEvaluator.Evaluate(merged);

            warn?.Invoke("[HybridAdaptive] Merge adaptativo concluído.");

            return new ParserResult(
                Status: plausible
                    ? ParseStatus.Success
                    : ParseStatus.PlausibilityWarning,

                IsPlausible: plausible,

                Confidence:
                    Math.Max(
                        primaryResult.Confidence,
                        secondaryResult.Confidence),

                ParserName: Name,

                Model: merged,

                UsedFallback: false,

                Stats: new ParserExecutionStats(
                    stopwatch.Elapsed,
                    0,
                    !plausible)
            );
        }
    }
}
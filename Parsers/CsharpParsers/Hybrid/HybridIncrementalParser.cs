using System;
using System.Collections.Generic;
using System.Diagnostics;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;

namespace RefactorScope.Parsers.Hybrid;

/// <summary>
/// Hybrid Incremental Parser
///
/// Estratégia:
///
/// 1. Executa parser primário (Regex)
/// 2. Avalia qualidade estrutural
/// 3. Caso o modelo já seja suficientemente completo:
///       retorna imediatamente
///
/// 4. Caso contrário:
///       executa parser secundário
///       e realiza merge.
///
/// Objetivo:
/// reduzir custo computacional.
///
/// Ideal para repositórios grandes
/// onde Regex já fornece boa cobertura.
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

        var primaryResult =
            primary.Parse(rootPath, include, exclude);

        if (primaryResult.Model != null &&
            primaryResult.Model.Referencias.Count > 20)
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
                Stats: primaryResult.Stats,
                Error: primaryResult.Error
            );
        }

        warn?.Invoke(
            "[HybridIncremental] Modelo incompleto. Executando parser secundário.");

        var secondaryResult =
            secondary.Parse(rootPath, include, exclude);

        var merged =
            ModeloMerger.Merge(
                primaryResult.Model!,
                secondaryResult.Model!);

        stopwatch.Stop();

        bool plausible =
            PlausibilityEvaluator.Evaluate(merged);

        warn?.Invoke("[HybridIncremental] Merge incremental concluído.");

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
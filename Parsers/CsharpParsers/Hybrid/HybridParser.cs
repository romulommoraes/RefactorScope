using System;
using System.Collections.Generic;
using System.Diagnostics;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;

namespace RefactorScope.Parsers.CsharpParsers.Hybrid;

/// <summary>
/// Parser híbrido que orquestra dois parsers diferentes.
/// 
/// Modos disponíveis:
/// 
/// Failover
///     Usa parser primário.
///     Caso falhe ou gere resultado implausível,
///     executa o parser secundário.
///
/// Merge
///     Executa ambos os parsers e combina seus resultados.
///     Regex define estrutura (tipos).
///     Textual enriquece dependências.
/// </summary>
public class HybridParser : IParserCodigo
{
    private readonly IParserCodigo primary;
    private readonly IParserCodigo secondary;
    private readonly HybridMode mode;
    private readonly Action<string>? warn;

    public string Name => $"HybridParser ({mode})";

    public HybridParser(
        IParserCodigo primaryParser,
        IParserCodigo secondaryParser,
        HybridMode mode,
        Action<string>? warn = null)
    {
        primary = primaryParser;
        secondary = secondaryParser;
        this.mode = mode;
        this.warn = warn;
    }

    public IParserResult Parse(
        string rootPath,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null)
    {
        var stopwatch = Stopwatch.StartNew();

        var primaryResult =
            primary.Parse(rootPath, include, exclude);

        if (mode == HybridMode.Failover)
        {
            if (primaryResult.Status == ParseStatus.Success &&
                primaryResult.IsPlausible)
            {
                stopwatch.Stop();

                warn?.Invoke(
                    $"[Hybrid] Resultado aceito do parser primário ({primary.Name}).");

                return CriarEnvelope(
                    primaryResult,
                    ParseStatus.Success,
                    stopwatch.Elapsed,
                    false);
            }

            warn?.Invoke(
                $"[Hybrid] {primary.Name} falhou ou gerou anomalia. Executando fallback: {secondary.Name}");

            var fallback =
                secondary.Parse(rootPath, include, exclude);

            stopwatch.Stop();

            return CriarEnvelope(
                fallback,
                ParseStatus.FallbackTriggered,
                stopwatch.Elapsed,
                true);
        }

        // -------------------------
        // MERGE MODE
        // -------------------------

        warn?.Invoke(
            "[Hybrid] Modo Merge ativo. Executando parsers...");

        var secondaryResult =
            secondary.Parse(rootPath, include, exclude);

        var merged =
            ModeloMerger.Merge(
                primaryResult.Model!,
                secondaryResult.Model!);

        stopwatch.Stop();

        bool plausible =
            PlausibilityEvaluator.Evaluate(merged);

        warn?.Invoke(
            "[Hybrid] Merge concluído. Modelo enriquecido.");

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

    private IParserResult CriarEnvelope(
        IParserResult inner,
        ParseStatus status,
        TimeSpan time,
        bool usedFallback)
    {
        return new ParserResult(
            Status: status,
            IsPlausible: inner.IsPlausible,
            Confidence: inner.Confidence,
            ParserName: $"{Name} -> {inner.ParserName}",
            Model: inner.Model,
            UsedFallback: usedFallback,
            Stats: new ParserExecutionStats(
                time,
                inner.Stats?.EstimatedMemoryBytes ?? 0,
                !inner.IsPlausible),
            Error: inner.Error);
    }
}
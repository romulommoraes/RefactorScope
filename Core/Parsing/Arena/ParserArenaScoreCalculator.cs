using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Parsing.Arena;

/// <summary>
/// Computes comparative parser scores within the context of a single project.
///
/// Important:
/// - scoring is relative to the runs of the same project
/// - speed has lower weight than structural coverage and plausibility
/// - failed runs are heavily penalized
/// </summary>
public static class ParserArenaScoreCalculator
{
    public static void ApplyScores(ParserArenaProjectResult projectResult)
    {
        if (projectResult == null)
            throw new ArgumentNullException(nameof(projectResult));

        if (projectResult.Runs.Count == 0)
            return;

        double maxTypes = Math.Max(1, projectResult.Runs.Max(r => r.TypeCount));
        double maxReferences = Math.Max(1, projectResult.Runs.Max(r => r.ReferenceCount));
        double minExecutionMs = Math.Max(1, projectResult.Runs.Min(r => Math.Max(1, r.ExecutionTime.TotalMilliseconds)));
        double maxExecutionMs = Math.Max(minExecutionMs, projectResult.Runs.Max(r => Math.Max(1, r.ExecutionTime.TotalMilliseconds)));

        foreach (var run in projectResult.Runs)
        {
            run.ComparativeScore = ComputeScore(
                run,
                maxTypes,
                maxReferences,
                minExecutionMs,
                maxExecutionMs);
        }
    }

    private static double ComputeScore(
        ParserArenaRunResult run,
        double maxTypes,
        double maxReferences,
        double minExecutionMs,
        double maxExecutionMs)
    {
        if (run.Status == ParseStatus.Failed)
            return 0;

        double statusScore = run.Status switch
        {
            ParseStatus.Success => 100,
            ParseStatus.PlausibilityWarning => 70,
            ParseStatus.FallbackTriggered => 50,
            ParseStatus.Failed => 0,
            _ => 0
        };

        double confidenceScore = Clamp01(run.Confidence) * 40.0;
        double plausibilityScore = run.IsPlausible ? 15.0 : 0.0;
        double typeCoverageScore = Normalize(run.TypeCount, maxTypes) * 20.0;
        double referenceCoverageScore = Normalize(run.ReferenceCount, maxReferences) * 25.0;

        // Faster is better, but only as a softer tie-breaker.
        double speedScore = NormalizeInverse(
            Math.Max(1, run.ExecutionTime.TotalMilliseconds),
            minExecutionMs,
            maxExecutionMs) * 10.0;

        double fallbackPenalty = run.UsedFallback ? 10.0 : 0.0;

        var finalScore =
            statusScore +
            confidenceScore +
            plausibilityScore +
            typeCoverageScore +
            referenceCoverageScore +
            speedScore -
            fallbackPenalty;

        return Math.Round(Math.Max(0, finalScore), 2);
    }

    private static double Normalize(double value, double max)
    {
        if (max <= 0)
            return 0;

        return Clamp01(value / max);
    }

    private static double NormalizeInverse(double value, double min, double max)
    {
        if (max <= min)
            return 1.0;

        var normalized = (value - min) / (max - min);
        return Clamp01(1.0 - normalized);
    }

    private static double Clamp01(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}
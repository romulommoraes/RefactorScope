using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Enum;

namespace RefactorScope.Core.Parsing.Arena;

/// <summary>
/// Aggregates all parser runs executed for a single project in Arena mode.
/// </summary>
public sealed class ParserArenaProjectResult
{
    private static readonly Dictionary<ParserStrategy, int> StrategyOrder = new()
    {
        [ParserStrategy.RegexFast] = 1,
        [ParserStrategy.Selective] = 2,
        [ParserStrategy.AdaptiveExperimental] = 3,
        [ParserStrategy.IncrementalExperimental] = 4,
        [ParserStrategy.Comparative] = 99
    };

    public required string ProjectName { get; init; }

    public required string ProjectPath { get; init; }

    public List<ParserArenaRunResult> Runs { get; } = new();

    public bool HasFailures => Runs.Any(r => r.Status == ParseStatus.Failed);

    public int TotalRuns => Runs.Count;

    /// <summary>
    /// Runs ordered in a deterministic presentation order by strategy.
    /// </summary>
    public IReadOnlyList<ParserArenaRunResult> OrderedRuns =>
        Runs.OrderBy(r => StrategyOrder.GetValueOrDefault(r.Strategy, int.MaxValue))
            .ThenBy(r => r.ParserName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Best comparative run for the current project.
    /// Returns null when no runs are available.
    /// </summary>
    public ParserArenaRunResult? BestRun =>
        Runs.OrderByDescending(r => r.ComparativeScore)
            .ThenByDescending(r => r.Confidence)
            .ThenByDescending(r => r.TypeCount)
            .ThenByDescending(r => r.ReferenceCount)
            .ThenBy(r => r.ExecutionTime)
            .FirstOrDefault();
}
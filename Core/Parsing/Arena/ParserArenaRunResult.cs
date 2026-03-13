using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Enum;

namespace RefactorScope.Core.Parsing.Arena;

/// <summary>
/// Represents a single parser execution over a single project inside Arena mode.
/// This is the atomic comparative unit used later for JSON, CSV and dashboards.
/// </summary>
public sealed class ParserArenaRunResult
{
    public required string ProjectName { get; init; }

    public required string ProjectPath { get; init; }

    public required ParserStrategy Strategy { get; init; }

    public required string ParserName { get; init; }

    public required ParseStatus Status { get; init; }

    public required double Confidence { get; init; }

    public required bool IsPlausible { get; init; }

    public required bool UsedFallback { get; init; }

    public required TimeSpan ExecutionTime { get; init; }

    public required int FileCount { get; init; }

    public required int TypeCount { get; init; }

    public required int ReferenceCount { get; init; }

    /// <summary>
    /// Comparative score assigned inside the context of its own project.
    /// Higher is better.
    /// </summary>
    public double ComparativeScore { get; set; }

    public string? ErrorMessage { get; init; }

    public static ParserArenaRunResult FromParserResult(
        string projectName,
        string projectPath,
        ParserStrategy strategy,
        IParserResult result)
    {
        return new ParserArenaRunResult
        {
            ProjectName = projectName,
            ProjectPath = projectPath,
            Strategy = strategy,
            ParserName = result.ParserName,
            Status = result.Status,
            Confidence = result.Confidence,
            IsPlausible = result.IsPlausible,
            UsedFallback = result.UsedFallback,
            ExecutionTime = result.Stats?.ExecutionTime ?? TimeSpan.Zero,
            FileCount = result.Model?.Arquivos.Count ?? 0,
            TypeCount = result.Model?.Tipos.Count ?? 0,
            ReferenceCount = result.Model?.Referencias.Count ?? 0,
            ComparativeScore = 0,
            ErrorMessage = result.Error?.Message
        };
    }

    public static ParserArenaRunResult FromException(
        string projectName,
        string projectPath,
        ParserStrategy strategy,
        string parserName,
        Exception ex)
    {
        return new ParserArenaRunResult
        {
            ProjectName = projectName,
            ProjectPath = projectPath,
            Strategy = strategy,
            ParserName = parserName,
            Status = ParseStatus.Failed,
            Confidence = 0,
            IsPlausible = false,
            UsedFallback = false,
            ExecutionTime = TimeSpan.Zero,
            FileCount = 0,
            TypeCount = 0,
            ReferenceCount = 0,
            ComparativeScore = 0,
            ErrorMessage = ex.Message
        };
    }
}
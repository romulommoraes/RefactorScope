namespace RefactorScope.CLI;

/// <summary>
/// Represents the startup mode selection resolved before configuration loading.
/// </summary>
public sealed class StartupModeSelection
{
    public required string ConfigPath { get; init; }

    public required bool IsSelfAnalysis { get; init; }

    public required bool IsBatchMode { get; init; }
}
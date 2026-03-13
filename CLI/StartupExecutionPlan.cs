using RefactorScope.Core.Parsing.Enum;

namespace RefactorScope.CLI;

/// <summary>
/// Plano de execução resolvido pelo CLI antes do carregamento completo do pipeline.
/// Se o modo for SingleParser, SelectedParser deve conter um parser concreto.
/// Se o modo for Comparative ou BatchArena, SelectedParser pode ser nulo.
/// </summary>
public sealed class StartupExecutionPlan
{
    public required string ConfigPath { get; init; }

    public required AnalysisScope Scope { get; init; }

    public required ExecutionMode Mode { get; init; }

    public ParserStrategy? SelectedParser { get; init; }

    public bool IsSelfAnalysis => Scope == AnalysisScope.Self;
}
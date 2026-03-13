using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;

namespace RefactorScope.Analyzers.Solid.Rules
{
    public interface IAbsolutionRule
    {
        bool ShouldPardon(SolidSuspicion suspicion, AnalysisContext context);
    }
}
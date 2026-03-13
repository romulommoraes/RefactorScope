using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;

namespace RefactorScope.Analyzers.Solid.Rules
{
    public sealed class OrchestratorAbsolutionRule : IAbsolutionRule
    {
        private readonly SolidConfig _config;

        public OrchestratorAbsolutionRule(SolidConfig config)
        {
            _config = config;
        }

        public bool ShouldPardon(SolidSuspicion suspicion, AnalysisContext context)
        {
            if (suspicion.Principle != SolidPrinciple.SRP)
                return false;

            return _config.OrchestratorSuffixes
                .Any(s => suspicion.ClassName.EndsWith(s));
        }
    }
}
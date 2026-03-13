using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;

namespace RefactorScope.Analyzers.Solid.Rules
{
    public sealed class CoreDependencyAbsolutionRule : IAbsolutionRule
    {
        private readonly SolidConfig _config;

        public CoreDependencyAbsolutionRule(SolidConfig config)
        {
            _config = config;
        }

        public bool ShouldPardon(SolidSuspicion suspicion, AnalysisContext context)
        {
            if (suspicion.Principle != SolidPrinciple.DIP)
                return false;

            return _config.AllowedCoreDependencies
                .Any(ns => suspicion.Namespace.StartsWith(ns));
        }
    }
}
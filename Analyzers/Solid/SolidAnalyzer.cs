using RefactorScope.Analyzers.Solid.Rules;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers.Solid
{
    public sealed class SolidAnalyzer : IAnalyzer
    {
        private readonly SolidConfig _config;
        private readonly List<IAbsolutionRule> _absolutionRules;
        private readonly List<IOmissionRule> _omissionRules;

        public SolidAnalyzer()
        {
            _config = new SolidConfig();

            _absolutionRules = new()
            {
                new OrchestratorAbsolutionRule(_config),
                new CoreDependencyAbsolutionRule(_config)
            };

            _omissionRules = new()
            {
                new PublicZeroUsageOmissionRule()
            };
        }

        public string Name => "SOLID";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var suspicions = Pass1Heuristic(context);

            foreach (var omission in _omissionRules)
                suspicions.AddRange(omission.Generate(context));

            suspicions = suspicions
                .Where(s => !_absolutionRules.Any(r => r.ShouldPardon(s, context)))
                .ToList();

            return new SolidResult(suspicions);
        }

        private List<SolidSuspicion> Pass1Heuristic(AnalysisContext context)
        {
            var result = new List<SolidSuspicion>();

            var coupling = context.Results
                .OfType<CouplingResult>()
                .FirstOrDefault();

            if (coupling == null)
                return result;

            foreach (var module in coupling.TypeFanOutByModule)
            {
                foreach (var type in module.Value)
                {
                    if (type.Value > _config.SrpFanOutThreshold)
                    {
                        result.Add(new SolidSuspicion
                        {
                            Principle = SolidPrinciple.SRP,
                            ClassName = type.Key,
                            Namespace = module.Key,
                            Reason = $"FanOut > {_config.SrpFanOutThreshold}"
                        });
                    }
                }
            }

            return result;
        }
    }
}
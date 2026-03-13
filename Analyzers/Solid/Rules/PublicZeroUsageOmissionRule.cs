using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers.Solid.Rules
{
    public sealed class PublicZeroUsageOmissionRule : IOmissionRule
    {
        public IEnumerable<SolidSuspicion> Generate(AnalysisContext context)
        {
            var classification = context.Results
                .OfType<ArchitecturalClassificationResult>()
                .FirstOrDefault();

            if (classification == null)
                return Enumerable.Empty<SolidSuspicion>();

            return classification.Items
                .Where(i => i.UsageCount == 0 && i.Status == "Ativo")
                .Select(i => new SolidSuspicion
                {
                    Principle = SolidPrinciple.SRP,
                    ClassName = i.TypeName,
                    Namespace = i.Namespace,
                    Reason = "Public class with zero usage."
                });
        }
    }
}
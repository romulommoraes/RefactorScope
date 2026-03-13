using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    public class StructuralCandidateProbabilityResult : IAnalysisResult
    {
        public IReadOnlyList<StructuralCandidateProbabilityItem> Items { get; }

        public StructuralCandidateProbabilityResult(IReadOnlyList<StructuralCandidateProbabilityItem> items)
        {
            Items = items;
        }

        public IReadOnlyList<StructuralCandidateProbabilityItem> Unresolved(double threshold)
            => Items.Where(i => i.Probability >= threshold).ToList();
    }
}
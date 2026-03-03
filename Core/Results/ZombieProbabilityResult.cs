using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    public class ZombieProbabilityResult : IAnalysisResult
    {
        public IReadOnlyList<ZombieProbabilityItem> Items { get; }

        public ZombieProbabilityResult(IReadOnlyList<ZombieProbabilityItem> items)
        {
            Items = items;
        }

        public IReadOnlyList<ZombieProbabilityItem> ConfirmedZombies(double threshold)
            => Items.Where(i => i.Probability >= threshold).ToList();
    }
}
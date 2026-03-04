using RefactorScope.Core.Model;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Analyzers
{
    public sealed class ArchitecturalHygieneAnalyzer
    {
        public HygieneReport Analyze(ConsolidatedReport report)
        {
            var architecture = report.GetResult<ArchitecturalClassificationResult>();
            var isolated = report.GetResult<CoreIsolationResult>();

            if (architecture == null)
                return new HygieneReport(0, 0, 0, 0, 0, 0);

            var items = architecture.Items;
            var total = items.Count;

            var unreferenced = items.Count(i => i.UsageCount == 0);

            var globalNamespace = items.Count(i => i.Namespace == "Global");

            int namespaceDrift = architecture.Items.Count(i =>
            {
                if (string.IsNullOrWhiteSpace(i.Namespace))
                    return true;

                if (string.IsNullOrWhiteSpace(i.Folder))
                    return false;

                var expectedSuffix = i.Folder
                    .Replace("\\", ".")
                    .Replace("/", ".");

                return !i.Namespace.EndsWith(expectedSuffix);
            });

            var isolatedCoreCount = isolated?.IsolatedCoreTypes.Count ?? 0;

            var entropy = ComputeEntropy(items);

            return new HygieneReport(
                total,
                unreferenced,
                globalNamespace,
                namespaceDrift,
                isolatedCoreCount,
                entropy
            );
        }

        private static double ComputeEntropy(IReadOnlyList<ArchitecturalClassificationItem> items)
        {
            var groups = items.GroupBy(i => i.Folder).ToList();
            var total = items.Count;

            if (total == 0 || groups.Count <= 1)
                return 0;

            double entropy = 0;

            foreach (var group in groups)
            {
                var p = group.Count() / (double)total;
                if (p > 0)
                    entropy -= p * Math.Log(p, 2);
            }

            var maxEntropy = Math.Log(groups.Count, 2);

            return maxEntropy == 0 ? 0 : entropy / maxEntropy;
        }
    }
}
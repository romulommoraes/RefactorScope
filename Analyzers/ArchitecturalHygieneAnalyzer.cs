using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Analyzers
{
    public sealed class ArchitecturalHygieneAnalyzer
    {
        public HygieneReport Analyze(ConsolidatedReport report)
        {
            var architecture =
                report.GetResult<ArchitecturalClassificationResult>();

            var isolated =
                report.GetResult<CoreIsolationResult>();

            if (architecture == null)
                return Empty();

            // 🔥 Fonte oficial unificada
            var effectiveZombies =
                report.GetEffectiveZombieTypes();

            var total = architecture.Items.Count;

            var deadCount = effectiveZombies.Count;

            var coreCount =
                architecture.Items.Count(i => i.Layer == "Core");

            var legacyCount =
                architecture.Items.Count(i => i.Status.Contains("Legado"));

            var isolatedCoreCount =
                isolated?.IsolatedCoreTypes.Count ?? 0;

            var entropy = ComputeEntropy(architecture);

            var smellIndex =
                (deadCount * 1.2)
                + (legacyCount * 0.8)
                + (isolatedCoreCount * 1.0)
                + (entropy * 50);

            return new HygieneReport
            {
                TotalClasses = total,
                DeadCount = deadCount,
                LegacyCount = legacyCount,
                CoreCount = coreCount,
                RemovalCandidates = deadCount,
                IsolatedCoreCount = isolatedCoreCount,
                StructuralEntropy = entropy,
                SmellIndex = smellIndex
            };
        }

        private static HygieneReport Empty()
        {
            return new HygieneReport
            {
                TotalClasses = 0,
                DeadCount = 0,
                LegacyCount = 0,
                CoreCount = 0,
                RemovalCandidates = 0,
                IsolatedCoreCount = 0,
                StructuralEntropy = 0,
                SmellIndex = 0
            };
        }

        private static double ComputeEntropy(
            ArchitecturalClassificationResult architecture)
        {
            var groups =
                architecture.Items.GroupBy(i => i.Folder).ToList();

            var total = architecture.Items.Count;

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

            return maxEntropy == 0
                ? 0
                : entropy / maxEntropy;
        }
    }
}
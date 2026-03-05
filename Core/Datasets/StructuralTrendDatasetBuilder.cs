using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Historical dataset representing structural health trends over time.
    ///
    /// ⚠️ Source of Truth for candidate resolution:
    /// Always uses ConsolidatedReport.GetEffectiveUnresolvedCandidates(),
    /// ensuring probabilistic refinement is respected when available.
    ///
    /// Legacy compatibility:
    /// The column "ZombieRate" is preserved for compatibility with
    /// previously generated datasets and dashboards.
    ///
    /// Conceptually this value now represents:
    ///     Unresolved Candidate Rate
    ///
    /// Planned migration (future version):
    ///     ZombieRate → UnresolvedRate
    /// </summary>
    public class StructuralTrendDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_structural_trend";

        public string[] Headers => new[]
        {
            "Timestamp",
            "Scope",
            "StructuralScore",
            "Coupling",
            "ZombieRate",     // legacy column (UnresolvedRate)
            "IsolationRate",
            "CoreDensity"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var coupling = report.GetResult<CouplingResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var architecture = report.GetResult<ArchitecturalClassificationResult>();

            var totalTypes = context.Model.Tipos.Count;

            if (totalTypes == 0)
                yield break;

            // ===============================
            // Official source of unresolved candidates
            // ===============================

            var unresolvedCandidates =
                report.GetEffectiveUnresolvedCandidates();

            // ===============================
            // Metrics
            // ===============================

            var totalFanOut = coupling?.ModuleFanOut.Values.Sum() ?? 0;
            var normalizedCoupling = totalFanOut / (double)totalTypes;

            var unresolvedRate =
                unresolvedCandidates.Count / (double)totalTypes;

            var isolationRate =
                isolated?.IsolatedCoreTypes.Count / (double)totalTypes ?? 0;

            var coreDensity =
                architecture?.Items.Count(i => i.Layer == "Core")
                / (double)totalTypes ?? 0;

            yield return new[]
            {
                DateTime.UtcNow.ToString("o"),
                context.Config.RootPath,
                ComputeScore(
                    normalizedCoupling,
                    unresolvedRate,
                    isolationRate,
                    coreDensity
                ).ToString("0.00"),
                normalizedCoupling.ToString("0.00"),
                unresolvedRate.ToString("0.00"),
                isolationRate.ToString("0.00"),
                coreDensity.ToString("0.00")
            };
        }

        private static double ComputeScore(
            double coupling,
            double unresolved,
            double isolation,
            double core)
        {
            var score =
                100
                - (coupling * 30)
                - (unresolved * 25)
                - (isolation * 20)
                + (core * 15);

            return Math.Max(0, Math.Min(100, score));
        }
    }
}
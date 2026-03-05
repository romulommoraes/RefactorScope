using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Global structural metrics dataset.
    ///
    /// Designed for KPI dashboards and gauges representing
    /// overall architectural health of the analyzed scope.
    ///
    /// ⚠ Source of Truth:
    /// Unresolved candidates are obtained through
    /// ConsolidatedReport.GetEffectiveUnresolvedCandidates().
    ///
    /// Legacy compatibility:
    /// The metric name "ZombieRate" is preserved for compatibility
    /// with historical datasets and dashboards.
    /// Conceptually it now represents the rate of Unresolved Candidates.
    /// </summary>
    public class GlobalMetricsDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_global_metrics";

        public string[] Headers => new[]
        {
            "Metric",
            "Value"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var coupling = report.GetResult<CouplingResult>();
            var entries = report.GetResult<EntryPointHeuristicResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var arch = report.GetResult<ArchitecturalClassificationResult>();

            var structuralCandidates = report.GetStructuralCandidates();
            var unresolvedCandidates = report.GetEffectiveUnresolvedCandidates();

            var tipos = context.Model.Tipos;
            var total = tipos.Count;

            if (total == 0)
                yield break;

            var totalFanOut = coupling?.ModuleFanOut.Values.Sum() ?? 0;

            var normalizedCoupling = Normalize(totalFanOut, total);

            // Legacy ZombieRate now represents unresolved candidate density
            var unresolvedRate = unresolvedCandidates.Count / (double)total;

            var isolationRate =
                isolated?.IsolatedCoreTypes.Count / (double)total ?? 0;

            var entryDensity =
                entries?.EntryPoints.Count / (double)total ?? 0;

            var coreDensity =
                arch?.Items.Count(i => i.Layer == "Core") / (double)total ?? 0;

            yield return new[] { "Coupling", normalizedCoupling.ToString("0.00") };

            // Legacy name preserved for compatibility
            yield return new[] { "ZombieRate", unresolvedRate.ToString("0.00") };

            yield return new[] { "IsolationRate", isolationRate.ToString("0.00") };

            yield return new[] { "EntryPointDensity", entryDensity.ToString("0.00") };

            yield return new[] { "CoreDensity", coreDensity.ToString("0.00") };
        }

        /// <summary>
        /// Normalizes FanOut by total number of types.
        /// </summary>
        private static double Normalize(int fanOut, int totalTipos)
        {
            if (totalTipos == 0)
                return 0;

            return fanOut / (double)totalTipos;
        }
    }
}
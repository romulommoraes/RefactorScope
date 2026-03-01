using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset de métricas globais.
    /// 
    /// Ideal para KPI dashboards e gauges.
    /// Representa a saúde estrutural do escopo analisado.
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
            var coupling = report.Results.OfType<CouplingResult>().FirstOrDefault();
            var zombies = report.Results.OfType<ZombieResult>().FirstOrDefault();
            var entries = report.Results.OfType<EntryPointHeuristicResult>().FirstOrDefault();
            var isolated = report.Results.OfType<CoreIsolationResult>().FirstOrDefault();
            var arch = report.Results.OfType<ArchitecturalClassificationResult>().FirstOrDefault();

            var tipos = context.Model.Tipos;
            var total = tipos.Count;

            if (total == 0)
                yield break;

            var totalFanOut = coupling?.ModuleFanOut.Values.Sum() ?? 0;
            var zombieRate = zombies?.ZombieTypes.Count / (double)total ?? 0;
            var isolationRate = isolated?.IsolatedCoreTypes.Count / (double)total ?? 0;
            var entryDensity = entries?.EntryPoints.Count / (double)total ?? 0;
            var coreDensity = arch?.Items.Count(i => i.Layer == "Core") / (double)total ?? 0;

            yield return new[] { "Coupling", Normalize(totalFanOut, total).ToString("0.00") };
            yield return new[] { "ZombieRate", zombieRate.ToString("0.00") };
            yield return new[] { "IsolationRate", isolationRate.ToString("0.00") };
            yield return new[] { "EntryPointDensity", entryDensity.ToString("0.00") };
            yield return new[] { "CoreDensity", coreDensity.ToString("0.00") };
        }

        /// <summary>
        /// Normaliza FanOut por tipo.
        /// </summary>
        private static double Normalize(int fanOut, int totalTipos)
        {
            if (totalTipos == 0) return 0;
            return fanOut / (double)totalTipos;
        }
    }
}
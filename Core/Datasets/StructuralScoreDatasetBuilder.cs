using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Calcula o Structural Score (0-100).
    /// 
    /// Representa a maturidade arquitetural do escopo analisado.
    /// Ideal para KPI executivo.
    /// </summary>
    public class StructuralScoreDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_structural_score";

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

            var normalizedCoupling = Normalize(totalFanOut, total);

            var score =
                100
                - (normalizedCoupling * 30)
                - (zombieRate * 25)
                - (isolationRate * 20)
                - (entryDensity * 10)
                + (coreDensity * 15);

            score = Clamp(score, 0, 100);

            yield return new[]
            {
                "StructuralScore",
                score.ToString("0.00")
            };
        }

        private static double Normalize(int fanOut, int totalTipos)
        {
            if (totalTipos == 0) return 0;
            return fanOut / (double)totalTipos;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
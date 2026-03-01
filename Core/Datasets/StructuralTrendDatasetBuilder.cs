using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    public class StructuralTrendDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_structural_trend";

        public string[] Headers => new[]
        {
            "Timestamp",
            "Scope",
            "StructuralScore",
            "Coupling",
            "ZombieRate",
            "IsolationRate",
            "CoreDensity"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var score = report.Results.OfType<CouplingResult>().FirstOrDefault();
            var zombies = report.Results.OfType<ZombieResult>().FirstOrDefault();
            var isolated = report.Results.OfType<CoreIsolationResult>().FirstOrDefault();
            var arch = report.Results.OfType<ArchitecturalClassificationResult>().FirstOrDefault();

            var tipos = context.Model.Tipos;
            var total = tipos.Count;

            if (total == 0)
                yield break;

            var totalFanOut = score?.ModuleFanOut.Values.Sum() ?? 0;
            var zombieRate = zombies?.ZombieTypes.Count / (double)total ?? 0;
            var isolationRate = isolated?.IsolatedCoreTypes.Count / (double)total ?? 0;
            var coreDensity = arch?.Items.Count(i => i.Layer == "Core") / (double)total ?? 0;

            var normalizedCoupling = totalFanOut / (double)total;

            yield return new[]
            {
                DateTime.UtcNow.ToString("o"),
                context.Config.RootPath,
                ComputeScore(normalizedCoupling, zombieRate, isolationRate, coreDensity).ToString("0.00"),
                normalizedCoupling.ToString("0.00"),
                zombieRate.ToString("0.00"),
                isolationRate.ToString("0.00"),
                coreDensity.ToString("0.00")
            };
        }

        private static double ComputeScore(double coupling, double zombie, double isolation, double core)
        {
            var score =
                100
                - (coupling * 30)
                - (zombie * 25)
                - (isolation * 20)
                + (core * 15);

            return Math.Max(0, Math.Min(100, score));
        }
    }
}
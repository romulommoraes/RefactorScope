using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset histórico de tendência estrutural.
    ///
    /// ⚠️ Fonte Única de Verdade para zombies:
    /// Sempre utiliza ConsolidatedReport.GetEffectiveZombieTypes(),
    /// respeitando o modelo probabilístico quando disponível.
    ///
    /// Nunca consome ZombieResult diretamente.
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
            "ZombieRate",
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
            // Fonte Oficial de Zombie
            // ===============================
            var threshold = context.Config
                .ZombieDetection
                .MinZombieProbabilityThreshold;

            var effectiveZombies =
                report.GetEffectiveZombieTypes();

            // ===============================
            // Métricas
            // ===============================
            var totalFanOut = coupling?.ModuleFanOut.Values.Sum() ?? 0;
            var normalizedCoupling = totalFanOut / (double)totalTypes;

            var zombieRate =
                effectiveZombies.Count / (double)totalTypes;

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
                    zombieRate,
                    isolationRate,
                    coreDensity
                ).ToString("0.00"),
                normalizedCoupling.ToString("0.00"),
                zombieRate.ToString("0.00"),
                isolationRate.ToString("0.00"),
                coreDensity.ToString("0.00")
            };
        }

        private static double ComputeScore(
            double coupling,
            double zombie,
            double isolation,
            double core)
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
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset de saúde arquitetural por módulo físico.
    ///
    /// Ideal para dashboards KPI:
    /// - Gauge
    /// - Ranking
    /// - Donut charts
    ///
    /// Métricas são normalizadas dentro do escopo analisado.
    /// Funciona corretamente mesmo em análise parcial.
    /// </summary>
    public class ArchitecturalHealthDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_arch_health";

        public string[] Headers => new[]
        {
            "Module",
            "ZombieRate",
            "IsolationRate",
            "CoreDensity",
            "EntryDensity"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var zombies = report.Results.OfType<ZombieResult>().FirstOrDefault();
            var entries = report.Results.OfType<EntryPointHeuristicResult>().FirstOrDefault();
            var isolated = report.Results.OfType<CoreIsolationResult>().FirstOrDefault();
            var arch = report.Results.OfType<ArchitecturalClassificationResult>().FirstOrDefault();

            var tipos = context.Model.Tipos;

            var modules = tipos.GroupBy(t => ExtractTopFolder(t.DeclaredInFile));

            foreach (var module in modules)
            {
                var tiposDoModulo = module.ToList();
                var total = tiposDoModulo.Count;

                if (total == 0)
                    continue;

                var zombieCount = zombies?.ZombieTypes.Count(z => tiposDoModulo.Any(t => t.Name == z)) ?? 0;
                var isolatedCount = isolated?.IsolatedCoreTypes.Count(i => tiposDoModulo.Any(t => t.Name == i)) ?? 0;
                var entryCount = entries?.EntryPoints.Count(e => tiposDoModulo.Any(t => t.Name == e)) ?? 0;
                var coreCount = tiposDoModulo.Count(t => arch?.Items.Any(a => a.TypeName == t.Name && a.Layer == "Core") == true);

                yield return new[]
                {
                    module.Key,
                    (zombieCount / (double)total).ToString("0.00"),
                    (isolatedCount / (double)total).ToString("0.00"),
                    (coreCount / (double)total).ToString("0.00"),
                    (entryCount / (double)total).ToString("0.00")
                };
            }
        }

        /// <summary>
        /// Extrai o módulo físico a partir do caminho relativo do arquivo.
        /// Usa a primeira pasta abaixo da raiz como identificador do módulo.
        /// </summary>
        private static string ExtractTopFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Root";

            var parts = path
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 1
                ? parts[0]
                : "Root";
        }
    }
}
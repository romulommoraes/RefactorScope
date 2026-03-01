using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset global tabular por tipo.
    ///
    /// Cada linha representa um tipo do sistema.
    /// Ideal para:
    /// - Heatmaps
    /// - Scatter plots
    /// - Rankings
    /// - Drill-down BI
    ///
    /// Funciona corretamente em análises parciais.
    /// </summary>
    public class GlobalTypesDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_types_global";

        public string[] Headers => new[]
        {
            "Type",
            "Module",
            "Layer",
            "Usage",
            "StructuralStatus",
            "IsZombie",
            "IsIsolated",
            "IsEntryPoint"
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

            foreach (var tipo in tipos)
            {
                var archItem = arch?.Items.FirstOrDefault(a => a.TypeName == tipo.Name);

                yield return new[]
                {
                    tipo.Name,
                    ExtractTopFolder(tipo.DeclaredInFile),
                    archItem?.Layer ?? "Unknown",
                    archItem?.UsageCount.ToString() ?? "0",
                    archItem?.StructuralStatus ?? "Unknown",
                    zombies?.ZombieTypes.Contains(tipo.Name) == true ? "1" : "0",
                    isolated?.IsolatedCoreTypes.Contains(tipo.Name) == true ? "1" : "0",
                    entries?.EntryPoints.Contains(tipo.Name) == true ? "1" : "0"
                };
            }
        }

        /// <summary>
        /// Extrai o módulo físico do caminho do arquivo.
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
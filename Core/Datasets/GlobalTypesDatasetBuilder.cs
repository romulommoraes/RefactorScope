using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Global per-type dataset.
    ///
    /// Each row represents a system type.
    ///
    /// Ideal for:
    /// - Heatmaps
    /// - Scatter plots
    /// - Rankings
    /// - BI drill-down
    ///
    /// Fully compatible with partial analyses.
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
            "IsStructuralCandidate",
            "IsUnresolved",
            "IsIsolated",
            "IsEntryPoint"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var structuralCandidates = report.GetStructuralCandidates();
            var unresolvedCandidates = report.GetEffectiveUnresolvedCandidates();

            var entries = report.GetResult<EntryPointHeuristicResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var arch = report.GetResult<ArchitecturalClassificationResult>();

            var tipos = context.Model.Tipos;

            foreach (var tipo in tipos)
            {
                var archItem = arch?.Items.FirstOrDefault(a => a.TypeName == tipo.Name);

                var isStructuralCandidate =
                    structuralCandidates.Contains(tipo.Name);

                var isUnresolved =
                    unresolvedCandidates.Contains(tipo.Name);

                var isIsolated =
                    isolated?.IsolatedCoreTypes.Contains(tipo.Name) == true;

                var isEntryPoint =
                    entries?.EntryPoints.Contains(tipo.Name) == true;

                yield return new[]
                {
                    tipo.Name,
                    ExtractTopFolder(tipo.DeclaredInFile),
                    archItem?.Layer ?? "Unknown",
                    archItem?.UsageCount.ToString() ?? "0",
                    archItem?.StructuralStatus ?? "Unknown",
                    isStructuralCandidate ? "1" : "0",
                    isUnresolved ? "1" : "0",
                    isIsolated ? "1" : "0",
                    isEntryPoint ? "1" : "0"
                };
            }
        }

        /// <summary>
        /// Extracts the top-level module folder from the file path.
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
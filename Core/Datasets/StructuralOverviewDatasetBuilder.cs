using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset estrutural por módulo físico.
    /// 
    /// Este dataset é ideal para visualizações de radar chart,
    /// mostrando a maturidade arquitetural por módulo dentro
    /// do escopo analisado.
    ///
    /// Importante:
    /// - Não depende da classificação heurística de pastas
    /// - Utiliza apenas o universo real analisado (context.Model)
    /// - Funciona corretamente em análises parciais
    /// </summary>
    public class StructuralOverviewDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_structural_overview";

        public string[] Headers => new[]
        {
            "Module",
            "TotalTypes",
            "CoreTypes",
            "ZombieTypes",
            "EntryPoints",
            "IsolatedTypes"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var zombies = report.Results.OfType<StructuralCandidateResult>().FirstOrDefault();
            var entries = report.Results.OfType<EntryPointHeuristicResult>().FirstOrDefault();
            var isolated = report.Results.OfType<CoreIsolationResult>().FirstOrDefault();
            var arch = report.Results.OfType<ArchitecturalClassificationResult>().FirstOrDefault();

            // Universo real analisado
            var tipos = context.Model.Tipos;

            // Agrupamento físico por módulo
            var modules = tipos.GroupBy(t => ExtractTopFolder(t.DeclaredInFile));

            foreach (var module in modules)
            {
                var tiposDoModulo = module.ToList();

                yield return new[]
                {
                    module.Key,
                    tiposDoModulo.Count.ToString(),
                    tiposDoModulo.Count(t => arch?.Items.Any(a => a.TypeName == t.Name && a.Layer == "Core") == true).ToString(),
                    zombies?.StructuralCandidateTypes.Count(z => tiposDoModulo.Any(t => t.Name == z)).ToString() ?? "0",
                    entries?.EntryPoints.Count(e => tiposDoModulo.Any(t => t.Name == e)).ToString() ?? "0",
                    isolated?.IsolatedCoreTypes.Count(i => tiposDoModulo.Any(t => t.Name == i)).ToString() ?? "0"
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
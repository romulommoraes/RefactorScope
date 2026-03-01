using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Infrastructure;
using RefactorScope.Core.Structure;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Classifica os tipos do sistema em camadas arquiteturais
    /// com base em regras configuráveis (layerRules).
    ///
    /// NÃO contém heurísticas hardcoded.
    /// A definição de camadas é externa e fornecida via config.
    /// </summary>
    public class ArchitecturalClassificationAnalyzer : IAnalyzer
    {
        public string Name => "architecture";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            var usageMap = referencias
                .GroupBy(r => r.ToType)
                .ToDictionary(g => g.Key, g => g.Count());

            var items = new List<ArchitecturalClassificationItem>();

            foreach (var tipo in tipos)
            {
                var usage = usageMap.ContainsKey(tipo.Name)
                    ? usageMap[tipo.Name]
                    : 0;

                var layer = LayerRuleEvaluator.ResolveLayer(
                    tipo,
                    context.Config.LayerRules
                );

                var folder = StructuralAlignmentEvaluator.ExtractFolder(
                    tipo.DeclaredInFile
                );

                var namespaceAlignment =
                    StructuralAlignmentEvaluator.EvaluateNamespaceAlignment(
                        folder,
                        tipo.Namespace
                    );

                var structuralStatus =
                    StructuralAlignmentEvaluator.EvaluateStructuralStatus(
                        folder,
                        tipo.Namespace,
                        layer
                    );

                var status = DetectStatus(usage);
                var removal = DetectRemovalCandidate(status);

                items.Add(new ArchitecturalClassificationItem
                {
                    TypeName = tipo.Name,
                    Namespace = tipo.Namespace,
                    Folder = folder,
                    Layer = layer,
                    NamespaceAlignment = namespaceAlignment,
                    StructuralStatus = structuralStatus,
                    Status = status,
                    RemovalCandidate = removal,
                    UsageCount = usage
                });
            }

            return new ArchitecturalClassificationResult(items);
        }

        /// <summary>
        /// Define o status estrutural do tipo.
        /// </summary>
        private string DetectStatus(int usage)
        {
            return usage == 0
                ? "Morto Absoluto"
                : "Ativo";
        }

        /// <summary>
        /// Determina se o tipo é candidato à remoção.
        /// </summary>
        private string DetectRemovalCandidate(string status)
        {
            return status switch
            {
                "Morto Absoluto" => "SIM",
                _ => "NÃO"
            };
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Infrastructure;
using RefactorScope.Core.Structure;
using RefactorScope.Core.Model;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Classifica os tipos do sistema em camadas arquiteturais
    /// com base em regras configuráveis (layerRules).
    ///
    /// ⚠ IMPORTANTE (ADR-EXP-007):
    /// Esta análise é puramente estrutural.
    /// 
    /// - NÃO declara morte definitiva.
    /// - NÃO confirma Structural Candidate.
    /// - NÃO toma decisão de remoção.
    ///
    /// A ausência de referência é apenas um indício estrutural.
    /// A confirmação final é responsabilidade do modelo probabilístico.
    /// </summary>
    public class ArchitecturalClassificationAnalyzer : IAnalyzer
    {
        public string Name => "architecture";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            // Mapa de uso estrutural (quantas vezes um tipo é referenciado)
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

                var folderHierarchy = ExtractFolderHierarchy(
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
                var removal = DetectRemovalCandidate(
                    tipo.Name,
                    usage,
                    new HashSet<string>(),
                    new HashSet<string>()
                );


                items.Add(new ArchitecturalClassificationItem
                {
                    TypeName = tipo.Name,
                    Namespace = tipo.Namespace,
                    DeclaredInFile = TrimRootFolder(tipo.DeclaredInFile),
                    Folder = folder,
                    FolderHierarchy = folderHierarchy,
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
        ///
        /// ⚠ NÃO representa confirmação de zombie.
        /// Apenas indica ausência de referência direta no escopo analisado.
        /// </summary>
        private string DetectStatus(int usage)
        {
            return usage == 0
                ? "Sem Referência Estrutural"
                : "Referenciado";
        }

        /// <summary>
        /// Neutraliza qualquer decisão automática de remoção.
        ///
        /// ⚠ Remoção nunca é decidida por análise estrutural isolada.
        /// Sempre requer validação manual ou confirmação probabilística.
        /// </summary>
        private string DetectRemovalCandidate(
            string typeName,
            int usage,
            HashSet<string> unresolved,
            HashSet<string> patternSimilarity)
        {
            // Se possui uso, não é candidato
            if (usage > 0)
                return "N/A";

            // Entry point explícito
            if (typeName == "Program")
                return "N/A";

            // Similaridade arquitetural detectada
            if (patternSimilarity.Contains(typeName))
                return "Requer Análise";

            // Estruturalmente não explicado
            if (unresolved.Contains(typeName))
                return "Remoção Indicada";

            return "Requer Análise";
        }

        private static string TrimRootFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var parts = path.Split(Path.DirectorySeparatorChar);

            // remove o primeiro segmento (ex: RefactorScope)
            if (parts.Length > 1)
                return string.Join(Path.DirectorySeparatorChar, parts.Skip(1));

            return path;
        }

        private static string ExtractFolderHierarchy(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var directory = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(directory))
                return string.Empty;

            var normalized = directory
                .Replace("\\", "/");

            // remove possível raiz do projeto
            var parts = normalized.Split('/');

            if (parts.Length <= 1)
                return normalized;

            return string.Join("/", parts.Skip(1));
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Classifica tipos conforme heurísticas arquiteturais.
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

                var layer = DetectLayer(tipo);
                var status = DetectStatus(tipo, usage);
                var removal = DetectRemovalCandidate(status);

                items.Add(new ArchitecturalClassificationItem
                {
                    TypeName = tipo.Name,
                    Namespace = tipo.Namespace,
                    Layer = layer,
                    Status = status,
                    RemovalCandidate = removal,
                    UsageCount = usage
                });
            }

            return new ArchitecturalClassificationResult(items);
        }

        private string DetectLayer(dynamic tipo)
        {
            if (tipo.Name == "Program")
                return "Infra";

            if (tipo.Name.StartsWith("Aba"))
                return "UI";

            if (tipo.Namespace.Contains("Nucleo") ||
                tipo.Namespace.Contains("Limbic"))
                return "Core";

            return "Aplicação";
        }

        private string DetectStatus(dynamic tipo, int usage)
        {
            if (usage == 0)
                return "Morto Absoluto";

            return "Ativo";
        }

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
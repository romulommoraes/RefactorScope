using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Calcula Fan-Out estrutural por módulo.
    /// 
    /// Scope-safe:
    /// Opera apenas sobre o universo analisado.
    /// </summary>
    public class CouplingAnalyzer : IAnalyzer
    {
        public string Name => "coupling";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var fanOutPorModulo = new Dictionary<string, int>();
            var fanOutPorTipoPorModulo = new Dictionary<string, Dictionary<string, int>>();

            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            foreach (var tipo in tipos)
            {
                var modulo = ExtractTopFolder(tipo.DeclaredInFile);
                var fanOut = referencias.Count(r => r.FromType == tipo.Name);

                if (!fanOutPorModulo.ContainsKey(modulo))
                    fanOutPorModulo[modulo] = 0;

                fanOutPorModulo[modulo] += fanOut;

                if (!fanOutPorTipoPorModulo.ContainsKey(modulo))
                    fanOutPorTipoPorModulo[modulo] = new Dictionary<string, int>();

                fanOutPorTipoPorModulo[modulo][tipo.Name] = fanOut;
            }

            return new CouplingResult(fanOutPorModulo, fanOutPorTipoPorModulo);
        }

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
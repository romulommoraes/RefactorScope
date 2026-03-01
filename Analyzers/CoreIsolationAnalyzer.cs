using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    public class CoreIsolationAnalyzer : IAnalyzer
    {
        public string Name => "coreIsolation";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            var referenced = referencias
                .Select(r => r.ToType)
                .ToHashSet();

            var entryPoints = tipos
                .Where(t =>
                    t.Name == "Program" ||
                    t.Name.StartsWith("Aba"))
                .Select(t => t.Name)
                .ToHashSet();

            var isolatedCore = tipos
                .Where(t =>
                    IsCore(t.Namespace) &&
                    !referenced.Contains(t.Name) &&
                    !entryPoints.Contains(t.Name))
                .Select(t => t.Name)
                .ToList();

            return new CoreIsolationResult(isolatedCore);
        }

        private bool IsCore(string ns)
        {
            return ns.Contains("Nucleo") ||
                   ns.Contains("Limbic");
        }
    }
}
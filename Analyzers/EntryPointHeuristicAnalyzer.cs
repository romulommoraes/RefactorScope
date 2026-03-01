using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    public class EntryPointHeuristicAnalyzer : IAnalyzer
    {
        public string Name => "entrypoints";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var entryPoints = context.Model.Tipos
                .Where(t =>
                    t.Name == "Program" ||
                    t.Name.StartsWith("Aba"))
                .Select(t => t.Name)
                .ToList();

            return new EntryPointHeuristicResult(entryPoints);
        }
    }
}
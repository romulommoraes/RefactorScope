using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    public class EntryPointHeuristicResult : IAnalysisResult
    {
        public IReadOnlyList<string> EntryPoints { get; }

        public EntryPointHeuristicResult(IReadOnlyList<string> entryPoints)
        {
            EntryPoints = entryPoints;
        }
    }
}
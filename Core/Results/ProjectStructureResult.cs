using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    public class ProjectStructureResult : IAnalysisResult
    {
        public IReadOnlyList<string> Lines { get; }

        public ProjectStructureResult(IEnumerable<string> lines)
        {
            Lines = lines.ToList();
        }
    }
}
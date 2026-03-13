using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado da classificação arquitetural.
    /// </summary>
    public class ArchitecturalClassificationResult : IAnalysisResult
    {
        public IReadOnlyList<ArchitecturalClassificationItem> Items { get; }

        public ArchitecturalClassificationResult(IReadOnlyList<ArchitecturalClassificationItem> items)
        {
            Items = items;
        }
    }
}
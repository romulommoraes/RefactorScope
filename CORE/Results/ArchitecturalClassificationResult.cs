using RefactorScope.Core.Abstractions;

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

    public class ArchitecturalClassificationItem
    {
        public string TypeName { get; init; } = string.Empty;
        public string Namespace { get; init; } = string.Empty;
        public string Layer { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string RemovalCandidate { get; init; } = string.Empty;
        public int UsageCount { get; init; }
    }
}
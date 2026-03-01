using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Representa tipos da camada Core isolados estruturalmente.
    /// </summary>
    public class CoreIsolationResult : IAnalysisResult
    {
        public IReadOnlyList<string> IsolatedCoreTypes { get; }

        public CoreIsolationResult(IReadOnlyList<string> isolatedCoreTypes)
        {
            IsolatedCoreTypes = isolatedCoreTypes;
        }
    }
}
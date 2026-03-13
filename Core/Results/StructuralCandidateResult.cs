using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado da análise de tipos potencialmente não utilizados.
    /// </summary>
    public class StructuralCandidateResult : IAnalysisResult
    {
        /// <summary>
        /// Tipos que não possuem referências externas.
        /// </summary>
        public IReadOnlyList<string> StructuralCandidateTypes { get; }

        public StructuralCandidateResult(IReadOnlyList<string> zombieTypes)
        {
            StructuralCandidateTypes = zombieTypes;
        }
    }
}
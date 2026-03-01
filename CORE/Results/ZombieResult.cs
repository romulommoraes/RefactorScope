using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado da análise de tipos potencialmente não utilizados.
    /// </summary>
    public class ZombieResult : IAnalysisResult
    {
        /// <summary>
        /// Tipos que não possuem referências externas.
        /// </summary>
        public IReadOnlyList<string> ZombieTypes { get; }

        public ZombieResult(IReadOnlyList<string> zombieTypes)
        {
            ZombieTypes = zombieTypes;
        }
    }
}
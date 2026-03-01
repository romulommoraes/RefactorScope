using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Representa o relatório consolidado da execução.
    /// Agrupa os resultados produzidos pelos analisadores.
    /// </summary>
    public class ConsolidatedReport
    {
        /// <summary>
        /// Resultados produzidos pelos analisadores.
        /// </summary>
        public IReadOnlyCollection<IAnalysisResult> Results { get; }

        /// <summary>
        /// Timestamp da execução.
        /// </summary>
        public DateTime ExecutionTime { get; }

        /// <summary>
        /// Escopo alvo da análise.
        /// </summary>
        public string TargetScope { get; }

        public ConsolidatedReport(
            IReadOnlyCollection<IAnalysisResult> results,
            DateTime executionTime,
            string targetScope)
        {
            Results = results;
            ExecutionTime = executionTime;
            TargetScope = targetScope;
        }
    }
}
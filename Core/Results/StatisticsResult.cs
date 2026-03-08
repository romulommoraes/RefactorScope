using RefactorScope.Core.Abstractions;
using RefactorScope.Statistics.Models;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado do módulo de estatísticas.
    /// </summary>
    public class StatisticsResult : IAnalysisResult
    {
        public string Name => "Statistics";

        public StatisticsReport Report { get; }

        public StatisticsResult(StatisticsReport report)
        {
            Report = report;
        }
    }
}
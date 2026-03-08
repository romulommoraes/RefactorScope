using RefactorScope.Core.Abstractions;
using RefactorScope.Estimation.Models;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado da estimativa de esforço de refatoração.
    /// </summary>
    public class EffortEstimateResult : IAnalysisResult
    {
        public string Name => "RefactorEffort";

        public EffortEstimate Estimate { get; }

        public EffortEstimateResult(EffortEstimate estimate)
        {
            Estimate = estimate;
        }
    }
}
using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Representa o resultado da avaliação de Fitness Gates.
    /// Define o estado de prontidão arquitetural do sistema.
    /// </summary>
    public class FitnessGateResult : IAnalysisResult
    {
        public IReadOnlyList<FitnessGateStatus> Gates { get; }

        public bool HasFailure => Gates.Any(g => g.Status == GateStatus.Fail);

        public FitnessGateResult(IReadOnlyList<FitnessGateStatus> gates)
        {
            Gates = gates;
        }
    }
}
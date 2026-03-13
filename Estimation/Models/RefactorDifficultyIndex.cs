namespace RefactorScope.Estimation.Models
{
    /// <summary>
    /// Representa os componentes individuais do Refactor Difficulty Index (RDI).
    /// 
    /// O RDI é composto por quatro dimensões principais:
    /// 
    /// • StructuralRisk        → problemas estruturais detectados
    /// • CouplingPressure      → pressão arquitetural causada por acoplamento
    /// • SizePressure          → pressão estrutural causada pelo tamanho do código
    /// • RefactorActions       → tipos de refactor detectados heurísticamente
    /// 
    /// Cada dimensão é normalizada entre 0 e 25.
    /// O índice final varia entre 0 e 100.
    /// </summary>
    public record RefactorDifficultyIndex(
        double StructuralRisk,
        double CouplingPressure,
        double SizePressure,
        double RefactorActions
    )
    {
        /// <summary>
        /// Soma total do índice.
        /// </summary>
        public int Total =>
            (int)Math.Round(
                StructuralRisk +
                CouplingPressure +
                SizePressure +
                RefactorActions
            );
    }
}
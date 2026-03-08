namespace RefactorScope.Core.Configuration
{
    /// <summary>
    /// Configuração do módulo de estimativa de esforço de refatoração.
    /// </summary>
    public class EstimatorConfig
    {
        /// <summary>
        /// Ativa ou desativa o EffortEstimator.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
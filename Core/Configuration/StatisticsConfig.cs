// ==========================================
// ARQUIVO: StatisticsConfig.cs
// CAMINHO: RefactorScope\Core\Configuration\StatisticsConfig.cs
// ==========================================
namespace RefactorScope.Core.Configuration
{
    /// <summary>
    /// Configuração para o módulo de validação estatística (Self-Validation Layer).
    /// </summary>
    public class StatisticsConfig
    {
        /// <summary>
        /// Define se a camada estatística observacional deve ser executada.
        /// Padrão: true.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
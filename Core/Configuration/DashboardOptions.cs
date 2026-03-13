namespace RefactorScope.Core.Configuration
{
    /// <summary>
    /// Configuração visual dos dashboards HTML.
    /// </summary>
    public sealed class DashboardOptions
    {
        /// <summary>
        /// Nome lógico do tema visual.
        ///
        /// Valores suportados:
        /// - midnight-blue
        /// - ember-ops
        /// - neon-grid
        /// </summary>
        public string Theme { get; set; } = "midnight-blue";
    }
}
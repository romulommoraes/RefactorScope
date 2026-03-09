namespace RefactorScope.Exporters.Styling
{
    /// <summary>
    /// Resolve o arquivo CSS do tema visual dos dashboards.
    ///
    /// Convenção
    /// ---------
    /// O sistema usa:
    /// - dashboard-base.css
    /// - dashboard-components.css
    /// - dashboard-theme-*.css
    ///
    /// Esta classe recebe o nome lógico do tema vindo da configuração
    /// e retorna o nome do arquivo CSS correspondente.
    ///
    /// Temas suportados
    /// ----------------
    /// - midnight-blue
    /// - ember-ops
    /// - neon-grid
    ///
    /// Fallback
    /// --------
    /// Quando o tema não é informado ou é inválido,
    /// o sistema usa midnight-blue como padrão.
    /// </summary>
    public static class DashboardThemeSelector
    {
        public const string DefaultThemeFile = "dashboard-theme-midnight-blue.css";

        public static string ResolveFileName(string? themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return DefaultThemeFile;

            return themeName.Trim().ToLowerInvariant() switch
            {
                "midnight-blue" => "dashboard-theme-midnight-blue.css",
                "ember-ops" => "dashboard-theme-ember-ops.css",
                "neon-grid" => "dashboard-theme-neon-grid.css",
                _ => DefaultThemeFile
            };
        }
    }
}
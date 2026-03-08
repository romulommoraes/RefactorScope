namespace RefactorScope.Exporters.Styling
{
    /// <summary>
    /// Resolve o arquivo CSS do tema visual dos dashboards.
    ///
    /// Estratégia
    /// ----------
    /// O sistema usa:
    /// - dashboard-base.css
    /// - dashboard-components.css
    /// - theme-*.css
    ///
    /// Este seletor mapeia o nome informado em configuração
    /// para o arquivo de tema correspondente.
    ///
    /// Convenção
    /// ---------
    /// O valor retornado deve apontar apenas para o nome do arquivo.
    /// O chamador é responsável por montar o path final:
    ///
    /// assets/css/{themeFile}
    ///
    /// Fallback
    /// --------
    /// Quando o tema não é informado ou é inválido,
    /// o sistema usa CyberBlue como padrão.
    /// </summary>
    public static class DashboardThemeSelector
    {
        public const string DefaultTheme = "theme-cyberblue.css";

        public static string ResolveFileName(string? themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return DefaultTheme;

            return themeName.Trim().ToLowerInvariant() switch
            {
                "cyberblue" => "theme-cyberblue.css",
                "blue" => "theme-cyberblue.css",

                "cyberorange" => "theme-cyberorange.css",
                "orange" => "theme-cyberorange.css",

                "cyberyellow" => "theme-cyberyellow.css",
                "yellow" => "theme-cyberyellow.css",

                "matrixcyan" => "theme-matrixcyan.css",
                "cyan" => "theme-matrixcyan.css",

                _ => DefaultTheme
            };
        }
    }
}
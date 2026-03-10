using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

using RefactorScope.Exporters.Dashboards;

using RefactorScope.Exporters.Reports;
using RefactorScope.Exporters.Styling;



namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter da família arquitetural.
    ///
    /// Objetivo
    /// --------
    /// Integrar os artefatos arquiteturais ao pipeline padronizado de IExporter
    /// mantendo compatibilidade com o novo shell visual compartilhado da suíte HTML.
    ///
    /// Estratégia atual
    /// ----------------
    /// Este adapter publica dois artefatos complementares:
    ///
    /// 1. Relatorio_Arquitetural.md
    ///    - leitura textual rápida
    ///    - exportação documental portátil
    ///    - útil fora do navegador
    ///
    /// 2. ArchitecturalDashboard.html
    ///    - visão executiva visual
    ///    - compatível com tema compartilhado
    ///    - integrado ao hub HTML
    ///
    /// Observação
    /// ----------
    /// Este adapter não duplica regra de negócio.
    /// Ele apenas orquestra os exporters arquiteturais
    /// e garante a infraestrutura visual necessária.
    /// </summary>
    public sealed class ArchitecturalDashboardExporterAdapter : IExporter
    {
        public string Name => "architectural-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var markdownPath = Path.Combine(outputPath, "Relatorio_Arquitetural.md");
            var htmlPath = Path.Combine(outputPath, "ArchitecturalDashboard.html");

            var themeFileName = ResolveThemeFileName(context);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            var markdownExporter = new MarkdownReportExporter();
            markdownExporter.Export(report, markdownPath);

            var htmlExporter = new ArchitecturalDashboardExporter();
            htmlExporter.Export(report, htmlPath, themeFileName);
        }

        private static string ResolveThemeFileName(AnalysisContext context)
        {
            try
            {
                var config = context?.Config;
                var themeName = TryReadDashboardTheme(config);
                return DashboardThemeSelector.ResolveFileName(themeName);
            }
            catch
            {
                return DashboardThemeSelector.DefaultThemeFile;
            }
        }

        private static string? TryReadDashboardTheme(RefactorScopeConfig? config)
        {
            if (config == null)
                return null;

            var prop = config.GetType().GetProperty("DashboardTheme");
            if (prop == null)
                return null;

            return prop.GetValue(config) as string;
        }
    }
}
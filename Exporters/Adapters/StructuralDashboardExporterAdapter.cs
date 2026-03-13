using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using RefactorScope.Exporters.Dashboards;

namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter da família estrutural.
    ///
    /// Objetivo
    /// --------
    /// Integrar o dashboard estrutural ao pipeline padronizado de IExporter,
    /// usando o mesmo shell visual compartilhado do Hub HTML.
    ///
    /// Estratégia atual
    /// ----------------
    /// Este adapter:
    /// - resolve o tema visual a partir do contexto/configuração
    /// - garante os assets compartilhados da suíte HTML
    /// - delega a geração do dashboard ao exporter estrutural
    ///
    /// Observação
    /// ----------
    /// A lógica analítica continua no StructuralInventoryExporter.
    /// Este adapter apenas faz a orquestração do pipeline visual.
    /// </summary>
    public sealed class StructuralDashboardExporterAdapter : IExporter
    {
        public string Name => "structural-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var htmlPath = Path.Combine(outputPath, "StructuralDashboard.html");
            var themeFileName = ResolveThemeFileName(context);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            var exporter = new StructuralInventoryExporter();
            exporter.Export(report, htmlPath, themeFileName);
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
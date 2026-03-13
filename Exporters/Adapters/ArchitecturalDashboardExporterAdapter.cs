using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Core.Reporting;
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
    /// </summary>
    public sealed class ArchitecturalDashboardExporterAdapter : IExporter
    {
        public string Name => "architectural-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            ExportDirect(
                context,
                report,
                parsingResult: null,
                outputPath);
        }

        public static void ExportDirect(
            AnalysisContext context,
            ConsolidatedReport report,
            IParserResult? parsingResult,
            string outputPath)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

            Directory.CreateDirectory(outputPath);

            var legacyMarkdownPath = Path.Combine(outputPath, "Relatorio_Arquitetural.md");
            var executiveMarkdownPath = Path.Combine(outputPath, "Relatorio_Executivo.md");
            var htmlPath = Path.Combine(outputPath, "ArchitecturalDashboard.html");

            var themeFileName = ResolveThemeFileName(context);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            // --------------------------------------------
            // 1. Markdown arquitetural legado
            // --------------------------------------------
            var legacyMarkdownExporter = new MarkdownReportExporter();
            legacyMarkdownExporter.Export(report, legacyMarkdownPath);

            // --------------------------------------------
            // 2. Relatório executivo com telemetria do parser
            // --------------------------------------------
            var snapshot = ReportSnapshotBuilder.Build(report, parsingResult);

            var executiveMarkdownExporter = new ExecutiveMarkdownReportExporter();
            executiveMarkdownExporter.Export(snapshot, executiveMarkdownPath);

            // --------------------------------------------
            // 3. Dashboard arquitetural HTML
            // --------------------------------------------
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
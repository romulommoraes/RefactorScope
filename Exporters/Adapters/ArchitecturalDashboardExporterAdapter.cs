using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Core.Reporting;
using RefactorScope.Core.Parsing;

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
    /// Este adapter publica três artefatos complementares:
    ///
    /// 1. Relatorio_Arquitetural.md
    ///    - leitura textual arquitetural legada
    ///    - exportação documental portátil
    ///    - útil fora do navegador
    ///
    /// 2. Relatorio_Executivo.md
    ///    - leitura executiva consolidada
    ///    - baseada em ReportSnapshot
    ///    - preparada para convergir com batch / analytics
    ///
    /// 3. ArchitecturalDashboard.html
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
            // 2. Novo relatório executivo
            // --------------------------------------------
            // Neste estágio, o ReportSnapshot deve ser a fonte canônica do relatório executivo.
            // Como o ParserResult não faz parte do contrato padrão de Results do AnalysisContext,
            // evitamos acoplamento frágil aqui e construímos o snapshot a partir do ConsolidatedReport.
            // Quando a telemetria do parser estiver formalmente disponível no pipeline,
            // este ponto pode ser enriquecido sem alterar o exporter.
            var snapshot = ReportSnapshotBuilder.Build(report, null);

            var executiveMarkdownExporter = new ExecutiveMarkdownReportExporter();
            executiveMarkdownExporter.Export(snapshot, executiveMarkdownPath);

            // --------------------------------------------
            // 3. Dashboard arquitetural HTML
            // --------------------------------------------
            var htmlExporter = new ArchitecturalDashboardExporter();
            htmlExporter.Export(report, htmlPath, themeFileName);
        }

        private static ParserResult? TryGetParserResult(AnalysisContext context)
        {
            if (context?.Results == null)
                return null;

            return context.Results
                .OfType<ParserResult>()
                .FirstOrDefault();
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
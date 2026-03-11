using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Core.Configuration;
using RefactorScope.Exporters.Styling;
using System.Text;

namespace RefactorScope.Exporters.Dashboards
{
    /// <summary>
    /// Exportador do Hub HTML.
    ///
    /// Responsabilidade
    /// ----------------
    /// Gerar apenas o index.html do hub visual da suíte de dashboards.
    ///
    /// Este exporter:
    /// - garante assets CSS compartilhados
    /// - resolve o tema visual
    /// - detecta quais dashboards já foram gerados
    /// - constrói o ponto central de navegação
    /// </summary>
    public sealed class HtmlDashboardExporter : IExporter
    {
        public string Name => "html-dashboard-hub";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            ExportHub(
                context,
                report,
                parsingResult: null,
                outputPath: outputPath);
        }

        public void ExportHub(
            AnalysisContext context,
            ConsolidatedReport report,
            IParserResult? parsingResult,
            string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var themeFileName = ResolveThemeFileName(context);

            //legado
            //EnsureCssAssets(outputPath, themeFileName);
            //EnsureVendorAssets(outputPath);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            var structuralFileName =
                File.Exists(Path.Combine(outputPath, "StructuralDashboard.html"))
                    ? "StructuralDashboard.html"
                    : string.Empty;

            var architecturalFileName =
                File.Exists(Path.Combine(outputPath, "ArchitecturalDashboard.html"))
                    ? "ArchitecturalDashboard.html"
                    : string.Empty;

            var architecturalMarkdownFileName =
                File.Exists(Path.Combine(outputPath, "Relatorio_Arquitetural.md"))
                    ? "Relatorio_Arquitetural.md"
                    : string.Empty;

            var parsingFileName =
                File.Exists(Path.Combine(outputPath, "ParsingDashboard.html"))
                    ? "ParsingDashboard.html"
                    : string.Empty;

            var qualityFileName =
                File.Exists(Path.Combine(outputPath, "QualityDashboard.html"))
                    ? "QualityDashboard.html"
                    : string.Empty;

            var hubExporter = new HubDashboardExporter();

            hubExporter.Export(
                report,
                outputPath,
                parserName: parsingResult?.ParserName ?? "Unavailable",
                parserConfidence: parsingResult?.Confidence ?? 0,
                parsingExecution: parsingResult?.Stats?.ExecutionTime ?? TimeSpan.Zero,
                parsingFiles: parsingResult?.Model?.Arquivos.Count ?? 0,
                parsingTypes: parsingResult?.Model?.Tipos.Count ?? 0,
                parsingReferences: parsingResult?.Model?.Referencias.Count ?? 0,
                structuralFileName: structuralFileName,
                architecturalFileName: architecturalFileName,
                parsingFileName: parsingFileName,
                qualityFileName: qualityFileName,
                architecturalMarkdownFileName: architecturalMarkdownFileName,
                themeFileName: themeFileName);
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
            return config?.Dashboard?.Theme;
        }
         

    }
}
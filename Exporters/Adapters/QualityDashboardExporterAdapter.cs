using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Dashboards;
using RefactorScope.Exporters.Styling;
namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter do dashboard Quality / Gates.
    ///
    /// Observação:
    /// -----------
    /// Como este dashboard depende de parsing telemetry real,
    /// a integração mais limpa é chamá-lo explicitamente no runner
    /// que já possui parserName / confidence / files / types / refs.
    ///
    /// Este adapter pode ser mantido para futura padronização,
    /// mas o uso direto via runner pode ser mais simples no momento.
    /// </summary>
    public sealed class QualityDashboardExporterAdapter : IExporter
    {
        public string Name => "quality-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            // Este adapter propositalmente não faz nada por enquanto,
            // pois o dashboard de qualidade depende de dados de parsing
            // explícitos que não vivem naturalmente no AnalysisContext.
            //
            // Recomenda-se chamada direta no runner de exportação:
            // QualityDashboardExporter.Export(...)
        }

        public static void ExportDirect(
            AnalysisContext context,
            ConsolidatedReport report,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences,
            string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var htmlPath = Path.Combine(outputPath, "QualityDashboard.html");
            var themeFileName = ResolveThemeFileName(context);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            var exporter = new QualityDashboardExporter();
            exporter.Export(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences,
                htmlPath,
                themeFileName);
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
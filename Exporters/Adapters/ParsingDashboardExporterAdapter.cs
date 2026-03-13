using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

using RefactorScope.Exporters.Dashboards;
using RefactorScope.Exporters.Styling;

namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter da família de parsing.
    ///
    /// Objetivo
    /// --------
    /// Integrar o dashboard de parsing ao pipeline padronizado de IExporter,
    /// alinhando-o ao shell visual compartilhado da suíte HTML.
    ///
    /// Estratégia atual
    /// ----------------
    /// Este adapter:
    /// - recupera o parser result a partir do contexto
    /// - resolve o tema configurado
    /// - garante os assets compartilhados
    /// - delega a geração do HTML ao exporter de parsing
    ///
    /// Observação
    /// ----------
    /// A lógica analítica continua concentrada em ParsingDashboardExporter.
    /// Este adapter apenas orquestra a integração com a suíte HTML.
    /// </summary>
    public sealed class ParsingDashboardExporterAdapter : IExporter
    {
        public string Name => "parsing-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var parserResult = TryGetParserResult(context);
            if (parserResult == null)
                return;

            Directory.CreateDirectory(outputPath);

            var htmlPath = Path.Combine(outputPath, "ParsingDashboard.html");
            var themeFileName = ResolveThemeFileName(context);

            DashboardAssetCopier.CopyAll(outputPath, themeFileName);

            var exporter = new ParsingDashboardExporter();
            exporter.Export(parserResult, htmlPath, themeFileName);
        }

        private static IParserResult? TryGetParserResult(AnalysisContext context)
        {
            try
            {
                var prop = context.GetType().GetProperty("ParserResult");
                return prop?.GetValue(context) as IParserResult;
            }
            catch
            {
                return null;
            }
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
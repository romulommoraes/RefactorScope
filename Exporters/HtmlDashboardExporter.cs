using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Estimation.Models;
using System.Text;

namespace RefactorScope.Exporters
{
    public class HtmlDashboardExporter : IExporter
    {
        public string Name => "html-dashboard";

        public void Export(AnalysisContext context, ConsolidatedReport report, string outputPath)
        {
            var dashboardDir = Path.Combine(outputPath, "dashboard");
            Directory.CreateDirectory(dashboardDir);

            EnsureVendorAssets(dashboardDir);

            // -------------------------------------------------
            // 1) Structural dashboard
            // -------------------------------------------------
            var structuralExporter = new StructuralInventoryExporter();
            structuralExporter.Export(report, dashboardDir);

            // -------------------------------------------------
            // 2) Parsing dashboard
            // -------------------------------------------------
            string parsingFileName = "ParsingDashboard.html";

            var parserResult = TryGetParserResult(context);

            if (parserResult != null)
            {
                var parsingExporter = new ParsingDashboardExporter();
                parsingExporter.Export(parserResult, dashboardDir);
            }

            // -------------------------------------------------
            // 3) Architectural report (markdown for now)
            // -------------------------------------------------
            string architecturalFileName = "Relatorio_Arquitetural.md";
            var markdownPath = Path.Combine(dashboardDir, architecturalFileName);

            var markdownExporter = new MarkdownReportExporter();
            markdownExporter.Export(report, markdownPath);

            // -------------------------------------------------
            // 4) Hub index.html
            // -------------------------------------------------
            var hubExporter = new HubDashboardExporter();

            var parserName = parserResult?.ParserName ?? "Unknown";
            var parserConfidence = parserResult?.Confidence ?? 0;
            var parsingExecution = parserResult?.Stats?.ExecutionTime ?? TimeSpan.Zero;
            var parsingFiles = parserResult?.Model?.Arquivos.Count ?? 0;
            var parsingTypes = parserResult?.Model?.Tipos.Count ?? 0;
            var parsingReferences = parserResult?.Model?.Referencias.Count ?? 0;

            var effort = TryGetEffortEstimate(context);

            hubExporter.Export(
                report,
                dashboardDir,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences,
                effort,
                structuralFileName: "StructuralDashboard.html",
                architecturalFileName: architecturalFileName,
                parsingFileName: parsingFileName,
                qualityFileName: "QualityDashboard.html");
        }

        // -------------------------------------------------
        // Helpers
        // -------------------------------------------------

        private static void EnsureVendorAssets(string dashboardDir)
        {
            var vendorDir = Path.Combine(dashboardDir, "assets", "vendor");
            Directory.CreateDirectory(vendorDir);

            var chartsCssPath = Path.Combine(vendorDir, "charts.min.css");
            var augmentedUiPath = Path.Combine(vendorDir, "augmented-ui.min.css");

            // Placeholder local vendor files.
            // Replace with the real library contents when you add them to the project.
            if (!File.Exists(chartsCssPath))
            {
                File.WriteAllText(chartsCssPath, PlaceholderChartsCss(), Encoding.UTF8);
            }

            if (!File.Exists(augmentedUiPath))
            {
                File.WriteAllText(augmentedUiPath, PlaceholderAugmentedUiCss(), Encoding.UTF8);
            }
        }

        private static string PlaceholderChartsCss()
        {
            return """
/* Placeholder Charts.css file.
   Replace with the official minified Charts.css file for production. */

.charts-css {
    width: 100%;
    border-collapse: collapse;
}

.charts-css caption {
    margin-bottom: 10px;
}

.charts-css tbody tr {
    height: 42px;
}

.charts-css tbody tr th {
    text-align: left;
    font-weight: 600;
    padding-right: 12px;
    white-space: nowrap;
}

.charts-css tbody tr td {
    position: relative;
    background: rgba(255,255,255,0.03);
    overflow: hidden;
}

.charts-css tbody tr td::before {
    content: "";
    position: absolute;
    inset: 0;
    width: calc(var(--size, 0) * 100%);
    background: var(--color, linear-gradient(180deg, #39d5ff, #2f88ff));
    opacity: .9;
}

.charts-css tbody tr td .data {
    position: relative;
    z-index: 1;
    display: inline-block;
    padding: 10px 12px;
    color: #fff;
    font-weight: 700;
}
""";
        }

        private static string PlaceholderAugmentedUiCss()
        {
            return """
/* Placeholder augmented-ui file.
   Replace with the official augmented-ui CSS file for production. */

[augmented-ui] {
    clip-path: polygon(
        10px 0,
        calc(100% - 10px) 0,
        100% 10px,
        100% calc(100% - 10px),
        calc(100% - 10px) 100%,
        10px 100%,
        0 calc(100% - 10px),
        0 10px
    );
}
""";
        }

        private static dynamic? TryGetParserResult(AnalysisContext context)
        {
            try
            {
                var prop = context.GetType().GetProperty("ParserResult");
                return prop?.GetValue(context);
            }
            catch
            {
                return null;
            }
        }

        private static EffortEstimate? TryGetEffortEstimate(AnalysisContext context)
        {
            try
            {
                var prop = context.GetType().GetProperty("EffortEstimate");
                return prop?.GetValue(context) as EffortEstimate;
            }
            catch
            {
                return null;
            }
        }
    }
}
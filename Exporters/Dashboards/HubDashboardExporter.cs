using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards
{
    internal sealed class HubDashboardExporter
    {
        public void Export(
            ConsolidatedReport report,
            string outputDirectory,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences,
            string structuralFileName,
            string architecturalFileName,
            string parsingFileName,
            string qualityFileName,
            string architecturalMarkdownFileName,
            string themeFileName)
        {
            Directory.CreateDirectory(outputDirectory);

            var html = GenerateHtml(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences,
                structuralFileName,
                architecturalFileName,
                parsingFileName,
                qualityFileName,
                architecturalMarkdownFileName,
                themeFileName);

            File.WriteAllText(
                Path.Combine(outputDirectory, "index.html"),
                html,
                Encoding.UTF8);
        }

        private string GenerateHtml(
            ConsolidatedReport report,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences,
            string structuralFileName,
            string architecturalFileName,
            string parsingFileName,
            string qualityFileName,
            string architecturalMarkdownFileName,
            string themeFileName)
        {
            var metrics = DashboardMetricsCalculator.BuildHubMetrics(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences);

            var targetName = string.IsNullOrWhiteSpace(report.TargetScope)
                ? "Unknown Scope"
                : Path.GetFileName(report.TargetScope.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            var sb = new StringBuilder();

            sb.AppendLine(DashboardHtmlShell.RenderDocumentStart(
                "RefactorScope Hub",
                themeFileName));

            sb.AppendLine($"""
<div class="topbar" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <div class="brand">
        <div class="brand-kicker">RefactorScope // Analysis Hub</div>
        <h1>Structural Command Nexus</h1>
        <div class="subtitle">Target Project: <b>{Html(targetName)}</b></div>
    </div>

    <div class="run-meta">
        <div><b>Generated:</b> {report.ExecutionTime:yyyy-MM-dd HH:mm} UTC</div>
        <div><b>Parser:</b> {Html(metrics.ParserName)}</div>
        <div><b>Target Scope:</b> {Html(report.TargetScope)}</div>
        <div><b>Readiness:</b> {Html(metrics.FitStatus)}</div>
    </div>
</div>
""");

            sb.AppendLine("""<div class="nav-grid">""");

            if (!string.IsNullOrWhiteSpace(structuralFileName))
            {
                sb.AppendLine(DashboardHtmlShell.RenderNavCard(
                    "Module // 01",
                    "Structural",
                    "Dead code candidates, namespace drift, unresolved review and structural classification.",
                    structuralFileName));
            }
            else
            {
                sb.AppendLine(DashboardHtmlShell.RenderUnavailableNavCard(
                    "Module // 01",
                    "Structural",
                    "Not generated in this execution."));
            }

            if (!string.IsNullOrWhiteSpace(architecturalFileName))
            {
                sb.AppendLine(DashboardHtmlShell.RenderNavCard(
                    "Module // 02",
                    "Architectural",
                    "Health by module, balance metrics, coupling and isolation analysis.",
                    architecturalFileName));
            }
            else
            {
                sb.AppendLine(DashboardHtmlShell.RenderUnavailableNavCard(
                    "Module // 02",
                    "Architectural",
                    "Not generated in this execution."));
            }

            if (!string.IsNullOrWhiteSpace(parsingFileName))
            {
                sb.AppendLine(DashboardHtmlShell.RenderNavCard(
                    "Module // 03",
                    "Parsing",
                    "Parser confidence, extraction density, throughput and parsed type coverage.",
                    parsingFileName));
            }
            else
            {
                sb.AppendLine(DashboardHtmlShell.RenderUnavailableNavCard(
                    "Module // 03",
                    "Parsing",
                    "Unavailable in this execution."));
            }

            if (!string.IsNullOrWhiteSpace(qualityFileName))
            {
                sb.AppendLine(DashboardHtmlShell.RenderNavCard(
                    "Module // 04",
                    "Quality / Gates",
                    "Fitness gates, warnings, quality signals and execution stability.",
                    qualityFileName));
            }
            else
            {
                sb.AppendLine(DashboardHtmlShell.RenderUnavailableNavCard(
                    "Module // 04",
                    "Quality / Gates",
                    "Not generated yet."));
            }

            sb.AppendLine("</div>");

            sb.AppendLine($"""
<div class="grid-kpis hub-kpis">
    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Total Classes</div>
        <div class="value">{metrics.Hygiene.TotalClasses}</div>
        <div class="hint">Structurally classified types in the analyzed scope</div>
    </div>

    <div class="kpi warning" augmented-ui="tr-clip bl-clip border">
        <div class="label">Dead Code Candidates</div>
        <div class="value">{metrics.DeadCodeCandidates}</div>
        <div class="hint">Types still compatible with the dead code hypothesis</div>
    </div>

    <div class="kpi alert" augmented-ui="tr-clip bl-clip border">
        <div class="label">Unresolved</div>
        <div class="value">{metrics.Unresolved}</div>
        <div class="hint">Candidates whose dead code hypothesis could not be refuted</div>
    </div>

    <div class="kpi {(metrics.Hygiene.SmellIndex > 40 ? "alert" : metrics.Hygiene.SmellIndex > 20 ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Code Smell Index</div>
        <div class="value">{metrics.Hygiene.SmellIndex:0.0}</div>
        <div class="hint">Composite structural hygiene score. Lower is better</div>
    </div>


    <div class="kpi {DashboardMetricsCalculator.GetBandCssClass(metrics.OverallSupportScore)}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Statistical Support</div>
        <div class="value" style="font-size:24px;">{Html(metrics.OverallSupportBand)}</div>
        <div class="hint">Overall heuristic support level for the current analysis snapshot</div>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Executive Statistical Support</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Support Matrix</h3>
    <table class="link-table compact-table">
        <thead>
            <tr>
                <th>Signal</th>
                <th>Support</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>Parser</td>
                <td><span class="badge {DashboardMetricsCalculator.GetBandBadgeCss(metrics.ParserSupportScore)}"><span class="badge-dot"></span> {Html(metrics.ParserSupportBand)}</span></td>
            </tr>
            <tr>
                <td>Structural</td>
                <td><span class="badge {DashboardMetricsCalculator.GetBandBadgeCss(metrics.StructuralSupportScore)}"><span class="badge-dot"></span> {Html(metrics.StructuralSupportBand)}</span></td>
            </tr>
            <tr>
                <td>Coupling</td>
                <td><span class="badge {DashboardMetricsCalculator.GetBandBadgeCss(metrics.CouplingSupportScore)}"><span class="badge-dot"></span> {Html(metrics.CouplingSupportBand)}</span></td>
            </tr>
            <tr>
                <td>Effort</td>
                <td><span class="badge {DashboardMetricsCalculator.GetBandBadgeCss(metrics.EffortSupportScore)}"><span class="badge-dot"></span> {Html(metrics.EffortSupportBand)}</span></td>
            </tr>
            <tr>
                <td><b>Overall</b></td>
                <td><span class="badge {DashboardMetricsCalculator.GetBandBadgeCss(metrics.OverallSupportScore)}"><span class="badge-dot"></span> <b>{Html(metrics.OverallSupportBand)}</b></span></td>
            </tr>
        </tbody>
    </table>
</div>
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Executive Interpretation</h3>
    <p style="margin-top:0;">
        {Html(metrics.SupportNarrative)}
    </p>

    <ul class="clean" style="margin-top:16px;">
        <li>Support levels are heuristic and should be read as evidence strength, not absolute certainty.</li>
        <li>Parser support reflects extraction confidence and structural usability.</li>
        <li>Structural support reflects how much of the dead code hypothesis could be refined or narrowed.</li>
        <li>Effort support should be interpreted cautiously when confidence is limited.</li>
    </ul>
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Run Overview</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            int parseMax = Math.Max(metrics.ParsingFiles, Math.Max(metrics.ParsingTypes, metrics.ParsingReferences));
            parseMax = Math.Max(parseMax, 1);

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Pipeline Telemetry</h3>

    <div class="status-row" style="margin-bottom:14px;">
        <div class="badge green"><span class="badge-dot"></span> Parser: {Html(metrics.ParserName)}</div>
        <div class="badge {(metrics.ParserConfidence >= 0.85 ? "green" : metrics.ParserConfidence >= 0.65 ? "amber" : "red")}"><span class="badge-dot"></span> Confidence: {metrics.ParserConfidence:P0}</div>
        <div class="badge {(string.Equals(metrics.FitStatus, "Ready", StringComparison.OrdinalIgnoreCase) ? "green" : string.Equals(metrics.FitStatus, "Attention Required", StringComparison.OrdinalIgnoreCase) ? "red" : "amber")}"><span class="badge-dot"></span> Gates: {Html(metrics.FitStatus)}</div>
    </div>

    <div class="chart-wrap">
        <table class="charts-css">
            <caption>Parsing / Scope / Quality Signals</caption>
            <tbody>
                <tr>
                    <th scope="row">Files</th>
                    <td style="--size: {SafeRatio(metrics.ParsingFiles, parseMax)};">
                        <span class="data">{metrics.ParsingFiles}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Types</th>
                    <td style="--size: {SafeRatio(metrics.ParsingTypes, parseMax)};">
                        <span class="data">{metrics.ParsingTypes}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Refs</th>
                    <td style="--size: {SafeRatio(metrics.ParsingReferences, parseMax)};">
                        <span class="data">{metrics.ParsingReferences}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Confidence</th>
                    <td style="--size: {Clamp01(metrics.ParserConfidence).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{metrics.ParserConfidence:P0}</span>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>

    <ul class="clean" style="margin-top:14px;">
        <li><b>Execution:</b> {metrics.ParsingExecution.TotalMilliseconds:0} ms</li>
        <li><b>Files:</b> {metrics.ParsingFiles} / <b>Types:</b> {metrics.ParsingTypes} / <b>References:</b> {metrics.ParsingReferences}</li>
        <li><b>Average Abstractness:</b> {metrics.AvgAbstractness:0.00}</li>
        <li><b>Average Instability:</b> {metrics.AvgInstability:0.00}</li>
        <li><b>Average Distance:</b> {metrics.AvgDistance:0.00}</li>
    </ul>
</div>
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Executive Analysis</h3>
    <ul class="clean">
        <li>{Html(metrics.CoreMessage)}</li>
        <li>{Html(metrics.DriftMessage)}</li>
        <li>{Html(metrics.UnresolvedMessage)}</li>
        <li>{Html(metrics.SmellMessage)}</li>
        <li>{(metrics.SolidAlerts > 0 ? $"SOLID alerts were detected: {metrics.SolidAlerts}." : "No SOLID alerts were detected.")}</li>
        <li>{(metrics.CouplingSuspects > 0 ? $"Implicit coupling hotspots were detected: {metrics.CouplingSuspects}." : "No implicit coupling hotspots were detected.")}</li>
    </ul>

    <div style="margin-top:16px;">
        <div class="badge {(metrics.Unresolved > 0 ? "red" : "green")}"><span class="badge-dot"></span> Unresolved: {metrics.Unresolved}</div>
        <div class="badge {(metrics.Hygiene.NamespaceDriftCount > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Namespace Drift: {metrics.Hygiene.NamespaceDriftCount}</div>
        <div class="badge {(metrics.SolidAlerts > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> SOLID Alerts: {metrics.SolidAlerts}</div>
        <div class="badge {(metrics.CouplingSuspects > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Implicit Coupling: {metrics.CouplingSuspects}</div>
    </div>
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Hub Navigation Matrix</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
        <h3>Available Dashboards</h3>
        <table class="link-table">
            <thead>
                <tr>
                    <th>Dashboard</th>
                    <th>Primary Focus</th>
                    <th>Target Signals</th>
                    <th>Open</th>
                </tr>
            </thead>
            <tbody>
""");

            if (!string.IsNullOrWhiteSpace(structuralFileName))
            {
                sb.AppendLine($"""
<tr>
    <td>Structural</td>
    <td>Dead code review / structural risk</td>
    <td>Dead Code Candidates, Pattern Similarity, Unresolved, Namespace Drift</td>
    <td><a href="{Html(structuralFileName)}">Open Structural Dashboard</a></td>
</tr>
""");
            }

            if (!string.IsNullOrWhiteSpace(architecturalFileName))
            {
                sb.AppendLine($"""
<tr>
    <td>Architectural</td>
    <td>Health by module / A-I-D balance</td>
    <td>Score, Coupling, Isolation, Abstractness, Instability, Distance</td>
    <td><a href="{Html(architecturalFileName)}">Open Architectural Dashboard</a></td>
</tr>
""");
            }

            if (!string.IsNullOrWhiteSpace(architecturalMarkdownFileName))
            {
                sb.AppendLine($"""
<tr>
    <td>Architectural Markdown</td>
    <td>Portable textual companion report</td>
    <td>Project structure, architectural explanation, metrics narrative</td>
    <td><a href="{Html(architecturalMarkdownFileName)}">Open Markdown Report</a></td>
</tr>
""");
            }

            if (!string.IsNullOrWhiteSpace(parsingFileName))
            {
                sb.AppendLine($"""
<tr>
    <td>Parsing</td>
    <td>Parser quality / structural extraction</td>
    <td>Confidence, Files, Types, References, Throughput</td>
    <td><a href="{Html(parsingFileName)}">Open Parsing Dashboard</a></td>
</tr>
""");
            }

            if (!string.IsNullOrWhiteSpace(qualityFileName))
            {
                sb.AppendLine($"""
<tr>
    <td>Quality / Gates</td>
    <td>Validation / execution quality</td>
    <td>Fitness Gates, Warnings, Stability, Crash Reports</td>
    <td><a href="{Html(qualityFileName)}">Open Quality Dashboard</a></td>
</tr>
""");
            }

            sb.AppendLine("""
            </tbody>
        </table>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Refactor Command Summary</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Code Hygiene Summary</h3>
    <ul class="clean">
        <li><b>Code Smell Index:</b> {metrics.Hygiene.SmellIndex:0.0}</li>
        <li><b>Hygiene Level:</b> {Html(metrics.Hygiene.HygieneLevel)}</li>
        <li><b>Dead Code Candidates:</b> {metrics.DeadCodeCandidates}</li>
        <li><b>Unresolved:</b> {metrics.Unresolved}</li>
        <li><b>Namespace Drift:</b> {metrics.Hygiene.NamespaceDriftCount}</li>
        <li><b>Isolated Core:</b> {metrics.Hygiene.IsolatedCoreCount}</li>
    </ul>
</div>
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Refactor Effort Estimation</h3>
    <ul class="clean">
        <li><b>RDI:</b> {metrics.EffortRdi}</li>
        <li><b>Difficulty:</b> {Html(metrics.EffortDifficulty)}</li>
        <li><b>Estimated Hours:</b> {metrics.EffortHours:0.0}</li>
        <li><b>Confidence:</b> {metrics.EffortConfidence:0.00}</li>
    </ul>

    <div class="chart-wrap" style="margin-top:16px;">
        <table class="charts-css">
            <caption>Estimated Refactor Load</caption>
            <tbody>
                <tr>
                    <th scope="row">Hours</th>
                    <td style="--size: {Clamp01(metrics.EffortHours / 80.0).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{metrics.EffortHours:0.0}h</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Confidence</th>
                    <td style="--size: {Clamp01(metrics.EffortConfidence).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{metrics.EffortConfidence:0.00}</span>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
                "Generated by RefactorScope Hub // Cyberpunk Publication Layout"));

            return sb.ToString();
        }

        private static string Html(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static double Clamp01(double value)
            => Math.Max(0, Math.Min(1, value));

        private static string SafeRatio(double value, double max)
        {
            if (max <= 0)
                return "0";

            return Clamp01(value / max).ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
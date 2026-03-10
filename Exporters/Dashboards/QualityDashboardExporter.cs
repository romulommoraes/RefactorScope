using RefactorScope.Core.Results;
using RefactorScope.Exporters.Dashboards;
using RefactorScope.Exporters.Styling;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards
{
    public sealed class QualityDashboardExporter
    {
        public void Export(
            ConsolidatedReport report,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences,
            string htmlPath,
            string themeFileName)
        {
            var html = GenerateHtml(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences,
                themeFileName);

            File.WriteAllText(htmlPath, html, Encoding.UTF8);
        }

        private string GenerateHtml(
            ConsolidatedReport report,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences,
            string themeFileName)
        {
            var metrics = DashboardMetricsCalculator.BuildQualityMetrics(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences);

            var charts = new QualityControlChartsRenderer();

            var gateReadiness = ComputeGateReadiness(metrics);
            var unresolvedPressure = ComputeUnresolvedPressure(metrics);

            var statisticsBand = DashboardMetricsCalculator.GetSupportBandFromScore(metrics.StatisticsCoverageScore);
            var statisticsBandCss = DashboardMetricsCalculator.GetBandCssClass(metrics.StatisticsCoverageScore);
            var statisticsBadgeCss = DashboardMetricsCalculator.GetBadgeCssForBand(metrics.StatisticsCoverageScore);

            var gateVisuals =
                metrics.GateRows
                .Select(row => new QualityControlChartsRenderer.QualityGateVisual(
                    row.Name,
                    MapGateStatus(row.Status),
                    row.Details))
                .ToList();

            var targetName = string.IsNullOrWhiteSpace(report.TargetScope)
                ? "Unknown Scope"
                : Path.GetFileName(report.TargetScope.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            var sb = new StringBuilder();

            sb.AppendLine(DashboardHtmlShell.RenderDocumentStart(
                "Quality / Gates Dashboard",
                themeFileName));

            sb.AppendLine($"""
<div class="topbar" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <div class="brand">
        <div class="brand-kicker">RefactorScope // Quality Module</div>
        <h1>Quality / Gates Dashboard</h1>
        <div class="subtitle">Target Project: <b>{Html(targetName)}</b></div>
    </div>

    <div class="run-meta">
        <div><b>Generated:</b> {report.ExecutionTime:yyyy-MM-dd HH:mm} UTC</div>
        <div><b>Parser:</b> {Html(metrics.ParserName)}</div>
        <div><b>Readiness:</b> {Html(metrics.FitStatus)}</div>
        <div><b>Target Scope:</b> {Html(report.TargetScope)}</div>
    </div>
</div>
""");

            sb.AppendLine($"""
<div class="grid-kpis quality-kpis">
    <div class="kpi {(string.Equals(metrics.FitStatus, "Ready", StringComparison.OrdinalIgnoreCase) ? "good" : string.Equals(metrics.FitStatus, "Attention Required", StringComparison.OrdinalIgnoreCase) ? "alert" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Fitness Gates</div>
        <div class="value" style="font-size:24px;">{Html(metrics.FitStatus)}</div>
        <div class="hint">Execution readiness derived from fitness gate validation</div>
    </div>

    <div class="kpi {metrics.ParserBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Parser Confidence</div>
        <div class="value">{metrics.ParserConfidence:P0}</div>
        <div class="hint">Heuristic confidence that the parsed model is structurally usable</div>
    </div>

    <div class="kpi {metrics.EffortBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Effort Confidence</div>
        <div class="value">{metrics.EffortConfidence:0.00}</div>
        <div class="hint">Confidence level attached to the current refactor effort estimate</div>
    </div>

    <div class="kpi {statisticsBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Statistics Support</div>
        <div class="value" style="font-size:24px;">{Html(statisticsBand)}</div>
        <div class="hint">Availability and completeness of the statistical validation payload</div>
    </div>

    <div class="kpi {(metrics.Hub.Unresolved > 0 ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Unresolved</div>
        <div class="value">{metrics.Hub.Unresolved}</div>
        <div class="hint">Candidates whose dead code hypothesis could not be refuted</div>
    </div>

    <div class="kpi {metrics.OverallReadinessCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Overall Readiness</div>
        <div class="value" style="font-size:24px;">{Html(metrics.OverallReadinessBand)}</div>
        <div class="hint">Executive quality band across gates, parser, effort and statistics</div>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Execution Quality Overview</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Executive Interpretation</h3>
    <p style="margin-top:0;">{Html(metrics.Narrative)}</p>

    <ul class="clean" style="margin-top:16px;">
        <li>Fitness gates summarize whether the execution appears ready for interpretation.</li>
        <li>Parser confidence reflects extraction usability, not absolute correctness.</li>
        <li>Effort confidence should be interpreted cautiously when statistical support is limited.</li>
        <li>Quality bands are executive heuristics and not formal proofs of validity.</li>
    </ul>
</div>
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Execution Snapshot</h3>

    <div class="status-row" style="margin-bottom:14px;">
        <div class="badge {DashboardMetricsCalculator.GetBadgeCssForBand(metrics.ParserConfidence)}"><span class="badge-dot"></span> Parser: {Html(metrics.ParserBand)}</div>
        <div class="badge {DashboardMetricsCalculator.GetBadgeCssForBand(metrics.EffortConfidence)}"><span class="badge-dot"></span> Effort: {Html(metrics.EffortBand)}</div>
        <div class="badge {statisticsBadgeCss}"><span class="badge-dot"></span> Statistics: {Html(statisticsBand)}</div>
        <div class="badge {DashboardMetricsCalculator.GetBadgeCssForBand(metrics.OverallReadinessScore)}"><span class="badge-dot"></span> Overall: {Html(metrics.OverallReadinessBand)}</div>
    </div>

    <ul class="clean">
        <li><b>Execution Time:</b> {metrics.ParsingExecution.TotalMilliseconds:0} ms</li>
        <li><b>Files:</b> {metrics.ParsingFiles}</li>
        <li><b>Types:</b> {metrics.ParsingTypes}</li>
        <li><b>References:</b> {metrics.ParsingReferences}</li>
        <li><b>Estimated Hours:</b> {metrics.EffortHours:0.0}</li>
        <li><b>RDI:</b> {metrics.EffortRdi:0.##}</li>
        <li><b>Statistics Support Score:</b> {metrics.StatisticsCoverageScore:0.00}</li>
    </ul>
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Trust Flow</h2>
        <div class="line"></div>
    </div>
    <div class="quality-half-grid">
""");

            sb.AppendLine($"""
<div class="panel half-panel chart-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    {charts.RenderQualityRadar(
        metrics.ParserConfidence,
        metrics.EffortConfidence,
        metrics.StatisticsCoverageScore,
        gateReadiness,
        unresolvedPressure,
        metrics.OverallReadinessScore)}
</div>
""");

            sb.AppendLine($"""
<div class="panel half-panel chart-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    {charts.RenderTrustFlowSankey(
        metrics.ParserConfidence,
        metrics.EffortConfidence,
        metrics.StatisticsCoverageScore,
        gateReadiness,
        metrics.OverallReadinessScore)}
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Fitness Gate Strip</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            if (gateVisuals.Count == 0)
            {
                sb.AppendLine("<p>No explicit fitness gate details were available for this execution.</p>");
            }
            else
            {
                sb.AppendLine(charts.RenderFitnessGateStrip(gateVisuals));
            }

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Fitness Gates // Detailed Table</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            if (metrics.GateRows.Count == 0)
            {
                sb.AppendLine("<p>No explicit fitness gate details were available for this execution.</p>");
            }
            else
            {
                sb.AppendLine("""
<table class="link-table">
    <thead>
        <tr>
            <th>Gate</th>
            <th>Status</th>
            <th>Details</th>
        </tr>
    </thead>
    <tbody>
""");

                foreach (var row in metrics.GateRows)
                {
                    sb.AppendLine($"""
<tr>
    <td>{Html(row.Name)}</td>
    <td><span class="badge {row.BadgeCss}"><span class="badge-dot"></span> {Html(row.Status)}</span></td>
    <td>{Html(row.Details)}</td>
</tr>
""");
                }

                sb.AppendLine("""
    </tbody>
</table>
""");
            }

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Statistics & Effort</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Effort Estimation</h3>
    <ul class="clean">
        <li><b>Difficulty:</b> {Html(metrics.EffortDifficulty)}</li>
        <li><b>Estimated Hours:</b> {metrics.EffortHours:0.0}</li>
        <li><b>Confidence:</b> {metrics.EffortConfidence:0.00}</li>
        <li><b>RDI:</b> {metrics.EffortRdi:0.##}</li>
    </ul>

    <div class="chart-wrap" style="margin-top:16px;">
        <table class="charts-css">
            <caption>Effort Confidence Snapshot</caption>
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

            sb.AppendLine("""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Statistical Summary</h3>
""");

            if (metrics.StatisticsRows.Count == 0)
            {
                sb.AppendLine("<p>No structured statistics payload was available for this execution.</p>");
            }
            else
            {
                sb.AppendLine("""
<table class="link-table compact-table">
    <thead>
        <tr>
            <th>Metric</th>
            <th>Value</th>
        </tr>
    </thead>
    <tbody>
""");

                foreach (var row in metrics.StatisticsRows)
                {
                    sb.AppendLine($"""
<tr>
    <td>{Html(row.Name)}</td>
    <td>{Html(row.Value)}</td>
</tr>
""");
                }

                sb.AppendLine("""
    </tbody>
</table>
""");
            }

            sb.AppendLine("</div></div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Methodology & Notes</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
        <ul class="clean">
            <li>This dashboard summarizes quality signals across parsing, gating, effort and statistical support.</li>
            <li>Fitness gates express execution readiness, not scientific certainty.</li>
            <li>Effort confidence is useful as a planning aid and should not be treated as an absolute forecast.</li>
            <li>Statistics support reflects the availability and completeness of the current statistical payload.</li>
            <li>Final interpretation should always include human review, especially when unresolved structural signals remain.</li>
        </ul>
    </div>
</div>
""");

            sb.AppendLine("""
<style>
.grid-kpis.quality-kpis {
    display: grid;
    grid-template-columns: repeat(6, minmax(0, 1fr));
    gap: 16px;
}

.quality-half-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
    align-items: stretch;
}

.half-panel {
    min-width: 0;
    overflow: hidden;
}

.chart-panel {
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    min-height: 460px;
}

.chart-panel .chart-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    min-width: 0;
}

.dashboard-radar-svg,
.dashboard-sankey-svg,
.dashboard-donut-svg,
.dashboard-constellation-svg {
    display: block;
    max-width: 100%;
    height: auto;
    margin: 0 auto;
}

.chart-container {
    min-width: 0;
}

.chart-interpretation {
    color: #d7e4f1;
    font-size: 12px;
}

.chart-interpretation ul li {
    margin-bottom: 8px;
}

@media (max-width: 1400px) {
    .grid-kpis.quality-kpis {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }
}

@media (max-width: 1080px) {
    .quality-half-grid {
        grid-template-columns: 1fr;
    }

    .chart-panel {
        min-height: unset;
    }
}

@media (max-width: 980px) {
    .grid-kpis.quality-kpis {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}
</style>
""");

            sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
                "Generated by RefactorScope Quality / Gates Dashboard"));

            return sb.ToString();
        }

        private static QualityControlChartsRenderer.QualityGateStatus MapGateStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return QualityControlChartsRenderer.QualityGateStatus.Info;

            return status.Trim().ToLowerInvariant() switch
            {
                "pass" => QualityControlChartsRenderer.QualityGateStatus.Pass,
                "warn" => QualityControlChartsRenderer.QualityGateStatus.Warn,
                "warning" => QualityControlChartsRenderer.QualityGateStatus.Warn,
                "fail" => QualityControlChartsRenderer.QualityGateStatus.Fail,
                _ => QualityControlChartsRenderer.QualityGateStatus.Info
            };
        }

        private static double ComputeGateReadiness(dynamic metrics)
        {
            try
            {
                if (metrics.GateRows == null || metrics.GateRows.Count == 0)
                    return 0.75;

                var total = metrics.GateRows.Count;
                var pass = 0;
                var warn = 0;
                var fail = 0;

                foreach (var row in metrics.GateRows)
                {
                    var status = (row.Status?.ToString() ?? string.Empty).Trim().ToLowerInvariant();

                    switch (status)
                    {
                        case "pass":
                            pass++;
                            break;
                        case "warn":
                        case "warning":
                            warn++;
                            break;
                        case "fail":
                            fail++;
                            break;
                    }
                }

                var score =
                    ((pass * 1.0) +
                     (warn * 0.55) +
                     (fail * 0.10)) / Math.Max(1, total);

                return Clamp01(score);
            }
            catch
            {
                return 0.75;
            }
        }

        private static double ComputeUnresolvedPressure(dynamic metrics)
        {
            try
            {
                var unresolved = (double)metrics.Hub.Unresolved;
                var parsingTypes = (double)Math.Max(1, metrics.ParsingTypes);

                var pressure = unresolved / parsingTypes;

                return Clamp01(pressure * 4.0);
            }
            catch
            {
                return 0.0;
            }
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
    }
}
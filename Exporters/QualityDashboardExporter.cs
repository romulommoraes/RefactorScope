using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Dashboard de qualidade e gates da execução.
    ///
    /// Objetivo
    /// --------
    /// Consolidar sinais transversais de qualidade da execução,
    /// incluindo:
    ///
    /// - fitness gates
    /// - confiança do parsing
    /// - esforço estimado
    /// - suporte estatístico
    /// - leitura executiva sobre confiabilidade e prontidão
    ///
    /// Observação
    /// ----------
    /// Este exporter não calcula métricas centrais de domínio.
    /// Ele consome resultados já existentes e aplica apenas
    /// agregação executiva leve voltada à apresentação.
    /// </summary>
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
            var hubMetrics = DashboardMetricsCalculator.BuildHubMetrics(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences);

            var fitness = report.GetResult<FitnessGateResult>();
            var effortResult = report.GetResult<EffortEstimateResult>();
            var effort = effortResult?.Estimate;
            var statistics = report.GetResult<StatisticsResult>();

            var targetName = string.IsNullOrWhiteSpace(report.TargetScope)
                ? "Unknown Scope"
                : Path.GetFileName(report.TargetScope.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            var gateRows = ExtractFitnessGateRows(fitness);
            var statsRows = ExtractStatisticsRows(statistics);

            var fitStatus = fitness == null
                ? "Unknown"
                : fitness.HasFailure ? "Attention Required" : "Ready";

            var parserBand = DashboardMetricsCalculator.GetSupportBandFromScore(parserConfidence);
            var parserBandCss = DashboardMetricsCalculator.GetBandCssClass(parserConfidence);

            var effortConfidence = effort?.Confidence ?? 0;
            var effortBand = DashboardMetricsCalculator.GetSupportBandFromScore(effortConfidence);
            var effortBandCss = DashboardMetricsCalculator.GetBandCssClass(effortConfidence);

            var statsSupportScore = CalculateStatisticsSupportScore(statistics);
            var statsBand = DashboardMetricsCalculator.GetSupportBandFromScore(statsSupportScore);
            var statsBandCss = DashboardMetricsCalculator.GetBandCssClass(statsSupportScore);

            var overallReadinessScore = CalculateOverallReadinessScore(
                parserConfidence,
                effortConfidence,
                fitness == null ? 0.50 : (fitness.HasFailure ? 0.30 : 0.90),
                statsSupportScore);

            var overallReadinessBand = DashboardMetricsCalculator.GetSupportBandFromScore(overallReadinessScore);
            var overallReadinessCss = DashboardMetricsCalculator.GetBandCssClass(overallReadinessScore);

            var narrative = BuildQualityNarrative(
                fitStatus,
                parserBand,
                effortBand,
                statsBand,
                overallReadinessBand);

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
        <div><b>Parser:</b> {Html(parserName)}</div>
        <div><b>Readiness:</b> {Html(fitStatus)}</div>
        <div><b>Target Scope:</b> {Html(report.TargetScope)}</div>
    </div>
</div>
""");

            sb.AppendLine($"""
<div class="grid-kpis quality-kpis">
    <div class="kpi {(string.Equals(fitStatus, "Ready", StringComparison.OrdinalIgnoreCase) ? "good" : string.Equals(fitStatus, "Attention Required", StringComparison.OrdinalIgnoreCase) ? "alert" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Fitness Gates</div>
        <div class="value" style="font-size:24px;">{Html(fitStatus)}</div>
        <div class="hint">Execution readiness derived from fitness gate validation</div>
    </div>

    <div class="kpi {parserBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Parser Confidence</div>
        <div class="value">{parserConfidence:P0}</div>
        <div class="hint">Heuristic confidence that the parsed model is structurally usable</div>
    </div>

    <div class="kpi {effortBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Effort Confidence</div>
        <div class="value">{effortConfidence:0.00}</div>
        <div class="hint">Confidence level attached to the current refactor effort estimate</div>
    </div>

    <div class="kpi {statsBandCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Statistics Support</div>
        <div class="value" style="font-size:24px;">{Html(statsBand)}</div>
        <div class="hint">Availability and completeness of the statistical validation payload</div>
    </div>

    <div class="kpi {(hubMetrics.Unresolved > 0 ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Unresolved</div>
        <div class="value">{hubMetrics.Unresolved}</div>
        <div class="hint">Candidates whose dead code hypothesis could not be refuted</div>
    </div>

    <div class="kpi {overallReadinessCss}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Overall Readiness</div>
        <div class="value" style="font-size:24px;">{Html(overallReadinessBand)}</div>
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
    <p style="margin-top:0;">{Html(narrative)}</p>

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
        <div class="badge {BadgeCssForBand(parserConfidence)}"><span class="badge-dot"></span> Parser: {Html(parserBand)}</div>
        <div class="badge {BadgeCssForBand(effortConfidence)}"><span class="badge-dot"></span> Effort: {Html(effortBand)}</div>
        <div class="badge {BadgeCssForBand(statsSupportScore)}"><span class="badge-dot"></span> Statistics: {Html(statsBand)}</div>
        <div class="badge {BadgeCssForBand(overallReadinessScore)}"><span class="badge-dot"></span> Overall: {Html(overallReadinessBand)}</div>
    </div>

    <ul class="clean">
        <li><b>Execution Time:</b> {parsingExecution.TotalMilliseconds:0} ms</li>
        <li><b>Files:</b> {parsingFiles}</li>
        <li><b>Types:</b> {parsingTypes}</li>
        <li><b>References:</b> {parsingReferences}</li>
        <li><b>Estimated Hours:</b> {hubMetrics.EffortHours:0.0}</li>
        <li><b>RDI:</b> {hubMetrics.EffortRdi:0.##}</li>
    </ul>
</div>
""");

            sb.AppendLine("</div></div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Fitness Gates</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            if (gateRows.Count == 0)
            {
                sb.AppendLine("""
<p>No explicit fitness gate details were available for this execution.</p>
""");
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

                foreach (var row in gateRows)
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

            sb.AppendLine("""
    </div>
</div>
""");

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
        <li><b>Difficulty:</b> {Html(hubMetrics.EffortDifficulty)}</li>
        <li><b>Estimated Hours:</b> {hubMetrics.EffortHours:0.0}</li>
        <li><b>Confidence:</b> {hubMetrics.EffortConfidence:0.00}</li>
        <li><b>RDI:</b> {hubMetrics.EffortRdi:0.##}</li>
    </ul>

    <div class="chart-wrap" style="margin-top:16px;">
        <table class="charts-css">
            <caption>Effort Confidence Snapshot</caption>
            <tbody>
                <tr>
                    <th scope="row">Hours</th>
                    <td style="--size: {Clamp01(hubMetrics.EffortHours / 80.0).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{hubMetrics.EffortHours:0.0}h</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Confidence</th>
                    <td style="--size: {Clamp01(hubMetrics.EffortConfidence).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{hubMetrics.EffortConfidence:0.00}</span>
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

            if (statsRows.Count == 0)
            {
                sb.AppendLine("""
<p>No structured statistics payload was available for this execution.</p>
""");
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

                foreach (var row in statsRows)
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

            sb.AppendLine("""
</div>
""");

            sb.AppendLine("""
</div></div>
""");

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

@media (max-width: 1400px) {
    .grid-kpis.quality-kpis {
        grid-template-columns: repeat(3, minmax(0, 1fr));
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

        private static List<GateRow> ExtractFitnessGateRows(FitnessGateResult? fitness)
        {
            var rows = new List<GateRow>();

            if (fitness == null)
                return rows;

            rows.Add(new GateRow(
                "Overall Fitness",
                fitness.HasFailure ? "Failed" : "Passed",
                fitness.HasFailure
                    ? "At least one gate signaled attention or failure."
                    : "No failing fitness gate was detected.",
                fitness.HasFailure ? "red" : "green"));

            try
            {
                var type = fitness.GetType();
                var candidateCollections = new[]
                {
                    "Failures",
                    "Alerts",
                    "Warnings",
                    "Items",
                    "Gates"
                };

                foreach (var propertyName in candidateCollections)
                {
                    var prop = type.GetProperty(propertyName);
                    if (prop == null)
                        continue;

                    var value = prop.GetValue(fitness);
                    if (value is not System.Collections.IEnumerable enumerable)
                        continue;

                    foreach (var item in enumerable)
                    {
                        if (item == null)
                            continue;

                        var name = TryGetProperty(item, "Name")
                                   ?? TryGetProperty(item, "Gate")
                                   ?? TryGetProperty(item, "Rule")
                                   ?? item.GetType().Name;

                        var status = TryGetProperty(item, "Status")
                                     ?? TryGetProperty(item, "Level")
                                     ?? propertyName;

                        var details = TryGetProperty(item, "Message")
                                      ?? TryGetProperty(item, "Reason")
                                      ?? TryGetProperty(item, "Description")
                                      ?? "No additional detail available.";

                        rows.Add(new GateRow(
                            name,
                            status,
                            details,
                            ResolveBadgeFromStatus(status)));
                    }

                    if (rows.Count > 1)
                        break;
                }
            }
            catch
            {
                // fallback silencioso: fica só a linha geral
            }

            return rows;
        }

        private static List<NamedValueRow> ExtractStatisticsRows(StatisticsResult? statistics)
        {
            var rows = new List<NamedValueRow>();

            if (statistics?.Report == null)
                return rows;

            var report = statistics.Report;

            if (report.Confidence != null)
            {
                rows.Add(new NamedValueRow(
                    "Classes Per File",
                    report.Confidence.ClassesPerFile.ToString("0.###", CultureInfo.InvariantCulture)));

                rows.Add(new NamedValueRow(
                    "References Per Class",
                    report.Confidence.ReferencesPerClass.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            if (report.Summary != null)
            {
                rows.Add(new NamedValueRow(
                    "Mean Coupling",
                    report.Summary.MeanCoupling.ToString("0.###", CultureInfo.InvariantCulture)));

                rows.Add(new NamedValueRow(
                    "Unresolved Candidate Ratio",
                    report.Summary.UnresolvedCandidateRatio.ToString("0.###", CultureInfo.InvariantCulture)));

                rows.Add(new NamedValueRow(
                    "Namespace Drift Ratio",
                    report.Summary.NamespaceDriftRatio.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        private static double CalculateStatisticsSupportScore(StatisticsResult? statistics)
        {
            if (statistics?.Report == null)
                return 0.20;

            var report = statistics.Report;

            var hasConfidenceBlock = report.Confidence != null;
            var hasSummaryBlock = report.Summary != null;

            var score = 0.35;

            if (hasConfidenceBlock)
                score += 0.25;

            if (hasSummaryBlock)
                score += 0.25;

            if (hasConfidenceBlock)
            {
                if (report.Confidence.ClassesPerFile > 0)
                    score += 0.10;

                if (report.Confidence.ReferencesPerClass > 0)
                    score += 0.10;
            }

            return Math.Max(0, Math.Min(1, score));
        }

        private static double CalculateOverallReadinessScore(
            double parserConfidence,
            double effortConfidence,
            double gateScore,
            double statisticsSupport)
        {
            return Clamp01(
                (Clamp01(parserConfidence) * 0.35) +
                (Clamp01(effortConfidence) * 0.20) +
                (Clamp01(gateScore) * 0.30) +
                (Clamp01(statisticsSupport) * 0.15));
        }

        private static string BuildQualityNarrative(
            string fitStatus,
            string parserBand,
            string effortBand,
            string statisticsBand,
            string overallBand)
        {
            return $"The current execution is marked as {fitStatus.ToLowerInvariant()} overall. " +
                   $"Parsing confidence is {parserBand.ToLowerInvariant()}, effort support is {effortBand.ToLowerInvariant()}, " +
                   $"statistical support is {statisticsBand.ToLowerInvariant()}, and the overall quality snapshot is {overallBand.ToLowerInvariant()}.";
        }

        private static string BadgeCssForBand(double score)
        {
            if (score >= 0.80) return "green";
            if (score >= 0.55) return "amber";
            return "red";
        }

        private static string ResolveBadgeFromStatus(string? status)
        {
            var text = (status ?? string.Empty).ToLowerInvariant();

            if (text.Contains("pass") || text.Contains("ok") || text.Contains("ready") || text.Contains("success"))
                return "green";

            if (text.Contains("warn") || text.Contains("attention") || text.Contains("moderate"))
                return "amber";

            if (text.Contains("fail") || text.Contains("error") || text.Contains("critical"))
                return "red";

            return "amber";
        }

        private static string? TryGetProperty(object instance, string propertyName)
        {
            try
            {
                return instance.GetType().GetProperty(propertyName)?.GetValue(instance)?.ToString();
            }
            catch
            {
                return null;
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

        private readonly record struct GateRow(
            string Name,
            string Status,
            string Details,
            string BadgeCss);

        private readonly record struct NamedValueRow(
            string Name,
            string Value);
    }
}
using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters
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
            var hygiene = new ArchitecturalHygieneAnalyzer().Analyze(report);
            var structural = report.GetStructuralCandidateBreakdown();
            var coupling = report.GetResult<CouplingResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var solid = report.GetResult<SolidResult>();
            var fitness = report.GetResult<FitnessGateResult>();
            var effortResult = report.GetResult<EffortEstimateResult>();
            var effort = effortResult?.Estimate;

            var unresolved = structural.ProbabilisticConfirmed;
            var patternSimilarity = structural.PatternSimilarity;
            var structuralCandidates = structural.StructuralCandidates;
            var couplingSuspects = implicitCoupling?.Suspicions.Count ?? 0;
            var solidAlerts = solid?.Alerts.Count ?? 0;

            double avgAbstractness = 0;
            double avgInstability = 0;
            double avgDistance = 0;

            if (coupling != null && coupling.AbstractnessByModule.Any())
            {
                avgAbstractness = coupling.AbstractnessByModule.Values.Average();
                avgInstability = coupling.InstabilityByModule.Values.Average();
                avgDistance = coupling.DistanceByModule.Values.Average();
            }

            var targetName = string.IsNullOrWhiteSpace(report.TargetScope)
                ? "Unknown Scope"
                : Path.GetFileName(report.TargetScope.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            var fitStatus = fitness == null
                ? "Unknown"
                : fitness.HasFailure ? "Attention Required" : "Ready";

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
        <div><b>Parser:</b> {Html(parserName)}</div>
        <div><b>Target Scope:</b> {Html(report.TargetScope)}</div>
        <div><b>Readiness:</b> {Html(fitStatus)}</div>
    </div>
</div>
""");

            sb.AppendLine("""<div class="nav-grid">""");

            if (!string.IsNullOrWhiteSpace(structuralFileName))
            {
                sb.AppendLine(DashboardHtmlShell.RenderNavCard(
                    "Module // 01",
                    "Structural",
                    "Dead code candidates, namespace drift, coupling suspicion and removal analysis.",
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
                    "Health by module, score, Robert Martin metrics and architectural balance.",
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
                    "Parser confidence, structural density, throughput and parsed type coverage.",
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
                    "Fitness gates, statistics, warnings, crashes and execution quality signals.",
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
<div class="grid-kpis">
    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Total Classes</div>
        <div class="value">{hygiene.TotalClasses}</div>
        <div class="hint">Structural inventory</div>
    </div>

    <div class="kpi warning" augmented-ui="tr-clip bl-clip border">
        <div class="label">Structural Candidates</div>
        <div class="value">{structuralCandidates}</div>
        <div class="hint">Zero-reference candidates</div>
    </div>

    <div class="kpi good" augmented-ui="tr-clip bl-clip border">
        <div class="label">Pattern Similarity</div>
        <div class="value">{patternSimilarity}</div>
        <div class="hint">Pattern-compatible candidates</div>
    </div>

    <div class="kpi alert" augmented-ui="tr-clip bl-clip border">
        <div class="label">Unresolved</div>
        <div class="value">{unresolved}</div>
        <div class="hint">Manual inspection required</div>
    </div>

    <div class="kpi warning" augmented-ui="tr-clip bl-clip border">
        <div class="label">Namespace Drift</div>
        <div class="value">{hygiene.NamespaceDriftCount}</div>
        <div class="hint">Namespace ↔ folder mismatch</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Global Namespace</div>
        <div class="value">{hygiene.GlobalNamespaceCount}</div>
        <div class="hint">Legacy structural smell</div>
    </div>

    <div class="kpi good" augmented-ui="tr-clip bl-clip border">
        <div class="label">Isolated Core</div>
        <div class="value">{hygiene.IsolatedCoreCount}</div>
        <div class="hint">Protected boundaries</div>
    </div>

    <div class="kpi {(hygiene.SmellIndex > 40 ? "alert" : hygiene.SmellIndex > 20 ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Smell Index</div>
        <div class="value">{hygiene.SmellIndex:0.0}</div>
        <div class="hint">Composite hygiene indicator</div>
    </div>

    <div class="kpi {(string.Equals(hygiene.HygieneLevel, "Healthy", StringComparison.OrdinalIgnoreCase) ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Hygiene Level</div>
        <div class="value" style="font-size:24px;">{Html(hygiene.HygieneLevel)}</div>
        <div class="hint">Structural health classification</div>
    </div>

    <div class="kpi {(couplingSuspects > 0 ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Implicit Coupling</div>
        <div class="value">{couplingSuspects}</div>
        <div class="hint">Directional dependency hotspots</div>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Run Overview</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

            int parseMax = Math.Max(parsingFiles, Math.Max(parsingTypes, parsingReferences));
            parseMax = Math.Max(parseMax, 1);

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Pipeline Telemetry</h3>

    <div class="status-row" style="margin-bottom:14px;">
        <div class="badge green"><span class="badge-dot"></span> Parser: {Html(parserName)}</div>
        <div class="badge {(parserConfidence >= 0.85 ? "green" : parserConfidence >= 0.65 ? "amber" : "red")}"><span class="badge-dot"></span> Confidence: {parserConfidence:P0}</div>
        <div class="badge {(fitness == null ? "amber" : fitness.HasFailure ? "red" : "green")}"><span class="badge-dot"></span> Gates: {Html(fitStatus)}</div>
    </div>

    <div class="chart-wrap">
        <table class="charts-css">
            <caption>Parsing / Scope / Quality Signals</caption>
            <tbody>
                <tr>
                    <th scope="row">Files</th>
                    <td style="--size: {SafeRatio(parsingFiles, parseMax)};">
                        <span class="data">{parsingFiles}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Types</th>
                    <td style="--size: {SafeRatio(parsingTypes, parseMax)};">
                        <span class="data">{parsingTypes}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Refs</th>
                    <td style="--size: {SafeRatio(parsingReferences, parseMax)};">
                        <span class="data">{parsingReferences}</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Confidence</th>
                    <td style="--size: {Clamp01(parserConfidence).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{parserConfidence:P0}</span>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>

    <ul class="clean" style="margin-top:14px;">
        <li><b>Execution:</b> {parsingExecution.TotalMilliseconds:0} ms</li>
        <li><b>Files:</b> {parsingFiles} / <b>Types:</b> {parsingTypes} / <b>References:</b> {parsingReferences}</li>
        <li><b>Average Abstractness:</b> {avgAbstractness:0.00}</li>
        <li><b>Average Instability:</b> {avgInstability:0.00}</li>
        <li><b>Average Distance:</b> {avgDistance:0.00}</li>
    </ul>
</div>
""");

            var unresolvedMessage = unresolved > 0
                ? "Remaining structural candidates persist after refinement."
                : "No unresolved candidates remain after refinement.";

            var coreMessage = hygiene.IsolatedCoreCount > 0
                ? "Core isolation detected and architectural boundaries appear preserved."
                : "Core isolation not detected. Review possible leakage to outer layers.";

            var driftMessage = hygiene.NamespaceDriftCount > 0
                ? "Namespace drift detected. Folder hierarchy and namespace map need review."
                : "Namespace hierarchy aligned with the physical project structure.";

            var smellMessage = hygiene.SmellIndex <= 20
                ? "Smell index remains in a healthy range."
                : hygiene.SmellIndex <= 40
                    ? "Smell index indicates moderate architectural attention."
                    : "Smell index indicates elevated structural degradation risk.";

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Executive Analysis</h3>
    <ul class="clean">
        <li>{Html(coreMessage)}</li>
        <li>{Html(driftMessage)}</li>
        <li>{Html(unresolvedMessage)}</li>
        <li>{Html(smellMessage)}</li>
        <li>{(solidAlerts > 0 ? $"SOLID alerts detected: {solidAlerts}." : "No SOLID alerts detected.")}</li>
        <li>{(couplingSuspects > 0 ? $"Implicit coupling hotspots detected: {couplingSuspects}." : "No implicit coupling hotspots detected.")}</li>
    </ul>

    <div style="margin-top:16px;">
        <div class="badge {(unresolved > 0 ? "red" : "green")}"><span class="badge-dot"></span> Unresolved: {unresolved}</div>
        <div class="badge {(hygiene.NamespaceDriftCount > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Namespace Drift: {hygiene.NamespaceDriftCount}</div>
        <div class="badge {(solidAlerts > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> SOLID Alerts: {solidAlerts}</div>
        <div class="badge {(couplingSuspects > 0 ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Implicit Coupling: {couplingSuspects}</div>
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
    <td>Dead code probability / structural risk</td>
    <td>Structural Candidates, Pattern Similarity, Unresolved, Namespace Drift</td>
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
    <td>Confidence, Files, Types, References, Time/Class</td>
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
    <td>Fitness Gates, Statistics, Crash Reports, Warnings</td>
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
    <h3>Code Hygiene</h3>
    <ul class="clean">
        <li><b>CodeSmellIndex:</b> {hygiene.SmellIndex:0.0}</li>
        <li><b>Hygiene:</b> {Html(hygiene.HygieneLevel)}</li>
        <li><b>Dead Code Candidates:</b> {hygiene.UnreferencedCount}</li>
        <li><b>Namespace Drift:</b> {hygiene.NamespaceDriftCount}</li>
        <li><b>Global Namespace:</b> {hygiene.GlobalNamespaceCount}</li>
        <li><b>Isolated Core:</b> {hygiene.IsolatedCoreCount}</li>
    </ul>
</div>
""");

            var effortDifficulty = effort?.Difficulty ?? "Unknown";
            var effortHours = effort?.EstimatedHours ?? 0;
            var effortConfidence = effort?.Confidence ?? 0;
            var effortRdi = effort?.RDI ?? 0;

            sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Refactor Effort Estimation</h3>
    <ul class="clean">
        <li><b>RDI:</b> {effortRdi}</li>
        <li><b>Difficulty:</b> {Html(effortDifficulty)}</li>
        <li><b>Estimated Hours:</b> {effortHours:0.0}</li>
        <li><b>Confidence:</b> {effortConfidence:0.00}</li>
    </ul>

    <div class="chart-wrap" style="margin-top:16px;">
        <table class="charts-css">
            <caption>Estimated Refactor Load</caption>
            <tbody>
                <tr>
                    <th scope="row">Hours</th>
                    <td style="--size: {Clamp01(effortHours / 80.0).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{effortHours:0.0}h</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Confidence</th>
                    <td style="--size: {Clamp01(effortConfidence).ToString("0.####", CultureInfo.InvariantCulture)};">
                        <span class="data">{effortConfidence:0.00}</span>
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
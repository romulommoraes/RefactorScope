using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using RefactorScope.Estimation.Models;
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
            EffortEstimate? effort = null,
            string structuralFileName = "StructuralDashboard.html",
            string architecturalFileName = "Relatorio_Arquitetural.md",
            string parsingFileName = "ParsingDashboard.html",
            string qualityFileName = "QualityDashboard.html")
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
                effort,
                structuralFileName,
                architecturalFileName,
                parsingFileName,
                qualityFileName);

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
            EffortEstimate? effort,
            string structuralFileName,
            string architecturalFileName,
            string parsingFileName,
            string qualityFileName)
        {
            var hygiene = new ArchitecturalHygieneAnalyzer().Analyze(report);
            var structural = report.GetStructuralCandidateBreakdown();
            var coupling = report.GetResult<CouplingResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var solid = report.GetResult<SolidResult>();
            var fitness = report.GetResult<FitnessGateResult>();

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

            sb.AppendLine("""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>RefactorScope Hub</title>

<link rel="stylesheet" href="assets/vendor/charts.min.css" />
<link rel="stylesheet" href="assets/vendor/augmented-ui.min.css" />

<style>
:root
{
    --bg: #050816;
    --bg-2: #090d1f;
    --panel: rgba(10, 16, 34, 0.78);
    --panel-2: rgba(14, 22, 44, 0.92);

    --line: rgba(90, 170, 255, 0.20);
    --line-strong: rgba(255, 56, 106, 0.32);

    --cyan: #39d5ff;
    --cyan-soft: #7cecff;
    --blue: #2f88ff;
    --red: #ff3b6b;
    --red-soft: #ff6b8f;
    --amber: #ffc857;
    --green: #49f2b8;
    --text: #e8f3ff;
    --muted: #8ea9c7;
    --white-soft: #cfe8ff;

    --glow-cyan: 0 0 10px rgba(57, 213, 255, .35), 0 0 24px rgba(57, 213, 255, .12);
    --glow-red: 0 0 10px rgba(255, 59, 107, .30), 0 0 24px rgba(255, 59, 107, .10);
    --glow-panel: 0 0 0 1px rgba(57, 213, 255, .10), 0 0 24px rgba(47,136,255,.08);
}

* { box-sizing: border-box; }

html, body
{
    margin: 0;
    padding: 0;
    background:
        radial-gradient(circle at top left, rgba(47,136,255,.10), transparent 28%),
        radial-gradient(circle at top right, rgba(255,59,107,.08), transparent 24%),
        linear-gradient(180deg, #030510 0%, #050816 40%, #040612 100%);
    color: var(--text);
    font-family: "Segoe UI", "Inter", Arial, sans-serif;
}

body::before
{
    content: "";
    position: fixed;
    inset: 0;
    pointer-events: none;
    opacity: .18;
    background-image:
        linear-gradient(rgba(57,213,255,.05) 1px, transparent 1px),
        linear-gradient(90deg, rgba(57,213,255,.04) 1px, transparent 1px);
    background-size: 40px 40px, 40px 40px;
}

body::after
{
    content: "";
    position: fixed;
    inset: 0;
    pointer-events: none;
    opacity: .08;
    background: repeating-linear-gradient(
        180deg,
        rgba(255,255,255,0.05) 0px,
        rgba(255,255,255,0.05) 1px,
        transparent 2px,
        transparent 4px
    );
}

.wrapper
{
    width: min(1500px, 94vw);
    margin: 0 auto;
    padding: 28px 0 60px;
}

.topbar
{
    position: relative;
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 20px;
    margin-bottom: 22px;
    padding: 18px 22px;
    background: linear-gradient(180deg, rgba(12,20,40,.88), rgba(8,13,28,.82));
    border: 1px solid rgba(57,213,255,.18);
    box-shadow: var(--glow-panel);
    overflow: hidden;
}

.topbar::before,
.topbar::after
{
    content: "";
    position: absolute;
    height: 2px;
    left: 0;
    right: 0;
}

.topbar::before
{
    top: 0;
    background: linear-gradient(90deg, transparent, var(--cyan), transparent 45%, var(--red), transparent);
}

.topbar::after
{
    bottom: 0;
    background: linear-gradient(90deg, transparent, var(--red), transparent 55%, var(--cyan), transparent);
}

.brand
{
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.brand-kicker
{
    color: var(--cyan);
    font-size: 12px;
    letter-spacing: .24em;
    text-transform: uppercase;
    text-shadow: var(--glow-cyan);
}

.brand h1
{
    margin: 0;
    font-size: 42px;
    line-height: 1;
    letter-spacing: .04em;
    text-transform: uppercase;
    color: var(--white-soft);
}

.brand .subtitle
{
    color: var(--muted);
    font-size: 14px;
}

.run-meta
{
    display: grid;
    gap: 8px;
    min-width: 280px;
    color: var(--muted);
    font-size: 13px;
    text-align: right;
}

.run-meta b
{
    color: var(--text);
    font-weight: 600;
}

.nav-grid
{
    display: grid;
    grid-template-columns: repeat(4, minmax(180px, 1fr));
    gap: 14px;
    margin-bottom: 24px;
}

.nav-card
{
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 116px;
    padding: 16px 18px;
    color: var(--text);
    text-decoration: none;
    background: linear-gradient(180deg, rgba(11,18,37,.95), rgba(7,12,26,.90));
    border: 1px solid rgba(57,213,255,.16);
    box-shadow: var(--glow-panel);
    transition: transform .18s ease, border-color .18s ease, box-shadow .18s ease;
    overflow: hidden;
}

.nav-card:hover
{
    transform: translateY(-2px);
    border-color: rgba(255,59,107,.36);
    box-shadow: var(--glow-red);
}

.nav-card .tag
{
    color: var(--cyan);
    text-transform: uppercase;
    letter-spacing: .18em;
    font-size: 11px;
}

.nav-card .title
{
    font-size: 22px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: .04em;
}

.nav-card .desc
{
    color: var(--muted);
    font-size: 13px;
    line-height: 1.4;
}

.nav-card .accent
{
    position: absolute;
    right: 14px;
    top: 12px;
    width: 48px;
    height: 48px;
    border-radius: 50%;
    border: 2px solid rgba(57,213,255,.32);
    box-shadow: inset 0 0 18px rgba(57,213,255,.08), 0 0 12px rgba(57,213,255,.12);
}

.grid-kpis
{
    display: grid;
    grid-template-columns: repeat(5, minmax(180px, 1fr));
    gap: 14px;
    margin-bottom: 28px;
}

.kpi
{
    position: relative;
    padding: 18px;
    background: linear-gradient(180deg, rgba(11,18,37,.92), rgba(8,13,28,.90));
    border: 1px solid rgba(57,213,255,.14);
    box-shadow: var(--glow-panel);
    min-height: 108px;
    overflow: hidden;
}

.kpi::before
{
    content: "";
    position: absolute;
    inset: auto 0 0 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--cyan), transparent 70%);
    opacity: .8;
}

.kpi.alert::before { background: linear-gradient(90deg, transparent, var(--red), transparent 70%); }
.kpi.warning::before { background: linear-gradient(90deg, transparent, var(--amber), transparent 70%); }
.kpi.good::before { background: linear-gradient(90deg, transparent, var(--green), transparent 70%); }

.kpi .label
{
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: .14em;
    color: var(--muted);
    margin-bottom: 10px;
}

.kpi .value
{
    font-size: 32px;
    font-weight: 800;
    line-height: 1;
    letter-spacing: .03em;
    color: var(--white-soft);
}

.kpi .hint
{
    margin-top: 10px;
    color: var(--muted);
    font-size: 12px;
}

.section
{
    margin-top: 24px;
}

.section-title
{
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 14px;
    margin-bottom: 12px;
}

.section-title h2
{
    margin: 0;
    font-size: 24px;
    letter-spacing: .06em;
    text-transform: uppercase;
    color: var(--white-soft);
}

.section-title .line
{
    flex: 1;
    height: 1px;
    background: linear-gradient(90deg, rgba(57,213,255,.45), rgba(255,59,107,.18), transparent);
}

.dual
{
    display: grid;
    grid-template-columns: 1.2fr .8fr;
    gap: 16px;
}

.panel
{
    position: relative;
    padding: 18px;
    background: linear-gradient(180deg, rgba(11,18,37,.92), rgba(8,13,28,.90));
    border: 1px solid rgba(57,213,255,.16);
    box-shadow: var(--glow-panel);
    overflow: hidden;
}

.panel::before
{
    content: "";
    position: absolute;
    top: 0;
    left: 12px;
    width: 120px;
    height: 2px;
    background: linear-gradient(90deg, var(--cyan), transparent);
}

.panel::after
{
    content: "";
    position: absolute;
    bottom: 0;
    right: 12px;
    width: 120px;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--red));
}

.panel h3
{
    margin: 0 0 12px;
    font-size: 16px;
    text-transform: uppercase;
    letter-spacing: .12em;
    color: var(--cyan-soft);
}

.panel p,
.panel li
{
    color: var(--muted);
    line-height: 1.5;
}

ul.clean
{
    margin: 0;
    padding-left: 18px;
}

.status-row
{
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
}

.badge
{
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 7px 10px;
    border: 1px solid rgba(57,213,255,.18);
    background: rgba(12, 18, 36, .8);
    color: var(--white-soft);
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: .08em;
}

.badge-dot
{
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background: var(--cyan);
    box-shadow: 0 0 12px rgba(57,213,255,.45);
}

.badge.red .badge-dot { background: var(--red); box-shadow: 0 0 12px rgba(255,59,107,.45); }
.badge.green .badge-dot { background: var(--green); box-shadow: 0 0 12px rgba(73,242,184,.45); }
.badge.amber .badge-dot { background: var(--amber); box-shadow: 0 0 12px rgba(255,200,87,.45); }

.chart-wrap
{
    margin-top: 8px;
}

.charts-css
{
    width: 100%;
    color: var(--white-soft);
}

.charts-css caption
{
    caption-side: top;
    text-align: left;
    color: var(--muted);
    margin-bottom: 10px;
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: .12em;
}

.charts-css tbody tr td,
.charts-css tbody tr th
{
    padding-bottom: 8px;
}

.charts-css tbody tr td
{
    --color: linear-gradient(180deg, rgba(57,213,255,.95), rgba(47,136,255,.55));
}

.charts-css tbody tr:nth-child(2) td
{
    --color: linear-gradient(180deg, rgba(255,200,87,.95), rgba(255,59,107,.55));
}

.charts-css tbody tr:nth-child(3) td
{
    --color: linear-gradient(180deg, rgba(73,242,184,.95), rgba(57,213,255,.45));
}

table.link-table
{
    width: 100%;
    border-collapse: collapse;
    margin-top: 8px;
}

table.link-table th,
table.link-table td
{
    padding: 12px 10px;
    border-bottom: 1px solid rgba(57,213,255,.10);
    text-align: left;
}

table.link-table th
{
    color: var(--cyan-soft);
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: .12em;
}

table.link-table td
{
    color: var(--text);
}

table.link-table a
{
    color: var(--cyan);
    text-decoration: none;
}

table.link-table a:hover
{
    color: var(--red-soft);
}

.footer
{
    margin-top: 28px;
    color: var(--muted);
    font-size: 12px;
    text-align: center;
}

@media (max-width: 1200px)
{
    .grid-kpis { grid-template-columns: repeat(3, minmax(180px, 1fr)); }
    .nav-grid { grid-template-columns: repeat(2, minmax(180px, 1fr)); }
    .dual { grid-template-columns: 1fr; }
}

@media (max-width: 720px)
{
    .topbar { flex-direction: column; }
    .run-meta { text-align: left; }
    .grid-kpis { grid-template-columns: repeat(2, minmax(150px, 1fr)); }
    .status-row { grid-template-columns: 1fr; }
}

@media (max-width: 520px)
{
    .grid-kpis,
    .nav-grid
    {
        grid-template-columns: 1fr;
    }

    .brand h1 { font-size: 30px; }
}
</style>
</head>
<body>
""");

            sb.AppendLine("<div class='wrapper'>");

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

            sb.AppendLine($"""
<div class="nav-grid">
    <a class="nav-card" augmented-ui="tr-clip bl-clip border" href="{Html(structuralFileName)}">
        <div class="tag">Module // 01</div>
        <div class="title">Structural</div>
        <div class="desc">Dead code candidates, namespace drift, coupling suspicion and removal analysis.</div>
        <div class="accent"></div>
    </a>

    <a class="nav-card" augmented-ui="tr-clip bl-clip border" href="{Html(architecturalFileName)}">
        <div class="tag">Module // 02</div>
        <div class="title">Architectural</div>
        <div class="desc">Health by module, score, Robert Martin metrics and architectural balance.</div>
        <div class="accent"></div>
    </a>

    <a class="nav-card" augmented-ui="tr-clip bl-clip border" href="{Html(parsingFileName)}">
        <div class="tag">Module // 03</div>
        <div class="title">Parsing</div>
        <div class="desc">Parser confidence, structural density, throughput and parsed type coverage.</div>
        <div class="accent"></div>
    </a>

    <a class="nav-card" augmented-ui="tr-clip bl-clip border" href="{Html(qualityFileName)}">
        <div class="tag">Module // 04</div>
        <div class="title">Quality / Gates</div>
        <div class="desc">Fitness gates, statistics, warnings, crashes and execution quality signals.</div>
        <div class="accent"></div>
    </a>
</div>
""");

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

            sb.AppendLine($"""
<tr>
    <td>Structural</td>
    <td>Dead code probability / structural risk</td>
    <td>Structural Candidates, Pattern Similarity, Unresolved, Namespace Drift</td>
    <td><a href="{Html(structuralFileName)}">Open Structural Dashboard</a></td>
</tr>
<tr>
    <td>Architectural</td>
    <td>Health by module / A-I-D balance</td>
    <td>Score, Coupling, Isolation, Abstractness, Instability, Distance</td>
    <td><a href="{Html(architecturalFileName)}">Open Architectural Report</a></td>
</tr>
<tr>
    <td>Parsing</td>
    <td>Parser quality / structural extraction</td>
    <td>Confidence, Files, Types, References, Time/Class</td>
    <td><a href="{Html(parsingFileName)}">Open Parsing Dashboard</a></td>
</tr>
<tr>
    <td>Quality / Gates</td>
    <td>Validation / execution quality</td>
    <td>Fitness Gates, Statistics, Crash Reports, Warnings</td>
    <td><a href="{Html(qualityFileName)}">Open Quality Dashboard</a></td>
</tr>
""");

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

            sb.AppendLine("""
</div>
</div>

<div class="footer">
    Generated by RefactorScope Hub // Cyberpunk Publication Layout
</div>

</div>
</body>
</html>
""");

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
using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters
{
    public sealed class StructuralInventoryExporter
    {
        public void Export(ConsolidatedReport report, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var htmlPath = Path.Combine(outputDirectory, "StructuralDashboard.html");
            ExportHtml(report, htmlPath);
        }

        private void ExportHtml(ConsolidatedReport report, string path)
        {
            var classification = report.Results
                .OfType<ArchitecturalClassificationResult>()
                .FirstOrDefault();

            var solid = report.Results
                .OfType<SolidResult>()
                .FirstOrDefault();

            if (classification == null)
                return;

            var items = classification.Items;

            var hygieneAnalyzer = new ArchitecturalHygieneAnalyzer();
            var hygiene = hygieneAnalyzer.Analyze(report);

            // 🔹 Fonte canônica da análise estrutural
            var breakdown = report.GetStructuralCandidateBreakdown();

            var candidatesCount = breakdown.StructuralCandidates;
            var confirmedCount = breakdown.ProbabilisticConfirmed;
            var suspiciousCount = breakdown.Suspicious;
            var absolvedCount = breakdown.PatternSimilarity;

            var confirmed = report.GetEffectiveUnresolvedCandidates();
            var suspicious = report.GetPatternSimilarityCandidates();

            var sb = new StringBuilder();

            sb.AppendLine("<html><head><meta charset='UTF-8'>");

            #region STYLE

            sb.AppendLine("<style>");
            sb.AppendLine("body { background:#0f1117; color:#e6edf3; font-family:Segoe UI; margin:30px; }");
            sb.AppendLine(".card { display:inline-block; padding:20px; margin:10px; background:#161b22; border-radius:10px; min-width:220px; vertical-align:top; }");
            sb.AppendLine(".danger { color:#ff6b6b; }");
            sb.AppendLine(".warning { color:#ffd166; }");
            sb.AppendLine(".ok { color:#06d6a0; }");
            sb.AppendLine("table { border-collapse: collapse; width:100%; margin-top:20px; }");
            sb.AppendLine("th, td { border:1px solid #30363d; padding:8px; }");
            sb.AppendLine("th { cursor:pointer; background:#21262d; }");

            sb.AppendLine("tr.danger { background:#2d1117; color:#ff6b6b; }");
            sb.AppendLine("tr.warning { background:#332701; color:#ffd166; }");
            sb.AppendLine("tr.core { background:#0d1b2a; }");
            sb.AppendLine("tr.ok { background:#0f1f14; color:#7ee787; }");

            sb.AppendLine(@"
.tooltip { position: relative; cursor: help; }
.tooltip .tooltiptext {
  visibility: hidden;
  width: 320px;
  background-color: #1f2937;
  color: #fff;
  text-align: left;
  padding: 14px;
  border-radius: 6px;
  position: absolute;
  z-index: 1;
  top: 125%;
  left: 50%;
  margin-left: -160px;
  opacity: 0;
  transition: opacity 0.3s;
  font-size: 12px;
}
.tooltip:hover .tooltiptext {
  visibility: visible;
  opacity: 1;
}");
            sb.AppendLine("</style>");

            #endregion

            #region SCRIPT

            sb.AppendLine("<script>");
            sb.AppendLine(@"
function searchTable() {
  var input = document.getElementById('search');
  var filter = input.value.toLowerCase();
  var rows = document.getElementById('table').rows;
  for (var i = 1; i < rows.length; i++) {
    rows[i].style.display = rows[i].innerText.toLowerCase().includes(filter) ? '' : 'none';
  }
}
function sortTable(n) {
  var table = document.getElementById('table');
  var switching = true;
  var dir = 'asc';
  while (switching) {
    switching = false;
    var rows = table.rows;
    for (var i = 1; i < rows.length - 1; i++) {
      var x = rows[i].getElementsByTagName('TD')[n];
      var y = rows[i + 1].getElementsByTagName('TD')[n];
      var shouldSwitch = false;
      if (dir == 'asc' && x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase()) shouldSwitch = true;
      if (dir == 'desc' && x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase()) shouldSwitch = true;
      if (shouldSwitch) {
        rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
        switching = true;
      }
    }
    if (!switching && dir == 'asc') { dir = 'desc'; switching = true; }
}");
            sb.AppendLine("</script>");

            #endregion

            sb.AppendLine("</head><body>");

            sb.AppendLine("<h1>🧬 Structural Dashboard</h1>");
            sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");

            #region KPI CARDS

            sb.AppendLine("<div>");
            sb.AppendLine($"<div class='card'>Total Classes<br><b>{hygiene.TotalClasses}</b></div>");

            sb.AppendLine($@"<div class='card tooltip warning'>
                Structural Candidates<br>
                <b>{candidatesCount}</b>
                <span class='tooltiptext'>
                    Types with zero structural references inside the analyzed scope.
                    This does NOT indicate dead code.
                    It only means the type is not directly referenced in the parsed graph.
                    Common cases:
                    - CLI commands
                    - plugin rules
                    - reflection usage
                    - framework entry points
                </span></div>");

            sb.AppendLine($@"<div class='card tooltip ok'>
            Pattern Similarity<br>
            <b>{absolvedCount}</b>
            <span class='tooltiptext'>
            Classes that exhibit structural similarity
            to known design patterns.
            </span></div>");

            sb.AppendLine($@"<div class='card tooltip danger'>
            Unresolved<br>
            <b>{confirmedCount}</b>
            <span class='tooltiptext'>
                References that could not be resolved by the structural parser.

                May indicate:
                - missing dependencies
                - incomplete parsing
                - namespace mismatch
                - broken architecture links
            </span></div>");

            sb.AppendLine($"<div class='card tooltip warning'>Namespace Drift<br>" +
                $"<b>{hygiene.NamespaceDriftCount}</b>" +
                $"<span class='tooltiptext'>Namespace Drift (Strict Rule): Exact correspondence to folder structure</span>" +
                $"</div>");

            sb.AppendLine($@"<div class='card tooltip'>
                Global Namespace<br>
                <b>{hygiene.GlobalNamespaceCount}</b>
                <span class='tooltiptext'>
                Number of classes declared without a namespace.

                Using the global namespace reduces architectural clarity
                and weakens modular organization.

                Well-structured projects usually avoid global namespace usage.
                </span></div>");
            sb.AppendLine($"<div class='card tooltip'>Isolated Core<br><b>{hygiene.IsolatedCoreCount}</b> <span class='tooltiptext'>Number of Core types that remain structurally independent from the rest of the system. These elements represent stable architectural foundations. If the value is low, the Core may be leaking responsibilities or depending on outer layers.</span></div>");

            sb.AppendLine($@"<div class='card tooltip'>
            Smell Index<br>
            <b>{hygiene.SmellIndex:0.0}</b>
            <span class='tooltiptext'>
            Composite structural smell indicator.

            Components:
            • Dead Code Ratio (Unreferenced classes)
            • Namespace Drift Ratio (namespace vs folder misalignment)
            • Global Namespace Ratio (legacy structural usage)
            • Core Isolation Ratio (core architectural boundary integrity)
            • Structural Entropy (distribution disorder across modules)

            Higher values indicate architectural degradation
            and increased maintenance risk.
            </span>
            </div>");

            sb.AppendLine($@"<div class='card tooltip'>
            Hygiene Level<br>
            <b>{hygiene.HygieneLevel}</b>
            <span class='tooltiptext'>
            Overall structural health classification derived
            from the SmellIndex score.

            Evaluates architectural hygiene considering
            dead code probability, legacy structure,
            core isolation and structural entropy.
            </span>
            </div>");

            if (solid != null)
            {
                sb.AppendLine($@"<div class='card tooltip'>
            SOLID Alerts<br>
            <b>{solid.Alerts.Count}</b>
            <span class='tooltiptext'>Two-pass heuristic detection, SOLID Alerts
            Number of detected violations of SOLID design principles.
            Low values indicate stable object-oriented design.
            High values indicate architectural erosion or responsibility leaks.
            Examples:
            • SRP violations
            • LSP violations
            • interface misuse</span>
            </div>");
            }

            // ------------------------------------------------------
            // Coupling Suspicion KPI
            // ------------------------------------------------------

            var implicitCoupling = report.Results
                .OfType<ImplicitCouplingResult>()
                .FirstOrDefault();

            var couplingSuspects = implicitCoupling?.Suspicions.Count ?? 0;

            sb.AppendLine($@"<div class='card tooltip warning'>
            Implicit Coupling<br>
            <b>{couplingSuspects}</b>
            <span class='tooltiptext'>
            Classes whose dependencies concentrate strongly toward a single module.

            Detection considers:
            • Fan-out asymmetry
            • Dominant dependency direction
            • Structural concentration of references

            These signals highlight potential architectural coupling hotspots,
            but do not necessarily indicate design violations.
            </span></div>");

            // ------------------------------------------------------
            // Robert Martin Metrics KPI
            // ------------------------------------------------------

            var coupling = report.Results
                .OfType<CouplingResult>()
                .FirstOrDefault();

            double avgAbstractness = 0;
            double avgInstability = 0;
            double avgDistance = 0;

            if (coupling != null && coupling.AbstractnessByModule.Any())
            {
                avgAbstractness = coupling.AbstractnessByModule.Values.Average();
                avgInstability = coupling.InstabilityByModule.Values.Average();
                avgDistance = coupling.DistanceByModule.Values.Average();
            }

            sb.AppendLine($@"<div class='card tooltip'>
            Abstractness (A)<br>
            <b>{avgAbstractness:0.00}</b>
            <span class='tooltiptext'>
            Architectural abstraction ratio.

            A = Na / Nc

            Where:
            Na = number of abstract types
            Nc = total types in module

            Higher values indicate greater use of abstraction.
            </span></div>");

                        sb.AppendLine($@"<div class='card tooltip'>
            Instability (I)<br>
            <b>{avgInstability:0.00}</b>
            <span class='tooltiptext'>
            Architectural instability.

            I = Ce / (Ce + Ca)

            Where:
            Ce = outgoing dependencies
            Ca = incoming dependencies

            Values closer to 1 indicate modules
            that depend heavily on other modules.
            </span></div>");

                        sb.AppendLine($@"<div class='card tooltip'>
            Distance (D)<br>
            <b>{avgDistance:0.00}</b>
            <span class='tooltiptext'>
            Distance from Main Sequence.

            D = | A + I − 1 |

            Measures how far a module is from the
            ideal architectural balance between
            abstraction and stability.
            </span></div>");

            sb.AppendLine("</div>");

            #endregion

            #region RADAR

            sb.AppendLine("<div style='margin-top:40px;'>");
              sb.AppendLine(RenderCharts(
               items,
               breakdown,
               hygiene,
               suspicious.ToHashSet(),
               confirmed.ToHashSet(),
               solid,
               implicitCoupling, coupling));
            sb.AppendLine("</div>");

            #endregion

            #region TABLE

            sb.AppendLine("<table id='table'>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th onclick='sortTable(0)'>Classe</th>");
            sb.AppendLine("<th onclick='sortTable(1)'>Arquivo</th>");
            sb.AppendLine("<th onclick='sortTable(2)'>Namespace</th>");
            sb.AppendLine("<th onclick='sortTable(3)'>Usos</th>");
            sb.AppendLine("<th onclick='sortTable(4)'>Status</th>");
            sb.AppendLine("<th onclick='sortTable(5)'>Remoção</th>");
            sb.AppendLine("</tr>");

            int GetPriority(dynamic item)
            {
                var typeName = (string)item.TypeName;

                if (confirmed.Contains(typeName))
                    return 0;

                if (suspicious.Contains(typeName))
                    return 1;

                if (item.Layer == "Core")
                    return 2;

                return 3;
            }

            var orderedItems = items
                .OrderBy(i => GetPriority(i))
                .ThenBy(i => i.TypeName)
                .ToList();

            foreach (var item in orderedItems)
            {
                string rowClass = "ok";

                if (confirmed.Contains(item.TypeName))
                    rowClass = "danger";
                else if (suspicious.Contains(item.TypeName))
                    rowClass = "warning";
                else if (item.Layer == "Core")
                    rowClass = "core";

                sb.AppendLine($"<tr class='{rowClass}'>");
                sb.AppendLine($"<td>{item.TypeName}</td>");
                var fileName = string.IsNullOrWhiteSpace(item.DeclaredInFile)
                    ? "<i>unknown</i>"
                    : item.DeclaredInFile;
                sb.AppendLine($"<td>{fileName}</td>");
                sb.AppendLine($"<td>{item.Namespace}</td>");
                sb.AppendLine($"<td>{item.UsageCount}</td>");
                sb.AppendLine($"<td>{item.Status}</td>");
                sb.AppendLine($"<td>{item.RemovalCandidate}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            #endregion

            #region IMPLICIT COUPLING TABLE

            if (implicitCoupling != null && implicitCoupling.Suspicions.Any())
            {
                sb.AppendLine("<h2 style='margin-top:50px;'>⚠ Implicit Coupling Candidates</h2>");

                sb.AppendLine("<table>");
                sb.AppendLine("<tr>");
                sb.AppendLine("<th>Type</th>");
                sb.AppendLine("<th>Module</th>");
                sb.AppendLine("<th>Target Module</th>");
                sb.AppendLine("<th>Fan-Out</th>");
                sb.AppendLine("<th>Fan-In</th>");
                sb.AppendLine("<th>Dominance</th>");
                sb.AppendLine("<th>Volume</th>");
                sb.AppendLine("</tr>");

                foreach (var s in implicitCoupling.Suspicions)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{s.TypeName}</td>");
                    sb.AppendLine($"<td>{s.Module}</td>");
                    sb.AppendLine($"<td>{s.TargetModule}</td>");
                    sb.AppendLine($"<td>{s.FanOut}</td>");
                    sb.AppendLine($"<td>{s.FanIn}</td>");
                    sb.AppendLine($"<td>{s.Dominance:0.00}</td>");
                    sb.AppendLine($"<td>{s.Volume}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</table>");
                    sb.AppendLine(@"
                    <div style='margin-top:8px;font-size:12px;color:#9aa4b2;max-width:800px;'>

                    <b>Metric Definitions</b><br>

                    <b>Fan-Out</b> — Number of outgoing dependencies from the module.<br>

                    <b>Fan-In</b> — Number of incoming dependencies targeting the module.<br>

                    <b>Dominance</b> — Relative structural influence derived from Fan-Out versus Fan-In. 
                    Higher values indicate modules exerting stronger control over other modules.<br>

                    <b>Volume</b> — Total dependency interaction involving the module 
                    (Fan-In + Fan-Out), representing the structural traffic of the component.

                    </div>");

            }

            #endregion

            #region SOLID TABLE

            if (solid != null && solid.Alerts.Any())
            {
                sb.AppendLine("<h2 style='margin-top:50px;'>⚠ SOLID Alerts</h2>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Principle</th><th>Class</th><th>Namespace</th><th>Reason</th></tr>");
                foreach (var alert in solid.Alerts)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{alert.Principle}</td>");
                    sb.AppendLine($"<td>{alert.ClassName}</td>");
                    sb.AppendLine($"<td>{alert.Namespace}</td>");
                    sb.AppendLine($"<td>{alert.Reason}</td>");
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
            }

            #endregion

            #region DISCLAIMER

            sb.AppendLine(@"
                <div style='margin-top:60px; padding:20px; background:#111827; border-radius:10px; font-size:13px; color:#9ca3af;'>
                <h3>📘 Methodology & Limitations</h3>
                This dashboard is based on heuristic static structural analysis of the analyzed codebase.
                Metrics highlight architectural signals and potential anomalies, not definitive violations.

                Structural Candidates
                Classes with zero structural references in the analyzed graph.
                These are not automatically dead code.
                Typical causes include CLI entry points, plugin rules, reflection usage, or framework wiring.

                Pattern Similarity
                Subset of candidates that resemble known structural patterns (interfaces, DI adapters, factories, etc.).
                Higher values usually indicate intentional architectural structures rather than dead code.

                Unresolved
                Candidates that remain unexplained after probabilistic refinement.
                These are the strongest signals of potential dead code or incomplete integration.

                Namespace Drift
                Classes whose namespace does not match the folder hierarchy under the project root.
                Strict rule: namespace should reflect the physical module structure.

                Global Namespace
                Classes declared without a namespace.
                These reduce modular clarity and should normally be avoided.

                Isolated Core
                Core-layer classes that remain structurally independent from outer layers.
                Healthy architectures usually keep Core stable and referenced by other modules,
                while avoiding dependencies from Core to infrastructure.

                SOLID Alerts
                Heuristic detection of potential violations of object-oriented design principles.
                Examples include Single Responsibility violations, Liskov Substitution conflicts,
                and interface misuse.

                Radar Interpretation

                Radar values are normalized between 0 and 1 relative to the total number of classes.

                Typical interpretation:

                0.00 – 0.10  Healthy signal  
                0.10 – 0.25  Moderate architectural signal  
                0.25 – 0.50  Structural attention recommended  
                0.50 – 1.00  High architectural risk indicator

                Important:
                High values do not necessarily indicate errors, but highlight areas that deserve architectural review.

                This analysis operates purely at the static structural level.
                Runtime mechanisms such as reflection, dependency injection, or framework behavior may produce false positives.
                Final architectural decisions should always involve human review.

                    <br><br>
                    <b>Implicit Coupling</b><br>
                    Implicit coupling highlights classes whose dependencies concentrate toward a single module or subsystem.
                    This signal may reveal architectural hotspots such as orchestration layers,
                    integration adapters, or hidden dependency concentration.

                    These detections are heuristic and should be interpreted as architectural signals rather than definitive issues.

                    <br><br>
                    <b>Architectural Stability Metrics (Robert Martin)</b><br>

                    Abstractness (A) measures how much of a module consists of abstractions.

                    Instability (I) measures how dependent a module is on other modules.

                    Distance from Main Sequence (D) measures how far a module is from the ideal balance
                    between abstraction and stability.

                    Architectures close to the main sequence tend to be easier to evolve and maintain.

                    These metrics are widely used in architectural analysis tools such as NDepend.
                </div>");



            #endregion

            sb.AppendLine("</body></html>");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private string RenderCharts(
            IReadOnlyList<ArchitecturalClassificationItem> items,
            StructuralCandidateAnalysisBreakdown breakdown,
            HygieneReport hygiene,
            HashSet<string> suspicious,
            HashSet<string> confirmed,
            SolidResult? solid,
            ImplicitCouplingResult? implicitCoupling,
            CouplingResult? coupling)
        {
            var sb = new StringBuilder();
            var render = new ChartsRenderer();

            var coreDiagnosis =
                hygiene.IsolatedCoreCount == 0
                    ? "Core layer may be coupled with infrastructure or outer layers."
                    : hygiene.IsolatedCoreCount < 3
                        ? "Core partially isolated but with limited structural protection."
                        : "Core isolation detected. Architectural boundaries appear preserved.";

            var namespaceDiagnosis =
                hygiene.NamespaceDriftCount > hygiene.TotalClasses * 0.15
                    ? "High namespace drift detected. Folder structure may not reflect architectural boundaries."
                    : hygiene.NamespaceDriftCount > hygiene.TotalClasses * 0.05
                        ? "Moderate namespace drift detected. Minor structural inconsistencies present."
                        : "Namespace hierarchy aligned with project structure.";

            var globalNamespaceDiagnosis =
                hygiene.GlobalNamespaceCount > 0
                    ? "Global namespace usage detected. This may indicate legacy code or architectural leakage."
                    : "No global namespace usage detected.";

            var unresolvedDiagnosis =
                breakdown.ProbabilisticConfirmed > 0
                    ? "Potential dead or orphaned code detected through structural analysis."
                    : "No unresolved structural candidates detected.";

            var solidDiagnosis =
                (solid?.Alerts.Count ?? 0) > 5
                    ? "Multiple SOLID violations detected. Design refactoring recommended."
                    : (solid?.Alerts.Count ?? 0) > 0
                        ? "Minor SOLID design alerts detected."
                        : "No SOLID design alerts detected.";

            double couplingRating =
                hygiene.TotalClasses == 0
                    ? 0
                    : implicitCoupling?.Suspicions.Count ?? 0 / (double)hygiene.TotalClasses;
            var implicitCouplingDiagnosis =
    couplingRating switch
    {
        <= 0.02 =>
            "Implicit coupling is minimal. Modules appear structurally independent.",

        <= 0.05 =>
            "Minor implicit coupling patterns detected. Monitor for architectural drift.",

        <= 0.10 =>
            "Implicit coupling emerging between modules. Architectural boundaries may be weakening.",

        <= 0.20 =>
            "Significant implicit coupling detected. Hidden dependencies may reduce modularity.",

        _ =>
            "Critical implicit coupling detected. System architecture may be entangled."
    };

            sb.AppendLine("<div style='display:flex; gap:40px; flex-wrap:wrap;'>");
            sb.AppendLine(render.RenderRadarSvg(hygiene, breakdown, solid, implicitCoupling));

            if (coupling != null)
            {
                sb.AppendLine(render.RenderArchitecturalGalaxy(coupling));
            }
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li>{coreDiagnosis}</li>");
            sb.AppendLine($"<li>{namespaceDiagnosis}</li>");
            sb.AppendLine($"<li>{globalNamespaceDiagnosis}</li>");
            sb.AppendLine($"<li>{unresolvedDiagnosis}</li>");
            sb.AppendLine($"<li>{solidDiagnosis}</li>");
            sb.AppendLine($"<li>{implicitCouplingDiagnosis}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");


            return sb.ToString();
        }
    }
}
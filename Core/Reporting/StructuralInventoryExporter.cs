using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Model;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;
using System.Globalization;
using System.Text;

namespace RefactorScope.Core.Reporting
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

            var confirmed = report.GetConfirmedZombies();
            var suspicious = report.GetSuspiciousZombies();

            var confirmedCount = confirmed.Count;
            var candidatesCount = hygiene.UnreferencedCount;
            var absolvedCount = candidatesCount - confirmedCount;

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


                                sb.AppendLine($@"
                    <div class='card tooltip warning'>
                    Structural Candidates<br>
                    <b>{candidatesCount}</b>
                    <span class='tooltiptext'>
                    Classes flagged as potential dead-code candidates
                    based on structural reference analysis.

                    These classes appear to have zero or near-zero
                    static usage in the analyzed codebase.

                    This stage represents the initial detection phase.
                    </span>
                    </div>");

                                sb.AppendLine($@"
                    <div class='card tooltip ok'>
                    Pattern Similarity<br>
                    <b>{absolvedCount}</b>
                    <span class='tooltiptext'>
                    Classes that exhibit structural similarity
                    to known design patterns.

                    Similarity is detected using heuristics such as:

                    • Interface usage  
                    • Factory patterns  
                    • Strategy patterns  
                    • Dependency injection structures  

                    Important:
                    Pattern similarity does NOT guarantee runtime usage.
                    Legacy code may still appear in this category.
                    </span>
                    </div>");

                                sb.AppendLine($@"
                    <div class='card tooltip danger'>
                    Unresolved<br>
                    <b>{confirmedCount}</b>
                    <span class='tooltiptext'>
                    Classes that could not be associated with
                    recognized structural patterns.

                    These remain potential dead-code candidates,
                    but the analyzer cannot provide definitive proof.

                    Manual inspection is recommended.
                    </span>
                    </div>");


            sb.AppendLine($"<div class='card warning'>Namespace Drift<br><b>{hygiene.NamespaceDriftCount}</b></div>");
            sb.AppendLine($"<div class='card'>Global Namespace<br><b>{hygiene.GlobalNamespaceCount}</b></div>");
            sb.AppendLine($"<div class='card'>Isolated Core<br><b>{hygiene.IsolatedCoreCount}</b></div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Structural Entropy<br>
<b>{hygiene.StructuralEntropy:0.000}</b>
<span class='tooltiptext'>
Normalized Shannon Entropy (0–1 scale).

0.00–0.25 → Homogeneous structure  
0.25–0.60 → Moderate dispersion  
0.60–1.00 → High architectural fragmentation
</span>
</div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Smell Index<br>
<b>{hygiene.SmellIndex:0.0}</b>
<span class='tooltiptext'>
Proportional composite hygiene indicator (0–100).

Unreferenced weight: 40  
Namespace Drift weight: 20  
Isolation weight: 20  
Entropy weight: 20
</span>
</div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Hygiene Level<br>
<b>{hygiene.HygieneLevel}</b>
<span class='tooltiptext'>
Healthy → Stable structure  
Stable → Controlled signals  
Degrading → Hygiene erosion  
Critical → Architectural risk  
Structural Risk → Severe issue density
</span>
</div>");

            if (solid != null)
            {
                sb.AppendLine($@"
<div class='card tooltip'>
SOLID Alerts<br>
<b>{solid.Alerts.Count}</b>
<span class='tooltiptext'>
Two-Pass Heuristic Model:
1) Suspicion detection  
2) Contextual absolution filtering
</span>
</div>");
            }

            sb.AppendLine("</div>");

            #endregion


            #region RADAR RESTAURADO

            sb.AppendLine("<div style='margin-top:40px;'>");
            sb.AppendLine("<h2>📡 Architectural Radar</h2>");
            sb.AppendLine(RenderRadarSvg(hygiene));
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

            foreach (var item in items)
            {
                string rowClass = "";

                if (confirmed.Contains(item.TypeName))
                    rowClass = "danger";
                else if (suspicious.Contains(item.TypeName))
                    rowClass = "warning";
                else if (item.Layer == "Core")
                    rowClass = "core";

                sb.AppendLine($"<tr class='{rowClass}'>");
                sb.AppendLine($"<td>{item.TypeName}</td>");
                sb.AppendLine($"<td>{item.DeclaredInFile}</td>");
                sb.AppendLine($"<td>{item.Namespace}</td>");
                sb.AppendLine($"<td>{item.UsageCount}</td>");
                sb.AppendLine($"<td>{item.Status}</td>");
                sb.AppendLine($"<td>{item.RemovalCandidate}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

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
Structural Unreferenced does NOT imply runtime unused.
Namespace Drift indicates folder/namespace misalignment.
Smell Index is proportional and statistically normalized.
These metrics are architectural signals, not formal proofs.
Human review remains mandatory.
</div>");

            #endregion

            sb.AppendLine("</body></html>");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private string RenderRadarSvg(HygieneReport h)
        {
            double Normalize(int value, int total) => total == 0 ? 0 : value / (double)total;
            string Fmt(double val) => val.ToString(CultureInfo.InvariantCulture);

            var values = new[]
            {
                Normalize(h.UnreferencedCount, h.TotalClasses),
                Normalize(h.NamespaceDriftCount, h.TotalClasses),
                Normalize(h.GlobalNamespaceCount, h.TotalClasses),
                h.StructuralEntropy,
                Normalize(h.IsolatedCoreCount, h.TotalClasses)
            };

            var labels = new[]
            {
                "Unreferenced",
                "Namespace Drift",
                "Global",
                "Entropy",
                "Isolation"
            };

            int size = 320;
            int center = size / 2;
            int radius = 110;
            int levels = 4;

            var sb = new StringBuilder();
            sb.AppendLine($"<svg width='{size}' height='{size}' style='background:#161b22;border-radius:12px'>");

            for (int l = 1; l <= levels; l++)
            {
                double r = radius * (l / (double)levels);
                sb.AppendLine($"<circle cx='{center}' cy='{center}' r='{Fmt(r)}' fill='none' stroke='#30363d' stroke-width='1' />");
            }

            for (int i = 0; i < values.Length; i++)
            {
                double angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                double x = center + radius * Math.Cos(angle);
                double y = center + radius * Math.Sin(angle);

                sb.AppendLine($"<line x1='{center}' y1='{center}' x2='{Fmt(x)}' y2='{Fmt(y)}' stroke='#30363d' />");

                double lx = center + (radius + 20) * Math.Cos(angle);
                double ly = center + (radius + 20) * Math.Sin(angle);

                sb.AppendLine($"<text x='{Fmt(lx)}' y='{Fmt(ly)}' fill='#e6edf3' font-size='12' text-anchor='middle'>{labels[i]}</text>");
            }

            var points = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                double angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                double r = radius * values[i];
                double x = center + r * Math.Cos(angle);
                double y = center + r * Math.Sin(angle);

                points.Add($"{Fmt(x)},{Fmt(y)}");
            }

            sb.AppendLine($"<polygon points='{string.Join(" ", points)}' fill='rgba(255,99,132,0.4)' stroke='#ff6384' stroke-width='2'/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
    }
}
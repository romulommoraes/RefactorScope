using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Analyzers;
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
            sb.AppendLine("tr.dead { background:#2d1117; }");
            sb.AppendLine("tr.legacy { background:#332701; }");
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
  }
}");
            sb.AppendLine("</script>");

            #endregion

            sb.AppendLine("</head><body>");

            sb.AppendLine("<h1>🧬 Structural Dashboard</h1>");
            sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");

            #region KPI CARDS

            sb.AppendLine("<div>");
            sb.AppendLine($"<div class='card'>Total Classes<br><b>{hygiene.TotalClasses}</b></div>");
            sb.AppendLine($"<div class='card danger'>Dead<br><b>{hygiene.DeadCount}</b></div>");
            sb.AppendLine($"<div class='card warning'>Legacy<br><b>{hygiene.LegacyCount}</b></div>");
            sb.AppendLine($"<div class='card ok'>Core<br><b>{hygiene.CoreCount}</b></div>");
            sb.AppendLine($"<div class='card'>Removal Candidates<br><b>{hygiene.RemovalCandidates}</b></div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Structural Entropy<br>
<b>{hygiene.StructuralEntropy:0.000}</b>
<span class='tooltiptext'>
Normalized Shannon Entropy (0–1 scale).

0.00–0.25 → Homogeneous structure  
0.25–0.60 → Moderate dispersion  
0.60–1.00 → High architectural fragmentation  

Higher values indicate structural disorder.
</span>
</div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Smell Index<br>
<b>{hygiene.SmellIndex:0.0}</b>
<span class='tooltiptext'>
Composite hygiene indicator (0–100 scale).

0–20 → Healthy  
20–40 → Attention  
40–60 → Degrading  
60–80 → Critical  
80–100 → Structural Collapse  

Heuristic early-warning metric.
</span>
</div>");

            sb.AppendLine($@"
<div class='card tooltip'>
Hygiene Level<br>
<b>{hygiene.HygieneLevel}</b>
<span class='tooltiptext'>
Textual interpretation of Smell Index.

Healthy → Controlled  
Attention → Growing signals  
Degrading → Smell accumulation  
Critical → Architectural risk  
Structural Collapse → Severe erosion
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
2) Absolution filtering  

This is an alert system, not a formal compliance proof.
</span>
</div>");
            }

            sb.AppendLine("</div>");

            #endregion

            #region RADAR (LEFT) + SEARCH BELOW

            sb.AppendLine("<div style='margin-top:40px;'>");
            sb.AppendLine("<h2>📡 Architectural Radar</h2>");
            sb.AppendLine(RenderRadarSvg(hygiene));

            sb.AppendLine(@"
<div style='margin-top:20px;'>
  <input id='search' onkeyup='searchTable()' placeholder='Search...' 
         style='width:350px; padding:8px; background:#161b22; border:1px solid #30363d; color:white;'>
</div>");

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
                var rowClass = item.Status.Contains("Morto") ? "dead"
                             : item.Status.Contains("Legado") ? "legacy"
                             : item.Layer == "Core" ? "core"
                             : "";

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
            if (solid != null && solid.Alerts.Any()) { sb.AppendLine("<h2 style='margin-top:50px;'>⚠ SOLID Alerts</h2>"); sb.AppendLine("<table>"); sb.AppendLine("<tr><th>Principle</th><th>Class</th><th>Namespace</th><th>Reason</th></tr>"); foreach (var alert in solid.Alerts) { sb.AppendLine("<tr>"); sb.AppendLine($"<td>{alert.Principle}</td>"); sb.AppendLine($"<td>{alert.ClassName}</td>"); sb.AppendLine($"<td>{alert.Namespace}</td>"); sb.AppendLine($"<td>{alert.Reason}</td>"); sb.AppendLine("</tr>"); } sb.AppendLine("</table>"); }
            #endregion 
            #region DISCLAIMER
            sb.AppendLine(@" <div style='margin-top:60px; padding:20px; background:#111827; border-radius:10px; font-size:13px; color:#9ca3af;'> <h3>📘 Methodology & Limitations</h3> This dashboard is based on heuristic structural analysis. SOLID evaluation follows a two-pass refinement model: • Pass 1 – Structural suspicion detection • Pass 2 – Contextual absolution filtering Entropy is computed using normalized Shannon entropy. Smell Index is a composite heuristic metric. These indicators are early-warning architectural signals. They are NOT formal proofs of design violations. Final architectural judgment must always be performed by a human reviewer. </div> "); 
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
                Normalize(h.DeadCount, h.TotalClasses),
                Normalize(h.LegacyCount, h.TotalClasses),
                Normalize(h.RemovalCandidates, h.TotalClasses),
                h.StructuralEntropy,
                Normalize(h.IsolatedCoreCount, h.TotalClasses)
            };

            var labels = new[] { "Dead", "Legacy", "Removal", "Entropy", "Isolation" };

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
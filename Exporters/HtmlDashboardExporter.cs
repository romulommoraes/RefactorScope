using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;
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

            var htmlPath = Path.Combine(dashboardDir, "index.html");

            var confirmed = report.GetEffectiveZombieTypes();
            var structural = report.GetStructuralCandidates();

            var html = BuildHtml(confirmed, structural);

            File.WriteAllText(htmlPath, html);
        }

        private string BuildHtml(
            IReadOnlyList<string> confirmed,
            IReadOnlyList<string> structural)
        {
            var confirmedList = new StringBuilder();
            foreach (var z in confirmed)
                confirmedList.AppendLine($"<li class='confirmed'>{z}</li>");

            var structuralList = new StringBuilder();
            foreach (var z in structural)
                structuralList.AppendLine($"<li>{z}</li>");

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<title>RefactorScope Dashboard</title>
<script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
<script src='https://cdn.jsdelivr.net/npm/papaparse@5.4.1/papaparse.min.js'></script>
<style>
body {{ background:#111; color:#eee; font-family:Arial; }}
.container {{ display:flex; flex-wrap:wrap; }}
.card {{ background:#1b1b1b; margin:20px; padding:20px; border-radius:8px; width:45%; }}

.confirmed {{
    color:#ff4c4c;
    font-weight:bold;
}}

.section {{
    margin:20px;
    padding:20px;
    background:#1b1b1b;
    border-radius:8px;
}}

.count {{
    font-size:20px;
    font-weight:bold;
}}

</style>
</head>
<body>

<h1>RefactorScope Dashboard</h1>

<div class='section'>
<h2>Zombie Classification (ADR-EXP-007)</h2>
<p class='count'>Confirmed Zombies: <span style='color:#ff4c4c'>{confirmed.Count}</span></p>
<p class='count'>Structural Candidates: {structural.Count}</p>

<h3>Confirmed Zombies</h3>
<ul>
{confirmedList}
</ul>

<h3>All Structural Candidates</h3>
<ul>
{structuralList}
</ul>
</div>

<div class='container'>
<div class='card'>
<canvas id='radarChart'></canvas>
</div>
<div class='card'>
<canvas id='heatChart'></canvas>
</div>
</div>

<script>
function loadCSV(path, callback) {{
    Papa.parse(path, {{
        download: true,
        header: true,
        complete: results => callback(results.data)
    }});
}}

loadCSV('../dataset_arch_health.csv', data => {{

    const modules = data.map(x => x.Module);
    const zombies = data.map(x => parseFloat(x.ZombieRate));
    const isolation = data.map(x => parseFloat(x.IsolationRate));

    new Chart(document.getElementById('radarChart'), {{
        type: 'radar',
        data: {{
            labels: modules,
            datasets: [
                {{ label: 'Zombie Rate', data: zombies }},
                {{ label: 'Isolation', data: isolation }}
            ]
        }}
    }});

    new Chart(document.getElementById('heatChart'), {{
        type: 'bar',
        data: {{
            labels: modules,
            datasets: [
                {{ label: 'Zombie Heat', data: zombies }}
            ]
        }}
    }});

}});
</script>

</body>
</html>";
        }
    }
}
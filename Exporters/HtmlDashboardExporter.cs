using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
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

            var unresolved = report.GetEffectiveUnresolvedCandidates();
            var structural = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();

            var html = BuildHtml(unresolved, structural, patternSimilarity);

            File.WriteAllText(htmlPath, html);
        }

        private string BuildHtml(
            IReadOnlyList<string> unresolved,
            IReadOnlyList<string> structural,
            IReadOnlyList<string> patternSimilarity)
        {
            var unresolvedList = new StringBuilder();
            foreach (var item in unresolved)
                unresolvedList.AppendLine($"<li class='unresolved'>{item}</li>");

            var structuralList = new StringBuilder();
            foreach (var item in structural)
                structuralList.AppendLine($"<li>{item}</li>");

            var patternSimilarityList = new StringBuilder();
            foreach (var item in patternSimilarity)
                patternSimilarityList.AppendLine($"<li class='pattern'>{item}</li>");

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

.card {{
    background:#1b1b1b;
    margin:20px;
    padding:20px;
    border-radius:8px;
    width:45%;
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

.unresolved {{
    color:#ff4c4c;
    font-weight:bold;
}}

.pattern {{
    color:#06d6a0;
}}

</style>

</head>
<body>

<h1>RefactorScope Dashboard</h1>

<div class='section'>
<h2>Structural Candidate Analysis (ADR-EXP-007)</h2>

<p class='count'>Structural Candidates: {structural.Count}</p>

<p class='count'>
Pattern Similarity: 
<span style='color:#06d6a0'>{patternSimilarity.Count}</span>
</p>

<p class='count'>
Unresolved: 
<span style='color:#ff4c4c'>{unresolved.Count}</span>
</p>

<h3>Unresolved Candidates</h3>
<ul>
{unresolvedList}
</ul>

<h3>Pattern Similarity</h3>
<ul>
{patternSimilarityList}
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
    const candidates = data.map(x => parseFloat(x.CandidateRate));
    const isolation = data.map(x => parseFloat(x.IsolationRate));

    new Chart(document.getElementById('radarChart'), {{
        type: 'radar',
        data: {{
            labels: modules,
            datasets: [
                {{ label: 'Structural Candidates', data: candidates }},
                {{ label: 'Isolation', data: isolation }}
            ]
        }}
    }});

    new Chart(document.getElementById('heatChart'), {{
        type: 'bar',
        data: {{
            labels: modules,
            datasets: [
                {{ label: 'Candidate Density', data: candidates }}
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
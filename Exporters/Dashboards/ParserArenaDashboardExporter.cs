using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Arena;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Exporters.Dashboards.Renderers;
using RefactorScope.Exporters.Styling;
using System.Globalization;

using System.Text;

namespace RefactorScope.Exporters.Dashboards;

/// <summary>
/// Arena Dashboard
///
/// Responsabilidade
/// ----------------
/// Renderizar o dashboard HTML comparativo do modo Arena,
/// consolidando:
///
/// - comparação entre estratégias de parser
/// - vencedores por projeto
/// - médias globais por estratégia
/// - telemetria executiva
/// - tabelas comparativas detalhadas
/// - visualizações P5 dedicadas
/// </summary>
public sealed class ParserArenaDashboardExporter
{
    public void Export(
        IReadOnlyList<ParserArenaProjectResult> results,
        string htmlPath,
        string themeFileName)
    {
        var html = GenerateHtml(results, themeFileName);
        File.WriteAllText(htmlPath, html, Encoding.UTF8);
    }

    private string GenerateHtml(
        IReadOnlyList<ParserArenaProjectResult> results,
        string themeFileName)
    {
        results ??= Array.Empty<ParserArenaProjectResult>();

        var allRuns = results
            .SelectMany(r => r.Runs)
            .ToList();

        var totalProjects = results.Count;
        var totalRuns = allRuns.Count;
        var totalFailures = allRuns.Count(r => r.Status == ParseStatus.Failed);
        var successRuns = allRuns.Count(r => r.Status == ParseStatus.Success);
        var avgConfidence = allRuns.Count == 0 ? 0 : Clamp01(allRuns.Average(r => r.Confidence));
        var avgScore = allRuns.Count == 0 ? 0 : allRuns.Average(r => r.ComparativeScore);
        var avgExecutionMs = allRuns.Count == 0 ? 0 : allRuns.Average(r => r.ExecutionTime.TotalMilliseconds);

        var winners = results
            .Select(r => r.BestRun)
            .Where(r => r != null)
            .Cast<ParserArenaRunResult>()
            .ToList();

        var winnerGroups = winners
            .GroupBy(w => ShortenStrategyName(w.Strategy))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var strategyMetrics = BuildStrategyMetrics(allRuns);
        var visualMetrics = BuildStrategyVisualMetrics(strategyMetrics);

        var bestOverallRun = allRuns
            .OrderByDescending(r => r.ComparativeScore)
            .ThenByDescending(r => r.Confidence)
            .ThenBy(r => r.ExecutionTime)
            .FirstOrDefault();

        var healthiestStrategy = strategyMetrics
            .OrderByDescending(m => m.AverageScore)
            .ThenByDescending(m => m.AverageConfidence)
            .ThenBy(m => m.AverageExecutionMs)
            .FirstOrDefault();

        var supportBand = GetSupportBand(avgConfidence);
        var targetName = $"Parser Arena // {totalProjects} project(s)";

        var charts = new ParserArenaControlChartsRendererP5();
        var sb = new StringBuilder();

        sb.AppendLine(DashboardHtmlShell.RenderDocumentStart(
            "Parser Arena Dashboard",
            themeFileName));

        // =====================================================
        // TOPBAR
        // =====================================================

        sb.AppendLine($"""
<div class="topbar" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <div class="brand">
        <div class="brand-kicker">RefactorScope // Comparative Arena</div>
        <h1>Parser Arena Dashboard</h1>
        <div class="subtitle">Comparative parser analysis across one or more target projects</div>
    </div>

    <div class="run-meta">
        <div class="optic-mode-wrapper">
            <span class="optic-label">OPTIC_MODE</span>
            <button id="themeCyclerBtn" class="red-tactical-btn" aria-label="Cycle Theme" title="Engage Optic Cycle"></button>
        </div>
        <div><b>Generated:</b> {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC</div>
        <div><b>Target:</b> {Html(targetName)}</div>
        <div><b>Projects:</b> {totalProjects}</div>
        <div><b>Avg Confidence:</b> {avgConfidence:P0}</div>
        <div><b>Support Band:</b> {Html(supportBand)}</div>
    </div>
</div>
""");

        // =====================================================
        // KPI GRID
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Arena Signal Grid</h2>
        <div class="line"></div>
    </div>
</div>
""");

        sb.AppendLine($"""
<div class="grid-kpis arena-kpis">
    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Projects</div>
        <div class="value">{totalProjects}</div>
        <div class="hint">Projects evaluated by the arena execution</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Runs</div>
        <div class="value">{totalRuns}</div>
        <div class="hint">Total parser executions across all projects</div>
    </div>

    <div class="kpi {(totalFailures == 0 ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Failures</div>
        <div class="value">{totalFailures}</div>
        <div class="hint">Runs that failed completely during arena execution</div>
    </div>

    <div class="kpi {(avgConfidence >= 0.85 ? "good" : avgConfidence >= 0.65 ? "warning" : "alert")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Avg Confidence</div>
        <div class="value">{avgConfidence:P0}</div>
        <div class="hint">Average parser confidence across all comparative runs</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Avg Score</div>
        <div class="value">{avgScore:0.00}</div>
        <div class="hint">Average comparative score across all runs</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Avg Execution</div>
        <div class="value">{avgExecutionMs:0} ms</div>
        <div class="hint">Average execution time per comparative run</div>
    </div>

    <div class="kpi {(bestOverallRun != null ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Best Overall Run</div>
        <div class="value" style="font-size:22px;">{Html(bestOverallRun?.ProjectName ?? "N/A")}</div>
        <div class="hint">{Html(bestOverallRun == null ? "No successful run available." : $"{ShortenStrategyName(bestOverallRun.Strategy)} // {bestOverallRun.ComparativeScore:0.00}")}</div>
    </div>

    <div class="kpi {(healthiestStrategy != null ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Best Strategy</div>
        <div class="value" style="font-size:22px;">{Html(healthiestStrategy?.StrategyName ?? "N/A")}</div>
        <div class="hint">{Html(healthiestStrategy == null ? "No strategy aggregate available." : $"Avg score {healthiestStrategy.AverageScore:0.00} // Avg confidence {Clamp01(healthiestStrategy.AverageConfidence):P0}")}</div>
    </div>

    <div class="kpi {(successRuns == totalRuns ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Success Runs</div>
        <div class="value">{successRuns}</div>
        <div class="hint">Runs completed successfully without full parser failure</div>
    </div>

    <div class="kpi {(winners.Count == totalProjects && totalProjects > 0 ? "good" : "warning")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Winner Coverage</div>
        <div class="value">{(totalProjects == 0 ? 0 : (winners.Count / (double)totalProjects)):P0}</div>
        <div class="hint">Projects that produced at least one comparative winner</div>
    </div>
</div>
""");

        // =====================================================
        // EXTENDED READING
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Executive Reading</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

        sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Batch Summary</h3>
    <ul class="clean">
        <li><b>Projects:</b> {totalProjects}</li>
        <li><b>Total Runs:</b> {totalRuns}</li>
        <li><b>Success Runs:</b> {successRuns}</li>
        <li><b>Failures:</b> {totalFailures}</li>
        <li><b>Average Confidence:</b> {avgConfidence:P0}</li>
        <li><b>Average Comparative Score:</b> {avgScore:0.00}</li>
        <li><b>Average Execution:</b> {avgExecutionMs:0.00} ms</li>
        <li><b>Top Strategy:</b> {Html(healthiestStrategy?.StrategyName ?? "Unknown")}</li>
        <li><b>Best Overall Run:</b> {Html(bestOverallRun?.ProjectName ?? "Unknown")}</li>
        <li><b>Best Overall Parser:</b> {Html(bestOverallRun == null ? "Unknown" : ShortenParserName(bestOverallRun.ParserName))}</li>
    </ul>
</div>
""");

        sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Comparative Diagnosis</h3>
    <ul class="clean">
        <li>{Html(GetBatchConfidenceDiagnosis(avgConfidence))}</li>
        <li>{Html(GetFailureDiagnosis(totalFailures, totalRuns))}</li>
        <li>{Html(GetWinnerDiagnosis(winnerGroups))}</li>
        <li>{Html(GetStrategyDiagnosis(healthiestStrategy))}</li>
        <li>{Html(GetExecutionDiagnosis(avgExecutionMs))}</li>
    </ul>

    <div style="margin-top:16px;">
        <div class="badge {(avgConfidence >= 0.85 ? "green" : avgConfidence >= 0.65 ? "amber" : "red")}"><span class="badge-dot"></span> Avg Confidence: {avgConfidence:P0}</div>
        <div class="badge {(totalFailures == 0 ? "green" : totalFailures <= Math.Max(1, totalRuns / 5) ? "amber" : "red")}" style="margin-top:8px;"><span class="badge-dot"></span> Failures: {totalFailures}</div>
        <div class="badge {(healthiestStrategy != null ? "green" : "amber")}" style="margin-top:8px;"><span class="badge-dot"></span> Top Strategy: {Html(healthiestStrategy?.StrategyName ?? "N/A")}</div>
    </div>
</div>
""");

        sb.AppendLine("</div></div>");

        // =====================================================
        // CHARTS
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Charts & Diagnostics</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("""
<div class="arena-chart-grid">
""");

        sb.AppendLine(charts.RenderStrategyPerformanceScatter(visualMetrics));
        sb.AppendLine(charts.RenderWinnerDonut(winnerGroups));
        sb.AppendLine(charts.RenderEfficiencyScatter(visualMetrics));
        sb.AppendLine(charts.RenderProjectStrategyHeatmap(results));

        sb.AppendLine("""
</div>
""");

        sb.AppendLine("</div></div>");

        // =====================================================
        // STRATEGY COMPARISON TABLE
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Strategy Aggregates</h2>
        <div class="line"></div>
    </div>
    <div class="panel wide-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("""
<div class="table-shell">
<table class="link-table parsing-table">
    <thead>
        <tr>
            <th>Strategy</th>
            <th>Runs</th>
            <th>Avg Score</th>
            <th>Avg Confidence</th>
            <th>Avg Time</th>
            <th>Avg Types</th>
            <th>Avg Refs</th>
            <th>Failures</th>
        </tr>
    </thead>
    <tbody>
""");

        foreach (var metric in strategyMetrics)
        {
            sb.AppendLine($"""
<tr>
    <td>{Html(metric.StrategyName)}</td>
    <td>{metric.RunCount}</td>
    <td>{metric.AverageScore:0.00}</td>
    <td>{Clamp01(metric.AverageConfidence):P0}</td>
    <td>{metric.AverageExecutionMs:0.00} ms</td>
    <td>{metric.AverageTypes:0.00}</td>
    <td>{metric.AverageReferences:0.00}</td>
    <td>{metric.Failures}</td>
</tr>
""");
        }

        sb.AppendLine("""
    </tbody>
</table>
</div>
</div></div>
""");

        // =====================================================
        // DETAILED PROJECT TABLE
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Project Comparative Matrix</h2>
        <div class="line"></div>
    </div>
    <div class="panel wide-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("""
<div style="margin-bottom:16px;">
    <input id="searchProjects"
           type="text"
           onkeyup="searchArenaProjects()"
           placeholder="Search project, parser, strategy..."
           style="width:100%; padding:12px; border-radius:10px; border:1px solid rgba(255,255,255,0.14); background:rgba(255,255,255,0.03); color:#fff;" />
</div>
""");

        sb.AppendLine("""
<div class="table-shell">
<table id="arenaProjectTable" class="link-table parsing-table">
    <thead>
        <tr>
            <th onclick="sortArenaTable('arenaProjectTable', 0)">Project</th>
            <th onclick="sortArenaTable('arenaProjectTable', 1)">Strategy</th>
            <th onclick="sortArenaTable('arenaProjectTable', 2)">Parser</th>
            <th onclick="sortArenaTable('arenaProjectTable', 3)">Status</th>
            <th onclick="sortArenaTable('arenaProjectTable', 4)">Score</th>
            <th onclick="sortArenaTable('arenaProjectTable', 5)">Confidence</th>
            <th onclick="sortArenaTable('arenaProjectTable', 6)">Files</th>
            <th onclick="sortArenaTable('arenaProjectTable', 7)">Types</th>
            <th onclick="sortArenaTable('arenaProjectTable', 8)">Refs</th>
            <th onclick="sortArenaTable('arenaProjectTable', 9)">Time</th>
            <th onclick="sortArenaTable('arenaProjectTable', 10)">Fallback</th>
        </tr>
    </thead>
    <tbody>
""");

        foreach (var project in results)
        {
            foreach (var run in project.OrderedRuns)
            {
                sb.AppendLine($"""
<tr>
    <td>{Html(project.ProjectName)}</td>
    <td>{Html(ShortenStrategyName(run.Strategy))}</td>
    <td>{Html(ShortenParserName(run.ParserName))}</td>
    <td>{Html(run.Status.ToString())}</td>
    <td>{run.ComparativeScore:0.00}</td>
    <td>{Clamp01(run.Confidence):P0}</td>
    <td>{run.FileCount}</td>
    <td>{run.TypeCount}</td>
    <td>{run.ReferenceCount}</td>
    <td>{run.ExecutionTime.TotalMilliseconds:0} ms</td>
    <td>{(run.UsedFallback ? "Yes" : "No")}</td>
</tr>
""");
            }
        }

        sb.AppendLine("""
    </tbody>
</table>
</div>
</div></div>
""");

        // =====================================================
        // WINNERS TABLE
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Best Parser Per Project</h2>
        <div class="line"></div>
    </div>
    <div class="panel wide-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("""
<div class="table-shell">
<table class="link-table parsing-table">
    <thead>
        <tr>
            <th>Project</th>
            <th>Winner</th>
            <th>Strategy</th>
            <th>Status</th>
            <th>Score</th>
            <th>Confidence</th>
            <th>Types</th>
            <th>Refs</th>
            <th>Time</th>
        </tr>
    </thead>
    <tbody>
""");

        foreach (var project in results)
        {
            var best = project.BestRun;

            if (best == null)
            {
                sb.AppendLine($"""
<tr>
    <td>{Html(project.ProjectName)}</td>
    <td>N/A</td>
    <td>N/A</td>
    <td>N/A</td>
    <td>0.00</td>
    <td>0%</td>
    <td>0</td>
    <td>0</td>
    <td>0 ms</td>
</tr>
""");
                continue;
            }

            sb.AppendLine($"""
<tr>
    <td>{Html(project.ProjectName)}</td>
    <td>{Html(ShortenParserName(best.ParserName))}</td>
    <td>{Html(ShortenStrategyName(best.Strategy))}</td>
    <td>{Html(best.Status.ToString())}</td>
    <td>{best.ComparativeScore:0.00}</td>
    <td>{Clamp01(best.Confidence):P0}</td>
    <td>{best.TypeCount}</td>
    <td>{best.ReferenceCount}</td>
    <td>{best.ExecutionTime.TotalMilliseconds:0} ms</td>
</tr>
""");
        }

        sb.AppendLine("""
    </tbody>
</table>
</div>
</div></div>
""");

        // =====================================================
        // METHODOLOGY
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Methodology & Interpretation</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
        <ul class="clean">
            <li>This dashboard compares parser strategies executed over one or more target projects.</li>
            <li>Comparative Score is contextual to each project and should be interpreted as a relative ranking signal, not as a universal benchmark.</li>
            <li>Confidence is heuristic and should be read as structural usability, not formal parser correctness.</li>
            <li>Winner distribution helps reveal which strategy most often dominates across the evaluated project set.</li>
            <li>Strategy aggregates are useful for publication, comparative baselines and future CSV / JSON analytical exports.</li>
            <li>Failures do not invalidate the whole arena, but they indicate strategies or projects deserving inspection.</li>
        </ul>
    </div>
</div>
""");

        // =====================================================
        // STYLES + SCRIPT
        // =====================================================

        sb.AppendLine("""
<style>
.grid-kpis.arena-kpis {
    display: grid;
    grid-template-columns: repeat(5, minmax(0, 1fr));
    gap: 16px;
}

.arena-chart-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 24px;
    align-items: stretch;
}

.arena-chart-grid .chart-container {
    min-width: 0;
}

@media (max-width: 1400px) {
    .grid-kpis.arena-kpis {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }
}

@media (max-width: 1180px) {
    .arena-chart-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 980px) {
    .grid-kpis.arena-kpis {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

.table-shell {
    max-height: 720px;
    overflow-y: auto;
    overflow-x: auto;
    border: 1px solid rgba(255,255,255,0.08);
    border-radius: 12px;
}

.parsing-table thead th {
    position: sticky;
    top: 0;
    z-index: 2;
    background: rgba(8, 18, 40, 0.96);
    backdrop-filter: blur(4px);
}
</style>

<script>
function searchArenaProjects() {
  var input = document.getElementById('searchProjects');
  var filter = input.value.toLowerCase();
  var table = document.getElementById('arenaProjectTable');
  var rows = table.getElementsByTagName('tr');

  for (var i = 1; i < rows.length; i++) {
    rows[i].style.display = rows[i].innerText.toLowerCase().includes(filter) ? '' : 'none';
  }
}

function sortArenaTable(tableId, n) {
  var table = document.getElementById(tableId);
  var switching = true;
  var dir = 'asc';

  while (switching) {
    switching = false;
    var rows = table.rows;

    for (var i = 1; i < rows.length - 1; i++) {
      var x = rows[i].getElementsByTagName('TD')[n];
      var y = rows[i + 1].getElementsByTagName('TD')[n];
      var shouldSwitch = false;

      if (!x || !y) continue;

      var xv = x.innerText.toLowerCase();
      var yv = y.innerText.toLowerCase();

      if (dir === 'asc' && xv > yv) shouldSwitch = true;
      if (dir === 'desc' && xv < yv) shouldSwitch = true;

      if (shouldSwitch) {
        rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
        switching = true;
      }
    }

    if (!switching && dir === 'asc') {
      dir = 'desc';
      switching = true;
    }
  }
}
</script>
""");

        sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
            "Generated by RefactorScope Parser Arena Dashboard // Shared Hub Layout"));

        return sb.ToString();
    }

    // =====================================================
    // METRICS
    // =====================================================

    private static List<StrategyAggregateMetric> BuildStrategyMetrics(
        IReadOnlyList<ParserArenaRunResult> runs)
    {
        return runs
            .GroupBy(r => r.Strategy)
            .OrderBy(g => StrategyOrder(g.Key))
            .Select(g => new StrategyAggregateMetric
            {
                Strategy = g.Key,
                StrategyName = ShortenStrategyName(g.Key),
                RunCount = g.Count(),
                AverageScore = g.Average(x => x.ComparativeScore),
                AverageConfidence = g.Average(x => Clamp01(x.Confidence)),
                AverageExecutionMs = g.Average(x => x.ExecutionTime.TotalMilliseconds),
                AverageFiles = g.Average(x => x.FileCount),
                AverageTypes = g.Average(x => x.TypeCount),
                AverageReferences = g.Average(x => x.ReferenceCount),
                Failures = g.Count(x => x.Status == ParseStatus.Failed)
            })
            .ToList();
    }

    private static List<ParserArenaControlChartsRendererP5.StrategyVisualMetric> BuildStrategyVisualMetrics(
        IReadOnlyList<StrategyAggregateMetric> metrics)
    {
        return metrics
            .Select(m => new ParserArenaControlChartsRendererP5.StrategyVisualMetric
            {
                Strategy = m.Strategy,
                StrategyName = m.StrategyName,
                Color = GetStrategyColor(m.Strategy),
                RunCount = m.RunCount,
                AverageScore = m.AverageScore,
                AverageConfidence = m.AverageConfidence,
                AverageExecutionMs = m.AverageExecutionMs,
                AverageTypes = m.AverageTypes,
                AverageReferences = m.AverageReferences,
                Failures = m.Failures
            })
            .ToList();
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private static int StrategyOrder(ParserStrategy strategy)
    {
        return strategy switch
        {
            ParserStrategy.RegexFast => 1,
            ParserStrategy.Selective => 2,
            ParserStrategy.AdaptiveExperimental => 3,
            ParserStrategy.IncrementalExperimental => 4,
            ParserStrategy.Comparative => 99,
            _ => 100
        };
    }

    private static string ShortenStrategyName(ParserStrategy strategy)
    {
        return strategy switch
        {
            ParserStrategy.RegexFast => "Regex",
            ParserStrategy.Selective => "Selective",
            ParserStrategy.AdaptiveExperimental => "Adaptive",
            ParserStrategy.IncrementalExperimental => "Incremental",
            ParserStrategy.Comparative => "Comparative",
            _ => strategy.ToString()
        };
    }

    private static string ShortenParserName(string parserName)
    {
        return parserName switch
        {
            "CSharpRegex" => "Regex",
            "CSharpTextual" => "Textual",
            "HybridSelectiveParser" => "Selective",
            "HybridParser (Adaptive)" => "Adaptive",
            "HybridParser (Incremental)" => "Incremental",
            _ => parserName
        };
    }

    private static string GetStrategyColor(ParserStrategy strategy)
    {
        return strategy switch
        {
            ParserStrategy.RegexFast => "#ff9a3c",
            ParserStrategy.Selective => "#7fd36b",
            ParserStrategy.AdaptiveExperimental => "#6ea8ff",
            ParserStrategy.IncrementalExperimental => "#ffc15d",
            ParserStrategy.Comparative => "#ff6a3d",
            _ => "#8fa8ff"
        };
    }

    private static string GetSupportBand(double confidence)
    {
        var safe = Clamp01(confidence);
        if (safe >= 0.85) return "High";
        if (safe >= 0.65) return "Medium";
        return "Low";
    }

    private static string GetBatchConfidenceDiagnosis(double confidence)
    {
        var safe = Clamp01(confidence);

        if (safe >= 0.85)
            return "Comparative batch confidence is high. The average structural extraction signal looks strong across the evaluated projects.";

        if (safe >= 0.65)
            return "Comparative batch confidence is moderate. The arena is usable, but some runs may still deserve manual inspection.";

        return "Comparative batch confidence is low. Read rankings carefully before treating them as stable comparative evidence.";
    }

    private static string GetFailureDiagnosis(int failures, int totalRuns)
    {
        if (totalRuns <= 0)
            return "No comparative runs were produced for this batch.";

        if (failures == 0)
            return "No parser run failed completely. Comparative evidence is operationally clean.";

        if (failures <= Math.Max(1, totalRuns / 5))
            return "A small portion of comparative runs failed. The arena remains usable, but specific projects or strategies deserve review.";

        return "Failure volume is elevated. Arena conclusions should be interpreted with caution until the failing runs are inspected.";
    }

    private static string GetWinnerDiagnosis(IReadOnlyDictionary<string, int> winnerGroups)
    {
        if (winnerGroups.Count == 0)
            return "No winner distribution could be established for this run.";

        var top = winnerGroups
            .OrderByDescending(x => x.Value)
            .First();

        return $"{top.Key} won the largest number of projects in this arena, suggesting stronger comparative consistency across the evaluated scopes.";
    }

    private static string GetStrategyDiagnosis(StrategyAggregateMetric? metric)
    {
        if (metric == null)
            return "No aggregate strategy leader was identified in the current arena.";

        return $"{metric.StrategyName} currently leads the aggregate comparison with average score {metric.AverageScore:0.00} and average confidence {Clamp01(metric.AverageConfidence):P0}.";
    }

    private static string GetExecutionDiagnosis(double avgExecutionMs)
    {
        if (avgExecutionMs <= 25)
            return "Average comparative execution time is very lean, which favors repeated benchmarking.";

        if (avgExecutionMs <= 250)
            return "Average comparative execution time remains operationally healthy for iterative benchmarking.";

        return "Average comparative execution time is elevated. For repeated arena usage, performance optimization may become relevant.";
    }

    private static double Clamp01(double value)
        => Math.Max(0, Math.Min(1, value));

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

    private static string F(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed class StrategyAggregateMetric
    {
        public ParserStrategy Strategy { get; init; }
        public string StrategyName { get; init; } = "Unknown";
        public int RunCount { get; init; }
        public double AverageScore { get; init; }
        public double AverageConfidence { get; init; }
        public double AverageExecutionMs { get; init; }
        public double AverageFiles { get; init; }
        public double AverageTypes { get; init; }
        public double AverageReferences { get; init; }
        public int Failures { get; init; }
    }
}
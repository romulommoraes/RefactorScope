using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Exporters.Styling;
using System.Text;
using RefactorScope.Exporters.Dashboards.Renderers;

namespace RefactorScope.Exporters.Dashboards;

/// <summary>
/// Parsing Dashboard
///
/// Responsabilidade
/// ----------------
/// Renderizar o dashboard HTML do módulo de parsing, com foco em:
///
/// - telemetria executiva
/// - confiança heurística
/// - densidade estrutural
/// - sinais de risco
/// - visualização tipo "control room"
///
/// Diretriz desta versão
/// ---------------------
/// Esta versão remove os gauges e reorganiza o layout:
///
/// - radar e donut em 50/50
/// - sankey full width
/// - heat strip full width
/// - legenda do radar em leitura textual compacta
/// - paleta neon mais coerente com o theme
/// </summary>
public sealed class ParsingDashboardExporter
{
    public void Export(
        IParserResult result,
        string htmlPath,
        string themeFileName)
    {
        var html = GenerateHtml(result, themeFileName);
        File.WriteAllText(htmlPath, html, Encoding.UTF8);
    }

    private string GenerateHtml(
        IParserResult result,
        string themeFileName)
    {
        // =====================================================
        // 1. MÉTRICAS BASE
        // =====================================================

        var files = result.Model?.Arquivos.Count ?? 0;
        var types = result.Model?.Tipos.Count ?? 0;
        var refs = result.Model?.Referencias.Count ?? 0;

        double executionMs = result.Stats?.ExecutionTime.TotalMilliseconds ?? 0;

        double normalizedConfidence = Clamp01(result.Confidence);

        double typesPerFile = files == 0 ? 0 : types / (double)files;
        double refsPerType = types == 0 ? 0 : refs / (double)types;
        double msPerFile = files == 0 ? 0 : executionMs / files;
        double msPerType = types == 0 ? 0 : executionMs / types;

        long memoryBytes = TryGetMemoryBytes(result);
        double memoryPerFileKb = files == 0 ? 0 : (memoryBytes / 1024d) / files;
        double memoryPerTypeKb = types == 0 ? 0 : (memoryBytes / 1024d) / types;

        var parserConfidenceBand = GetConfidenceBand(normalizedConfidence);
        var sparseExtraction = IsSparseExtraction(refsPerType, typesPerFile);
        var anomalyDetected = TryGetAnomalyDetected(result);

        var extractionIndex = ComputeExtractionIndex(
            normalizedConfidence,
            refsPerType,
            typesPerFile,
            msPerType);

        var targetName = GetTargetName(result);

        // =====================================================
        // 2. HEURÍSTICAS EXPERIMENTAIS PARA CHARTS
        // =====================================================

        var routeMetrics = BuildExperimentalRouteMetrics(
            files,
            types,
            refs,
            normalizedConfidence,
            sparseExtraction,
            anomalyDetected,
            msPerType);

        // =====================================================
        // 3. RENDERERS
        // =====================================================

        var controlCharts = new ParsingControlChartsRendererP5();
        var sb = new StringBuilder();

        // =====================================================
        // 4. DOCUMENT START
        // =====================================================

        sb.AppendLine(DashboardHtmlShell.RenderDocumentStart(
            "Parsing Dashboard",
            themeFileName));

        // =====================================================
        // 5. TOPBAR
        // =====================================================

        sb.AppendLine($"""
<div class="topbar" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <div class="brand">
        <div class="brand-kicker">RefactorScope // Parsing Module</div>
        <h1>Parsing Dashboard</h1>
        <div class="subtitle">Parser: <b>{Html(result.ParserName)}</b></div>
    </div>

    <div class="run-meta">
        <div><b>Generated:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</div>
        <div><b>Target:</b> {Html(targetName)}</div>
        <div><b>Confidence:</b> {normalizedConfidence:P0}</div>
        <div><b>Confidence Band:</b> {Html(parserConfidenceBand)}</div>
    </div>
</div>
""");

        // =====================================================
        // 6. KPI GRID
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Parsing Signal Grid</h2>
        <div class="line"></div>
    </div>
</div>
""");

        sb.AppendLine($"""
<div class="grid-kpis parsing-kpis">
    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Parser</div>
        <div class="value" style="font-size:22px;">{Html(result.ParserName)}</div>
        <div class="hint">Parsing strategy used to extract structural data from the codebase</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Files</div>
        <div class="value">{files}</div>
        <div class="hint">Source files parsed into the structural model</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Types</div>
        <div class="value">{types}</div>
        <div class="hint">Distinct types extracted into the parsing model</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">References</div>
        <div class="value">{refs}</div>
        <div class="hint">Structural references extracted between parsed types</div>
    </div>

    <div class="kpi {(normalizedConfidence >= 0.85 ? "good" : normalizedConfidence >= 0.65 ? "warning" : "alert")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Confidence</div>
        <div class="value">{normalizedConfidence:P0}</div>
        <div class="hint">Heuristic confidence that the parsed model is structurally usable</div>
    </div>

    <div class="kpi {(normalizedConfidence >= 0.85 ? "good" : normalizedConfidence >= 0.65 ? "warning" : "alert")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Confidence Band</div>
        <div class="value" style="font-size:24px;">{Html(parserConfidenceBand)}</div>
        <div class="hint">High, Medium or Low confidence band derived from parser confidence</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Execution</div>
        <div class="value">{executionMs:0} ms</div>
        <div class="hint">Total parsing execution time for the analyzed scope</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Type Density</div>
        <div class="value">{typesPerFile:0.00}</div>
        <div class="hint">Average number of extracted types per parsed file</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">Refs / Type</div>
        <div class="value">{refsPerType:0.00}</div>
        <div class="hint">Average extracted reference density per parsed type</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">ms / File</div>
        <div class="value">{msPerFile:0.00}</div>
        <div class="hint">Average parsing cost per file</div>
    </div>

    <div class="kpi" augmented-ui="tr-clip bl-clip border">
        <div class="label">ms / Type</div>
        <div class="value">{msPerType:0.00}</div>
        <div class="hint">Average parsing cost per extracted type</div>
    </div>

    <div class="kpi {(sparseExtraction ? "warning" : "good")}" augmented-ui="tr-clip bl-clip border">
        <div class="label">Sparse Extraction</div>
        <div class="value" style="font-size:24px;">{(sparseExtraction ? "Yes" : "No")}</div>
        <div class="hint">Indicates extraction density lower than expected for a structurally healthy parse</div>
    </div>
</div>
""");

        // =====================================================
        // 7. EXTENDED STATS + EXECUTIVE READING
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Extended Parsing Statistics</h2>
        <div class="line"></div>
    </div>
    <div class="dual">
""");

        sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Telemetry Summary</h3>
    <ul class="clean">
        <li><b>Files:</b> {files}</li>
        <li><b>Types:</b> {types}</li>
        <li><b>References:</b> {refs}</li>
        <li><b>Execution Time:</b> {executionMs:0.00} ms</li>
        <li><b>Type Density:</b> {typesPerFile:0.00}</li>
        <li><b>References per Type:</b> {refsPerType:0.00}</li>
        <li><b>Milliseconds per File:</b> {msPerFile:0.00}</li>
        <li><b>Milliseconds per Type:</b> {msPerType:0.00}</li>
        <li><b>Memory per File:</b> {memoryPerFileKb:0.00} KB</li>
        <li><b>Memory per Type:</b> {memoryPerTypeKb:0.00} KB</li>
        <li><b>Extraction Index:</b> {extractionIndex:0.00}</li>
        <li><b>Anomaly Detected:</b> {(anomalyDetected ? "Yes" : "No")}</li>
    </ul>
</div>
""");

        sb.AppendLine($"""
<div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
    <h3>Executive Reading</h3>
    <ul class="clean">
        <li>{Html(GetConfidenceDiagnosis(normalizedConfidence))}</li>
        <li>{Html(GetDensityDiagnosis(refsPerType, typesPerFile))}</li>
        <li>{Html(GetPerformanceDiagnosis(msPerType, msPerFile))}</li>
        <li>{Html(GetSparseExtractionDiagnosis(sparseExtraction))}</li>
        <li>{Html(GetAnomalyDiagnosis(anomalyDetected))}</li>
    </ul>

    <div style="margin-top:16px;">
        <div class="badge {(normalizedConfidence >= 0.85 ? "green" : normalizedConfidence >= 0.65 ? "amber" : "red")}"><span class="badge-dot"></span> Confidence: {normalizedConfidence:P0}</div>
        <div class="badge {(sparseExtraction ? "amber" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Sparse Extraction: {(sparseExtraction ? "Yes" : "No")}</div>
        <div class="badge {(anomalyDetected ? "red" : "green")}" style="margin-top:8px;"><span class="badge-dot"></span> Anomaly Detected: {(anomalyDetected ? "Yes" : "No")}</div>
    </div>
</div>
""");

        sb.AppendLine("</div></div>");

        // =====================================================
        // 8. CHARTS & DIAGNOSTICS (FIXED 3-COLUMN GRID)
        // =====================================================
        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Charts & Diagnostics</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("<div style='display:grid; grid-template-columns: auto auto 1fr; gap:24px; align-items:stretch; width:100%;'>");

        sb.AppendLine(controlCharts.RenderParsingRadarEnhanced(typesPerFile, refsPerType, normalizedConfidence, msPerType));

        sb.AppendLine(controlCharts.RenderParsingRouteDonut(
            routeMetrics.SafeFiles,
            routeMetrics.ComplexFiles,
            routeMetrics.AstPatternFiles,
            routeMetrics.RecoveryFiles,
            routeMetrics.RiskFiles));

        sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border' style='margin:0; background:rgba(255,255,255,0.02); min-width:320px; display:flex; flex-direction:column; justify-content:center;'>");
        sb.AppendLine("<ul class='clean' style='font-size:12px; line-height:1.6;'>");
        sb.AppendLine("<li><b>Safe:</b> Files likely handled in the most stable parsing route.</li>");
        sb.AppendLine("<li><b>Complex:</b> Files requiring more structural interpretation effort.</li>");
        sb.AppendLine("<li><b>AST / Pattern:</b> Files dependent on structural pattern handling.</li>");
        sb.AppendLine("<li><b>Recovery:</b> Files requiring fallback or recovery behavior.</li>");
        sb.AppendLine("<li><b>Risk:</b> Concentration in highest reliability-pressure bucket.</li>");
        sb.AppendLine($"<li style='margin-top:12px; padding-top:12px; border-top:1px solid rgba(255,255,255,0.1); color:#ff9a3c;'>{Html(GetRouteDiagnosis(routeMetrics))}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        sb.AppendLine("</div></div>");

        // =====================================================
        // 9. SANKEY FULL WIDTH
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Parsing Flow</h2>
        <div class="line"></div>
    </div>
    <div class="panel wide-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine(controlCharts.RenderParsingFlowSankey(
            files,
            routeMetrics.SafeFiles,
            routeMetrics.ComplexFiles,
            routeMetrics.AstPatternFiles,
            routeMetrics.RecoveryFiles,
            types,
            refs,
            routeMetrics.RiskFiles));

        sb.AppendLine("</div></div>");

        // =====================================================
        // 10. HEAT STRIP FULL WIDTH
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Risk Heat</h2>
        <div class="line"></div>
    </div>
    <div class="panel wide-panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine(controlCharts.RenderRiskHeatStrip(
            routeMetrics.SafeFiles,
            routeMetrics.ModerateRiskFiles,
            routeMetrics.SparseFiles,
            routeMetrics.AnomalyFiles,
            routeMetrics.RecoveryDependentFiles));

        sb.AppendLine("</div></div>");

        // =====================================================
        // 12. PARSED TYPES TABLE
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Parsed Types</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

        sb.AppendLine("""
<div style="margin-bottom:16px;">
    <input id="search"
           type="text"
           onkeyup="searchTable()"
           placeholder="Search type, namespace, file..."
           style="width:100%; padding:12px; border-radius:10px; border:1px solid rgba(255,255,255,0.14); background:rgba(255,255,255,0.03); color:#fff;" />
</div>
""");

        sb.AppendLine("""
<div class="table-shell">
<table id="table" class="link-table parsing-table">
    <thead>
        <tr>
            <th onclick="sortTable(0)">Type</th>
            <th onclick="sortTable(1)">Namespace</th>
            <th onclick="sortTable(2)">File</th>
            <th onclick="sortTable(3)">References</th>
        </tr>
    </thead>
    <tbody>
""");

        foreach (var type in result.Model?.Tipos ?? Enumerable.Empty<TipoInfo>())
        {
            var refsCount = result.Model?.Referencias?.Count(r => r.FromType == type.Name) ?? 0;

            sb.AppendLine($"""
<tr>
    <td>{Html(type.Name)}</td>
    <td>{Html(type.Namespace)}</td>
    <td>{Html(type.DeclaredInFile)}</td>
    <td>{refsCount}</td>
</tr>
""");
        }

        sb.AppendLine("""
    </tbody>
</table>
</div>
</div>
""");

        // =====================================================
        // 13. METHODOLOGY
        // =====================================================

        sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Methodology & Interpretation</h2>
        <div class="line"></div>
    </div>
    <div class="panel" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
        <ul class="clean">
            <li>This dashboard describes parser output quality, extraction density and route-distribution heuristics.</li>
            <li>Confidence is heuristic and should be interpreted as structural usability, not absolute correctness.</li>
            <li>Sankey, donut and route heat visuals are currently fed by experimental route estimation until per-route parsing telemetry becomes explicit.</li>
            <li>Low reference density may suggest sparse extraction, but does not prove parser failure.</li>
            <li>Anomaly flags indicate atypical parse characteristics that deserve review.</li>
            <li>Performance ratios help compare parser strategies across projects and baselines.</li>
            <li>Type Density means average extracted types per parsed file. Lower values may reflect lean structure or conservative extraction.</li>
        </ul>
    </div>
</div>
""");

        // =====================================================
        // 14. LOCAL STYLES + SCRIPT
        // =====================================================

        sb.AppendLine("""
<style>
.grid-kpis.parsing-kpis {
    display: grid;
    grid-template-columns: repeat(6, minmax(0, 1fr));
    gap: 16px;
}

.compact-panel,
.wide-panel {
    overflow: hidden;
    min-width: 0;
}

.chart-container {
    min-width: 0;
    flex: 0 0 auto;
}

@media (max-width: 1080px) {
    .chart-container {
        width: 100%;
    }
}

.wide-panel {
    width: 100%;
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
    .grid-kpis.parsing-kpis {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
}

@media (max-width: 1080px) {
    .parsing-half-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 980px) {
    .grid-kpis.parsing-kpis {
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
function searchTable() {
  var input = document.getElementById('search');
  var filter = input.value.toLowerCase();
  var table = document.getElementById('table');
  var rows = table.getElementsByTagName('tr');

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

      if (!x || !y) continue;

      if (dir === 'asc' && x.innerText.toLowerCase() > y.innerText.toLowerCase()) shouldSwitch = true;
      if (dir === 'desc' && x.innerText.toLowerCase() < y.innerText.toLowerCase()) shouldSwitch = true;

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

        // =====================================================
        // 15. DOCUMENT END
        // =====================================================

        sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
            "Generated by RefactorScope Parsing Dashboard // Shared Hub Layout"));

        return sb.ToString();
    }

    private static double ComputeExtractionIndex(
        double confidence,
        double refsPerType,
        double typesPerFile,
        double msPerType)
    {
        var safeConfidence = Clamp01(confidence);

        var densityScore = Math.Min(1.0, refsPerType / 3.0);
        var structureScore = Math.Min(1.0, typesPerFile / 4.0);
        var performanceScore = msPerType <= 0
            ? 1.0
            : Math.Max(0, 1.0 - Math.Min(1.0, msPerType / 50.0));

        return ((safeConfidence * 0.5) + (densityScore * 0.25) + (structureScore * 0.15) + (performanceScore * 0.10)) * 100.0;
    }

    private static bool IsSparseExtraction(double refsPerType, double typesPerFile)
        => refsPerType < 0.80 || typesPerFile < 0.50;

    private static string GetConfidenceBand(double confidence)
    {
        var safeConfidence = Clamp01(confidence);

        if (safeConfidence >= 0.85) return "High";
        if (safeConfidence >= 0.65) return "Medium";
        return "Low";
    }

    private static RouteMetrics BuildExperimentalRouteMetrics(
        int files,
        int types,
        int refs,
        double confidence,
        bool sparseExtraction,
        bool anomalyDetected,
        double msPerType)
    {
        if (files <= 0)
            return new RouteMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0);

        var safeConfidence = Clamp01(confidence);

        var safeRatio = safeConfidence >= 0.85 ? 0.62 : safeConfidence >= 0.65 ? 0.48 : 0.32;
        var complexRatio = safeConfidence >= 0.85 ? 0.18 : safeConfidence >= 0.65 ? 0.24 : 0.28;
        var astRatio = types > 0 && refs > 0 ? 0.12 : 0.08;
        var recoveryRatio = anomalyDetected ? 0.12 : 0.08;

        if (sparseExtraction)
        {
            safeRatio -= 0.10;
            complexRatio += 0.04;
            astRatio += 0.03;
            recoveryRatio += 0.03;
        }

        if (msPerType > 25)
        {
            safeRatio -= 0.04;
            complexRatio += 0.02;
            recoveryRatio += 0.02;
        }

        safeRatio = Math.Max(0.15, safeRatio);
        complexRatio = Math.Max(0.10, complexRatio);
        astRatio = Math.Max(0.05, astRatio);
        recoveryRatio = Math.Max(0.03, recoveryRatio);

        var totalRatio = safeRatio + complexRatio + astRatio + recoveryRatio;

        safeRatio /= totalRatio;
        complexRatio /= totalRatio;
        astRatio /= totalRatio;
        recoveryRatio /= totalRatio;

        var safeFiles = (int)Math.Round(files * safeRatio);
        var complexFiles = (int)Math.Round(files * complexRatio);
        var astPatternFiles = (int)Math.Round(files * astRatio);

        var usedSoFar = safeFiles + complexFiles + astPatternFiles;
        var recoveryFiles = Math.Max(0, files - usedSoFar);

        var riskFiles = 0;
        if (safeConfidence < 0.65) riskFiles += Math.Max(1, files / 6);
        if (sparseExtraction) riskFiles += Math.Max(1, files / 8);
        if (anomalyDetected) riskFiles += Math.Max(1, files / 10);

        riskFiles = Math.Min(files, riskFiles);

        var moderateRiskFiles = Math.Min(files, Math.Max(1, complexFiles / 2));
        var sparseFiles = sparseExtraction ? Math.Max(1, files / 5) : Math.Max(0, files / 20);
        var anomalyFiles = anomalyDetected ? Math.Max(1, files / 8) : 0;
        var recoveryDependentFiles = Math.Max(0, recoveryFiles / 2);

        return new RouteMetrics(
            SafeFiles: safeFiles,
            ComplexFiles: complexFiles,
            AstPatternFiles: astPatternFiles,
            RecoveryFiles: recoveryFiles,
            RiskFiles: riskFiles,
            ModerateRiskFiles: moderateRiskFiles,
            SparseFiles: sparseFiles,
            AnomalyFiles: anomalyFiles,
            RecoveryDependentFiles: recoveryDependentFiles);
    }

    private static bool TryGetAnomalyDetected(IParserResult result)
    {
        try
        {
            var stats = result.Stats;
            if (stats == null)
                return false;

            var prop = stats.GetType().GetProperty("AnomalyDetected");
            if (prop == null)
                return false;

            var value = prop.GetValue(stats);
            return value is bool flag && flag;
        }
        catch
        {
            return false;
        }
    }

    private static string GetRouteDiagnosis(RouteMetrics routeMetrics)
    {
        var total = routeMetrics.SafeFiles
            + routeMetrics.ComplexFiles
            + routeMetrics.AstPatternFiles
            + routeMetrics.RecoveryFiles;

        if (total <= 0)
            return "No route distribution data was produced for this execution.";

        var safeRatio = routeMetrics.SafeFiles / (double)total;
        var recoveryRatio = routeMetrics.RecoveryFiles / (double)total;
        var riskRatio = routeMetrics.RiskFiles / (double)Math.Max(1, total);

        if (safeRatio >= 0.60 && riskRatio <= 0.10)
            return "Route distribution looks operationally healthy. Most files remained in stable parsing paths.";

        if (recoveryRatio >= 0.20 || riskRatio >= 0.15)
            return "Route distribution suggests elevated fallback or recovery pressure. Inspect parser behavior in more detail.";

        return "Route distribution is mixed. The execution appears usable, but some files required more complex handling paths.";
    }

    private static long TryGetMemoryBytes(IParserResult result)
    {
        try
        {
            var stats = result.Stats;
            if (stats == null)
                return 0;

            var prop = stats.GetType().GetProperty("MemoryBytes")
                      ?? stats.GetType().GetProperty("AllocatedBytes")
                      ?? stats.GetType().GetProperty("MemoryUsageBytes");

            if (prop == null)
                return 0;

            var value = prop.GetValue(stats);
            return value switch
            {
                long l => l,
                int i => i,
                double d => (long)d,
                float f => (long)f,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    private static string GetTargetName(IParserResult result)
    {
        try
        {
            var model = result.Model;
            if (model == null || model.Arquivos == null || !model.Arquivos.Any())
                return "Unknown Scope";

            var firstFile = model.Arquivos.FirstOrDefault();
            if (firstFile == null)
                return "Unknown Scope";

            var filePath = TryGetArquivoPath(firstFile);
            if (string.IsNullOrWhiteSpace(filePath))
                return "Unknown Scope";

            var normalized = filePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var parts = normalized.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0 ? parts[0] : "Unknown Scope";
        }
        catch
        {
            return "Unknown Scope";
        }
    }

    private static string? TryGetArquivoPath(ArquivoInfo arquivo)
    {
        try
        {
            var type = arquivo.GetType();

            var prop =
                type.GetProperty("Path")
                ?? type.GetProperty("FilePath")
                ?? type.GetProperty("FullPath")
                ?? type.GetProperty("RelativePath")
                ?? type.GetProperty("Nome")
                ?? type.GetProperty("Name");

            return prop?.GetValue(arquivo)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string GetConfidenceDiagnosis(double confidence)
    {
        var safeConfidence = Clamp01(confidence);

        if (safeConfidence >= 0.85)
            return "Parser confidence is high. The extracted structural model appears reliable for downstream analysis.";

        if (safeConfidence >= 0.65)
            return "Parser confidence is moderate. The extracted model appears usable, but some structural loss may exist.";

        return "Parser confidence is low. Review extraction quality before relying on downstream architectural conclusions.";
    }

    private static string GetDensityDiagnosis(double refsPerType, double typesPerFile)
    {
        if (refsPerType < 0.80)
            return "Reference density is low. The parse may still be structurally sparse, or the target codebase may expose fewer explicit dependencies.";

        if (typesPerFile < 0.50)
            return "Type density per file is low. This may indicate sparse source structure or partial extraction coverage.";

        return "Structural density appears healthy. Parsed types and references show consistent extraction volume.";
    }

    private static string GetPerformanceDiagnosis(double msPerType, double msPerFile)
    {
        if (msPerType > 25 || msPerFile > 80)
            return "Parsing cost is elevated for this scope. Compare against other parser strategies if performance becomes relevant.";

        return "Parsing cost remains in an acceptable range for the extracted structural volume.";
    }

    private static string GetSparseExtractionDiagnosis(bool sparseExtraction)
        => sparseExtraction
            ? "Sparse extraction was detected. This does not prove parser failure, but the structural graph may be thinner than expected."
            : "No sparse extraction signal was detected. Structural density appears compatible with a healthy parse.";

    private static string GetAnomalyDiagnosis(bool anomalyDetected)
        => anomalyDetected
            ? "An anomaly flag was raised during parsing. Review this execution before treating the extracted model as fully representative."
            : "No anomaly flag was raised during parsing telemetry.";

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

    private sealed record RouteMetrics(
        int SafeFiles,
        int ComplexFiles,
        int AstPatternFiles,
        int RecoveryFiles,
        int RiskFiles,
        int ModerateRiskFiles,
        int SparseFiles,
        int AnomalyFiles,
        int RecoveryDependentFiles);
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Arena;
using RefactorScope.Core.Parsing.Enum;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards.Renderers;

public sealed class ParserArenaControlChartsRendererP5
{
    private const string BgColor = "#0b1020";
    private const string AccentPrimary = "#ff9a3c";
    private const string SemanticSafe = "#7fd36b";
    private const string SemanticComplex = "#6ea8ff";
    private const string SemanticPattern = "#ffc15d";
    private const string SemanticRecovery = "#ff9a52";
    private const string SemanticRisk = "#ff6a3d";
    private const string TextMain = "#eef4ff";
    private const string TextMuted = "#8fa8ff";

    private static string F(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    // =====================================================
    // 1. STRATEGY PERFORMANCE SCATTER
    // X = confidence
    // Y = comparative score
    // size = avg types
    // =====================================================
    public string RenderStrategyPerformanceScatter(
     IReadOnlyList<StrategyVisualMetric> metrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:linear-gradient(180deg, rgba(10,18,34,0.88), rgba(6,12,24,0.94));border-radius:18px;border:1px solid rgba(120,180,255,0.10);box-shadow:inset 0 0 30px rgba(80,140,255,0.05), 0 0 22px rgba(0,180,255,0.06);min-height:460px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Strategy Performance Scatter</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Confidence × score, with bubble size representing average extracted types.</div>""");
        sb.AppendLine("""<div id="arena-performance-scatter-container" style="display:flex;justify-content:center;width:100%;"></div>""");

        var pointsJs = string.Join(",\n", metrics.Select(m => $$"""
{
    name: "{{EscapeJs(m.StrategyName)}}",
    color: "{{m.Color}}",
    confidence: {{F(Clamp01(m.AverageConfidence) * 100)}},
    score: {{F(m.AverageScore)}},
    types: {{F(m.AverageTypes)}}
}
"""));

        sb.AppendLine($$"""
<script>
const arenaPerformanceScatterSketch = (p) => {
    const points = [
        {{pointsJs}}
    ];

    const chart = { x: 76, y: 40, w: 420, h: 250 };

    function extent(values, fallbackMin, fallbackMax) {
        if (!values.length) return [fallbackMin, fallbackMax];
        let min = Math.min(...values);
        let max = Math.max(...values);

        if (min === max) {
            min -= 5;
            max += 5;
        }

        const pad = Math.max((max - min) * 0.18, 2);
        return [min - pad, max + pad];
    }

    let [minX, maxX] = extent(points.map(p => p.confidence), 0, 100);
    let [minY, maxY] = extent(points.map(p => p.score), 0, 100);

    minX = Math.max(0, minX);
    maxX = Math.min(100, maxX);

    p.setup = () => {
        let canvas = p.createCanvas(580, 360);
        canvas.parent('arena-performance-scatter-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();

        // background zones
        p.noStroke();
        p.fill('rgba(50,120,255,0.06)');
        p.rect(chart.x, chart.y + chart.h * 0.50, chart.w * 0.50, chart.h * 0.50, 10);

        p.fill('rgba(255,160,60,0.05)');
        p.rect(chart.x + chart.w * 0.50, chart.y, chart.w * 0.50, chart.h * 0.50, 10);

        p.stroke('rgba(255,255,255,0.08)');
        p.strokeWeight(1);

        for (let i = 0; i <= 5; i++) {
            let x = chart.x + (chart.w / 5) * i;
            let y = chart.y + (chart.h / 5) * i;
            p.line(x, chart.y, x, chart.y + chart.h);
            p.line(chart.x, y, chart.x + chart.w, y);
        }

        p.stroke('rgba(180,210,255,0.18)');
        p.line(chart.x, chart.y + chart.h, chart.x + chart.w, chart.y + chart.h);
        p.line(chart.x, chart.y, chart.x, chart.y + chart.h);

        p.noStroke();
        p.fill('#d9e7ff');
        p.textSize(11);

        for (let i = 0; i <= 5; i++) {
            let xv = minX + ((maxX - minX) / 5) * i;
            let yv = maxY - ((maxY - minY) / 5) * i;

            p.textAlign(p.CENTER, p.TOP);
            p.text(xv.toFixed(0), chart.x + (chart.w / 5) * i, chart.y + chart.h + 8);

            p.textAlign(p.RIGHT, p.CENTER);
            p.text(yv.toFixed(1), chart.x - 10, chart.y + (chart.h / 5) * i);
        }

        p.push();
        p.translate(18, chart.y + chart.h / 2);
        p.rotate(-Math.PI / 2);
        p.textAlign(p.CENTER, p.CENTER);
        p.text("Average Score", 0, 0);
        p.pop();

        p.textAlign(p.CENTER, p.CENTER);
        p.text("Average Confidence", chart.x + chart.w / 2, chart.y + chart.h + 34);

        let hovered = null;

        for (let i = 0; i < points.length; i++) {
            const pt = points[i];
            const x = p.map(pt.confidence, minX, maxX, chart.x, chart.x + chart.w);
            const y = p.map(pt.score, minY, maxY, chart.y + chart.h, chart.y);
            const r = p.map(pt.types, 0, Math.max(...points.map(z => z.types), 1), 18, 34);

            const d = p.dist(p.mouseX, p.mouseY, x, y);
            if (d <= r * 0.5) hovered = { ...pt, x, y, r };

            p.drawingContext.shadowColor = pt.color;
            p.drawingContext.shadowBlur = hovered?.name === pt.name ? 24 : 14;

            p.fill(pt.color + '55');
            p.stroke(pt.color);
            p.strokeWeight(hovered?.name === pt.name ? 2.8 : 1.6);
            p.ellipse(x, y, r, r);

            p.drawingContext.shadowBlur = 0;
            p.fill('#eef4ff');
            p.noStroke();
            p.textSize(10);
            p.textAlign(p.CENTER, p.CENTER);
            p.text(pt.name, x, y - r * 0.75);
        }

        if (hovered) {
            const boxW = 170;
            const boxH = 74;
            let tx = hovered.x + 18;
            let ty = hovered.y - 14;

            if (tx + boxW > p.width - 8) tx = hovered.x - boxW - 18;
            if (ty + boxH > p.height - 8) ty = p.height - boxH - 8;
            if (ty < 8) ty = 8;

            p.drawingContext.shadowColor = hovered.color;
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,14,28,0.96)');
            p.stroke(hovered.color);
            p.strokeWeight(1.2);
            p.rect(tx, ty, boxW, boxH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f2f6ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.text(hovered.name, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Confidence: ${hovered.confidence.toFixed(1)}%`, tx + 12, ty + 28);
            p.text(`Score: ${hovered.score.toFixed(2)}`, tx + 12, ty + 42);
            p.text(`Avg Types: ${hovered.types.toFixed(1)}`, tx + 12, ty + 56);
        }
    };
};
new p5(arenaPerformanceScatterSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 2. WINNER DONUT
    // =====================================================
    public string RenderWinnerDonut(
        IReadOnlyDictionary<string, int> winnerGroups)
    {
        var regex = winnerGroups.TryGetValue("Regex", out var r) ? r : 0;
        var selective = winnerGroups.TryGetValue("Selective", out var s) ? s : 0;
        var adaptive = winnerGroups.TryGetValue("Adaptive", out var a) ? a : 0;
        var incremental = winnerGroups.TryGetValue("Incremental", out var i) ? i : 0;
        var total = Math.Max(1, regex + selective + adaptive + incremental);

        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:460px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Winner Distribution</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">How often each strategy won across the batch projects.</div>""");
        sb.AppendLine("""<div id="arena-winner-donut-container" style="display:flex;justify-content:center;width:100%;"></div>""");

        sb.AppendLine($$"""
<script>
const arenaWinnerDonutSketch = (p) => {
    const data = [
        { label: "Regex", value: {{regex}}, color: "#ff9a3c" },
        { label: "Selective", value: {{selective}}, color: "#7fd36b" },
        { label: "Adaptive", value: {{adaptive}}, color: "#6ea8ff" },
        { label: "Incremental", value: {{incremental}}, color: "#ffc15d" }
    ];
    const total = {{total}};

    p.setup = () => {
        let canvas = p.createCanvas(390, 390);
        canvas.parent('arena-winner-donut-container');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        p.translate(p.width / 2, p.height / 2 - 18);

        const outerRadius = 105;
        const innerRadius = 72;

        p.noFill();
        p.strokeWeight(1.5);
        p.stroke('rgba(255,255,255,0.08)');

        p.push();
        p.rotate(p.frameCount * 0.25);
        p.drawingContext.setLineDash([8, 10]);
        p.ellipse(0, 0, outerRadius * 2 + 26);
        p.pop();

        p.push();
        p.rotate(-p.frameCount * 0.45);
        p.drawingContext.setLineDash([3, 7]);
        p.ellipse(0, 0, innerRadius * 2 - 20);
        p.pop();

        p.drawingContext.setLineDash([]);

        let currentAngle = -90;

        data.forEach(item => {
            if (item.value <= 0) return;

            const angleSpan = (item.value / total) * 360;
            const gap = 3;

            p.drawingContext.shadowColor = item.color;
            p.drawingContext.shadowBlur = 16;
            p.stroke(item.color);
            p.strokeWeight(28);

            if (angleSpan > gap) {
                p.arc(
                    0, 0,
                    outerRadius + innerRadius,
                    outerRadius + innerRadius,
                    currentAngle + gap / 2,
                    currentAngle + angleSpan - gap / 2
                );
            }

            currentAngle += angleSpan;
        });

        p.drawingContext.shadowBlur = 0;
        p.noStroke();
        p.fill('{{TextMain}}');
        p.textAlign(p.CENTER, p.CENTER);
        p.textSize(38);
        p.textStyle(p.BOLD);
        p.text(total, 0, -4);

        p.fill('{{TextMuted}}');
        p.textSize(11);
        p.textStyle(p.NORMAL);
        p.text("Project Winners", 0, 22);

        const legendStartX = -145;
        const legendY = 160;
        p.textAlign(p.LEFT, p.CENTER);

        data.forEach((item, index) => {
            const x = legendStartX + (index % 2) * 145;
            const y = legendY + Math.floor(index / 2) * 24;

            p.drawingContext.shadowColor = item.color;
            p.drawingContext.shadowBlur = 8;
            p.fill(item.color);
            p.rect(x, y - 5, 9, 9, 2);

            p.drawingContext.shadowBlur = 0;
            p.fill('{{TextMain}}');
            p.textSize(10);
            p.text(`${item.label} (${item.value})`, x + 15, y);
        });
    };
};
new p5(arenaWinnerDonutSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 3. EFFICIENCY SCATTER
    // X = execution time
    // Y = confidence
    // size = avg references
    // =====================================================
    public string RenderEfficiencyScatter(
        IReadOnlyList<StrategyVisualMetric> metrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:linear-gradient(180deg, rgba(10,18,34,0.88), rgba(6,12,24,0.94));border-radius:18px;border:1px solid rgba(120,180,255,0.10);box-shadow:inset 0 0 30px rgba(80,140,255,0.05), 0 0 22px rgba(0,180,255,0.06);min-height:460px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Efficiency Scatter</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Execution cost × confidence, with bubble size representing average extracted references.</div>""");
        sb.AppendLine("""<div id="arena-efficiency-scatter-container" style="display:flex;justify-content:center;width:100%;"></div>""");

        var pointsJs = string.Join(",\n", metrics.Select(m => $$"""
{
    name: "{{EscapeJs(m.StrategyName)}}",
    color: "{{m.Color}}",
    cost: {{F(m.AverageExecutionMs)}},
    confidence: {{F(Clamp01(m.AverageConfidence) * 100)}},
    refs: {{F(m.AverageReferences)}}
}
"""));

        sb.AppendLine($$"""
<script>
const arenaEfficiencyScatterSketch = (p) => {
    const points = [
        {{pointsJs}}
    ];

    const chart = { x: 76, y: 40, w: 420, h: 250 };

    function extent(values, fallbackMin, fallbackMax) {
        if (!values.length) return [fallbackMin, fallbackMax];
        let min = Math.min(...values);
        let max = Math.max(...values);

        if (min === max) {
            min -= 5;
            max += 5;
        }

        const pad = Math.max((max - min) * 0.18, 2);
        return [min - pad, max + pad];
    }

    let [minX, maxX] = extent(points.map(p => p.cost), 0, 100);
    let [minY, maxY] = extent(points.map(p => p.confidence), 0, 100);

    minX = Math.max(0, minX);
    minY = Math.max(0, minY);
    maxY = Math.min(100, maxY);

    p.setup = () => {
        let canvas = p.createCanvas(580, 360);
        canvas.parent('arena-efficiency-scatter-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();

        p.noStroke();
        p.fill('rgba(80,220,160,0.05)');
        p.rect(chart.x, chart.y, chart.w * 0.45, chart.h * 0.45, 10);

        p.fill('rgba(255,80,80,0.05)');
        p.rect(chart.x + chart.w * 0.55, chart.y + chart.h * 0.55, chart.w * 0.45, chart.h * 0.45, 10);

        p.stroke('rgba(255,255,255,0.08)');
        p.strokeWeight(1);

        for (let i = 0; i <= 5; i++) {
            let x = chart.x + (chart.w / 5) * i;
            let y = chart.y + (chart.h / 5) * i;
            p.line(x, chart.y, x, chart.y + chart.h);
            p.line(chart.x, y, chart.x + chart.w, y);
        }

        p.stroke('rgba(180,210,255,0.18)');
        p.line(chart.x, chart.y + chart.h, chart.x + chart.w, chart.y + chart.h);
        p.line(chart.x, chart.y, chart.x, chart.y + chart.h);

        p.noStroke();
        p.fill('#d9e7ff');
        p.textSize(11);

        for (let i = 0; i <= 5; i++) {
            let xv = minX + ((maxX - minX) / 5) * i;
            let yv = maxY - ((maxY - minY) / 5) * i;

            p.textAlign(p.CENTER, p.TOP);
            p.text(xv.toFixed(0), chart.x + (chart.w / 5) * i, chart.y + chart.h + 8);

            p.textAlign(p.RIGHT, p.CENTER);
            p.text(yv.toFixed(0), chart.x - 10, chart.y + (chart.h / 5) * i);
        }

        p.push();
        p.translate(18, chart.y + chart.h / 2);
        p.rotate(-Math.PI / 2);
        p.textAlign(p.CENTER, p.CENTER);
        p.text("Average Confidence", 0, 0);
        p.pop();

        p.textAlign(p.CENTER, p.CENTER);
        p.text("Relative Execution Cost", chart.x + chart.w / 2, chart.y + chart.h + 34);

        let hovered = null;

        for (let i = 0; i < points.length; i++) {
            const pt = points[i];
            const x = p.map(pt.cost, minX, maxX, chart.x, chart.x + chart.w);
            const y = p.map(pt.confidence, minY, maxY, chart.y + chart.h, chart.y);
            const r = p.map(pt.refs, 0, Math.max(...points.map(z => z.refs), 1), 18, 34);

            const d = p.dist(p.mouseX, p.mouseY, x, y);
            if (d <= r * 0.5) hovered = { ...pt, x, y, r };

            p.drawingContext.shadowColor = pt.color;
            p.drawingContext.shadowBlur = hovered?.name === pt.name ? 24 : 14;

            p.fill(pt.color + '55');
            p.stroke(pt.color);
            p.strokeWeight(hovered?.name === pt.name ? 2.8 : 1.6);
            p.ellipse(x, y, r, r);

            p.drawingContext.shadowBlur = 0;
            p.fill('#eef4ff');
            p.noStroke();
            p.textSize(10);
            p.textAlign(p.CENTER, p.CENTER);
            p.text(pt.name, x, y - r * 0.75);
        }

        if (hovered) {
            const boxW = 176;
            const boxH = 74;
            let tx = hovered.x + 18;
            let ty = hovered.y - 14;

            if (tx + boxW > p.width - 8) tx = hovered.x - boxW - 18;
            if (ty + boxH > p.height - 8) ty = p.height - boxH - 8;
            if (ty < 8) ty = 8;

            p.drawingContext.shadowColor = hovered.color;
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,14,28,0.96)');
            p.stroke(hovered.color);
            p.strokeWeight(1.2);
            p.rect(tx, ty, boxW, boxH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f2f6ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.text(hovered.name, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Exec Cost: ${hovered.cost.toFixed(1)} ms`, tx + 12, ty + 28);
            p.text(`Confidence: ${hovered.confidence.toFixed(1)}%`, tx + 12, ty + 42);
            p.text(`Avg Refs: ${hovered.refs.toFixed(1)}`, tx + 12, ty + 56);
        }
    };
};
new p5(arenaEfficiencyScatterSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 4. PROJECT X STRATEGY HEATMAP
    // =====================================================
    public string RenderProjectStrategyHeatmap(
        IReadOnlyList<ParserArenaProjectResult> results)
    {
        var sb = new StringBuilder();

        var strategies = new[]
        {
        ParserStrategy.RegexFast,
        ParserStrategy.Selective,
        ParserStrategy.AdaptiveExperimental,
        ParserStrategy.IncrementalExperimental
    };

        var projectNames = results.Select(r => r.ProjectName).ToList();

        var cells = new List<string>();
        var allScores = new List<double>();

        foreach (var project in results)
        {
            foreach (var strategy in strategies)
            {
                var run = project.Runs.FirstOrDefault(r => r.Strategy == strategy);
                var score = run?.ComparativeScore ?? 0;
                allScores.Add(score);
            }
        }

        var minScore = allScores.Count == 0 ? 0 : allScores.Min();
        var maxScore = allScores.Count == 0 ? 1 : allScores.Max();

        if (Math.Abs(maxScore - minScore) < 0.0001)
            maxScore = minScore + 1;

        foreach (var project in results)
        {
            foreach (var strategy in strategies)
            {
                var run = project.Runs.FirstOrDefault(r => r.Strategy == strategy);

                cells.Add($$"""
{
    project: "{{EscapeJs(project.ProjectName)}}",
    strategy: "{{EscapeJs(ShortenStrategyName(strategy))}}",
    score: {{F(run?.ComparativeScore ?? 0)}},
    confidence: {{F(run?.Confidence ?? 0)}},
    types: {{run?.TypeCount ?? 0}},
    refs: {{run?.ReferenceCount ?? 0}}
}
""");
            }
        }

        var strategiesJs = string.Join(", ", strategies.Select(s => $"\"{ShortenStrategyName(s)}\""));
        var projectsJs = string.Join(", ", projectNames.Select(p => $"\"{EscapeJs(p)}\""));
        var cellsJs = string.Join(",\n", cells);

        sb.AppendLine("""
<div class="chart-container"
     style="display:flex;flex-direction:column;align-items:center;padding:24px;background:linear-gradient(180deg, rgba(8,16,34,0.96), rgba(5,10,20,0.98));border-radius:18px;border:1px solid rgba(120,180,255,0.12);box-shadow:inset 0 0 40px rgba(0,120,255,0.06), 0 0 24px rgba(0,180,255,0.08);min-height:460px;position:relative;overflow:hidden;"
     augmented-ui="tl-clip tr-clip br-clip bl-clip border">
""");

        sb.AppendLine("""
<div style="position:absolute;inset:0;background:
linear-gradient(90deg, rgba(0,160,255,0.03), transparent 25%, transparent 75%, rgba(255,40,80,0.03));
pointer-events:none;"></div>
""");

        sb.AppendLine("""
<h3 style="margin:0 0 6px 0;text-align:center;position:relative;z-index:1;">Project × Strategy Heatmap</h3>
<div class="chart-note" style="margin:0 0 18px 0;font-size:12px;color:#9fb3c8;text-align:center;position:relative;z-index:1;">
Comparative score by project and strategy. Blue = low signal, red = peak signal.
</div>
<div id="arena-heatmap-container"
     style="width:100%;display:flex;justify-content:center;align-items:center;position:relative;z-index:1;overflow:hidden;">
</div>
""");

        sb.AppendLine($$"""
<script>
const arenaHeatmapSketch = (p) => {
    const projects = [{{projectsJs}}];
    const strategies = [{{strategiesJs}}];
    const cells = [{{cellsJs}}];

    const minScore = {{F(minScore)}};
    const maxScore = {{F(maxScore)}};

    let hovered = null;

    function normalize(v) {
        return (v - minScore) / Math.max(0.0001, (maxScore - minScore));
    }

    function neonColor(t) {
        t = Math.max(0, Math.min(1, t));

        const stops = [
            [0.00, [ 40, 120, 255]],
            [0.25, [  0, 220, 255]],
            [0.50, [180,  70, 255]],
            [0.75, [255, 140,  40]],
            [1.00, [255,  40,  60]]
        ];

        for (let i = 0; i < stops.length - 1; i++) {
            const a = stops[i];
            const b = stops[i + 1];

            if (t >= a[0] && t <= b[0]) {
                const local = (t - a[0]) / (b[0] - a[0]);
                const r = a[1][0] + (b[1][0] - a[1][0]) * local;
                const g = a[1][1] + (b[1][1] - a[1][1]) * local;
                const bl = a[1][2] + (b[1][2] - a[1][2]) * local;
                return [r, g, bl];
            }
        }

        return [255, 40, 60];
    }

    p.setup = () => {
        const container = document.getElementById('arena-heatmap-container');
        const availableWidth = Math.max(620, container.offsetWidth || 760);

        const leftPad = 92;
        const rightPad = 20;
        const topPad = 56;
        const bottomPad = 20;

        const cols = Math.max(1, strategies.length);
        const rows = Math.max(1, projects.length);

        const cellW = Math.min(112, Math.floor((availableWidth - leftPad - rightPad) / cols));
        const cellH = 64;

        const width = leftPad + cols * cellW + rightPad;
        const height = topPad + rows * cellH + bottomPad;

        let canvas = p.createCanvas(width, height);
        canvas.parent('arena-heatmap-container');
        p.textFont("monospace");

        p._heatLeftPad = leftPad;
        p._heatTopPad = topPad;
        p._heatCellW = cellW;
        p._heatCellH = cellH;
    };

    p.draw = () => {
        p.clear();
        hovered = null;

        const leftPad = p._heatLeftPad;
        const topPad = p._heatTopPad;
        const cellW = p._heatCellW;
        const cellH = p._heatCellH;

        p.stroke('rgba(120,180,255,0.06)');
        p.strokeWeight(1);

        for (let x = leftPad; x <= leftPad + strategies.length * cellW; x += cellW) {
            p.line(x, topPad - 10, x, topPad + projects.length * cellH);
        }

        for (let y = topPad; y <= topPad + projects.length * cellH; y += cellH) {
            p.line(leftPad, y, leftPad + strategies.length * cellW, y);
        }

        p.noStroke();
        p.fill('#cfe4ff');
        p.textSize(11);
        p.textAlign(p.CENTER, p.CENTER);

        for (let c = 0; c < strategies.length; c++) {
            let label = strategies[c];
            if (label.length > 10)
                label = label.substring(0, 8) + "..";

            p.text(label, leftPad + c * cellW + cellW / 2, 24);
        }

        for (let r = 0; r < projects.length; r++) {
            let projectLabel = projects[r];
            if (projectLabel.length > 10)
                projectLabel = projectLabel.substring(0, 8) + "..";

            p.textAlign(p.RIGHT, p.CENTER);
            p.fill('#9fb3c8');
            p.text(projectLabel, leftPad - 12, topPad + r * cellH + cellH / 2);
        }

        for (let i = 0; i < cells.length; i++) {
            const cell = cells[i];
            const col = strategies.indexOf(cell.strategy);
            const row = projects.indexOf(cell.project);

            if (col < 0 || row < 0) continue;

            const x = leftPad + col * cellW;
            const y = topPad + row * cellH;

            const t = normalize(cell.score);
            const rgb = neonColor(t);
            const fillCol = p.color(rgb[0], rgb[1], rgb[2], 215);
            const glowCol = `rgba(${rgb[0]},${rgb[1]},${rgb[2]},0.95)`;

            const isHover =
                p.mouseX >= x + 4 && p.mouseX <= x + cellW - 4 &&
                p.mouseY >= y + 4 && p.mouseY <= y + cellH - 4;

            if (isHover) hovered = { ...cell, x, y, rgb };

            p.drawingContext.shadowColor = glowCol;
            p.drawingContext.shadowBlur = isHover ? 26 : 18;

            p.fill(fillCol);
            p.stroke(isHover ? 'rgba(255,255,255,0.88)' : `rgba(${rgb[0]},${rgb[1]},${rgb[2]},0.78)`);
            p.strokeWeight(isHover ? 1.8 : 1.0);
            p.rect(x + 4, y + 6, cellW - 8, cellH - 12, 10);

            p.drawingContext.shadowBlur = 0;

            p.noStroke();
            p.fill('rgba(8,12,22,0.18)');
            p.rect(x + 10, y + 12, cellW - 20, 10, 4);

            p.fill('#f7fbff');
            p.textAlign(p.CENTER, p.CENTER);
            p.textSize(12);
            p.text(cell.score.toFixed(1), x + cellW / 2, y + cellH / 2 + 4);
        }

        if (hovered) {
            const tipW = 180;
            const tipH = 92;
            let tx = hovered.x + 16;
            let ty = hovered.y - 8;

            if (tx + tipW > p.width - 8) tx = hovered.x - tipW - 16;
            if (ty + tipH > p.height - 8) ty = p.height - tipH - 8;
            if (ty < 8) ty = 8;

            const glowCol = `rgba(${hovered.rgb[0]},${hovered.rgb[1]},${hovered.rgb[2]},0.95)`;

            p.drawingContext.shadowColor = glowCol;
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,12,22,0.96)');
            p.stroke(glowCol);
            p.strokeWeight(1.2);
            p.rect(tx, ty, tipW, tipH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f3f8ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.text(`${hovered.project} // ${hovered.strategy}`, tx + 12, ty + 10);

            p.fill('#9fb3c8');
            p.text(`Score: ${hovered.score.toFixed(2)}`, tx + 12, ty + 30);
            p.text(`Confidence: ${(hovered.confidence * 100).toFixed(1)}%`, tx + 12, ty + 45);
            p.text(`Types: ${hovered.types} | Refs: ${hovered.refs}`, tx + 12, ty + 60);
        }
    };
};
new p5(arenaHeatmapSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
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

    private static string EscapeJs(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static double Clamp01(double value)
        => Math.Max(0, Math.Min(1, value));

    private static double ClampScore(double score)
    {
        if (score <= 0) return 0;
        return Math.Max(0, Math.Min(100, score / 2.2));
    }

    public sealed class StrategyVisualMetric
    {
        public required ParserStrategy Strategy { get; init; }
        public required string StrategyName { get; init; }
        public required string Color { get; init; }
        public int RunCount { get; init; }
        public double AverageScore { get; init; }
        public double AverageConfidence { get; init; }
        public double AverageExecutionMs { get; init; }
        public double AverageTypes { get; init; }
        public double AverageReferences { get; init; }
        public int Failures { get; init; }
    }
}
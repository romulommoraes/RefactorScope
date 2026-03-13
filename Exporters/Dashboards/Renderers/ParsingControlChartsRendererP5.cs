using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards.Renderers;

public sealed class ParsingControlChartsRendererP5
{
    // =====================================================
    // PALETA VISUAL
    // =====================================================

    private const string BgColor = "#0b1020";
    private const string AccentPrimary = "#ff9a3c";
    private const string AccentHot = "#ff8a3d";
    private const string SemanticSafe = "#7fd36b";
    private const string SemanticComplex = "#6ea8ff";
    private const string SemanticPattern = "#ffc15d";
    private const string SemanticRecovery = "#ff9a52";
    private const string SemanticRisk = "#ff6a3d";
    private const string TextMain = "#eef4ff";
    private const string TextMuted = "#8fa8ff";

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    // =====================================================
    // 1. RADAR (P5) + TOOLTIP
    // =====================================================
    public string RenderParsingRadarEnhanced(
        double typesPerFile,
        double refsPerType,
        double confidence,
        double msPerType)
    {
        var valType = Clamp01(typesPerFile / 4.0) * 100;
        var valRich = Clamp01(refsPerType / 3.0) * 100;
        var valConf = Clamp01(confidence) * 100;
        var valEff = Clamp01(1.0 - Math.Min(1.0, msPerType / 50.0)) * 100;

        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:450px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Parsing Control Radar</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Dynamic extraction quality scan.</div>""");
        sb.AppendLine("""<div id="p5-radar-container" style="display:flex;justify-content:center;width:100%;"></div>""");

        sb.AppendLine($$"""
<script>
const radarSketch = (p) => {
    const data = [{{F(valType)}}, {{F(valRich)}}, {{F(valConf)}}, {{F(valEff)}}];
    const labels = ["Type Density", "Graph Rich", "Confidence", "Efficiency"];
    const rawValues = [
        "{{F(typesPerFile)}} types/file",
        "{{F(refsPerType)}} refs/type",
        "{{F(confidence * 100)}}% confidence",
        "{{F(msPerType)}} ms/type"
    ];
    const maxRadius = 100;
    let hovered = null;

    p.setup = () => {
        let canvas = p.createCanvas(440, 360);
        canvas.parent('p5-radar-container');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        hovered = null;
        p.translate(p.width / 2, p.height / 2);

        let pulse = p.sin(p.frameCount * 3) * 5 + 10;

        p.stroke('{{TextMuted}}');
        p.noFill();
        p.strokeWeight(1);
        p.drawingContext.shadowBlur = 0;

        for (let i = 1; i <= 5; i++) {
            let r = (maxRadius / 5) * i;
            p.beginShape();
            for (let a = 0; a < 360; a += 90) {
                p.vertex(p.cos(a - 90) * r, p.sin(a - 90) * r);
            }
            p.endShape(p.CLOSE);

            if (i < 5) {
                p.fill('{{TextMuted}}');
                p.noStroke();
                p.textSize(9);
                p.text(`${i * 20}%`, 5, -r);
                p.noFill();
                p.stroke('{{TextMuted}}');
            }
        }

        for (let a = 0; a < 360; a += 90) {
            p.line(0, 0, p.cos(a - 90) * maxRadius, p.sin(a - 90) * maxRadius);
        }

        p.drawingContext.shadowColor = '{{AccentPrimary}}';
        p.drawingContext.shadowBlur = pulse;
        p.stroke('{{AccentPrimary}}');
        p.strokeWeight(2.5);
        p.fill('rgba(255, 154, 60, 0.2)');

        p.beginShape();
        for (let i = 0; i < 4; i++) {
            let angle = (i * 90) - 90;
            let r = p.map(data[i], 0, 100, 0, maxRadius);
            p.vertex(p.cos(angle) * r, p.sin(angle) * r);
        }
        p.endShape(p.CLOSE);

        for (let i = 0; i < 4; i++) {
            let angle = (i * 90) - 90;
            let r = p.map(data[i], 0, 100, 0, maxRadius);
            let px = p.cos(angle) * r;
            let py = p.sin(angle) * r;

            const mouseDx = p.mouseX - (p.width / 2 + px);
            const mouseDy = p.mouseY - (p.height / 2 + py);
            const isHover = Math.sqrt(mouseDx * mouseDx + mouseDy * mouseDy) <= 9;

            if (isHover) {
                hovered = {
                    label: labels[i],
                    normalized: data[i],
                    raw: rawValues[i],
                    x: px,
                    y: py
                };
            }

            p.fill('{{BgColor}}');
            p.stroke('{{AccentPrimary}}');
            p.strokeWeight(isHover ? 2.8 : 2);
            p.drawingContext.shadowBlur = isHover ? 18 : 10;
            p.drawingContext.shadowColor = '{{AccentPrimary}}';
            p.ellipse(px, py, isHover ? 10 : 8, isHover ? 10 : 8);
            p.drawingContext.shadowBlur = 0;
        }

        p.drawingContext.shadowBlur = 0;
        let scanAngle = p.frameCount * 2;
        p.noStroke();
        p.fill('rgba(255, 154, 60, 0.15)');
        p.arc(0, 0, maxRadius * 2, maxRadius * 2, scanAngle - 45, scanAngle);
        p.stroke('{{AccentPrimary}}');
        p.strokeWeight(1.5);
        p.line(0, 0, p.cos(scanAngle) * maxRadius, p.sin(scanAngle) * maxRadius);

        p.fill('{{TextMain}}');
        p.noStroke();
        p.textSize(12);

        p.textAlign(p.CENTER, p.CENTER);
        p.text(labels[0], 0, -maxRadius - 20);
        p.text(labels[2], 0, maxRadius + 20);

        p.textAlign(p.LEFT, p.CENTER);
        p.text(labels[1], maxRadius + 12, 0);

        p.textAlign(p.RIGHT, p.CENTER);
        p.text(labels[3], -maxRadius - 12, 0);

        p.resetMatrix();

        if (hovered) {
            const boxW = 170;
            const boxH = 72;
            let tx = p.mouseX + 16;
            let ty = p.mouseY - 12;

            if (tx + boxW > p.width - 8) tx = p.mouseX - boxW - 16;
            if (ty + boxH > p.height - 8) ty = p.height - boxH - 8;
            if (ty < 8) ty = 8;

            p.drawingContext.shadowColor = '{{AccentPrimary}}';
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,14,28,0.96)');
            p.stroke('{{AccentPrimary}}');
            p.strokeWeight(1.2);
            p.rect(tx, ty, boxW, boxH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f2f6ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.text(hovered.label, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Normalized: ${hovered.normalized.toFixed(1)}%`, tx + 12, ty + 30);
            p.text(`Raw: ${hovered.raw}`, tx + 12, ty + 46);
        }
    };
};
new p5(radarSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 2. DONUT (P5) + TOOLTIP
    // =====================================================
    public string RenderParsingRouteDonut(int safe, int complex, int ast, int recovery, int risk)
    {
        var total = Math.Max(1, safe + complex + ast + recovery + risk);
        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:450px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Route Distribution</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Parsing workload across execution routes.</div>""");
        sb.AppendLine("""<div id="p5-donut-container" style="display:flex;justify-content:center;width:100%;"></div>""");

        sb.AppendLine($$"""
<script>
const donutSketch = (p) => {
    const donutData = [
        { label: "Safe", value: {{safe}}, color: "{{SemanticSafe}}" },
        { label: "Complex", value: {{complex}}, color: "{{SemanticComplex}}" },
        { label: "AST/Pattern", value: {{ast}}, color: "{{SemanticPattern}}" },
        { label: "Recovery", value: {{recovery}}, color: "{{SemanticRecovery}}" },
        { label: "Risk", value: {{risk}}, color: "{{SemanticRisk}}" }
    ];
    const total = {{total}};
    let hovered = null;

    p.setup = () => {
        let canvas = p.createCanvas(360, 360);
        canvas.parent('p5-donut-container');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        hovered = null;
        p.translate(p.width / 2, p.height / 2 - 20);

        const outerRadius = 100;
        const innerRadius = 75;

        p.noFill();
        p.strokeWeight(1.5);
        p.stroke('rgba(255,255,255,0.1)');

        p.push();
        p.rotate(p.frameCount * 0.4);
        p.drawingContext.setLineDash([8, 12, 25, 12]);
        p.ellipse(0, 0, outerRadius * 2 + 30);
        p.pop();

        p.push();
        p.rotate(-p.frameCount * 0.6);
        p.drawingContext.setLineDash([4, 4]);
        p.ellipse(0, 0, innerRadius * 2 - 15);
        p.pop();

        p.drawingContext.setLineDash([]);

        let currentAngle = -90;
        p.strokeCap(p.SQUARE);

        donutData.forEach(item => {
            let angleSpan = (item.value / total) * 360;
            let gap = item.value > 0 ? 3 : 0;

            const midAngle = currentAngle + angleSpan / 2;
            const mouseAngle = p.degrees(Math.atan2(p.mouseY - (p.height / 2 - 20), p.mouseX - p.width / 2));
            let normalizedMouseAngle = mouseAngle;
            if (normalizedMouseAngle < -90) normalizedMouseAngle += 360;

            const distToCenter = p.dist(p.mouseX, p.mouseY, p.width / 2, p.height / 2 - 20);
            const isHover =
                item.value > 0 &&
                distToCenter >= innerRadius - 10 &&
                distToCenter <= outerRadius + 18 &&
                normalizedMouseAngle >= currentAngle &&
                normalizedMouseAngle <= currentAngle + angleSpan;

            if (isHover) hovered = { ...item };

            p.drawingContext.shadowColor = item.color;
            p.drawingContext.shadowBlur = isHover ? 20 : 12;
            p.stroke(item.color);
            p.strokeWeight(isHover ? 28 : 25);

            if (angleSpan > gap) {
                p.arc(0, 0, outerRadius + innerRadius, outerRadius + innerRadius, currentAngle + gap / 2, currentAngle + angleSpan - gap / 2);
            }

            currentAngle += angleSpan;
        });

        p.drawingContext.shadowBlur = 0;
        p.fill('{{TextMain}}');
        p.noStroke();
        p.textAlign(p.CENTER, p.CENTER);
        p.textSize(36);
        p.textStyle(p.BOLD);
        p.text(total, 0, -5);

        p.textSize(11);
        p.textStyle(p.NORMAL);
        p.fill('{{TextMuted}}');
        p.text("Total Routed", 0, 22);

        let startX = -140;
        let legendY = 160;
        p.textAlign(p.LEFT, p.CENTER);

        donutData.forEach((item, index) => {
            let xOffset = startX + (index % 3) * 105;
            let yOffset = legendY + Math.floor(index / 3) * 25;

            p.drawingContext.shadowColor = item.color;
            p.drawingContext.shadowBlur = 8;
            p.fill(item.color);
            p.rect(xOffset, yOffset - 4, 8, 8, 2);

            p.drawingContext.shadowBlur = 0;
            p.fill('{{TextMain}}');
            p.textSize(10);
            p.text(item.label, xOffset + 14, yOffset);
        });

        p.resetMatrix();

        if (hovered) {
            const boxW = 150;
            const boxH = 60;
            let tx = p.mouseX + 16;
            let ty = p.mouseY - 12;

            if (tx + boxW > p.width - 8) tx = p.mouseX - boxW - 16;
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
            p.text(hovered.label, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Files: ${hovered.value}`, tx + 12, ty + 30);
            p.text(`Share: ${(hovered.value / total * 100).toFixed(1)}%`, tx + 12, ty + 44);
        }
    };
};
new p5(donutSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 3. HEAT STRIP (P5) + TOOLTIP + GLOW
    // =====================================================
    public string RenderRiskHeatStrip(int safe, int moderate, int sparse, int anomaly, int recoveryDep)
    {
        var total = Math.Max(1, safe + moderate + sparse + anomaly + recoveryDep);
        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);" augmented-ui="tr-clip bl-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Parsing Risk Heat Strip</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Operational buckets showing reliability pressure.</div>""");

        sb.AppendLine("""<div id="p5-heat-container" style="width:100%; display:flex; justify-content:center;"></div>""");

        sb.AppendLine("<div style='display:flex;flex-wrap:wrap;justify-content:center;gap:12px;margin-top:20px;'>");
        var labels = new[]
        {
            ("Safe", safe, SemanticSafe),
            ("Moderate", moderate, SemanticComplex),
            ("Sparse", sparse, SemanticPattern),
            ("Anomaly", anomaly, SemanticRecovery),
            ("Recovery Dep.", recoveryDep, SemanticRisk)
        };

        foreach (var b in labels)
        {
            sb.AppendLine($"""<div style="display:flex;align-items:center;gap:10px;padding:8px 14px;background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.06);min-width:140px;justify-content:space-between;" augmented-ui="tl-clip br-clip border"><div style="display:flex;align-items:center;gap:10px;"><div style="width:10px;height:10px;background:{b.Item3};box-shadow:0 0 10px {b.Item3};"></div><div style="color:{TextMuted};font-size:12px;">{b.Item1}</div></div><div style="color:#f4e6d8;font-weight:700;">{b.Item2}</div></div>""");
        }

        sb.AppendLine("</div>");

        sb.AppendLine($$"""
<script>
const heatSketch = (p) => {
    const buckets = [
        { label: "Safe", val: {{safe}}, col: '{{SemanticSafe}}' },
        { label: "Moderate", val: {{moderate}}, col: '{{SemanticComplex}}' },
        { label: "Sparse", val: {{sparse}}, col: '{{SemanticPattern}}' },
        { label: "Anomaly", val: {{anomaly}}, col: '{{SemanticRecovery}}' },
        { label: "Recovery Dep.", val: {{recoveryDep}}, col: '{{SemanticRisk}}' }
    ];
    const total = {{total}};
    let segments = [];
    let hovered = null;

    p.setup = () => {
        let w = document.getElementById('p5-heat-container').offsetWidth || 1000;
        let canvas = p.createCanvas(w, 52);
        canvas.parent('p5-heat-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        segments = [];
        hovered = null;

        let currentX = 0;

        buckets.forEach(b => {
            if (b.val <= 0) return;

            let w = (b.val / total) * p.width;
            if (w < 6) w = 6;

            const seg = { ...b, x: currentX, w: w };
            segments.push(seg);

            const isHover =
                p.mouseX >= currentX &&
                p.mouseX <= currentX + w &&
                p.mouseY >= 0 &&
                p.mouseY <= p.height;

            if (isHover) hovered = seg;

            p.drawingContext.shadowColor = b.col;
            p.drawingContext.shadowBlur = isHover ? 24 : 16;
            p.fill(b.col);
            p.noStroke();
            p.rect(currentX, 4, w - 2, p.height - 8, 6);

            p.drawingContext.shadowBlur = 0;

            if (w > 28) {
                p.fill('#050814');
                p.textAlign(p.CENTER, p.CENTER);
                p.textStyle(p.BOLD);
                p.textSize(12);
                p.text(b.val, currentX + (w - 2) / 2, p.height / 2);
            }

            currentX += w;
        });

        let scanX = (p.frameCount * 5) % p.width;
        p.drawingContext.shadowColor = '#ffffff';
        p.drawingContext.shadowBlur = 18;
        p.fill('rgba(255,255,255,0.92)');
        p.noStroke();
        p.rect(scanX, 2, 3, p.height - 4, 2);
        p.drawingContext.shadowBlur = 0;

        if (hovered) {
            const boxW = 160;
            const boxH = 60;
            let tx = p.mouseX + 14;
            let ty = p.mouseY - 48;

            if (tx + boxW > p.width - 8) tx = p.mouseX - boxW - 14;
            if (ty < 8) ty = 8;

            p.drawingContext.shadowColor = hovered.col;
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,14,28,0.96)');
            p.stroke(hovered.col);
            p.strokeWeight(1.2);
            p.rect(tx, ty, boxW, boxH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f2f6ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.text(hovered.label, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Files: ${hovered.val}`, tx + 12, ty + 30);
            p.text(`Share: ${(hovered.val / total * 100).toFixed(1)}%`, tx + 12, ty + 44);
        }
    };
};
new p5(heatSketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // =====================================================
    // 4. SANKEY (P5) + TOOLTIP
    // =====================================================
    public string RenderParsingFlowSankey(
        int inputFiles, int safeFiles, int complexFiles, int astPatternFiles,
        int recoveryFiles, int extractedTypes, int extractedReferences, int riskFiles)
    {
        var maxFlow = Math.Max(1, new[]
        {
            inputFiles, safeFiles, complexFiles, astPatternFiles,
            recoveryFiles, extractedTypes, extractedReferences, riskFiles
        }.Max());

        var sb = new StringBuilder();

        sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05); overflow-x: auto;" augmented-ui="tl-clip tr-clip bl-clip br-clip border">""");
        sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Parsing Flow Sankey</h3>""");
        sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Flow of structural extraction from input scope to output graph and risk buckets.</div>""");
        sb.AppendLine("""<div id="p5-sankey-container"></div>""");

        sb.AppendLine($$"""
<script>
const sankeySketch = (p) => {
    const maxFlow = {{maxFlow}};

    const nodes = [
        { id: 0, name: "Input Files",      x: 40,   y: 180, val: {{inputFiles}},          col: '{{AccentPrimary}}' },
        { id: 1, name: "Safe Route",       x: 290,  y: 90,  val: {{safeFiles}},           col: '{{SemanticSafe}}' },
        { id: 2, name: "Complex Route",    x: 290,  y: 180, val: {{complexFiles}},        col: '{{SemanticComplex}}' },
        { id: 3, name: "AST / Pattern",    x: 290,  y: 270, val: {{astPatternFiles}},     col: '{{SemanticPattern}}' },
        { id: 4, name: "Recovery",         x: 290,  y: 360, val: {{recoveryFiles}},       col: '{{SemanticRecovery}}' },
        { id: 5, name: "Extracted Types",  x: 670,  y: 140, val: {{extractedTypes}},      col: '{{SemanticSafe}}' },
        { id: 6, name: "Extracted Refs",   x: 670,  y: 290, val: {{extractedReferences}}, col: '{{SemanticComplex}}' },
        { id: 7, name: "Risk Bucket",      x: 1000, y: 215, val: {{riskFiles}},           col: '{{SemanticRisk}}' }
    ];

    const links = [
        { src: 0, tgt: 1, val: {{safeFiles}},                                                   col: '{{SemanticSafe}}',     offset: 0.1, label: "Input → Safe Route" },
        { src: 0, tgt: 2, val: {{complexFiles}},                                                col: '{{SemanticComplex}}',  offset: 0.3, label: "Input → Complex Route" },
        { src: 0, tgt: 3, val: {{astPatternFiles}},                                             col: '{{SemanticPattern}}',  offset: 0.5, label: "Input → AST / Pattern" },
        { src: 0, tgt: 4, val: {{recoveryFiles}},                                               col: '{{SemanticRecovery}}', offset: 0.7, label: "Input → Recovery" },
        { src: 1, tgt: 5, val: {{Math.Min(safeFiles + astPatternFiles, extractedTypes)}},      col: '{{SemanticSafe}}',     offset: 0.2, label: "Safe Route → Extracted Types" },
        { src: 2, tgt: 6, val: {{Math.Min(complexFiles + recoveryFiles, extractedReferences)}}, col: '{{SemanticComplex}}',  offset: 0.4, label: "Complex Route → Extracted Refs" },
        { src: 3, tgt: 5, val: {{Math.Min(astPatternFiles, extractedTypes)}},                   col: '{{SemanticPattern}}',  offset: 0.6, label: "AST / Pattern → Extracted Types" },
        { src: 4, tgt: 7, val: {{Math.Max(1, riskFiles)}},                                      col: '{{SemanticRecovery}}', offset: 0.8, label: "Recovery → Risk Bucket" },
        { src: 5, tgt: 7, val: {{Math.Max(1, Math.Min(riskFiles, extractedTypes / 5))}},       col: '{{AccentHot}}',        offset: 0.9, label: "Extracted Types → Risk Bucket" },
        { src: 6, tgt: 7, val: {{Math.Max(1, Math.Min(riskFiles, extractedReferences / 10))}}, col: '{{SemanticRisk}}',     offset: 0.55, label: "Extracted Refs → Risk Bucket" }
    ];

    let hoveredNode = null;
    let hoveredLink = null;

    p.setup = () => {
        let canvas = p.createCanvas(1180, 440);
        canvas.parent('p5-sankey-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        hoveredNode = null;
        hoveredLink = null;

        p.noFill();

        links.forEach(link => {
            let source = nodes[link.src];
            let target = nodes[link.tgt];
            let thickness = p.map(link.val, 0, maxFlow, 6, 32);

            let x1 = source.x + 140;
            let y1 = source.y + 22;
            let x2 = target.x;
            let y2 = target.y + 22;
            let c1x = x1 + (x2 - x1) * 0.42;
            let c2x = x1 + (x2 - x1) * 0.68;

            let tMid = 0.5;
            let mx = p.bezierPoint(x1, c1x, c2x, x2, tMid);
            let my = p.bezierPoint(y1, y1, y2, y2, tMid);
            let isHover = p.dist(p.mouseX, p.mouseY, mx, my) <= Math.max(12, thickness * 0.6);

            if (isHover) hoveredLink = { ...link, mx, my };

            p.strokeWeight(isHover ? thickness + 2 : thickness);
            p.drawingContext.shadowBlur = isHover ? 22 : 15;
            p.drawingContext.shadowColor = link.col;

            let alpha = p.map(p.sin(p.frameCount * 2 + link.src * 20), -1, 1, 60, 160);
            let col = p.color(link.col);
            col.setAlpha(alpha);
            p.stroke(col);
            p.bezier(x1, y1, c1x, y1, c2x, y2, x2, y2);

            let t = (p.frameCount * 0.006 + link.offset) % 1;
            let px = p.bezierPoint(x1, c1x, c2x, x2, t);
            let py = p.bezierPoint(y1, y1, y2, y2, t);

            p.drawingContext.shadowColor = '#fff';
            p.drawingContext.shadowBlur = 20;
            p.fill(255);
            p.noStroke();
            p.ellipse(px, py, thickness * 0.5);
            p.noFill();
        });

        nodes.forEach(node => {
            const isHover =
                p.mouseX >= node.x &&
                p.mouseX <= node.x + 140 &&
                p.mouseY >= node.y &&
                p.mouseY <= node.y + 44;

            if (isHover) hoveredNode = node;

            p.drawingContext.shadowBlur = isHover ? 22 : 15;
            p.drawingContext.shadowColor = node.col;
            p.fill('rgba(6,12,24,0.92)');
            p.stroke(node.col);
            p.strokeWeight(isHover ? 2.2 : 1.5);
            p.rect(node.x, node.y, 140, 44, 8);

            p.drawingContext.shadowBlur = 0;
            p.fill('#f4e6d8');
            p.noStroke();
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.textStyle(p.BOLD);
            p.text(node.name, node.x + 12, node.y + 10);

            p.fill('{{TextMuted}}');
            p.textStyle(p.NORMAL);
            p.text(node.val, node.x + 12, node.y + 25);
        });

        const hovered = hoveredNode || hoveredLink;

        if (hovered) {
            const boxW = 190;
            const boxH = 74;
            let tx = p.mouseX + 18;
            let ty = p.mouseY - 14;
            const color = hovered.col;

            if (tx + boxW > p.width - 8) tx = p.mouseX - boxW - 18;
            if (ty + boxH > p.height - 8) ty = p.height - boxH - 8;
            if (ty < 8) ty = 8;

            p.drawingContext.shadowColor = color;
            p.drawingContext.shadowBlur = 18;
            p.fill('rgba(8,14,28,0.96)');
            p.stroke(color);
            p.strokeWeight(1.2);
            p.rect(tx, ty, boxW, boxH, 10);

            p.drawingContext.shadowBlur = 0;
            p.noStroke();
            p.fill('#f2f6ff');
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);

            if (hoveredNode) {
                p.text(hoveredNode.name, tx + 12, ty + 10);
                p.fill('#9fb3c8');
                p.text(`Value: ${hoveredNode.val}`, tx + 12, ty + 30);
                p.text(`Stage Node`, tx + 12, ty + 46);
            } else {
                p.text(hoveredLink.label, tx + 12, ty + 10);
                p.fill('#9fb3c8');
                p.text(`Flow: ${hoveredLink.val}`, tx + 12, ty + 30);
                p.text(`Structural route`, tx + 12, ty + 46);
            }
        }
    };
};
new p5(sankeySketch);
</script>
""");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
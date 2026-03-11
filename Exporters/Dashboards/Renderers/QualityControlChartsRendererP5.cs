using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System;

namespace RefactorScope.Exporters.Dashboards.Renderers
{
    public sealed class QualityControlChartsRendererP5
    {
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
        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));

        // =====================================================
        // 1. QUALITY RADAR (6 EIXOS)
        // =====================================================
        public string RenderQualityRadar(double parserConfidence, double effortConfidence, double statisticsSupport, double gateReadiness, double unresolvedPressure, double overallReadiness)
        {
            var values = new[] {
                Clamp01(parserConfidence) * 100, Clamp01(effortConfidence) * 100,
                Clamp01(statisticsSupport) * 100, Clamp01(gateReadiness) * 100,
                Clamp01(1.0 - unresolvedPressure) * 100, Clamp01(overallReadiness) * 100
            };

            var sb = new StringBuilder();

            sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:450px;" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Quality Signal Radar</h3>""");
            sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Quality posture across trust dimensions.</div>""");
            sb.AppendLine("""<div id="p5-quality-radar-container" style="display:flex;justify-content:center;width:100%;"></div>""");

            sb.AppendLine($$"""
<script>
const qualityRadarSketch = (p) => {
    const data = [{{F(values[0])}}, {{F(values[1])}}, {{F(values[2])}}, {{F(values[3])}}, {{F(values[4])}}, {{F(values[5])}}];
    const labels = ["Parser", "Effort", "Statistics", "Gates", "Unresolved", "Overall"];
    const maxRadius = 100;

    p.setup = () => {
        let canvas = p.createCanvas(420, 320);
        canvas.parent('p5-quality-radar-container');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        p.translate(p.width / 2, p.height / 2);
        let pulse = p.sin(p.frameCount * 3) * 5 + 10;
        let numAxes = labels.length;

        p.stroke('{{TextMuted}}'); p.noFill(); p.strokeWeight(1); p.drawingContext.shadowBlur = 0;
        for (let i = 1; i <= 5; i++) {
            let r = (maxRadius / 5) * i;
            p.beginShape();
            for (let a = 0; a < 360; a += (360 / numAxes)) p.vertex(p.cos(a - 90) * r, p.sin(a - 90) * r);
            p.endShape(p.CLOSE);
            if(i < 5) {
                p.fill('{{TextMuted}}'); p.noStroke(); p.textSize(9); p.text(`${i * 20}%`, 5, -r);
                p.noFill(); p.stroke('{{TextMuted}}');
            }
        }
        for (let a = 0; a < 360; a += (360 / numAxes)) p.line(0, 0, p.cos(a - 90) * maxRadius, p.sin(a - 90) * maxRadius);

        p.drawingContext.shadowColor = '{{AccentPrimary}}'; p.drawingContext.shadowBlur = pulse;
        p.stroke('{{AccentPrimary}}'); p.strokeWeight(2.5); p.fill('rgba(255, 154, 60, 0.2)');
        p.beginShape();
        for (let i = 0; i < numAxes; i++) {
            let angle = (i * (360 / numAxes)) - 90;
            let r = p.map(data[i], 0, 100, 0, maxRadius);
            p.vertex(p.cos(angle) * r, p.sin(angle) * r);
        }
        p.endShape(p.CLOSE);

        let scanAngle = p.frameCount * 2;
        p.drawingContext.shadowBlur = 0;
        p.noStroke(); p.fill('rgba(255, 154, 60, 0.15)');
        p.arc(0, 0, maxRadius * 2, maxRadius * 2, scanAngle - 60, scanAngle);
        p.stroke('{{AccentPrimary}}'); p.strokeWeight(1.5);
        p.line(0, 0, p.cos(scanAngle) * maxRadius, p.sin(scanAngle) * maxRadius);

        p.fill('{{TextMain}}'); p.noStroke(); p.textSize(11);
        for (let i = 0; i < numAxes; i++) {
            let angle = (i * (360 / numAxes)) - 90;
            let lx = p.cos(angle) * (maxRadius + 20);
            let ly = p.sin(angle) * (maxRadius + 20);
            p.textAlign(p.CENTER, p.CENTER);
            if (p.cos(angle) > 0.35) p.textAlign(p.LEFT, p.CENTER);
            else if (p.cos(angle) < -0.35) p.textAlign(p.RIGHT, p.CENTER);
            p.text(labels[i], lx, ly);
        }
    };
};
new p5(qualityRadarSketch);
</script>
""");

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        // =====================================================
        // 2. FITNESS STRIP (HTML DOM com Augmented UI)
        // =====================================================
        public string RenderFitnessGateStrip(IReadOnlyList<QualityControlChartsRenderer.QualityGateVisual> gates)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);' augmented-ui='tr-clip bl-clip border'>");
            sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Fitness Gate Strip</h3>");
            sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>Linear gate board for rapid operational reading.</div>");
            sb.AppendLine("<div style='display:grid;gap:12px;'>");

            foreach (var gate in gates)
            {
                var color = gate.Status switch
                {
                    QualityControlChartsRenderer.QualityGateStatus.Pass => SemanticSafe,
                    QualityControlChartsRenderer.QualityGateStatus.Warn => SemanticPattern,
                    QualityControlChartsRenderer.QualityGateStatus.Fail => SemanticRisk,
                    _ => SemanticComplex
                };

                var htmlName = gate.Name.Replace("<", "&lt;").Replace(">", "&gt;");
                var htmlDetails = gate.Details.Replace("<", "&lt;").Replace(">", "&gt;");

                // CORREÇÃO: Removido augmented-ui do badge para permitir border-radius: 999px
                sb.AppendLine($"""
<div style="display:grid;grid-template-columns:180px 100px 1fr;gap:16px;align-items:center;padding:12px 16px;background:rgba(255,255,255,0.03);border-left:4px solid {color};" augmented-ui="tl-clip br-clip border">
    <div style="color:{TextMain};font-weight:700;font-size:12px;">{htmlName}</div>
    <div style="display:inline-flex;align-items:center;justify-content:center;padding:4px 12px;border-radius:999px;background:{color};color:#08111f;font-weight:800;font-size:11px;">
        {gate.Status}
    </div>
    <div style="color:{TextMuted};font-size:12px;">{htmlDetails}</div>
</div>
""");
            }

            sb.AppendLine("</div></div>");
            return sb.ToString();
        }

        // =====================================================
        // 3. TRUST FLOW SANKEY (P5)
        // =====================================================
        public string RenderTrustFlowSankey(double parserConf, double effortConf, double statsSupport, double gateReady, double overallReady)
        {
            var pScore = (int)Math.Round(Clamp01(parserConf) * 100);
            var eScore = (int)Math.Round(Clamp01(effortConf) * 100);
            var sScore = (int)Math.Round(Clamp01(statsSupport) * 100);
            var gScore = (int)Math.Round(Clamp01(gateReady) * 100);
            var oScore = (int)Math.Round(Clamp01(overallReady) * 100);

            var sb = new StringBuilder();

            // CORREÇÃO: overflow-x removido para evitar a barra de rolagem
            sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);" augmented-ui="tl-clip tr-clip bl-clip br-clip border">""");
            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Trust Flow Sankey</h3>""");
            sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Support layers contributing to final execution readiness.</div>""");

            // CORREÇÃO: estilo CSS injetado para forçar o canvas do p5.js a ser responsivo e escalar como um SVG
            sb.AppendLine("""<style>#p5-trust-sankey-container canvas { width: 100% !important; max-width: 860px; height: auto !important; }</style>""");
            sb.AppendLine("""<div id="p5-trust-sankey-container" style="width:100%; display:flex; justify-content:center;"></div>""");

            sb.AppendLine($$"""
<script>
const trustSankeySketch = (p) => {
    const nodes = [
        { id: 0, name: "Parser",      x: 20,  y: 52,  val: {{pScore}}, col: '{{SemanticComplex}}' },
        { id: 1, name: "Effort",      x: 20,  y: 156, val: {{eScore}}, col: '{{SemanticPattern}}' },
        { id: 2, name: "Statistics",  x: 20,  y: 260, val: {{sScore}}, col: '{{AccentPrimary}}' },
        { id: 3, name: "Fitness Gates",x: 360, y: 156, val: {{gScore}}, col: '{{SemanticSafe}}' },
        { id: 4, name: "Overall Readiness", x: 700, y: 156, val: {{oScore}}, col: '{{AccentHot}}' }
    ];

    const links = [
        { src: 0, tgt: 3, val: {{pScore}}, col: '{{SemanticComplex}}', offset: 0.1 },
        { src: 1, tgt: 3, val: {{eScore}}, col: '{{SemanticPattern}}', offset: 0.4 },
        { src: 2, tgt: 3, val: {{sScore}}, col: '{{AccentPrimary}}', offset: 0.7 },
        { src: 3, tgt: 4, val: {{gScore}}, col: '{{SemanticSafe}}', offset: 0.3 },
        { src: 1, tgt: 4, val: {{Math.Max(8, eScore / 2)}}, col: '#ffb14d', offset: 0.6 },
        { src: 2, tgt: 4, val: {{Math.Max(8, sScore / 2)}}, col: '{{AccentPrimary}}', offset: 0.9 }
    ];

    p.setup = () => {
        let canvas = p.createCanvas(860, 360);
        canvas.parent('p5-trust-sankey-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        p.noFill();

        links.forEach(link => {
            let source = nodes[link.src]; let target = nodes[link.tgt];
            let thickness = p.map(link.val, 0, 100, 8, 26);
            let x1 = source.x + 120; let y1 = source.y + 24;
            let x2 = target.x;       let y2 = target.y + 24;
            let c1x = x1 + (x2 - x1) * 0.4; let c2x = x1 + (x2 - x1) * 0.7;

            p.strokeWeight(thickness);
            p.drawingContext.shadowBlur = 15; p.drawingContext.shadowColor = link.col;
            let alpha = p.map(p.sin(p.frameCount * 2 + link.src * 20), -1, 1, 60, 160);
            let col = p.color(link.col); col.setAlpha(alpha);
            p.stroke(col);
            p.bezier(x1, y1, c1x, y1, c2x, y2, x2, y2);

            let t = (p.frameCount * 0.005 + link.offset) % 1;
            let px = p.bezierPoint(x1, c1x, c2x, x2, t);
            let py = p.bezierPoint(y1, y1, y2, y2, t);
            
            p.drawingContext.shadowColor = '#fff'; p.drawingContext.shadowBlur = 20;
            p.fill(255); p.noStroke(); p.ellipse(px, py, thickness * 0.5); p.noFill();
        });

        nodes.forEach(node => {
            p.drawingContext.shadowBlur = 15; p.drawingContext.shadowColor = node.col;
            p.fill('rgba(6,12,24,0.92)'); p.stroke(node.col); p.strokeWeight(1.5);
            p.rect(node.x, node.y, 120, 48, 8);

            p.drawingContext.shadowBlur = 0; p.fill('#f4e6d8'); p.noStroke();
            p.textAlign(p.LEFT, p.TOP); p.textSize(10.5); p.textStyle(p.BOLD); p.text(node.name, node.x + 10, node.y + 12);
            p.fill('{{TextMuted}}'); p.textStyle(p.NORMAL); p.text(`${node.val}%`, node.x + 10, node.y + 28);
        });
    };
};
new p5(trustSankeySketch);
</script>
""");

            sb.AppendLine("</div>");
            return sb.ToString();
        }
    }
}
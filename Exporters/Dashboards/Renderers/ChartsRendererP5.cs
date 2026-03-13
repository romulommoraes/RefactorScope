using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RefactorScope.Exporters.Dashboards.Renderers
{
    public sealed class ChartsRendererP5
    {
        private const string BgColor = "#0b1020";
        private const string AccentPrimary = "#ff9a3c";
        private const string TextMain = "#eef4ff";
        private const string TextMuted = "#8fa8ff";

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        // =====================================================
        // 1. ARCHITECTURAL RADAR (8 EIXOS)
        // =====================================================
        public string RenderRadarSvgEnhanced(
            HygieneReport hygiene,
            StructuralCandidateAnalysisBreakdown breakdown,
            SolidResult? solid,
            ImplicitCouplingResult? implicitCoupling)
        {
            static double Normalize(int value, int total) => total == 0 ? 0 : value / (double)total;

            var solidAlerts = solid?.Alerts.Count ?? 0;
            var couplingScore = implicitCoupling == null ? 0 : Normalize(implicitCoupling.Suspicions.Count, hygiene.TotalClasses);

            var values = new[]
            {
                Normalize(breakdown.StructuralCandidates, hygiene.TotalClasses) * 100,
                Normalize(breakdown.PatternSimilarity, hygiene.TotalClasses) * 100,
                Normalize(breakdown.ProbabilisticConfirmed, hygiene.TotalClasses) * 100,
                Normalize(hygiene.NamespaceDriftCount, hygiene.TotalClasses) * 100,
                Normalize(hygiene.GlobalNamespaceCount, hygiene.TotalClasses) * 100,
                Normalize(hygiene.IsolatedCoreCount, hygiene.TotalClasses) * 100,
                couplingScore * 100,
                Math.Min(1.0, Normalize(solidAlerts, hygiene.TotalClasses) * 5) * 100
            };

            var rawValues = new[]
            {
                breakdown.StructuralCandidates,
                breakdown.PatternSimilarity,
                breakdown.ProbabilisticConfirmed,
                hygiene.NamespaceDriftCount,
                hygiene.GlobalNamespaceCount,
                hygiene.IsolatedCoreCount,
                implicitCoupling?.Suspicions.Count ?? 0,
                solidAlerts
            };

            var sb = new StringBuilder();

            sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Architectural Risk Radar</h3>""");
            sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Structural signal concentration.</div>""");
            sb.AppendLine("""<div id="p5-arch-radar-container" style="display:flex;justify-content:center;width:100%;"></div>""");

            sb.AppendLine($$"""
<script>
const archRadarSketch = (p) => {
    const data = [{{F(values[0])}}, {{F(values[1])}}, {{F(values[2])}}, {{F(values[3])}}, {{F(values[4])}}, {{F(values[5])}}, {{F(values[6])}}, {{F(values[7])}}];
    const labels = ["Dead Code", "Pattern", "Unresolved", "Drift", "Global", "Core", "Coupling", "SOLID"];
    const rawValues = [{{rawValues[0]}}, {{rawValues[1]}}, {{rawValues[2]}}, {{rawValues[3]}}, {{rawValues[4]}}, {{rawValues[5]}}, {{rawValues[6]}}, {{rawValues[7]}}];
    const maxRadius = 110;
    let hovered = null;

    p.setup = () => {
        let canvas = p.createCanvas(460, 360);
        canvas.parent('p5-arch-radar-container');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        hovered = null;
        p.translate(p.width / 2, p.height / 2);
        let pulse = p.sin(p.frameCount * 3) * 5 + 10;
        let numAxes = labels.length;

        // Grid
        p.stroke('{{TextMuted}}');
        p.noFill();
        p.strokeWeight(1);
        p.drawingContext.shadowBlur = 0;

        for (let i = 1; i <= 5; i++) {
            let r = (maxRadius / 5) * i;
            p.beginShape();
            for (let a = 0; a < 360; a += (360 / numAxes))
                p.vertex(p.cos(a - 90) * r, p.sin(a - 90) * r);
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

        for (let a = 0; a < 360; a += (360 / numAxes))
            p.line(0, 0, p.cos(a - 90) * maxRadius, p.sin(a - 90) * maxRadius);

        // Polígono Neon
        p.drawingContext.shadowColor = '{{AccentPrimary}}';
        p.drawingContext.shadowBlur = pulse;
        p.stroke('{{AccentPrimary}}');
        p.strokeWeight(2.5);
        p.fill('rgba(255, 154, 60, 0.2)');
        p.beginShape();
        for (let i = 0; i < numAxes; i++) {
            let angle = (i * (360 / numAxes)) - 90;
            let r = p.map(data[i], 0, 100, 0, maxRadius);
            p.vertex(p.cos(angle) * r, p.sin(angle) * r);
        }
        p.endShape(p.CLOSE);

        // Pontos com hover
        for (let i = 0; i < numAxes; i++) {
            let angle = (i * (360 / numAxes)) - 90;
            let r = p.map(data[i], 0, 100, 0, maxRadius);
            let px = p.cos(angle) * r;
            let py = p.sin(angle) * r;

            const worldX = (p.width / 2) + px;
            const worldY = (p.height / 2) + py;
            const isHover = p.dist(p.mouseX, p.mouseY, worldX, worldY) <= 9;

            if (isHover) {
                hovered = {
                    label: labels[i],
                    normalized: data[i],
                    raw: rawValues[i],
                    x: worldX,
                    y: worldY
                };
            }

            p.drawingContext.shadowColor = '{{AccentPrimary}}';
            p.drawingContext.shadowBlur = isHover ? 18 : 10;
            p.fill('{{BgColor}}');
            p.stroke('{{AccentPrimary}}');
            p.strokeWeight(isHover ? 2.8 : 2.0);
            p.ellipse(px, py, isHover ? 10 : 8, isHover ? 10 : 8);
            p.drawingContext.shadowBlur = 0;
        }

        // Scanner Line
        p.drawingContext.shadowBlur = 0;
        let scanAngle = p.frameCount * 2;
        p.noStroke();
        p.fill('rgba(255, 154, 60, 0.15)');
        p.arc(0, 0, maxRadius * 2, maxRadius * 2, scanAngle - 45, scanAngle);
        p.stroke('{{AccentPrimary}}');
        p.strokeWeight(1.5);
        p.line(0, 0, p.cos(scanAngle) * maxRadius, p.sin(scanAngle) * maxRadius);

        // Labels
        p.fill('{{TextMain}}');
        p.noStroke();
        p.textSize(11);
        for (let i = 0; i < numAxes; i++) {
            let angle = (i * (360 / numAxes)) - 90;
            let lx = p.cos(angle) * (maxRadius + 25);
            let ly = p.sin(angle) * (maxRadius + 20);
            p.textAlign(p.CENTER, p.CENTER);
            if (p.cos(angle) > 0.35) p.textAlign(p.LEFT, p.CENTER);
            else if (p.cos(angle) < -0.35) p.textAlign(p.RIGHT, p.CENTER);
            p.text(labels[i], lx, ly);
        }

        p.resetMatrix();

        if (hovered) {
            const boxW = 170;
            const boxH = 72;
            let tx = hovered.x + 16;
            let ty = hovered.y - 12;

            if (tx + boxW > p.width - 8) tx = hovered.x - boxW - 16;
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
            p.text(`Raw Signal: ${hovered.raw}`, tx + 12, ty + 46);
        }
    };
};
new p5(archRadarSketch);
</script>
""");

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        // =====================================================
        // 2. ARCHITECTURAL GALAXY (SCATTER PLOT)
        // =====================================================
        public string RenderArchitecturalGalaxyEnhanced(CouplingResult coupling)
        {
            var modules = coupling.AbstractnessByModule.Keys.Distinct().ToList();
            var rankedModules = modules.Select(module => new
            {
                Name = module,
                FanOut = coupling.ModuleFanOut.GetValueOrDefault(module),
                Abs = coupling.AbstractnessByModule.GetValueOrDefault(module),
                Inst = coupling.InstabilityByModule.GetValueOrDefault(module),
                Dist = coupling.DistanceByModule.GetValueOrDefault(module)
            }).OrderByDescending(x => x.FanOut).ToList();

            var highlightModules = rankedModules.Take(4).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();

            sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);" augmented-ui="tl-clip tr-clip br-clip bl-clip border">""");
            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Architectural Galaxy (A/I)</h3>""");
            sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Instability (X) vs Abstractness (Y).</div>""");
            sb.AppendLine("""<div id="p5-galaxy-container" style="display:flex;justify-content:center;width:100%;"></div>""");

            var jsData = new StringBuilder("[");
            foreach (var m in rankedModules)
            {
                var fill = m.FanOut > 20 ? "#ff8a3d" : m.FanOut > 10 ? "#ffc15d" : "#ffb14d";
                var highlighted = highlightModules.Contains(m.Name).ToString().ToLower();
                jsData.Append($"{{ name: '{EscapeJs(m.Name)}', inst: {F(m.Inst)}, abs: {F(m.Abs)}, fan: {m.FanOut}, dist: {F(m.Dist)}, col: '{fill}', hl: {highlighted} }},");
            }
            jsData.Append("]");

            sb.AppendLine($$"""
<script>
const galaxySketch = (p) => {
    const points = {{jsData.ToString()}};
    let hovered = null;
    
    p.setup = () => {
        let canvas = p.createCanvas(420, 420);
        canvas.parent('p5-galaxy-container');
        p.textFont("monospace");
    };

    p.draw = () => {
        p.clear();
        hovered = null;

        let margin = 40;
        let plotW = p.width - (margin * 2);
        let plotH = p.height - (margin * 2);

        // Zone Fills
        p.noStroke();
        p.fill('rgba(255,140,40,0.020)');
        p.rect(margin, margin, plotW, plotH);
        p.fill('rgba(255,180,90,0.030)');
        p.triangle(margin, margin, p.width - margin, p.height - margin, margin, p.height - margin);
        p.fill('rgba(255,120,40,0.020)');
        p.triangle(margin, margin, p.width - margin, margin, p.width - margin, p.height - margin);

        // Grid & Ticks
        p.stroke('rgba(210,125,48,0.16)');
        p.strokeWeight(1);
        p.fill('rgba(235,214,194,0.52)');
        p.noStroke();
        p.textSize(9);

        [0, 0.25, 0.5, 0.75, 1].forEach(tick => {
            let x = margin + plotW * tick;
            let y = margin + plotH * (1 - tick);
            p.stroke('rgba(210,125,48,0.16)');
            p.line(x, margin, x, p.height - margin);
            p.line(margin, y, p.width - margin, y);
            p.noStroke();
            p.textAlign(p.CENTER, p.TOP);
            p.text(tick, x, p.height - margin + 8);
            p.textAlign(p.RIGHT, p.CENTER);
            p.text(tick, margin - 6, y);
        });

        // Main Axes
        p.stroke('rgba(235,150,70,0.38)');
        p.strokeWeight(1.5);
        p.line(margin, p.height - margin, p.width - margin, p.height - margin);
        p.line(margin, p.height - margin, margin, margin);

        // Main Sequence
        p.stroke('rgba(255,190,100,0.40)');
        p.drawingContext.setLineDash([5, 5]);
        p.line(margin, margin, p.width - margin, p.height - margin);
        p.drawingContext.setLineDash([]);
        
        p.noStroke();
        p.fill('#f2e1d2');
        p.textSize(11);
        p.textAlign(p.RIGHT, p.TOP);
        p.text("Instability", p.width - margin, p.height - margin + 25);
        p.textAlign(p.LEFT, p.BOTTOM);
        p.text("Abstractness", 0, margin - 15);

        // Bubbles
        points.forEach(pt => {
            let x = margin + plotW * pt.inst;
            let y = (p.height - margin) - (plotH * pt.abs);
            let baseSize = 5 + Math.min(12, pt.fan / 2.8);
            let size = baseSize + p.sin(p.frameCount * 0.05 + pt.fan) * 2;

            const isHover = p.dist(p.mouseX, p.mouseY, x, y) <= size + 4;
            if (isHover) hovered = { ...pt, x, y, size };

            p.drawingContext.shadowColor = pt.col;
            p.drawingContext.shadowBlur = isHover ? 20 : 12;
            p.fill(pt.col);
            p.stroke('#fff0d6');
            p.strokeWeight(isHover ? 2.2 : 1.5);
            p.ellipse(x, y, size * 2);

            p.drawingContext.shadowBlur = 0;

            if (pt.hl) {
                p.fill('#f4e6d8');
                p.noStroke();
                p.textSize(10);
                p.textAlign(p.LEFT, p.BOTTOM);
                p.text(pt.name, x + size + 4, y - size - 2);
            }
        });

        if (hovered) {
            const boxW = 185;
            const boxH = 88;
            let tx = hovered.x + 16;
            let ty = hovered.y - 12;

            if (tx + boxW > p.width - 8) tx = hovered.x - boxW - 16;
            if (ty + boxH > p.height - 8) ty = p.height - boxH - 8;
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
            p.text(hovered.name, tx + 12, ty + 10);
            p.fill('#9fb3c8');
            p.text(`Abstractness: ${hovered.abs.toFixed(2)}`, tx + 12, ty + 30);
            p.text(`Instability: ${hovered.inst.toFixed(2)}`, tx + 12, ty + 44);
            p.text(`Distance: ${hovered.dist.toFixed(2)}`, tx + 12, ty + 58);
            p.text(`Fan-Out: ${hovered.fan}`, tx + 12, ty + 72);
        }
    };
};
new p5(galaxySketch);
</script>
""");

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string EscapeJs(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"");
        }
    }
}
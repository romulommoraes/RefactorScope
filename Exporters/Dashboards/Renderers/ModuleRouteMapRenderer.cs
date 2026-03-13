using RefactorScope.Exporters.Projections.Architecture;
using System.Text;
using System.Text.Json;

namespace RefactorScope.Exporters.Dashboards.Renderers
{
    public sealed class ModuleRouteMapRenderer
    {
        public string RenderModuleRouteMap(ModuleRouteMapModel model)
        {
            if (model.Nodes.Count == 0 || model.Edges.Count == 0)
                return string.Empty;

            var nodesJson = JsonSerializer.Serialize(
                model.Nodes.Select(n => new
                {
                    id = n.Id,
                    label = n.Label,
                    kind = n.Kind,
                    subtitle = n.Subtitle,
                    pressure = n.Pressure,
                    inDegree = n.InDegree,
                    outDegree = n.OutDegree,
                    weightedIn = n.WeightedIn,
                    weightedOut = n.WeightedOut,
                    hubScore = n.HubScore
                }));

            var edgesJson = JsonSerializer.Serialize(
                model.Edges.Select(e => new
                {
                    from = ToId(e.From),
                    to = ToId(e.To),
                    type = e.Type,
                    weight = e.Weight,
                    traffic = e.Traffic
                }));

            var sb = new StringBuilder();

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Structural Route Map</h2>
        <div class="line"></div>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);width:100%;" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            sb.AppendLine("""
<h3 style="margin:0 0 6px 0;text-align:center;">Structural Route Map</h3>
<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">
Inferred entry zones, transit stations, consolidation hubs and output routes. This map does not represent exact runtime traces.
</div>
""");

            sb.AppendLine("""<style>#p5-route-map-container canvas { width: 100% !important; max-width: 1600px; height: auto !important; }</style>""");
            sb.AppendLine("""<div id="p5-route-map-container" style="width:100%; display:flex; justify-content:center; align-items:center;"></div>""");

            var script = """
<script>
const routeMapNodes = __NODES_JSON__;
const routeMapLinks = __EDGES_JSON__;

const routeMapSketch = (p) => {
    let modules = [];
    let links = [];
    let particles = [];
    let palette;
    let selectedNode = null;

    p.setup = () => {
        p.createCanvas(1600, 980).parent('p5-route-map-container');
        p.pixelDensity(2);
        p.textFont("monospace");
        p.rectMode(p.CORNER);

        palette = {
            bg: p.color("#03060a"),
            grid: p.color(0, 229, 255, 14),
            text: p.color("#00e5ff"),
            muted: p.color("#255b7a"),
            whiteSoft: p.color(230, 245, 255, 180),

            bootstrap: p.color("#7ad3ff"),
            entry: p.color("#00e5ff"),
            process: p.color("#b500ff"),
            station: p.color("#ffaa00"),
            hub: p.color("#ff0055"),
            support: p.color("#7a7aff"),
            exit: p.color("#00ff88"),

            uni: p.color("#0088ff"),
            bi: p.color("#ff4400"),
            stationFlow: p.color("#ffcc00"),
            outFlow: p.color("#00ffcc"),

            low: p.color("#00ff88"),
            mid: p.color("#ffaa00"),
            high: p.color("#ff3355")
        };

        buildLayout();
        buildParticles();
    };

    p.draw = () => {
        p.background(palette.bg);
        drawGrid();
        drawZones();
        drawWeightedLinks();
        drawParticles();
        drawModulesAdvanced();
        drawTelemetryPanels();
        drawLegendAdvanced();
        drawTitleAdvanced();
        drawHUDOverlay();
    };

    p.mousePressed = () => {
        selectedNode = null;
        for (let m of modules) {
            if (p.mouseX >= m.x && p.mouseX <= m.x + m.w && p.mouseY >= m.y && p.mouseY <= m.y + m.h) {
                selectedNode = m;
                break;
            }
        }
    };

    function buildLayout() {
        const columns = {
            bootstrap: { x: 60,   startY: 130 },
            entry:     { x: 60,   startY: 290 },
            process:   { x: 360,  startY: 110 },
            station:   { x: 760,  startY: 145 },
            hub:       { x: 1080, startY: 250 },
            support:   { x: 1080, startY: 110 },
            exit:      { x: 1380, startY: 150 }
        };

        const counts = {
            bootstrap: 0, entry: 0, process: 0, station: 0, hub: 0, support: 0, exit: 0
        };

        modules = routeMapNodes.map(n => {
            const kind = n.kind || "process";
            const column = columns[kind] || columns.process;
            const index = counts[kind]++;

            const ySpacing =
                kind === "hub" ? 120 :
                kind === "support" ? 110 :
                kind === "bootstrap" ? 90 :
                145;

            const x = column.x;
            const y = column.startY + (index * ySpacing);
            const size = getNodeSize(kind, n.label);

            return {
                ...n,
                x: x,
                y: y,
                w: size.w,
                h: size.h,
                hexCode: "0x" + Math.floor(p.random(1000, 9999)).toString(16).toUpperCase()
            };
        });

        links = routeMapLinks;
    }

    function getNodeSize(kind, label) {
        const baseWidth = Math.max(175, Math.min(255, 125 + (label.length * 7)));

        if (kind === "hub") return { w: Math.max(220, baseWidth), h: 86 };
        if (kind === "process") return { w: baseWidth, h: 68 };
        if (kind === "support") return { w: Math.max(190, baseWidth - 10), h: 60 };
        if (kind === "bootstrap") return { w: Math.max(170, baseWidth - 20), h: 56 };

        return { w: baseWidth, h: 64 };
    }

    function buildParticles() {
        particles = [];

        for (let i = 0; i < links.length; i++) {
            const link = links[i];
            const count = Math.max(2, Math.floor(link.weight * 0.8));

            if (link.type === "bi") {
                for (let n = 0; n < count; n++) {
                    particles.push(makeParticle(i, p.random(0.0025, 0.0046), 0));
                    particles.push(makeParticle(i, p.random(0.0022, 0.0042), 1));
                }
            } else {
                for (let n = 0; n < count; n++) {
                    particles.push(makeParticle(i, p.random(0.0018, 0.0040), 0));
                }
            }
        }
    }

    function makeParticle(linkIndex, speed, reverseMode) {
        return {
            linkIndex: linkIndex,
            t: p.random(),
            speed: speed,
            reverseMode: reverseMode,
            size: p.random(2.5, 4.8)
        };
    }

    function getModule(id) {
        return modules.find(m => m.id === id);
    }

    function drawGrid() {
        p.stroke(palette.grid);
        p.strokeWeight(1);
        for (let x = 0; x < p.width; x += 50) p.line(x, 0, x, p.height);
        for (let y = 0; y < p.height; y += 50) p.line(0, y, p.width, y);

        p.stroke(0, 229, 255, 35);
        p.line(p.width / 2 - 40, p.height / 2, p.width / 2 + 40, p.height / 2);
        p.line(p.width / 2, p.height / 2 - 40, p.width / 2, p.height / 2 + 40);
        p.noFill();
        p.ellipse(p.width / 2, p.height / 2, 54, 54);
    }

    function drawZones() {
        const zones = [
            { x: 40, y: 95,  w: 250, h: 120, label: "SEC_ALPHA // BOOTSTRAP" },
            { x: 40, y: 245, w: 250, h: 250, label: "SEC_ALPHA2 // ENTRY" },
            { x: 320, y: 75, w: 360, h: 470, label: "SEC_BETA // PROCESSING" },
            { x: 720, y: 110, w: 290, h: 360, label: "SEC_GAMMA // STATIONS" },
            { x: 1040, y: 80, w: 300, h: 120, label: "SEC_DELTA0 // SUPPORT" },
            { x: 1040, y: 210, w: 300, h: 220, label: "SEC_DELTA // HUB" },
            { x: 1360, y: 100, w: 220, h: 360, label: "SEC_OMEGA // EXPORT / OUTPUT" }
        ];

        for (let z of zones) {
            drawZoneBrackets(z);
            p.fill(palette.text);
            p.noStroke();
            p.textAlign(p.LEFT, p.TOP);
            p.textStyle(p.BOLD);
            p.textSize(10);
            p.text(z.label, z.x + 10, z.y - 15);
        }
    }

    function drawZoneBrackets(z) {
        p.stroke(palette.muted);
        p.noFill();
        p.strokeWeight(2);
        let L = 20;

        p.line(z.x, z.y + L, z.x, z.y);
        p.line(z.x, z.y, z.x + L, z.y);
        p.line(z.x + z.w - L, z.y, z.x + z.w, z.y);
        p.line(z.x + z.w, z.y, z.x + z.w, z.y + L);

        p.line(z.x, z.y + z.h - L, z.x, z.y + z.h);
        p.line(z.x, z.y + z.h, z.x + L, z.y + z.h);
        p.line(z.x + z.w - L, z.y + z.h, z.x + z.w, z.y + z.h);
        p.line(z.x + z.w, z.y + z.h - L, z.x + z.w, z.y + z.h);
    }

    function drawTitleAdvanced() {
        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(24);
        p.text("STRUCTURAL ROUTE MAP", 50, 24);

        p.fill(palette.entry);
        p.textStyle(p.NORMAL);
        p.textSize(12);
        p.text("Directional routes, transit stations, route weights, node pressure and inferred hub intensity.", 50, 54);

        p.fill(palette.whiteSoft);
        p.textSize(10);
        p.text("This map shows structural circulation patterns, not exact runtime execution traces.", 50, 72);
    }

    function drawWeightedLinks() {
        for (let link of links) {
            if (link.type === "bi") drawWeightedBidirectional(link);
            else drawWeightedUni(link);
        }
    }

    function drawWeightedUni(link) {
        let a = getModule(link.from);
        let b = getModule(link.to);
        if (!a || !b) return;

        let path = getPath(a, b, 0);
        let c = getLinkColor(link.type);
        let alpha = link.type === "uniGhost" ? 40 : p.map(link.weight, 1, 8, 90, 220);
        let sw = link.type === "uniGhost" ? 1 : p.map(link.weight, 1, 8, 1.2, 4.5);

        let cc = p.color(c);
        cc.setAlpha(alpha);

        p.noFill();
        p.stroke(cc);
        p.strokeWeight(sw);
        p.drawingContext.shadowBlur = link.type === "uniGhost" ? 0 : p.map(link.traffic, 0.2, 1.0, 4, 14);
        p.drawingContext.shadowColor = c.toString();

        p.bezier(path.x1, path.y1, path.cp1x, path.cp1y, path.cp2x, path.cp2y, path.x2, path.y2);
        p.drawingContext.shadowBlur = 0;

        drawArrowAtEnd(path, c);
        drawLinkWeightTag(path, link.weight, c);
    }

    function drawWeightedBidirectional(link) {
        let a = getModule(link.from);
        let b = getModule(link.to);
        if (!a || !b) return;

        let forward = getPath(a, b, 16);
        let backward = getPath(b, a, -16);

        let c = getLinkColor(link.type);
        let sw = p.map(link.weight, 1, 8, 1.4, 3.8);

        let c1 = p.color(c);
        c1.setAlpha(170);

        p.noFill();
        p.stroke(c1);
        p.strokeWeight(sw);
        p.drawingContext.shadowBlur = p.map(link.traffic, 0.2, 1.0, 6, 16);
        p.drawingContext.shadowColor = c.toString();

        p.bezier(forward.x1, forward.y1, forward.cp1x, forward.cp1y, forward.cp2x, forward.cp2y, forward.x2, forward.y2);
        p.bezier(backward.x1, backward.y1, backward.cp1x, backward.cp1y, backward.cp2x, backward.cp2y, backward.x2, backward.y2);

        p.drawingContext.shadowBlur = 0;

        drawArrowAtEnd(forward, c);
        drawArrowAtEnd(backward, c);
        drawLinkWeightTag(forward, link.weight, c);
    }

    function getPath(a, b, offsetCurve) {
        let sx = a.x + a.w;
        let sy = a.y + a.h / 2;
        let ex = b.x;
        let ey = b.y + b.h / 2;

        if (b.x < a.x) {
            sx = a.x;
            ex = b.x + b.w;
        }

        let dx = ex - sx;
        let bend = Math.max(55, Math.abs(dx) * 0.38);

        return {
            x1: sx,
            y1: sy,
            cp1x: sx + (dx >= 0 ? bend : -bend),
            cp1y: sy + offsetCurve,
            cp2x: ex - (dx >= 0 ? bend : -bend),
            cp2y: ey + offsetCurve,
            x2: ex,
            y2: ey
        };
    }

    function drawArrowAtEnd(path, c) {
        let t = 0.98;
        let x = p.bezierPoint(path.x1, path.cp1x, path.cp2x, path.x2, t);
        let y = p.bezierPoint(path.y1, path.cp1y, path.cp2y, path.y2, t);
        let tx = p.bezierTangent(path.x1, path.cp1x, path.cp2x, path.x2, t);
        let ty = p.bezierTangent(path.y1, path.cp1y, path.cp2y, path.y2, t);

        p.push();
        p.translate(x, y);
        p.rotate(p.atan2(ty, tx));
        p.noStroke();
        p.fill(c);
        p.triangle(0, 0, -12, 4, -12, -4);
        p.pop();
    }

    function drawLinkWeightTag(path, weight, c) {
        let t = 0.52;
        let x = p.bezierPoint(path.x1, path.cp1x, path.cp2x, path.x2, t);
        let y = p.bezierPoint(path.y1, path.cp1y, path.cp2y, path.y2, t);

        p.noStroke();
        p.fill(5, 12, 18, 210);
        p.rect(x - 18, y - 9, 36, 18, 3);

        p.stroke(c);
        p.strokeWeight(1);
        p.noFill();
        p.rect(x - 18, y - 9, 36, 18, 3);

        p.fill(255);
        p.noStroke();
        p.textAlign(p.CENTER, p.CENTER);
        p.textSize(9);
        p.text(weight, x, y);
    }

    function drawParticles() {
        for (let pt of particles) {
            let link = links[pt.linkIndex];
            if (link.type === "bi") drawBiParticle(pt, link);
            else drawUniParticle(pt, link);
        }
    }

    function drawUniParticle(pt, link) {
        let a = getModule(link.from);
        let b = getModule(link.to);
        if (!a || !b) return;

        let path = getPath(a, b, 0);

        pt.t += pt.speed * p.map(link.traffic, 0.2, 1.0, 0.8, 1.3);
        if (pt.t > 1) pt.t = 0;

        let x = p.bezierPoint(path.x1, path.cp1x, path.cp2x, path.x2, pt.t);
        let y = p.bezierPoint(path.y1, path.cp1y, path.cp2y, path.y2, pt.t);
        let tx = p.bezierTangent(path.x1, path.cp1x, path.cp2x, path.x2, pt.t);
        let ty = p.bezierTangent(path.y1, path.cp1y, path.cp2y, path.y2, pt.t);

        let c = getParticleColor(link);

        p.noStroke();
        p.fill(255);
        p.drawingContext.shadowBlur = p.map(link.weight, 1, 8, 8, 18);
        p.drawingContext.shadowColor = c.toString();

        p.push();
        p.translate(x, y);
        p.rotate(p.atan2(ty, tx));
        p.rect(-pt.size, -1.2, pt.size * 2.2, 2.4);
        p.pop();

        p.drawingContext.shadowBlur = 0;
    }

    function drawBiParticle(pt, link) {
        let a = getModule(link.from);
        let b = getModule(link.to);
        if (!a || !b) return;

        let path = pt.reverseMode === 0 ? getPath(a, b, 16) : getPath(b, a, -16);

        pt.t += pt.speed * p.map(link.traffic, 0.2, 1.0, 0.85, 1.35);
        if (pt.t > 1) pt.t = 0;

        let x = p.bezierPoint(path.x1, path.cp1x, path.cp2x, path.x2, pt.t);
        let y = p.bezierPoint(path.y1, path.cp1y, path.cp2y, path.y2, pt.t);
        let tx = p.bezierTangent(path.x1, path.cp1x, path.cp2x, path.x2, pt.t);
        let ty = p.bezierTangent(path.y1, path.cp1y, path.cp2y, path.y2, pt.t);

        p.noStroke();
        p.fill(255);
        p.drawingContext.shadowBlur = p.map(link.weight, 1, 8, 10, 20);
        p.drawingContext.shadowColor = palette.bi.toString();

        p.push();
        p.translate(x, y);
        p.rotate(p.atan2(ty, tx));
        p.rect(-pt.size, -1.5, pt.size * 2.4, 3);
        p.pop();

        p.drawingContext.shadowBlur = 0;
    }

    function getParticleColor(link) {
        const from = getModule(link.from);
        if (!from) return palette.uni;

        if (from.kind === "station") return palette.stationFlow;
        if (from.kind === "hub") return palette.outFlow;
        if (link.type === "uniGhost") return p.color(0, 229, 255, 70);
        return palette.uni;
    }

    function drawModulesAdvanced() {
        for (let m of modules) drawModuleAdvanced(m);
    }

    function drawModuleAdvanced(m) {
        let base = getModuleColor(m.kind);
        let risk = getPressureColor(m.pressure);
        let cut = 12;
        let blink = p.frameCount % 60 < 30;
        let selected = selectedNode && selectedNode.id === m.id;

        p.drawingContext.shadowBlur = selected ? 22 : 15;
        p.drawingContext.shadowColor = base.toString();

        p.fill(3, 6, 10, 228);
        p.stroke(risk);
        p.strokeWeight(selected ? 2.8 : (m.kind === "hub" ? 2.1 : 1.3));

        p.beginShape();
        p.vertex(m.x + cut, m.y);
        p.vertex(m.x + m.w, m.y);
        p.vertex(m.x + m.w, m.y + m.h - cut);
        p.vertex(m.x + m.w - cut, m.y + m.h);
        p.vertex(m.x, m.y + m.h);
        p.vertex(m.x, m.y + cut);
        p.endShape(p.CLOSE);

        p.drawingContext.shadowBlur = 0;

        p.noStroke();
        p.fill(base);
        p.rect(m.x + cut + 5, m.y, 26, 3);
        p.rect(m.x + cut + 36, m.y, 7, 3);

        if (blink || m.kind !== "hub") {
            p.fill(m.kind === "hub" ? palette.hub : palette.text);
            p.ellipse(m.x + m.w - 15, m.y + 15, 6, 6);
        }

        drawTopologyBadge(m, base);

        p.fill(255);
        p.noStroke();
        p.textAlign(p.LEFT, p.CENTER);
        p.textStyle(p.BOLD);
        p.textSize(m.kind === "hub" ? 14 : 12);
        p.text(m.label.toUpperCase(), m.x + 36, m.y + m.h / 2 - 11);

        p.fill(palette.text);
        p.textStyle(p.NORMAL);
        p.textSize(9);
        p.text("ID:" + m.hexCode + " // " + m.subtitle, m.x + 36, m.y + m.h / 2 + 7);

        p.fill(palette.whiteSoft);
        p.textSize(8.5);
        p.text("IN:" + m.inDegree + " OUT:" + m.outDegree + " TRAF:" + (m.weightedIn + m.weightedOut), m.x + 36, m.y + m.h / 2 + 20);

        drawMiniPressureBar(m, risk);
        drawMiniHubRing(m, base);

        if (selected) drawSelectedPanel(m);
    }

    function drawMiniPressureBar(m, c) {
        let barX = m.x + m.w - 70;
        let barY = m.y + m.h - 10;
        let barW = 50;
        let fillW = barW * m.pressure;

        p.noStroke();
        p.fill(30, 45, 58, 200);
        p.rect(barX, barY, barW, 3, 2);

        p.fill(c);
        p.rect(barX, barY, fillW, 3, 2);
    }

    function drawMiniHubRing(m, c) {
        if (m.kind !== "hub") return;

        p.noFill();
        p.stroke(c);
        p.strokeWeight(1);
        let pulse = p.map(p.sin(p.frameCount * 0.05), -1, 1, 0, 10);
        p.rect(m.x - 5 - pulse / 2, m.y - 5 - pulse / 2, m.w + 10 + pulse, m.h + 10 + pulse, 5);
    }

    function drawSelectedPanel(m) {
        let px = m.x + m.w + 18;
        let py = m.y - 4;
        let pw = 170;
        let ph = 78;

        p.noStroke();
        p.fill(4, 10, 16, 235);
        p.rect(px, py, pw, ph, 5);

        p.stroke(getPressureColor(m.pressure));
        p.strokeWeight(1.2);
        p.noFill();
        p.rect(px, py, pw, ph, 5);

        p.fill(255);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(10);
        p.text("NODE TELEMETRY", px + 10, py + 8);

        p.textStyle(p.NORMAL);
        p.textSize(9);
        p.text("ROLE: " + m.kind.toUpperCase(), px + 10, py + 24);
        p.text("PRESSURE: " + m.pressure.toFixed(2), px + 10, py + 36);
        p.text("HUB SCORE: " + m.hubScore.toFixed(2), px + 10, py + 48);
        p.text("IN/OUT: " + m.inDegree + " / " + m.outDegree, px + 10, py + 60);
    }

    function drawTopologyBadge(m, c) {
        let cx = m.x + 18;
        let cy = m.y + m.h / 2 - 3;

        p.push();
        p.translate(cx, cy);
        p.noFill();
        p.stroke(c);
        p.strokeWeight(1.5);

        if (m.kind === "bootstrap") {
            p.rect(-6, -6, 12, 12);
            p.line(-10, -10, 10, 10);
        } else if (m.kind === "entry") {
            p.rect(-6, -6, 12, 12);
            p.line(-12, 0, -6, 0);
        } else if (m.kind === "process") {
            polygon(0, 0, 7, 6);
        } else if (m.kind === "station") {
            p.rect(-5, -5, 10, 10);
            p.fill(c);
            p.rect(-2, -2, 4, 4);
        } else if (m.kind === "hub") {
            p.ellipse(0, 0, 14, 14);
            p.ellipse(0, 0, 6, 6);
            p.line(-10, 0, 10, 0);
            p.line(0, -10, 0, 10);
        } else if (m.kind === "support") {
            polygon(0, 0, 6, 4);
        } else if (m.kind === "exit") {
            p.triangle(6, 0, -6, -6, -6, 6);
            p.line(10, -6, 10, 6);
        }

        p.pop();
    }

    function polygon(x, y, radius, npoints) {
        let angle = p.TWO_PI / npoints;
        p.beginShape();
        for (let a = 0; a < p.TWO_PI; a += angle) {
            p.vertex(x + p.cos(a) * radius, y + p.sin(a) * radius);
        }
        p.endShape(p.CLOSE);
    }

    function getModuleColor(kind) {
        if (kind === "bootstrap") return palette.bootstrap;
        if (kind === "entry") return palette.entry;
        if (kind === "process") return palette.process;
        if (kind === "station") return palette.station;
        if (kind === "hub") return palette.hub;
        if (kind === "support") return palette.support;
        return palette.exit;
    }

    function getPressureColor(value) {
        if (value < 0.34) return palette.low;
        if (value < 0.67) return palette.mid;
        return palette.high;
    }

    function getLinkColor(type) {
        if (type === "bi") return palette.bi;
        if (type === "uniGhost") return p.color(0, 229, 255, 60);
        return palette.uni;
    }

    function drawTelemetryPanels() {
        drawTopRightSummary();
        drawBottomRightTrafficPanel();
    }

    function drawTopRightSummary() {
        let x = p.width - 285;
        let y = 26;
        let w = 235;
        let h = 132;

        p.noStroke();
        p.fill(4, 10, 16, 220);
        p.rect(x, y, w, h, 5);

        p.stroke(palette.muted);
        p.strokeWeight(1);
        p.noFill();
        p.rect(x, y, w, h, 5);

        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(11);
        p.text("NETWORK SUMMARY", x + 12, y + 10);

        p.textStyle(p.NORMAL);
        p.textSize(10);

        let bootstrapCount = links.filter(l => {
            const from = getModule(l.from);
            return from && from.kind === "bootstrap";
        }).length;

        let uniCount = links.filter(l => l.type === "uni").length;
        let biCount = links.filter(l => l.type === "bi").length;
        let hubs = modules.filter(m => m.kind === "hub").length;
        let stations = modules.filter(m => m.kind === "station").length;
        let exporters = modules.filter(m => m.kind === "exit").length;

        p.fill(255);
        p.text("MODULES: " + modules.length, x + 12, y + 34);
        p.text("BOOTSTRAP ROUTES: " + bootstrapCount, x + 12, y + 50);
        p.text("ROUTES (UNI): " + uniCount, x + 12, y + 66);
        p.text("ROUTES (BI): " + biCount, x + 12, y + 82);
        p.text("STATIONS: " + stations, x + 12, y + 98);
        p.text("HUBS: " + hubs + " // EXPORTERS: " + exporters, x + 12, y + 114);
    }

    function drawBottomRightTrafficPanel() {
        let x = p.width - 315;
        let y = p.height - 170;
        let w = 265;
        let h = 120;

        p.noStroke();
        p.fill(4, 10, 16, 220);
        p.rect(x, y, w, h, 5);

        p.stroke(palette.muted);
        p.strokeWeight(1);
        p.noFill();
        p.rect(x, y, w, h, 5);

        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(11);
        p.text("TOP TRAFFIC NODES", x + 12, y + 10);

        let ranked = [...modules]
            .sort((a, b) => (b.weightedIn + b.weightedOut) - (a.weightedIn + a.weightedOut))
            .slice(0, 4);

        for (let i = 0; i < ranked.length; i++) {
            let m = ranked[i];
            let yy = y + 33 + i * 20;
            let c = getPressureColor(m.pressure);

            p.fill(c);
            p.rect(x + 12, yy + 4, 8, 8, 2);

            p.fill(255);
            p.textStyle(p.NORMAL);
            p.textSize(9.5);
            p.text(m.label.toUpperCase() + "  //  TRAF:" + (m.weightedIn + m.weightedOut), x + 28, yy);
        }
    }

    function drawLegendAdvanced() {
        let x = 50;
        let y = p.height - 94;

        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(11);
        p.text("TOPOLOGY / RISK / ROUTES", x, y - 20);

        const items = [
            ["BOOTSTRAP", palette.bootstrap],
            ["ENTRY", palette.entry],
            ["PROCESS", palette.process],
            ["STATION", palette.station],
            ["HUB", palette.hub],
            ["SUPPORT", palette.support],
            ["EXIT", palette.exit]
        ];

        for (let i = 0; i < items.length; i++) {
            p.stroke(items[i][1]);
            p.strokeWeight(1.4);
            p.noFill();
            p.rect(x, y, 10, 10);

            p.fill(255);
            p.noStroke();
            p.textStyle(p.NORMAL);
            p.textSize(10);
            p.text(items[i][0], x + 16, y);
            x += p.textWidth(items[i][0]) + 56;
        }

        let y2 = p.height - 52;
        p.fill(palette.text);
        p.textStyle(p.BOLD);
        p.textSize(11);
        p.text("WEIGHTED ROUTES", 50, y2 - 18);

        p.stroke(palette.uni);
        p.strokeWeight(3);
        p.line(50, y2, 94, y2);
        p.noStroke();
        p.fill(palette.uni);
        p.triangle(94, y2, 84, y2 - 4, 84, y2 + 4);
        p.fill(255);
        p.textStyle(p.NORMAL);
        p.textSize(10);
        p.text("UNI", 102, y2 - 5);

        p.stroke(palette.bi);
        p.strokeWeight(2);
        p.line(160, y2 - 3, 204, y2 - 3);
        p.line(160, y2 + 3, 204, y2 + 3);
        p.noStroke();
        p.fill(palette.bi);
        p.triangle(204, y2 - 3, 194, y2 - 7, 194, y2 + 1);
        p.triangle(160, y2 + 3, 170, y2 - 1, 170, y2 + 7);
        p.fill(255);
        p.text("BI", 214, y2 - 5);

        p.fill(palette.low);
        p.rect(270, y2 - 5, 10, 10, 2);
        p.fill(255);
        p.text("LOW PRESSURE", 286, y2 - 5);

        p.fill(palette.mid);
        p.rect(405, y2 - 5, 10, 10, 2);
        p.fill(255);
        p.text("MID", 421, y2 - 5);

        p.fill(palette.high);
        p.rect(475, y2 - 5, 10, 10, 2);
        p.fill(255);
        p.text("HIGH", 491, y2 - 5);
    }

    function drawHUDOverlay() {
        let scanY = (p.frameCount * 2) % p.height;
        p.stroke(0, 229, 255, 26);
        p.strokeWeight(4);
        p.line(0, scanY, p.width, scanY);

        p.stroke(0, 0, 0, 38);
        p.strokeWeight(1);
        for (let y = 0; y < p.height; y += 4) p.line(0, y, p.width, y);
    }
};

new p5(routeMapSketch);
</script>
""";

            script = script
                .Replace("__NODES_JSON__", nodesJson)
                .Replace("__EDGES_JSON__", edgesJson);

            sb.AppendLine(script);
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private static string ToId(string module)
        {
            return module
                .Replace(".", "_", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "_", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }
    }
}
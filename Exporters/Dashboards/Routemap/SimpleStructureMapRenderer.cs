using System.Text;
using System.Text.Json;

namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    public sealed class SimpleStructureMapRenderer
    {
        public string Render(SimpleStructureMapModel model, bool usarRefinoHeuristico = false)
        {
            return usarRefinoHeuristico
                ? RenderSimple2(model)
                : RenderBaseline(model);
        }

        public string RenderBaseline(SimpleStructureMapModel model)
        {
            return RenderInternal(model, usarRefinoHeuristico: false);
        }

        public string RenderSimple2(SimpleStructureMapModel model)
        {
            return RenderInternal(model, usarRefinoHeuristico: true);
        }

        private string RenderInternal(SimpleStructureMapModel model, bool usarRefinoHeuristico)
        {
            if (model == null || model.Nodes == null || model.Nodes.Count == 0)
                return string.Empty;

            var refinedNodes = usarRefinoHeuristico
                ? BuildRefinedNodes(model.Nodes)
                : model.Nodes.Select(n => new RefinedSimpleStructureNode
                {
                    Id = n.Id,
                    Label = n.Label,
                    ParentId = n.ParentId,
                    Depth = n.Depth,
                    CsFileCount = n.CsFileCount,
                    VisualKind = InferBaselineVisualKind(n.Depth, n.Label),
                    ZoneLabel = ResolveBaselineZoneLabel(n.Depth),
                    IsExporter = false,
                    IsCentralCandidate = false
                }).ToList();

            var nodesJson = JsonSerializer.Serialize(
                refinedNodes.Select(n => new
                {
                    id = n.Id,
                    label = n.Label,
                    parentId = n.ParentId,
                    depth = n.Depth,
                    csFileCount = n.CsFileCount,
                    visualKind = n.VisualKind,
                    zoneLabel = n.ZoneLabel,
                    isExporter = n.IsExporter,
                    isCentralCandidate = n.IsCentralCandidate
                }));

            var sb = new StringBuilder();

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Simple Structure Map</h2>
        <div class="line"></div>
    </div>
</div>
""");

            sb.AppendLine("""
<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);width:100%;" augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            sb.AppendLine($"""
<h3 style="margin:0 0 6px 0;text-align:center;">{(usarRefinoHeuristico ? "Simple Structure Map 2.0" : "Simple Structure Map")}</h3>
<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">
{(usarRefinoHeuristico
    ? "Refined structural projection using optional visual heuristics for exporter and central candidate highlighting."
    : "Simplified structural projection based on the project tree. Folders without relevant structural backing are omitted.")}
</div>
""");

            sb.AppendLine("""
<style>
#p5-simple-structure-scroll {
    width: 100%;
    max-height: 980px;
    overflow-y: auto;
    overflow-x: hidden;
    display: flex;
    justify-content: center;
    align-items: flex-start;
    padding-right: 6px;
}

#p5-simple-structure-container {
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: flex-start;
}

#p5-simple-structure-container canvas {
    display: block;
    max-width: 100%;
    height: auto !important;
}
</style>
""");

            sb.AppendLine("""
<div id="p5-simple-structure-scroll">
    <div id="p5-simple-structure-container"></div>
</div>
""");

            var script = """
<script>
const simpleStructureNodes = __NODES_JSON__;
const useSimple2 = __USE_SIMPLE2__;

const simpleStructureSketch = (p) => {
    let nodes = [];
    let links = [];
    let palette;
    let selectedNode = null;
    let layoutMetrics = null;

    function estimateLayoutMetrics() {
        const grouped = {};
        let maxDepth = 0;
        let maxColumnCount = 0;

        for (const raw of simpleStructureNodes) {
            if (!grouped[raw.depth]) grouped[raw.depth] = [];
            grouped[raw.depth].push(raw);
            if (raw.depth > maxDepth) maxDepth = raw.depth;
        }

        for (const key of Object.keys(grouped)) {
            maxColumnCount = Math.max(maxColumnCount, grouped[key].length);
        }

        const topMargin = 130;
        const bottomMargin = 180;
        const usableHeight = Math.max(760, maxColumnCount * 78);
        const canvasHeight = topMargin + usableHeight + bottomMargin;

        return {
            maxDepth,
            maxColumnCount,
            topMargin,
            bottomMargin,
            usableHeight,
            canvasHeight
        };
    }

    p.setup = () => {
        layoutMetrics = estimateLayoutMetrics();

        p.createCanvas(1360, layoutMetrics.canvasHeight).parent('p5-simple-structure-container');
        p.pixelDensity(2);
        p.textFont("monospace");
        p.rectMode(p.CORNER);

        palette = {
            bg: p.color("#03060a"),
            grid: p.color(0, 229, 255, 14),
            text: p.color("#00e5ff"),
            muted: p.color("#255b7a"),
            whiteSoft: p.color(230, 245, 255, 180),

            root: p.color("#ff0055"),
            folder: p.color("#00e5ff"),
            file: p.color("#00ff88"),
            deep: p.color("#ffaa00"),

            central: p.color("#ff9a00"),
            exporter: p.color("#00ffcc"),
            centralExporter: p.color("#fff275"),

            link: p.color("#0088ff"),
            fileLink: p.color("#00ffcc")
        };

        buildLayout();
    };

    p.draw = () => {
        p.background(palette.bg);
        drawGrid();
        drawZones();
        drawLinks();
        drawNodes();
        drawLegend();
        drawTitle();
        drawStatsPanel();
        drawHUDOverlay();
    };

    p.mousePressed = () => {
        selectedNode = null;
        for (let node of nodes) {
            if (p.mouseX >= node.x && p.mouseX <= node.x + node.w &&
                p.mouseY >= node.y && p.mouseY <= node.y + node.h) {
                selectedNode = node;
                break;
            }
        }
    };

    function buildLayout() {
        const grouped = {};
        let maxDepth = 0;

        for (const raw of simpleStructureNodes) {
            if (!grouped[raw.depth]) grouped[raw.depth] = [];
            grouped[raw.depth].push(raw);
            if (raw.depth > maxDepth) maxDepth = raw.depth;
        }

        const zoneLeft = 30;
        const zoneRight = 30;
        const zoneGap = 12;
        const zoneTop = 95;
        const zoneInsetX = 12;
        const zoneInsetY = 24;

        const zoneCount = Math.max(1, maxDepth + 1);
        const totalZoneWidth = p.width - zoneLeft - zoneRight;
        const zoneWidth = (totalZoneWidth - ((zoneCount - 1) * zoneGap)) / zoneCount;
        const zoneInnerWidth = zoneWidth - (zoneInsetX * 2);

        nodes = [];
        links = [];

        for (let depth = 0; depth <= maxDepth; depth++) {
            const column = grouped[depth] || [];
            const zoneX = zoneLeft + depth * (zoneWidth + zoneGap);
            const nodeX = zoneX + zoneInsetX;

            const availableColumnHeight = layoutMetrics.usableHeight - (zoneInsetY * 2);
            const spacing = column.length <= 1
                ? availableColumnHeight
                : Math.max(58, availableColumnHeight / column.length);

            for (let i = 0; i < column.length; i++) {
                const raw = column[i];
                const size = getNodeSize(raw, zoneInnerWidth);

                const node = {
                    ...raw,
                    x: nodeX,
                    y: zoneTop + zoneInsetY + i * spacing,
                    w: size.w,
                    h: size.h
                };

                nodes.push(node);
            }
        }

        for (const node of nodes) {
            if (node.parentId) {
                const parent = nodes.find(n => n.id === node.parentId);
                if (parent) {
                    links.push({
                        from: parent.id,
                        to: node.id,
                        type: node.visualKind === "file" ? "file" : "folder"
                    });
                }
            }
        }
    }

    function getNodeSize(raw, zoneInnerWidth) {
        const label = raw.label || "";
        const hardMax = Math.max(140, zoneInnerWidth);
        const preferred = 90 + label.length * 5.2;
        const baseWidth = Math.max(138, Math.min(hardMax, preferred));

        if (raw.visualKind === "root")
            return { w: Math.min(hardMax, Math.max(150, baseWidth)), h: 54 };

        if (raw.visualKind === "file")
            return { w: Math.min(hardMax, Math.max(142, baseWidth - 8)), h: 40 };

        if (raw.visualKind === "deep")
            return { w: Math.min(hardMax, Math.max(140, baseWidth - 4)), h: 44 };

        if (raw.visualKind === "central" || raw.visualKind === "exporter" || raw.visualKind === "centralExporter")
            return { w: Math.min(hardMax, Math.max(150, baseWidth)), h: 50 };

        return { w: Math.min(hardMax, Math.max(142, baseWidth)), h: 46 };
    }

    function getNode(id) {
        return nodes.find(n => n.id === id);
    }

    function drawGrid() {
        p.stroke(palette.grid);
        p.strokeWeight(1);

        for (let x = 0; x < p.width; x += 40) p.line(x, 0, x, p.height);
        for (let y = 0; y < p.height; y += 40) p.line(0, y, p.width, y);

        p.stroke(0, 229, 255, 22);
        p.line(p.width / 2 - 35, p.height / 2, p.width / 2 + 35, p.height / 2);
        p.line(p.width / 2, p.height / 2 - 35, p.width / 2, p.height / 2 + 35);
        p.noFill();
        p.ellipse(p.width / 2, p.height / 2, 48, 48);
    }

    function drawZones() {
        const maxDepth = nodes.length === 0 ? 0 : Math.max(...nodes.map(n => n.depth));

        const zoneLeft = 30;
        const zoneRight = 30;
        const zoneGap = 12;
        const zoneTop = 95;
        const zoneHeight = layoutMetrics.usableHeight + 56;

        const zoneCount = Math.max(1, maxDepth + 1);
        const totalZoneWidth = p.width - zoneLeft - zoneRight;
        const zoneWidth = (totalZoneWidth - ((zoneCount - 1) * zoneGap)) / zoneCount;

        for (let d = 0; d <= maxDepth; d++) {
            const x = zoneLeft + d * (zoneWidth + zoneGap);
            const sample = nodes.find(n => n.depth === d);
            const label = sample ? sample.zoneLabel : resolveZoneLabelFallback(d);

            drawZoneBrackets({
                x: x,
                y: zoneTop,
                w: zoneWidth,
                h: zoneHeight
            });

            p.fill(palette.text);
            p.noStroke();
            p.textAlign(p.LEFT, p.TOP);
            p.textStyle(p.BOLD);
            p.textSize(9.5);
            p.text("SEC_" + d + " // " + label, x + 8, zoneTop - 15);
        }
    }

    function resolveZoneLabelFallback(d) {
        if (!useSimple2) {
            return d === 0 ? "ROOT" :
                   d === 1 ? "MODULE / FOLDER" :
                   d === 2 ? "SUBTREE" :
                   "DEEP STRUCTURE";
        }

        return d === 0 ? "SURFACE" :
               d === 1 ? "INTERNAL" :
               d === 2 ? "DEEP" :
               d === 3 ? "CORE-DEEP" :
               "LEAF";
    }

    function drawZoneBrackets(z) {
        p.stroke(palette.muted);
        p.noFill();
        p.strokeWeight(2);
        let L = 16;

        p.line(z.x, z.y + L, z.x, z.y);
        p.line(z.x, z.y, z.x + L, z.y);

        p.line(z.x + z.w - L, z.y, z.x + z.w, z.y);
        p.line(z.x + z.w, z.y, z.x + z.w, z.y + L);

        p.line(z.x, z.y + z.h - L, z.x, z.y + z.h);
        p.line(z.x, z.y + z.h, z.x + L, z.y + z.h);

        p.line(z.x + z.w - L, z.y + z.h, z.x + z.w, z.y + z.h);
        p.line(z.x + z.w, z.y + z.h - L, z.x + z.w, z.y + z.h);
    }

    function drawTitle() {
        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(22);
        p.text(useSimple2 ? "SIMPLE STRUCTURE MAP 2.0" : "SIMPLE STRUCTURE MAP", 40, 24);

        p.fill(palette.folder);
        p.textStyle(p.NORMAL);
        p.textSize(11);
        p.text(
            useSimple2
                ? "Baseline structural view with optional visual heuristics for exporters and central candidates."
                : "Structural projection of folders and visible structural branches.",
            40, 52);

        p.fill(palette.whiteSoft);
        p.textSize(9.5);
        p.text(
            useSimple2
                ? "Experimental refinement layer over the baseline renderer."
                : "Honest fallback view for projects without a reliable inferred flow topology.",
            40, 68);
    }

    function drawLinks() {
        for (const link of links) {
            const from = getNode(link.from);
            const to = getNode(link.to);

            if (!from || !to) continue;

            const c = link.type === "file" ? palette.fileLink : palette.link;
            const alpha = link.type === "file" ? 110 : 85;

            const cc = p.color(c);
            cc.setAlpha(alpha);

            const x1 = from.x + from.w;
            const y1 = from.y + from.h / 2;
            const x2 = to.x;
            const y2 = to.y + to.h / 2;

            const bend = Math.max(20, Math.abs(x2 - x1) * 0.32);

            p.noFill();
            p.stroke(cc);
            p.strokeWeight(link.type === "file" ? 1.2 : 1.5);
            p.drawingContext.shadowBlur = link.type === "file" ? 4 : 8;
            p.drawingContext.shadowColor = c.toString();

            p.bezier(
                x1, y1,
                x1 + bend, y1,
                x2 - bend, y2,
                x2, y2);

            p.drawingContext.shadowBlur = 0;
            drawArrow(x1, y1, x2, y2, bend, c);
        }
    }

    function drawArrow(x1, y1, x2, y2, bend, c) {
        const t = 0.97;
        const px = p.bezierPoint(x1, x1 + bend, x2 - bend, x2, t);
        const py = p.bezierPoint(y1, y1, y2, y2, t);
        const tx = p.bezierTangent(x1, x1 + bend, x2 - bend, x2, t);
        const ty = p.bezierTangent(y1, y1, y2, y2, t);

        p.push();
        p.translate(px, py);
        p.rotate(p.atan2(ty, tx));
        p.noStroke();
        p.fill(c);
        p.triangle(0, 0, -8, 3.5, -8, -3.5);
        p.pop();
    }

    function drawNodes() {
        for (const node of nodes) {
            drawNode(node);
        }
    }

    function drawNode(node) {
        const c = getNodeColor(node);
        const cut = 10;
        const selected = selectedNode && selectedNode.id === node.id;

        p.drawingContext.shadowBlur = selected ? 18 : 12;
        p.drawingContext.shadowColor = c.toString();

        p.fill(3, 6, 10, 228);
        p.stroke(c);
        p.strokeWeight(selected ? 2.2 : (node.visualKind === "root" ? 1.8 : 1.1));

        p.beginShape();
        p.vertex(node.x + cut, node.y);
        p.vertex(node.x + node.w, node.y);
        p.vertex(node.x + node.w, node.y + node.h - cut);
        p.vertex(node.x + node.w - cut, node.y + node.h);
        p.vertex(node.x, node.y + node.h);
        p.vertex(node.x, node.y + cut);
        p.endShape(p.CLOSE);

        p.drawingContext.shadowBlur = 0;

        p.noStroke();
        p.fill(c);
        p.rect(node.x + cut + 4, node.y, 14, 3);
        p.rect(node.x + cut + 22, node.y, 5, 3);

        drawNodeBadge(node, c);

        p.fill(255);
        p.noStroke();
        p.textAlign(p.LEFT, p.CENTER);
        p.textStyle(p.BOLD);
        p.textSize(node.visualKind === "root" ? 11 : 9.5);

        const displayLabel = shorten(node.label, 18);
        p.text(displayLabel.toUpperCase(), node.x + 28, node.y + node.h / 2 - 5);

        p.fill(palette.text);
        p.textStyle(p.NORMAL);
        p.textSize(7.5);
        p.text(getSubtitle(node), node.x + 28, node.y + node.h / 2 + 8);

        if (selected)
            drawSelectedPanel(node);
    }

    function drawNodeBadge(node, c) {
        const cx = node.x + 14;
        const cy = node.y + node.h / 2 - 1;

        p.push();
        p.translate(cx, cy);
        p.noFill();
        p.stroke(c);
        p.strokeWeight(1.2);

        if (node.visualKind === "root") {
            p.ellipse(0, 0, 12, 12);
            p.line(-8, 0, 8, 0);
            p.line(0, -8, 0, 8);
        } else if (node.visualKind === "folder") {
            p.rect(-5, -4, 10, 8);
        } else if (node.visualKind === "deep") {
            p.polygon = undefined;
            polygon(0, 0, 5, 6);
        } else if (node.visualKind === "file") {
            p.rect(-4, -5, 8, 10);
            p.line(-2, -1, 2, -1);
            p.line(-2, 2, 2, 2);
        } else if (node.visualKind === "central") {
            p.ellipse(0, 0, 10, 10);
            p.line(-6, 0, 6, 0);
            p.line(0, -6, 0, 6);
        } else if (node.visualKind === "exporter") {
            p.triangle(5, 0, -5, -5, -5, 5);
            p.line(6, -5, 6, 5);
        } else if (node.visualKind === "centralExporter") {
            p.ellipse(0, 0, 10, 10);
            p.triangle(8, 0, -1, -5, -1, 5);
        }

        p.pop();
    }

    function polygon(x, y, radius, npoints) {
        const angle = p.TWO_PI / npoints;
        p.beginShape();
        for (let a = 0; a < p.TWO_PI; a += angle) {
            p.vertex(x + p.cos(a) * radius, y + p.sin(a) * radius);
        }
        p.endShape(p.CLOSE);
    }

    function getNodeColor(node) {
        if (node.visualKind === "centralExporter") return palette.centralExporter;
        if (node.visualKind === "exporter") return palette.exporter;
        if (node.visualKind === "central") return palette.central;
        if (node.visualKind === "root") return palette.root;
        if (node.visualKind === "folder") return palette.folder;
        if (node.visualKind === "deep") return palette.deep;
        return palette.file;
    }

    function getSubtitle(node) {
        if (node.visualKind === "root")
            return "ROOT_SCOPE";

        if (node.visualKind === "file")
            return "CS_FILE";

        if (node.visualKind === "centralExporter")
            return "CENTRAL_EXPORT";

        if (node.visualKind === "exporter")
            return "EXPORT_NODE";

        if (node.visualKind === "central")
            return "CENTRAL_CANDIDATE";

        if (node.visualKind === "deep")
            return "DEEP // D:" + node.depth;

        return "FOLDER // D:" + node.depth;
    }

    function shorten(text, maxLen) {
        if (!text) return "";
        if (text.length <= maxLen) return text;
        return text.substring(0, maxLen - 3) + "...";
    }

    function drawSelectedPanel(node) {
        let px = Math.min(node.x + node.w + 12, p.width - 210);
        let py = Math.max(100, node.y - 4);
        let pw = 180;
        let ph = 94;

        p.noStroke();
        p.fill(4, 10, 16, 235);
        p.rect(px, py, pw, ph, 5);

        p.stroke(getNodeColor(node));
        p.strokeWeight(1.1);
        p.noFill();
        p.rect(px, py, pw, ph, 5);

        p.fill(255);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(9.5);
        p.text("NODE TELEMETRY", px + 10, py + 8);

        p.textStyle(p.NORMAL);
        p.textSize(8.5);
        p.text("TYPE: " + node.visualKind.toUpperCase(), px + 10, py + 24);
        p.text("DEPTH: " + node.depth, px + 10, py + 38);
        p.text("EXPORTER: " + (node.isExporter ? "YES" : "NO"), px + 10, py + 52);
        p.text("CENTRAL: " + (node.isCentralCandidate ? "YES" : "NO"), px + 10, py + 66);
        p.text("LABEL: " + shorten(node.label, 18), px + 10, py + 80);
    }

    function drawStatsPanel() {
        const x = p.width - 250;
        const y = p.height - 172;
        const w = 210;
        const h = 130;

        p.noStroke();
        p.fill(4, 10, 16, 220);
        p.rect(x, y, w, h, 5);

        p.stroke(palette.muted);
        p.strokeWeight(1);
        p.noFill();
        p.rect(x, y, w, h, 5);

        const rootCount = nodes.filter(n => n.visualKind === "root").length;
        const folderCount = nodes.filter(n => n.visualKind === "folder").length;
        const deepCount = nodes.filter(n => n.visualKind === "deep").length;
        const centralCount = nodes.filter(n => n.visualKind === "central" || n.visualKind === "centralExporter").length;
        const exporterCount = nodes.filter(n => n.visualKind === "exporter" || n.visualKind === "centralExporter").length;
        const maxDepth = nodes.length === 0 ? 0 : Math.max(...nodes.map(n => n.depth));

        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(10);
        p.text(useSimple2 ? "STRUCTURE SUMMARY 2.0" : "STRUCTURE SUMMARY", x + 10, y + 10);

        p.fill(255);
        p.textStyle(p.NORMAL);
        p.textSize(8.8);
        p.text("ROOTS: " + rootCount, x + 10, y + 32);
        p.text("FOLDERS: " + folderCount, x + 10, y + 48);
        p.text("DEEP: " + deepCount, x + 10, y + 64);
        p.text("EXPORTERS: " + exporterCount, x + 10, y + 80);
        p.text("CENTRAL: " + centralCount, x + 10, y + 96);
        p.text("MAX DEPTH: " + maxDepth + " // LINKS: " + links.length, x + 10, y + 112);
    }

    function drawLegend() {
        let x = 40;
        let y = p.height - 90;

        p.fill(palette.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textStyle(p.BOLD);
        p.textSize(10);
        p.text(useSimple2 ? "STRUCTURE LEGEND 2.0" : "STRUCTURE LEGEND", x, y - 18);

        const items = useSimple2
            ? [
                ["ROOT", palette.root],
                ["FOLDER", palette.folder],
                ["DEEP", palette.deep],
                ["CENTRAL", palette.central],
                ["EXPORTER", palette.exporter],
                ["CENTRAL+EXPORT", palette.centralExporter]
            ]
            : [
                ["ROOT", palette.root],
                ["FOLDER", palette.folder],
                ["DEEP", palette.deep],
                ["CS FILE", palette.file]
            ];

        for (let i = 0; i < items.length; i++) {
            p.stroke(items[i][1]);
            p.strokeWeight(1.2);
            p.noFill();
            p.rect(x, y, 9, 9);

            p.fill(255);
            p.noStroke();
            p.textStyle(p.NORMAL);
            p.textSize(8.5);
            p.text(items[i][0], x + 14, y - 1);

            x += p.textWidth(items[i][0]) + 48;
        }

        let y2 = p.height - 52;

        p.fill(palette.text);
        p.textStyle(p.BOLD);
        p.textSize(10);
        p.text("LINK TYPES", 40, y2 - 16);

        p.stroke(palette.link);
        p.strokeWeight(1.8);
        p.line(40, y2, 76, y2);
        p.noStroke();
        p.fill(palette.link);
        p.triangle(76, y2, 68, y2 - 3.5, 68, y2 + 3.5);
        p.fill(255);
        p.textStyle(p.NORMAL);
        p.textSize(8.5);
        p.text("FOLDER TREE", 84, y2 - 5);

        p.stroke(palette.fileLink);
        p.strokeWeight(1.8);
        p.line(176, y2, 212, y2);
        p.noStroke();
        p.fill(palette.fileLink);
        p.triangle(212, y2, 204, y2 - 3.5, 204, y2 + 3.5);
        p.fill(255);
        p.text("FILE LEAF", 220, y2 - 5);
    }

    function drawHUDOverlay() {
        let scanY = (p.frameCount * 2) % p.height;
        p.stroke(0, 229, 255, 20);
        p.strokeWeight(3);
        p.line(0, scanY, p.width, scanY);

        p.stroke(0, 0, 0, 32);
        p.strokeWeight(1);
        for (let y = 0; y < p.height; y += 4) {
            p.line(0, y, p.width, y);
        }
    }
};

new p5(simpleStructureSketch);
</script>
""";

            script = script
                .Replace("__NODES_JSON__", nodesJson)
                .Replace("__USE_SIMPLE2__", usarRefinoHeuristico ? "true" : "false");

            sb.AppendLine(script);
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        // =====================================================
        // SIMPLE 2.0 HEURISTICS
        // =====================================================

        private static List<RefinedSimpleStructureNode> BuildRefinedNodes(
            IReadOnlyList<SimpleStructureNode> nodes)
        {
            var childrenByParent = nodes
                .GroupBy(n => n.ParentId ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            int CountDescendants(SimpleStructureNode node)
            {
                if (!childrenByParent.TryGetValue(node.Id, out var children))
                    return 0;

                int total = children.Count;

                foreach (var child in children)
                    total += CountDescendants(child);

                return total;
            }

            var refined = new List<RefinedSimpleStructureNode>();

            foreach (var node in nodes)
            {
                var descendants = CountDescendants(node);
                var isExporter = LooksLikeExporter(node.Label);

                var isCentralCandidate =
                    node.Depth > 0 &&
                    (LooksLikeCentralCandidate(node.Label) ||
                     descendants >= 4 ||
                     (isExporter && descendants >= 1));

                refined.Add(new RefinedSimpleStructureNode
                {
                    Id = node.Id,
                    Label = node.Label,
                    ParentId = node.ParentId,
                    Depth = node.Depth,
                    CsFileCount = node.CsFileCount,
                    IsExporter = isExporter,
                    IsCentralCandidate = isCentralCandidate,
                    VisualKind = ResolveRefinedVisualKind(node, isExporter, isCentralCandidate),
                    ZoneLabel = ResolveRefinedZoneLabel(node.Depth)
                });
            }

            return refined;
        }

        private static bool LooksLikeExporter(string label)
        {
            return ContainsAny(label,
                "Exportacao", "Exportação", "Export", "Excel", "Csv", "Json",
                "Writer", "Serializer", "Markdown", "Output", "Saida", "Saída");
        }

        private static bool LooksLikeCentralCandidate(string label)
        {
            return ContainsAny(label,
                "Nucleo", "Núcleo", "Core", "Orquestr", "Orchestr", "Coordinator", "Coordenador");
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var term in terms)
            {
                if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ResolveRefinedVisualKind(
            SimpleStructureNode node,
            bool isExporter,
            bool isCentralCandidate)
        {
            // ROOT sempre vence
            if (node.Depth == 0)
                return "root";

            // arquivo sempre vence
            if (!string.IsNullOrWhiteSpace(node.Label) &&
                node.Label.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return "file";

            if (isExporter && isCentralCandidate)
                return "centralExporter";

            if (isExporter)
                return "exporter";

            if (isCentralCandidate)
                return "central";

            if (node.Depth >= 3)
                return "deep";

            return "folder";
        }

        private static string InferBaselineVisualKind(int depth, string label)
        {
            if (depth == 0)
                return "root";

            if (!string.IsNullOrWhiteSpace(label) &&
                label.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return "file";

            if (depth >= 3)
                return "deep";

            return "folder";
        }

        private static string ResolveBaselineZoneLabel(int depth)
        {
            return depth switch
            {
                0 => "ROOT",
                1 => "MODULE / FOLDER",
                2 => "SUBTREE",
                _ => "DEEP STRUCTURE"
            };
        }

        private static string ResolveRefinedZoneLabel(int depth)
        {
            return depth switch
            {
                0 => "SURFACE",
                1 => "INTERNAL",
                2 => "DEEP",
                3 => "CORE-DEEP",
                _ => "LEAF"
            };
        }

        private sealed class RefinedSimpleStructureNode
        {
            public string Id { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
            public string ParentId { get; init; } = string.Empty;
            public int Depth { get; init; }
            public int CsFileCount { get; init; }
            public bool IsExporter { get; init; }
            public bool IsCentralCandidate { get; init; }
            public string VisualKind { get; init; } = "folder";
            public string ZoneLabel { get; init; } = "STRUCTURE";
        }
    }
}
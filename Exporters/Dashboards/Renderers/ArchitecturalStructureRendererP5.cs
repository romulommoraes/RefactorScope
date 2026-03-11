using RefactorScope.Core.Results;
using System.Text;

namespace RefactorScope.Exporters.Dashboards.Renderers
{
    public sealed class ArchitecturalStructureRendererP5
    {
        public string RenderRadialTree(ProjectStructureResult? structure, string fallbackRootName = "Project")
        {
            var resolvedRootName = ResolveRootNameFromStructure(structure, fallbackRootName);
            var folderPaths = BuildFolderPaths(structure, resolvedRootName);

            var sb = new StringBuilder();

            var jsArray = string.Join(",\n  ",
                folderPaths.Select(p => $"\"{p.Replace("\\", "/").Replace("\"", "\\\"")}\""));

            sb.AppendLine("""
<div class="chart-container"
     style="display:flex;flex-direction:column;align-items:center;justify-content:flex-start;padding:10px 12px 12px 12px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);height:100%;min-height:980px;"
     augmented-ui="tl-clip tr-clip bl-clip br-clip border">
""");

            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;font-size:18px;">Architectural Structure</h3>""");
            sb.AppendLine($"""<div class="chart-note" style="margin:0 0 10px 0;font-size:13px;color:#9fb3c8;text-align:center;">Root: <b>{Html(resolvedRootName)}</b> // domains and hierarchical layout.</div>""");
            sb.AppendLine("""<style>#p5-radial-tree-container canvas { width: 100% !important; max-width: 1400px; height: auto !important; display:block; }</style>""");
            sb.AppendLine("""<div id="p5-radial-tree-container" style="width:100%; display:flex; justify-content:center; align-items:center; flex:1;"></div>""");

            sb.AppendLine($$"""
<script>
const radialTreeSketch = (p) => {
  let folderPaths = [
  {{jsArray}}
  ];

  const BG = "#050814";
  const LABEL_STROKE = "rgba(0,0,0,0.92)";

  const DOMAIN_COLORS = [
    "#00f0ff", "#ff9a3c", "#39ff14", "#6c63ff",
    "#ff003c", "#ffea00", "#d100ff", "#00aeff"
  ];

  let rootNode;
  let allNodes = [];
  let links = [];
  let maxDepth = 0;
  let domains = [];

  p.setup = () => {
    let canvas = p.createCanvas(1400, 1160);
    canvas.parent("p5-radial-tree-container");
    p.angleMode(p.DEGREES);
    p.textFont("monospace");

    buildTree();
    calculateLeafCounts(rootNode);

    let radiusStep = (p.min(p.width, p.height) / 2 - 95) / Math.max(1, maxDepth);
    calculatePositions(rootNode, 0, 360, 0, radiusStep);
  };

  p.draw = () => {
    p.background(BG);
    p.translate(p.width / 2, p.height / 2);

    drawRadarBackground();
    drawDomainShading();
    drawLinks();
    drawNodes();
  };

  function buildTree() {
    rootNode = { name: "{{resolvedRootName}}", children: [], depth: 0, leafCount: 0 };
    allNodes.push(rootNode);

    folderPaths.forEach(path => {
      let normalized = path.replaceAll("\\\\", "/");
      let prefix = "{{resolvedRootName}}/";
      let parts = normalized.startsWith(prefix)
        ? normalized.replace(prefix, "").split("/").filter(Boolean)
        : normalized.split("/").filter(Boolean);

      let currentNode = rootNode;

      parts.forEach((part, index) => {
        let existingChild = currentNode.children.find(c => c.name === part);
        if (!existingChild) {
          let newNode = {
            name: part,
            children: [],
            depth: index + 1,
            parent: currentNode
          };
          currentNode.children.push(newNode);
          allNodes.push(newNode);
          currentNode = newNode;
          if (newNode.depth > maxDepth) maxDepth = newNode.depth;
        } else {
          currentNode = existingChild;
        }
      });
    });
  }

  function calculateLeafCounts(node) {
    if (node.children.length === 0) {
      node.leafCount = 1;
    } else {
      node.leafCount = 0;
      node.children.forEach(c => {
        node.leafCount += calculateLeafCounts(c);
      });
    }
    return node.leafCount;
  }

  function calculatePositions(node, angleStart, angleEnd, depth, radiusStep) {
    let r = depth * radiusStep;
    let angle = (angleStart + angleEnd) / 2;

    if (depth === 0) {
      node.x = 0;
      node.y = 0;
      node.angle = 0;
    } else {
      node.x = p.cos(angle) * r;
      node.y = p.sin(angle) * r;
      node.angle = angle;

      if (depth === 1) {
        let colorIndex = rootNode.children.indexOf(node) % DOMAIN_COLORS.length;
        node.color = DOMAIN_COLORS[colorIndex];
        domains.push({
          name: node.name,
          angleStart: angleStart,
          angleEnd: angleEnd,
          color: node.color,
          radius: maxDepth * radiusStep + 42
        });
      } else {
        node.color = node.parent.color;
      }

      links.push({ source: node.parent, target: node, color: node.color });
    }

    let currentAngle = angleStart;
    node.children.forEach(child => {
      let angleSpan = (child.leafCount / Math.max(1, node.leafCount)) * (angleEnd - angleStart);
      calculatePositions(child, currentAngle, currentAngle + angleSpan, depth + 1, radiusStep);
      currentAngle += angleSpan;
    });
  }

  function drawRadarBackground() {
    p.noFill();
    p.stroke(255, 255, 255, 10);
    p.strokeWeight(1);

    let radiusStep = (p.min(p.width, p.height) / 2 - 95) / Math.max(1, maxDepth);

    for (let i = 1; i <= maxDepth + 1; i++) {
      p.ellipse(0, 0, i * radiusStep * 2);
    }
  }

  function drawDomainShading() {
    p.noStroke();

    domains.forEach(d => {
      let c = p.color(d.color);
      c.setAlpha(16);
      p.fill(c);

      p.beginShape();
      p.vertex(0, 0);
      for (let a = d.angleStart; a <= d.angleEnd; a += 1) {
        p.vertex(p.cos(a) * d.radius, p.sin(a) * d.radius);
      }
      p.endShape(p.CLOSE);

      let edge = p.color(d.color);
      edge.setAlpha(120);
      p.stroke(edge);
      p.strokeWeight(1.6);
      p.line(0, 0, p.cos(d.angleStart) * d.radius, p.sin(d.angleStart) * d.radius);
      p.noStroke();
    });
  }

  function drawLinks() {
    p.noFill();

    links.forEach((link, i) => {
      p.stroke(link.color);
      p.drawingContext.shadowBlur = 10;
      p.drawingContext.shadowColor = link.color;
      p.strokeWeight(p.map(link.target.depth, 1, Math.max(1, maxDepth), 2.2, 0.6));

      p.line(link.source.x, link.source.y, link.target.x, link.target.y);

      let t = (p.frameCount * 0.002 + (i * 0.1)) % 1;

      let px = p.lerp(link.target.x, link.source.x, t);
      let py = p.lerp(link.target.y, link.source.y, t);

      p.drawingContext.shadowColor = "#ffffff";
      p.drawingContext.shadowBlur = 14;
      p.fill(255);
      p.noStroke();
      p.ellipse(px, py, 3.8, 3.8);
    });

    p.drawingContext.shadowBlur = 0;
  }

  function drawNodes() {
    p.drawingContext.shadowBlur = 0;

    allNodes.forEach(node => {
      let nColor = node.depth === 0 ? "#ffffff" : node.color;

      p.drawingContext.shadowBlur = 15;
      p.drawingContext.shadowColor = nColor;
      p.fill(BG);
      p.stroke(nColor);
      p.strokeWeight(2);

      let nodeSize = node.depth === 0 ? 16 : p.map(node.depth, 1, Math.max(1, maxDepth), 12, 6);
      p.ellipse(node.x, node.y, nodeSize, nodeSize);

      p.noStroke();
      p.fill(nColor);
      p.ellipse(node.x, node.y, nodeSize * 0.4);

      p.drawingContext.shadowBlur = 0;

      p.push();
      p.translate(node.x, node.y);

      if (node.depth === 0) {
        drawRootLabel(node);
      } else {
        drawRadialLabel(node);
      }

      p.pop();
    });
  }

  function drawRootLabel(node) {
    let label = node.name;

    p.textAlign(p.CENTER, p.BOTTOM);
    p.textSize(20);
    p.textStyle(p.BOLD);

    p.drawingContext.shadowBlur = 0;
    p.drawingContext.shadowColor = "transparent";

    p.stroke(0, 0, 0, 180);
    p.strokeWeight(2.4);
    p.strokeJoin(p.ROUND);
    p.fill(255);
    p.text(label, 0, -18);

    p.noStroke();
  }

  function drawRadialLabel(node) {
    let isLeftSide = node.angle > 90 && node.angle < 270;
    let textAngle = isLeftSide ? node.angle + 180 : node.angle;
    p.rotate(textAngle);

    let label = node.name;
    let fontSize = p.map(node.depth, 1, Math.max(1, maxDepth), 14, 11.5);
    p.textSize(fontSize);
    p.textStyle(p.BOLD);

    let radialOffset = p.map(node.depth, 1, Math.max(1, maxDepth), 18, 13);
    let perpendicularOffset =
      ((node.depth % 2 === 0) ? 1 : -1) * p.map(node.depth, 1, Math.max(1, maxDepth), 6, 2.5);

    let xOffset = isLeftSide ? -radialOffset : radialOffset;
    let yOffset = perpendicularOffset;

    p.textAlign(isLeftSide ? p.RIGHT : p.LEFT, p.CENTER);

    let isLeaf = node.children.length === 0;

    p.drawingContext.shadowBlur = isLeaf ? 14 : 10;
    p.drawingContext.shadowColor = node.color;

    p.stroke(LABEL_STROKE);
    p.strokeWeight(isLeaf ? 3 : 3.4);
    p.strokeJoin(p.ROUND);

    if (isLeaf) {
      p.fill(255);
    } else {
      p.fill(255, 255, 255, 220);
    }

    p.text(label, xOffset, yOffset);

    p.drawingContext.shadowBlur = 0;
    p.noStroke();
  }
};
new p5(radialTreeSketch);
</script>
""");

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string ResolveRootNameFromStructure(ProjectStructureResult? structure, string fallbackRootName)
        {
            if (structure?.Lines == null || !structure.Lines.Any())
                return fallbackRootName;

            foreach (var rawLine in structure.Lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var label = ExtractTreeLabel(rawLine);
                if (!string.IsNullOrWhiteSpace(label))
                    return label;
            }

            return fallbackRootName;
        }

        private static List<string> BuildFolderPaths(ProjectStructureResult? structure, string rootName)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (structure?.Lines == null || structure.Lines.Count == 0)
                return results.ToList();

            var rootLine = structure.Lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (string.IsNullOrWhiteSpace(rootLine))
                return results.ToList();

            var parsedRoot = ExtractTreeLabel(rootLine);
            if (string.IsNullOrWhiteSpace(parsedRoot))
                parsedRoot = rootName;

            var stack = new List<string> { parsedRoot };

            bool firstLineConsumed = false;

            foreach (var rawLine in structure.Lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var line = rawLine.Replace('\t', ' ');

                if (!line.Contains("├──") && !line.Contains("└──"))
                    continue;

                var label = ExtractTreeLabel(line);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                if (!firstLineConsumed)
                {
                    firstLineConsumed = true;
                    continue; // raiz já está no stack
                }

                if (LooksLikeFile(label))
                    continue;

                var rawDepth = GetTreeDepth(line);
                var effectiveDepth = rawDepth + 1; // filhos imediatos da raiz entram no nível 1

                while (stack.Count > effectiveDepth)
                    stack.RemoveAt(stack.Count - 1);

                if (stack.Count == effectiveDepth)
                {
                    stack.Add(label);
                }
                else if (stack.Count > effectiveDepth)
                {
                    stack[effectiveDepth] = label;
                }
                else
                {
                    while (stack.Count < effectiveDepth)
                        stack.Add("?");
                    stack.Add(label);
                }

                if (stack.Count > 1)
                    results.Add(string.Join("/", stack));
            }

            return results
                .Where(p => p.StartsWith(parsedRoot + "/", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetTreeDepth(string line)
        {
            int depth = 0;
            int index = 0;

            while (index + 4 <= line.Length)
            {
                var chunk = line.Substring(index, 4);

                if (chunk == "│   " || chunk == "    ")
                {
                    depth++;
                    index += 4;
                    continue;
                }

                break;
            }

            return depth;
        }

        private static string ExtractTreeLabel(string line)
        {
            var branchIndex = line.IndexOf("├── ", StringComparison.Ordinal);
            if (branchIndex >= 0)
                return line[(branchIndex + 4)..].Trim();

            branchIndex = line.IndexOf("└── ", StringComparison.Ordinal);
            if (branchIndex >= 0)
                return line[(branchIndex + 4)..].Trim();

            return string.Empty;
        }

        private static bool LooksLikeFile(string label)
        {
            var normalized = label.Trim();

            return normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
        }

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

        // =====================================================
        // 2. INFORMATION FLOW PIPELINE
        // =====================================================
        public string RenderInformationFlowPipeline()
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Control Room // Deep Structure</h2>
        <div class="line"></div>
    </div>
""");

            sb.AppendLine("""<div class="chart-container" style="display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);width:100%;" augmented-ui="tl-clip tr-clip bl-clip br-clip border">""");
            sb.AppendLine("""<h3 style="margin:0 0 6px 0;text-align:center;">Information Flow Pipeline</h3>""");
            sb.AppendLine("""<div class="chart-note" style="margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;">Parsing &rarr; structural model &rarr; analyzers &rarr; reconsolidation &rarr; outputs.</div>""");

            sb.AppendLine("""<style>#p5-pipeline-container canvas { width: 100% !important; max-width: 1600px; height: auto !important; }</style>""");
            sb.AppendLine("""<div id="p5-pipeline-container" style="width:100%; display:flex; justify-content:center; align-items:center;"></div>""");

            sb.AppendLine("""
<script>
const pipelineSketch = (p) => {
    let nodes = [];
    let links = [];
    let particles = [];
    let palette;

    p.setup = () => {
        let canvas = p.createCanvas(1600, 980);
        canvas.parent('p5-pipeline-container');
        p.pixelDensity(2);
        p.textFont("monospace");
        p.rectMode(p.CORNER);
        p.angleMode(p.DEGREES);

        palette = {
            bg: p.color("#0b1020"), 
            grid: p.color(40, 90, 140, 30),
            text: p.color("#eef4ff"),
            muted: p.color("#8fa8ff"),
            parser: p.color("#ff9a3c"),
            parseBranch: p.color("#ffc15d"),
            dto: p.color("#7fd36b"),
            analyzer: p.color("#6ea8ff"),
            derived: p.color("#d08cff"),
            hub: p.color("#ff6a3d"),
            output: p.color("#00e5ff"),
            optional: p.color("#ff9ad5")
        };

        buildLayout();
        buildParticles();
    };

    p.draw = () => {
        p.clear();
        drawBackground();
        drawDomainBands();
        drawLinks();
        drawParticles();
        drawNodes();
        drawLegend();
    };

    function buildLayout() {
        nodes = [
            p.makeNode("startup", "Startup / CLI", 60, 80, 180, 54, "entry"),
            p.makeNode("config", "RunConfiguration", 300, 80, 180, 54, "entry"),
            p.makeNode("scope", "RefactorScopeConfig", 540, 80, 200, 54, "entry"),
            p.makeNode("selector", "ParserSelector", 780, 80, 180, 54, "parser"),
            p.makeNode("strategy", "Parsing Strategy", 1040, 80, 180, 54, "parser"),

            p.makeNode("regex", "Regex Baseline", 1320, 80, 180, 52, "parseBranch"),
            p.makeNode("complexity", "Complexity Scan", 1320, 180, 180, 52, "parseBranch"),
            p.makeNode("textual", "Textual / Selective", 1320, 280, 180, 52, "parseBranch"),

            p.makeNode("merge", "Parsing Merge", 1040, 280, 180, 54, "parser"),
            p.makeNode("parserResult", "IParserResult", 780, 280, 180, 54, "dto"),
            p.makeNode("model", "ModeloEstrutural", 540, 280, 180, 54, "dto"),
            p.makeNode("context", "AnalysisContext", 300, 280, 180, 54, "dto"),

            p.makeNode("structure", "Project Structure", 60, 420, 180, 54, "analyzer"),
            p.makeNode("candidates", "Structural Candidates", 300, 420, 190, 54, "analyzer"),
            p.makeNode("architecture", "Architecture", 540, 420, 160, 54, "analyzer"),
            p.makeNode("coupling", "Coupling", 840, 420, 140, 54, "analyzer"),
            p.makeNode("implicit", "Implicit Coupling", 1080, 420, 180, 54, "analyzer"),

            p.makeNode("refinement", "Refinement", 220, 540, 160, 54, "derived"),
            p.makeNode("fitness", "Fitness Gates", 460, 540, 160, 54, "derived"),
            p.makeNode("hygiene", "Hygiene", 680, 540, 140, 54, "derived"),
            p.makeNode("solid", "SOLID", 920, 540, 120, 54, "derived"),

            p.makeNode("report", "ConsolidatedReport", 540, 680, 240, 64, "hub"),
            p.makeNode("statistics", "Statistics", 1020, 680, 150, 54, "optional"),
            p.makeNode("effort", "Effort", 1260, 680, 120, 54, "optional"),

            p.makeNode("html", "HTML Outputs", 60, 840, 160, 54, "output"),
            p.makeNode("markdown", "Markdown Outputs", 300, 840, 190, 54, "output"),
            p.makeNode("csv", "CSV Outputs", 540, 840, 150, 54, "output"),
            p.makeNode("analyzerOut", "Output Analyzers", 780, 840, 190, 54, "output"),
            p.makeNode("statsOut", "Output Statistics", 1020, 840, 180, 54, "output"),
            p.makeNode("effortOut", "Output Effort", 1260, 840, 170, 54, "output")
        ];

        links = [
            p.makeLink("startup", "config", "main"), p.makeLink("config", "scope", "main"),
            p.makeLink("scope", "selector", "main"), p.makeLink("selector", "strategy", "main"),
            p.makeLink("strategy", "regex", "branch"), p.makeLink("strategy", "complexity", "branch"),
            p.makeLink("strategy", "textual", "branch"), p.makeLink("regex", "merge", "branch"),
            p.makeLink("complexity", "merge", "branch"), p.makeLink("textual", "merge", "branch"),
            p.makeLink("merge", "parserResult", "main"), p.makeLink("parserResult", "model", "main"),
            p.makeLink("model", "context", "main"), p.makeLink("context", "structure", "analyzer"),
            p.makeLink("context", "candidates", "analyzer"), p.makeLink("context", "architecture", "analyzer"),
            p.makeLink("context", "coupling", "analyzer"), p.makeLink("context", "implicit", "analyzer"),
            p.makeLink("candidates", "refinement", "derived"), p.makeLink("coupling", "solid", "derived"),
            p.makeLink("architecture", "fitness", "derived"), p.makeLink("architecture", "hygiene", "derived"),
            p.makeLink("implicit", "hygiene", "derived"), p.makeLink("structure", "report", "return"),
            p.makeLink("refinement", "report", "return"), p.makeLink("fitness", "report", "return"),
            p.makeLink("hygiene", "report", "return"), p.makeLink("solid", "report", "return"),
            p.makeLink("candidates", "report", "return"), p.makeLink("architecture", "report", "return"),
            p.makeLink("coupling", "report", "return"), p.makeLink("implicit", "report", "return"),
            p.makeLink("report", "statistics", "optional"), p.makeLink("report", "effort", "optional"),
            p.makeLink("report", "html", "output"), p.makeLink("report", "markdown", "output"),
            p.makeLink("report", "csv", "output"), p.makeLink("report", "analyzerOut", "output"),
            p.makeLink("statistics", "statsOut", "output"), p.makeLink("effort", "effortOut", "output")
        ];
    }

    function buildParticles() {
        particles = [];
        for (let i = 0; i < links.length; i++) {
            particles.push({
                linkIndex: i,
                t: p.random(),
                speed: p.random(0.002, 0.005),
                size: p.random(3, 6)
            });
        }
    }

    p.makeNode = (id, label, x, y, w, h, kind) => {
        return { id, label, x, y, w, h, kind };
    };

    p.makeLink = (from, to, kind) => {
        return { from, to, kind };
    };

    function getNode(id) {
        return nodes.find(n => n.id === id);
    }

    function drawBackground() {
        p.stroke(palette.grid);
        p.strokeWeight(1);
        for (let x = 0; x < p.width; x += 40) p.line(x, 0, x, p.height);
        for (let y = 0; y < p.height; y += 40) p.line(0, y, p.width, y);
    }

    function drawDomainBands() {
        const bands = [
            { x: 40, y: 45, w: 720, h: 120, title: "CONFIG", c: p.color(100, 140, 255, 12) },
            { x: 760, y: 45, w: 760, h: 310, title: "PARSING", c: p.color(255, 154, 60, 10) },
            { x: 40, y: 380, w: 1240, h: 240, title: "ANALYSIS", c: p.color(110, 168, 255, 10) }
        ];

        for (let b of bands) {
            p.noStroke();
            p.fill(b.c);
            p.rect(b.x, b.y, b.w, b.h, 16);
            p.fill(255, 255, 255, 100);
            p.textAlign(p.LEFT, p.TOP);
            p.textSize(11);
            p.textStyle(p.BOLD);
            p.text(b.title, b.x + 14, b.y + 10);
        }
    }

    function getEdgePoints(a, b) {
        let cx1 = a.x + a.w / 2; let cy1 = a.y + a.h / 2;
        let cx2 = b.x + b.w / 2; let cy2 = b.y + b.h / 2;
        let dx = cx2 - cx1; let dy = cy2 - cy1;
        let isHoriz = p.abs(dx) > p.abs(dy) * 0.8;
        
        let x1 = cx1, y1 = cy1, x2 = cx2, y2 = cy2;
        
        if (isHoriz) {
            x1 = dx > 0 ? a.x + a.w : a.x;
            x2 = dx > 0 ? b.x : b.x + b.w;
        } else {
            y1 = dy > 0 ? a.y + a.h : a.y;
            y2 = dy > 0 ? b.y : b.y + b.h;
        }
        
        let cp1x = x1, cp1y = y1, cp2x = x2, cp2y = y2;
        if (isHoriz) {
            let bend = p.max(40, p.abs(x2 - x1) * 0.4);
            cp1x = x1 + (dx > 0 ? bend : -bend); cp2x = x2 + (dx > 0 ? -bend : bend);
        } else {
            let bend = p.max(40, p.abs(y2 - y1) * 0.4);
            cp1y = y1 + (dy > 0 ? bend : -bend); cp2y = y2 + (dy > 0 ? -bend : bend);
        }
        
        return { x1, y1, cp1x, cp1y, cp2x, cp2y, x2, y2 };
    }

    function drawLinks() {
        for (let link of links) {
            let a = getNode(link.from);
            let b = getNode(link.to);
            let { x1, y1, cp1x, cp1y, cp2x, cp2y, x2, y2 } = getEdgePoints(a, b);

            let col = getLinkColor(link.kind);
            let alpha = p.map(p.sin(p.frameCount * 2 + a.x * 0.01), -1, 1, 80, 180);
            
            let c = p.color(col); c.setAlpha(alpha);
            p.stroke(c);
            p.strokeWeight(getLinkThickness(link.kind));
            p.noFill();
            p.drawingContext.shadowBlur = 10;
            p.drawingContext.shadowColor = col.toString();
            
            p.bezier(x1, y1, cp1x, cp1y, cp2x, cp2y, x2, y2);
            p.drawingContext.shadowBlur = 0;

            let t = 1.0;
            let tx = p.bezierTangent(x1, cp1x, cp2x, x2, t);
            let ty = p.bezierTangent(y1, cp1y, cp2y, y2, t);
            
            p.push();
            p.translate(x2, y2);
            p.rotate(p.atan2(ty, tx));
            p.noStroke(); p.fill(col);
            p.triangle(0, 0, -10, 5, -10, -5);
            p.pop();
        }
    }

    function drawParticles() {
        for (let pt of particles) {
            let link = links[pt.linkIndex];
            let { x1, y1, cp1x, cp1y, cp2x, cp2y, x2, y2 } = getEdgePoints(getNode(link.from), getNode(link.to));

            pt.t += pt.speed;
            if (pt.t > 1) pt.t = 0;

            let px = p.bezierPoint(x1, cp1x, cp2x, x2, pt.t);
            let py = p.bezierPoint(y1, cp1y, cp2y, y2, pt.t);

            let col = getLinkColor(link.kind);
            p.noStroke(); p.fill(255);
            p.drawingContext.shadowBlur = 15;
            p.drawingContext.shadowColor = col.toString();
            p.ellipse(px, py, pt.size);
            p.drawingContext.shadowBlur = 0;
        }
    }

    function drawNodes() {
        for (let node of nodes) drawSciFiNode(node);
    }

    function drawSciFiNode(node) {
        let col = getNodeColor(node.kind);
        let pulse = p.map(p.sin(p.frameCount * 2 + node.x * 0.05), -1, 1, 5, 16);
        let c = 12; 

        p.noStroke(); p.fill(p.red(col), p.green(col), p.blue(col), 15);
        p.rect(node.x - 5, node.y - 5, node.w + 10, node.h + 10, 16);

        p.fill(12, 18, 30, 240);
        p.stroke(col);
        p.strokeWeight(node.kind === "hub" ? 2.2 : 1.4);
        p.drawingContext.shadowBlur = pulse;
        p.drawingContext.shadowColor = col.toString();

        p.beginShape();
        p.vertex(node.x + c, node.y);
        p.vertex(node.x + node.w, node.y);
        p.vertex(node.x + node.w, node.y + node.h - c);
        p.vertex(node.x + node.w - c, node.y + node.h);
        p.vertex(node.x, node.y + node.h);
        p.vertex(node.x, node.y + c);
        p.endShape(p.CLOSE);

        p.drawingContext.shadowBlur = 0;

        p.noStroke(); p.fill(col);
        p.rect(node.x + 8, node.y + 12, 4, node.h - 24, 2);

        p.fill(255); p.noStroke(); p.textAlign(p.LEFT, p.CENTER);
        p.textSize(node.kind === "hub" ? 15 : 12);
        p.textStyle(p.BOLD);
        p.text(node.label, node.x + 22, node.y + node.h / 2 - 4);

        p.fill(255, 255, 255, 130);
        p.textSize(10); p.textStyle(p.NORMAL);
        p.text(getSubtitle(node.kind), node.x + 22, node.y + node.h / 2 + 12);
    }

    function getSubtitle(kind) {
        if (kind === "entry") return "startup / config";
        if (kind === "parser") return "flow control";
        if (kind === "parseBranch") return "parser branch";
        if (kind === "dto") return "data carrier";
        if (kind === "analyzer") return "domain reader";
        if (kind === "derived") return "derived signal";
        if (kind === "hub") return "reconsolidation point";
        if (kind === "optional") return "optional branch";
        if (kind === "output") return "projection";
        return "";
    }

    function getNodeColor(k) {
        return k==="entry"?palette.muted : k==="parser"?palette.parser : k==="parseBranch"?palette.parseBranch : k==="dto"?palette.dto : k==="analyzer"?palette.analyzer : k==="derived"?palette.derived : k==="hub"?palette.hub : k==="optional"?palette.optional : palette.output;
    }
    function getLinkColor(k) {
        return k==="main"?palette.parser : k==="branch"?palette.parseBranch : k==="analyzer"?palette.analyzer : k==="derived"?palette.derived : k==="return"?palette.hub : k==="optional"?palette.optional : palette.output;
    }
    function getLinkThickness(k) {
        return k==="main"?3.5 : k==="return"?3.2 : k==="output"?3.0 : k==="branch"?2.2 : k==="analyzer"?2.1 : k==="derived"?1.8 : 1.9;
    }

    function drawLegend() {
        const items = [
            ["Main Flow", palette.parser], ["Parser Branch", palette.parseBranch],
            ["DTO / Context", palette.dto], ["Analyzer", palette.analyzer],
            ["Derived Signal", palette.derived], ["Consolidation", palette.hub], ["Output", palette.output]
        ];

        let x = 40; let y = p.height - 30;
        for (let i = 0; i < items.length; i++) {
            p.fill(items[i][1]); p.noStroke(); p.rect(x, y, 10, 10, 2);
            p.fill(255, 255, 255, 180); p.textAlign(p.LEFT, p.CENTER); p.textSize(11);
            p.text(items[i][0], x + 16, y + 5);
            x += p.textWidth(items[i][0]) + 50;
        }
    }
};
new p5(pipelineSketch);
</script>
""");

            sb.AppendLine("</div></div>");
            return sb.ToString();
        }
    }
}
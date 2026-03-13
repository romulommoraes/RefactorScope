// ==========================================
// DATA PAYLOAD (Extract from tests-executive-data.json)
// ==========================================
const TEST_DATA = {
    meta: { 
        overallLineCoverage: 46.53, 
        coveredLines: 14812, 
        totalLines: 31832, 
        distance: 3.47, 
        suiteStatus: "healthy-mvp-core" 
    },
    sectors: [
        { name: "Estimation", cov: 91.26, status: "good" },
        { name: "Statistics", cov: 78.57, status: "attention" },
        { name: "Execution", cov: 66.22, status: "attention" },
        { name: "Parsers", cov: 62.90, status: "attention" },
        { name: "Core", cov: 48.89, status: "critical" },
        { name: "Exporters", cov: 46.55, status: "critical" },
        { name: "Analyzers", cov: 33.35, status: "critical" },
        { name: "CLI", cov: 28.29, status: "critical" },
        { name: "Infrastructure", cov: 15.90, status: "critical" }
    ],
    hotspots: [
        { class: "ParserArenaDashboardExporter", method: "GenerateHtml", comp: 108 },
        { class: "TableRenderer", method: "Render", comp: 108 },
        { class: "FileSize", method: "GetSuffix", comp: 79 },
        { class: "CSharpRegexParser", method: "PrepareStructuralScan", comp: 72 },
        { class: "TerminalRenderer", method: "ResolveModuleColor", comp: 66 }
    ],
    topNs: ["Execution.Dump.Strategies", "Execution.Dump", "Estimation.Scoring", "Estimation.Classification", "Exporters.Dumps"],
    botNs: ["Core.Structure", "Exporters.Datasets", "Dashboards.Renderers", "Reporting.Export", "Solid.Rules"],
    criticalMass: [
        { name: "ModuleRouteMapRenderer", lines: 1680, cov: 0.00 },
        { name: "SimpleStructureMapRenderer", lines: 1638, cov: 0.00 },
        { name: "StructuralInventoryExporter", lines: 1418, cov: 3.95 },
        { name: "DashboardMetricsCalculator", lines: 958, cov: 35.49 },
        { name: "QualityDashboardExporter", lines: 888, cov: 0.00 }
    ]
};

// ==========================================
// HUD ENGINE UTILS (OTIMIZADO P/ PERFORMANCE)
// ==========================================

const THEME_PALETTES = {
    'midnight-blue': {
        bg: "rgba(5, 8, 20, 0)", grid: "rgba(0, 229, 255, 0.2)", text: "#eef4ff", muted: "#8fa8ff", accent: "#00e5ff", good: "#00ff88", attention: "#ff9a3c", critical: "#ff3333", panel: "rgba(12, 18, 30, 0.95)", white: "#ffffff"
    },
    'ember-ops': {
        bg: "rgba(5, 8, 20, 0)", grid: "rgba(255, 154, 60, 0.2)", text: "#fff3e0", muted: "#cc8a5c", accent: "#ff9a3c", good: "#00ff88", attention: "#ffaa00", critical: "#ff3333", panel: "rgba(30, 15, 10, 0.95)", white: "#ffffff"
    },
    'neon-grid': {
        bg: "rgba(5, 8, 20, 0)", grid: "rgba(57, 255, 20, 0.2)", text: "#f0fff0", muted: "#7ac97a", accent: "#39ff14", good: "#39ff14", attention: "#ffea00", critical: "#ff0055", panel: "rgba(10, 25, 10, 0.95)", white: "#ffffff"
    }
};

function getDynamicTheme() {
    let themeKey = document.documentElement.getAttribute('data-dashboard-theme') || 'midnight-blue';
    return THEME_PALETTES[themeKey] || THEME_PALETTES['midnight-blue'];
}

function drawSpaceshipBezel(p, w, h, theme) {
    p.push();
    p.stroke(theme.grid);
    p.strokeWeight(1);
    p.noFill();
    
    p.drawingContext.setLineDash([2, 8]);
    p.line(w/2, 0, w/2, h);
    p.line(0, h/2, w, h/2);
    p.drawingContext.setLineDash([]);

    let cL = 12;
    p.stroke(theme.accent);
    p.strokeWeight(2);
    p.line(6, 6, 6 + cL, 6); p.line(6, 6, 6, 6 + cL);
    p.line(w-6, 6, w-6-cL, 6); p.line(w-6, 6, w-6, 6+cL);
    p.line(6, h-6, 6+cL, h-6); p.line(6, h-6, 6, h-6-cL);
    p.line(w-6, h-6, w-6-cL, h-6); p.line(w-6, h-6, w-6, h-6-cL);

    if (p.frameCount % 60 < 30) {
        p.noStroke();
        p.fill(theme.critical);
        p.ellipse(w - 15, 15, 4, 4);
        p.fill(theme.text);
        p.textSize(8);
        p.textFont("monospace");
        p.textAlign(p.RIGHT, p.CENTER);
        p.text("REC", w - 22, 15);
    }
    p.pop();
}

function drawCyberTooltip(p, title, lines, mx, my, accentColor, logicalW, logicalH, theme) {
    p.push();
    p.textFont("monospace");
    p.textSize(9);
    
    let maxW = p.textWidth(title);
    lines.forEach(l => {
        let w = p.textWidth(l);
        if(w > maxW) maxW = w;
    });
    
    let tw = maxW + 20;
    let th = 22 + (lines.length * 14) + 8;
    
    let tx = mx + 10;
    let ty = my + 10;
    if (tx + tw > logicalW) tx = mx - tw - 10;
    if (ty + th > logicalH) ty = my - th - 10;

    p.drawingContext.shadowBlur = 10;
    p.drawingContext.shadowColor = accentColor;
    p.fill(theme.panel);
    p.stroke(accentColor);
    p.strokeWeight(1);
    p.rect(tx, ty, tw, th);
    p.drawingContext.shadowBlur = 0;

    p.noStroke();
    p.fill(accentColor);
    p.rect(tx, ty, 3, th);

    p.fill(theme.white);
    p.textAlign(p.LEFT, p.TOP);
    p.textStyle(p.BOLD);
    p.text(title, tx + 8, ty + 6);

    p.textStyle(p.NORMAL);
    p.fill(theme.text);
    lines.forEach((l, i) => {
        p.text(l, tx + 8, ty + 22 + (i * 14));
    });
    p.pop();
}

const PANEL_W = 400;
const PANEL_H = 260;

// ==========================================
// 1. GLOBAL COVERAGE GAUGE
// ==========================================
const globalCoverageSketch = (p) => {
    let coverage = TEST_DATA.meta.overallLineCoverage;
    let target = 50.0;
    
    p.setup = () => {
        let container = document.getElementById('p5-global-coverage');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-global-coverage');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-global-coverage');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);
        
        let mx = p.mouseX / scaleFactor;
        let my = p.mouseY / scaleFactor;
        let cx = PANEL_W / 2;
        let cy = PANEL_H / 2;

        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);

        p.translate(cx, cy);
        
        let radius = 70;
        let d = p.dist(mx, my, cx, cy);
        let isHover = d > radius - 20 && d < radius + 20;
        
        p.push();
        p.rotate(p.frameCount * 0.5);
        p.noFill();
        p.stroke(T.grid);
        p.strokeWeight(1);
        p.drawingContext.setLineDash([4, 10]);
        p.ellipse(0, 0, radius * 2 - 25);
        p.drawingContext.setLineDash([]);
        p.pop();

        p.noFill();
        p.strokeWeight(10);
        p.stroke(T.grid);
        p.arc(0, 0, radius * 2, radius * 2, 135, 405);
        
        let targetAngle = p.map(target, 0, 100, 135, 405);
        p.stroke(T.white);
        p.strokeWeight(2);
        let tx = p.cos(targetAngle) * (radius + 10);
        let ty = p.sin(targetAngle) * (radius + 10);
        p.line(0, 0, tx, ty);
        
        let currentCoverage = p.lerp(0, coverage, p.frameCount * 0.03);
        if (currentCoverage > coverage) currentCoverage = coverage;
        let endAngle = p.map(currentCoverage, 0, 100, 135, 405);
        
        let arcColor = currentCoverage >= target ? T.good : T.attention;
        
        p.drawingContext.shadowBlur = isHover ? 20 : 10;
        p.drawingContext.shadowColor = arcColor;
        p.stroke(isHover ? T.white : arcColor);
        p.strokeWeight(10);
        p.arc(0, 0, radius * 2, radius * 2, 135, endAngle);
        p.drawingContext.shadowBlur = 0;
        
        p.noStroke();
        p.fill(T.white);
        p.textAlign(p.CENTER, p.CENTER);
        p.textSize(28);
        p.text(currentCoverage.toFixed(1) + "%", 0, -8);
        
        p.fill(T.accent);
        p.textSize(8);
        p.text("SYS_COVERAGE", 0, 18);
        
        p.fill(currentCoverage >= target ? T.good : T.critical);
        p.textSize(7);
        let diff = (target - currentCoverage).toFixed(2);
        p.text(currentCoverage >= target ? "TARGET SECURED" : `DEFICIT: ${diff}%`, 0, 32);

        p.translate(-cx, -cy); 
        if (isHover) {
            drawCyberTooltip(p, "GLOBAL SENSOR", [
                `CURRENT : ${coverage}%`,
                `TARGET  : ${target}%`
            ], mx, my, arcColor, PANEL_W, PANEL_H, T);
        }
    };
};

// ==========================================
// 2. SECTOR STATUS
// ==========================================
const sectorStatusSketch = (p) => {
    let items = TEST_DATA.sectors;

    p.setup = () => {
        let container = document.getElementById('p5-sector-status');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-sector-status');
        p.textFont("monospace");
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-sector-status');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);
        let mx = p.mouseX / scaleFactor;
        let my = p.mouseY / scaleFactor;

        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);

        let startY = 30;
        let barHeight = 10; 
        let spacing = 22; 
        let maxBarW = PANEL_W - 130;

        let targetX = 100 + (maxBarW * 0.5); 
        p.stroke(T.grid);
        p.strokeWeight(1);
        p.drawingContext.setLineDash([2, 4]);
        p.line(targetX, 20, targetX, PANEL_H - 20);
        p.drawingContext.setLineDash([]);

        p.noStroke();
        let hoveredItem = null;

        items.forEach((item, i) => {
            let y = startY + (i * spacing);
            if (mx > 90 && mx < 100 + maxBarW && my > y && my < y + barHeight) hoveredItem = item;
        });

        items.forEach((item, i) => {
            let y = startY + (i * spacing);
            let isHover = hoveredItem === item;
            let isDimmed = hoveredItem !== null && !isHover;
            
            p.fill(isHover ? T.white : (isDimmed ? T.muted : T.text));
            p.textAlign(p.RIGHT, p.CENTER);
            p.textSize(8);
            p.text(item.name.toUpperCase(), 90, y + barHeight/2);
            
            p.fill(T.grid);
            p.rect(100, y, maxBarW, barHeight);
            
            let col = item.status === "good" ? T.good : item.status === "attention" ? T.attention : T.critical;
            
            let w = p.map(item.cov, 0, 100, 0, maxBarW);
            let currentW = p.lerp(0, w, p.frameCount * 0.05);
            if (currentW > w) currentW = w;
            
            let c = p.color(col); c.setAlpha(isDimmed ? 60 : 255);
            
            p.drawingContext.shadowBlur = isHover ? 15 : 0;
            p.drawingContext.shadowColor = col;
            p.fill(c);
            p.rect(100, y, currentW, barHeight);
            p.drawingContext.shadowBlur = 0;
            
            p.fill(isHover ? T.white : (isDimmed ? "rgba(255,255,255,0.2)" : T.white));
            p.textAlign(p.LEFT, p.CENTER);
            p.text(item.cov.toFixed(1) + "%", 106 + currentW, y + barHeight/2);

            if (isHover) {
                p.noFill(); p.stroke(T.white); p.strokeWeight(1);
                p.line(98, y-2, 104, y-2); p.line(98, y-2, 98, y+4);
                p.line(98, y+barHeight+2, 104, y+barHeight+2); p.line(98, y+barHeight+2, 98, y+barHeight-4);
            }
        });

        if (hoveredItem) {
            let col = hoveredItem.status === "good" ? T.good : hoveredItem.status === "attention" ? T.attention : T.critical;
            drawCyberTooltip(p, hoveredItem.name.toUpperCase(), [`COV: ${hoveredItem.cov}%`, `STATUS: ${hoveredItem.status.toUpperCase()}`], mx, my, col, PANEL_W, PANEL_H, T);
        }
    };
};

// ==========================================
// 3. RISK MATRIX
// ==========================================
const riskMatrixSketch = (p) => {
    let hotspots = TEST_DATA.hotspots;

    p.setup = () => {
        let container = document.getElementById('p5-risk-matrix');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-risk-matrix');
        p.angleMode(p.DEGREES);
        p.textFont("monospace");
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-risk-matrix');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);
        let mx = (p.mouseX / scaleFactor) - (PANEL_W / 2);
        let my = (p.mouseY / scaleFactor) - (PANEL_H / 2);

        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);

        p.translate(PANEL_W / 2, PANEL_H / 2);
        
        p.noFill();
        p.stroke(T.grid);
        p.strokeWeight(1);
        for(let r = 30; r <= 100; r += 23.3) {
            p.ellipse(0, 0, r*2);
        }
        
        let sweepAngle = p.frameCount * 2;
        p.push();
        p.rotate(sweepAngle);
        p.fill(T.accent); p.noStroke();
        p.arc(0, 0, 200, 200, -30, 0); 
        p.stroke(T.accent); p.strokeWeight(2);
        p.line(0, 0, 100, 0);
        p.pop();

        let angleStep = 360 / hotspots.length;
        let hoveredNode = null;
        
        hotspots.forEach((h, i) => {
            let angle = (i * angleStep) - 90;
            let dist = p.map(h.comp, 60, 110, 30, 95); 
            let x = p.cos(angle) * dist;
            let y = p.sin(angle) * dist;
            h.x = x; h.y = y;
            h.angle = angle < 0 ? angle + 360 : angle; 
            if (p.dist(mx, my, x, y) < 15) hoveredNode = h;
        });

        hotspots.forEach((h) => {
            let isHover = hoveredNode === h;
            let angleDiff = Math.abs((sweepAngle % 360) - h.angle);
            let isSwept = angleDiff < 15 || angleDiff > 345;

            p.stroke(T.grid);
            p.line(0, 0, h.x, h.y);
            
            p.drawingContext.shadowBlur = (isHover || isSwept) ? 15 : 5;
            p.drawingContext.shadowColor = T.critical;
            p.fill(isHover ? T.white : T.panel);
            p.stroke(T.critical);
            p.strokeWeight(isHover ? 2 : 1.5);
            let s = (isHover || isSwept) ? 14 : 8;
            p.ellipse(h.x, h.y, s);
            p.drawingContext.shadowBlur = 0;
            
            if (isHover || isSwept) {
                p.fill(T.white); p.noStroke();
                p.textSize(8);
                p.textAlign(h.x > 0 ? p.LEFT : p.RIGHT, p.CENTER);
                let tx = h.x > 0 ? h.x + 10 : h.x - 10;
                let shortClass = h.class.length > 15 ? h.class.substring(0,12)+"..." : h.class;
                p.text(`${shortClass}\nCC:${h.comp}`, tx, h.y);
            }
        });

        if (hoveredNode) {
            p.translate(-PANEL_W/2, -PANEL_H/2);
            drawCyberTooltip(p, hoveredNode.class, [`CC: ${hoveredNode.comp}`], p.mouseX/scaleFactor, p.mouseY/scaleFactor, T.critical, PANEL_W, PANEL_H, T);
        }
    };
};

// ==========================================
// 4. NAMESPACE POLARITY
// ==========================================
const polaritySketch = (p) => {
    let topNs = TEST_DATA.topNs;
    let botNs = TEST_DATA.botNs;

    p.setup = () => {
        let container = document.getElementById('p5-polarity');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-polarity');
        p.textFont("monospace");
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-polarity');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);
        let mx = p.mouseX / scaleFactor;
        let my = p.mouseY / scaleFactor;
        
        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);
        let centerX = PANEL_W / 2;
        
        p.stroke(T.grid); p.strokeWeight(1);
        p.line(centerX, 30, centerX, PANEL_H - 30);
        
        p.noStroke(); p.textSize(8);
        p.fill(T.critical); p.textAlign(p.RIGHT, p.CENTER);
        p.text("0% (CRITICAL)", centerX - 10, 24);
        
        p.fill(T.good); p.textAlign(p.LEFT, p.CENTER);
        p.text("100% (SECURE)", centerX + 10, 24);

        let startY = 50;
        let spacing = 36;
        let barW = 130;
        
        let hoveredIndexL = -1; let hoveredIndexR = -1;

        for(let i = 0; i < 5; i++) {
            let y = startY + (i * spacing);
            if (mx > centerX - barW && mx < centerX && my > y && my < y + 16) hoveredIndexL = i;
            if (mx > centerX && mx < centerX + barW && my > y && my < y + 16) hoveredIndexR = i;
        }

        for(let i = 0; i < 5; i++) {
            let y = startY + (i * spacing);
            
            // LEFT
            let isHoverL = hoveredIndexL === i;
            p.drawingContext.shadowBlur = isHoverL ? 15 : 0;
            p.drawingContext.shadowColor = T.critical;
            let fillL = p.color(T.critical); fillL.setAlpha(isHoverL ? 100 : 30);
            p.fill(fillL); p.stroke(T.critical); p.strokeWeight(isHoverL ? 2 : 1);
            p.rect(centerX - barW, y, barW, 16);
            p.drawingContext.shadowBlur = 0;
            
            p.noStroke(); p.fill(isHoverL ? T.white : T.text);
            p.textAlign(p.RIGHT, p.CENTER); p.textSize(7.5);
            p.text(botNs[i].length > 20 ? botNs[i].substring(0, 17) + "..." : botNs[i], centerX - 6, y + 8);

            // RIGHT
            let isHoverR = hoveredIndexR === i;
            p.drawingContext.shadowBlur = isHoverR ? 15 : 0;
            p.drawingContext.shadowColor = T.good;
            let fillR = p.color(T.good); fillR.setAlpha(isHoverR ? 100 : 30);
            p.fill(fillR); p.stroke(T.good); p.strokeWeight(isHoverR ? 2 : 1);
            p.rect(centerX, y, barW, 16);
            p.drawingContext.shadowBlur = 0;
            
            p.noStroke(); p.fill(isHoverR ? T.white : T.text);
            p.textAlign(p.LEFT, p.CENTER);
            p.text(topNs[i].length > 20 ? topNs[i].substring(0, 17) + "..." : topNs[i], centerX + 6, y + 8);
        }

        if (hoveredIndexL !== -1) drawCyberTooltip(p, botNs[hoveredIndexL], ["COV: 0.00%"], mx, my, T.critical, PANEL_W, PANEL_H, T);
        if (hoveredIndexR !== -1) drawCyberTooltip(p, topNs[hoveredIndexR], ["COV: 100.00%"], mx, my, T.good, PANEL_W, PANEL_H, T);
    };
};

// ==========================================
// 5. CRITICAL MASS DEFICIT
// ==========================================
const criticalMassSketch = (p) => {
    let bubbles = TEST_DATA.criticalMass;

    p.setup = () => {
        let container = document.getElementById('p5-critical-mass');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-critical-mass');
        p.textFont("monospace");
        
        bubbles[0].x = 180; bubbles[0].y = 120; 
        bubbles[1].x = 280; bubbles[1].y = 100; 
        bubbles[2].x = 100; bubbles[2].y = 180; 
        bubbles[3].x = 250; bubbles[3].y = 190; 
        bubbles[4].x = 120; bubbles[4].y = 80; 
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-critical-mass');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);
        let mx = p.mouseX / scaleFactor;
        let my = p.mouseY / scaleFactor;

        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);
        let hoveredBubble = null;

        bubbles.forEach((b) => {
            let r = p.map(b.lines, 800, 1700, 18, 40);
            b.r = r;
            b.dy = b.y + p.sin(p.frameCount * 0.02 + b.x) * 8;
            if (p.dist(mx, my, b.x, b.dy) < r) hoveredBubble = b;
        });

        p.stroke(T.grid); p.strokeWeight(1);
        p.line(bubbles[0].x, bubbles[0].dy, bubbles[1].x, bubbles[1].dy);
        p.line(bubbles[0].x, bubbles[0].dy, bubbles[2].x, bubbles[2].dy);
        p.line(bubbles[0].x, bubbles[0].dy, bubbles[3].x, bubbles[3].dy);
        p.line(bubbles[0].x, bubbles[0].dy, bubbles[4].x, bubbles[4].dy);

        bubbles.forEach((b) => {
            let isHover = hoveredBubble === b;
            let cCol = b.cov === 0 ? T.critical : T.attention;
            
            p.drawingContext.shadowBlur = isHover ? 20 : 5;
            p.drawingContext.shadowColor = cCol;
            let fillC = p.color(cCol); fillC.setAlpha(isHover ? 80 : 20);
            p.fill(fillC); p.stroke(cCol); p.strokeWeight(isHover ? 2 : 1);
            p.ellipse(b.x, b.dy, b.r * 2);
            p.drawingContext.shadowBlur = 0;

            p.stroke(cCol); p.line(b.x - 3, b.dy, b.x + 3, b.dy); p.line(b.x, b.dy - 3, b.x, b.dy + 3);

            if (isHover || b.r > 25) {
                p.fill(T.white); p.noStroke(); p.textAlign(p.CENTER, p.CENTER); p.textSize(8);
                p.text(b.lines + "L", b.x, b.dy + b.r + 8);
            }
        });

        if (hoveredBubble) {
            let col = hoveredBubble.cov === 0 ? T.critical : T.attention;
            drawCyberTooltip(p, hoveredBubble.name, [`MASS: ${hoveredBubble.lines}`, `COV: ${hoveredBubble.cov}%`], mx, my, col, PANEL_W, PANEL_H, T);
        }
    };
};

// ==========================================
// 6. TELEMETRY HUB (FIXED & SCROLLING)
// ==========================================
const telemetryHubSketch = (p) => {
    let logs = [
        "> SYSTEM BOOT SEQUENCE INITIATED",
        "> CONNECTING TO MATRICES...",
        "> DATA FETCH: SUCCESS",
        "> AGGREGATING COVERAGE METRICS..."
    ];

    let options = [
        `> EXTRACTED ${TEST_DATA.meta.totalLines} TOTAL LINES`,
        `> FOUND ${TEST_DATA.meta.coveredLines} COVERED LINES`,
        `> DISTANCE TO TARGET: ${TEST_DATA.meta.distance}%`,
        "> CHECKING HOTSPOTS...",
        "> VERIFYING POLARITY...",
        `> SUITE: ${TEST_DATA.meta.suiteStatus.toUpperCase()}`
    ];
    
    let optionIndex = 0;

    p.setup = () => {
        let container = document.getElementById('p5-telemetry-hub');
        let w = (container && container.clientWidth > 0) ? container.clientWidth : PANEL_W;
        let h = w * (PANEL_H / PANEL_W);
        p.createCanvas(w, h).parent('p5-telemetry-hub');
        p.textFont("monospace");
    };

    p.windowResized = () => {
        let container = document.getElementById('p5-telemetry-hub');
        if(container && container.clientWidth > 0) {
            let w = container.clientWidth;
            let h = w * (PANEL_H / PANEL_W);
            p.resizeCanvas(w, h);
        }
    };

    p.draw = () => {
        p.clear();
        let T = getDynamicTheme();
        let scaleFactor = p.width / PANEL_W;
        p.scale(scaleFactor);

        drawSpaceshipBezel(p, PANEL_W, PANEL_H, T);

        if (p.frameCount % 60 === 0) {
            logs.push(options[optionIndex]);
            optionIndex = (optionIndex + 1) % options.length;
            if (logs.length > 9) logs.shift(); 
        }

        p.fill(T.text);
        p.noStroke();
        p.textAlign(p.LEFT, p.TOP);
        p.textSize(10); 
        
        let startY = 25;
        for (let i = 0; i < logs.length; i++) {
            p.text(logs[i], 25, startY + (i * 18));
        }

        if (p.frameCount % 60 < 30) {
            p.fill(T.accent);
            p.rect(25 + p.textWidth(logs[logs.length-1]) + 5, startY + ((logs.length-1) * 18), 6, 10);
        }

        p.stroke(T.accent);
        p.strokeWeight(1.5);
        p.noFill();
        p.beginShape();
        for(let x = 25; x < PANEL_W - 25; x += 4) {
            let y = (PANEL_H - 35) + p.sin(p.frameCount * 0.05 + x * 0.05) * 10;
            if (x > 160 && x < 190) y -= p.random(0, 20);
            p.vertex(x, y);
        }
        p.endShape();
    };
};
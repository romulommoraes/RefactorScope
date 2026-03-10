using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards;

public sealed class ParsingControlChartsRenderer
{
    // =====================================================
    // PALETA VISUAL HÍBRIDA
    // =====================================================

    private const string AccentPrimary = "#ff9a3c";
    private const string AccentSoft = "#ffb14d";
    private const string AccentWarm = "#ffc15d";
    private const string AccentHot = "#ff8a3d";
    private const string AccentRisk = "#ff6a3d";

    // semântica contaminada pela paleta do tema
    private const string SemanticSafe = "#7fd36b";
    private const string SemanticComplex = "#6ea8ff";
    private const string SemanticPattern = "#ffc15d";
    private const string SemanticRecovery = "#ff9a52";
    private const string SemanticRisk = "#ff6a3d";

    private const string GridStroke = "rgba(210,120,45,0.34)";
    private const string AxisStroke = "rgba(210,125,48,0.26)";
    private const string SoftText = "#d9c8b8";
    private const string StrongText = "#f2e1d2";
    private const string LabelMuted = "rgba(235,214,194,0.42)";

    // =====================================================
    // RADAR
    // =====================================================

    public string RenderParsingRadarEnhanced(
        double typesPerFile,
        double refsPerType,
        double confidence,
        double msPerType)
    {
        static string F(double value)
            => value.ToString("0.###", CultureInfo.InvariantCulture);

        var labels = new[]
        {
            "Type Density",
            "Graph Rich",
            "Confidence",
            "Efficiency"
        };

        var values = new[]
        {
            Clamp01(typesPerFile / 4.0),
            Clamp01(refsPerType / 3.0),
            Clamp01(confidence),
            Clamp01(1.0 - Math.Min(1.0, msPerType / 50.0))
        };

        const int size = 360;
        const int center = size / 2;
        const int radius = 104;
        const int levels = 5;
        const int footerHeight = 40;

        var sb = new StringBuilder();

        // Ajuste simétrico: Centralização total e estilo de "card"
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:450px;'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Parsing Control Radar</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;max-width:80%;'>Higher area suggests stronger extraction quality and operational stability.</div>");
        sb.AppendLine("<div style='display:flex;justify-content:center;align-items:center;width:100%;'>");

        sb.AppendLine($"""
<svg width="{size}" height="{size + footerHeight}" viewBox="0 0 {size} {size + footerHeight}" style="display:block;border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="parsingRadarBg" cx="50%" cy="45%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="parsingRadarGlow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="4" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{size}" height="{size + footerHeight}" rx="16" fill="url(#parsingRadarBg)" />
""");

        for (var level = levels; level >= 1; level--)
        {
            var rr = radius * (level / (double)levels);
            var points = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                var x = center + rr * Math.Cos(angle);
                var y = center + rr * Math.Sin(angle);
                points.Add($"{F(x)},{F(y)}");
            }

            var fill = level % 2 == 0
                ? "rgba(255,140,40,0.030)"
                : "rgba(255,180,90,0.014)";

            sb.AppendLine($"<polygon points='{string.Join(" ", points)}' fill='{fill}' stroke='{GridStroke}' stroke-width='1' />");
        }

        for (var i = 0; i < values.Length; i++)
        {
            var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
            var x = center + radius * Math.Cos(angle);
            var y = center + radius * Math.Sin(angle);

            sb.AppendLine($"<line x1='{center}' y1='{center}' x2='{F(x)}' y2='{F(y)}' stroke='{AxisStroke}' stroke-width='1' />");
        }

        for (var level = 1; level <= levels; level++)
        {
            var rr = radius * (level / (double)levels);
            var y = center - rr + 11;

            sb.AppendLine($"<text x='{center + 6}' y='{F(y)}' fill='{LabelMuted}' font-size='9'>{level * 20}%</text>");
        }

        for (var i = 0; i < labels.Length; i++)
        {
            var angle = (Math.PI * 2 / labels.Length) * i - Math.PI / 2;
            var lx = center + (radius + 16) * Math.Cos(angle);
            var ly = center + (radius + 16) * Math.Sin(angle);

            var anchor = "middle";
            if (Math.Cos(angle) > 0.35) anchor = "start";
            else if (Math.Cos(angle) < -0.35) anchor = "end";

            sb.AppendLine($"<text x='{F(lx)}' y='{F(ly)}' fill='{StrongText}' font-size='11' text-anchor='{anchor}'>{labels[i]}</text>");
        }

        var polygon = new List<string>();

        for (var i = 0; i < values.Length; i++)
        {
            var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
            var pr = radius * values[i];
            var x = center + pr * Math.Cos(angle);
            var y = center + pr * Math.Sin(angle);

            polygon.Add($"{F(x)},{F(y)}");
        }

        sb.AppendLine($"<polygon points='{string.Join(" ", polygon)}' fill='rgba(255,140,60,0.18)' stroke='{AccentPrimary}' stroke-width='2.4' filter='url(#parsingRadarGlow)'/>");

        for (var i = 0; i < values.Length; i++)
        {
            var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
            var pr = radius * values[i];
            var x = center + pr * Math.Cos(angle);
            var y = center + pr * Math.Sin(angle);

            sb.AppendLine($"""
<circle cx="{F(x)}" cy="{F(y)}" r="4.6" fill="{AccentSoft}" stroke="#fff0d6" stroke-width="1.3">
    <title>{labels[i]} — {(values[i] * 100):0.0}%</title>
</circle>
""");
        }

        sb.AppendLine($"<circle cx='{center}' cy='{center}' r='2.5' fill='rgba(255,240,214,0.92)'/>");

        // Legenda centralizada perfeitamente no eixo X
        sb.AppendLine($"""
<g transform="translate({center}, {size + 18})">
    <rect x="-95" y="-12" width="12" height="12" rx="2" fill="rgba(255,140,60,0.18)" stroke="{AccentPrimary}" stroke-width="1.2"/>
    <text x="-75" y="-2" fill="{SoftText}" font-size="10" text-anchor="start">Normalized parsing signal distribution</text>
</g>
""");

        sb.AppendLine("</svg>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        return sb.ToString();
    }

    // =====================================================
    // DONUT
    // =====================================================

    public string RenderParsingRouteDonut(
        int safeFiles,
        int complexFiles,
        int astPatternFiles,
        int recoveryFiles,
        int riskFiles)
    {
        var segments = new[]
        {
            new Segment("Safe", safeFiles, SemanticSafe),
            new Segment("Complex", complexFiles, SemanticComplex),
            new Segment("AST / Pattern", astPatternFiles, SemanticPattern),
            new Segment("Recovery", recoveryFiles, SemanticRecovery),
            new Segment("Risk", riskFiles, SemanticRisk)
        };

        return RenderDonutChart(
            "Route Distribution",
            "How the parsing workload was distributed across execution routes.",
            segments);
    }

    private static string RenderDonutChart(
        string title,
        string subtitle,
        IReadOnlyList<Segment> segments)
    {
        static string F(double value)
            => value.ToString("0.###", CultureInfo.InvariantCulture);

        var total = Math.Max(1, segments.Sum(s => s.Value));

        // Ajustado para manter simetria total com o Radar (360x400)
        var width = 360;
        var cx = width / 2.0;
        var cy = 160.0; // Centralizado verticalmente melhor
        var radius = 85.0; // Levemente maior para preencher melhor o espaço
        var strokeWidth = 24.0;
        var circumference = 2 * Math.PI * radius;

        var chartHeight = 360;
        var footerHeight = 40;

        double offset = 0;
        var sb = new StringBuilder();

        // Container simétrico ao do Radar
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);min-height:450px;'>");
        sb.AppendLine($"<h3 style='margin:0 0 6px 0;text-align:center;'>{title}</h3>");
        sb.AppendLine($"<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;max-width:80%;'>{subtitle}</div>");
        sb.AppendLine("<div style='display:flex;justify-content:center;align-items:center;width:100%;'>");

        sb.AppendLine($"""
<svg width="{width}" height="{chartHeight + footerHeight}" viewBox="0 0 {width} {chartHeight + footerHeight}" style="display:block;border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="donutBg" cx="50%" cy="42%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="donutGlow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="5" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{width}" height="{chartHeight + footerHeight}" rx="16" fill="url(#donutBg)" />
""");

        sb.AppendLine($"""
    <circle cx="{F(cx)}" cy="{F(cy)}" r="{F(radius)}"
            fill="none"
            stroke="rgba(255,255,255,0.07)"
            stroke-width="{F(strokeWidth)}" />
""");

        foreach (var segment in segments)
        {
            if (segment.Value <= 0)
                continue;

            var fraction = segment.Value / (double)total;
            var dash = circumference * fraction;
            var gap = circumference - dash;

            sb.AppendLine($"""
    <circle cx="{F(cx)}" cy="{F(cy)}" r="{F(radius)}"
            fill="none"
            stroke="{segment.Color}"
            stroke-width="{F(strokeWidth)}"
            stroke-linecap="round"
            stroke-dasharray="{F(dash)} {F(gap)}"
            stroke-dashoffset="-{F(offset)}"
            transform="rotate(-90 {F(cx)} {F(cy)})"
            filter="url(#donutGlow)">
        <title>{segment.Name}: {segment.Value}</title>
    </circle>
""");

            offset += dash;
        }

        sb.AppendLine($"""
    <circle cx="{F(cx)}" cy="{F(cy)}" r="{F(radius - (strokeWidth / 2) - 8)}"
            fill="rgba(20,14,10,0.88)"
            stroke="rgba(255,177,77,0.10)"
            stroke-width="1" />

    <text x="{F(cx)}" y="{F(cy - 2)}" text-anchor="middle" fill="#f4e6d8" font-size="28" font-weight="700">{total}</text>
    <text x="{F(cx)}" y="{F(cy + 18)}" text-anchor="middle" fill="#d9c8b8" font-size="12">Total Routed</text>
""");

        // Legenda distribuída em 2 linhas para perfeita simetria visual
        var legendY = chartHeight - 20;
        var itemSpacing = 95;

        int itemsPerRow = 3;
        for (var i = 0; i < segments.Count; i++)
        {
            int row = i / itemsPerRow;
            int col = i % itemsPerRow;
            int itemsInThisRow = Math.Min(itemsPerRow, segments.Count - (row * itemsPerRow));

            double totalLegendWidth = (itemsInThisRow - 1) * itemSpacing;
            double startX = cx - (totalLegendWidth / 2.0);

            var x = startX + (col * itemSpacing);
            var y = legendY + (row * 24);
            var segment = segments[i];

            sb.AppendLine($"""
    <g transform="translate({F(x)},{F(y)})">
        <rect x="-8" y="-11" width="10" height="10" rx="2"
              fill="{segment.Color}"
              stroke="rgba(255,240,214,0.65)"
              stroke-width="0.8" />
        <text x="8" y="-2" fill="#d9c8b8" font-size="10" text-anchor="start">{segment.Name}</text>
    </g>
""");
        }

        sb.AppendLine("</svg>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        return sb.ToString();
    }

    // =====================================================
    // HEAT STRIP
    // =====================================================

    public string RenderRiskHeatStrip(
        int safeFiles,
        int moderateRiskFiles,
        int sparseFiles,
        int anomalyFiles,
        int recoveryDependentFiles)
    {
        var buckets = new[]
        {
            new StripBucket("Safe", safeFiles, SemanticSafe),
            new StripBucket("Moderate", moderateRiskFiles, SemanticComplex),
            new StripBucket("Sparse", sparseFiles, SemanticPattern),
            new StripBucket("Anomaly", anomalyFiles, SemanticRecovery),
            new StripBucket("Recovery Dep.", recoveryDependentFiles, SemanticRisk)
        };

        var total = Math.Max(1, buckets.Sum(b => b.Value));
        var sb = new StringBuilder();

        // Container ajustado ao padrão
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Parsing Risk Heat Strip</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>Operational buckets showing where reliability pressure is concentrated.</div>");

        sb.AppendLine("""
<div style="padding:22px;border-radius:16px;background:linear-gradient(180deg,rgba(18,21,25,0.96),rgba(10,12,14,0.96));border:1px solid rgba(210,120,45,0.20);">
""");

        sb.AppendLine("""
<div style="display:flex;height:34px;border-radius:12px;overflow:hidden;border:1px solid rgba(255,255,255,0.08);background:rgba(255,255,255,0.03);">
""");

        foreach (var bucket in buckets)
        {
            var size = Math.Max(4, (int)Math.Round((bucket.Value / (double)total) * 100));

            sb.AppendLine($"""
<div title="{bucket.Name}: {bucket.Value}" style="width:{size}%;background:{bucket.Color};display:flex;align-items:center;justify-content:center;font-size:11px;color:#08111f;font-weight:700;box-shadow:0 0 18px {bucket.Color};">
    {(bucket.Value > 0 ? bucket.Value.ToString() : string.Empty)}
</div>
""");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("<div style='height:20px;'></div>");

        // Área de diagnóstico usando flex-wrap para centralizar elementos de forma simétrica
        sb.AppendLine("<div style='display:flex;flex-wrap:wrap;justify-content:center;gap:12px;'>");

        foreach (var bucket in buckets)
        {
            sb.AppendLine($"""
<div style="display:flex;align-items:center;gap:10px;padding:8px 14px;border-radius:10px;background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.06);min-width:140px;justify-content:space-between;">
    <div style="display:flex;align-items:center;gap:10px;">
        <div style="width:12px;height:12px;border-radius:3px;background:{bucket.Color};box-shadow:0 0 12px {bucket.Color};"></div>
        <div style="color:{SoftText};font-size:12px;">{bucket.Name}</div>
    </div>
    <div style="color:#f4e6d8;font-weight:700;">{bucket.Value}</div>
</div>
""");
        }

        sb.AppendLine("</div></div></div>");

        return sb.ToString();
    }

    // =====================================================
    // SANKEY
    // =====================================================

    public string RenderParsingFlowSankey(
        int inputFiles,
        int safeFiles,
        int complexFiles,
        int astPatternFiles,
        int recoveryFiles,
        int extractedTypes,
        int extractedReferences,
        int riskFiles)
    {
        //static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var width = 1180;
        var height = 430;

        var maxFlow = Math.Max(1, new[]
        {
            inputFiles, safeFiles, complexFiles, astPatternFiles, recoveryFiles,
            extractedTypes, extractedReferences, riskFiles
        }.Max());

        double Thickness(int value)
            => 8 + ((value / (double)maxFlow) * 32);

        var sb = new StringBuilder();

        // Container ajustado
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Parsing Flow Sankey</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>Flow of structural extraction from input scope to output graph and risk buckets.</div>");

        sb.AppendLine($"""
<svg width="{width}" height="{height}" viewBox="0 0 {width} {height}" style="width:100%;height:auto;border-radius:16px;overflow:visible;max-width:1180px;">
    <defs>
        <radialGradient id="parsingSankeyBg" cx="50%" cy="45%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="sankeyGlow" x="-30%" y="-30%" width="160%" height="160%">
            <feGaussianBlur stdDeviation="2.2" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{width}" height="{height}" rx="16" fill="url(#parsingSankeyBg)" />
""");

        var nodes = new[]
        {
            new SankeyNode("Input Files", 70, 180, inputFiles, AccentPrimary),

            new SankeyNode("Safe Route", 320, 90, safeFiles, SemanticSafe),
            new SankeyNode("Complex Route", 320, 180, complexFiles, SemanticComplex),
            new SankeyNode("AST / Pattern", 320, 270, astPatternFiles, SemanticPattern),
            new SankeyNode("Recovery", 320, 360, recoveryFiles, SemanticRecovery),

            new SankeyNode("Extracted Types", 700, 140, extractedTypes, SemanticSafe),
            new SankeyNode("Extracted Refs", 700, 290, extractedReferences, SemanticComplex),

            new SankeyNode("Risk Bucket", 1030, 215, riskFiles, SemanticRisk)
        };

        var input = nodes[0];
        var safe = nodes[1];
        var complex = nodes[2];
        var ast = nodes[3];
        var recovery = nodes[4];
        var types = nodes[5];
        var refs = nodes[6];
        var risk = nodes[7];

        sb.AppendLine(RenderFlow(input, safe, Thickness(safeFiles), SemanticSafe));
        sb.AppendLine(RenderFlow(input, complex, Thickness(complexFiles), SemanticComplex));
        sb.AppendLine(RenderFlow(input, ast, Thickness(astPatternFiles), SemanticPattern));
        sb.AppendLine(RenderFlow(input, recovery, Thickness(recoveryFiles), SemanticRecovery));

        sb.AppendLine(RenderFlow(safe, types, Thickness(Math.Min(safeFiles + astPatternFiles, extractedTypes)), SemanticSafe));
        sb.AppendLine(RenderFlow(complex, refs, Thickness(Math.Min(complexFiles + recoveryFiles, extractedReferences)), SemanticComplex));
        sb.AppendLine(RenderFlow(ast, types, Thickness(Math.Min(astPatternFiles, extractedTypes)), SemanticPattern));
        sb.AppendLine(RenderFlow(recovery, risk, Thickness(Math.Max(1, riskFiles)), SemanticRecovery));
        sb.AppendLine(RenderFlow(types, risk, Thickness(Math.Max(1, Math.Min(riskFiles, extractedTypes / 5))), AccentHot));
        sb.AppendLine(RenderFlow(refs, risk, Thickness(Math.Max(1, Math.Min(riskFiles, extractedReferences / 10))), SemanticRisk));

        foreach (var node in nodes)
        {
            sb.AppendLine(RenderSankeyNode(node));
        }

        sb.AppendLine("</svg>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    private static string RenderSankeyNode(SankeyNode node)
    {
        return $"""
<g>
    <rect x="{node.X}" y="{node.Y}" rx="10" ry="10" width="140" height="44"
          fill="rgba(6,12,24,0.92)" stroke="{node.Color}" stroke-width="1.2" style="filter:drop-shadow(0 0 12px {node.Color}55);" />
    <text x="{node.X + 12}" y="{node.Y + 18}" fill="#f4e6d8" font-size="11" font-weight="700">{node.Name}</text>
    <text x="{node.X + 12}" y="{node.Y + 33}" fill="{SoftText}" font-size="11">{node.Value}</text>
</g>
""";
    }

    private static string RenderFlow(
        SankeyNode from,
        SankeyNode to,
        double thickness,
        string color)
    {
        static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var x1 = from.X + 140;
        var y1 = from.Y + 22;
        var x2 = to.X;
        var y2 = to.Y + 22;

        var c1x = x1 + ((x2 - x1) * 0.42);
        var c2x = x1 + ((x2 - x1) * 0.68);

        return $"""
<path d="M {F(x1)} {F(y1)} C {F(c1x)} {F(y1)}, {F(c2x)} {F(y2)}, {F(x2)} {F(y2)}"
      fill="none"
      stroke="{color}"
      stroke-opacity="0.86"
      stroke-width="{F(thickness)}"
      stroke-linecap="round"
      filter="url(#sankeyGlow)" />
""";
    }

    private static double Clamp01(double value)
        => Math.Max(0, Math.Min(1, value));

    private sealed record Segment(string Name, int Value, string Color);
    private sealed record StripBucket(string Name, int Value, string Color);
    private sealed record SankeyNode(string Name, double X, double Y, int Value, string Color);
}
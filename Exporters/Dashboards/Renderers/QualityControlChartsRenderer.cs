using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Dashboards.Renderers;

public sealed class QualityControlChartsRenderer
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

    public string RenderQualityRadar(
        double parserConfidence,
        double effortConfidence,
        double statisticsSupport,
        double gateReadiness,
        double unresolvedPressure,
        double overallReadiness)
    {
        static string F(double value)
            => value.ToString("0.###", CultureInfo.InvariantCulture);

        var labels = new[]
        {
            "Parser",
            "Effort",
            "Statistics",
            "Gates",
            "Unresolved Ctrl",
            "Overall"
        };

        var values = new[]
        {
            Clamp01(parserConfidence),
            Clamp01(effortConfidence),
            Clamp01(statisticsSupport),
            Clamp01(gateReadiness),
            Clamp01(1.0 - unresolvedPressure),
            Clamp01(overallReadiness)
        };

        const int size = 360;
        const int center = size / 2;
        const int radius = 96;
        const int levels = 5;
        const int footerHeight = 40;

        var sb = new StringBuilder();

        // Container simétrico de proporção exata para alinhar com o Sankey
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);height:100%;min-height:450px;box-sizing:border-box;'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Quality Signal Radar</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>Quality posture across the main execution trust dimensions.</div>");

        // Estrutura Lado-a-Lado [Radar + Legenda]
        sb.AppendLine("<div style='display:flex;flex-direction:row;align-items:center;justify-content:center;gap:36px;width:100%;flex:1;flex-wrap:wrap;'>");

        sb.AppendLine($"""
<svg width="{size}" height="{size + footerHeight}" viewBox="0 0 {size} {size + footerHeight}" style="display:block;max-width:100%;height:auto;flex-shrink:0;border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="qualityRadarBg" cx="50%" cy="45%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="qualityRadarGlow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="4" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{size}" height="{size + footerHeight}" rx="16" fill="url(#qualityRadarBg)" />
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

            sb.AppendLine(
                $"<polygon points='{string.Join(" ", points)}' fill='{fill}' stroke='{GridStroke}' stroke-width='1' />");
        }

        for (var i = 0; i < values.Length; i++)
        {
            var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
            var x = center + radius * Math.Cos(angle);
            var y = center + radius * Math.Sin(angle);

            sb.AppendLine(
                $"<line x1='{center}' y1='{center}' x2='{F(x)}' y2='{F(y)}' stroke='{AxisStroke}' stroke-width='1' />");
        }

        for (var level = 1; level <= levels; level++)
        {
            var rr = radius * (level / (double)levels);
            var y = center - rr + 11;

            sb.AppendLine(
                $"<text x='{center + 6}' y='{F(y)}' fill='{LabelMuted}' font-size='9'>{level * 20}%</text>");
        }

        for (var i = 0; i < labels.Length; i++)
        {
            var angle = (Math.PI * 2 / labels.Length) * i - Math.PI / 2;
            var lx = center + (radius + 18) * Math.Cos(angle);
            var ly = center + (radius + 18) * Math.Sin(angle);

            var anchor = "middle";
            if (Math.Cos(angle) > 0.35) anchor = "start";
            else if (Math.Cos(angle) < -0.35) anchor = "end";

            sb.AppendLine(
                $"<text x='{F(lx)}' y='{F(ly)}' fill='{StrongText}' font-size='11' text-anchor='{anchor}'>{labels[i]}</text>");
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

        sb.AppendLine(
            $"<polygon points='{string.Join(" ", polygon)}' fill='rgba(255,140,60,0.18)' stroke='{AccentPrimary}' stroke-width='2.4' filter='url(#qualityRadarGlow)'/>");

        for (var i = 0; i < values.Length; i++)
        {
            var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
            var pr = radius * values[i];
            var x = center + pr * Math.Cos(angle);
            var y = center + pr * Math.Sin(angle);

            sb.AppendLine($"""
<circle cx="{F(x)}" cy="{F(y)}" r="4.5" fill="{AccentSoft}" stroke="#fff0d6" stroke-width="1.2">
    <title>{labels[i]} — {(values[i] * 100):0.0}%</title>
</circle>
""");
        }

        sb.AppendLine($"<circle cx='{center}' cy='{center}' r='2.5' fill='rgba(255,240,214,0.92)'/>");

        sb.AppendLine($"""
<g transform="translate(20,{size + 18})">
    <rect x="0" y="-12" width="12" height="12" rx="2" fill="rgba(255,140,60,0.18)" stroke="{AccentPrimary}" stroke-width="1.2"/>
    <text x="18" y="-2" fill="{SoftText}" font-size="10">Normalized quality signal distribution</text>
</g>
""");

        sb.AppendLine("</svg>");

        // Legenda lateralizada com a paleta híbrida
        sb.AppendLine($"""
<div class="chart-interpretation" style="flex:1;max-width:320px;min-width:240px;">
    <ul style="list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:16px;">
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Parser:</b><br/>Confidence that structural extraction is usable for downstream analysis.</li>
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Effort:</b><br/>Confidence attached to current refactor effort estimation.</li>
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Statistics:</b><br/>Support level of the current statistical payload.</li>
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Gates:</b><br/>Aggregate readiness inferred from fitness gate states.</li>
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Unresolved Ctrl:</b><br/>Inverse pressure of unresolved structural candidates.</li>
        <li style="font-size:12px;color:#c8d6e6;line-height:1.4;"><span style="color:{AccentPrimary};margin-right:8px;font-size:10px;">■</span><b style="color:{StrongText};">Overall:</b><br/>Executive readiness across all quality layers.</li>
    </ul>
</div>
""");

        sb.AppendLine("</div>"); // Fecha o flex row
        sb.AppendLine("</div>"); // Fecha o card

        return sb.ToString();
    }

    // =====================================================
    // FITNESS STRIP
    // =====================================================

    public string RenderFitnessGateStrip(IReadOnlyList<QualityGateVisual> gates)
    {
        var sb = new StringBuilder();

        // Container ajustado
        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Fitness Gate Strip</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>Linear gate board for rapid operational reading.</div>");
        sb.AppendLine("<div style='display:grid;gap:10px;'>");

        foreach (var gate in gates)
        {
            var color = gate.Status switch
            {
                QualityGateStatus.Pass => SemanticSafe,
                QualityGateStatus.Warn => SemanticPattern,
                QualityGateStatus.Fail => SemanticRisk,
                _ => SemanticComplex
            };

            var glow = gate.Status switch
            {
                QualityGateStatus.Pass => "rgba(127,211,107,0.14)",
                QualityGateStatus.Warn => "rgba(255,193,93,0.14)",
                QualityGateStatus.Fail => "rgba(255,106,61,0.14)",
                _ => "rgba(110,168,255,0.14)"
            };

            sb.AppendLine($"""
<div style="display:grid;grid-template-columns:180px 110px 1fr;gap:12px;align-items:center;padding:12px 14px;border-radius:14px;background:linear-gradient(180deg,{glow},rgba(8,12,22,0.92));border:1px solid {color};box-shadow:0 0 24px rgba(0,0,0,0.16);">
    <div style="color:{StrongText};font-weight:700;font-size:12px;">{Html(gate.Name)}</div>
    <div style="display:inline-flex;align-items:center;justify-content:center;padding:6px 10px;border-radius:999px;background:{color};color:#08111f;font-weight:800;font-size:11px;">
        {gate.Status}
    </div>
    <div style="color:{SoftText};font-size:12px;">{Html(gate.Details)}</div>
</div>
""");
        }

        sb.AppendLine("</div></div>");
        return sb.ToString();
    }

    // =====================================================
    // TRUST FLOW SANKEY
    // =====================================================

    public string RenderTrustFlowSankey(
        double parserConfidence,
        double effortConfidence,
        double statisticsSupport,
        double gateReadiness,
        double overallReadiness)
    {
        //static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var parserScore = ToWeight(parserConfidence);
        var effortScore = ToWeight(effortConfidence);
        var statsScore = ToWeight(statisticsSupport);
        var gatesScore = ToWeight(gateReadiness);
        var overallScore = ToWeight(overallReadiness);

        // O canvas interno do SVG foi expandido de 420 para 800 para dar espaço ao "esticamento" orgânico.
        const int width = 800;
        const int height = 400;

        var sb = new StringBuilder();

        sb.AppendLine("<div class='chart-container' style='display:flex;flex-direction:column;align-items:center;padding:24px;background:rgba(20,25,30,0.3);border-radius:16px;border:1px solid rgba(255,255,255,0.05);height:100%;min-height:450px;box-sizing:border-box;'>");
        sb.AppendLine("<h3 style='margin:0 0 6px 0;text-align:center;'>Trust Flow Sankey</h3>");
        sb.AppendLine("<div class='chart-note' style='margin:0 0 24px 0;font-size:12px;color:#9fb3c8;text-align:center;'>How the major support layers contribute to the final execution readiness signal.</div>");

        sb.AppendLine("<div style='display:flex;align-items:center;justify-content:center;width:100%;flex:1;'>");

        // SVG ocupa 90% do card, mas o viewBox desenha a arte usando 800px internamente.
        sb.AppendLine($"""
<svg width="100%" height="100%" viewBox="0 0 {width} {height}" style="display:block;width:90%;max-width:{width}px;height:auto;border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="trustSankeyBg" cx="50%" cy="45%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="trustGlow" x="-30%" y="-30%" width="160%" height="160%">
            <feGaussianBlur stdDeviation="2.2" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{width}" height="{height}" rx="16" fill="url(#trustSankeyBg)" />
""");

        // Nós redistribuídos perfeitamente dentro da malha de 800px.
        // Coluna 1: X = 20, Coluna 2: X = 340, Coluna 3: X = 660. 
        // Cada retângulo tem 120 de largura, gerando um gap exato de 200px entre as colunas.
        var nodes = new[]
        {
            new FlowNode("Parser", 20, 52, parserScore, SemanticComplex),
            new FlowNode("Effort", 20, 126, effortScore, SemanticPattern),
            new FlowNode("Statistics", 20, 200, statsScore, AccentWarm),

            new FlowNode("Fitness Gates", 340, 126, gatesScore, SemanticSafe),

            new FlowNode("Overall Readiness", 660, 126, overallScore, AccentHot)
        };

        sb.AppendLine(RenderTrustFlow(nodes[0], nodes[3], parserScore, SemanticComplex));
        sb.AppendLine(RenderTrustFlow(nodes[1], nodes[3], effortScore, SemanticPattern));
        sb.AppendLine(RenderTrustFlow(nodes[2], nodes[3], statsScore, AccentWarm));
        sb.AppendLine(RenderTrustFlow(nodes[3], nodes[4], gatesScore, SemanticSafe));
        sb.AppendLine(RenderTrustFlow(nodes[1], nodes[4], Math.Max(8, effortScore / 2), AccentSoft));
        sb.AppendLine(RenderTrustFlow(nodes[2], nodes[4], Math.Max(8, statsScore / 2), AccentPrimary));

        foreach (var node in nodes)
        {
            sb.AppendLine($"""
<g>
    <rect x="{node.X}" y="{node.Y}" width="120" height="48" rx="10"
          fill="rgba(6,12,24,0.92)" stroke="{node.Color}" stroke-width="1.2" style="filter:drop-shadow(0 0 12px {node.Color}55);" />
    <text x="{node.X + 10}" y="{node.Y + 18}" fill="{StrongText}" font-size="10.5" font-weight="700">{node.Name}</text>
    <text x="{node.X + 10}" y="{node.Y + 34}" fill="{SoftText}" font-size="10.5">{node.Value}%</text>
</g>
""");
        }

        sb.AppendLine("</svg>");
        sb.AppendLine("</div>"); // Fecha o flex row
        sb.AppendLine("</div>"); // Fecha o card

        return sb.ToString();
    }

    private static string RenderTrustFlow(
        FlowNode from,
        FlowNode to,
        int weight,
        string color)
    {
        static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var thickness = 8 + (weight / 100.0) * 18.0;

        var x1 = from.X + 120; // Largura exata do rect
        var y1 = from.Y + 24;
        var x2 = to.X;
        var y2 = to.Y + 24;

        // Curvatura bezier recalculada para o novo distanciamento longo
        var c1x = x1 + ((x2 - x1) * 0.40);
        var c2x = x1 + ((x2 - x1) * 0.70);

        return $"""
<path d="M {F(x1)} {F(y1)} C {F(c1x)} {F(y1)}, {F(c2x)} {F(y2)}, {F(x2)} {F(y2)}"
      fill="none"
      stroke="{color}"
      stroke-opacity="0.88"
      stroke-width="{F(thickness)}"
      stroke-linecap="round"
      filter="url(#trustGlow)" />
""";
    }

    private static int ToWeight(double value)
        => (int)Math.Round(Clamp01(value) * 100.0);

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

    private static double Clamp01(double value)
        => Math.Max(0, Math.Min(1, value));

    public sealed record QualityGateVisual(
        string Name,
        QualityGateStatus Status,
        string Details);

    public enum QualityGateStatus
    {
        Pass,
        Warn,
        Fail,
        Info
    }

    private sealed record FlowNode(string Name, double X, double Y, int Value, string Color);
}
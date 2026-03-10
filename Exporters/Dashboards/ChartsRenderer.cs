using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RefactorScope.Exporters.Dashboards
{
    /// <summary>
    /// Responsável pela renderização dos gráficos SVG utilizados
    /// nos dashboards HTML do RefactorScope.
    ///
    /// Objetivo atual
    /// --------------
    /// Fornecer visualizações leves, portáveis e sem dependência de
    /// bibliotecas externas de charting, preservando:
    ///
    /// - compatibilidade com exportação HTML estática
    /// - controle visual fino
    /// - integração com o tema visual da suíte de dashboards
    ///
    /// Direção visual adotada
    /// ----------------------
    /// Esta versão assume o estilo visual "enhanced" como padrão,
    /// substituindo os SVGs antigos.
    ///
    /// Os gráficos foram ajustados para:
    /// - dialogar com o tema ember
    /// - ter maior legibilidade em publicação
    /// - ocupar melhor o espaço visual disponível
    /// - evitar aparência excessivamente "raw" ou provisória
    ///
    /// Observação arquitetural
    /// -----------------------
    /// Esta classe renderiza apenas gráficos.
    /// Ela não calcula métricas de domínio.
    ///
    /// Ou seja:
    /// - cálculos analíticos ficam fora daqui
    /// - esta classe apenas converte dados em SVG
    ///
    /// Evolução futura
    /// ---------------
    /// No futuro, esta classe pode ser expandida para suportar:
    ///
    /// - múltiplos estilos de renderização por configuração
    /// - escolha explícita de tema visual por chart
    /// - labels adaptativos
    /// - modos "publication" e "compact"
    /// </summary>
    public sealed class ChartsRenderer
    {
        private const int RadarBaseSize = 410;
        private const int GalaxyBaseWidth = 430;
        private const int GalaxyBaseHeight = 430;

        /// <summary>
        /// Renderiza o radar arquitetural principal.
        ///
        /// Sinais representados
        /// --------------------
        /// - Dead Code Candidates
        /// - Pattern Similarity
        /// - Unresolved
        /// - Namespace Drift
        /// - Global Namespace
        /// - Core Isolation
        /// - Implicit Coupling
        /// - SOLID Alerts
        ///
        /// Interpretação
        /// -------------
        /// O radar mostra distribuição normalizada dos sinais estruturais.
        /// Áreas maiores indicam maior concentração relativa de sinais.
        ///
        /// Importante:
        /// -----------
        /// Nem todo eixo representa "erro".
        /// Alguns, como Core Isolation, exigem leitura contextual.
        /// </summary>
        public string RenderRadarSvgEnhanced(
            HygieneReport hygiene,
            StructuralCandidateAnalysisBreakdown breakdown,
            SolidResult? solid,
            ImplicitCouplingResult? implicitCoupling)
        {
            static double Normalize(int value, int total)
                => total == 0 ? 0 : value / (double)total;

            static string Fmt(double value)
                => value.ToString("0.###", CultureInfo.InvariantCulture);

            var solidAlerts = solid?.Alerts.Count ?? 0;

            var couplingScore = implicitCoupling == null
                ? 0
                : Normalize(implicitCoupling.Suspicions.Count, hygiene.TotalClasses);

            var values = new[]
            {
                Normalize(breakdown.StructuralCandidates, hygiene.TotalClasses),
                Normalize(breakdown.PatternSimilarity, hygiene.TotalClasses),
                Normalize(breakdown.ProbabilisticConfirmed, hygiene.TotalClasses),
                Normalize(hygiene.NamespaceDriftCount, hygiene.TotalClasses),
                Normalize(hygiene.GlobalNamespaceCount, hygiene.TotalClasses),
                Normalize(hygiene.IsolatedCoreCount, hygiene.TotalClasses),
                couplingScore,
                Math.Min(1.0, Normalize(solidAlerts, hygiene.TotalClasses) * 5)
            };

            var labels = new[]
            {
                "Dead Code",
                "Pattern",
                "Unresolved",
                "Drift",
                "Global",
                "Core",
                "Coupling",
                "SOLID"
            };

            var size = RadarBaseSize;
            var center = size / 2;
            var radius = 128;
            var levels = 5;
            var footerHeight = 46;

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Architectural Risk Radar</h3>");
            sb.AppendLine("<div class='chart-note' style='margin:4px 0 12px 0;font-size:12px;color:#9fb3c8;'>Higher area indicates stronger concentration of structural signals.</div>");
            sb.AppendLine("<div class='tooltip'>");

            sb.AppendLine($"""
<svg width="{size}" height="{size + footerHeight}" viewBox="0 0 {size} {size + footerHeight}" style="border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="radarBg" cx="50%" cy="45%" r="75%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="radarGlow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="5" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{size}" height="{size + footerHeight}" rx="16" fill="url(#radarBg)" />
""");

            // Fundo poligonal por níveis
            for (var level = levels; level >= 1; level--)
            {
                var levelRadius = radius * (level / (double)levels);
                var levelPoints = new List<string>();

                for (var i = 0; i < values.Length; i++)
                {
                    var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                    var x = center + levelRadius * Math.Cos(angle);
                    var y = center + levelRadius * Math.Sin(angle);

                    levelPoints.Add($"{Fmt(x)},{Fmt(y)}");
                }

                var fill = level % 2 == 0
                    ? "rgba(255,140,40,0.030)"
                    : "rgba(255,180,90,0.014)";

                sb.AppendLine(
                    $"<polygon points='{string.Join(" ", levelPoints)}' fill='{fill}' stroke='rgba(210,120,45,0.34)' stroke-width='1' />");
            }

            // Eixos
            for (var i = 0; i < values.Length; i++)
            {
                var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                var x = center + radius * Math.Cos(angle);
                var y = center + radius * Math.Sin(angle);

                sb.AppendLine(
                    $"<line x1='{center}' y1='{center}' x2='{Fmt(x)}' y2='{Fmt(y)}' stroke='rgba(210,125,48,0.26)' stroke-width='1' />");
            }

            // Labels de nível
            for (var level = 1; level <= levels; level++)
            {
                var levelRadius = radius * (level / (double)levels);
                var y = center - levelRadius + 11;

                sb.AppendLine(
                    $"<text x='{center + 6}' y='{Fmt(y)}' fill='rgba(235,214,194,0.42)' font-size='9'>{level * 20}%</text>");
            }

            // Labels externos
            for (var i = 0; i < labels.Length; i++)
            {
                var angle = (Math.PI * 2 / labels.Length) * i - Math.PI / 2;
                var lx = center + (radius + 16) * Math.Cos(angle);
                var ly = center + (radius + 16) * Math.Sin(angle);

                var anchor = "middle";
                if (Math.Cos(angle) > 0.35)
                    anchor = "start";
                else if (Math.Cos(angle) < -0.35)
                    anchor = "end";

                sb.AppendLine(
                    $"<text x='{Fmt(lx)}' y='{Fmt(ly)}' fill='#f2e1d2' font-size='11' text-anchor='{anchor}'>" +
                    $"<title>{labels[i]} — {(values[i] * 100):0}% of classes</title>" +
                    $"{labels[i]}</text>");
            }

            // Polígono principal
            var points = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                var pointRadius = radius * values[i];
                var x = center + pointRadius * Math.Cos(angle);
                var y = center + pointRadius * Math.Sin(angle);

                points.Add($"{Fmt(x)},{Fmt(y)}");
            }

            sb.AppendLine(
                $"<polygon points='{string.Join(" ", points)}' fill='rgba(255,140,60,0.18)' stroke='#ff9a3c' stroke-width='2.4' filter='url(#radarGlow)'/>");

            // Pontos marcados
            for (var i = 0; i < values.Length; i++)
            {
                var angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                var pointRadius = radius * values[i];
                var x = center + pointRadius * Math.Cos(angle);
                var y = center + pointRadius * Math.Sin(angle);

                sb.AppendLine($"""
<circle cx="{Fmt(x)}" cy="{Fmt(y)}" r="4.8" fill="#ffb14d" stroke="#fff0d6" stroke-width="1.4">
    <title>{labels[i]} — {(values[i] * 100):0.0}% of classes</title>
</circle>
""");
            }

            // Centro
            sb.AppendLine(
                $"<circle cx='{center}' cy='{center}' r='2.5' fill='rgba(255,240,214,0.92)'/>");

            // Legenda curta
            sb.AppendLine($"""
<g transform="translate(20,{size + 18})">
    <rect x="0" y="-12" width="12" height="12" rx="2" fill="rgba(255,140,60,0.18)" stroke="#ff9a3c" stroke-width="1.2"/>
    <text x="18" y="-2" fill="#d9c8b8" font-size="10">Normalized structural signal distribution</text>
</g>
""");

            sb.AppendLine("</svg>");

            sb.AppendLine(
            @"<span class='tooltiptext'>
Enhanced radar of structural signals.

Interpretation:
• Larger area = stronger concentration of structural findings
• Unresolved is the strongest manual-review bucket
• Core should be interpreted in context, not as an error by itself
</span>");

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>
        /// Renderiza a galaxy arquitetural baseada no modelo A/I
        /// (Abstractness / Instability).
        ///
        /// Leitura do gráfico
        /// ------------------
        /// - eixo X = Instability
        /// - eixo Y = Abstractness
        /// - tamanho da bolha = intensidade de fan-out
        /// - linha tracejada = Main Sequence
        ///
        /// A proposta visual desta versão é servir como figura de leitura
        /// executiva, não apenas como scatter técnico bruto.
        /// </summary>
        public string RenderArchitecturalGalaxyEnhanced(CouplingResult coupling)
        {
            static string Fmt(double value)
                => value.ToString("0.###", CultureInfo.InvariantCulture);

            var width = GalaxyBaseWidth;
            var height = GalaxyBaseHeight;
            var margin = 44;
            var footerHeight = 44;

            var plotWidth = width - (margin * 2);
            var plotHeight = height - (margin * 2);

            var modules = coupling.AbstractnessByModule.Keys
                .Distinct()
                .ToList();

            var rankedModules = modules
                .Select(module => new
                {
                    Name = module,
                    FanOut = coupling.ModuleFanOut.GetValueOrDefault(module),
                    Abstractness = coupling.AbstractnessByModule.GetValueOrDefault(module),
                    Instability = coupling.InstabilityByModule.GetValueOrDefault(module),
                    Distance = coupling.DistanceByModule.GetValueOrDefault(module)
                })
                .OrderByDescending(x => x.FanOut)
                .ThenByDescending(x => x.Distance)
                .ToList();

            var highlightModules = rankedModules
                .Take(4)
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Architectural Galaxy (A/I Distribution)</h3>");
            sb.AppendLine("<div class='chart-note' style='margin:4px 0 12px 0;font-size:12px;color:#9fb3c8;'>Bubble size reflects module fan-out. Warmer colors suggest higher coupling intensity.</div>");
            sb.AppendLine("<div class='tooltip'>");

            sb.AppendLine($"""
<svg width="{width}" height="{height + footerHeight}" viewBox="0 0 {width} {height + footerHeight}" style="border-radius:16px;overflow:visible">
    <defs>
        <radialGradient id="galaxyBg" cx="50%" cy="45%" r="78%">
            <stop offset="0%" stop-color="rgba(120,52,8,0.18)" />
            <stop offset="55%" stop-color="rgba(28,14,6,0.14)" />
            <stop offset="100%" stop-color="rgba(8,5,3,0.03)" />
        </radialGradient>

        <filter id="bubbleGlow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="3.4" result="blur"/>
            <feMerge>
                <feMergeNode in="blur"/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>

    <rect x="0" y="0" width="{width}" height="{height + footerHeight}" rx="16" fill="url(#galaxyBg)" />
""");

            // Zonas suaves de fundo
            sb.AppendLine($"""
<rect x="{margin}" y="{margin}" width="{Fmt(plotWidth)}" height="{Fmt(plotHeight)}" fill="rgba(255,140,40,0.020)" />
<polygon points="{margin},{margin} {width - margin},{height - margin} {margin},{height - margin}"
         fill="rgba(255,180,90,0.030)" />
<polygon points="{margin},{margin} {width - margin},{margin} {width - margin},{height - margin}"
         fill="rgba(255,120,40,0.020)" />
""");

            // Grid e ticks
            foreach (var tick in new[] { 0.0, 0.25, 0.50, 0.75, 1.0 })
            {
                var x = margin + plotWidth * tick;
                var y = margin + plotHeight * (1 - tick);

                sb.AppendLine(
                    $"<line x1='{Fmt(x)}' y1='{margin}' x2='{Fmt(x)}' y2='{height - margin}' stroke='rgba(210,125,48,0.16)' stroke-width='1' />");

                sb.AppendLine(
                    $"<line x1='{margin}' y1='{Fmt(y)}' x2='{width - margin}' y2='{Fmt(y)}' stroke='rgba(210,125,48,0.16)' stroke-width='1' />");

                sb.AppendLine(
                    $"<text x='{Fmt(x)}' y='{height - margin + 16}' fill='rgba(235,214,194,0.52)' font-size='9' text-anchor='middle'>{tick:0.##}</text>");

                sb.AppendLine(
                    $"<text x='{margin - 10}' y='{Fmt(y + 3)}' fill='rgba(235,214,194,0.52)' font-size='9' text-anchor='end'>{tick:0.##}</text>");
            }

            // Eixos
            sb.AppendLine(
                $"<line x1='{margin}' y1='{height - margin}' x2='{width - margin}' y2='{height - margin}' stroke='rgba(235,150,70,0.38)' stroke-width='1.2'/>");

            sb.AppendLine(
                $"<line x1='{margin}' y1='{height - margin}' x2='{margin}' y2='{margin}' stroke='rgba(235,150,70,0.38)' stroke-width='1.2'/>");

            // Main sequence
            sb.AppendLine(
                $"<line x1='{margin}' y1='{margin}' x2='{width - margin}' y2='{height - margin}' stroke='rgba(255,190,100,0.40)' stroke-width='1.2' stroke-dasharray='5,5'/>");

            // Labels dos eixos
            sb.AppendLine(
                $"<text x='{width - 76}' y='{height - 10}' fill='#f2e1d2' font-size='11'>Instability</text>");

            sb.AppendLine(
                $"<text x='10' y='20' fill='#f2e1d2' font-size='11'>Abstractness</text>");

            sb.AppendLine(
                $"<text x='{width - 138}' y='{margin + 12}' fill='rgba(255,205,140,0.58)' font-size='9'>Main Sequence</text>");

            foreach (var module in rankedModules)
            {
                var abstractness = module.Abstractness;
                var instability = module.Instability;
                var fanOut = module.FanOut;

                var x = margin + plotWidth * instability;
                var y = (height - margin) - (plotHeight * abstractness);

                var size = 5 + Math.Min(12, fanOut / 2.8);

                var fill = "#ffb14d";
                var stroke = "#fff0d6";

                if (fanOut > 20)
                {
                    fill = "#ff8a3d";
                    stroke = "#ffe0c2";
                }
                else if (fanOut > 10)
                {
                    fill = "#ffc15d";
                    stroke = "#fff1cc";
                }

                sb.AppendLine($"""
<circle cx="{Fmt(x)}" cy="{Fmt(y)}" r="{Fmt(size)}" fill="{fill}" fill-opacity="0.84" stroke="{stroke}" stroke-width="1.5" filter="url(#bubbleGlow)">
    <title>{module.Name}
Abstractness: {abstractness:0.00}
Instability: {instability:0.00}
Distance: {module.Distance:0.00}
FanOut: {fanOut}</title>
</circle>
""");

                if (highlightModules.Contains(module.Name))
                {
                    var tx = x + size + 6;
                    var ty = y - size - 4;

                    if (tx > width - 90)
                        tx = x - size - 6;

                    var anchor = tx < x ? "end" : "start";

                    sb.AppendLine($"""
<text x="{Fmt(tx)}" y="{Fmt(ty)}" fill="#f4e6d8" font-size="10" text-anchor="{anchor}">{module.Name}</text>
""");
                }
            }

            // Legenda
            var legendX = margin;
            var legendY = height + 18;

            sb.AppendLine($"""
<g transform="translate({Fmt(legendX)},{Fmt(legendY)})">
    <circle cx="8" cy="0" r="5" fill="#ffb14d" fill-opacity="0.84" stroke="#fff0d6" stroke-width="1"/>
    <text x="20" y="4" fill="#d9c8b8" font-size="10">Low coupling</text>

    <circle cx="128" cy="0" r="6.5" fill="#ffc15d" fill-opacity="0.84" stroke="#fff1cc" stroke-width="1"/>
    <text x="142" y="4" fill="#d9c8b8" font-size="10">Moderate coupling</text>

    <circle cx="298" cy="0" r="8" fill="#ff8a3d" fill-opacity="0.84" stroke="#ffe0c2" stroke-width="1"/>
    <text x="312" y="4" fill="#d9c8b8" font-size="10">High coupling</text>
</g>
""");

            sb.AppendLine("</svg>");

            sb.AppendLine(
            @"<span class='tooltiptext'>
Enhanced A/I distribution view.

Interpretation:
• X axis = Instability
• Y axis = Abstractness
• Bubble size = fan-out intensity
• Dashed diagonal = Main Sequence
• Labeled bubbles highlight the most coupled modules
</span>");

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
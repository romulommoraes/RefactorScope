using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters
{
    public class ChartsRenderer
    {
        const int ChartSize = 320;

        public string RenderRadarSvg(
            HygieneReport h,
            StructuralCandidateAnalysisBreakdown breakdown,
            SolidResult? solid,
            ImplicitCouplingResult? implicitCoupling)
        {
            double Normalize(int value, int total) => total == 0 ? 0 : value / (double)total;
            string Fmt(double val) => val.ToString(CultureInfo.InvariantCulture);

            var solidAlerts = solid?.Alerts.Count ?? 0;

            double couplingScore =
                implicitCoupling == null
                ? 0
                : Normalize(implicitCoupling.Suspicions.Count, h.TotalClasses);

            var values = new[]
            {
                Normalize(breakdown.StructuralCandidates, h.TotalClasses),
                Normalize(breakdown.PatternSimilarity, h.TotalClasses),
                Normalize(breakdown.ProbabilisticConfirmed, h.TotalClasses),
                Normalize(h.NamespaceDriftCount, h.TotalClasses),
                Normalize(h.GlobalNamespaceCount, h.TotalClasses),
                Normalize(h.IsolatedCoreCount, h.TotalClasses),
                couplingScore,
                Math.Min(1.0, Normalize(solidAlerts, h.TotalClasses) * 5)
            };

            var labels = new[]
            {
                "Structural Candidates",
                "Pattern Similarity",
                "Unresolved",
                "Namespace Drift",
                "Global Namespace",
                "Core Isolation",
                "Implicit Coupling",
                "SOLID Alerts"
            };

            int size = ChartSize;
            int center = size / 2;
            int radius = 110;
            int levels = 4;

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Architectural Risk Radar</h3>");

            sb.AppendLine(
            "<div class='tooltip'>");

            sb.AppendLine($"<svg width='{size}' height='{size}' style='background:#161b22;border-radius:12px'>");

            for (int l = 1; l <= levels; l++)
            {
                double r = radius * (l / (double)levels);
                sb.AppendLine($"<circle cx='{center}' cy='{center}' r='{Fmt(r)}' fill='none' stroke='#30363d' stroke-width='1' />");
            }

            for (int i = 0; i < values.Length; i++)
            {
                double angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                double x = center + radius * Math.Cos(angle);
                double y = center + radius * Math.Sin(angle);

                sb.AppendLine($"<line x1='{center}' y1='{center}' x2='{Fmt(x)}' y2='{Fmt(y)}' stroke='#30363d' />");

                double lx = center + (radius + 22) * Math.Cos(angle);
                double ly = center + (radius + 22) * Math.Sin(angle);

                sb.AppendLine(
                $"<text x='{Fmt(lx)}' y='{Fmt(ly)}' fill='#e6edf3' font-size='11' text-anchor='middle'>" +
                $"<title>{labels[i]} — {(values[i] * 100):0}% of classes</title>" +
                $"{labels[i]}</text>");
            }

            var points = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                double angle = (Math.PI * 2 / values.Length) * i - Math.PI / 2;
                double r = radius * values[i];
                double x = center + r * Math.Cos(angle);
                double y = center + r * Math.Sin(angle);

                points.Add($"{Fmt(x)},{Fmt(y)}");
            }

            sb.AppendLine(
            $"<polygon points='{string.Join(" ", points)}' fill='rgba(255,99,132,0.35)' stroke='#ff6384' stroke-width='2'/>");

            sb.AppendLine("</svg>");

            sb.AppendLine(
            @"<span class='tooltiptext'>
Radar summarizing architectural risk indicators.

Higher values indicate larger concentration of potential structural issues:
• Dead code candidates
• Pattern similarity clusters
• Namespace drift
• Hidden coupling
• SOLID violations
</span>");

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public string RenderArchitecturalGalaxy(CouplingResult coupling)
        {
            string Fmt(double val) => val.ToString(CultureInfo.InvariantCulture);

            int width = ChartSize;
            int height = ChartSize;
            int margin = 35;

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Architectural Galaxy (A/I Distribution)</h3>");

            sb.AppendLine("<div class='tooltip'>");

            sb.AppendLine($"<svg width='{width}' height='{height}' style='background:#161b22;border-radius:12px'>");

            sb.AppendLine($"<line x1='{margin}' y1='{height - margin}' x2='{width - margin}' y2='{height - margin}' stroke='#30363d'/>");
            sb.AppendLine($"<line x1='{margin}' y1='{height - margin}' x2='{margin}' y2='{margin}' stroke='#30363d'/>");

            sb.AppendLine($"<text x='{width - 80}' y='{height - 8}' fill='#e6edf3' font-size='11'>Instability</text>");
            sb.AppendLine($"<text x='5' y='20' fill='#e6edf3' font-size='11'>Abstractness</text>");

            sb.AppendLine($"<line x1='{margin}' y1='{margin}' x2='{width - margin}' y2='{height - margin}' stroke='#444' stroke-dasharray='4,4'/>");

            foreach (var module in coupling.AbstractnessByModule.Keys)
            {
                double A = coupling.AbstractnessByModule[module];
                double I = coupling.InstabilityByModule.GetValueOrDefault(module);

                int couplingStrength = coupling.ModuleFanOut.GetValueOrDefault(module);

                double x = margin + (width - margin * 2) * I;
                double y = (height - margin) - (height - margin * 2) * A;

                double size = 4 + Math.Min(10, couplingStrength / 3.0);

                string color = "#58a6ff";

                if (couplingStrength > 20)
                    color = "#ff6b6b";
                else if (couplingStrength > 10)
                    color = "#ffd166";

                sb.AppendLine(
                    $"<circle cx='{Fmt(x)}' cy='{Fmt(y)}' r='{Fmt(size)}' fill='{color}' opacity='0.9'>" +
                    $"<title>{module}\nAbstractness: {A:0.00}\nInstability: {I:0.00}\nFanOut: {couplingStrength}</title>" +
                    $"</circle>");
            }

            sb.AppendLine("</svg>");

            sb.AppendLine(
            @"<span class='tooltiptext'>
Architectural Galaxy visualizes module positioning based on
Robert Martin's A/I model.

A = Abstractness
I = Instability

Modules should ideally lie near the Main Sequence (A + I = 1).
Points far from this line indicate architectural tension.
Circle size represents coupling intensity.
</span>");

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
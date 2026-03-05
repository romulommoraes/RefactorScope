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
        public string RenderRadarSvg(
                                    HygieneReport h,
                                    StructuralCandidateAnalysisBreakdown breakdown,
                                    SolidResult? solid)
        {
            double Normalize(int value, int total) => total == 0 ? 0 : value / (double)total;
            string Fmt(double val) => val.ToString(CultureInfo.InvariantCulture);
            var solidAlerts = solid?.Alerts.Count ?? 0;

                var values = new[]
                        {
                Normalize(breakdown.StructuralCandidates, h.TotalClasses),
                Normalize(breakdown.PatternSimilarity, h.TotalClasses),
                Normalize(breakdown.ProbabilisticConfirmed, h.TotalClasses),
                Normalize(h.NamespaceDriftCount, h.TotalClasses),
                Normalize(h.GlobalNamespaceCount, h.TotalClasses),
                Normalize(h.IsolatedCoreCount, h.TotalClasses),
                Math.Min(1.0, Normalize(solidAlerts, h.TotalClasses) * 5)
            };

            var labels = new[]
            {
                "Structural Candidates",
                "Pattern Similarity",
                "Unresolved",
                "Namespace Drift",
                "Global Namespace",
                "Isolation",
                "SOLID Alerts"
            };

            int size = 320;
            int center = size / 2;
            int radius = 110;
            int levels = 4;

            var sb = new StringBuilder();
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

                double lx = center + (radius + 20) * Math.Cos(angle);
                double ly = center + (radius + 20) * Math.Sin(angle);

                sb.AppendLine($"<text x='{Fmt(lx)}' y='{Fmt(ly)}' fill='#e6edf3' font-size='12' text-anchor='middle'>{labels[i]}</text>");
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

            sb.AppendLine($"<polygon points='{string.Join(" ", points)}' fill='rgba(255,99,132,0.4)' stroke='#ff6384' stroke-width='2'/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }

        public string RenderFunnelChart(StructuralCandidateAnalysisBreakdown breakdown)
        {
            var max = breakdown.StructuralCandidates;

            double Scale(int v) => max == 0 ? 0 : (v / (double)max) * 200;

            var sb = new StringBuilder();

            sb.AppendLine("<div style='background:#161b22;padding:20px;border-radius:10px;'>");
            sb.AppendLine("<h3>Candidate Refinement</h3>");
            sb.AppendLine("<svg width='240' height='160'>");

            int y = 20;

            void Bar(string label, int value)
            {
                var w = Scale(value);

                sb.AppendLine($"<rect x='20' y='{y}' width='{w}' height='20' fill='#ff6384'/>");
                sb.AppendLine($"<text x='25' y='{y + 15}' fill='#fff' font-size='11'>{label} ({value})</text>");

                y += 35;
            }

            Bar("Candidates", breakdown.StructuralCandidates);
            Bar("Pattern Match", breakdown.PatternSimilarity);
            Bar("Unresolved", breakdown.ProbabilisticConfirmed);

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public string RenderLayerDistribution(IReadOnlyList<ArchitecturalClassificationItem> items)
        {
            var groups = items
                .GroupBy(i => i.Layer ?? "Unknown")
                .OrderByDescending(g => g.Count());

            int max = groups.Max(g => g.Count());

            double Scale(int v) => (v / (double)max) * 200;

            var sb = new StringBuilder();

            sb.AppendLine("<div style='background:#161b22;padding:20px;border-radius:10px;'>");
            sb.AppendLine("<h3>Layer Distribution</h3>");
            sb.AppendLine("<svg width='260' height='200'>");

            int y = 20;

            foreach (var g in groups)
            {
                var w = Scale(g.Count());

                sb.AppendLine($"<rect x='20' y='{y}' width='{w}' height='18' fill='#4dabf7'/>");
                sb.AppendLine($"<text x='25' y='{y + 14}' fill='#fff' font-size='11'>{g.Key} ({g.Count()})</text>");

                y += 28;
            }

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public string RenderModuleHeatmap(IReadOnlyList<ArchitecturalClassificationItem> items)
        {
            string GetModule(string ns)
            {
                if (string.IsNullOrWhiteSpace(ns))
                    return "Unknown";

                return ns.Split('.').First();
            }

            var modules = items
                .GroupBy(i => GetModule(i.Namespace))
                .Select(g => new
                {
                    Module = g.Key,
                    Unresolved = g.Count(x => x.Status == "Sem Referência Estrutural")
                })
                .OrderByDescending(x => x.Unresolved);

            int max = modules.Max(m => m.Unresolved);

            var sb = new StringBuilder();

            sb.AppendLine("<div style='background:#161b22;padding:20px;border-radius:10px;'>");
            sb.AppendLine("<h3>Unresolved × Module</h3>");
            sb.AppendLine("<svg width='240' height='200'>");

            int y = 20;

            foreach (var m in modules)
            {
                double intensity = max == 0 ? 0 : m.Unresolved / (double)max;

                int r = (int)(255 * intensity);
                int g = (int)(80 * (1 - intensity));

                string color = $"rgb({r},{g},60)";

                sb.AppendLine($"<rect x='20' y='{y}' width='160' height='18' fill='{color}'/>");
                sb.AppendLine($"<text x='25' y='{y + 14}' fill='#fff' font-size='11'>{m.Module} ({m.Unresolved})</text>");

                y += 26;
            }

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public string RenderDependencyGravity(IReadOnlyList<ArchitecturalClassificationItem> items)
        {
            var top = items
                .OrderByDescending(i => i.UsageCount)
                .Take(8)
                .ToList();

            int max = top.Max(i => i.UsageCount);

            double Scale(int v) => max == 0 ? 0 : (v / (double)max) * 180;

            var sb = new StringBuilder();

            sb.AppendLine("<div style='background:#161b22;padding:20px;border-radius:10px;'>");
            sb.AppendLine("<h3>Dependency Gravity</h3>");
            sb.AppendLine("<svg width='260' height='200'>");

            int y = 20;

            foreach (var i in top)
            {
                var w = Scale(i.UsageCount);

                sb.AppendLine($"<rect x='20' y='{y}' width='{w}' height='16' fill='#ffd166'/>");
                sb.AppendLine($"<text x='25' y='{y + 12}' fill='#fff' font-size='10'>{i.TypeName} ({i.UsageCount})</text>");

                y += 24;
            }

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public string RenderModuleHeatmapIntensity(
    IReadOnlyList<ArchitecturalClassificationItem> items,
    HashSet<string> pattern,
    HashSet<string> unresolved)
        {
            string GetModule(string ns)
                => string.IsNullOrWhiteSpace(ns)
                    ? "Global"
                    : ns.Split('.').First();

            var modules = items
                .GroupBy(i => GetModule(i.Namespace))
                .Select(g => new
                {
                    Module = g.Key,
                    Structural = g.Count(x => x.Status == "Sem Referência Estrutural"),
                    Pattern = g.Count(x => pattern.Contains(x.TypeName)),
                    Unresolved = g.Count(x => unresolved.Contains(x.TypeName))
                })
                .OrderByDescending(x => x.Unresolved)
                .ToList();

            int max = modules.Max(m =>
                Math.Max(m.Structural,
                Math.Max(m.Pattern, m.Unresolved)));

            string Color(int value)
            {
                double t = max == 0 ? 0 : value / (double)max;

                int r = (int)(239 * t);
                int g = (int)(68 * (1 - t));
                int b = (int)(246 * (1 - t));

                return $"rgb({r},{g},{b})";
            }

            var sb = new StringBuilder();

            int cellW = 70;
            int cellH = 24;
            int startX = 120;
            int y = 30;

            sb.AppendLine("<div>");
            sb.AppendLine("<h3>Module × Candidate Heatmap</h3>");
            sb.AppendLine("<svg width='420' height='240'>");

            // headers
            sb.AppendLine($"<text x='{startX}' y='20' fill='#e6edf3'>Structural</text>");
            sb.AppendLine($"<text x='{startX + cellW}' y='20' fill='#e6edf3'>Pattern</text>");
            sb.AppendLine($"<text x='{startX + cellW * 2}' y='20' fill='#e6edf3'>Unresolved</text>");

            foreach (var m in modules)
            {
                sb.AppendLine($"<text x='10' y='{y + 16}' fill='#e6edf3'>{m.Module}</text>");

                sb.AppendLine($"<rect x='{startX}' y='{y}' width='{cellW}' height='{cellH}' fill='{Color(m.Structural)}'/>");
                sb.AppendLine($"<rect x='{startX + cellW}' y='{y}' width='{cellW}' height='{cellH}' fill='{Color(m.Pattern)}'/>");
                sb.AppendLine($"<rect x='{startX + cellW * 2}' y='{y}' width='{cellW}' height='{cellH}' fill='{Color(m.Unresolved)}'/>");

                y += cellH + 6;
            }

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

    }
}

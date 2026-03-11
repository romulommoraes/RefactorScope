using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using System.Text;

namespace RefactorScope.Exporters.Dashboards
{
    public sealed class ArchitecturalDashboardExporter
    {
        public void Export(ConsolidatedReport report, string outputPath)
        {
            Export(report, outputPath, DashboardThemeSelector.DefaultThemeFile);
        }

        public void Export(
            ConsolidatedReport report,
            string outputPath,
            string themeFileName)
        {
            var html = GenerateHtml(report, themeFileName);
            File.WriteAllText(outputPath, html, Encoding.UTF8);
        }

        private string GenerateHtml(
            ConsolidatedReport report,
            string themeFileName)
        {
            var architecture = report.GetResult<ArchitecturalClassificationResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var coupling = report.GetResult<CouplingResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var structure = report.GetResult<ProjectStructureResult>();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            var sb = new StringBuilder();

            sb.AppendLine(DashboardHtmlShell.RenderDocumentStart(
                "Architectural Dashboard",
                themeFileName));

            sb.AppendLine("<div class='page-header' augmented-ui='tl-clip tr-clip bl-clip br-clip border'>");
            sb.AppendLine("<div class='brand'>");
            sb.AppendLine("<div class='brand-kicker'>RefactorScope // Architectural Layer</div>");
            sb.AppendLine("<h1>Architectural Dashboard</h1>");
            sb.AppendLine($"<div class='subtitle'>Target Scope: <b>{Html(report.TargetScope)}</b></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='run-meta'>");
            sb.AppendLine($"<div><b>Generated:</b> {report.ExecutionTime:yyyy-MM-dd HH:mm} UTC</div>");
            sb.AppendLine("<div><b>View:</b> Executive architectural summary</div>");
            sb.AppendLine($"<div><b>Scope:</b> {Html(report.TargetScope)}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            var modules = architecture?.Items
                .GroupBy(i => i.Folder)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<IGrouping<string, ArchitecturalClassificationItem>>();

            var avgScore = CalculateAverageScore(modules, unresolved, isolated, coupling);
            var avgAbstractness = coupling?.AbstractnessByModule.Any() == true
                ? coupling.AbstractnessByModule.Values.Average()
                : 0;

            var avgInstability = coupling?.InstabilityByModule.Any() == true
                ? coupling.InstabilityByModule.Values.Average()
                : 0;

            var avgDistance = coupling?.DistanceByModule.Any() == true
                ? coupling.DistanceByModule.Values.Average()
                : 0;

            sb.AppendLine("<div class='grid-kpis' style='display:grid; grid-template-columns:repeat(4, 1fr); gap:16px; margin-bottom:32px;'>");
            AppendKpi(sb, "Modules", modules.Count.ToString(), "Detected architectural folders");
            AppendKpi(sb, "Average Score", $"{avgScore:0.0}", "Composite health by module");
            AppendKpi(sb, "Unresolved", unresolved.Count.ToString(), "Final unresolved structural candidates", unresolved.Count > 0 ? "alert" : "good");
            AppendKpi(sb, "Implicit Coupling", $"{implicitCoupling?.Suspicions.Count ?? 0}", "Potential dependency hotspots", (implicitCoupling?.Suspicions.Count ?? 0) > 0 ? "warning" : "good");
            AppendKpi(sb, "Abstractness (A)", $"{avgAbstractness:0.00}", "Average architectural abstraction");
            AppendKpi(sb, "Instability (I)", $"{avgInstability:0.00}", "Average outgoing dependency pressure");
            AppendKpi(sb, "Distance (D)", $"{avgDistance:0.00}", "Average distance from main sequence");
            AppendKpi(sb, "Core Density", "Check Table", "Amount of inner core objects");
            sb.AppendLine("</div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Architecture & Structure</h2>
        <div class="line"></div>
    </div>
</div>
""");

            // =====================================================
            // 1. TABELA
            // =====================================================
            sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border' style='margin-bottom:24px; display:flex; flex-direction:column; min-width:0;'>");
            sb.AppendLine("<h3>Architectural Health by Module</h3>");
            sb.AppendLine("<div class='table-wrap' style='flex:1;'>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Module</th><th>Score</th><th>Unresolved</th><th>Coupling</th><th>Isolation</th><th>Core Density</th></tr>");

            foreach (var module in modules)
            {
                var total = module.Count();
                if (total == 0)
                    continue;

                var unresolvedCount = unresolved.Count(z => module.Any(m => m.TypeName == z));
                var isolatedCount = isolated?.IsolatedCoreTypes.Count(i => module.Any(m => m.TypeName == i)) ?? 0;
                var fanOut = coupling?.ModuleFanOut.GetValueOrDefault(module.Key) ?? 0;
                var coreCount = module.Count(t => t.Layer == "Core");

                var candidateRate = unresolvedCount / (double)total;
                var isolationRate = isolatedCount / (double)total;
                var couplingRate = fanOut / (double)total;
                var coreDensity = coreCount / (double)total;

                var score = ArchitecturalScoreCalculator.Calculate(
                    module.Key,
                    total,
                    unresolvedCount,
                    isolatedCount,
                    fanOut,
                    coreCount);

                score = Math.Max(0, Math.Min(100, score));

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{Html(module.Key)}</td>");
                sb.AppendLine($"<td>{ScoreHtml(score)}</td>");
                sb.AppendLine($"<td>{unresolvedCount} ({candidateRate:0%})</td>");
                sb.AppendLine($"<td>{couplingRate:0.00}</td>");
                sb.AppendLine($"<td>{isolationRate:0.00}</td>");
                sb.AppendLine($"<td>{coreDensity:0.00}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            // =====================================================
            // 2. ÁRVORE
            // =====================================================
            var p5Renderer = new ArchitecturalStructureRendererP5();
            var preferredRootName = ResolveRootName(report.TargetScope);

            sb.AppendLine(p5Renderer.RenderRadialTree(structure, preferredRootName));

            // =====================================================
            // 3. INFORMATION FLOW PIPELINE
            // =====================================================
            sb.AppendLine(p5Renderer.RenderInformationFlowPipeline());

            sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
                "Generated by RefactorScope Architectural Dashboard"));

            return sb.ToString();
        }

        private static string ResolveRootName(string? targetScope)
        {
            if (string.IsNullOrWhiteSpace(targetScope))
                return "Project";

            var trimmed = targetScope.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var name = Path.GetFileName(trimmed);

            return string.IsNullOrWhiteSpace(name)
                ? "Project"
                : name;
        }

        private static void AppendKpi(
            StringBuilder sb,
            string label,
            string value,
            string hint,
            string stateClass = "")
        {
            var cssClass = string.IsNullOrWhiteSpace(stateClass)
                ? "kpi"
                : $"kpi {stateClass}";

            sb.AppendLine($"<div class='{cssClass}' augmented-ui='tr-clip bl-clip border' style='height:100%; box-sizing:border-box;'>");
            sb.AppendLine($"<div class='label'>{Html(label)}</div>");
            sb.AppendLine($"<div class='value'>{Html(value)}</div>");
            sb.AppendLine($"<div class='hint'>{Html(hint)}</div>");
            sb.AppendLine("</div>");
        }

        private static double CalculateAverageScore(
            IReadOnlyList<IGrouping<string, ArchitecturalClassificationItem>> modules,
            IReadOnlyList<string> unresolved,
            CoreIsolationResult? isolated,
            CouplingResult? coupling)
        {
            if (modules.Count == 0)
                return 0;

            var scores = new List<double>();

            foreach (var module in modules)
            {
                var total = module.Count();
                if (total == 0)
                    continue;

                var unresolvedCount = unresolved.Count(z => module.Any(m => m.TypeName == z));
                var isolatedCount = isolated?.IsolatedCoreTypes.Count(i => module.Any(m => m.TypeName == i)) ?? 0;
                var fanOut = coupling?.ModuleFanOut.GetValueOrDefault(module.Key) ?? 0;
                var coreCount = module.Count(t => t.Layer == "Core");

                var score = ArchitecturalScoreCalculator.Calculate(
                    module.Key,
                    total,
                    unresolvedCount,
                    isolatedCount,
                    fanOut,
                    coreCount);

                scores.Add(Math.Max(0, Math.Min(100, score)));
            }

            return scores.Count == 0 ? 0 : scores.Average();
        }

        private static string ScoreHtml(double score)
        {
            var css = score >= 70
                ? "metric-good"
                : score >= 40
                    ? "metric-warn"
                    : "metric-bad";

            return $"<span class='{css}'>{score:0.0}</span>";
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
    }
}
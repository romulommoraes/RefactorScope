using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Styling;
using System.Text;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exporta o dashboard arquitetural em HTML.
    ///
    /// Objetivo
    /// --------
    /// Fornecer uma visualização executiva e navegável da saúde arquitetural,
    /// complementar ao relatório Markdown.
    ///
    /// Escopo atual
    /// ------------
    /// - saúde por módulo
    /// - métricas de Robert Martin (A / I / D)
    /// - coupling implícito
    /// - fitness gates
    /// - estrutura do projeto em formato textual estilizado
    ///
    /// Observação
    /// ----------
    /// Esta versão usa o shell visual compartilhado da suíte HTML,
    /// permitindo seleção de tema sem alterar a estrutura do dashboard.
    /// </summary>
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
            var fitness = report.GetResult<FitnessGateResult>();
            var structure = report.GetResult<ProjectStructureResult>();

            var structuralCandidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
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
            sb.AppendLine($"<div><b>View:</b> Executive architectural summary</div>");
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

            var fitStatus = fitness == null
                ? "Unknown"
                : fitness.HasFailure ? "Attention Required" : "Ready";

            sb.AppendLine("<div class='grid-kpis'>");
            AppendKpi(sb, "Modules", modules.Count.ToString(), "Detected architectural folders");
            AppendKpi(sb, "Average Score", $"{avgScore:0.0}", "Composite health by module");
            AppendKpi(sb, "Unresolved", unresolved.Count.ToString(), "Final unresolved structural candidates", unresolved.Count > 0 ? "alert" : "good");
            AppendKpi(sb, "Fitness", fitStatus, "Overall architectural readiness", fitness == null ? "warning" : fitness.HasFailure ? "alert" : "good");
            AppendKpi(sb, "Abstractness (A)", $"{avgAbstractness:0.00}", "Average architectural abstraction");
            AppendKpi(sb, "Instability (I)", $"{avgInstability:0.00}", "Average outgoing dependency pressure");
            AppendKpi(sb, "Distance (D)", $"{avgDistance:0.00}", "Average distance from main sequence");
            AppendKpi(sb, "Implicit Coupling", $"{implicitCoupling?.Suspicions.Count ?? 0}", "Potential dependency hotspots", (implicitCoupling?.Suspicions.Count ?? 0) > 0 ? "warning" : "good");
            sb.AppendLine("</div>");

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Module Health</h2>
        <div class="line"></div>
    </div>
</div>
""");

            sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border'>");
            sb.AppendLine("<h3>Architectural Health by Module</h3>");
            sb.AppendLine("<div class='table-wrap'>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Module</th><th>Score</th><th>Unresolved</th><th>Coupling</th><th>Isolation</th><th>Core Density</th></tr>");

            foreach (var module in modules)
            {
                var total = module.Count();
                if (total == 0) continue;

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

            sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Architectural Signals</h2>
        <div class="line"></div>
    </div>
</div>
""");

            sb.AppendLine("<div class='panel-grid'>");

            sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border'>");
            sb.AppendLine("<h3>Interpretation</h3>");
            sb.AppendLine("<ul class='clean'>");
            sb.AppendLine($"<li>Structural Candidates: {structuralCandidates.Count}</li>");
            sb.AppendLine($"<li>Pattern Similarity: {patternSimilarity.Count}</li>");
            sb.AppendLine($"<li>Unresolved: {unresolved.Count}</li>");
            sb.AppendLine($"<li>Implicit Coupling Suspicions: {implicitCoupling?.Suspicions.Count ?? 0}</li>");
            sb.AppendLine($"<li>Average Abstractness: {avgAbstractness:0.00}</li>");
            sb.AppendLine($"<li>Average Instability: {avgInstability:0.00}</li>");
            sb.AppendLine($"<li>Average Distance: {avgDistance:0.00}</li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<div class='note' style='margin-top:14px;'>");
            sb.AppendLine("""
This dashboard is an executive architectural view.
It complements the Markdown report rather than replacing it.
The Markdown artifact remains useful for direct textual reading, project tree inspection and documentation-friendly export.
""");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border'>");
            sb.AppendLine("<h3>Fitness Gates</h3>");

            if (fitness != null && fitness.Gates.Any())
            {
                sb.AppendLine("<div class='table-wrap'>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Gate</th><th>Status</th><th>Message</th></tr>");

                foreach (var gate in fitness.Gates)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{Html(gate.GateName)}</td>");
                    sb.AppendLine($"<td>{GateStatusHtml(gate.Status)}</td>");
                    sb.AppendLine($"<td>{Html(gate.Message)}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div class='note'>No fitness gate information available for this execution.</div>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            if (structure != null && structure.Lines.Any())
            {
                sb.AppendLine("""
<div class="section">
    <div class="section-title">
        <h2>Project Structure</h2>
        <div class="line"></div>
    </div>
</div>
""");

                sb.AppendLine("<div class='panel' augmented-ui='tl-clip tr-clip bl-clip br-clip border'>");
                sb.AppendLine("<h3>Root Structure Snapshot</h3>");
                sb.AppendLine("<pre class='tree-view'>");

                foreach (var line in structure.Lines)
                    sb.AppendLine(Html(line));

                sb.AppendLine("</pre>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine(DashboardHtmlShell.RenderDocumentEnd(
                "Generated by RefactorScope Architectural Dashboard"));

            return sb.ToString();
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

            sb.AppendLine($"<div class='{cssClass}' augmented-ui='tr-clip bl-clip border'>");
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
                if (total == 0) continue;

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

        private static string GateStatusHtml(GateStatus status)
        {
            return status switch
            {
                GateStatus.Pass => "<span class='metric-good'>PASS</span>",
                GateStatus.Warn => "<span class='metric-warn'>WARN</span>",
                GateStatus.Fail => "<span class='metric-bad'>FAIL</span>",
                _ => Html(status.ToString())
            };
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
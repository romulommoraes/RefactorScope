using System.Text;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exports an architectural report in Markdown format.
    ///
    /// The report reflects the same semantic pipeline used in the dashboard:
    ///
    /// Structural Candidates
    ///        ↓
    /// Pattern Similarity
    ///        ↓
    /// Unresolved
    ///
    /// Unresolved candidates represent the final probabilistic classification.
    /// </summary>
    public sealed class MarkdownReportExporter
    {
        public void Export(ConsolidatedReport report, string outputPath)
        {
            var sb = new StringBuilder();

            var architecture = report.GetResult<ArchitecturalClassificationResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var coupling = report.GetResult<CouplingResult>();
            var fitness = report.GetResult<FitnessGateResult>();
            var structure = report.GetResult<ProjectStructureResult>();

            if (architecture == null)
                return;

            var structuralCandidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            sb.AppendLine("# 🧬 RefactorScope – Architectural Report");
            sb.AppendLine();
            sb.AppendLine($"📅 **Execution Time:** {report.ExecutionTime:yyyy-MM-dd HH:mm} UTC  ");
            sb.AppendLine($"📂 **Target Scope:** `{report.TargetScope}`  ");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // =====================================================
            // Project Structure
            // =====================================================

            if (structure != null)
            {
                sb.AppendLine("## 📂 Project Structure (Clean)");
                sb.AppendLine();

                sb.AppendLine("```");

                foreach (var line in structure.Lines)
                {
                    sb.AppendLine(line);
                }

                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            // =====================================================
            // Structural Candidate Analysis
            // =====================================================

            sb.AppendLine("## 🔎 Structural Candidate Analysis (ADR-EXP-007)");
            sb.AppendLine();

            sb.AppendLine($"- **Structural Candidates:** {structuralCandidates.Count}");
            sb.AppendLine($"- **Pattern Similarity:** {patternSimilarity.Count}");
            sb.AppendLine($"- **Unresolved:** {unresolved.Count}");
            sb.AppendLine();

            // =====================================================
            // Architectural Health by Module
            // =====================================================

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 🏥 Architectural Health by Module");
            sb.AppendLine();

            var modules = architecture.Items.GroupBy(i => i.Folder);

            foreach (var module in modules)
            {
                var total = module.Count();
                if (total == 0) continue;

                var unresolvedCount = unresolved
                    .Count(z => module.Any(m => m.TypeName == z));

                var isolatedCount = isolated?.IsolatedCoreTypes
                    .Count(i => module.Any(m => m.TypeName == i)) ?? 0;

                var fanOut = coupling?.ModuleFanOut
                    .GetValueOrDefault(module.Key) ?? 0;

                var candidateRate = unresolvedCount / (double)total;
                var isolationRate = isolatedCount / (double)total;
                var couplingRate = fanOut / (double)total;
                var coreDensity = module.Count(t => t.Layer == "Core") / (double)total;

                var score =
                    100
                    - (couplingRate * 30)
                    - (candidateRate * 25)
                    - (isolationRate * 20)
                    + (coreDensity * 15);

                score = Math.Max(0, Math.Min(100, score));

                var scoreEmoji = score >= 70 ? "🟢"
                                 : score >= 40 ? "🟡"
                                 : "🔴";

                sb.AppendLine($"### {scoreEmoji} {module.Key}");
                sb.AppendLine();
                sb.AppendLine($"- **Score:** `{score:0.0}`");
                sb.AppendLine($"- **Unresolved Candidates:** {(unresolvedCount > 0 ? "🔴" : "🟢")} {unresolvedCount} ({candidateRate:0%})");
                sb.AppendLine($"- **Coupling:** {couplingRate:0.00}");
                sb.AppendLine($"- **Isolation:** {isolationRate:0.00}");
                sb.AppendLine($"- **Core Density:** {coreDensity:0.00}");
                sb.AppendLine();
            }

            // =====================================================
            // Fitness Gates
            // =====================================================

            if (fitness != null)
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 🚦 Fitness Gates");
                sb.AppendLine();

                foreach (var gate in fitness.Gates)
                {
                    var emoji = gate.Status switch
                    {
                        GateStatus.Pass => "🟢",
                        GateStatus.Warn => "🟡",
                        GateStatus.Fail => "🔴",
                        _ => "⚪"
                    };

                    sb.AppendLine($"- {emoji} **{gate.GateName}** {gate.Message}");
                }

                sb.AppendLine();
                sb.AppendLine(fitness.HasFailure
                    ? "🔴 **Architecture NOT ready for CI/CD**"
                    : "🟢 **Architecture ready for CI/CD**");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // =====================================================
            // Metrics Explanation
            // =====================================================

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 📘 Metrics Explanation");
            sb.AppendLine();

            sb.AppendLine("**Structural Candidates**  ");
            sb.AppendLine("Classes detected with zero or near-zero structural references in the analyzed scope.  ");
            sb.AppendLine("These are potential dead-code candidates based purely on static structural analysis.");
            sb.AppendLine();

            sb.AppendLine("**Pattern Similarity**  ");
            sb.AppendLine("Structural candidates that match known architectural patterns such as:");
            sb.AppendLine("- Dependency Injection usage");
            sb.AppendLine("- Interface-based abstractions");
            sb.AppendLine("- Factory / Strategy structures");
            sb.AppendLine();
            sb.AppendLine("Pattern similarity indicates the class likely participates in a valid architectural pattern.");
            sb.AppendLine("However, it does **not guarantee runtime usage**.");
            sb.AppendLine();

            sb.AppendLine("**Unresolved**  ");
            sb.AppendLine("Candidates that could not be explained by recognized structural patterns.");
            sb.AppendLine("These remain potential dead-code candidates after probabilistic refinement.");
            sb.AppendLine("Manual inspection is recommended.");
            sb.AppendLine();

            sb.AppendLine("**Coupling**  ");
            sb.AppendLine("Average fan-out of types inside the module.");
            sb.AppendLine("Higher values indicate stronger inter-module dependency.");
            sb.AppendLine();

            sb.AppendLine("**Isolation**  ");
            sb.AppendLine("Core-layer types with no incoming structural references.");
            sb.AppendLine("These may indicate incomplete architecture integration.");
            sb.AppendLine();

            sb.AppendLine("**Core Density**  ");
            sb.AppendLine("Proportion of types belonging to the Core architectural layer.");
            sb.AppendLine("Higher density generally indicates stronger domain encapsulation.");
            sb.AppendLine();

            sb.AppendLine("**Architectural Score**  ");
            sb.AppendLine("Composite structural health indicator (0–100) based on:");
            sb.AppendLine("- Coupling impact");
            sb.AppendLine("- Unresolved candidate density");
            sb.AppendLine("- Isolation rate");
            sb.AppendLine("- Core density bonus");
            sb.AppendLine();
            sb.AppendLine("The score is normalized and intended as a heuristic indicator, not a formal proof.");

            sb.AppendLine("_Generated by RefactorScope_");

            File.WriteAllText(outputPath, sb.ToString());
        }
    }
}
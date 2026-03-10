using RefactorScope.Core.Metrics;
using RefactorScope.Core.Results;
using System.Text;

namespace RefactorScope.Exporters.Reports
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
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
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

                var score = ArchitecturalScoreCalculator.Calculate(
                    module.Key,
                    total,
                    unresolvedCount,
                    isolatedCount,
                    fanOut,
                    module.Count(t => t.Layer == "Core"));

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
            // Implicit Coupling Analysis
            // =====================================================

            if (implicitCoupling != null && implicitCoupling.Suspicions.Any())
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## ⚠ Implicit Coupling Suspicion");
                sb.AppendLine();

                sb.AppendLine("| Type | Module | Target Module | Fan-Out | Fan-In | Dominance | Volume |");
                sb.AppendLine("|------|--------|---------------|--------|--------|-----------|--------|");

                foreach (var s in implicitCoupling.Suspicions)
                {
                    sb.AppendLine(
                        $"| {s.TypeName} | {s.Module} | {s.TargetModule} | {s.FanOut} | {s.FanIn} | {s.Dominance:0.00} | {s.Volume} |");
                }

                sb.AppendLine();
                sb.AppendLine("Possible architectural coupling detected based on structural heuristics.");
                sb.AppendLine("Manual inspection is recommended.");
                sb.AppendLine();
            }

            // =====================================================
            // Architectural Stability Metrics (Robert Martin)
            // =====================================================

            if (coupling != null)
            {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 🧭 Architectural Stability Metrics (Robert Martin)");
                sb.AppendLine();

                sb.AppendLine("| Module | Abstractness (A) | Instability (I) | Distance (D) |");
                sb.AppendLine("|--------|------------------|-----------------|--------------|");

                foreach (var module in coupling.AbstractnessByModule.Keys)
                {
                    var a = coupling.AbstractnessByModule.GetValueOrDefault(module);
                    var i = coupling.InstabilityByModule.GetValueOrDefault(module);
                    var d = coupling.DistanceByModule.GetValueOrDefault(module);

                    sb.AppendLine($"| {module} | {a:0.00} | {i:0.00} | {d:0.00} |");
                }

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

            // =====================================================
            // Architectural Stability Metrics (Robert Martin)
            // =====================================================

            sb.AppendLine();
            sb.AppendLine("**Architectural Stability Metrics (Robert Martin)**  ");
            sb.AppendLine("The following metrics are derived from the architectural model proposed by Robert C. Martin.");
            sb.AppendLine("They are intended as structural indicators rather than strict architectural rules.");
            sb.AppendLine();

            sb.AppendLine("**Abstractness (A)**  ");
            sb.AppendLine("Represents the proportion of abstractions within a module.");
            sb.AppendLine();
            sb.AppendLine("A = Na / Nc");
            sb.AppendLine();
            sb.AppendLine("Where:");
            sb.AppendLine("- Na = number of abstract types (interfaces or abstract classes)");
            sb.AppendLine("- Nc = total number of types in the module.");
            sb.AppendLine();
            sb.AppendLine("Higher values indicate a more abstract module.");
            sb.AppendLine();

            sb.AppendLine("**Instability (I)**  ");
            sb.AppendLine("Measures how dependent a module is on other modules.");
            sb.AppendLine();
            sb.AppendLine("I = Ce / (Ce + Ca)");
            sb.AppendLine();
            sb.AppendLine("Where:");
            sb.AppendLine("- Ce = outgoing dependencies");
            sb.AppendLine("- Ca = incoming dependencies");
            sb.AppendLine();
            sb.AppendLine("Values closer to 1 indicate modules that depend heavily on other modules.");
            sb.AppendLine();

            sb.AppendLine("**Distance from Main Sequence (D)**  ");
            sb.AppendLine();
            sb.AppendLine("D = | A + I − 1 |");
            sb.AppendLine();
            sb.AppendLine("This metric measures how far a module is from the architectural equilibrium line between abstraction and stability.");
            sb.AppendLine();
            sb.AppendLine("Values close to 0 indicate balanced architecture.");
            sb.AppendLine();
            sb.AppendLine("Higher values indicate architectural tension such as:");
            sb.AppendLine("- overly concrete and rigid modules");
            sb.AppendLine("- overly abstract but unstable modules");
            sb.AppendLine();
            sb.AppendLine("These metrics should be interpreted as architectural signals, not strict violations.");
            sb.AppendLine();


            // =====================================================
            // Implicit Coupling Detection
            // =====================================================

            sb.AppendLine();
            sb.AppendLine("**Implicit Coupling Detection**  ");
            sb.AppendLine("Implicit Coupling identifies classes whose dependencies concentrate towards a specific module or subsystem.");
            sb.AppendLine();
            sb.AppendLine("This heuristic analyzes structural patterns such as:");
            sb.AppendLine("- strong directional dependency concentration");
            sb.AppendLine("- high fan-out towards a single module");
            sb.AppendLine("- asymmetric dependency flows between modules");
            sb.AppendLine();
            sb.AppendLine("A flagged class does not necessarily represent a design problem.");
            sb.AppendLine();
            sb.AppendLine("Typical legitimate cases include:");
            sb.AppendLine("- orchestrators");
            sb.AppendLine("- adapters between subsystems");
            sb.AppendLine("- integration layers");
            sb.AppendLine();
            sb.AppendLine("These signals are intended to highlight areas that may benefit from architectural review.");
            sb.AppendLine();

            sb.AppendLine("_Generated by RefactorScope_");

            File.WriteAllText(outputPath, sb.ToString());
        }
    }
}
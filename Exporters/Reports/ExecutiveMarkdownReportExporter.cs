using RefactorScope.Core.Reporting;
using System;
using System.IO;
using System.Text;

namespace RefactorScope.Exporters.Reports
{
    /// <summary>
    /// Gera um relatório executivo em Markdown a partir de um ReportSnapshot.
    ///
    /// Objetivo:
    /// - fornecer uma leitura humana e portátil da execução
    /// - consolidar parser telemetry, sinais estruturais, leitura arquitetural e readiness
    /// - servir como base textual para o fluxo normal e para o futuro batch
    /// </summary>
    public sealed class ExecutiveMarkdownReportExporter
    {
        public void Export(ReportSnapshot snapshot, string outputPath)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var sb = new StringBuilder();

            RenderHeader(sb, snapshot);
            RenderExecutiveOverview(sb, snapshot);
            RenderParserTelemetry(sb, snapshot);
            RenderStructuralAnalysis(sb, snapshot);
            RenderArchitectureOverview(sb, snapshot);
            RenderQualityInterpretation(sb, snapshot);
            RenderOperationalGuidance(sb, snapshot);
            RenderGlossary(sb);
            RenderFooter(sb);

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static void RenderHeader(StringBuilder sb, ReportSnapshot snapshot)
        {
            sb.AppendLine("# RefactorScope — Executive Analysis Report");
            sb.AppendLine();
            sb.AppendLine("> Executive textual companion for the current analysis snapshot.");
            sb.AppendLine("> This report consolidates parser telemetry, structural signals, architectural indicators and readiness interpretation.");
            sb.AppendLine();
            sb.AppendLine($"- **Generated at:** {snapshot.ExecutionTimeUtc:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine($"- **Target scope:** `{snapshot.TargetScope}`");
            sb.AppendLine($"- **Parser:** `{snapshot.Parsing.ParserName}`");
            sb.AppendLine($"- **Confidence band:** `{snapshot.Parsing.ConfidenceBand}`");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderExecutiveOverview(StringBuilder sb, ReportSnapshot snapshot)
        {
            var parserConfidence = Clamp01(snapshot.Parsing.ParserConfidence);

            sb.AppendLine("## Executive Overview");
            sb.AppendLine();
            sb.AppendLine("This section summarizes the overall state of the run in a compact, management-friendly format.");
            sb.AppendLine("It should be read as an architectural snapshot, not as a formal proof.");
            sb.AppendLine();

            sb.AppendLine("| Signal | Value | Interpretation |");
            sb.AppendLine("|--------|-------|----------------|");
            sb.AppendLine($"| Parser Confidence | `{parserConfidence:P0}` | {snapshot.Parsing.ConfidenceBand} confidence for structural extraction |");
            sb.AppendLine($"| Structural Candidates | `{snapshot.Structural.StructuralCandidates}` | Initial dead-code hypothesis set |");
            sb.AppendLine($"| Unresolved | `{snapshot.Structural.Unresolved}` | Candidates still not explained after refinement |");
            sb.AppendLine($"| Pattern Similarity | `{snapshot.Structural.PatternSimilarity}` | Candidates protected by architectural pattern similarity |");
            sb.AppendLine($"| Implicit Coupling | `{snapshot.Architectural.ImplicitCouplingSuspects}` | Heuristic coupling hotspots |");
            sb.AppendLine($"| Modules | `{snapshot.Architectural.Modules}` | Architectural groups detected in classification |");
            sb.AppendLine($"| Fitness Status | `{snapshot.Quality.FitStatus}` | Execution-level readiness gate summary |");
            sb.AppendLine($"| Overall Readiness | `{snapshot.Quality.OverallReadinessBand}` | Consolidated operational interpretation |");
            sb.AppendLine();

            sb.AppendLine("> **Executive reading:**");
            sb.AppendLine($"> Parsing posture is **{snapshot.Parsing.ConfidenceBand}**, structural pressure remains at **{snapshot.Structural.Unresolved} unresolved**, and the run is currently interpreted as **{snapshot.Quality.OverallReadinessBand} readiness**.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderParserTelemetry(StringBuilder sb, ReportSnapshot snapshot)
        {
            var parserConfidence = Clamp01(snapshot.Parsing.ParserConfidence);

            sb.AppendLine("## Parser Telemetry");
            sb.AppendLine();
            sb.AppendLine("This section describes how the parser behaved during the run and how much structural evidence was extracted.");
            sb.AppendLine();

            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Parser Name | `{snapshot.Parsing.ParserName}` |");
            sb.AppendLine($"| Confidence | `{parserConfidence:P0}` |");
            sb.AppendLine($"| Confidence Band | `{snapshot.Parsing.ConfidenceBand}` |");
            sb.AppendLine($"| Files | `{snapshot.Parsing.Files}` |");
            sb.AppendLine($"| Types | `{snapshot.Parsing.Types}` |");
            sb.AppendLine($"| References | `{snapshot.Parsing.References}` |");
            sb.AppendLine($"| Execution Time | `{snapshot.Parsing.ExecutionMs:0} ms` |");
            sb.AppendLine($"| Types / File | `{snapshot.Parsing.TypesPerFile:0.00}` |");
            sb.AppendLine($"| References / Type | `{snapshot.Parsing.ReferencesPerType:0.00}` |");
            sb.AppendLine($"| ms / File | `{snapshot.Parsing.MsPerFile:0.00}` |");
            sb.AppendLine($"| ms / Type | `{snapshot.Parsing.MsPerType:0.00}` |");
            sb.AppendLine($"| Estimated Memory | `{snapshot.Parsing.MemoryBytes:N0} bytes` |");
            sb.AppendLine($"| Sparse Extraction | `{YesNo(snapshot.Parsing.SparseExtraction)}` |");
            sb.AppendLine($"| Anomaly Detected | `{YesNo(snapshot.Parsing.AnomalyDetected)}` |");
            sb.AppendLine($"| Extraction Index | `{snapshot.Parsing.ExtractionIndex:0.00}` |");
            sb.AppendLine();

            sb.AppendLine("### Context");
            sb.AppendLine();
            sb.AppendLine($"- {snapshot.Parsing.ConfidenceDiagnosis}");
            sb.AppendLine($"- {snapshot.Parsing.DensityDiagnosis}");
            sb.AppendLine($"- {snapshot.Parsing.PerformanceDiagnosis}");
            sb.AppendLine($"- {snapshot.Parsing.SparseExtractionDiagnosis}");
            sb.AppendLine($"- {snapshot.Parsing.AnomalyDiagnosis}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderStructuralAnalysis(StringBuilder sb, ReportSnapshot snapshot)
        {
            sb.AppendLine("## Structural Candidate Analysis");
            sb.AppendLine();
            sb.AppendLine("This section reflects the canonical dead-code pipeline:");
            sb.AppendLine();
            sb.AppendLine("`Structural Candidates -> Pattern Similarity / Suspicious -> Unresolved`");
            sb.AppendLine();

            sb.AppendLine("| Metric | Value | Meaning |");
            sb.AppendLine("|--------|-------|---------|");
            sb.AppendLine($"| Structural Candidates | `{snapshot.Structural.StructuralCandidates}` | Initial structurally weak types |");
            sb.AppendLine($"| Pattern Similarity | `{snapshot.Structural.PatternSimilarity}` | Candidates explained by recognized architectural patterns |");
            sb.AppendLine($"| Suspicious | `{snapshot.Structural.Suspicious}` | Intermediate candidates still requiring caution |");
            sb.AppendLine($"| Unresolved | `{snapshot.Structural.Unresolved}` | Final unresolved hypothesis after refinement |");
            sb.AppendLine($"| Reduction Rate | `{snapshot.Structural.ReductionRate:P0}` | How much of the initial set was softened by pattern recognition |");
            sb.AppendLine();

            sb.AppendLine("### Interpretation");
            sb.AppendLine();
            sb.AppendLine("- A high **Structural Candidates** count means the raw structural scan found many types with weak support.");
            sb.AppendLine("- A high **Pattern Similarity** count usually indicates good recovery through DI, interface usage or bootstrap references.");
            sb.AppendLine("- A high **Unresolved** count suggests stronger suspicion of dead code or structural disconnects.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderArchitectureOverview(StringBuilder sb, ReportSnapshot snapshot)
        {
            sb.AppendLine("## Architectural Overview");
            sb.AppendLine();
            sb.AppendLine("This section summarizes architectural tension signals derived from the consolidated analysis.");
            sb.AppendLine();

            sb.AppendLine("| Metric | Value | Reading |");
            sb.AppendLine("|--------|-------|---------|");
            sb.AppendLine($"| Modules | `{snapshot.Architectural.Modules}` | Distinct architectural groups detected in the classification layer |");
            sb.AppendLine($"| Average Score | `{snapshot.Architectural.AverageScore:0.0}` | Composite health score across modules |");
            sb.AppendLine($"| Average Abstractness | `{snapshot.Architectural.AverageAbstractness:0.00}` | Mean abstraction level across modules |");
            sb.AppendLine($"| Average Instability | `{snapshot.Architectural.AverageInstability:0.00}` | Mean outward dependency pressure |");
            sb.AppendLine($"| Average Distance | `{snapshot.Architectural.AverageDistance:0.00}` | Distance from main sequence |");
            sb.AppendLine($"| Implicit Coupling Suspicions | `{snapshot.Architectural.ImplicitCouplingSuspects}` | Concentrated dependency hotspots |");
            sb.AppendLine();

            sb.AppendLine("### Context");
            sb.AppendLine();
            sb.AppendLine("- **Implicit Coupling** does not always indicate design failure; orchestration layers may legitimately trigger this signal.");
            sb.AppendLine("- **Average Score** is heuristic and should be interpreted as an architectural barometer, not an absolute truth.");
            sb.AppendLine("- **A / I / D** metrics are useful for comparative reading across modules and runs.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderQualityInterpretation(StringBuilder sb, ReportSnapshot snapshot)
        {
            sb.AppendLine("## Quality Interpretation");
            sb.AppendLine();
            sb.AppendLine("This section transforms raw metrics into a more executive narrative.");
            sb.AppendLine();

            sb.AppendLine("| Dimension | Band / Value |");
            sb.AppendLine("|-----------|---------------|");
            sb.AppendLine($"| Parser Confidence | `{snapshot.Parsing.ConfidenceBand}` |");
            sb.AppendLine($"| Statistics Coverage | `{snapshot.Quality.StatisticsCoverageBand}` ({snapshot.Quality.StatisticsCoverageScore:0.00}) |");
            sb.AppendLine($"| Overall Readiness | `{snapshot.Quality.OverallReadinessBand}` ({snapshot.Quality.OverallReadinessScore:0.00}) |");
            sb.AppendLine($"| Fitness Status | `{snapshot.Quality.FitStatus}` |");
            sb.AppendLine($"| SOLID Alerts | `{snapshot.Quality.SolidAlerts}` |");
            sb.AppendLine();

            sb.AppendLine("### Narrative");
            sb.AppendLine();
            sb.AppendLine($"The current execution is interpreted as **{snapshot.Quality.OverallReadinessBand} readiness**.");
            sb.AppendLine($"Parser confidence is **{snapshot.Parsing.ConfidenceBand}**, statistics coverage is **{snapshot.Quality.StatisticsCoverageBand}**, and the global fitness status is **{snapshot.Quality.FitStatus}**.");
            sb.AppendLine();

            sb.AppendLine("### Recommended Reading");
            sb.AppendLine();
            sb.AppendLine("- Use this section as the fastest summary for publication, release notes or benchmark interpretation.");
            sb.AppendLine("- For detailed forensics, the HTML dashboards remain the richer visual layer.");
            sb.AppendLine("- For BI workflows, the same snapshot can feed JSON and CSV analytical exports.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderOperationalGuidance(StringBuilder sb, ReportSnapshot snapshot)
        {
            var parserConfidence = Clamp01(snapshot.Parsing.ParserConfidence);

            sb.AppendLine("## Operational Guidance");
            sb.AppendLine();
            sb.AppendLine("Below is a concise operational interpretation of the current run.");
            sb.AppendLine();
            sb.AppendLine("### Suggested interpretation");
            sb.AppendLine();

            if (parserConfidence < 0.65)
            {
                sb.AppendLine("- Parser output should be treated with caution because extraction confidence is low.");
            }
            else
            {
                sb.AppendLine("- Parser output is strong enough for higher-level interpretation.");
            }

            if (snapshot.Structural.Unresolved > 0)
            {
                sb.AppendLine($"- `{snapshot.Structural.Unresolved}` unresolved candidate(s) still deserve manual inspection.");
            }
            else
            {
                sb.AppendLine("- No unresolved candidates remain after the current refinement threshold.");
            }

            if (snapshot.Architectural.ImplicitCouplingSuspects > 0)
            {
                sb.AppendLine($"- `{snapshot.Architectural.ImplicitCouplingSuspects}` implicit coupling hotspot(s) were detected and may deserve architectural review.");
            }
            else
            {
                sb.AppendLine("- No major implicit coupling hotspots were detected in this snapshot.");
            }

            if (!string.Equals(snapshot.Quality.FitStatus, "Ready", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("- The current run should not be treated as fully ready without additional inspection of gates and structural context.");
            }
            else
            {
                sb.AppendLine("- The current run presents a healthy enough structural baseline for forward interpretation.");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderGlossary(StringBuilder sb)
        {
            sb.AppendLine("## Glossary");
            sb.AppendLine();
            sb.AppendLine("**Structural Candidates**");
            sb.AppendLine("Types initially flagged by the structural scan as weakly supported or potentially disconnected.");
            sb.AppendLine();

            sb.AppendLine("**Pattern Similarity**");
            sb.AppendLine("Candidates whose suspiciousness is softened by recognized architectural patterns such as DI, interfaces or bootstrap references.");
            sb.AppendLine();

            sb.AppendLine("**Unresolved**");
            sb.AppendLine("Candidates that remain unrefuted after refinement and therefore still deserve manual inspection.");
            sb.AppendLine();

            sb.AppendLine("**Implicit Coupling**");
            sb.AppendLine("A heuristic signal indicating concentrated dependency direction or asymmetrical architectural pressure.");
            sb.AppendLine();

            sb.AppendLine("**Overall Readiness**");
            sb.AppendLine("A synthetic, human-readable interpretation combining parser confidence, structural risk and architectural gate signals.");
            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void RenderFooter(StringBuilder sb)
        {
            sb.AppendLine("_Generated by RefactorScope Executive Reporting Layer_");
        }

        private static string YesNo(bool value)
            => value ? "Yes" : "No";

        private static double Clamp01(double value)
            => Math.Max(0, Math.Min(1, value));
    }
}
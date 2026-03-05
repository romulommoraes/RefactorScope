using System.Text.Json;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exports consolidated structural analysis results in JSON format.
    ///
    /// The exported data reflects the semantic pipeline introduced in ADR-EXP-007:
    ///
    /// Structural Candidates
    ///        ↓
    /// Pattern Similarity
    ///        ↓
    /// Unresolved
    ///
    /// This dump is intended for debugging, auditing and external tooling.
    /// </summary>
    public class DumpAnaliseExporter : IExporter
    {
        public string Name => "dumpAnalysis";

        public void Export(AnalysisContext context, ConsolidatedReport report, string outputPath)
        {
            var structuralCandidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            var isolated = report.Results
                .OfType<CoreIsolationResult>()
                .FirstOrDefault()?.IsolatedCoreTypes;

            var entryPoints = report.Results
                .OfType<EntryPointHeuristicResult>()
                .FirstOrDefault()?.EntryPoints;

            var output = new
            {
                StructuralCandidates = structuralCandidates,
                PatternSimilarity = patternSimilarity,
                Unresolved = unresolved,
                IsolatedCoreTypes = isolated,
                EntryPoints = entryPoints
            };

            var json = JsonSerializer.Serialize(
                output,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var path = Path.Combine(outputPath, "RefactorScope_Analysis.json");

            File.WriteAllText(path, json);
        }
    }
}
using System.Text.Json;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Dumps
{
    public sealed class DumpAnaliseExporter : IExporter
    {
        public string Name => "dumpAnalysis";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var structuralCandidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            var isolated = report.Results
                .OfType<CoreIsolationResult>()
                .FirstOrDefault()
                ?.IsolatedCoreTypes;

            var entryPoints = report.Results
                .OfType<EntryPointHeuristicResult>()
                .FirstOrDefault()
                ?.EntryPoints;

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

            var rootOutputPath = context.Config.OutputPath;
            var dumpsRoot = Path.Combine(rootOutputPath, "dumps");
            var moduleDirectory = Path.Combine(dumpsRoot, "analysis");

            Directory.CreateDirectory(moduleDirectory);

            var path = Path.Combine(moduleDirectory, "RefactorScope_Analysis.json");

            File.WriteAllText(path, json);
        }
    }
}
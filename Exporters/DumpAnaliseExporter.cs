using System.Text.Json;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exporta os resultados consolidados da análise.
    /// </summary>
    public class DumpAnaliseExporter : IExporter
    {
        public string Name => "dumpAnalysis";

        public void Export(AnalysisContext context, ConsolidatedReport report, string outputPath)
        {
            var zombies = report.Results
                .OfType<ZombieResult>()
                .FirstOrDefault()?.ZombieTypes;

            var isolated = report.Results
                .OfType<CoreIsolationResult>()
                .FirstOrDefault()?.IsolatedCoreTypes;

            var entry = report.Results
                .OfType<EntryPointHeuristicResult>()
                .FirstOrDefault()?.EntryPoints;

            var output = new
            {
                Zombies = zombies,
                CoreIsolado = isolated,
                EntryPoints = entry
            };

            var json = JsonSerializer.Serialize(
                output,
                new JsonSerializerOptions { WriteIndented = true });

            var path = Path.Combine(outputPath, "RefactorScope_Analysis.json");

            File.WriteAllText(path, json);
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Enum;

namespace RefactorScope.Core.Parsing.Arena;

/// <summary>
/// Executes Arena batch mode over multiple projects and multiple parser strategies.
/// This first version keeps everything in memory and does not export files yet.
/// </summary>
public sealed class ParserArenaOrchestrator
{
    private static readonly ParserStrategy[] ComparativeStrategies =
    {
        ParserStrategy.RegexFast,
        ParserStrategy.Selective,
        ParserStrategy.AdaptiveExperimental,
        ParserStrategy.IncrementalExperimental
    };

    public IReadOnlyList<ParserArenaProjectResult> ExecuteBatch(
        string batchPath,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null,
        Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(batchPath))
            throw new ArgumentException("Batch path cannot be null or empty.", nameof(batchPath));

        if (!Directory.Exists(batchPath))
            throw new DirectoryNotFoundException($"Batch path not found: {batchPath}");

        var projectDirectories = Directory
            .GetDirectories(batchPath)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<ParserArenaProjectResult>();

        foreach (var projectDirectory in projectDirectories)
        {
            var projectName = Path.GetFileName(projectDirectory);

            log?.Invoke($"Project: {projectName}");

            var projectResult = new ParserArenaProjectResult
            {
                ProjectName = projectName,
                ProjectPath = projectDirectory
            };

            foreach (var strategy in ComparativeStrategies)
            {
                IParserCodigo parser;

                try
                {
                    parser = ParserSelector.ResolveParser(strategy, silent: true);
                }
                catch (Exception ex)
                {
                    var failedResolution = ParserArenaRunResult.FromException(
                        projectName,
                        projectDirectory,
                        strategy,
                        strategy.ToString(),
                        ex);

                    projectResult.Runs.Add(failedResolution);
                    log?.Invoke($"  - {strategy}: failed to resolve parser ({ex.Message})");
                    continue;
                }

                try
                {
                    log?.Invoke($"  - Running {parser.Name}...");

                    var parserResult = parser.Parse(
                        projectDirectory,
                        include,
                        exclude);

                    var arenaRun = ParserArenaRunResult.FromParserResult(
                        projectName,
                        projectDirectory,
                        strategy,
                        parserResult);

                    projectResult.Runs.Add(arenaRun);

                    log?.Invoke(
                        $"    status={arenaRun.Status}, confidence={arenaRun.Confidence:0.00}, files={arenaRun.FileCount}, types={arenaRun.TypeCount}, refs={arenaRun.ReferenceCount}, time={arenaRun.ExecutionTime.TotalMilliseconds:0}ms");
                }
                catch (Exception ex)
                {
                    var failedRun = ParserArenaRunResult.FromException(
                        projectName,
                        projectDirectory,
                        strategy,
                        parser.Name,
                        ex);

                    projectResult.Runs.Add(failedRun);

                    log?.Invoke($"    failed: {ex.Message}");
                }
            }

            ParserArenaScoreCalculator.ApplyScores(projectResult);
            results.Add(projectResult);
        }

        return results;
    }
}
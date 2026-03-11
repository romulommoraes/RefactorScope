using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Parsing;
using RefactorScope.Core.Parsing.Arena;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Exporters.Dashboards;
using RefactorScope.Exporters.Styling;
using RefactorScope.Infrastructure;
using Spectre.Console;

namespace RefactorScope.CLI;

/// <summary>
/// Encapsula o fluxo de CLI do Parser Arena / Comparative mode,
/// evitando que o Program.cs concentre responsabilidades visuais
/// e operacionais específicas desse modo.
/// </summary>
public static class ParserArenaCliRunner
{
    public static bool RunSingleProject(
        RefactorScopeConfig config,
        bool isSelfAnalysis)
    {
        try
        {
            TerminalRenderer.Section("Parser Arena");

            var modeLabel = isSelfAnalysis
                ? "Comparative mode selected for self analysis."
                : "Comparative mode selected for single-project analysis.";

            TerminalRenderer.Step(modeLabel);

            if (!Directory.Exists(config.RootPath))
            {
                Console.WriteLine($"[ERRO] Root path não encontrada: {config.RootPath}");
                return false;
            }

            var projectName = Path.GetFileName(
                config.RootPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            if (string.IsNullOrWhiteSpace(projectName))
                projectName = config.RootPath;

            var projectResult = new ParserArenaProjectResult
            {
                ProjectName = projectName,
                ProjectPath = config.RootPath
            };

            var strategies = new[]
            {
            ParserStrategy.RegexFast,
            ParserStrategy.Selective,
            ParserStrategy.AdaptiveExperimental,
            ParserStrategy.IncrementalExperimental
        };

            TerminalRenderer.Success($"Arena target: {config.RootPath}");

            var effectiveExcludes = BuildSingleProjectExcludes(config);

            var runs =
                TerminalRenderer.WithSpinner(
                    "Running comparative parser analysis...",
                    () =>
                    {
                        var results = new List<ParserArenaRunResult>();

                        foreach (var strategy in strategies)
                        {
                            IParserCodigo parser;

                            try
                            {
                                parser = ParserSelector.ResolveParser(strategy, silent: true);
                            }
                            catch (Exception ex)
                            {
                                results.Add(
                                    ParserArenaRunResult.FromException(
                                        projectName,
                                        config.RootPath,
                                        strategy,
                                        strategy.ToString(),
                                        ex));

                                continue;
                            }

                            try
                            {
                                var parserResult = parser.Parse(
                                    config.RootPath,
                                    config.Include,
                                    effectiveExcludes);

                                results.Add(
                                    ParserArenaRunResult.FromParserResult(
                                        projectName,
                                        config.RootPath,
                                        strategy,
                                        parserResult));
                            }
                            catch (Exception ex)
                            {
                                results.Add(
                                    ParserArenaRunResult.FromException(
                                        projectName,
                                        config.RootPath,
                                        strategy,
                                        parser.Name,
                                        ex));
                            }
                        }

                        return results;
                    });

            projectResult.Runs.AddRange(runs);

            ParserArenaScoreCalculator.ApplyScores(projectResult);

            var wrappedResults = new List<ParserArenaProjectResult> { projectResult };

            RenderDetailedResults(wrappedResults);
            RenderWinners(wrappedResults);

            TerminalRenderer.Success("Comparative analysis executed successfully.");
            Console.WriteLine("[INFO] Next step: add optional batch mode as a separate startup mode.");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO NO COMPARATIVE]: {ex.Message}");
            CrashLogger.Log(ex, "COMPARATIVE_SINGLE_PROJECT");
            return false;
        }
    }

    public static bool RunBatch(RefactorScopeConfig config)
    {
        try
        {
            TerminalRenderer.Section("Parser Arena");
            TerminalRenderer.Step("Comparative batch mode selected.");

            var batchPath = ResolveBatchPath(config);

            if (!Directory.Exists(batchPath))
            {
                Console.WriteLine($"[ERRO] Pasta Batch não encontrada em: {batchPath}");
                Console.WriteLine("[INFO] O modo batch procura uma pasta 'Batch' na raiz acima do projeto efetivo.");
                return false;
            }

            var projectDirectories = Directory
                .GetDirectories(batchPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (projectDirectories.Count == 0)
            {
                Console.WriteLine($"[ERRO] Nenhum projeto encontrado na pasta Batch: {batchPath}");
                return false;
            }

            TerminalRenderer.Success(
                $"Arena initialized. Projects detected: {projectDirectories.Count}");

            var strategies = new[]
            {
                ParserStrategy.RegexFast,
                ParserStrategy.Selective,
                ParserStrategy.AdaptiveExperimental,
                ParserStrategy.IncrementalExperimental
            };

            var effectiveExcludes = BuildBatchExcludes(config);
            var projectResults = new List<ParserArenaProjectResult>();

            TerminalRenderer.WithSpinner(
                "Running comparative parser batch...",
                () =>
                {
                    foreach (var projectDirectory in projectDirectories)
                    {
                        var effectiveProjectRoot =
                            ProjectRootResolver.ResolveEffectiveProjectRoot(projectDirectory);

                        var projectName = Path.GetFileName(
                            effectiveProjectRoot.TrimEnd(
                                Path.DirectorySeparatorChar,
                                Path.AltDirectorySeparatorChar));

                        if (string.IsNullOrWhiteSpace(projectName))
                            projectName = effectiveProjectRoot;

                        var projectResult = new ParserArenaProjectResult
                        {
                            ProjectName = projectName,
                            ProjectPath = effectiveProjectRoot
                        };

                        foreach (var strategy in strategies)
                        {
                            IParserCodigo parser;

                            try
                            {
                                parser = ParserSelector.ResolveParser(strategy, silent: true);
                            }
                            catch (Exception ex)
                            {
                                projectResult.Runs.Add(
                                    ParserArenaRunResult.FromException(
                                        projectName,
                                        effectiveProjectRoot,
                                        strategy,
                                        strategy.ToString(),
                                        ex));

                                continue;
                            }

                            try
                            {
                                var parserResult = parser.Parse(
                                    effectiveProjectRoot,
                                    config.Include,
                                    effectiveExcludes);

                                projectResult.Runs.Add(
                                    ParserArenaRunResult.FromParserResult(
                                        projectName,
                                        effectiveProjectRoot,
                                        strategy,
                                        parserResult));
                            }
                            catch (Exception ex)
                            {
                                projectResult.Runs.Add(
                                    ParserArenaRunResult.FromException(
                                        projectName,
                                        effectiveProjectRoot,
                                        strategy,
                                        parser.Name,
                                        ex));
                            }
                        }

                        ParserArenaScoreCalculator.ApplyScores(projectResult);
                        projectResults.Add(projectResult);

                        var themeFileName = "theme-ember-ops.css"; // ou o resolver que você já usa
                        DashboardAssetCopier.CopyAll(config.OutputPath, themeFileName);

                        var arenaHtmlPath = Path.Combine(config.OutputPath, "ParserArenaDashboard.html");

                        var exporter = new ParserArenaDashboardExporter();
                        exporter.Export(projectResults, arenaHtmlPath, themeFileName);
                    }

                    return true;
                });

            RenderDetailedResults(projectResults);
            RenderWinners(projectResults);

            TerminalRenderer.Success("Comparative batch executed successfully.");
            Console.WriteLine("[INFO] Next step: export Arena JSON, CSV and comparative HTML dashboard.");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO NO COMPARATIVE]: {ex.Message}");
            CrashLogger.Log(ex, "COMPARATIVE_ARENA");
            return false;
        }
    }

    /// <summary>
    /// Batch is resolved from the effective project root.
    ///
    /// Example:
    ///   Configured root: C:\Repos\RefactorScope
    ///   Effective root : C:\Repos\RefactorScope\RefactorScope
    ///   Batch          : C:\Repos\RefactorScope\Batch
    /// </summary>
    public static string ResolveBatchPath(RefactorScopeConfig config)
    {
        return @"C:\Users\romul\source\repos\RefactorScope\Batch";
    }

    private static void RenderDetailedResults(
        IReadOnlyList<ParserArenaProjectResult> results)
    {
        if (results == null || results.Count == 0)
            return;

        TerminalRenderer.Section("Parser Arena Detailed Summary");

        foreach (var project in results)
        {
            AnsiConsole.MarkupLine($"[bold yellow]{project.ProjectName}[/]");

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Expand()
                .AddColumn("Parser")
                .AddColumn("Strategy")
                .AddColumn("Status")
                .AddColumn("Score")
                .AddColumn("Confidence")
                .AddColumn("Types")
                .AddColumn("Refs")
                .AddColumn("Time");

            foreach (var run in project.OrderedRuns)
            {
                var statusMarkup = run.Status switch
                {
                    ParseStatus.Success => "[green]Success[/]",
                    ParseStatus.PlausibilityWarning => "[yellow]Warning[/]",
                    ParseStatus.FallbackTriggered => "[orange1]Fallback[/]",
                    ParseStatus.Failed => "[red]Failed[/]",
                    _ => run.Status.ToString()
                };

                table.AddRow(
                    ShortenParserName(run.ParserName),
                    ShortenStrategyName(run.Strategy),
                    statusMarkup,
                    run.ComparativeScore.ToString("0.00"),
                    run.Confidence.ToString("0.00"),
                    run.TypeCount.ToString(),
                    run.ReferenceCount.ToString(),
                    $"{run.ExecutionTime.TotalMilliseconds:0}ms");
            }

            AnsiConsole.Write(table);
            Console.WriteLine();
        }
    }

    private static void RenderWinners(
        IReadOnlyList<ParserArenaProjectResult> results)
    {
        if (results == null || results.Count == 0)
            return;

        TerminalRenderer.Section("Best Parser Per Project");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand()
            .AddColumn("Project")
            .AddColumn("Winner")
            .AddColumn("Strategy")
            .AddColumn("Status")
            .AddColumn("Score")
            .AddColumn("Confidence")
            .AddColumn("Types")
            .AddColumn("Refs")
            .AddColumn("Time");

        foreach (var project in results)
        {
            var best = project.BestRun;

            if (best == null)
            {
                table.AddRow(
                    project.ProjectName,
                    "N/A",
                    "N/A",
                    "N/A",
                    "0.00",
                    "0.00",
                    "0",
                    "0",
                    "0ms");

                continue;
            }

            var statusMarkup = best.Status switch
            {
                ParseStatus.Success => "[green]Success[/]",
                ParseStatus.PlausibilityWarning => "[yellow]Warning[/]",
                ParseStatus.FallbackTriggered => "[orange1]Fallback[/]",
                ParseStatus.Failed => "[red]Failed[/]",
                _ => best.Status.ToString()
            };

            table.AddRow(
                project.ProjectName,
                ShortenParserName(best.ParserName),
                ShortenStrategyName(best.Strategy),
                statusMarkup,
                best.ComparativeScore.ToString("0.00"),
                best.Confidence.ToString("0.00"),
                best.TypeCount.ToString(),
                best.ReferenceCount.ToString(),
                $"{best.ExecutionTime.TotalMilliseconds:0}ms");
        }

        AnsiConsole.Write(table);
        Console.WriteLine();
    }

    private static string ShortenStrategyName(ParserStrategy strategy)
    {
        return strategy switch
        {
            ParserStrategy.RegexFast => "Regex",
            ParserStrategy.Selective => "Selective",
            ParserStrategy.AdaptiveExperimental => "Adaptive",
            ParserStrategy.IncrementalExperimental => "Incremental",
            ParserStrategy.Comparative => "Comparative",
            _ => strategy.ToString()
        };
    }

    private static string ShortenParserName(string parserName)
    {
        return parserName switch
        {
            "CSharpRegex" => "Regex",
            "CSharpTextual" => "Textual",
            "HybridSelectiveParser" => "Selective",
            "HybridParser (Adaptive)" => "Adaptive",
            "HybridParser (Incremental)" => "Incremental",
            _ => parserName
        };
    }

    private static IEnumerable<string> BuildSingleProjectExcludes(RefactorScopeConfig config)
    {
        var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (config.Exclude != null)
        {
            foreach (var item in config.Exclude)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    excludes.Add(item.Trim());
            }
        }

        excludes.Add("bin");
        excludes.Add("bin/**");
        excludes.Add("obj");
        excludes.Add("obj/**");
        excludes.Add("dumps");
        excludes.Add("dumps/**");
        excludes.Add("datasets");
        excludes.Add("datasets/**");
        excludes.Add("output");
        excludes.Add("output/**");

        return excludes;
    }

    private static IEnumerable<string> BuildBatchExcludes(RefactorScopeConfig config)
    {
        var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (config.Exclude != null)
        {
            foreach (var item in config.Exclude)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    excludes.Add(item.Trim());
            }
        }

        excludes.Add("bin");
        excludes.Add("bin/**");
        excludes.Add("obj");
        excludes.Add("obj/**");
        excludes.Add("dumps");
        excludes.Add("dumps/**");
        excludes.Add("datasets");
        excludes.Add("datasets/**");
        excludes.Add("output");
        excludes.Add("output/**");

        return excludes;
    }
}
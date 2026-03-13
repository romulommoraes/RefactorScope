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

public static class ParserArenaCliRunner
{
    public static bool RunSingleProject(
        RefactorScopeConfig config,
        AnalysisScope scope)
    {
        try
        {
            TerminalRenderer.Section("Parser Comparative");

            var modeLabel = scope == AnalysisScope.Self
                ? "Comparative mode selected for self analysis."
                : "Comparative mode selected for current target.";

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

            TerminalRenderer.Success($"Comparative target: {config.RootPath}");

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

            RunSingleProjectComparativeDashboardExporter(
                wrappedResults,
                ResolveArenaOutputPath(),
                ResolveThemeFileName(config),
                scope);

            TerminalRenderer.Success("Comparative analysis executed successfully.");
            Console.WriteLine("[INFO] Comparative HTML dashboard generated.");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO NO COMPARATIVE]: {ex.Message}");
            CrashLogger.Log(ex, "COMPARATIVE_SINGLE_PROJECT");
            return false;
        }
    }

    public static bool RunBatch(
        RefactorScopeConfig config,
        AnalysisScope scope)
    {
        try
        {
            TerminalRenderer.Section("Parser Arena");
            TerminalRenderer.Step(
                scope == AnalysisScope.Self
                    ? "Batch Arena selected under self-analysis scope."
                    : "Batch Arena selected under normal-analysis scope.");

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
                    }

                    return true;
                });

            RenderDetailedResults(projectResults);
            RenderWinners(projectResults);

            RunBatchArenaDashboardExporter(
                projectResults,
                ResolveArenaOutputPath(),
                ResolveThemeFileName(config));

            TerminalRenderer.Success("Comparative batch executed successfully.");
            Console.WriteLine("[INFO] Arena comparative HTML dashboard generated.");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO NO COMPARATIVE]: {ex.Message}");
            CrashLogger.Log(ex, "COMPARATIVE_ARENA");
            return false;
        }
    }

    public static string ResolveBatchPath(RefactorScopeConfig config)
    {
        return @"C:\Users\romul\source\repos\RefactorScope\Batch";
    }

    private static string ResolveArenaOutputPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Batch");
        Directory.CreateDirectory(outputPath);
        return outputPath;
    }

    private static void RunSingleProjectComparativeDashboardExporter(
        IReadOnlyList<ParserArenaProjectResult> results,
        string outputDirectory,
        string themeFileName,
        AnalysisScope scope)
    {
        try
        {
            if (results == null || results.Count == 0)
                return;

            Directory.CreateDirectory(outputDirectory);

            DashboardAssetCopier.CopyAll(outputDirectory, themeFileName);

            var fileName = scope == AnalysisScope.Self
                ? "ParserComparativeSelfDashboard.html"
                : "ParserComparativeDashboard.html";

            var htmlPath = Path.Combine(outputDirectory, fileName);

            var exporter = new ParserArenaDashboardExporter();
            exporter.Export(results, htmlPath, themeFileName);

            TerminalRenderer.Success(
                scope == AnalysisScope.Self
                    ? "parser-comparative-self-dashboard gerado"
                    : "parser-comparative-dashboard gerado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO AO GERAR HTML - parser-comparative-dashboard]: {ex.Message}");
            CrashLogger.Log(ex, "HTML_EXPORT_PARSER-COMPARATIVE-DASHBOARD");
        }
    }

    private static void RunBatchArenaDashboardExporter(
        IReadOnlyList<ParserArenaProjectResult> results,
        string outputDirectory,
        string themeFileName)
    {
        try
        {
            if (results == null || results.Count == 0)
                return;

            Directory.CreateDirectory(outputDirectory);

            DashboardAssetCopier.CopyAll(outputDirectory, themeFileName);

            var htmlPath = Path.Combine(outputDirectory, "ParserArenaDashboard.html");

            var exporter = new ParserArenaDashboardExporter();
            exporter.Export(results, htmlPath, themeFileName);

            TerminalRenderer.Success("parser-arena-dashboard gerado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO AO GERAR HTML - parser-arena-dashboard]: {ex.Message}");
            CrashLogger.Log(ex, "HTML_EXPORT_PARSER-ARENA-DASHBOARD");
        }
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
                    Clamp01(run.Confidence).ToString("0.00"),
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
                Clamp01(best.Confidence).ToString("0.00"),
                best.TypeCount.ToString(),
                best.ReferenceCount.ToString(),
                $"{best.ExecutionTime.TotalMilliseconds:0}ms");
        }

        AnsiConsole.Write(table);
        Console.WriteLine();
    }

    private static string ResolveThemeFileName(RefactorScopeConfig? config)
    {
        try
        {
            var themeName = TryReadDashboardTheme(config);
            return DashboardThemeSelector.ResolveFileName(themeName);
        }
        catch
        {
            return DashboardThemeSelector.DefaultThemeFile;
        }
    }

    private static string? TryReadDashboardTheme(RefactorScopeConfig? config)
    {
        return config?.Dashboard?.Theme;
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

    private static double Clamp01(double value)
        => Math.Max(0, Math.Min(1, value));
}
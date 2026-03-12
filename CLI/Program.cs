using RefactorScope.Analyzers;
using RefactorScope.Analyzers.Solid;
using RefactorScope.CLI;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Datasets;
using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Parsing;
using RefactorScope.Core.Parsing.Arena;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Core.Results;
using RefactorScope.Execution.Dump;
using RefactorScope.Exporters.Adapters;
using RefactorScope.Exporters.Dashboards;
using RefactorScope.Exporters.Datasets;
using RefactorScope.Exporters.Dumps;
using RefactorScope.Exporters.Infrastructure;
using RefactorScope.Exporters.Reports;
using RefactorScope.Exporters.Styling;
using RefactorScope.Infrastructure;
using RefactorScope.Statistics.Engines;
using Spectre.Console;

Console.WriteLine();

IParserResult? parsingResult = null;

// =====================================================
// PIPELINE PRINCIPAL
// =====================================================

if (!RunConfiguration(out var config, out var executionPlan))
    return;

switch (executionPlan.Mode)
{
    case ExecutionMode.BatchArena:
        if (!ParserArenaCliRunner.RunBatch(config, executionPlan.Scope))
            return;
        return;

    case ExecutionMode.Comparative:
        if (!ParserArenaCliRunner.RunSingleProject(config, executionPlan.Scope))
            return;
        return;

    case ExecutionMode.SingleParser:
    default:
        if (executionPlan.SelectedParser == null)
        {
            TerminalRenderer.Warn("Nenhum parser concreto foi selecionado para Single Parser.");
            return;
        }

        config.Parser = executionPlan.SelectedParser.Value.ToString();

        if (!RunParsing(
                config,
                executionPlan.SelectedParser.Value,
                out var context,
                out parsingResult))
        {
            return;
        }

        RunTopLevelRecovery(context);

        if (!RunAnalysis(context, out var report))
            return;

        RunArchitecturalHygiene(report);

        PrintStructuralBreakdown(report);

        if (!RunExport(config, context, report, parsingResult))
            return;

        RunVisualization(report);

        ApplyCiExitCode(report);
        return;
}

/* =====================================================
   CONFIGURAÇÃO
   ===================================================== */

static bool RunConfiguration(
    out RefactorScopeConfig config,
    out StartupExecutionPlan executionPlan)
{
    config = null!;
    executionPlan = null!;

    try
    {
        const bool enableInteractiveSelector = true;
        const bool enableParserSelector = true;

        executionPlan =
            StartupExecutionPlanSelector.Resolve(
                "refactorscope.json",
                "refactorscope_v1_1_self.json",
                enableInteractiveSelector,
                enableParserSelector);

        config = ConfigLoader.Load(executionPlan.ConfigPath);

        ConfigValidator.Validate(config);

        config.FitnessGates =
            FitnessGateConfigResolver.Resolve(
                config.FitnessGates,
                msg => TerminalRenderer.Warn(msg));

        if (!Directory.Exists(config.RootPath))
        {
            Console.WriteLine($"[ERRO] Invalid rootPath: {config.RootPath}");
            return false;
        }

        TerminalRenderer.ShowHeader(config.RootPath);

        TerminalRenderer.Step(
            $"Scope: {executionPlan.Scope} | Mode: {executionPlan.Mode}");

        if (executionPlan.Mode == ExecutionMode.SingleParser &&
            executionPlan.SelectedParser != null)
        {
            TerminalRenderer.Step(
                $"Selected parser: {executionPlan.SelectedParser.Value}");
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA CONFIGURAÇÃO]: {ex.Message}");
        CrashLogger.Log(ex, "CONFIGURATION");
        return false;
    }
}

/* =====================================================
   PARSING
   ===================================================== */

static bool RunParsing(
    RefactorScopeConfig config,
    ParserStrategy parserStrategy,
    out AnalysisContext context,
    out IParserResult? parsingResult)
{
    context = null!;
    parsingResult = null;

    try
    {
        var parser = ParserSelector.ResolveParser(parserStrategy);

        var result =
            TerminalRenderer.WithSpinner(
                "Parsing código...",
                () => parser.Parse(
                    config.RootPath,
                    config.Include,
                    config.Exclude));

        parsingResult = result;

        if (result.Status == ParseStatus.Failed || result.Model == null)
        {
            Console.WriteLine(
                $"[ERRO CRÍTICO NO PARSING]: O parser {result.ParserName} falhou.");

            if (result.Error != null)
                CrashLogger.Log(result.Error, "PARSING_CRITICAL");

            return false;
        }

        TerminalRenderer.ParsingStrategy(result.ParserName);

        if (result.UsedFallback)
            TerminalRenderer.ParsingFallback("Primary", "Fallback");

        if (result.ParserName.Contains("Merge"))
            TerminalRenderer.ParsingMerge();

        TerminalRenderer.ParsingSummary(
            result.Model.Arquivos.Count,
            result.Model.Tipos.Count,
            result.Model.Referencias.Count,
            result.Stats?.ExecutionTime ?? TimeSpan.Zero);

        TerminalRenderer.Success(
            $"Parsing concluído em {result.Stats?.ExecutionTime.TotalMilliseconds:F0}ms usando {result.ParserName}");

        WarnDuplicateTipos(result.Model.Tipos);

        context = new AnalysisContext(config, result.Model);

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NÃO TRATADO NO PARSING]: {ex.Message}");
        CrashLogger.Log(ex, "PARSING_UNHANDLED");
        return false;
    }
}

static void WarnDuplicateTipos(IEnumerable<TipoInfo> tipos)
{
    var duplicates = tipos
        .GroupBy(t => $"{t.Namespace}.{t.Name}")
        .Where(g => g.Count() > 1);

    foreach (var duplicate in duplicates)
    {
        TerminalRenderer.Warn(
            $"Tipo duplicado detectado: {duplicate.Key} (ocorrências: {duplicate.Count()})");
    }
}

/* =====================================================
   RECOVERY
   ===================================================== */

static void RunTopLevelRecovery(AnalysisContext context)
{
    try
    {
        // TopLevelReferenceRecovery.Run(context);

        /*
         FUTURE IMPLEMENTATION

         Strategy:
         1. Detect unreferenced types
         2. Scan bootstrap files (Program.cs)
         3. Sanitize comments and strings
         4. Detect textual references
         5. Rebuild structural snapshot with recovered references
        */
    }
    catch (Exception ex)
    {
        CrashLogger.Log(ex, "TOPLEVEL_RECOVERY");
    }
}

/* =====================================================
   ANÁLISE
   ===================================================== */

static bool RunAnalysis(
    AnalysisContext context,
    out ConsolidatedReport report)
{
    report = null!;

    try
    {
        var analyzers = BuildAnalyzers(context);
        var orchestrator = new AnalysisOrchestrator(analyzers);

        report =
            TerminalRenderer.WithSpinner(
                "Executando motor de análise...",
                () => orchestrator.Execute(context));

        TerminalRenderer.Success("Análise concluída");

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA ANÁLISE]: {ex.Message}");
        CrashLogger.Log(ex, "ANALYSIS");
        return false;
    }
}

static List<IAnalyzer> BuildAnalyzers(AnalysisContext context)
{
    var analyzers = new List<IAnalyzer>
    {
        new ProjectStructureAnalyzer()
    };

    if (IsEnabled(context, "zombie"))
        analyzers.Add(new StructuralCandidateAnalyzer());

    if (IsEnabled(context, "zombieRefinement") &&
        context.Config.StructuralCandidateDetection.EnableRefinement)
    {
        analyzers.Add(new StructuralCandidateRefinementAnalyzer());
    }

    if (IsEnabled(context, "architecture"))
        analyzers.Add(new ArchitecturalClassificationAnalyzer());

    if (IsEnabled(context, "entrypoints"))
        analyzers.Add(new EntryPointHeuristicAnalyzer());

    if (IsEnabled(context, "coreIsolation"))
        analyzers.Add(new CoreIsolationAnalyzer());

    if (IsEnabled(context, "coupling"))
    {
        analyzers.Add(new CouplingAnalyzer());
        analyzers.Add(new ImplicitCouplingAnalyzer());
    }

    if (IsEnabled(context, "statistics"))
        analyzers.Add(new StatisticsValidationAnalyzer());

    if (IsEnabled(context, "solid"))
        analyzers.Add(new SolidAnalyzer());

    analyzers.Add(new FitnessGateAnalyzer(context.Config.FitnessGates));

    return analyzers;
}

/* =====================================================
   HIGIENE
   ===================================================== */

static void RunArchitecturalHygiene(ConsolidatedReport report)
{
    var hygieneAnalyzer = new ArchitecturalHygieneAnalyzer();
    var hygiene = hygieneAnalyzer.Analyze(report);

    TerminalRenderer.Section("Code Hygiene");
    TerminalRenderer.HygieneSummary(hygiene);
}

/* =====================================================
   DEBUG
   ===================================================== */

static void PrintStructuralBreakdown(ConsolidatedReport report)
{
    var breakdown = report.GetStructuralCandidateBreakdown();

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold cyan]Structural Candidates Analysis Breakdown[/]");

    AnsiConsole.WriteLine($"Structural Candidates : {breakdown.StructuralCandidates}");
    AnsiConsole.WriteLine($"Probabilistic Confirmed (≥ {report.UnresolvedProbabilityThreshold:0.00}) : {breakdown.ProbabilisticConfirmed}");
    AnsiConsole.WriteLine($"Absolved : {breakdown.PatternSimilarity}");
    AnsiConsole.WriteLine($"Reduction : {breakdown.ReductionRate:P1}");
    AnsiConsole.WriteLine();
}

/* =====================================================
   EXPORTAÇÃO
   ===================================================== */

static bool RunExport(
    RefactorScopeConfig config,
    AnalysisContext context,
    ConsolidatedReport report,
    IParserResult? parsingResult)
{
    try
    {
        TerminalRenderer.Step("Gerando dumps, datasets e dashboards...");

        var pathResolver = PrepareExportInfrastructure(
            config,
            context,
            report,
            parsingResult);

        var rootOutputPath = pathResolver.RootOutputPath;

        var datasetBuilders = BuildDatasetBuilders();
        var coreExporters = BuildCoreExporters(datasetBuilders);
        var htmlExporters = BuildHtmlExportersWithoutHub();

        var strategy = DumpStrategyResolver.Resolve(config);

        // -------------------------------------------------
        // Bloco 1: exportadores gerais
        // -------------------------------------------------
        strategy.Execute(context, report, coreExporters);

        // -------------------------------------------------
        // Bloco 2: dashboards HTML especializados
        // -------------------------------------------------
        RunHtmlExporters(htmlExporters, context, report, rootOutputPath);

        ArchitecturalDashboardExporterAdapter.ExportDirect(
            context,
            report,
            parsingResult,
            rootOutputPath);

        RunParsingDashboardExporter(context, parsingResult, rootOutputPath);

        QualityDashboardExporterAdapter.ExportDirect(
            context,
            report,
            parsingResult?.ParserName ?? "Unavailable",
            parsingResult?.Confidence ?? 0,
            parsingResult?.Stats?.ExecutionTime ?? TimeSpan.Zero,
            parsingResult?.Model?.Arquivos.Count ?? 0,
            parsingResult?.Model?.Tipos.Count ?? 0,
            parsingResult?.Model?.Referencias.Count ?? 0,
            rootOutputPath);

        // -------------------------------------------------
        // Bloco 3: hub final
        // -------------------------------------------------
        RunHtmlHubExporter(context, report, parsingResult, rootOutputPath);

        TerminalRenderer.Success(
            "Dumps, datasets e dashboards gerados com sucesso");

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA EXPORTAÇÃO]: {ex.Message}");
        CrashLogger.Log(ex, "EXPORT");
        return false;
    }
}

/* =====================================================
   VISUALIZAÇÃO
   ===================================================== */

static void RunVisualization(ConsolidatedReport report)
{
    TerminalRenderer.Section("Architectural Health");

    var architecture = report.GetResult<ArchitecturalClassificationResult>();
    if (architecture == null)
        return;

    var modules = architecture.Items.GroupBy(i => i.Folder);
    var isolated = report.GetResult<CoreIsolationResult>();
    var coupling = report.GetResult<CouplingResult>();
    var effectiveUnresolved = report.GetEffectiveUnresolvedCandidates();

    var rows = new List<(string Module, double Score, string Unresolved, double Coupling, double Isolation)>();

    foreach (var module in modules)
    {
        var total = module.Count();
        if (total == 0)
            continue;

        var zombieCount =
            effectiveUnresolved.Count(z => module.Any(m => m.TypeName == z));

        var isolatedCount =
            isolated?.IsolatedCoreTypes.Count(i => module.Any(m => m.TypeName == i)) ?? 0;

        var fanOut =
            coupling?.ModuleFanOut.GetValueOrDefault(module.Key) ?? 0;

        var zombieRate = zombieCount / (double)total;
        var isolationRate = isolatedCount / (double)total;
        var couplingRate = fanOut / (double)total;
        var coreTypes = module.Count(t => t.Layer == "Core");

        var zombieDisplay = $"{zombieCount} ({zombieRate:0%})";

        var score = ArchitecturalScoreCalculator.Calculate(
            module.Key,
            total,
            zombieCount,
            isolatedCount,
            fanOut,
            coreTypes);

        rows.Add((
            module.Key,
            score,
            zombieDisplay,
            couplingRate,
            isolationRate));
    }

    TerminalRenderer.RenderArchitecturalHealthTable(rows);
    TerminalRenderer.CouplingHeuristicNotice();

    RenderFitnessGates(report);
}

/* =====================================================
   FITNESS GATES
   ===================================================== */

static void RenderFitnessGates(ConsolidatedReport report)
{
    var gates = report.Results
        .OfType<FitnessGateResult>()
        .FirstOrDefault();

    if (gates == null)
        return;

    TerminalRenderer.Section("Architecture Fitness Gates");

    foreach (var gate in gates.Gates)
    {
        switch (gate.Status)
        {
            case GateStatus.Pass:
                AnsiConsole.MarkupLine($"[green]✔ {gate.GateName}[/]");
                break;

            case GateStatus.Warn:
                AnsiConsole.MarkupLine($"[yellow]! {gate.GateName}[/] {gate.Message}");
                break;

            case GateStatus.Fail:
                AnsiConsole.MarkupLine($"[red]✖ {gate.GateName}[/] {gate.Message}");
                break;
        }
    }

    if (gates.HasFailure)
        AnsiConsole.MarkupLine("[bold red]Arquitetura NÃO pronta para CI/CD[/]");
    else
        AnsiConsole.MarkupLine("[bold green]Arquitetura pronta para CI/CD[/]");
}

/* =====================================================
   CI EXIT CODE
   ===================================================== */

static void ApplyCiExitCode(ConsolidatedReport report)
{
    var gates = report.Results
        .OfType<FitnessGateResult>()
        .FirstOrDefault();

    if (gates == null)
        return;

    Environment.ExitCode = gates.HasFailure ? 1 : 0;
}

/* =====================================================
   HELPERS
   ===================================================== */

static ExportOptions BuildExportOptions(RefactorScopeConfig config)
{
    return new ExportOptions
    {
        Enabled = true,
        OutputDirectory = config.OutputPath,

        Reports = new ReportExportOptions
        {
            GenerateHubHtml = true,
            GenerateDashboardsHtml = true,
            GenerateExecutiveReport = true,
            GenerateArchitecturalReport = true
        },

        Datasets = new DatasetExportOptions
        {
            GenerateAnalysisJson = true,
            GenerateSnapshotJson = true,
            GenerateCsvs = true
        },

        Dumps = new DumpExportOptions
        {
            Enabled = true,
            GenerateFullDump = true,
            IncludeTimestampInFileName = true,
            NormalizeWhitespace = true
        },

        Trends = new TrendExportOptions
        {
            Enabled = true,
            GenerateStructuralHistory = true
        }
    };
}

static ExportPackage BuildExportPackage(
    RefactorScopeConfig config,
    AnalysisContext context,
    ConsolidatedReport report,
    IParserResult? parsingResult)
{
    return new ExportPackage
    {
        RootOutputPath = config.OutputPath,
        AnalysisContext = context,
        Report = report,
        ParsingResult = parsingResult,
        GeneratedAtUtc = DateTime.UtcNow
    };
}

static ExportPathResolver PrepareExportInfrastructure(
    RefactorScopeConfig config,
    AnalysisContext context,
    ConsolidatedReport report,
    IParserResult? parsingResult)
{
    var exportOptions = BuildExportOptions(config);

    var exportPackage = BuildExportPackage(
        config,
        context,
        report,
        parsingResult);

    var pathResolver = new ExportPathResolver(exportPackage.RootOutputPath);

    var orchestrator = new ExportOrchestrator(exportOptions, pathResolver);
    orchestrator.Prepare();

    return pathResolver;
}

static List<IAnalyticalDatasetBuilder> BuildDatasetBuilders()
{
    return new List<IAnalyticalDatasetBuilder>
    {
        new GlobalTypesDatasetBuilder(),
        new StructuralOverviewDatasetBuilder(),
        new ArchitecturalHealthDatasetBuilder(),
        new ModuleContributionDatasetBuilder(),
        new TypeContributionDatasetBuilder(),
        new GlobalMetricsDatasetBuilder(),
        new StructuralScoreDatasetBuilder(),
        new StructuralTrendDatasetBuilder()
    };
}

static List<IExporter> BuildCoreExporters(
    List<IAnalyticalDatasetBuilder> datasetBuilders)
{
    return new List<IExporter>
    {
        new DumpAnaliseExporter(),
        new DumpIaExporter(),
        new DatasetExporter(datasetBuilders),
        new ProjectStructureExporter(),
        new FitnessGateCsvExporter()
    };
}

static List<IExporter> BuildHtmlExportersWithoutHub()
{
    return new List<IExporter>
    {
        new StructuralDashboardExporterAdapter()
    };
}

static bool IsEnabled(AnalysisContext context, string analyzerName)
{
    return context.Config.Analyzers.TryGetValue(analyzerName, out var enabled)
           && enabled;
}

static void RunHtmlExporters(
    IEnumerable<IExporter> exporters,
    AnalysisContext context,
    ConsolidatedReport report,
    string outputDirectory)
{
    Directory.CreateDirectory(outputDirectory);

    foreach (var exporter in exporters)
    {
        try
        {
            exporter.Export(context, report, outputDirectory);
            TerminalRenderer.Success($"{exporter.Name} gerado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO AO GERAR HTML - {exporter.Name}]: {ex.Message}");
            CrashLogger.Log(ex, $"HTML_EXPORT_{exporter.Name.ToUpperInvariant()}");
        }
    }
}

static void RunParsingDashboardExporter(
    AnalysisContext context,
    IParserResult? parsingResult,
    string outputDirectory)
{
    try
    {
        if (parsingResult == null)
            return;

        Directory.CreateDirectory(outputDirectory);

        var themeFileName = ResolveThemeFileName(context);
        DashboardAssetCopier.CopyAll(outputDirectory, themeFileName);

        var htmlPath = Path.Combine(outputDirectory, "ParsingDashboard.html");

        var exporter = new ParsingDashboardExporter();
        exporter.Export(parsingResult, htmlPath, themeFileName);

        TerminalRenderer.Success("parsing-dashboard gerado");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO AO GERAR HTML - parsing-dashboard]: {ex.Message}");
        CrashLogger.Log(ex, "HTML_EXPORT_PARSING-DASHBOARD");
    }
}

static void RunParserArenaDashboardExporter(
    AnalysisContext context,
    IReadOnlyList<ParserArenaProjectResult> results,
    string outputDirectory)
{
    try
    {
        if (results == null || results.Count == 0)
            return;

        Directory.CreateDirectory(outputDirectory);

        var themeFileName = ResolveThemeFileName(context);
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

static void RunHtmlHubExporter(
    AnalysisContext context,
    ConsolidatedReport report,
    IParserResult? parsingResult,
    string outputDirectory)
{
    try
    {
        Directory.CreateDirectory(outputDirectory);

        var exporter = new HtmlDashboardExporter();

        exporter.ExportHub(
            context,
            report,
            parsingResult,
            outputDirectory);

        TerminalRenderer.Success("html-dashboard-hub gerado");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO AO GERAR HUB HTML]: {ex.Message}");
        CrashLogger.Log(ex, "HTML_EXPORT_HUB");
    }
}

static string ResolveThemeFileName(AnalysisContext context)
{
    try
    {
        var config = context?.Config;
        var themeName = TryReadDashboardTheme(config);
        return DashboardThemeSelector.ResolveFileName(themeName);
    }
    catch
    {
        return DashboardThemeSelector.DefaultThemeFile;
    }
}

static string? TryReadDashboardTheme(RefactorScopeConfig? config)
{
    if (config == null)
        return null;

    var prop = config.GetType().GetProperty("DashboardTheme");
    if (prop == null)
        return null;

    return prop.GetValue(config) as string;
}
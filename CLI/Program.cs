using RefactorScope.Analyzers;
using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Datasets;
using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Parsing;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Core.Reporting;
using RefactorScope.Core.Results;
using RefactorScope.Execution.Dump;
using RefactorScope.Exporters;
using RefactorScope.Infrastructure;
using Spectre.Console;

Console.WriteLine();

// 🔬 TESTE DI CONTROLADO
var __fake = "services.AddScoped<FakeZombieService>();";

IParserResult? parsingResult = null;

// =====================================================
// PIPELINE PRINCIPAL
// =====================================================

if (!RunConfiguration(out var config)) return;

if (!RunParsing(config, out var context, out parsingResult)) return;

RunTopLevelRecovery(context);

if (!RunAnalysis(context, out var report)) return;

RunArchitecturalHygiene(report);

PrintStructuralBreakdown(report);

if (!RunExport(config, context, report, parsingResult)) return;

RunVisualization(report);

ApplyCiExitCode(report);



/* =====================================================
   CONFIGURAÇÃO
   ===================================================== */

static bool RunConfiguration(out RefactorScopeConfig config)
{
    config = null!;

    try
    {
        bool enableSelfSelector = true;

        var configPath =
            RefactorScope.CLI.SelfAnalysisSelector.ResolveConfigPath(
                "refactorscope.json",
                "refactorscope_v1_1_self.json",
                enableSelfSelector);

        config = ConfigLoader.Load(configPath);

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
    out AnalysisContext context,
    out IParserResult? parsingResult)
{
    context = null!;
    parsingResult = null;

    try
    {
        if (!Enum.TryParse<ParserStrategy>(
                config.Parser,
                true,
                out var strategy))
        {
            Console.WriteLine(
                $"[WARN] Estratégia de parser '{config.Parser}' inválida. Usando Selective.");

            strategy = ParserStrategy.Selective;
        }

        bool enableParserSelector = true;

        IParserCodigo parser =
            ParserSelector.ResolveParser(
                config.Parser,
                enableParserSelector);

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
            result.Stats?.ExecutionTime ?? TimeSpan.Zero
        );

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
    var duplicates =
        tipos
        .GroupBy(t => $"{t.Namespace}.{t.Name}")
        .Where(g => g.Count() > 1);

    foreach (var dup in duplicates)
    {
        TerminalRenderer.Warn(
            $"Tipo duplicado detectado: {dup.Key} (ocorrências: {dup.Count()})");
    }
}



/* =====================================================
   RECOVERY - A SER IMPLEMENTADO FUTURAMENTE
   ===================================================== */

static void RunTopLevelRecovery(AnalysisContext context)
{
    try
    {
        //TopLevelReferenceRecovery.Run(context);
        /*
//            FUTURE IMPLEMENTATION

//            Strategy:

//            1. Detect unreferenced types
//            2. Scan bootstrap files (Program.cs)
//            3. Sanitize comments and strings
//            4. Detect textual references
//            5. Rebuild structural snapshot with recovered references
//            */
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

        var datasetBuilders = BuildDatasetBuilders();
        var exporters = BuildExporters(datasetBuilders);

        var strategy = DumpStrategyResolver.Resolve(config);

        strategy.Execute(context, report, exporters);

        GenerateMarkdownReport(report, config.OutputPath);
        GenerateStructuralDashboard(report, config.OutputPath);

        if (parsingResult != null)
        {
            var parsingDashboard = new ParsingDashboardExporter();

            parsingDashboard.Export(parsingResult, config.OutputPath);
        }

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



static List<IExporter> BuildExporters(
    List<IAnalyticalDatasetBuilder> datasetBuilders)
{
    return new List<IExporter>
    {
        new DumpAnaliseExporter(),
        new DumpIaExporter(),
        new DatasetExporter(datasetBuilders),
        new ProjectStructureExporter(),
        new FitnessGateCsvExporter(),
        new HtmlDashboardExporter()
    };
}



/* =====================================================
   RELATÓRIOS
   ===================================================== */

static void GenerateMarkdownReport(
    ConsolidatedReport report,
    string outputDirectory)
{
    try
    {
        Directory.CreateDirectory(outputDirectory);

        var outputPath =
            Path.Combine(outputDirectory, "Relatorio_Arquitetural.md");

        var exporter = new MarkdownReportExporter();

        exporter.Export(report, outputPath);

        TerminalRenderer.Success("Relatório Markdown gerado");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO AO GERAR MD]: {ex.Message}");
        CrashLogger.Log(ex, "MARKDOWN_EXPORT");
    }
}



static void GenerateStructuralDashboard(
    ConsolidatedReport report,
    string outputDirectory)
{
    try
    {
        Directory.CreateDirectory(outputDirectory);

        var exporter = new StructuralInventoryExporter();

        exporter.Export(report, outputDirectory);

        TerminalRenderer.Success("Structural Dashboard gerado");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO AO GERAR DASHBOARD]: {ex.Message}");
        CrashLogger.Log(ex, "DASHBOARD_EXPORT");
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

    var rows =
        new List<(string Module, double Score, string Unresolved, double Coupling, double Isolation)>();

    foreach (var module in modules)
    {
        var total = module.Count();
        if (total == 0) continue;

        var zombieCount =
            effectiveUnresolved.Count(z => module.Any(m => m.TypeName == z));

        var isolatedCount =
            isolated?.IsolatedCoreTypes
            .Count(i => module.Any(m => m.TypeName == i)) ?? 0;

        var fanOut =
            coupling?.ModuleFanOut
            .GetValueOrDefault(module.Key) ?? 0;

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
            isolationRate
        ));
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
    var gates =
        report.Results
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
    var gates =
        report.Results
        .OfType<FitnessGateResult>()
        .FirstOrDefault();

    if (gates == null)
        return;

    Environment.ExitCode = gates.HasFailure ? 1 : 0;
}



/* =====================================================
   HELPERS
   ===================================================== */

static bool IsEnabled(AnalysisContext context, string analyzerName)
{
    return context.Config.Analyzers.TryGetValue(analyzerName, out var enabled)
           && enabled;
}
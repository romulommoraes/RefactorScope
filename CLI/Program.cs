using RefactorScope.Analyzers;
using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Datasets;
using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Reporting;
using RefactorScope.Core.Results;
using RefactorScope.Execution.Dump;
using RefactorScope.Exporters;
using RefactorScope.Infrastructure;
using Spectre.Console;


Console.WriteLine();

// 🔬 TESTE DI CONTROLADO (linha textual apenas)
var __fake = "services.AddScoped<FakeZombieService>();";


// =====================================================
// 🔹 FLUXO PRINCIPAL
// =====================================================

if (!TryRunConfiguration(out var config)) return;
if (!TryRunParsing(config, out var context)) return;
if (!TryRunAnalysis(context, out var report)) return;

// 🔥 DEBUG — Structural Candidate Breakdown
var breakdown = report.GetStructuralCandidateBreakdown();

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold cyan]Structural Candidates Analysis Breakdown[/]");
AnsiConsole.WriteLine($"Structural Candidates : {breakdown.StructuralCandidates}");
AnsiConsole.WriteLine($"Probabilistic Confirmed (≥ {report.UnresolvedProbabilityThreshold:0.00}) : {breakdown.ProbabilisticConfirmed}");
AnsiConsole.WriteLine($"Absolved : {breakdown.PatternSimilarity}");
AnsiConsole.WriteLine($"Reduction : {breakdown.ReductionRate:P1}");
AnsiConsole.WriteLine();

if (!TryRunExport(config, context, report)) return;

RunVisualization(report);
ApplyCiExitCode(report);



// =====================================================
// 🔹 BLOCO 1 — CONFIGURAÇÃO
// =====================================================
static bool TryRunConfiguration(out RefactorScopeConfig config)
{
    config = null!;

    try
    {
        bool enableSelfSelector = true;

        var configPath = RefactorScope.CLI.SelfAnalysisSelector
            .ResolveConfigPath(
                "refactorscope.json",
                "refactorscope_v1_1_self.json",
                enableSelfSelector);

        config = ConfigLoader.Load(configPath);

        ConfigValidator.Validate(config);

        config.FitnessGates = FitnessGateConfigResolver.Resolve(
            config.FitnessGates,
            msg => TerminalRenderer.Warn(msg)
        );

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



// =====================================================
// 🔹 BLOCO 2 — PARSING
// =====================================================
static bool TryRunParsing(RefactorScopeConfig config, out AnalysisContext context)
{
    context = null!;

    try
    {
        bool enableParserSelector = true;

        IParserCodigo parser = RefactorScope.CLI.ParserSelector
            .ResolveParser(enableParserSelector);

        var model = TerminalRenderer.WithSpinner(
            "Parsing código...",
            () => parser.Parse(config.RootPath, config.Include, config.Exclude));

        TerminalRenderer.Success("Parsing concluído");

        // 🔎 SANITY CHECK — apenas detecta duplicatas
        WarnDuplicateTipos(model.Tipos);

        context = new AnalysisContext(config, model);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NO PARSING]: {ex.Message}");
        CrashLogger.Log(ex, "PARSING");
        return false;
    }
}

static void WarnDuplicateTipos(IEnumerable<TipoInfo> tipos)
{
    var duplicates = tipos
        .GroupBy(t => $"{t.Namespace}.{t.Name}")
        .Where(g => g.Count() > 1)
        .ToList();

    foreach (var dup in duplicates)
    {
        TerminalRenderer.Warn(
            $"Tipo duplicado detectado: {dup.Key} (ocorrências: {dup.Count()})"
        );
    }
}



// =====================================================
// 🔹 BLOCO 3 — ANÁLISE + CONSOLIDAÇÃO
// =====================================================
static bool TryRunAnalysis(AnalysisContext context, out ConsolidatedReport report)
{
    report = null!;

    try
    {
        var analyzers = new List<IAnalyzer>();
        analyzers.Add(new ProjectStructureAnalyzer());

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
            analyzers.Add(new CouplingAnalyzer());

        if (IsEnabled(context, "coupling"))
            analyzers.Add(new ImplicitCouplingAnalyzer());

        if (IsEnabled(context, "solid"))
            analyzers.Add(new SolidAnalyzer());

        analyzers.Add(new FitnessGateAnalyzer(context.Config.FitnessGates));

        var orchestrator = new AnalysisOrchestrator(analyzers);

        var rawReport = TerminalRenderer.WithSpinner(
            "Executando analisadores...",
            () => orchestrator.Execute(context));

        TerminalRenderer.Success("Análise concluída");

        var consolidator = new ReportConsolidator();
        report = consolidator.Consolidate(rawReport);

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA ANÁLISE]: {ex.Message}");
        CrashLogger.Log(ex, "ANALYSIS");
        return false;
    }
}



// =====================================================
// 🔹 BLOCO 4 — EXPORTAÇÃO
// =====================================================
static bool TryRunExport(
    RefactorScopeConfig config,
    AnalysisContext context,
    ConsolidatedReport report)
{
    try
    {
        TerminalRenderer.Step("Gerando dumps e datasets...");

        var datasetBuilders = new List<IAnalyticalDatasetBuilder>
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

        var exporters = new List<IExporter>
        {
            new DumpAnaliseExporter(),
            new DumpIaExporter(),
            new DatasetExporter(datasetBuilders),
            new ProjectStructureExporter(),
            new FitnessGateCsvExporter(),
            new HtmlDashboardExporter()
        };

        var strategy = DumpStrategyResolver.Resolve(config);

        strategy.Execute(context, report, exporters);

        GenerateMarkdownReport(report, config.OutputPath);
        GenerateStructuralDashboard(report, config.OutputPath);

        TerminalRenderer.Success("Dumps e relatório gerados com sucesso");

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA EXPORTAÇÃO]: {ex.Message}");
        CrashLogger.Log(ex, "EXPORT");
        return false;
    }
}


// =====================================================
// 🔹 BLOCO 5 — RELATÓRIOS
// =====================================================
static void GenerateMarkdownReport(
    ConsolidatedReport report,
    string outputDirectory)
{
    try
    {
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(
            outputDirectory,
            "Relatorio_Arquitetural.md");

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



// =====================================================
// 🔹 BLOCO 6 — VISUALIZAÇÃO TERMINAL
// =====================================================
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

    foreach (var module in modules)
    {
        var total = module.Count();
        if (total == 0) continue;

        var zombieCount = effectiveUnresolved
            .Count(z => module.Any(m => m.TypeName == z));

        var isolatedCount = isolated?.IsolatedCoreTypes
            .Count(i => module.Any(m => m.TypeName == i)) ?? 0;

        var fanOut = coupling?.ModuleFanOut
            .GetValueOrDefault(module.Key) ?? 0;

        var zombieRate = zombieCount / (double)total;
        var isolationRate = isolatedCount / (double)total;
        var couplingRate = fanOut / (double)total;
        var coreDensity = module.Count(t => t.Layer == "Core") / (double)total;

        var zombieDisplay = $"{zombieCount} ({zombieRate:0%})";

        var coreTypes = module.Count(t => t.Layer == "Core");

        var score = ArchitecturalScoreCalculator.Calculate(
            module.Key,
            total,
            zombieCount,
            isolatedCount,
            fanOut,
            coreTypes);

        TerminalRenderer.ModuleHealth(
            module.Key,
            score,
            zombieDisplay,
            couplingRate,
            isolationRate
        );
    }

    RenderFitnessGates(report);
}



// =====================================================
// 🔹 BLOCO 7 — FITNESS GATES
// =====================================================
static void RenderFitnessGates(ConsolidatedReport report)
{
    var gates = report.Results
        .OfType<FitnessGateResult>()
        .FirstOrDefault();

    if (gates == null) return;

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



// =====================================================
// 🔹 BLOCO 8 — CI EXIT CODE
// =====================================================
static void ApplyCiExitCode(ConsolidatedReport report)
{
    var gates = report.Results
        .OfType<FitnessGateResult>()
        .FirstOrDefault();

    if (gates == null)
        return;

    if (gates.HasFailure)
    {
        Console.WriteLine();
        Console.WriteLine("[CI] Fitness Gates falharam.");
        Environment.ExitCode = 1;
    }
    else
    {
        Environment.ExitCode = 0;
    }
}



// =====================================================
// 🔹 HELPERS
// =====================================================
static bool IsEnabled(AnalysisContext context, string analyzerName)
{
    return context.Config.Analyzers.TryGetValue(analyzerName, out var enabled)
           && enabled;
}



// =====================================================
// 🔹 CRASH LOGGER
// =====================================================
/// <summary>
/// Sistema simples de logging de falhas para RefactorScope.
///
/// Objetivo:
/// Preservar Stack Trace completa sem poluir a interface do terminal.
///
/// Estratégia:
/// - Console mostra apenas mensagem amigável
/// - Arquivo físico armazena detalhes completos
///
/// Arquivo gerado:
///     refactorscope-crash.log
///
/// Conteúdo:
///     Timestamp
///     Fase da execução
///     Stack Trace completa
///
/// Isso facilita diagnosticar falhas ocorridas
/// em repositórios externos ou ambientes CI.
/// </summary>

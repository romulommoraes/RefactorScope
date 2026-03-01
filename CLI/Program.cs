using RefactorScope.Analyzers;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Datasets;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;
using RefactorScope.Exporters;
using RefactorScope.Infrastructure;
using RefactorScope.Parsers.CSharpRegex;

Console.WriteLine();

// O fluxo de execução central (Linear e Declarativo)
if (!TryRunConfiguration(out var config)) return;
if (!TryRunParsing(config, out var context)) return;
if (!TryRunAnalysis(context, out var report)) return;
if (!TryRunExport(config, context, report)) return;

RunVisualization(report);

// =====================================================
// 🔹 BLOCO 1 — CONFIGURAÇÃO
// =====================================================
static bool TryRunConfiguration(out RefactorScopeConfig config)
{
    config = null!;
    try
    {
        config = ConfigLoader.Load();
        ConfigValidator.Validate(config);

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
        IParserCodigo parser = new CSharpRegexParser();
        var model = TerminalRenderer.WithSpinner("Parsing código...",
            () => parser.Parse(config.RootPath, config.Include, config.Exclude));

        TerminalRenderer.Success("Parsing concluído");
        context = new AnalysisContext(config, model);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NO PARSING]: {ex.Message}");
        return false;
    }
}

// =====================================================
// 🔹 BLOCOS 3 e 4 — ANALISADORES E ORQUESTRAÇÃO
// =====================================================
static bool TryRunAnalysis(AnalysisContext context, out ConsolidatedReport report)
{
    report = null!;
    try
    {
        var analyzers = new List<IAnalyzer>
        {
            new ZombieAnalyzer(),
            new ArchitecturalClassificationAnalyzer(),
            new EntryPointHeuristicAnalyzer(),
            new CoreIsolationAnalyzer(),
            new CouplingAnalyzer()
        };

        var orchestrator = new AnalysisOrchestrator(analyzers);
        report = TerminalRenderer.WithSpinner("Executando analisadores...",
            () => orchestrator.Execute(context));

        TerminalRenderer.Success("Análise concluída");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA ANÁLISE]: {ex.Message}");
        return false;
    }
}

// =====================================================
// 🔹 BLOCOS 5, 6 e 7 — DATASETS, EXPORTADORES E DUMPS
// =====================================================
static bool TryRunExport(RefactorScopeConfig config, AnalysisContext context, ConsolidatedReport report)
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
            new ProjectStructureExporter()
        };

        var strategy = DumpStrategyResolver.Resolve(config);
        strategy.Execute(context, report, exporters);

        TerminalRenderer.Success("Dumps gerados com sucesso");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO NA EXPORTAÇÃO]: {ex.Message}");
        return false;
    }
}

// =====================================================
// 🔹 BLOCOS 8 e 9 — VISUALIZAÇÃO NO TERMINAL
// =====================================================
static void RunVisualization(ConsolidatedReport report)
{
    TerminalRenderer.Section("Architectural Health");

    var modules = report.Results
        .OfType<ArchitecturalClassificationResult>()
        .FirstOrDefault()?.Items
        .GroupBy(i => i.Folder);

    var zombies = report.Results.OfType<ZombieResult>().FirstOrDefault();
    var isolated = report.Results.OfType<CoreIsolationResult>().FirstOrDefault();
    var coupling = report.Results.OfType<CouplingResult>().FirstOrDefault();

    if (modules == null) return;

    foreach (var module in modules)
    {
        var total = module.Count();
        if (total == 0) continue;

        var zombieCount = zombies?.ZombieTypes.Count(z => module.Any(m => m.TypeName == z)) ?? 0;
        var isolatedCount = isolated?.IsolatedCoreTypes.Count(i => module.Any(m => m.TypeName == i)) ?? 0;
        var fanOut = coupling?.ModuleFanOut.GetValueOrDefault(module.Key) ?? 0;

        var zombieRate = zombieCount / (double)total;
        var isolationRate = isolatedCount / (double)total;
        var couplingRate = fanOut / (double)total;
        var coreDensity = module.Count(t => t.Layer == "Core") / (double)total;

        // 🟢 Zombie agora é absoluto + relativo
        var zombieDisplay = $"{zombieCount} ({zombieRate:0%})";

        var score =
            100
            - (couplingRate * 30)
            - (zombieRate * 25)
            - (isolationRate * 20)
            + (coreDensity * 15);

        score = Math.Max(0, Math.Min(100, score));

        TerminalRenderer.ModuleHealth(
            module.Key,
            score,
            zombieDisplay,
            couplingRate,
            isolationRate
        );
    }
}
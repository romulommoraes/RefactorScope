using RefactorScope.Analyzers;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Exporters;
using RefactorScope.Infrastructure;
using RefactorScope.Parsers.CSharpRegex;

Console.WriteLine();

try
{
    var config = ConfigLoader.Load();
    ConfigValidator.Validate(config);

    if (!Directory.Exists(config.RootPath))
    {
        Console.WriteLine($"Invalid rootPath: {config.RootPath}");
        return;
    }

    // Header visual
    TerminalRenderer.ShowHeader(config.RootPath);

    // Parse
    IParserCodigo parser = new CSharpRegexParser();

    var model = TerminalRenderer.WithSpinner(
        "Parsing código...",
        () => parser.Parse(config.RootPath)
    );

    TerminalRenderer.Success("Parsing concluído");



    var context = new AnalysisContext(config, model);

    var analyzers = new List<IAnalyzer>
    {
        new ZombieAnalyzer(),
        new ArchitecturalClassificationAnalyzer(),
        new EntryPointHeuristicAnalyzer(),
        new CoreIsolationAnalyzer()
    };

    var orchestrator = new AnalysisOrchestrator(analyzers);

    // Execução
    var report = TerminalRenderer.WithSpinner(
        "Executando analisadores...",
        () => orchestrator.Execute(context)
    );

    TerminalRenderer.Success("Análise concluída");

    TerminalRenderer.Step("Gerando dumps...");

    var exporters = new List<IExporter>
{
    new DumpAnaliseExporter(),
    new DumpIaExporter()
};

    var strategy = DumpStrategyResolver.Resolve(config);

    strategy.Execute(
        context,
        report,
        exporters
    );

    TerminalRenderer.Success("Dumps gerados com sucesso");

    // Seção de resultados
    TerminalRenderer.Section("Resultados");

    int zombieCount = 0;
    int isolatedCount = 0;
    int entryCount = 0;

    foreach (var result in report.Results)
    {
        if (result is RefactorScope.Core.Results.ZombieResult zombie)
        {
            zombieCount = zombie.ZombieTypes.Count;

            Console.WriteLine("\nZombie Types Found:");
            foreach (var z in zombie.ZombieTypes)
                Console.WriteLine($" - {z}");
        }

        if (result is RefactorScope.Core.Results.ArchitecturalClassificationResult arch)
        {
            Console.WriteLine("\nArchitectural Classification:");

            foreach (var item in arch.Items.OrderBy(x => x.UsageCount))
            {
                Console.WriteLine(
                    $" - {item.TypeName} " +
                    $"| Folder: {item.Folder} " +
                    $"| Namespace: {item.Namespace} " +
                    $"| Layer: {item.Layer} " +
                    $"| Align: {item.NamespaceAlignment} " +
                    $"| Structural: {item.StructuralStatus} " +
                    $"| Status: {item.Status} " +
                    $"| Remove: {item.RemovalCandidate}"
                );
            }
        }

        if (result is RefactorScope.Core.Results.EntryPointHeuristicResult ep)
        {
            entryCount = ep.EntryPoints.Count;

            Console.WriteLine("\nEntry Points:");
            foreach (var e in ep.EntryPoints)
                Console.WriteLine($" - {e}");
        }

        if (result is RefactorScope.Core.Results.CoreIsolationResult iso)
        {
            isolatedCount = iso.IsolatedCoreTypes.Count;

            Console.WriteLine("\nIsolated Core Types:");

            if (!iso.IsolatedCoreTypes.Any())
            {
                Console.WriteLine(" - None");
            }
            else
            {
                foreach (var t in iso.IsolatedCoreTypes)
                    Console.WriteLine($" - {t}");
            }
        }
    }

    // Resumo visual
    TerminalRenderer.Section("Resumo");
    TerminalRenderer.TableSummary(
        zombieCount,
        isolatedCount,
        entryCount
    );
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
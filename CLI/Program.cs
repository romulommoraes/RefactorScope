using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Parsers.CSharpRegex;
using RefactorScope.Analyzers;
using RefactorScope.Infrastructure;
using RefactorScope.Core.Abstractions;

Console.WriteLine();

try
{
    var config = ConfigLoader.Load();

    if (!Directory.Exists(config.RootPath))
    {
        Console.WriteLine($"Invalid rootPath: {config.RootPath}");
        return;
    }

    // Header visual
    TerminalRenderer.ShowHeader(config.RootPath);

    // Parse
    TerminalRenderer.Step("Parsing código...");
    IParserCodigo parser = new CSharpRegexParser();
    var model = parser.Parse(config.RootPath);
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
    TerminalRenderer.Step("Executando analisadores...");
    var report = orchestrator.Execute(context);
    TerminalRenderer.Success("Análise concluída");

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
                    $" - {item.TypeName} | Layer: {item.Layer} | Status: {item.Status} | Remove: {item.RemovalCandidate}"
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
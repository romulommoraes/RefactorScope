using RefactorScope.Core.Parsing;
using RefactorScope.Core.Parsing.Enum;

namespace RefactorScope.CLI;

public static class StartupExecutionPlanSelector
{
    public static StartupExecutionPlan Resolve(
        string defaultConfigPath,
        string selfConfigPath,
        bool enableInteractiveSelector,
        bool enableParserSelector)
    {
        if (!enableInteractiveSelector)
        {
            return new StartupExecutionPlan
            {
                ConfigPath = defaultConfigPath,
                Scope = AnalysisScope.Normal,
                Mode = ExecutionMode.SingleParser,
                SelectedParser = ParserStrategy.Selective
            };
        }

        var scope = ResolveScope();
        var mode = ResolveExecutionMode(scope);

        var configPath = scope == AnalysisScope.Self
            ? selfConfigPath
            : defaultConfigPath;

        ParserStrategy? selectedParser = null;

        if (mode == ExecutionMode.SingleParser)
            selectedParser = ResolveConcreteParser(enableParserSelector);

        return new StartupExecutionPlan
        {
            ConfigPath = configPath,
            Scope = scope,
            Mode = mode,
            SelectedParser = selectedParser
        };
    }

    private static AnalysisScope ResolveScope()
    {
        Console.WriteLine();
        Console.WriteLine("🧬 RefactorScope Analysis Scope");
        Console.WriteLine("---------------------------------");
        Console.WriteLine("1) Normal Analysis");
        Console.WriteLine("2) Self Analysis (RefactorScope analyzing itself)");
        Console.WriteLine();
        Console.Write("Select scope (1 or 2) [default=1]: ");

        var input = Console.ReadLine()?.Trim();

        var scope = input == "2"
            ? AnalysisScope.Self
            : AnalysisScope.Normal;

        Console.WriteLine(
            scope == AnalysisScope.Self
                ? "🔍 Self Analysis Scope Enabled"
                : "📦 Normal Analysis Scope Enabled");

        return scope;
    }

    private static ExecutionMode ResolveExecutionMode(AnalysisScope scope)
    {
        Console.WriteLine();
        Console.WriteLine("⚙️ Execution Mode");
        Console.WriteLine("---------------------------------");
        Console.WriteLine("1) Single Parser");
        Console.WriteLine("2) Comparative (current target only)");

        if (scope == AnalysisScope.Normal)
            Console.WriteLine("3) Batch Arena");

        Console.WriteLine();

        var prompt = scope == AnalysisScope.Normal
            ? "Select mode (1, 2 or 3) [default=1]: "
            : "Select mode (1 or 2) [default=1]: ";

        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("🧩 Single Parser Mode Enabled");
                return ExecutionMode.SingleParser;
            }

            if (scope == AnalysisScope.Self)
            {
                var selfMode = input switch
                {
                    "2" => ExecutionMode.Comparative,
                    "1" => ExecutionMode.SingleParser,
                    _ => (ExecutionMode?)null
                };

                if (selfMode.HasValue)
                {
                    Console.WriteLine(selfMode.Value switch
                    {
                        ExecutionMode.Comparative => "⚖️ Comparative Mode Enabled",
                        _ => "🧩 Single Parser Mode Enabled"
                    });

                    return selfMode.Value;
                }

                Console.WriteLine("[WARN] Em Self Analysis, apenas os modos 1) Single Parser e 2) Comparative estão disponíveis.");
                continue;
            }

            var normalMode = input switch
            {
                "2" => ExecutionMode.Comparative,
                "3" => ExecutionMode.BatchArena,
                "1" => ExecutionMode.SingleParser,
                _ => (ExecutionMode?)null
            };

            if (normalMode.HasValue)
            {
                Console.WriteLine(normalMode.Value switch
                {
                    ExecutionMode.Comparative => "⚖️ Comparative Mode Enabled",
                    ExecutionMode.BatchArena => "🗂️ Batch Arena Mode Enabled",
                    _ => "🧩 Single Parser Mode Enabled"
                });

                return normalMode.Value;
            }

            Console.WriteLine("[WARN] Opção inválida. Escolha um modo disponível.");
        }
    }

    private static ParserStrategy ResolveConcreteParser(bool enableParserSelector)
    {
        if (!enableParserSelector)
            return ParserStrategy.Selective;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("🧩 Parser Selection");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("1) Fast Scan (Regex)");
            Console.WriteLine("2) Accurate Scan (Selective)");
            Console.WriteLine("3) Adaptive (Experimental)");
            Console.WriteLine("4) Incremental (Experimental)");
            Console.WriteLine();
            Console.Write("Select parser [1-4] (default 2): ");

            var input = Console.ReadLine()?.Trim();

            var parser = input switch
            {
                "1" => ParserStrategy.RegexFast,
                "3" => ParserStrategy.AdaptiveExperimental,
                "4" => ParserStrategy.IncrementalExperimental,
                "" or null => ParserStrategy.Selective,
                "2" => ParserStrategy.Selective,
                _ => (ParserStrategy?)null
            };

            if (parser.HasValue)
                return parser.Value;

            Console.WriteLine("[WARN] Opção inválida. Escolha um parser concreto.");
        }
    }
}
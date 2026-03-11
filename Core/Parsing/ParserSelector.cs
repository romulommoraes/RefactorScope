using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Parsers.Common;
using RefactorScope.Parsers.CsharpParsers;
using RefactorScope.Parsers.CsharpParsers.Hybrid;

namespace RefactorScope.Core.Parsing
{
    /// <summary>
    /// Responsável por resolver qual estratégia de parsing será utilizada.
    ///
    /// Importante:
    /// - Comparative é uma estratégia de execução, não um parser estrutural único.
    /// - Por isso, ResolveStrategy() e ResolveParser() foram separados.
    /// - Também suporta resolução em modo silencioso, útil para Arena / Batch.
    /// </summary>
    public static class ParserSelector
    {
        public static ParserStrategy ResolveStrategy(
            string? configParserName,
            bool interactive)
        {
            ParserStrategy strategy;

            if (interactive)
            {
                strategy = PromptStrategy();
            }
            else if (!System.Enum.TryParse(
                        configParserName,
                        true,
                        out strategy))
            {
                Console.WriteLine(
                    $"[WARN] Estratégia de parser '{configParserName}' inválida. Usando Selective.");

                strategy = ParserStrategy.Selective;
            }

            return strategy;
        }

        /// <summary>
        /// Resolve um parser concreto a partir de uma estratégia.
        ///
        /// silent = true:
        /// desabilita logs internos de parsers híbridos, útil para execução
        /// em lote no Arena sem poluir spinner ou saída consolidada.
        /// </summary>
        public static IParserCodigo ResolveParser(
            ParserStrategy strategy,
            bool silent = false)
        {
            return strategy switch
            {
                ParserStrategy.RegexFast => BuildRegex(),
                ParserStrategy.Selective => BuildSelective(silent),
                ParserStrategy.AdaptiveExperimental => BuildAdaptive(silent),
                ParserStrategy.IncrementalExperimental => BuildIncremental(silent),

                ParserStrategy.Comparative =>
                    throw new InvalidOperationException(
                        "Comparative não deve ser resolvido como IParserCodigo. Use o fluxo do Arena."),

                _ => BuildSelective(silent)
            };
        }

        /// <summary>
        /// Resolve um parser a partir do nome configurado.
        ///
        /// silent = true:
        /// desabilita logs internos de parsers híbridos.
        /// </summary>
        public static IParserCodigo ResolveParser(
            string? configParserName,
            bool interactive,
            bool silent = false)
        {
            var strategy = ResolveStrategy(configParserName, interactive);
            return ResolveParser(strategy, silent);
        }

        private static ParserStrategy PromptStrategy()
        {
            Console.WriteLine();
            Console.WriteLine("🔎 Parser Selection");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("1) Fast Scan (Regex)");
            Console.WriteLine("2) Accurate Scan (Selective)");
            Console.WriteLine("3) Adaptive (Experimental)");
            Console.WriteLine("4) Incremental (Experimental)");
            Console.WriteLine("5) Comparative (Arena / Batch)");
            Console.WriteLine();

            Console.Write("Select parser [1-5] (default 2): ");

            var input = Console.ReadLine()?.Trim();

            return input switch
            {
                "1" => ParserStrategy.RegexFast,
                "3" => ParserStrategy.AdaptiveExperimental,
                "4" => ParserStrategy.IncrementalExperimental,
                "5" => ParserStrategy.Comparative,
                _ => ParserStrategy.Selective
            };
        }

        private static IParserCodigo BuildRegex()
        {
            var provider = BuildSourceProvider();
            return new CSharpRegexParser(provider);
        }

        private static IParserCodigo BuildTextual(Action<string>? logger = null)
        {
            var provider = BuildSourceProvider();

            return new CSharpTextualParser(
                provider,
                logger);
        }

        private static IParserCodigo BuildSelective(bool silent)
        {
            return new HybridSelectiveParser(
                BuildRegex(),
                BuildTextual(),
                CreateLogger("Selective", silent)
            );
        }

        private static IParserCodigo BuildAdaptive(bool silent)
        {
            return new HybridAdaptiveParser(
                BuildRegex(),
                BuildTextual(),
                CreateLogger("Adaptive", silent)
            );
        }

        private static IParserCodigo BuildIncremental(bool silent)
        {
            return new HybridIncrementalParser(
                BuildRegex(),
                BuildTextual(),
                CreateLogger("Incremental", silent)
            );
        }

        private static Action<string>? CreateLogger(
            string channel,
            bool silent)
        {
            if (silent)
                return null;

            return msg => Console.WriteLine($"[{channel}] {msg}");
        }

        private static SanitizedSourceProvider BuildSourceProvider()
        {
            IPreParser preParser = new CSharpPreParser();
            return new SanitizedSourceProvider(preParser);
        }
    }
}
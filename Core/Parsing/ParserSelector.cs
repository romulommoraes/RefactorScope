using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing.Enum;
using RefactorScope.Parsers.Common;
using RefactorScope.Parsers.CsharpParsers;
using RefactorScope.Parsers.CsharpParsers.Hybrid;

namespace RefactorScope.Core.Parsing
{

    /// <summary>
    /// Responsável por resolver qual parser será utilizado.
    /// 
    /// Pode operar em dois modos:
    /// - Configuração direta
    /// - Seleção interativa via CLI
    /// </summary>
    public static class ParserSelector
    {
        public static IParserCodigo ResolveParser(
            string? configParserName,
            bool interactive)
        {
            ParserStrategy strategy;

            if (interactive)
            {
                strategy = PromptStrategy();
            }
            else if (!System.Enum.TryParse<ParserStrategy>(
                        configParserName,
                        true,
                        out strategy))
            {
                Console.WriteLine(
                    $"[WARN] Estratégia de parser '{configParserName}' inválida. Usando Selective.");

                strategy = ParserStrategy.Selective;
            }

            return Build(strategy);
        }

        private static ParserStrategy PromptStrategy()
        {
            Console.WriteLine();
            Console.WriteLine("🧠 Parser Selection");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("1) Fast Scan (Regex)");
            Console.WriteLine("2) Accurate Scan (Selective)");
            Console.WriteLine("3) Adaptive (Experimental)");
            Console.WriteLine("4) Incremental (Experimental)");
            Console.WriteLine();

            Console.Write("Select parser [1-4] (default 2): ");

            var input = Console.ReadLine()?.Trim();

            return input switch
            {
                "1" => ParserStrategy.RegexFast,
                "3" => ParserStrategy.AdaptiveExperimental,
                "4" => ParserStrategy.IncrementalExperimental,
                _ => ParserStrategy.Selective
            };
        }

        private static IParserCodigo Build(ParserStrategy strategy)
        {
            return strategy switch
            {
                ParserStrategy.RegexFast => BuildRegex(),
                ParserStrategy.Selective => BuildSelective(),
                ParserStrategy.AdaptiveExperimental => BuildAdaptive(),
                ParserStrategy.IncrementalExperimental => BuildIncremental(),
                _ => BuildSelective()
            };
        }

        private static IParserCodigo BuildRegex()
        {
            var provider = BuildSourceProvider();
            return new CSharpRegexParser(provider);
        }

        private static IParserCodigo BuildTextual()
        {
            var provider = BuildSourceProvider();

            return new CSharpTextualParser(
                provider,
                msg => Console.WriteLine($"[WARN][Textual] {msg}")
            );
        }

        private static IParserCodigo BuildSelective()
        {
            return new HybridSelectiveParser(
                BuildRegex(),
                BuildTextual(),
                msg => Console.WriteLine($"[Selective] {msg}")
            );
        }

        private static IParserCodigo BuildAdaptive()
        {
            return new HybridAdaptiveParser(
                BuildRegex(),
                BuildTextual(),
                msg => Console.WriteLine($"[Adaptive] {msg}")
            );
        }

        private static IParserCodigo BuildIncremental()
        {
            return new HybridIncrementalParser(
                BuildRegex(),
                BuildTextual(),
                msg => Console.WriteLine($"[Incremental] {msg}")
            );
        }

        private static SanitizedSourceProvider BuildSourceProvider()
        {
            IPreParser preParser = new CSharpPreParser();
            return new SanitizedSourceProvider(preParser);
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Parsers.CsharpParsers;


namespace RefactorScope.CLI
{
    public static class ParserSelector
    {
        public static IParserCodigo ResolveParser(bool enableInteractiveSelector)
        {
            if (!enableInteractiveSelector)
                return new CSharpRegexParser();

            Console.WriteLine();
            Console.WriteLine("🧠 Parser Selection");
            Console.WriteLine("---------------------------");
            Console.WriteLine("1) Regex Parser (estável)");
            Console.WriteLine("2) Textual Parser (experimental)");
            Console.WriteLine();

            Console.Write("Select parser (1 or 2) [default=1]: ");

            var input = Console.ReadLine();

            if (input?.Trim() == "2")
            {
                Console.WriteLine("🧪 Textual Parser Enabled");
                return new CSharpTextualParser();
            }

            Console.WriteLine("📦 Regex Parser Enabled");
            return new CSharpRegexParser();
        }
    }
}
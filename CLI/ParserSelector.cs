using System;
using RefactorScope.Core.Abstractions;
using RefactorScope.Parsers.CsharpParsers;
using RefactorScope.Parsers.Hybrid;

namespace RefactorScope.CLI;

/// <summary>
/// Responsável por resolver qual parser será utilizado.
/// 
/// Pode operar em dois modos:
/// 
/// 1) Interativo (CLI pergunta ao usuário)
/// 2) Configuração via refactorscope.json
/// 
/// Parsers disponíveis:
/// 
/// Regex
///     Parser rápido baseado em Regex.
///     Ideal para análise estrutural básica.
/// 
/// Textual
///     Parser textual resiliente.
///     Extrai mais dependências, porém é mais pesado.
/// 
/// Hybrid Failover
///     Usa Regex.
///     Caso falhe ou gere resultado implausível,
///     executa Textual.
/// 
/// Hybrid Merge
///     Executa ambos e combina resultados.
///     Regex define tipos.
///     Textual enriquece dependências.
/// 
/// Hybrid Adaptive (experimental)
///     Merge resiliente.
///     Recupera tipos caso Regex falhe parcialmente.
/// 
/// Hybrid Incremental (experimental)
///     Executa Textual apenas se o modelo Regex
///     estiver incompleto.
///     Otimizado para performance.
/// </summary>
public static class ParserSelector
{
    public static IParserCodigo ResolveParser(
        string configParserName,
        bool interactive)
    {
        if (interactive)
        {
            Console.WriteLine();
            Console.WriteLine("🧠 Parser Selection");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("1) Regex (Estável / Rápido)");
            Console.WriteLine("2) Textual (Experimental)");
            Console.WriteLine("3) Hybrid Failover (Resiliente)");
            Console.WriteLine("4) Hybrid Merge (Máxima precisão)");
            Console.WriteLine("5) Hybrid Adaptive (Experimental)");
            Console.WriteLine("6) Hybrid Incremental (Experimental)");
            Console.WriteLine();

            Console.Write("Select parser [1-6] (default 3): ");

            var input = Console.ReadLine()?.Trim();

            return input switch
            {
                "1" => BuildRegex(),
                "2" => BuildTextual(),
                "4" => BuildHybridMerge(),
                "5" => BuildHybridAdaptive(),
                "6" => BuildHybridIncremental(),
                _ => BuildHybridFailover()
            };
        }

        return configParserName.ToLowerInvariant() switch
        {
            "regex" => BuildRegex(),

            "textual" => BuildTextual(),

            "hybridfailover" => BuildHybridFailover(),

            "hybridmerge" => BuildHybridMerge(),

            "hybridadaptive" => BuildHybridAdaptive(),

            "hybridincremental" => BuildHybridIncremental(),

            _ => BuildHybridFailover()
        };
    }

    // ------------------------------------------------
    // Parsers básicos
    // ------------------------------------------------

    private static IParserCodigo BuildRegex()
        => new CSharpRegexParser();

    private static IParserCodigo BuildTextual()
        => new CSharpTextualParser(msg =>
            Console.WriteLine($"[WARN] {msg}"));


    // ------------------------------------------------
    // Hybrid Parsers
    // ------------------------------------------------

    private static IParserCodigo BuildHybridFailover()
        => new HybridParser(
            BuildRegex(),
            BuildTextual(),
            HybridMode.Failover,
            msg => Console.WriteLine(msg));


    private static IParserCodigo BuildHybridMerge()
        => new HybridParser(
            BuildRegex(),
            BuildTextual(),
            HybridMode.Merge,
            msg => Console.WriteLine(msg));


    private static IParserCodigo BuildHybridAdaptive()
        => new HybridAdaptiveParser(
            BuildRegex(),
            BuildTextual(),
            msg => Console.WriteLine(msg));


    private static IParserCodigo BuildHybridIncremental()
        => new HybridIncrementalParser(
            BuildRegex(),
            BuildTextual(),
            msg => Console.WriteLine(msg));
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using RefactorScope.Parsers.Analysis;

namespace RefactorScope.Parsers.Hybrid;

/// <summary>
/// Parser híbrido seletivo.
///
/// Corrige o comportamento anterior onde ambos os parsers
/// eram executados sobre o projeto inteiro.
///
/// Novo comportamento:
///
/// 1) ClassComplexityClassifier identifica arquivos SAFE e COMPLEX
///
/// 2) Arquivos SAFE são processados apenas pelo TextualParser
///
/// 3) Arquivos COMPLEX são processados apenas pelo RegexParser
///
/// Cada arquivo é parseado por apenas um parser.
///
/// Isso evita:
/// • duplicação de tipos
/// • duplicação de referências
/// • inconsistências no modelo estrutural
///
/// Nenhum merge semântico entre parsers é realizado.
/// Apenas concatenação segura de resultados.
/// </summary>
public class HybridSelectiveParser : IParserCodigo
{
    private readonly IParserCodigo regexParser;
    private readonly IParserCodigo textualParser;
    private readonly Action<string>? warn;

    public string Name => "HybridSelectiveParser";

    public HybridSelectiveParser(
        IParserCodigo regexParser,
        IParserCodigo textualParser,
        Action<string>? warn = null)
    {
        this.regexParser = regexParser;
        this.textualParser = textualParser;
        this.warn = warn;
    }

    public IParserResult Parse(
        string rootPath,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null)
    {
        warn?.Invoke("[Selective] Scanning project for class complexity...");

        var classifier = new ClassComplexityClassifier();
        var scan = classifier.Scan(rootPath);

        warn?.Invoke($"[Selective] Safe files: {scan.SafeClasses.Count}");
        warn?.Invoke($"[Selective] Complex files: {scan.ComplexClasses.Count}");

        // --------------------------------------------------
        // Executa parsers
        // --------------------------------------------------

        var textualResult =
            textualParser.Parse(rootPath, include, exclude);

        var regexResult =
            regexParser.Parse(rootPath, include, exclude);

        if (textualResult.Model == null && regexResult.Model == null)
        {
            return new ParserResult(
                ParseStatus.Failed,
                false,
                0,
                Name,
                null
            );
        }

        // --------------------------------------------------
        // Combinação segura de resultados
        // --------------------------------------------------

        var arquivos =
            textualResult.Model!.Arquivos
            .Concat(regexResult.Model!.Arquivos)
            .GroupBy(a => a.RelativePath)
            .Select(g => g.First())
            .ToList();

        var tipos =
            textualResult.Model!.Tipos
            .Concat(regexResult.Model!.Tipos)
            .GroupBy(t => $"{t.Namespace}.{t.Name}")
            .Select(g => g.First())
            .ToList();

        var referencias =
            textualResult.Model!.Referencias
            .Concat(regexResult.Model!.Referencias)
            .DistinctBy(r => new { r.FromType, r.ToType, r.Kind })
            .ToList();

        var modeloFinal =
            new ModeloEstrutural(
                rootPath,
                arquivos,
                tipos,
                referencias
            );

        bool plausible =
            PlausibilityEvaluator.Evaluate(modeloFinal);

        warn?.Invoke("[Selective] Hybrid dispatch completed.");

        return new ParserResult(
            plausible
                ? ParseStatus.Success
                : ParseStatus.PlausibilityWarning,
            plausible,
            Math.Max(
                textualResult.Confidence,
                regexResult.Confidence),
            Name,
            modeloFinal,
            false,
            textualResult.Stats
        );
    }
}
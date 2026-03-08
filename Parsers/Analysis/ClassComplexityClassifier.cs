using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.Analysis;

/// <summary>
/// Analisa arquivos C# e classifica cada arquivo em dois grupos:
///
/// SAFE
/// COMPLEX
///
/// SAFE:
///     Arquivos que podem ser analisados com segurança
///     pelo TextualParser.
///
/// COMPLEX:
///     Arquivos que contêm construções que o TextualParser
///     pode interpretar incorretamente, sendo delegados ao
///     RegexParser.
///
/// Este classificador NÃO realiza parsing estrutural.
/// Apenas aplica heurísticas simples sobre o conteúdo.
///
/// A classificação é baseada no conteúdo textual do arquivo,
/// e não em AST ou análise sintática completa.
/// </summary>
public class ClassComplexityClassifier
{
    /// <summary>
    /// Executa o scan do projeto e classifica os arquivos.
    /// </summary>
    public ClassComplexityScanResult Scan(
        string rootPath,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null)
    {
        var result = new ClassComplexityScanResult();

        var scope = new ScopeRuleSet(include, exclude);

        var files = Directory.GetFiles(
            rootPath,
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (!scope.IsInScope(rootPath, file))
                continue;

            string source;

            try
            {
                source = File.ReadAllText(file);
            }
            catch
            {
                // Arquivo inacessível → tratado como complexo
                result.ComplexClasses.Add(file);
                continue;
            }

            if (IsComplex(source))
                result.ComplexClasses.Add(file);
            else
                result.SafeClasses.Add(file);
        }

        return result;
    }

    /// <summary>
    /// Heurísticas simples para identificar arquivos
    /// que podem causar problemas no TextualParser.
    ///
    /// Estas heurísticas são deliberadamente conservadoras.
    /// Se qualquer uma delas for encontrada, o arquivo é
    /// classificado como COMPLEX.
    /// </summary>
    private static bool IsComplex(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        // -------------------------------------------------
        // Records modernos
        // -------------------------------------------------
        if (source.Contains("record "))
            return true;

        // -------------------------------------------------
        // Init-only properties
        // -------------------------------------------------
        if (source.Contains(" init;"))
            return true;

        // -------------------------------------------------
        // Lambda expressions
        // -------------------------------------------------
        if (source.Contains("=>"))
            return true;

        // -------------------------------------------------
        // With expressions
        // -------------------------------------------------
        if (source.Contains("with {"))
            return true;

        return false;
    }
}
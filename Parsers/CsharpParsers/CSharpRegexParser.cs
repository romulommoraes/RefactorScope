using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using RefactorScope.Parsers.Common;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Parser C# baseado em Regex.
    ///
    /// Papel arquitetural:
    /// - servir como baseline estrutural global do projeto
    /// - detectar tipos com abordagem conservadora
    /// - extrair referências com custo baixo e cobertura ampla
    ///
    /// Observações importantes:
    /// - não realiza parsing sintático completo (AST)
    /// - utiliza fonte sanitizada pelo PreParser
    /// - aplica uma segunda limpeza local para reduzir ruído de comentários e strings
    /// - filtra falsos positivos léxicos através de StructuralTokenGuard
    /// </summary>
    public class CSharpRegexParser : IParserCodigo
    {
        public string Name => "CSharpRegex";

        private readonly SanitizedSourceProvider _sourceProvider;

        public CSharpRegexParser(SanitizedSourceProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
        }

        public CSharpRegexParser()
        {
            _sourceProvider = new SanitizedSourceProvider(
                new CSharpPreParser());
        }

        private static readonly Regex NamespaceRegex =
            new(@"namespace\s+([\w\.]+)",
                RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Regex restritiva para declaração de tipos.
        ///
        /// Exige que após o nome exista algo estrutural:
        /// {  :  <  where
        ///
        /// Isso reduz captura de identificadores soltos.
        /// </summary>
        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:<|\{|:|where)",
                RegexOptions.Compiled | RegexOptions.Multiline);

        public IParserResult Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            var stopwatch = Stopwatch.StartNew();
            long initialMemory = GC.GetTotalMemory(false);

            try
            {
                var arquivos = new List<ArquivoInfo>();
                var tipos = new List<TipoInfo>();
                var referencias = new List<ReferenciaInfo>();

                var scope = new FileSelectionScope(rootPath, include, exclude);

                var csFiles = Directory
                    .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                    .Where(scope.IsInScope)
                    .ToList();

                var originalSourcesByFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var scanSourcesByFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // =====================================================
                // 1) Coleta de tipos
                // =====================================================
                foreach (var file in csFiles)
                {
                    string sanitizedSource;

                    try
                    {
                        sanitizedSource = _sourceProvider.Read(file);
                    }
                    catch
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(rootPath, file);
                    var scanSource = PrepareStructuralScanSource(sanitizedSource);

                    originalSourcesByFile[relativePath] = sanitizedSource;
                    scanSourcesByFile[relativePath] = scanSource;

                    var nsMatch = NamespaceRegex.Match(scanSource);

                    var ns = nsMatch.Success
                        ? nsMatch.Groups[1].Value
                        : "Global";

                    var matches = TypeRegex.Matches(scanSource);

                    foreach (Match match in matches)
                    {
                        var kind = match.Groups[1].Value;
                        var typeName = match.Groups[2].Value;

                        if (!StructuralTokenGuard.IsValidDeclaredTypeName(typeName))
                            continue;

                        tipos.Add(new TipoInfo(
                            typeName,
                            ns,
                            kind,
                            relativePath,
                            new List<ReferenciaInfo>()));
                    }
                }

                // Evita duplicação de tipos detectados mais de uma vez
                tipos = tipos
                    .GroupBy(t => $"{t.Namespace}|{t.Name}|{t.DeclaredInFile}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                var tipoNames = tipos
                    .Select(t => t.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // =====================================================
                // 2) Detecção de referências
                // =====================================================
                foreach (var file in csFiles)
                {
                    var relativePath = Path.GetRelativePath(rootPath, file);

                    if (!scanSourcesByFile.TryGetValue(relativePath, out var scanSource))
                        continue;

                    var tiposDoArquivo = tipos
                        .Where(t => t.DeclaredInFile.Equals(relativePath, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (tiposDoArquivo.Count == 0)
                        continue;

                    foreach (var target in tipoNames)
                    {
                        if (!StructuralTokenGuard.IsValidReferenceTarget(target, tipoNames))
                            continue;

                        var pattern = $@"(?<![\w\.]){Regex.Escape(target)}(?![\w])";

                        if (!Regex.IsMatch(scanSource, pattern))
                            continue;

                        foreach (var tipo in tiposDoArquivo)
                        {
                            if (target.Equals(tipo.Name, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var refInfo = new ReferenciaInfo(
                                tipo.Name,
                                target,
                                TipoReferencia.Mention);

                            if (!referencias.Any(r =>
                                    r.FromType == refInfo.FromType &&
                                    r.ToType == refInfo.ToType &&
                                    r.Kind == refInfo.Kind))
                            {
                                referencias.Add(refInfo);
                            }
                        }
                    }
                }

                // =====================================================
                // 3) Atualização das referências nos tipos
                // =====================================================
                foreach (var tipo in tipos)
                {
                    var refs = referencias
                        .Where(r => r.FromType == tipo.Name)
                        .ToList();

                    typeof(TipoInfo)
                        .GetField("<References>k__BackingField",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic)
                        ?.SetValue(tipo, refs);
                }

                // =====================================================
                // 4) Construção dos ArquivoInfo
                // =====================================================
                foreach (var file in csFiles)
                {
                    var relativePath = Path.GetRelativePath(rootPath, file);

                    if (!originalSourcesByFile.TryGetValue(relativePath, out var source))
                        continue;

                    var nsMatch = NamespaceRegex.Match(scanSourcesByFile[relativePath]);

                    var ns = nsMatch.Success
                        ? nsMatch.Groups[1].Value
                        : "Global";

                    var tiposDoArquivo = tipos
                        .Where(t => t.DeclaredInFile.Equals(relativePath, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    arquivos.Add(new ArquivoInfo(
                        relativePath,
                        ns,
                        source,
                        tiposDoArquivo));
                }

                var model = new ModeloEstrutural(
                    rootPath,
                    arquivos,
                    tipos,
                    referencias);

                stopwatch.Stop();

                long memoryUsed = Math.Max(0, GC.GetTotalMemory(false) - initialMemory);

                bool isPlausible = PlausibilityEvaluator.Evaluate(model);

                var status = isPlausible
                    ? ParseStatus.Success
                    : ParseStatus.PlausibilityWarning;

                var stats = new ParserExecutionStats(
                    stopwatch.Elapsed,
                    memoryUsed,
                    !isPlausible);

                return new ParserResult(
                    status,
                    isPlausible,
                    tipos.Count / (double)Math.Max(arquivos.Count, 1),
                    Name,
                    model,
                    false,
                    stats);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new ParserResult(
                    ParseStatus.Failed,
                    false,
                    0,
                    Name,
                    null,
                    false,
                    new ParserExecutionStats(stopwatch.Elapsed, 0, true),
                    ex);
            }
        }

        /// <summary>
        /// Prepara uma versão do código mais segura para regex estrutural.
        ///
        /// Mantém o número de linhas aproximadamente estável, mas remove ruído
        /// comum que costuma induzir falsos positivos:
        /// - comentários de linha
        /// - comentários de bloco
        /// - strings normais
        /// - strings verbatim
        /// - caracteres literais
        ///
        /// Não é um lexer completo, mas já reduz bastante artefatos como
        /// "misuse" vindos de comentários ou texto livre.
        /// </summary>
        private static string PrepareStructuralScanSource(string source)
        {
            var sb = new StringBuilder(source.Length);

            bool inLineComment = false;
            bool inBlockComment = false;
            bool inString = false;
            bool inVerbatimString = false;
            bool inChar = false;
            bool escape = false;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (inLineComment)
                {
                    if (c == '\n')
                    {
                        inLineComment = false;
                        sb.Append('\n');
                    }
                    else if (c == '\r')
                    {
                        sb.Append('\r');
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        sb.Append("  ");
                        i++;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    continue;
                }

                if (inString)
                {
                    if (!escape && c == '"')
                    {
                        inString = false;
                        sb.Append(' ');
                        continue;
                    }

                    escape = !escape && c == '\\';

                    sb.Append(c == '\n' || c == '\r' ? c : ' ');
                    continue;
                }

                if (inVerbatimString)
                {
                    if (c == '"' && next == '"')
                    {
                        sb.Append("  ");
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inVerbatimString = false;
                        sb.Append(' ');
                        continue;
                    }

                    sb.Append(c == '\n' || c == '\r' ? c : ' ');
                    continue;
                }

                if (inChar)
                {
                    if (!escape && c == '\'')
                    {
                        inChar = false;
                        sb.Append(' ');
                        continue;
                    }

                    escape = !escape && c == '\\';
                    sb.Append(c == '\n' || c == '\r' ? c : ' ');
                    continue;
                }

                if (c == '/' && next == '/')
                {
                    inLineComment = true;
                    sb.Append("  ");
                    i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    sb.Append("  ");
                    i++;
                    continue;
                }

                if (c == '@' && next == '"')
                {
                    inVerbatimString = true;
                    sb.Append("  ");
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    escape = false;
                    sb.Append(' ');
                    continue;
                }

                if (c == '\'')
                {
                    inChar = true;
                    escape = false;
                    sb.Append(' ');
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
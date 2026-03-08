using System.Text.RegularExpressions;
using System.Diagnostics;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Scope;
using RefactorScope.Core.Parsing;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Parser C# baseado em Regex.
    ///
    /// Objetivos desta implementação:
    /// 
    /// - Preservar compatibilidade com a arquitetura atual do RefactorScope.
    /// - Utilizar SanitizedSourceProvider para evitar interferência de comentários XML.
    /// - Evitar múltiplos acessos a disco (cache de conteúdo).
    /// - Detectar tipos estruturais de forma conservadora.
    /// - Detectar referências com heurística segura para C#.
    ///
    /// Este parser NÃO realiza parsing sintático completo (AST).
    /// Ele extrai apenas a estrutura necessária para análise arquitetural.
    /// </summary>
    public class CSharpRegexParser : IParserCodigo
    {
        public string Name => "CSharpRegex";

        private readonly SanitizedSourceProvider _sourceProvider;

        /// <summary>
        /// Construtor principal utilizado pelo ParserSelector.
        /// </summary>
        public CSharpRegexParser(SanitizedSourceProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
        }

        /// <summary>
        /// Construtor de fallback para cenários de instância direta.
        /// </summary>
        public CSharpRegexParser()
        {
            _sourceProvider = new SanitizedSourceProvider(
                new Parsers.Common.CSharpPreParser());
        }

        /// <summary>
        /// Regex para detecção de namespace.
        /// </summary>
        private static readonly Regex NamespaceRegex =
            new(@"namespace\s+([\w\.]+)",
                RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Regex restritiva para declaração de tipos C#.
        /// 
        /// Exige que após o nome exista:
        ///     {
        ///     :
        ///     <
        ///     where
        /// 
        /// Isso evita capturar identificadores falsos.
        /// </summary>
        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:<|\{|:|where)",
                RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Palavras reservadas que não devem ser tratadas como tipos.
        /// </summary>
        private static readonly HashSet<string> ReservedKeywords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "with","init","var","new","return",
                "public","private","protected","internal",
                "static","void","string","int","bool",
                "namespace","class","interface","record","struct"
            };

        /// <summary>
        /// Método principal de parsing exigido por IParserCodigo.
        /// </summary>
        public IParserResult Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var scope = new ScopeRuleSet(include, exclude);

                var arquivos = new List<ArquivoInfo>();
                var tipos = new List<TipoInfo>();
                var referencias = new List<ReferenciaInfo>();

                var csFiles = Directory
                    .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => scope.IsInScope(rootPath, f))
                    .ToList();

                /// Cache de conteúdo para evitar múltiplos acessos a disco.
                var fileContents = new Dictionary<string, string>();

                // =====================================================
                // 1️⃣ Coleta de tipos
                // =====================================================
                foreach (var file in csFiles)
                {
                    string source;

                    try
                    {
                        source = _sourceProvider.Read(file);
                    }
                    catch
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(rootPath, file);

                    fileContents[relativePath] = source;

                    var nsMatch = NamespaceRegex.Match(source);

                    var ns = nsMatch.Success
                        ? nsMatch.Groups[1].Value
                        : "Global";

                    var matches = TypeRegex.Matches(source);

                    foreach (Match match in matches)
                    {
                        var kind = match.Groups[1].Value;
                        var typeName = match.Groups[2].Value;

                        if (ReservedKeywords.Contains(typeName))
                            continue;

                        if (!IsValidIdentifier(typeName))
                            continue;

                        tipos.Add(new TipoInfo(
                            typeName,
                            ns,
                            kind,
                            relativePath,
                            new List<ReferenciaInfo>()
                        ));
                    }
                }

                var tipoNames = tipos
                    .Select(t => t.Name)
                    .ToHashSet();

                // =====================================================
                // 2️⃣ Detecção de referências
                // =====================================================
                foreach (var file in csFiles)
                {
                    var relativePath = Path.GetRelativePath(rootPath, file);

                    if (!fileContents.TryGetValue(relativePath, out var source))
                        continue;

                    var tiposDoArquivo = tipos
                        .Where(t => t.DeclaredInFile == relativePath)
                        .ToList();

                    if (tiposDoArquivo.Count == 0)
                    {
                        // ainda escanear referências
                        tiposDoArquivo = new List<TipoInfo>();
                    }

                    foreach (var target in tipoNames)
                    {
                        /// Heurística robusta para identificar uso de tipo
                        /// mesmo em generics, namespaces qualificados e arrays.
                        var pattern =
                            $@"(?<![\w\.]){Regex.Escape(target)}(?![\w])";

                        if (!Regex.IsMatch(source, pattern))
                            continue;

                        foreach (var tipo in tiposDoArquivo)
                        {
                            if (target == tipo.Name)
                                continue;

                            referencias.Add(
                                new ReferenciaInfo(
                                    tipo.Name,
                                    target,
                                    TipoReferencia.Mention
                                )
                            );
                        }
                    }
                }

                // =====================================================
                // 3️⃣ Atualização das referências nos tipos
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
                // 4️⃣ Construção dos ArquivoInfo
                // =====================================================
                foreach (var file in csFiles)
                {
                    var relativePath = Path.GetRelativePath(rootPath, file);

                    if (!fileContents.TryGetValue(relativePath, out var source))
                        continue;

                    var nsMatch = NamespaceRegex.Match(source);

                    var ns = nsMatch.Success
                        ? nsMatch.Groups[1].Value
                        : "Global";

                    var tiposDoArquivo = tipos
                        .Where(t => t.DeclaredInFile == relativePath)
                        .ToList();

                    arquivos.Add(
                        new ArquivoInfo(
                            relativePath,
                            ns,
                            source,
                            tiposDoArquivo
                        )
                    );
                }

                var model = new ModeloEstrutural(
                    rootPath,
                    arquivos,
                    tipos,
                    referencias
                );

                stopwatch.Stop();

                ParserExecutionStats? stats = null;

                return new ParserResult(
                    ParseStatus.Success,
                    true,
                    tipos.Count / (double)Math.Max(arquivos.Count, 1),
                    Name,
                    model,
                    false,
                    stats
                );
            }
            catch (Exception ex)
            {
                return new ParserResult(
                    ParseStatus.Failed,
                    false,
                    0,
                    Name,
                    null,
                    false,
                    null,
                    ex
                );
            }
        }

        /// <summary>
        /// Validação básica de identificador C#.
        /// </summary>
        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            return name.All(c =>
                char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
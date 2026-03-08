using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using RefactorScope.Core.Scope;
using RefactorScope.Parsers.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Higienizador lexical incremental para parsing linha-a-linha.
    ///
    /// O PreParser já remove comentários e strings no nível de arquivo.
    /// Este higienizador atua apenas como proteção incremental
    /// durante parsing streaming.
    ///
    /// Resolve principalmente:
    ///
    /// • comentários multilinha que atravessam linhas
    /// • fragmentos residuais de comentário
    /// • ruído estrutural mínimo
    /// </summary>
    public class HigienizadorLexico
    {
        private bool insideBlockComment = false;

        public string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            if (insideBlockComment)
            {
                int end = line.IndexOf("*/");
                if (end != -1)
                {
                    insideBlockComment = false;
                    return CleanLine(line.Substring(end + 2));
                }

                return string.Empty;
            }

            int startBlock = line.IndexOf("/*");
            if (startBlock != -1)
            {
                int endBlock = line.IndexOf("*/", startBlock + 2);

                if (endBlock != -1)
                {
                    string before = line.Substring(0, startBlock);
                    string after = line.Substring(endBlock + 2);
                    return CleanLine(before + " " + after);
                }

                insideBlockComment = true;
                return line.Substring(0, startBlock);
            }

            int lineComment = line.IndexOf("//");
            if (lineComment != -1)
                line = line.Substring(0, lineComment);

            return line.Trim();
        }
    }

    /// <summary>
    /// Parser textual estrutural para código C#.
    ///
    /// Estratégia geral:
    ///
    ///     File
    ///        ↓
    ///     PreParser
    ///        ↓
    ///     TextualParser
    ///        ↓
    ///     HigienizadorLexico
    ///        ↓
    ///     Parsing estrutural
    ///
    /// Caso ocorra desbalanceamento de chaves:
    ///
    ///     TextualParser
    ///         ↓
    ///     HeuristicFix
    ///         ↓
    ///     RegexFallback (escopo da classe)
    ///         ↓
    ///     Parsing continua normalmente
    ///
    /// O RegexFallback NÃO reanalisa o arquivo inteiro.
    /// Ele atua apenas no trecho da classe atual.
    ///
    /// Isso evita:
    /// - repetição do HybridParser
    /// - aumento de complexidade
    /// - perda de performance
    ///
    /// O objetivo do HeuristicFix é apenas reduzir parte
    /// dos erros de escopo, não resolvê-los completamente.
    /// O restante é tratado pelo RegexFallback.
    /// </summary>

        public class CSharpTextualParser : IParserCodigo
        {
            public string Name => "CSharpTextual";

            private readonly SanitizedSourceProvider sourceProvider;
            private readonly Action<string>? warn;

            public CSharpTextualParser(
                SanitizedSourceProvider sourceProvider,
                Action<string>? warningCallback = null)
            {
                this.sourceProvider = sourceProvider;
                warn = warningCallback;
            }

            public CSharpTextualParser(Action<string>? warningCallback = null)
            {
                sourceProvider = new SanitizedSourceProvider(new CSharpPreParser());
                warn = warningCallback;
            }

            private static readonly Regex TypeRegex =
                new(@"\b(class|interface|record|struct)\s+([A-Z][A-Za-z0-9_]*)\b",
                    RegexOptions.Compiled);

            private static readonly Regex NamespaceRegex =
                new(@"\bnamespace\s+([\w\.]+)\b",
                    RegexOptions.Compiled);

            private static readonly Regex NewInstanceRegex =
                new(@"new\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
                    RegexOptions.Compiled);

            private static readonly Regex StaticCallRegex =
                new(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.",
                    RegexOptions.Compiled);

            private static readonly Regex GenericRegex =
                new(@"<\s*([A-Za-z_][A-Za-z0-9_]*)\s*>",
                    RegexOptions.Compiled);

            private static readonly Regex TypeofRegex =
                new(@"typeof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)",
                    RegexOptions.Compiled);

            private static readonly Regex NameofRegex =
                new(@"nameof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)",
                    RegexOptions.Compiled);

            private static readonly Regex WordRegex =
                new(@"\b[A-Za-z_][A-Za-z0-9_]*\b",
                    RegexOptions.Compiled);

            public IParserResult Parse(
                string rootPath,
                IEnumerable<string>? include = null,
                IEnumerable<string>? exclude = null)
            {
                var stopwatch = Stopwatch.StartNew();
                long initialMemory = GC.GetTotalMemory(false);

                try
                {
                    var scope = new ScopeRuleSet(include, exclude);

                    var arquivos = new List<ArquivoInfo>();
                    var tiposGlobais = new List<TipoInfo>();
                    var referenciasGlobais = new List<ReferenciaInfo>();

                    var csFiles = System.IO.Directory
                        .GetFiles(rootPath, "*.cs", System.IO.SearchOption.AllDirectories)
                        .Where(f => scope.IsInScope(rootPath, f))
                        .ToList();

                    foreach (var file in csFiles)
                    {
                        var relative = System.IO.Path.GetRelativePath(rootPath, file);
                        tiposGlobais.AddRange(MapearTipos(file, relative));
                    }

                    var nomesTipos = tiposGlobais.Select(t => t.Name).ToHashSet();

                    foreach (var file in csFiles)
                    {
                        referenciasGlobais.AddRange(
                            ExtrairReferencias(file, nomesTipos));
                    }

                    foreach (var tipo in tiposGlobais)
                    {
                        var refs = referenciasGlobais
                            .Where(r => r.FromType == tipo.Name)
                            .ToList();

                        typeof(TipoInfo)
                            .GetField("<References>k__BackingField",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic)
                            ?.SetValue(tipo, refs);
                    }

                    foreach (var file in csFiles)
                    {
                        var relative = System.IO.Path.GetRelativePath(rootPath, file);
                        var tiposArquivo = tiposGlobais.Where(t => t.DeclaredInFile == relative).ToList();

                        string source = sourceProvider.Read(file);
                        string ns = tiposArquivo.FirstOrDefault()?.Namespace ?? "Global";

                        arquivos.Add(new ArquivoInfo(relative, ns, source, tiposArquivo));
                    }

                    var modeloGerado =
                        new ModeloEstrutural(rootPath, arquivos, tiposGlobais, referenciasGlobais);

                    stopwatch.Stop();

                    long memoryUsed = GC.GetTotalMemory(false) - initialMemory;

                    bool isPlausible =
                        PlausibilityEvaluator.Evaluate(modeloGerado);

                    var status =
                        isPlausible ? ParseStatus.Success : ParseStatus.PlausibilityWarning;

                    var stats =
                        new ParserExecutionStats(stopwatch.Elapsed, memoryUsed, !isPlausible);

                    return new ParserResult(
                        status,
                        isPlausible,
                        isPlausible ? 0.95 : 0.40,
                        Name,
                        modeloGerado,
                        false,
                        stats
                    );
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

            private List<TipoInfo> MapearTipos(string path, string relative)
            {
                var tipos = new List<TipoInfo>();
                var hig = new HigienizadorLexico();

                string ns = "Global";

                var source = sourceProvider.Read(path);
                var lines = source.Split('\n');

                foreach (var raw in lines)
                {
                    var line = hig.CleanLine(raw);

                    if (string.IsNullOrEmpty(line))
                        continue;

                    var nsMatch = NamespaceRegex.Match(line);
                    if (nsMatch.Success)
                    {
                        ns = nsMatch.Groups[1].Value;
                        continue;
                    }

                    var typeMatch = TypeRegex.Match(line);
                    if (typeMatch.Success)
                    {
                        tipos.Add(new TipoInfo(
                            typeMatch.Groups[2].Value,
                            ns,
                            typeMatch.Groups[1].Value,
                            relative,
                            new List<ReferenciaInfo>()));
                    }
                }

                return tipos;
            }

        private List<ReferenciaInfo> ExtrairReferencias(
 string path,
 HashSet<string> tipos)
        {
            var refs = new List<ReferenciaInfo>();
            var hig = new HigienizadorLexico();

            int braceLevel = 0;
            int typeBraceLevel = -1;
            string? currentType = null;
            bool insideTypeScope = false; // NOVA FLAG: Garante que entramos no bloco antes de tentar sair

            var classBuffer = new StringBuilder();

            var source = sourceProvider.Read(path);
            var lines = source.Split('\n');

            void AddReference(string from, string to, TipoReferencia kind)
            {
                if (from == to) return;
                if (!tipos.Contains(to)) return;

                if (!refs.Any(r => r.FromType == from && r.ToType == to && r.Kind == kind))
                {
                    refs.Add(new ReferenciaInfo(from, to, kind));
                }
            }

            foreach (var raw in lines)
            {
                var line = hig.CleanLine(raw);
                if (string.IsNullOrEmpty(line))
                    continue;

                // -------------------------------------------------
                // 1. Detectar declaração de tipo
                // -------------------------------------------------
                var typeMatch = TypeRegex.Match(line);
                if (typeMatch.Success)
                {
                    currentType = typeMatch.Groups[2].Value;
                    typeBraceLevel = braceLevel;
                    insideTypeScope = false; // Resetamos a flag para a nova classe
                    classBuffer.Clear();
                }

                // -------------------------------------------------
                // 2. Atualizar contagem de chaves e validar entrada
                // -------------------------------------------------
                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                braceLevel += opens;

                // Se o nível subiu acima do baseline, confirmamos que entramos no corpo da classe
                if (currentType != null && braceLevel > typeBraceLevel)
                {
                    insideTypeScope = true;
                }

                if (currentType != null)
                    classBuffer.AppendLine(line);

                braceLevel -= closes;

                // -------------------------------------------------
                // 3. Extrair referências PRIMEIRO
                // -------------------------------------------------
                if (currentType != null)
                {
                    var fromType = currentType;

                    foreach (Match m in NewInstanceRegex.Matches(line))
                        AddReference(fromType, m.Groups[1].Value, TipoReferencia.Instantiation);

                    foreach (Match m in StaticCallRegex.Matches(line))
                        AddReference(fromType, m.Groups[1].Value, TipoReferencia.StaticCall);

                    foreach (Match m in GenericRegex.Matches(line))
                        AddReference(fromType, m.Groups[1].Value, TipoReferencia.Generic);

                    foreach (Match m in TypeofRegex.Matches(line))
                        AddReference(fromType, m.Groups[1].Value, TipoReferencia.Typeof);

                    foreach (Match m in NameofRegex.Matches(line))
                        AddReference(fromType, m.Groups[1].Value, TipoReferencia.Nameof);

                    foreach (Match m in WordRegex.Matches(line))
                        AddReference(fromType, m.Value, TipoReferencia.Mention);
                }

                // -------------------------------------------------
                // 4. Verificar saída de escopo DEPOIS de extrair as referências
                // -------------------------------------------------
                if (currentType != null && insideTypeScope && braceLevel <= typeBraceLevel)
                {
                    if (braceLevel < typeBraceLevel)
                    {
                        warn?.Invoke(
                            $"Desbalanceamento detectado ao sair de {currentType}. " +
                            $"Esperado: {typeBraceLevel}, Atual: {braceLevel}");

                        var recovered = RegexLocalRecovery.Recover(
                            classBuffer.ToString(),
                            currentType,
                            tipos);

                        refs.AddRange(recovered);
                    }

                    // Encerra o tracking desta classe
                    currentType = null;
                    typeBraceLevel = -1;
                    insideTypeScope = false;
                }
            }

            return refs;
        }
    }
}
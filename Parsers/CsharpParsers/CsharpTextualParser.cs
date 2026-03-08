using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using RefactorScope.Parsers.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Higienizador lexical incremental para parsing linha-a-linha.
    ///
    /// Papel:
    /// - remover ruído residual que ainda chega ao parser textual
    /// - preservar o fluxo streaming
    /// - reduzir falsos positivos de referência e desbalanceamento
    ///
    /// Importante:
    /// - não substitui um lexer completo
    /// - não reescreve o código
    /// - atua como camada defensiva incremental
    /// </summary>
    public class HigienizadorLexico
    {
        private bool insideBlockComment;
        private bool insideVerbatimString;
        private bool insideRegularString;

        public string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var sb = new StringBuilder(line.Length);

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                char next = i + 1 < line.Length ? line[i + 1] : '\0';

                if (insideBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        insideBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (insideVerbatimString)
                {
                    if (c == '"' && next == '"')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        insideVerbatimString = false;
                    }

                    continue;
                }

                if (insideRegularString)
                {
                    if (c == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        insideRegularString = false;
                    }

                    continue;
                }

                if (c == '/' && next == '*')
                {
                    insideBlockComment = true;
                    i++;
                    continue;
                }

                if (c == '/' && next == '/')
                {
                    break;
                }

                if (c == '@' && next == '"')
                {
                    insideVerbatimString = true;
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    insideRegularString = true;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString().Trim();
        }
    }

    /// <summary>
    /// Parser textual estrutural para código C#.
    ///
    /// Estratégia:
    /// - leitura streaming por linhas
    /// - detecção incremental de namespace, tipos e referências
    /// - recuperação local via Regex quando há perda de escopo
    ///
    /// Este parser é mais sensível ao contexto do que o Regex parser.
    /// Por isso, no pipeline seletivo, ele é utilizado como camada de refinamento
    /// sobre arquivos SAFE e não como fonte canônica global.
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
            new(@"\b(class|interface|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\b",
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
                var arquivos = new List<ArquivoInfo>();
                var tiposGlobais = new List<TipoInfo>();
                var referenciasGlobais = new List<ReferenciaInfo>();

                var scope = new FileSelectionScope(rootPath, include, exclude);

                var csFiles = Directory
                    .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                    .Where(scope.IsInScope)
                    .ToList();

                foreach (var file in csFiles)
                {
                    var relative = Path.GetRelativePath(rootPath, file);
                    tiposGlobais.AddRange(MapearTipos(file, relative));
                }

                tiposGlobais = tiposGlobais
                    .GroupBy(t => $"{t.Namespace}|{t.Name}|{t.DeclaredInFile}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                var nomesTipos = tiposGlobais
                    .Select(t => t.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var file in csFiles)
                {
                    referenciasGlobais.AddRange(
                        ExtrairReferencias(file, nomesTipos));
                }

                referenciasGlobais = referenciasGlobais
                    .GroupBy(r => $"{r.FromType}|{r.ToType}|{r.Kind}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

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
                    var relative = Path.GetRelativePath(rootPath, file);
                    var tiposArquivo = tiposGlobais
                        .Where(t => t.DeclaredInFile.Equals(relative, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    string source = sourceProvider.Read(file);
                    string ns = tiposArquivo.FirstOrDefault()?.Namespace ?? "Global";

                    arquivos.Add(new ArquivoInfo(relative, ns, source, tiposArquivo));
                }

                var modeloGerado =
                    new ModeloEstrutural(rootPath, arquivos, tiposGlobais, referenciasGlobais);

                stopwatch.Stop();

                long memoryUsed = Math.Max(0, GC.GetTotalMemory(false) - initialMemory);

                bool isPlausible = PlausibilityEvaluator.Evaluate(modeloGerado);

                var status = isPlausible
                    ? ParseStatus.Success
                    : ParseStatus.PlausibilityWarning;

                var stats =
                    new ParserExecutionStats(stopwatch.Elapsed, memoryUsed, !isPlausible);

                return new ParserResult(
                    status,
                    isPlausible,
                    isPlausible ? 0.95 : 0.40,
                    Name,
                    modeloGerado,
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
                    var kind = typeMatch.Groups[1].Value;
                    var typeName = typeMatch.Groups[2].Value;

                    if (!StructuralTokenGuard.IsValidDeclaredTypeName(typeName))
                        continue;

                    tipos.Add(new TipoInfo(
                        typeName,
                        ns,
                        kind,
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
            bool insideTypeScope = false;

            var classBuffer = new StringBuilder();

            var source = sourceProvider.Read(path);
            var lines = source.Split('\n');

            void AddReference(string from, string to, TipoReferencia kind)
            {
                if (string.IsNullOrWhiteSpace(from))
                    return;

                if (from == to)
                    return;

                if (!StructuralTokenGuard.IsValidReferenceTarget(to, tipos))
                    return;

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
                    var candidateType = typeMatch.Groups[2].Value;

                    if (StructuralTokenGuard.IsValidDeclaredTypeName(candidateType))
                    {
                        currentType = candidateType;
                        typeBraceLevel = braceLevel;
                        insideTypeScope = false;
                        classBuffer.Clear();
                    }
                }

                // -------------------------------------------------
                // 2. Atualizar contagem de chaves e validar entrada
                // -------------------------------------------------
                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                braceLevel += opens;

                if (currentType != null && braceLevel > typeBraceLevel)
                {
                    insideTypeScope = true;
                }

                if (currentType != null)
                    classBuffer.AppendLine(line);

                braceLevel -= closes;

                // -------------------------------------------------
                // 3. Extrair referências
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
                // 4. Verificar saída de escopo
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

                        foreach (var recoveredRef in recovered)
                        {
                            if (!refs.Any(r =>
                                r.FromType == recoveredRef.FromType &&
                                r.ToType == recoveredRef.ToType &&
                                r.Kind == recoveredRef.Kind))
                            {
                                refs.Add(recoveredRef);
                            }
                        }
                    }

                    currentType = null;
                    typeBraceLevel = -1;
                    insideTypeScope = false;
                }
            }

            return refs;
        }
    }
}
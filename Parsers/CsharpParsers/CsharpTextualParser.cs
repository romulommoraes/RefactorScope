using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Parsing;
using RefactorScope.Core.Model;
using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Higienizador lexical simplificado para código C#.
    /// </summary>
    public class HigienizadorLexico
    {
        private bool insideBlockComment = false;

        public string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            line = Regex.Replace(line, "\"(?:\\\\.|[^\"])*\"", "\"\"");

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
                else
                {
                    insideBlockComment = true;
                    return CleanLine(line.Substring(0, startBlock));
                }
            }

            int lineComment = line.IndexOf("//");
            if (lineComment != -1)
                line = line.Substring(0, lineComment);

            return line.Trim();
        }
    }

    /// <summary>
    /// Parser textual estrutural para código C#.
    /// Agora envelopado pela arquitetura de ParseResult para segurança e telemetria.
    /// </summary>
    public class CSharpTextualParser : IParserCodigo
    {
        public string Name => "CSharpTextual";

        private readonly Action<string>? warn;

        public CSharpTextualParser(Action<string>? warningCallback = null)
        {
            warn = warningCallback;
        }

        // Regex de estrutura
        private static readonly Regex TypeRegex = new(@"\b(class|interface|record|struct)\s+([A-Z][A-Za-z0-9_]*)\b", RegexOptions.Compiled);
        private static readonly Regex NamespaceRegex = new(@"\bnamespace\s+([\w\.]+)\b", RegexOptions.Compiled);

        // Regex de evidência
        private static readonly Regex GenericRegex = new(@"<\s*([A-Za-z_][A-Za-z0-9_]*)\s*>", RegexOptions.Compiled);
        private static readonly Regex TypeofRegex = new(@"typeof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)", RegexOptions.Compiled);
        private static readonly Regex NameofRegex = new(@"nameof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)", RegexOptions.Compiled);
        private static readonly Regex NewInstanceRegex = new(@"new\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.Compiled);
        private static readonly Regex StaticCallRegex = new(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.", RegexOptions.Compiled);
        private static readonly Regex DeclarationRegex = new(@"\b([A-Za-z_][A-Za-z0-9_]*)\s+[A-Za-z_][A-Za-z0-9_]*\s*(=|;)", RegexOptions.Compiled);
        private static readonly Regex WordRegex = new(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);

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

                var csFiles = Directory
                    .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => scope.IsInScope(rootPath, f))
                    .ToList();

                // -------------------------------------------------
                // PASSO 1 — Mapear todos os tipos do projeto
                // -------------------------------------------------
                foreach (var file in csFiles)
                {
                    try
                    {
                        var relative = Path.GetRelativePath(rootPath, file);
                        tiposGlobais.AddRange(MapearTipos(file, relative));
                    }
                    catch (Exception ex)
                    {
                        warn?.Invoke($"Parser ignorou arquivo '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }

                var nomesTipos = tiposGlobais.Select(t => t.Name).ToHashSet();

                // -------------------------------------------------
                // PASSO 2 — Extrair referências
                // -------------------------------------------------
                foreach (var file in csFiles)
                {
                    try
                    {
                        referenciasGlobais.AddRange(ExtrairReferencias(file, nomesTipos));
                    }
                    catch (Exception ex)
                    {
                        warn?.Invoke($"Falha ao analisar referências em '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }

                // -------------------------------------------------
                // PASSO 3 — Consolidar modelo
                // -------------------------------------------------
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
                    var tiposArquivo = tiposGlobais.Where(t => t.DeclaredInFile == relative).ToList();
                    string source = File.ReadAllText(file);
                    string ns = tiposArquivo.FirstOrDefault()?.Namespace ?? "Global";

                    arquivos.Add(new ArquivoInfo(relative, ns, source, tiposArquivo));
                }

                var modeloGerado = new ModeloEstrutural(rootPath, arquivos, tiposGlobais, referenciasGlobais);

                // =====================================================
                // 4️⃣ Finalização e Observabilidade
                // =====================================================
                stopwatch.Stop();
                long memoryUsed = GC.GetTotalMemory(false) - initialMemory;

                bool isPlausible = PlausibilityEvaluator.Evaluate(modeloGerado);
                var status = isPlausible ? ParseStatus.Success : ParseStatus.PlausibilityWarning;

                var stats = new ParserExecutionStats(stopwatch.Elapsed, memoryUsed, !isPlausible);

                // Confiança maior no Textual Parser devido à higienização
                double confidence = isPlausible ? 0.95 : 0.40;

                return new ParserResult(
                Status: status,
                IsPlausible: isPlausible,
                Confidence: confidence,
                ParserName: Name,
                Model: modeloGerado,
                UsedFallback: false,
                Stats: stats
            );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var stats = new ParserExecutionStats(stopwatch.Elapsed, 0, true);
                return new ParserResult(ParseStatus.Failed, false, 0.0, Name, null, false, stats, ex);
            }
        }

        private List<TipoInfo> MapearTipos(string path, string relative)
        {
            var tipos = new List<TipoInfo>();
            var hig = new HigienizadorLexico();
            string ns = "Global";

            foreach (var line in File.ReadLines(path))
            {
                if (line.Contains("AppendLine(@\"") || line.Contains("Append(@\""))
                    continue;

                string clean = hig.CleanLine(line);
                if (string.IsNullOrEmpty(clean))
                    continue;

                var nsMatch = NamespaceRegex.Match(clean);
                if (nsMatch.Success)
                {
                    ns = nsMatch.Groups[1].Value;
                    continue;
                }

                var typeMatch = TypeRegex.Match(clean);
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
        private List<ReferenciaInfo> ExtrairReferencias(string path, HashSet<string> tipos)
        {
            var refs = new List<ReferenciaInfo>();
            var hig = new HigienizadorLexico();

            int braceLevel = 0;
            int typeBraceLevel = -1;
            string? currentType = null;

            void AddReference(string from, string to, TipoReferencia kind)
            {
                if (from == to) return;
                if (!tipos.Contains(to)) return;

                if (!refs.Any(r => r.FromType == from && r.ToType == to && r.Kind == kind))
                {
                    refs.Add(new ReferenciaInfo(from, to, kind));
                }
            }

            foreach (var line in File.ReadLines(path))
            {
                string clean = hig.CleanLine(line);
                if (string.IsNullOrEmpty(clean)) continue;

                braceLevel += clean.Count(c => c == '{');

                var typeMatch = TypeRegex.Match(clean);
                if (typeMatch.Success)
                {
                    currentType = typeMatch.Groups[2].Value;
                    typeBraceLevel = braceLevel;
                }

                if (clean.Contains("}"))
                {
                    braceLevel -= clean.Count(c => c == '}');

                    if (currentType != null && braceLevel < typeBraceLevel)
                    {
                        currentType = null;
                        typeBraceLevel = -1;
                    }
                }

                if (currentType == null) continue;

                var fromType = currentType!;

                foreach (Match m in NewInstanceRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.Instantiation);

                foreach (Match m in StaticCallRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.StaticCall);

                foreach (Match m in GenericRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.Generic);

                foreach (Match m in TypeofRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.Typeof);

                foreach (Match m in NameofRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.Nameof);

                foreach (Match m in DeclarationRegex.Matches(clean))
                    AddReference(fromType, m.Groups[1].Value, TipoReferencia.Declaration);

                foreach (Match m in WordRegex.Matches(clean))
                    AddReference(fromType, m.Value, TipoReferencia.Mention);
            }

            if (braceLevel != 0)
            {
                warn?.Invoke(
                    $"Desbalanceamento de chaves detectado em '{Path.GetFileName(path)}'. " +
                    $"O escopo de referências pode estar impreciso.");
            }

            return refs;
        }
    }
}
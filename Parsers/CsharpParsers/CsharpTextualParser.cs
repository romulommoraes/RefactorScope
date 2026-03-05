using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.CsharpParsers

{
    /// <summary>
    /// Higienizador lexical simplificado para código C#.
    ///
    /// Responsabilidade:
    /// Remover conteúdo irrelevante para análise estrutural,
    /// como comentários e conteúdo de strings.
    ///
    /// Isso reduz drasticamente falsos positivos na análise textual.
    ///
    /// Características:
    /// - Mantém estado para comentários de bloco multilinha.
    /// - Neutraliza strings antes de processar comentários.
    /// - Evita o problema clássico de "String Trap"
    ///   (URLs ou comentários dentro de strings).
    /// </summary>
    public class HigienizadorLexico
    {
        private bool insideBlockComment = false;

        public string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            // -------------------------------------------------
            // 1. Neutralizar strings primeiro
            // -------------------------------------------------
            // Isso evita que comentários falsos dentro de strings
            // quebrem o parser.
            //
            // Ex:
            // string url = "http://site.com";
            // vira
            // string url = "";
            //
            line = Regex.Replace(line, "\"(?:\\\\.|[^\"])*\"", "\"\"");

            // -------------------------------------------------
            // 2. Processar comentários de bloco
            // -------------------------------------------------
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

            // -------------------------------------------------
            // 3. Remover comentários de linha
            // -------------------------------------------------
            int lineComment = line.IndexOf("//");

            if (lineComment != -1)
                line = line.Substring(0, lineComment);

            return line.Trim();
        }
    }

    /// <summary>
    /// Parser textual estrutural para código C#.
    ///
    /// Objetivo:
    /// Construir um grafo de dependência entre tipos
    /// sem depender de Roslyn ou AST completa.
    ///
    /// Estratégia:
    /// - Higienização lexical
    /// - Detecção de tipos
    /// - Extração heurística de referências
    /// - Qualificação de evidências
    ///
    /// Este parser é otimizado para:
    /// - análise arquitetural
    /// - detecção de código morto
    /// - análise de acoplamento
    ///
    /// Ele é projetado para degradar graciosamente:
    /// erros em arquivos individuais não interrompem a análise.
    /// </summary>
    public class CSharpTextualParser : IParserCodigo
    {
        public string Name => "CSharpTextual";

        /// <summary>
        /// Canal opcional para envio de avisos.
        /// Permite desacoplar o parser da UI (Console).
        /// </summary>
        private readonly Action<string>? warn;

        public CSharpTextualParser(Action<string>? warningCallback = null)
        {
            warn = warningCallback;
        }

        // -------------------------------------------
        // Regex de estrutura
        // -------------------------------------------

        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+([A-Z][A-Za-z0-9_]*)\b",
            RegexOptions.Compiled);

        private static readonly Regex NamespaceRegex =
            new(@"\bnamespace\s+([\w\.]+)\b",
            RegexOptions.Compiled);

        // -------------------------------------------
        // Regex de evidência
        // -------------------------------------------

        private static readonly Regex GenericRegex =
            new(@"<\s*([A-Za-z_][A-Za-z0-9_]*)\s*>",
            RegexOptions.Compiled);

        private static readonly Regex TypeofRegex =
            new(@"typeof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)",
            RegexOptions.Compiled);

        private static readonly Regex NameofRegex =
            new(@"nameof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)",
            RegexOptions.Compiled);

        private static readonly Regex NewInstanceRegex =
            new(@"new\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex StaticCallRegex =
            new(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.",
            RegexOptions.Compiled);

        /// <summary>
        /// Detecta declaração de variável tipada.
        /// Ex:
        /// UserService service;
        /// </summary>
        private static readonly Regex DeclarationRegex =
            new(@"\b([A-Za-z_][A-Za-z0-9_]*)\s+[A-Za-z_][A-Za-z0-9_]*\s*(=|;)",
            RegexOptions.Compiled);

        private static readonly Regex WordRegex =
            new(@"\b[A-Za-z_][A-Za-z0-9_]*\b",
            RegexOptions.Compiled);

        public ModeloEstrutural Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
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
                    warn?.Invoke(
                        $"Parser ignorou arquivo '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            var nomesTipos = tiposGlobais
                .Select(t => t.Name)
                .ToHashSet();

            // -------------------------------------------------
            // PASSO 2 — Extrair referências
            // -------------------------------------------------

            foreach (var file in csFiles)
            {
                try
                {
                    referenciasGlobais.AddRange(
                        ExtrairReferencias(file, nomesTipos));
                }
                catch (Exception ex)
                {
                    warn?.Invoke(
                        $"Falha ao analisar referências em '{Path.GetFileName(file)}': {ex.Message}");
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

                var tiposArquivo = tiposGlobais
                    .Where(t => t.DeclaredInFile == relative)
                    .ToList();

                string source = File.ReadAllText(file);

                string ns = tiposArquivo.FirstOrDefault()?.Namespace ?? "Global";

                arquivos.Add(new ArquivoInfo(
                    relative,
                    ns,
                    source,
                    tiposArquivo));
            }

            return new ModeloEstrutural(
                rootPath,
                arquivos,
                tiposGlobais,
                referenciasGlobais);
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

        private List<ReferenciaInfo> ExtrairReferencias(
            string path,
            HashSet<string> tipos)
        {
            var refs = new List<ReferenciaInfo>();
            var hig = new HigienizadorLexico();

            int braceLevel = 0;
            int typeBraceLevel = -1;
            string currentType = null;

            void AddReference(string from, string to, TipoReferencia kind)
            {
                if (from == to) return;
                if (!tipos.Contains(to)) return;

                if (!refs.Any(r =>
                    r.FromType == from &&
                    r.ToType == to &&
                    r.Kind == kind))
                {
                    refs.Add(new ReferenciaInfo(from, to, kind));
                }
            }

            foreach (var line in File.ReadLines(path))
            {
                string clean = hig.CleanLine(line);

                if (string.IsNullOrEmpty(clean))
                    continue;

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

                    if (currentType != null &&
                        braceLevel < typeBraceLevel)
                    {
                        currentType = null;
                        typeBraceLevel = -1;
                    }
                }

                if (currentType == null)
                    continue;

                foreach (Match m in NewInstanceRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.Instantiation);

                foreach (Match m in StaticCallRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.StaticCall);

                foreach (Match m in GenericRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.Generic);

                foreach (Match m in TypeofRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.Typeof);

                foreach (Match m in NameofRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.Nameof);

                foreach (Match m in DeclarationRegex.Matches(clean))
                    AddReference(currentType, m.Groups[1].Value, TipoReferencia.Declaration);

                foreach (Match m in WordRegex.Matches(clean))
                    AddReference(currentType, m.Value, TipoReferencia.Mention);
            }

            // -------------------------------------------------
            // Verificação de integridade de escopo
            // -------------------------------------------------

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
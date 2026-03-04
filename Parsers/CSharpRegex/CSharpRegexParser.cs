using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.CSharpRegex
{
    /// <summary>
    /// Parser C# baseado em Regex (versão endurecida).
    /// 
    /// Correções aplicadas:
    /// ✔ Ignora palavras reservadas modernas (with, init, etc.)
    /// ✔ Valida identificador C# válido
    /// ✔ Evita capturar construções "record with"
    /// ✔ Mantém compatibilidade com modelo atual
    /// </summary>
    public class CSharpRegexParser : IParserCodigo
    {
        public string Name => "CSharpRegex";

        private static readonly Regex NamespaceRegex =
            new(@"namespace\s+([\w\.]+)", RegexOptions.Compiled);

        // 🔒 Exige que após o nome exista:
        // espaço + {  OU
        // espaço + :  OU
        // espaço + where  OU
        // espaço + < (genérico)
        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:<|\{|:|where)",
                RegexOptions.Compiled);

        private static readonly HashSet<string> ReservedKeywords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "with", "init", "var", "new", "return",
                "public", "private", "protected", "internal",
                "static", "void", "string", "int", "bool",
                "namespace", "class", "interface", "record", "struct"
            };

        public ModeloEstrutural Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            var scope = new ScopeRuleSet(include, exclude);

            var arquivos = new List<ArquivoInfo>();
            var tipos = new List<TipoInfo>();

            var csFiles = Directory
                .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => scope.IsInScope(rootPath, f))
                .ToList();

            // =====================================================
            // 1️⃣ Coletar tipos
            // =====================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                var nsMatch = NamespaceRegex.Match(source);
                var ns = nsMatch.Success
                    ? nsMatch.Groups[1].Value
                    : "Global";

                var typeMatches = TypeRegex.Matches(source);

                foreach (Match match in typeMatches)
                {
                    var kind = match.Groups[1].Value;
                    var typeName = match.Groups[2].Value;

                    // 🔒 Ignorar palavras reservadas
                    if (ReservedKeywords.Contains(typeName))
                        continue;

                    // 🔒 Validar identificador C# válido
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

            var tipoNames = tipos.Select(t => t.Name).ToHashSet();
            var referencias = new List<ReferenciaInfo>();

            // =====================================================
            // 2️⃣ Detectar referências
            // =====================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                foreach (var tipo in tipos.Where(t => t.DeclaredInFile == relativePath))
                {
                    foreach (var target in tipoNames)
                    {
                        if (target == tipo.Name)
                            continue;

                        if (Regex.IsMatch(source, $@"\b{target}\b"))
                        {
                            referencias.Add(new ReferenciaInfo(tipo.Name, target));
                        }
                    }
                }
            }

            // =====================================================
            // 3️⃣ Atualizar referências nos tipos
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
            // 4️⃣ Construir ArquivoInfo
            // =====================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                var nsMatch = NamespaceRegex.Match(source);
                var ns = nsMatch.Success
                    ? nsMatch.Groups[1].Value
                    : "Global";

                var tiposDoArquivo = tipos
                    .Where(t => t.DeclaredInFile == relativePath)
                    .ToList();

                arquivos.Add(new ArquivoInfo(
                    relativePath,
                    ns,
                    source,
                    tiposDoArquivo
                ));
            }

            return new ModeloEstrutural(
                rootPath,
                arquivos,
                tipos,
                referencias
            );
        }

        // =====================================================
        // 🔎 Validação simples de identificador C#
        // =====================================================
        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            return name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
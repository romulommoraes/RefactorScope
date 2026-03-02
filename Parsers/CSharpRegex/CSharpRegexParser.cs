using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.CSharpRegex
{
    /// <summary>
    /// Parser C# baseado em Regex.
    /// 
    /// Constrói o Modelo Estrutural do sistema sem depender de AST ou Roslyn.
    /// 
    /// Responsabilidades:
    /// - Identificar tipos (class, interface, record, struct)
    /// - Detectar namespace
    /// - Mapear referências entre tipos
    /// - Construir ArquivoInfo
    /// - Produzir ModeloEstrutural consumido pelos Analyzers
    /// 
    /// O escopo de análise é determinado por ScopeRuleSet,
    /// garantindo comportamento determinístico entre ambientes.
    /// </summary>
    public class CSharpRegexParser : IParserCodigo
    {
        public string Name => "CSharpRegex";

        private static readonly Regex NamespaceRegex =
            new(@"namespace\s+([\w\.]+)", RegexOptions.Compiled);

        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+(\w+)",
                RegexOptions.Compiled);

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

            // =========================
            // 1️⃣ Coletar tipos
            // =========================
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

                    var invalid = new HashSet<string>
                    {
                        "public", "private", "internal", "protected",
                        "class", "interface", "record", "struct",
                        "static", "void", "string", "int", "bool"
                    };

                    if (invalid.Contains(typeName))
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

            // =========================
            // 2️⃣ Detectar referências
            // =========================
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

            // =========================
            // 3️⃣ Atualizar refs
            // =========================
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

            // =========================
            // 4️⃣ ArquivoInfo
            // =========================
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
    }
}
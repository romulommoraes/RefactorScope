using System.Text.RegularExpressions;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;

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
    /// Design:
    /// - Dois passos:
    ///     1) Descoberta de Tipos
    ///     2) Detecção de Referências
    /// </summary>
    public class CSharpRegexParser : IParserCodigo
    {
        public string Name => "CSharpRegex";

        /// <summary>
        /// Detecta namespace do arquivo.
        /// </summary>
        private static readonly Regex NamespaceRegex =
            new(@"namespace\s+([\w\.]+)", RegexOptions.Compiled);

        /// <summary>
        /// Detecta declaração de tipos.
        /// </summary>
        private static readonly Regex TypeRegex =
            new(@"\b(class|interface|record|struct)\s+(\w+)",
                RegexOptions.Compiled);

        public ModeloEstrutural Parse(string rootPath)
        {
            var arquivos = new List<ArquivoInfo>();
            var tipos = new List<TipoInfo>();

            var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

            // ==================================================
            // 1️⃣ Primeiro passo: coletar todos os tipos
            // ==================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                var namespaceMatch = NamespaceRegex.Match(source);
                var ns = namespaceMatch.Success
                    ? namespaceMatch.Groups[1].Value
                    : "Global";

                var typeMatches = TypeRegex.Matches(source);

                foreach (Match match in typeMatches)
                {
                    var kind = match.Groups[1].Value;
                    var typeName = match.Groups[2].Value;

                    // Filtro contra falsos positivos léxicos
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
                        new List<ReferenciaInfo>()));
                }
            }

            var tipoNames = tipos.Select(t => t.Name).ToHashSet();
            var referencias = new List<ReferenciaInfo>();

            // ==================================================
            // 2️⃣ Segundo passo: detectar referências reais
            // ==================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                foreach (var tipo in tipos.Where(t => t.DeclaredInFile == relativePath))
                {
                    foreach (var possibleTarget in tipoNames)
                    {
                        if (possibleTarget == tipo.Name)
                            continue;

                        if (Regex.IsMatch(source, $@"\b{possibleTarget}\b"))
                        {
                            referencias.Add(new ReferenciaInfo(tipo.Name, possibleTarget));
                        }
                    }
                }
            }

            // ==================================================
            // 3️⃣ Atualizar referências nos tipos
            // ==================================================
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

            // ==================================================
            // 4️⃣ Construir ArquivoInfo
            // ==================================================
            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                var namespaceMatch = NamespaceRegex.Match(source);
                var ns = namespaceMatch.Success
                    ? namespaceMatch.Groups[1].Value
                    : "Global";

                var tiposDoArquivo = tipos
                    .Where(t => t.DeclaredInFile == relativePath)
                    .ToList();

                arquivos.Add(new ArquivoInfo(
                    relativePath,
                    ns,
                    source,
                    tiposDoArquivo));
            }

            // ==================================================
            // 5️⃣ Modelo final
            // ==================================================
            return new ModeloEstrutural(
                rootPath,
                arquivos,
                tipos,
                referencias);
        }

        /// <summary>
        /// Método auxiliar para detecção léxica genérica de referências.
        /// Atualmente não utilizado no pipeline principal.
        /// Mantido para evolução futura.
        /// </summary>
        private List<ReferenciaInfo> DetectReferences(string source, string fromType)
        {
            var refs = new List<ReferenciaInfo>();
            var tokens = Regex.Matches(source, @"\b[A-Z]\w+\b");

            foreach (Match token in tokens)
            {
                var toType = token.Value;

                if (toType == fromType)
                    continue;

                refs.Add(new ReferenciaInfo(fromType, toType));
            }

            return refs;
        }
    }
}
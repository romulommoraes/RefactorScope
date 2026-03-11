using RefactorScope.Core.Scope;

namespace RefactorScope.Parsers.Common
{
    /// <summary>
    /// Resolve se um arquivo deve ser incluído na execução atual.
    ///
    /// Suporta dois modos:
    ///
    /// 1) Escopo tradicional via ScopeRuleSet
    /// 2) Seleção explícita por arquivo (absoluto ou relativo),
    ///    usada pelo HybridSelectiveParser
    ///
    /// Regras adicionais:
    /// - aplica exclusões estruturais padrão para evitar contaminação
    ///   do modelo por testes, artefatos de build e diretórios auxiliares
    /// - include explícito por arquivo pode furar a exclusão padrão
    /// </summary>
    internal sealed class FileSelectionScope
    {
        private readonly ScopeRuleSet _scope;
        private readonly HashSet<string>? _includeSet;
        private readonly HashSet<string>? _excludeSet;
        private readonly string _rootPath;
        private readonly bool _explicitFileSelection;

        public FileSelectionScope(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            _rootPath = Path.GetFullPath(rootPath);

            var includeList = include?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var excludeList = exclude?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            _scope = new ScopeRuleSet(includeList, excludeList);

            _includeSet = NormalizeSet(includeList);
            _excludeSet = NormalizeSet(excludeList);

            _explicitFileSelection =
                ContainsExplicitFilePath(includeList) ||
                ContainsExplicitFilePath(excludeList);
        }

        public bool IsInScope(string filePath)
        {
            var absolutePath = Path.GetFullPath(filePath);
            var absolute = Normalize(absolutePath);
            var relative = Normalize(Path.GetRelativePath(_rootPath, absolutePath));

            var explicitlyIncluded =
                _includeSet is { Count: > 0 } &&
                (_includeSet.Contains(absolute) || _includeSet.Contains(relative));

            var explicitlyExcluded =
                _excludeSet is { Count: > 0 } &&
                (_excludeSet.Contains(absolute) || _excludeSet.Contains(relative));

            // -------------------------------------------------
            // Exclusão estrutural padrão
            // -------------------------------------------------
            // Só é ignorada se o arquivo tiver sido explicitamente incluído.
            if (!explicitlyIncluded && IsIgnoredByDefault(relative))
                return false;

            // -------------------------------------------------
            // Modo 1: seleção explícita por arquivo
            // -------------------------------------------------
            if (_explicitFileSelection)
            {
                if (_includeSet is { Count: > 0 } && !explicitlyIncluded)
                    return false;

                if (explicitlyExcluded)
                    return false;

                return true;
            }

            // -------------------------------------------------
            // Modo 2: escopo tradicional
            // -------------------------------------------------
            if (explicitlyExcluded)
                return false;

            return _scope.IsInScope(_rootPath, absolutePath);
        }

        private static bool ContainsExplicitFilePath(IEnumerable<string>? values)
        {
            if (values == null)
                return false;

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var normalized = Normalize(value);

                if (normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Contains("/") ||
                    normalized.Contains("\\") ||
                    Path.IsPathRooted(value))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<string>? NormalizeSet(IEnumerable<string>? values)
        {
            if (values == null)
                return null;

            var normalized = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(Normalize)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return normalized.Count == 0 ? null : normalized;
        }

        private static bool IsIgnoredByDefault(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var parts = Normalize(relativePath)
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (IsIgnoredDirectoryName(part))
                    return true;

                if (LooksLikeAuxiliaryProject(part))
                    return true;
            }

            return false;
        }

        private static bool IsIgnoredDirectoryName(string name)
        {
            return name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".vs", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".idea", StringComparison.OrdinalIgnoreCase)
                || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                || name.Equals("packages", StringComparison.OrdinalIgnoreCase)
                || name.Equals("TestResults", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Docs", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Doc", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Documentation", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".github", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeAuxiliaryProject(string name)
        {
            return name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Benchmarks", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Benchmark", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Samples", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Sample", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Examples", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Example", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string path)
            => path.Replace('\\', '/').Trim();
    }
}
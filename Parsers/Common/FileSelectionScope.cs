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
            var absolute = Normalize(Path.GetFullPath(filePath));
            var relative = Normalize(Path.GetRelativePath(_rootPath, filePath));

            // -------------------------------------------------
            // Modo 1: seleção explícita por arquivo
            // -------------------------------------------------
            if (_explicitFileSelection)
            {
                if (_includeSet is { Count: > 0 })
                {
                    if (!_includeSet.Contains(absolute) &&
                        !_includeSet.Contains(relative))
                    {
                        return false;
                    }
                }

                if (_excludeSet is { Count: > 0 })
                {
                    if (_excludeSet.Contains(absolute) ||
                        _excludeSet.Contains(relative))
                    {
                        return false;
                    }
                }

                return true;
            }

            // -------------------------------------------------
            // Modo 2: escopo tradicional
            // -------------------------------------------------
            return _scope.IsInScope(_rootPath, filePath);
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
                .Select(v =>
                {
                    var fullPath =
                        Path.IsPathRooted(v)
                            ? v
                            : v;

                    return Normalize(fullPath);
                })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return normalized.Count == 0 ? null : normalized;
        }

        private static string Normalize(string path)
            => path.Replace('\\', '/').Trim();
    }
}
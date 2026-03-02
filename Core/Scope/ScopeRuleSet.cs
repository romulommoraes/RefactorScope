namespace RefactorScope.Core.Scope
{
    /// <summary>
    /// Representa o conjunto de regras de escopo determinístico.
    /// Responsável por decidir se um arquivo pertence ao domínio de análise.
    ///
    /// Regras:
    /// - Comparação case-insensitive
    /// - Separadores normalizados
    /// - Suporte a pasta e arquivo
    /// - Precedência: Exclude > Include
    /// </summary>
    public class ScopeRuleSet
    {
        private readonly List<string> _includes;
        private readonly List<string> _excludes;

        public ScopeRuleSet(
            IEnumerable<string>? includes,
            IEnumerable<string>? excludes)
        {
            _includes = Normalize(includes);
            _excludes = Normalize(excludes);
        }

        /// <summary>
        /// Determina se um caminho pertence ao escopo.
        /// </summary>
        public bool IsInScope(string rootPath, string fullPath)
        {
            var relative = NormalizePath(
                Path.GetRelativePath(rootPath, fullPath));

            if (MatchesAny(relative, _excludes))
                return false;

            if (_includes.Count == 0)
                return true;

            return MatchesAny(relative, _includes);
        }

        // =============================
        // Helpers
        // =============================

        private static List<string> Normalize(IEnumerable<string>? rules)
        {
            if (rules == null)
                return new List<string>();

            return rules
                .Select(NormalizePath)
                .ToList();
        }

        private static string NormalizePath(string path)
        {
            return path
                .Replace("\\", "/")
                .Trim()
                .TrimStart('/')
                .ToLowerInvariant();
        }

        private static bool MatchesAny(string relative, List<string> rules)
        {
            foreach (var rule in rules)
            {
                if (IsFolderRule(rule))
                {
                    if (relative.StartsWith(rule))
                        return true;
                }
                else
                {
                    if (relative.Equals(rule))
                        return true;
                }
            }

            return false;
        }

        private static bool IsFolderRule(string rule)
        {
            return !rule.Contains(".");
        }
    }
}
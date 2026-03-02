namespace RefactorScope.Core.Scope
{
    /// <summary>
    /// Responsável por descobrir arquivos dentro do escopo de análise.
    /// Separa descoberta de arquivos da lógica de parsing.
    /// </summary>
    public class ScopeFilteredFileDiscovery
    {
        private readonly ScopeRuleSet _rules;

        public ScopeFilteredFileDiscovery(ScopeRuleSet rules)
        {
            _rules = rules;
        }

        public List<string> Discover(string rootPath)
        {
            return Directory
                .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => _rules.IsInScope(rootPath, f))
                .ToList();
        }
    }
}
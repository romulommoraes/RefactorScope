using RefactorScope.Core.Model;

namespace RefactorScope.Core.Patterns
{
    public static class DesignPatternSignatureLibrary
    {
        private static readonly IReadOnlyList<PatternRule> _rules =
            new List<PatternRule>
            {
                new("Strategy", t => t.Name.EndsWith("Strategy")),
                new("Resolver", t => t.Name.EndsWith("Resolver")),
                new("Factory", t => t.Name.EndsWith("Factory")),
                new("Repository", t => t.Name.EndsWith("Repository")),
                new("Service", t => t.Name.EndsWith("Service")),
                new("Analyzer", t => t.Name.EndsWith("Analyzer")),
                new("Exporter", t => t.Name.EndsWith("Exporter")),
                new("Rule", t => t.Name.EndsWith("Rule")),
                new("Controller", t => t.Name.EndsWith("Controller")),
                new("Middleware", t => t.Name.EndsWith("Middleware"))

            };

        public static PatternSignatureResult Evaluate(TipoInfo tipo)
        {
            foreach (var rule in _rules)
            {
                if (rule.Match(tipo))
                    return PatternSignatureResult.Match(rule.PatternName);
            }

            return PatternSignatureResult.None();
        }
    }

    internal sealed record PatternRule(
        string PatternName,
        Func<TipoInfo, bool> Match
    );

    public sealed record PatternSignatureResult(
        bool IsMatch,
        string? PatternName)
    {
        public static PatternSignatureResult Match(string name)
            => new(true, name);

        public static PatternSignatureResult None()
            => new(false, null);
    }
}
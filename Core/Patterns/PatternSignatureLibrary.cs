using RefactorScope.Core.Model;

namespace RefactorScope.Core.Patterns
{
    /// <summary>
    /// Biblioteca central de reconhecimento de padrões estruturais
    /// baseada em assinaturas nominais e heurísticas leves.
    ///
    /// Objetivo:
    /// reduzir falsos positivos de "Unresolved"
    /// identificando padrões arquiteturais comuns.
    /// </summary>
    public static class DesignPatternSignatureLibrary
    {
        private static readonly IReadOnlyList<PatternRule> _rules =
            new List<PatternRule>
            {
                // ==============================
                // DESIGN PATTERNS
                // ==============================

                new("Strategy", t => t.Name.EndsWith("Strategy")),
                new("Factory", t => t.Name.EndsWith("Factory")),
                new("Builder", t => t.Name.EndsWith("Builder")),
                new("Adapter", t => t.Name.EndsWith("Adapter")),

                // ==============================
                // ARQUITETURA / INFRA
                // ==============================

                new("Repository", t => t.Name.EndsWith("Repository")),
                new("Service", t => t.Name.EndsWith("Service")),
                new("Provider", t => t.Name.EndsWith("Provider")),
                new("Manager", t => t.Name.EndsWith("Manager")),
                new("Dispatcher", t => t.Name.EndsWith("Dispatcher")),
                new("Handler", t => t.Name.EndsWith("Handler")),
                new("Mapper", t => t.Name.EndsWith("Mapper")),
                new("Resolver", t => t.Name.EndsWith("Resolver")),
                new("Registry", t => t.Name.EndsWith("Registry")),
                new("ServiceLocator", t => t.Name.EndsWith("ServiceLocator")),

                // ==============================
                // FRAMEWORK / TOOLING
                // ==============================

                new("Analyzer", t => t.Name.EndsWith("Analyzer")),
                new("Exporter", t => t.Name.EndsWith("Exporter")),
                new("Controller", t => t.Name.EndsWith("Controller")),
                new("Middleware", t => t.Name.EndsWith("Middleware")),
                new("Rule", t => t.Name.EndsWith("Rule")),

                // ==============================
                // INFRASTRUCTURE UTILITIES
                // ==============================

                new("Validator", t => t.Name.EndsWith("Validator")),
                new("Selector", t => t.Name.EndsWith("Selector")),
                new("Loader", t => t.Name.EndsWith("Loader")),
                new("Consolidator", t => t.Name.EndsWith("Consolidator")),
                new("Renderer", t => t.Name.EndsWith("Renderer")),

                // ==============================
                // DATA STRUCTURES
                // ==============================

                new("DTO", t => t.Name.EndsWith("Dto")),
                new("Converter", t => t.Name.EndsWith("Converter")),
                new("Options", t => t.Name.EndsWith("Options")),
                new("Configuration", t => t.Name.EndsWith("Configuration")),

                // ==============================
                // MESSAGING / CQRS
                // ==============================

                new("Event", t => t.Name.EndsWith("Event")),
                new("Command", t => t.Name.EndsWith("Command")),
                new("Query", t => t.Name.EndsWith("Query")),
                new("Request", t => t.Name.EndsWith("Request")),
                new("Response", t => t.Name.EndsWith("Response")),

                // ==============================
                // ERROR TYPES
                // ==============================

                new("Exception", t => t.Name.EndsWith("Exception"))
            };


        public static PatternSignatureResult Evaluate(TipoInfo tipo)
        {
            // 1️⃣ Nome
            foreach (var rule in _rules)
            {
                if (rule.Match(tipo))
                    return PatternSignatureResult.Match(rule.PatternName);
            }

            // 2️⃣ Heurísticas estruturais
            var heuristic = StructuralHeuristicLibrary.Evaluate(tipo);
            if (heuristic.IsMatch)
                return heuristic;

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
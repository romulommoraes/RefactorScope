using RefactorScope.Core.Model;
using RefactorScope.CORE.Context;

namespace RefactorScope.Infrastructure
{
    public static class LayerRuleEvaluator
    {
        public static string ResolveLayer(
            TipoInfo tipo,
            Dictionary<string, LayerRuleConfig>? rules
        )
        {
            if (rules == null)
                return "Aplicação";

            foreach (var layer in rules)
            {
                var rule = layer.Value;

                if (rule.NameEquals?.Any(x => x == tipo.Name) == true)
                    return layer.Key;

                if (rule.NameStartsWith?.Any(x => tipo.Name.StartsWith(x)) == true)
                    return layer.Key;

                if (rule.NamespaceContains?.Any(x => tipo.Namespace.Contains(x)) == true)
                    return layer.Key;
            }

            return "Aplicação";
        }
    }
}
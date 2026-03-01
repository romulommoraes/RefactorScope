using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;

namespace RefactorScope.Infrastructure
{
    /// <summary>
    /// Valida a consistência da configuração do RefactorScope.
    /// </summary>
    public static class ConfigValidator
    {
        public static void Validate(RefactorScopeConfig config)
        {
            if (config.LayerRules == null || !config.LayerRules.Any())
                throw new Exception("LayerRules não definidos no refactorscope.json");

            foreach (var layer in config.LayerRules)
            {
                var rule = layer.Value;

                if (rule == null)
                    throw new Exception($"Layer '{layer.Key}' possui configuração nula.");

                var hasRules =
                    (rule.NameStartsWith?.Any() ?? false) ||
                    (rule.NamespaceContains?.Any() ?? false) ||
                    (rule.NameEquals?.Any() ?? false);

                if (!hasRules)
                    throw new Exception($"Layer '{layer.Key}' não possui nenhuma regra válida.");
            }
        }
    }
}
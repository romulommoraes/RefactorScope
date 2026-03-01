namespace RefactorScope.Core.Context
{
    public class LayerRuleConfig
    {
        public List<string>? NameStartsWith { get; set; }
        public List<string>? NamespaceContains { get; set; }
        public List<string>? NameEquals { get; set; }
    }
}
namespace RefactorScope.CORE.Context
{
    public class LayerRuleConfig
    {
        public List<string>? NameStartsWith { get; set; }
        public List<string>? NamespaceContains { get; set; }
        public List<string>? NameEquals { get; set; }
    }
}
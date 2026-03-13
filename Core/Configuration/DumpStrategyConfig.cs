namespace RefactorScope.Core.Configuration
{
    public class DumpStrategyConfig
    {
        public string Mode { get; set; } = "global";
        public string SplitBy { get; set; } = "layer";
    }
}
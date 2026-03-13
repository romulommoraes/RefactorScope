namespace RefactorScope.Exporters.Projections.Architecture
{
    public sealed class ModuleRouteNode
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;

        // bootstrap, entry, process, station, hub, support, exit
        public string Kind { get; init; } = "process";

        public string Subtitle { get; init; } = "PROCESSING";

        public double Pressure { get; init; }
        public int InDegree { get; set; }
        public int OutDegree { get; set; }
        public int WeightedIn { get; set; }
        public int WeightedOut { get; set; }

        public double Traffic => WeightedIn + WeightedOut;
        public double HubScore { get; set; }

        public bool IsEntry { get; init; }
        public bool IsHub { get; init; }
        public bool IsExit { get; init; }
    }
}
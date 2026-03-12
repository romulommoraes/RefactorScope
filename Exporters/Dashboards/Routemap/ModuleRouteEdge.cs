namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    public sealed class ModuleRouteEdge
    {
        public string From { get; init; } = string.Empty;
        public string To { get; init; } = string.Empty;
        public string Type { get; init; } = "uni"; // uni, bi, uniGhost
        public int Weight { get; init; }
        public double Traffic { get; init; }
    }
}
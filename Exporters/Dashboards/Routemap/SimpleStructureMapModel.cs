namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    public sealed class SimpleStructureMapModel
    {
        public IReadOnlyList<SimpleStructureNode> Nodes { get; }

        public SimpleStructureMapModel(IReadOnlyList<SimpleStructureNode> nodes)
        {
            Nodes = nodes;
        }
    }

    public sealed class SimpleStructureNode
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string ParentId { get; init; } = string.Empty;
        public int Depth { get; init; }
        public int CsFileCount { get; init; }
    }
}
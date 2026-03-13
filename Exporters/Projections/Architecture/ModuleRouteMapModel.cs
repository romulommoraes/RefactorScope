using System.Collections.Generic;

namespace RefactorScope.Exporters.Projections.Architecture
{
    public sealed class ModuleRouteMapModel
    {
        public IReadOnlyList<ModuleRouteNode> Nodes { get; }
        public IReadOnlyList<ModuleRouteEdge> Edges { get; }

        public ModuleRouteMapModel(
            IReadOnlyList<ModuleRouteNode> nodes,
            IReadOnlyList<ModuleRouteEdge> edges)
        {
            Nodes = nodes;
            Edges = edges;
        }
    }
}
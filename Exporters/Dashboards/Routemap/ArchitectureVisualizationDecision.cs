namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    public sealed class ArchitectureVisualizationDecision
    {
        public ArchitectureVisualizationSource Source { get; init; }

        /// <summary>
        /// Só faz sentido quando Source == Inferred.
        /// </summary>
        public ArchitectureVisualizationMode? Mode { get; init; }

        public static ArchitectureVisualizationDecision CuratedRefactorScope()
        {
            return new ArchitectureVisualizationDecision
            {
                Source = ArchitectureVisualizationSource.CuratedRefactorScope,
                Mode = null
            };
        }

        public static ArchitectureVisualizationDecision Inferred(ArchitectureVisualizationMode mode)
        {
            return new ArchitectureVisualizationDecision
            {
                Source = ArchitectureVisualizationSource.Inferred,
                Mode = mode
            };
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;

public class SegmentedDumpStrategy : IDumpStrategy
{
    private readonly ISegmentationResolver _resolver;

    public SegmentedDumpStrategy(ISegmentationResolver resolver)
    {
        _resolver = resolver;
    }

    public void Execute(
        AnalysisContext context,
        ConsolidatedReport report,
        IEnumerable<IExporter> exporters)
    {
        var segments = _resolver.Resolve(context);

        foreach (var segment in segments)
        {
            var path = Path.Combine(
                context.Config.OutputPath,
                segment.Name
            );

            Directory.CreateDirectory(path);

            foreach (var exporter in exporters)
            {
                exporter.Export(
                    segment.Context,
                    report,
                    path
                );
            }
        }
    }
}
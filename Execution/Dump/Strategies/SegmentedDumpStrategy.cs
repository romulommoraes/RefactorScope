using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Execution.Dump.Segmentation;

namespace RefactorScope.Execution.Dump.Strategies
{
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
                foreach (var exporter in exporters)
                {
                    exporter.Export(
                        segment.Context,
                        report,
                        context.Config.OutputPath);
                }
            }
        }
    }
}
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Execution.Dump.Strategies
{
    public class GlobalDumpStrategy : IDumpStrategy
    {
        public void Execute(
            AnalysisContext context,
            ConsolidatedReport report,
            IEnumerable<IExporter> exporters)
        {
            var output = context.Config.OutputPath;
            Directory.CreateDirectory(output);

            foreach (var exporter in exporters)
            {
                exporter.Export(context, report, output);
            }
        }
    }
}

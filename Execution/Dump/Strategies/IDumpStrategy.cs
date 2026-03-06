using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
namespace RefactorScope.Execution.Dump.Strategies
{
    public interface IDumpStrategy
    {
        void Execute(
            AnalysisContext context,
            ConsolidatedReport report,
            IEnumerable<IExporter> exporters
        );
    }
}
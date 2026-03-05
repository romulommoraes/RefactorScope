using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

public interface IDumpStrategy
{
    void Execute(
        AnalysisContext context,
        ConsolidatedReport report,
        IEnumerable<IExporter> exporters
    );
}
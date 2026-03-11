using RefactorScope.Core.Parsing;

namespace RefactorScope.Core.Reporting
{
    public static class ConsolidatedReportSnapshotExtensions
    {
        public static ReportSnapshot ToSnapshot(
            this RefactorScope.Core.Results.ConsolidatedReport report,
            ParserResult? parserResult = null)
        {
            return ReportSnapshotBuilder.Build(report, parserResult);
        }
    }
}
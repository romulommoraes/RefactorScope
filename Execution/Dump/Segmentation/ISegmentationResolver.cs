using RefactorScope.Core.Context;
namespace RefactorScope.Execution.Dump.Segmentation
{
        public interface ISegmentationResolver
    {
        IEnumerable<SegmentScope> Resolve(AnalysisContext context);
    }
}
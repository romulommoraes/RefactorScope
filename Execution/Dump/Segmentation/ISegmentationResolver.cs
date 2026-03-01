using RefactorScope.Core.Context;

public interface ISegmentationResolver
{
    IEnumerable<SegmentScope> Resolve(AnalysisContext context);
}
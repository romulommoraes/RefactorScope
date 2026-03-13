using RefactorScope.Core.Context;
namespace RefactorScope.Execution.Dump.Segmentation
{
    public class SegmentScope
    {
        public string Name { get; }
        public AnalysisContext Context { get; }

        public SegmentScope(string name, AnalysisContext context)
        {
            Name = name;
            Context = context;
        }
    }
}
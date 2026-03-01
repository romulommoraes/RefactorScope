using RefactorScope.Core.Context;

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
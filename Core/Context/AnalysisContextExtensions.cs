using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Context
{
    public static class AnalysisContextExtensions
    {
        public static T? GetResult<T>(this AnalysisContext context)
            where T : class, IAnalysisResult
        {
            return context.Results
                .OfType<T>()
                .FirstOrDefault();
        }
    }
}
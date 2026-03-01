using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado do cálculo de acoplamento estrutural.
    /// </summary>
    public class CouplingResult : IAnalysisResult
    {
        public IReadOnlyDictionary<string, int> ModuleFanOut { get; }

        public IReadOnlyDictionary<string, Dictionary<string, int>> TypeFanOutByModule { get; }

        public CouplingResult(
            Dictionary<string, int> moduleFanOut,
            Dictionary<string, Dictionary<string, int>> typeFanOutByModule)
        {
            ModuleFanOut = moduleFanOut;
            TypeFanOutByModule = typeFanOutByModule;
        }
    }
}
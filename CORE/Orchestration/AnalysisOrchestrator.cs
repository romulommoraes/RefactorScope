using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Responsável por executar os analisadores habilitados
    /// e consolidar seus resultados.
    /// </summary>
    public class AnalysisOrchestrator
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;

        public AnalysisOrchestrator(IEnumerable<IAnalyzer> analyzers)
        {
            _analyzers = analyzers;
        }

        /// <summary>
        /// Executa os analisadores ativos conforme configuração.
        /// </summary>
        public ConsolidatedReport Execute(AnalysisContext context)
        {
            var activeResults = new List<IAnalysisResult>();

            foreach (var analyzer in _analyzers)
            {
                if (!IsAnalyzerEnabled(context, analyzer))
                    continue;

                var result = analyzer.Analyze(context);
                activeResults.Add(result);
            }

            return new ConsolidatedReport(
                activeResults,
                context.ExecutionTime,
                context.Config.RootPath);
        }

        private bool IsAnalyzerEnabled(AnalysisContext context, IAnalyzer analyzer)
        {
            if (context.Config.Analyzers == null)
                return true;

            if (!context.Config.Analyzers.TryGetValue(analyzer.Name, out var enabled))
                return true;

            return enabled;
        }
    }
}
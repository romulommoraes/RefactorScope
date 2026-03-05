using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Responsável por executar os analisadores habilitados
    /// e consolidar seus resultados.
    ///
    /// 🔒 Importante:
    /// O threshold probabilístico de zombie passa a fazer parte
    /// do estado consolidado da análise (ConsolidatedReport).
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
        /// Atualiza o contexto incrementalmente para suportar
        /// dependência entre analisadores (two-pass model).
        /// </summary>
        public ConsolidatedReport Execute(AnalysisContext context)
        {
            var activeResults = new List<IAnalysisResult>();

            foreach (var analyzer in _analyzers)
            {
                if (!IsAnalyzerEnabled(context, analyzer))
                    continue;

                var result = analyzer.Analyze(context);

                // 🔥 Atualiza contexto incrementalmente
                activeResults.Add(result);
                context.Results = activeResults;
            }

            // 🔥 Threshold oficial da análise
            var zombieThreshold =
                context.Config.StructuralCandidateDetection.MinUnresolvedProbabilityThreshold;

            return new ConsolidatedReport(
                activeResults,
                context.ExecutionTime,
                context.Config.RootPath,
                zombieThreshold
            );
        }

        /// <summary>
        /// Verifica se o analisador está habilitado na configuração.
        /// Se não houver configuração explícita, assume habilitado.
        /// </summary>
        private bool IsAnalyzerEnabled(
            AnalysisContext context,
            IAnalyzer analyzer)
        {
            if (context.Config.Analyzers == null)
                return true;

            if (!context.Config.Analyzers
                .TryGetValue(analyzer.Name, out var enabled))
                return true;

            return enabled;
        }
    }
}
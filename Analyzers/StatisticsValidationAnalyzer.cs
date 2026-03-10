using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Infrastructure;
using RefactorScope.Statistics.Engines;
using RefactorScope.Statistics.Models;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Adapter do módulo estatístico para o pipeline principal de análise.
    ///
    /// Responsabilidade
    /// ----------------
    /// Integrar o ValidationEngine, que hoje existe como motor estático,
    /// ao contrato esperado pelo AnalysisOrchestrator (IAnalyzer).
    ///
    /// Regras
    /// ------
    /// - nunca lança exceção para o pipeline principal
    /// - respeita o opt-in / opt-out via config
    /// - transforma StatisticsReport em StatisticsResult
    ///
    /// Observação arquitetural
    /// -----------------------
    /// O ValidationEngine permanece como motor observacional puro,
    /// enquanto esta classe funciona como adaptador de orquestração.
    /// </summary>
    public sealed class StatisticsValidationAnalyzer : IAnalyzer
    {
        public string Name => "statistics";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var partialReport = new ConsolidatedReport(
                context.Results.ToList(),
                DateTime.UtcNow,
                context.Config.RootPath,
                context.Config.StructuralCandidateDetection.MinUnresolvedProbabilityThreshold
            );

            var statisticsReport = ValidationEngine.RunSafely(
                context,
                partialReport,
                (ex, stage) => CrashLogger.Log(ex, stage));

            if (statisticsReport == null)
            {
                return new StatisticsResult(
                    new StatisticsReport(
                        new ParsingConfidence(0, 0),
                        new MetricsStatisticsSummary(0, 0, 0)));
            }

            return new StatisticsResult(statisticsReport);
        }
    }
}
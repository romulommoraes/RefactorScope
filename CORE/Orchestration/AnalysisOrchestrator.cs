using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Estimation;
using RefactorScope.Estimation.Models;
using RefactorScope.Statistics.Models;
using RefactorScope.Statistics.Engines;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Orquestra a execução das etapas de análise do RefactorScope.
    ///
    /// Este fluxo substitui a antiga PipelineStep architecture
    /// mantendo as mesmas fases conceituais:
    ///
    /// 1) AnalyzerStep
    /// 2) StatisticsStep
    /// 3) EstimationStep
    ///
    /// Cada etapa está isolada em um método.
    /// Caso uma pipeline formal seja necessária no futuro,
    /// basta extrair cada método como um Step.
    /// </summary>
    public class AnalysisOrchestrator
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;

        public AnalysisOrchestrator(IEnumerable<IAnalyzer> analyzers)
        {
            _analyzers = analyzers;
        }

        public ConsolidatedReport Execute(AnalysisContext context)
        {
            var results = new List<IAnalysisResult>();

            //------------------------------------------
            // STEP 1 — AnalyzerStep
            //------------------------------------------

            RunAnalyzers(context, results);

            //------------------------------------------
            // STEP 2 — Build consolidated report
            //------------------------------------------

            var report = BuildReport(context, results);

            //------------------------------------------
            // STEP 3 — StatisticsStep
            //------------------------------------------

            RunStatistics(context, ref report, results);

            //------------------------------------------
            // STEP 4 — EstimationStep
            //------------------------------------------

            RunEffortEstimation(context, report, results);

            return report;
        }

        // =====================================================
        // AnalyzerStep
        // =====================================================

        private void RunAnalyzers(
            AnalysisContext context,
            List<IAnalysisResult> results)
        {
            foreach (var analyzer in _analyzers)
            {
                if (!IsAnalyzerEnabled(context, analyzer))
                    continue;

                try
                {
                    var result = analyzer.Analyze(context);

                    results.Add(result);

                    // Atualiza contexto incremental
                    context.Results = results;
                }
                catch (Exception ex)
                {
                    Infrastructure.CrashLogger.Log(
                        ex,
                        $"ANALYZER_{analyzer.Name}");
                }
            }
        }

        // =====================================================
        // Build Report
        // =====================================================

        private ConsolidatedReport BuildReport(
            AnalysisContext context,
            List<IAnalysisResult> results)
        {
            var zombieThreshold =
                context.Config.StructuralCandidateDetection
                    .MinUnresolvedProbabilityThreshold;

            return new ConsolidatedReport(
                results,
                context.ExecutionTime,
                context.Config.RootPath,
                zombieThreshold
            );
        }

        // =====================================================
        // StatisticsStep
        // =====================================================

        private void RunStatistics(
            AnalysisContext context,
            ref ConsolidatedReport report,
            List<IAnalysisResult> results)
        {
            if (context.Config.Statistics?.Enabled != true)
                return;

            try
            {
                var statsReport = ValidationEngine.RunSafely(
                    context,
                    report,
                    (ex, phase) => Infrastructure.CrashLogger.Log(ex, phase)
                );

                if (statsReport == null)
                    return;

                results.Add(statsReport);

                context.Results = results;

                report = new ConsolidatedReport(
                    results,
                    report.ExecutionTime,
                    report.TargetScope,
                    report.UnresolvedProbabilityThreshold
                );
            }
            catch (Exception ex)
            {
                Infrastructure.CrashLogger.Log(ex, "STATISTICS_STEP");
            }
        }

        // =====================================================
        // EstimationStep
        // =====================================================

        private void RunEffortEstimation(
            AnalysisContext context,
            ConsolidatedReport report,
            List<IAnalysisResult> results)
        {
            if (context.Config.Estimator?.Enabled != true)
                return;

            try
            {
                var estimate = EffortEstimator.Estimate(context, report);

                var estimateResult = new EffortEstimateResult(estimate);

                results.Add(estimateResult);

                context.Results = results;
            }
            catch (Exception ex)
            {
                Infrastructure.CrashLogger.Log(ex, "ESTIMATION_STEP");
            }
        }

        // =====================================================
        // Helper
        // =====================================================

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
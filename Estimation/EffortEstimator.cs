using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Core.Metrics;
using RefactorScope.Estimation.Models;
using RefactorScope.Estimation.Scoring;
using RefactorScope.Statistics.Models;

namespace RefactorScope.Estimation
{
    /// <summary>
    /// Calcula a estimativa final de esforço de refatoração.
    ///
    /// O cálculo utiliza o Refactor Difficulty Index (RDI),
    /// baseado exclusivamente em métricas arquiteturais derivadas
    /// do modelo estrutural do sistema.
    ///
    /// O módulo Statistics é opcional e utilizado apenas para
    /// enriquecer o cálculo de confiança da estimativa.
    /// </summary>
    public static class EffortEstimator
    {
        public static EffortEstimate Estimate(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            // ------------------------------------------------
            // 1. Construir métricas arquiteturais
            // ------------------------------------------------

            var metrics =
                ArchitecturalMetricsBuilder.Build(context, report);

            // ------------------------------------------------
            // 2. Calcular RDI
            // ------------------------------------------------

            var rdi =
                RDICalculator.Calculate(context, metrics);

            int total = rdi.Total;

            // ------------------------------------------------
            // 3. Converter RDI em horas estimadas
            // ------------------------------------------------

            double estimatedHours = total * 0.4;

            string difficulty =
                total switch
                {
                    < 20 => "Trivial",
                    < 40 => "Low",
                    < 60 => "Moderate",
                    < 80 => "Hard",
                    _ => "Critical"
                };

            // ------------------------------------------------
            // 4. Confiança da estimativa (opcional)
            // ------------------------------------------------

            double confidence = 0.7; // valor padrão

            var stats = report.GetResult<StatisticsReport>();

            if (stats != null)
            {
                confidence =
                    Math.Clamp(
                        stats.Confidence.ClassesPerFile * 0.3 +
                        stats.Confidence.ReferencesPerClass * 0.1,
                        0.5,
                        0.9
                    );
            }

            return new EffortEstimate(
                total,
                difficulty,
                estimatedHours,
                confidence
            );
        }
    }
}
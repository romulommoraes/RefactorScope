// ==========================================
// ARQUIVO: ValidationEngine.cs
// CAMINHO: RefactorScope.Statistics\Engines\ValidationEngine.cs
// ==========================================
using System;
using System.Linq;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Statistics.Models;

namespace RefactorScope.Statistics.Engines
{
    /// <summary>
    /// Motor estatístico observacional.
    /// Processa os resultados para garantir a sanidade dos dados relatados.
    /// Executa de forma estritamente não-bloqueante (safely run).
    /// </summary>
    public static class ValidationEngine
    {
        /// <summary>
        /// Extrai métricas de saúde da análise atual. Nunca lança exceções.
        /// </summary>
        /// <param name="context">Contexto base contendo o Modelo Estrutural</param>
        /// <param name="report">Relatório consolidado contendo resultados dos analyzers</param>
        /// <param name="crashLogger">Action opcional para reportar erros internos sem abortar a execução</param>
        /// <returns>O relatório estatístico ou null se estiver desabilitado ou falhar.</returns>
        public static StatisticsReport? RunSafely(
            AnalysisContext context,
            ConsolidatedReport report,
            Action<Exception, string>? crashLogger = null)
        {
            // Respeita o contrato de opt-out (Opcional)
            if (context.Config.Statistics == null || !context.Config.Statistics.Enabled)
                return null;

            try
            {
                var model = context.Model;

                // Proteção matemática contra divisão por zero (Proteção Matemática)
                var totalFiles = Math.Max(model.Arquivos.Count, 1);
                var totalTypes = Math.Max(model.Tipos.Count, 1);

                // -------------------------------------------------
                // 1. Confiança de Parsing (Parsing Confidence)
                // -------------------------------------------------
                double classesPerFile = (double)model.Tipos.Count / totalFiles;
                double refsPerClass = (double)model.Referencias.Count / totalTypes;

                var confidence = new ParsingConfidence(classesPerFile, refsPerClass);

                // -------------------------------------------------
                // 2. Resumo das Métricas (Metrics Summary)
                // -------------------------------------------------
                var couplingResult = report.GetResult<CouplingResult>();
                double meanCoupling = couplingResult?.FanOutTotalByType.Values.DefaultIfEmpty(0).Average() ?? 0;

                // Consulta a Fonte Única de Verdade do relatório (Zumbis)
                var unresolvedCandidates = report.GetEffectiveUnresolvedCandidates();
                double unresolvedRatio = (double)unresolvedCandidates.Count / totalTypes;

                // Drift ratio
                var archResult = report.GetResult<ArchitecturalClassificationResult>();
                int driftCount = archResult?.Items.Count(i => i.StructuralStatus == "DriftLogico") ?? 0;
                double driftRatio = (double)driftCount / totalTypes;

                var summary = new MetricsStatisticsSummary(meanCoupling, unresolvedRatio, driftRatio);

                return new StatisticsReport(confidence, summary);
            }
            catch (Exception ex)
            {
                // Execução estritamente não-bloqueante. Falhas estatísticas não abortam CLI.
                crashLogger?.Invoke(ex, "STATISTICS_ENGINE");
                return null;
            }
        }
    }
}
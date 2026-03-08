using System;
using System.Linq;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Metrics
{
    /// <summary>
    /// Constrói métricas arquiteturais agregadas a partir do
    /// modelo estrutural e dos resultados dos analyzers.
    ///
    /// Essas métricas representam sinais estruturais consolidados
    /// utilizados por módulos de interpretação arquitetural
    /// como Estimation (RDI).
    ///
    /// Importante:
    /// • Não depende do módulo Statistics.
    /// • Opera apenas sobre Core + Results.
    ///
    /// Métricas calculadas:
    ///
    /// • MeanCoupling
    ///     Média de fan-out estrutural entre tipos, ajustada
    ///     semanticamente para ignorar módulos absolvidos
    ///     (tooling, composition root, infraestrutura).
    ///
    /// • UnresolvedCandidateRatio
    ///     Proporção de tipos classificados como unresolved
    ///     após refinamento estrutural.
    ///
    /// • NamespaceDriftRatio
    ///     Proporção de tipos classificados como drift lógico
    ///     pela análise arquitetural.
    /// </summary>
    public static class ArchitecturalMetricsBuilder
    {
        public static ArchitecturalMetrics Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var model = context.Model;

            int totalTypes = Math.Max(model.Tipos.Count, 1);

            double meanCoupling = ComputeEffectiveCoupling(report);

            var unresolved = report.GetEffectiveUnresolvedCandidates();

            double unresolvedRatio =
                (double)unresolved.Count / totalTypes;

            var arch =
                report.GetResult<ArchitecturalClassificationResult>();

            int driftCount =
                arch?.Items.Count(i => i.StructuralStatus == "DriftLogico") ?? 0;

            double driftRatio =
                (double)driftCount / totalTypes;

            return new ArchitecturalMetrics(
                meanCoupling,
                unresolvedRatio,
                driftRatio
            );
        }

        /// <summary>
        /// Calcula o coupling efetivo ignorando módulos cuja
        /// natureza estrutural legitima alto fan-out.
        /// </summary>
        private static double ComputeEffectiveCoupling(
            ConsolidatedReport report)
        {
            var coupling =
                report.GetResult<CouplingResult>();

            if (coupling == null)
                return 0;

            var architectural =
                report.GetResult<ArchitecturalClassificationResult>();

            if (architectural == null)
            {
                return coupling.FanOutTotalByType
                    .Values
                    .DefaultIfEmpty(0)
                    .Average();
            }

            var filtered =
                coupling.FanOutTotalByType
                    .Where(kv =>
                    {
                        var module =
                            architectural.Items
                                .FirstOrDefault(i => i.TypeName == kv.Key)
                                ?.Folder;

                        if (string.IsNullOrWhiteSpace(module))
                            return true;

                        if (ArchitecturalScoreCalculator.IsToolingModule(module))
                            return false;

                        if (ArchitecturalScoreCalculator.IsInfrastructure(module))
                            return false;

                        if (ArchitecturalScoreCalculator.IsCompositionRoot(module))
                            return false;

                        return true;
                    })
                    .Select(kv => kv.Value)
                    .DefaultIfEmpty(0);

            return filtered.Average();
        }
    }
}
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset de contribuição por módulo.
    ///
    /// Mostra quanto cada módulo contribui para uma métrica
    /// dentro do escopo analisado.
    ///
    /// Preparado para Coupling.
    /// </summary>
    public class ModuleContributionDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_module_contribution";

        public string[] Headers => new[]
        {
            "Metric",
            "Module",
            "AbsoluteValue",
            "ContributionWithinScope"
        };

        public IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report)
        {
            var coupling = report.Results
                .OfType<CouplingResult>()
                .FirstOrDefault();

            if (coupling == null)
                yield break;

            var total = coupling.ModuleFanOut.Values.Sum();

            if (total == 0)
                yield break;

            foreach (var kvp in coupling.ModuleFanOut)
            {
                yield return new[]
                {
                    "Coupling",
                    kvp.Key,
                    kvp.Value.ToString(),
                    (kvp.Value / (double)total).ToString("0.00")
                };
            }
        }
    }
}
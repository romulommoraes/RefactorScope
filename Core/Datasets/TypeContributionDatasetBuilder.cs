using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Dataset de contribuição por tipo dentro do módulo.
    /// Ideal para drill-down de coupling.
    /// </summary>
    public class TypeContributionDatasetBuilder : IAnalyticalDatasetBuilder
    {
        public string DatasetName => "dataset_type_contribution";

        public string[] Headers => new[]
        {
            "Metric",
            "Module",
            "Type",
            "AbsoluteValue",
            "ContributionWithinModule"
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

            foreach (var module in coupling.TypeFanOutByModule)
            {
                var total = module.Value.Values.Sum();

                if (total == 0)
                    continue;

                foreach (var tipo in module.Value)
                {
                    yield return new[]
                    {
                        "Coupling",
                        module.Key,
                        tipo.Key,
                        tipo.Value.ToString(),
                        (tipo.Value / (double)total).ToString("0.00")
                    };
                }
            }
        }
    }
}
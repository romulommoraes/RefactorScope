using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using RefactorScope.Core.Datasets;
using System.Text;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exporta datasets analíticos em CSV.
    ///
    /// ✔ BI Ready (QuickSight / PowerBI)
    /// ✔ Excel Compatible (UTF8 BOM)
    /// ✔ Trend Persistence (append-only)
    /// ✔ Funciona com escopo parcial
    ///
    /// Comportamento:
    /// - Datasets normais → sobrescritos
    /// - Dataset estrutural de tendência → histórico incremental
    /// </summary>
    public class DatasetExporter : IExporter
    {
        public string Name => "datasets";

        private readonly IEnumerable<IAnalyticalDatasetBuilder> _builders;

        public DatasetExporter(IEnumerable<IAnalyticalDatasetBuilder> builders)
        {
            _builders = builders;
        }

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var datasetPath = Path.Combine(outputPath, "datasets");
            Directory.CreateDirectory(datasetPath);

            var trendPath = Path.Combine(datasetPath, "trend");
            Directory.CreateDirectory(trendPath);

            foreach (var builder in _builders)
            {
                if (builder.DatasetName == "dataset_structural_trend")
                {
                    AppendTrend(builder, context, report, trendPath);
                    continue;
                }

                ExportStandard(builder, context, report, datasetPath);
            }
        }

        // =====================================================
        // 🔹 EXPORTAÇÃO PADRÃO
        // =====================================================
        private void ExportStandard(
            IAnalyticalDatasetBuilder builder,
            AnalysisContext context,
            ConsolidatedReport report,
            string datasetPath)
        {
            var file = Path.Combine(datasetPath, $"{builder.DatasetName}.csv");

            using var writer = new StreamWriter(
                file,
                false,
                new UTF8Encoding(true) // BOM
            );

            writer.WriteLine(string.Join(",", builder.Headers));

            foreach (var row in builder.Build(context, report))
            {
                writer.WriteLine(string.Join(",", row));
            }
        }

        // =====================================================
        // 🔹 EXPORTAÇÃO DE TENDÊNCIA (APPEND)
        // =====================================================
        private void AppendTrend(
            IAnalyticalDatasetBuilder builder,
            AnalysisContext context,
            ConsolidatedReport report,
            string trendPath)
        {
            var file = Path.Combine(trendPath, "structural_history.csv");
            var fileExists = File.Exists(file);

            using var writer = new StreamWriter(
                file,
                true, // append
                new UTF8Encoding(true)
            );

            if (!fileExists)
                writer.WriteLine(string.Join(",", builder.Headers));

            foreach (var row in builder.Build(context, report))
            {
                writer.WriteLine(string.Join(",", row));
            }
        }
    }
}
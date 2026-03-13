using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Datasets;
using RefactorScope.Core.Results;
using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters.Datasets
{
    /// <summary>
    /// Exporta datasets analíticos em CSV.
    ///
    /// Convenção atual de saída
    /// ------------------------
    /// - datasets principais  -> output/datasets/csvs
    /// - datasets de tendência -> output/datasets/trend
    ///
    /// Objetivo
    /// --------
    /// Garantir que os artefatos tabulares usados por BI, auditoria
    /// e análises comparativas sejam sempre gravados em uma estrutura
    /// estável e previsível, independente do outputPath recebido pelo
    /// pipeline legado de IExporter.
    ///
    /// Observação arquitetural
    /// -----------------------
    /// Nesta etapa do projeto, a âncora real da exportação é
    /// context.Config.OutputPath.
    ///
    /// Isso evita que datasets sejam gravados em subpastas indevidas
    /// quando o outputPath for repassado por fluxos intermediários.
    /// </summary>
    public sealed class DatasetExporter : IExporter
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
            var rootOutputPath = context.Config.OutputPath;

            var datasetsRootPath = Path.Combine(rootOutputPath, "datasets");
            Directory.CreateDirectory(datasetsRootPath);

            var csvPath = Path.Combine(datasetsRootPath, "csvs");
            Directory.CreateDirectory(csvPath);

            var trendPath = Path.Combine(datasetsRootPath, "trend");
            Directory.CreateDirectory(trendPath);

            foreach (var builder in _builders)
            {
                if (builder.DatasetName == "dataset_structural_trend")
                {
                    AppendTrend(builder, context, report, trendPath);
                    continue;
                }

                ExportStandard(builder, context, report, csvPath);
            }
        }

        // =====================================================
        // EXPORTAÇÃO PADRÃO
        // =====================================================

        private void ExportStandard(
            IAnalyticalDatasetBuilder builder,
            AnalysisContext context,
            ConsolidatedReport report,
            string csvPath)
        {
            var file = Path.Combine(csvPath, $"{builder.DatasetName}.csv");

            using var writer = new StreamWriter(
                file,
                false,
                new UTF8Encoding(true));

            writer.WriteLine(string.Join(",", builder.Headers));

            foreach (var row in builder.Build(context, report))
            {
                writer.WriteLine(string.Join(",", row.Select(FormatValue)));
            }
        }

        // =====================================================
        // EXPORTAÇÃO DE TENDÊNCIA (APPEND)
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
                true,
                new UTF8Encoding(true));

            if (!fileExists)
                writer.WriteLine(string.Join(",", builder.Headers));

            foreach (var row in builder.Build(context, report))
            {
                writer.WriteLine(string.Join(",", row.Select(FormatValue)));
            }
        }

        // =====================================================
        // FORMATAÇÃO SEGURA PARA CSV
        // =====================================================

        private string FormatValue(object? value)
        {
            if (value == null)
                return "";

            return value switch
            {
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
        }
    }
}
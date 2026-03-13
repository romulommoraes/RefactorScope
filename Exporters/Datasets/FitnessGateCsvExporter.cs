using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using System.Text;

namespace RefactorScope.Exporters.Datasets
{
    /// <summary>
    /// Exporta os fitness gates em CSV.
    ///
    /// Convenção atual de saída
    /// ------------------------
    /// output/datasets/csvs/fitness/dataset_fitness_gates.csv
    ///
    /// Motivo
    /// ------
    /// Este artefato é um dataset tabular consumível por BI, auditoria
    /// e filtros operacionais, portanto pertence ao bloco de datasets.
    /// </summary>
    public sealed class FitnessGateCsvExporter : IExporter
    {
        public string Name => "fitnessGateCsv";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var gates = report.Results
                .OfType<FitnessGateResult>()
                .FirstOrDefault();

            if (gates == null)
                return;

            var sb = new StringBuilder();

            sb.AppendLine("GateName,Status,Message");

            foreach (var gate in gates.Gates)
            {
                var name = Escape(gate.GateName);
                var status = Escape(gate.Status.ToString());
                var message = Escape(gate.Message);

                sb.AppendLine($"{name},{status},{message}");
            }

            var rootOutputPath = context.Config.OutputPath;
            var csvRootPath = Path.Combine(rootOutputPath, "datasets", "csvs");
            var fitnessDirectory = Path.Combine(csvRootPath, "fitness");

            Directory.CreateDirectory(fitnessDirectory);

            var path = Path.Combine(fitnessDirectory, "dataset_fitness_gates.csv");

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }
    }
}
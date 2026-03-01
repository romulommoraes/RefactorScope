using RefactorScope.Core.Orchestration;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Define o contrato para exportação de resultados de análise.
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Nome do exportador (ex: JSON, CSV, DumpIA).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Exporta o relatório consolidado.
        /// </summary>
        /// <param name="report">Relatório consolidado.</param>
        /// <param name="outputPath">Caminho de saída.</param>
        void Export(ConsolidatedReport report, string outputPath);
    }
}
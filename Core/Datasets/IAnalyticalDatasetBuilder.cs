using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Datasets
{
    /// <summary>
    /// Define o contrato para builders de datasets analíticos.
    /// 
    /// Responsável por transformar o relatório consolidado
    /// em linhas de dados prontas para exportação.
    /// </summary>
    public interface IAnalyticalDatasetBuilder
    {
        /// <summary>
        /// Nome do dataset gerado.
        /// </summary>
        string DatasetName { get; }

        /// <summary>
        /// Gera linhas do dataset a partir do relatório.
        /// </summary>
        IEnumerable<string[]> Build(
            AnalysisContext context,
            ConsolidatedReport report);

        /// <summary>
        /// Cabeçalhos do dataset.
        /// </summary>
        string[] Headers { get; }
    }
}
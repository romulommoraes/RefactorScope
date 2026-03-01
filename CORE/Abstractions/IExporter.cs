using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Define o contrato para exportação de resultados de análise.
    /// 
    /// Exportadores não decidem o destino.
    /// O CLI define o outputPath.
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Nome do exportador (ex: JSON, CSV, DumpIA).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Exporta os resultados da análise.
        /// </summary>
        /// <param name="context">Contexto da análise</param>
        /// <param name="report">Relatório consolidado</param>
        /// <param name="outputPath">Pasta de saída definida pelo CLI</param>
        void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath
        );
    }
}
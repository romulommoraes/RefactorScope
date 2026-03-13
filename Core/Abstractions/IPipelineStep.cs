using RefactorScope.Core.Context;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Representa um módulo executável no pipeline de análise.
    /// 
    /// Exemplos:
    /// - AnalyzerStep
    /// - StatisticsStep
    /// - EstimationStep
    /// - ExportStep (futuro)
    /// </summary>
    public interface IPipelineStep
    {
        string Name { get; }

        void Execute(AnalysisContext context);
    }
}
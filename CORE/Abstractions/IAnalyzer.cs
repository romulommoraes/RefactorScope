using RefactorScope.Core.Context;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Define o contrato de execução para analisadores estruturais.
    /// Cada analisador deve operar de forma isolada e determinística.
    /// </summary>
    public interface IAnalyzer
    {
        /// <summary>
        /// Nome único do analisador (usado na configuração).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executa a análise com base no contexto fornecido.
        /// </summary>
        /// <param name="context">Contexto de análise contendo modelo estrutural e configuração.</param>
        /// <returns>Resultado da análise.</returns>
        IAnalysisResult Analyze(AnalysisContext context);
    }
}
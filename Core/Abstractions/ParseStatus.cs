namespace RefactorScope.Core.Abstractions;

/// <summary>
/// Representa o estado final da execução de um parser.
/// Usado para indicar sucesso, avisos estruturais ou falhas.
/// </summary>
public enum ParseStatus
{
    /// <summary>
    /// O parsing foi concluído com sucesso e o modelo gerado é considerado confiável.
    /// </summary>
    Success,

    /// <summary>
    /// O parsing foi concluído, porém o avaliador de plausibilidade detectou anomalias estruturais.
    /// O modelo pode estar incompleto ou parcialmente incorreto.
    /// </summary>
    PlausibilityWarning,

    /// <summary>
    /// O parser primário falhou ou retornou resultado implausível e um parser de fallback assumiu.
    /// </summary>
    FallbackTriggered,

    /// <summary>
    /// O parsing falhou completamente devido a erro crítico ou exceção.
    /// </summary>
    Failed
}
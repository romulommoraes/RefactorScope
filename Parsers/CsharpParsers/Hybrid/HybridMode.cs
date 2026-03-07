namespace RefactorScope.Parsers.Hybrid;

/// <summary>
/// Define o comportamento do HybridParser.
/// </summary>
public enum HybridMode
{
    /// <summary>
    /// Usa o parser primário.
    /// Caso falhe ou gere resultado implausível,
    /// executa o parser secundário.
    /// </summary>
    Failover,

    /// <summary>
    /// Executa ambos os parsers e combina seus resultados.
    /// Regex define estrutura.
    /// Textual enriquece dependências.
    /// </summary>
    Merge,

    /// <summary>
    /// Merge resiliente.
    /// Caso o parser primário gere estrutura fraca,
    /// o parser secundário recupera tipos e dependências.
    /// </summary>
    Adaptive,

    /// <summary>
    /// Merge incremental.
    /// O parser secundário só executa caso o modelo
    /// primário esteja incompleto.
    /// Reduz custo computacional.
    /// </summary>
    Incremental
}
namespace RefactorScope.Core.Parsing.Enum;

/// <summary>
/// Define as estratégias disponíveis para parsing do código.
///
/// A estratégia controla qual motor principal será utilizado
/// para construir o modelo estrutural do projeto.
///
/// O design é modular para permitir experimentação e evolução
/// futura (ex.: SharpParse).
/// </summary>
public enum ParserStrategy
{
    /// <summary>
    /// Parser baseado exclusivamente em Regex.
    /// 
    /// Mais rápido, porém menos resiliente para sintaxes modernas
    /// ou construções complexas de C#.
    /// </summary>
    RegexFast,

    /// <summary>
    /// Parser híbrido seletivo.
    ///
    /// O sistema classifica previamente as classes e decide
    /// qual engine utilizar:
    ///
    /// - Textual → classes seguras
    /// - Regex → classes complexas
    /// - PatternSimilarity → fallback
    ///
    /// Estratégia padrão recomendada.
    /// </summary>
    Selective,

    /// <summary>
    /// Parser adaptativo experimental.
    ///
    /// O sistema tenta ajustar heurísticas dinamicamente
    /// baseado nos resultados de parsing.
    /// </summary>
    AdaptiveExperimental,

    /// <summary>
    /// Parser incremental experimental.
    ///
    /// Utilizado para parsing parcial de projetos grandes
    /// onde apenas arquivos alterados são reprocessados.
    /// </summary>
    IncrementalExperimental
}
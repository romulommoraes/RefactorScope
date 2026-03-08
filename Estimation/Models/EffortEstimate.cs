namespace RefactorScope.Estimation.Models
{
    /// <summary>
    /// Representa a estimativa final de esforço de refatoração calculada pelo sistema.
    /// 
    /// Esse modelo é o output principal do módulo de Estimation e resume:
    /// 
    /// • Índice de dificuldade de refatoração (RDI)
    /// • Classificação qualitativa da dificuldade
    /// • Estimativa de horas de refatoração
    /// • Confiança da estimativa baseada nas estatísticas do sistema
    /// 
    /// Esse objeto é posteriormente serializado em JSON ou consumido por dashboards.
    /// </summary>
    public record EffortEstimate(
        int RDI,
        string Difficulty,
        double EstimatedHours,
        double Confidence
    );
}
// ==========================================
// ARQUIVO: StatisticsReport.cs
// CAMINHO: RefactorScope.Statistics\Models\StatisticsReport.cs
// ==========================================
namespace RefactorScope.Statistics.Models
{
    /// <summary>
    /// Métricas de confiabilidade sobre a extração estrutural realizada pelos parsers.
    /// </summary>
    public record ParsingConfidence(
        double ClassesPerFile,
        double ReferencesPerClass
    );

    /// <summary>
    /// Resumo estatístico das métricas arquiteturais calculadas na execução.
    /// </summary>
    public record MetricsStatisticsSummary(
        double MeanCoupling,
        double UnresolvedCandidateRatio,
        double NamespaceDriftRatio
    );

    /// <summary>
    /// Relatório consolidado contendo a saúde estatística da análise do RefactorScope.
    /// </summary>
    public record StatisticsReport(
        ParsingConfidence Confidence,
        MetricsStatisticsSummary Summary
    );
}
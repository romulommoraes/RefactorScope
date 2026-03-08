using RefactorScope.Core.Context;

namespace RefactorScope.Estimation.Classification
{
    /// <summary>
    /// Classificador heurístico de refactors necessários.
    ///
    /// Esta versão utiliza apenas dados disponíveis no ModeloEstrutural,
    /// evitando dependência de parsing profundo (métodos, AST etc.).
    ///
    /// Heurísticas utilizadas:
    ///
    /// • Alto fan-out → possível necessidade de decoupling
    /// • Muitos tipos → pressão estrutural
    /// • Referências elevadas → possível classe "god object"
    /// </summary>
    public static class RefactorClassifier
    {
        public static double ComputeActionScore(AnalysisContext context)
        {
            var model = context.Model;

            int highFanOutTypes =
                model.Tipos.Count(t => t.References.Count > 10);

            int extremeFanOutTypes =
                model.Tipos.Count(t => t.References.Count > 20);

            double score =
                (highFanOutTypes * 2) +
                (extremeFanOutTypes * 4);

            return Math.Min(25, score);
        }
    }
}
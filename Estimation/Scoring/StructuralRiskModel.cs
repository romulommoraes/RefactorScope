namespace RefactorScope.Estimation.Scoring
{
    /// <summary>
    /// Calcula o componente StructuralRisk do RDI.
    ///
    /// Este valor mede o nível de degradação estrutural da arquitetura
    /// com base em dois sinais principais:
    ///
    /// • NamespaceDriftRatio
    ///     Proporção de tipos classificados como drift arquitetural.
    ///
    /// • UnresolvedCandidateRatio
    ///     Proporção de tipos potencialmente mortos após refinamento
    ///     estrutural (zombie candidates).
    ///
    /// O valor é normalizado para um máximo de 25 pontos.
    /// </summary>
    public static class StructuralRiskModel
    {
        public static double Compute(
            double namespaceDriftRatio,
            double unresolvedCandidateRatio)
        {
            double risk =
                (namespaceDriftRatio * 25) +
                (unresolvedCandidateRatio * 25);

            return Math.Min(25, risk);
        }
    }
}
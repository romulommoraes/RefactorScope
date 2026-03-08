namespace RefactorScope.Estimation.Scoring
{
    /// <summary>
    /// Calcula o impacto do acoplamento arquitetural na dificuldade
    /// de refatoração.
    ///
    /// A métrica utilizada é a média de fan-out estrutural entre tipos
    /// do sistema (MeanCoupling).
    ///
    /// Valores mais altos indicam maior pressão arquitetural para
    /// desacoplamento.
    ///
    /// O resultado é normalizado para um máximo de 25 pontos.
    /// </summary>
    public static class CouplingPressureModel
    {
        public static double Compute(double meanCoupling)
        {
            double pressure = meanCoupling * 3;

            return Math.Min(25, pressure);
        }
    }
}
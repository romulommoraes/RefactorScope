using RefactorScope.Core.Context;

namespace RefactorScope.Estimation.Scoring
{
    /// <summary>
    /// Mede pressão estrutural causada pelo tamanho do projeto.
    /// 
    /// Quanto maior a densidade estrutural (classes por arquivo),
    /// maior a probabilidade de refactors complexos.
    /// </summary>
    public static class SizePressureModel
    {
        public static double Compute(AnalysisContext context)
        {
            var model = context.Model;

            var files = Math.Max(model.Arquivos.Count, 1);

            double classesPerFile = (double)model.Tipos.Count / files;

            double pressure = classesPerFile * 8;

            return Math.Min(25, pressure);
        }
    }
}
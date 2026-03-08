using System.Linq;
using RefactorScope.Core.Model;

namespace RefactorScope.Parsers.CsharpParsers.Hybrid
{

    /// <summary>
    /// Responsável por combinar dois ModeloEstrutural garantindo
    /// integridade do grafo de tipos.
    /// </summary>
    public static class ModeloMerger
    {
        public static ModeloEstrutural Merge(ModeloEstrutural regexModel, ModeloEstrutural textualModel)
        {
            var tipos = regexModel.Tipos;
            var arquivos = regexModel.Arquivos;

            // Apenas tipos confirmados pelo Regex são aceitos
            var tiposValidados = tipos.Select(t => t.Name).ToHashSet();

            var referenciasCombinadas =
                regexModel.Referencias
                .Concat(textualModel.Referencias)
                .Where(r =>
                    tiposValidados.Contains(r.FromType) &&
                    tiposValidados.Contains(r.ToType))
                .DistinctBy(r => new { r.FromType, r.ToType, r.Kind })
                .ToList();

            // Reinjeta referências no grafo interno
            foreach (var tipo in tipos)
            {
                var refsDoTipo =
                    referenciasCombinadas
                    .Where(r => r.FromType == tipo.Name)
                    .ToList();

                typeof(TipoInfo)
                    .GetField("<References>k__BackingField",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(tipo, refsDoTipo);
            }

            return new ModeloEstrutural(
                regexModel.RootPath,
                arquivos,
                tipos,
                referenciasCombinadas);
        }
    }
}
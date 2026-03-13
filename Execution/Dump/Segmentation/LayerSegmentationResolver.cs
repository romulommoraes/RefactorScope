using RefactorScope.Core.Context;
using RefactorScope.Core.Model;
using RefactorScope.Infrastructure;
namespace RefactorScope.Execution.Dump.Segmentation
{
            public class LayerSegmentationResolver : ISegmentationResolver
        {
            public IEnumerable<SegmentScope> Resolve(AnalysisContext context)
            {
                var groups = context.Model.Tipos
                    .GroupBy(t => LayerRuleEvaluator.ResolveLayer(t, context.Config.LayerRules));

                foreach (var group in groups)
                {
                    var tipos = group.ToList();

                    var filteredModel = new ModeloEstrutural(
                        context.Model.RootPath,
                        context.Model.Arquivos.Where(a =>
                            a.Tipos.Any(t => tipos.Contains(t))).ToList(),
                        tipos,
                        context.Model.Referencias.Where(r =>
                            tipos.Any(t => t.Name == r.FromType)).ToList()
                    );

                    yield return new SegmentScope(
                        group.Key,
                        new AnalysisContext(context.Config, filteredModel)
                    );
                }
            }
        }
    }
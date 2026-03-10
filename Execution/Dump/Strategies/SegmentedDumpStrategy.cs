using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Execution.Dump.Segmentation;


namespace RefactorScope.Execution.Dump.Strategies
{ 
    public class SegmentedDumpStrategy : IDumpStrategy
    {
        private readonly ISegmentationResolver _resolver;

        public SegmentedDumpStrategy(ISegmentationResolver resolver)
        {
            _resolver = resolver;
        }

        public void Execute(
            AnalysisContext context,
            ConsolidatedReport report,
            IEnumerable<IExporter> exporters)
        {
            var segments = _resolver.Resolve(context);

            foreach (var segment in segments)
            {
                var path = Path.Combine(
                    context.Config.OutputPath,
                    segment.Name
                );

                //Directory.CreateDirectory(path);
                //ESSE FLUXO SERÁ ATUALIZADO! PARA CORRIGIR ESSA GAMBIARRA, O FLUXO DE EXPORTAÇÃO VAI SER REVISADO PARA RECEBER O PATH DE EXPORTAÇÃO, ASSIM CADA EXPORTADOR VAI DECIDIR SE QUER CRIAR PASTA OU NÃO, E QUAL NOME DAR PARA O ARQUIVO DE EXPORTAÇÃO

                foreach (var exporter in exporters)
                {
                    exporter.Export(
                        segment.Context,
                        report,
                        path
                    );
                }
            }
        }
    }
}

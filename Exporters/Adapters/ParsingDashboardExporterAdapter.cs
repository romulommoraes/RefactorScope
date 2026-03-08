using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter temporário para integrar o ParsingDashboardExporter
    /// ao pipeline padronizado de IExporter.
    ///
    /// Motivação
    /// ---------
    /// ParsingDashboardExporter já gera corretamente seu artefato HTML,
    /// mas ainda opera fora do contrato IExporter utilizado pelo fluxo
    /// principal do CLI.
    ///
    /// Este adapter permite que o dashboard de parsing entre no novo
    /// bloco de dashboards HTML mantendo a implementação atual intacta.
    ///
    /// Estratégia atual
    /// ----------------
    /// - recuperar ParserResult a partir do AnalysisContext
    /// - chamar ParsingDashboardExporter sem alterar sua assinatura atual
    /// - publicar no mesmo diretório compartilhado da suíte HTML
    ///
    /// Caminho de refatoração futura
    /// ----------------------------
    /// 1. Expor ParserResult de forma explícita e estável no contexto
    /// 2. Fazer ParsingDashboardExporter implementar IExporter nativamente
    /// 3. Eliminar reflexão/fallback e remover este adapter
    ///
    /// Observação
    /// ----------
    /// Este adapter existe apenas como camada de compatibilidade.
    /// A lógica de geração do HTML continua centralizada no exporter original.
    /// </summary>
    public sealed class ParsingDashboardExporterAdapter : IExporter
    {
        public string Name => "parsing-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var parserResult = TryGetParserResult(context);
            if (parserResult == null)
                return;

            var exporter = new ParsingDashboardExporter();
            exporter.Export(parserResult, outputPath);
        }

        private static dynamic? TryGetParserResult(AnalysisContext context)
        {
            try
            {
                var prop = context.GetType().GetProperty("ParserResult");
                return prop?.GetValue(context);
            }
            catch
            {
                return null;
            }
        }
    }
}
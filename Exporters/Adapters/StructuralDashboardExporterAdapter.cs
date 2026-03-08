using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Adapters
{
    /// <summary>
    /// Adapter temporário para integrar o StructuralInventoryExporter
    /// ao pipeline padronizado de IExporter.
    ///
    /// Motivação
    /// ---------
    /// O projeto possui exporters HTML mais recentes que ainda não
    /// implementam IExporter diretamente, pois nasceram fora do fluxo
    /// principal de exportação.
    ///
    /// Este adapter permite encaixar o dashboard estrutural no novo
    /// bloco de exportadores HTML sem alterar imediatamente a classe
    /// original, reduzindo risco de regressão.
    ///
    /// Estratégia atual
    /// ----------------
    /// - manter StructuralInventoryExporter como está
    /// - encapsular sua chamada via IExporter
    /// - permitir execução ordenada junto ao Hub HTML
    ///
    /// Caminho de refatoração futura
    /// ----------------------------
    /// 1. Extrair uma interface/contrato comum para exporters HTML
    /// 2. Tornar StructuralInventoryExporter um IExporter nativo
    /// 3. Remover este adapter após estabilização da suíte HTML
    ///
    /// Observação
    /// ----------
    /// Esta classe é deliberadamente fina e sem regra de negócio.
    /// Toda a lógica real continua no exporter original.
    /// </summary>
    public sealed class StructuralDashboardExporterAdapter : IExporter
    {
        public string Name => "structural-dashboard";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var exporter = new StructuralInventoryExporter();
            exporter.Export(report, outputPath);
        }
    }
}
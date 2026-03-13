using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Infrastructure
{
    /// <summary>
    /// Pacote consolidado de exportação.
    ///
    /// Intenção arquitetural
    /// ---------------------
    /// Esta classe representa a semente de uma futura camada unificada
    /// de exportação do RefactorScope.
    ///
    /// No estado atual do projeto, parte do pipeline ainda opera pelo
    /// contrato legado:
    ///
    ///     IExporter.Export(context, report, outputPath)
    ///
    /// Porém, à medida que a suíte de exportação cresceu, tornou-se
    /// necessário consolidar informações transversais em um único objeto,
    /// especialmente para cenários como:
    ///
    /// - exportação JSON única da execução ("analysis.json")
    /// - snapshots analíticos consolidados
    /// - observability futura da exportação
    /// - orquestração mais limpa da camada de output
    ///
    /// Papel atual
    /// -----------
    /// Hoje, esta classe ainda está em transição arquitetural.
    /// Seu uso é deliberadamente inicial e serve para:
    ///
    /// - encapsular o contexto global da exportação
    /// - preparar a migração para um pipeline mais unificado
    /// - evitar espalhar parâmetros soltos pelo código
    ///
    /// Papel futuro
    /// ------------
    /// Esta classe deverá servir como entrada principal para exportadores
    /// consolidados, especialmente:
    ///
    /// - ExportAnalysisJson
    /// - snapshots executivos
    /// - métricas de observability da exportação
    ///
    /// Observação
    /// ----------
    /// Enquanto a migração completa não ocorre, esta classe pode parecer
    /// subutilizada sob uma leitura puramente estrutural.
    /// Isso é esperado e faz parte da transição entre o modelo legado
    /// de exporters e o modelo consolidado de exportação.
    /// </summary>
    public sealed class ExportPackage
    {
        /// <summary>
        /// Diretório raiz do output final da execução.
        /// </summary>
        public required string RootOutputPath { get; init; }

        /// <summary>
        /// Relatório consolidado produzido pelo motor de análise.
        /// </summary>
        public required ConsolidatedReport Report { get; init; }

        /// <summary>
        /// Resultado do parsing, quando disponível.
        ///
        /// Nem todos os fluxos de exportação dependem dele diretamente,
        /// mas ele é importante para snapshots completos e para a futura
        /// exportação JSON unificada.
        /// </summary>
        public IParserResult? ParsingResult { get; init; }

        /// <summary>
        /// Contexto global da execução analítica.
        /// </summary>
        public required AnalysisContext AnalysisContext { get; init; }

        /// <summary>
        /// Timestamp UTC de geração do pacote.
        /// </summary>
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
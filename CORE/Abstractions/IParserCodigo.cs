using RefactorScope.Core.Model;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Define o contrato para parsers de código.
    /// Converte código fonte em Modelo Estrutural agnóstico.
    /// 
    /// O parser recebe instruções de escopo (include/exclude),
    /// mas não conhece a configuração diretamente,
    /// preservando o isolamento arquitetural.
    /// </summary>
    public interface IParserCodigo
    {
        /// <summary>
        /// Nome do parser (ex: CSharpRegex, Roslyn).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Constrói o modelo estrutural a partir do caminho alvo.
        /// 
        /// Include e Exclude permitem restringir o escopo de análise
        /// sem acoplar o parser ao sistema de configuração.
        /// </summary>
        /// <param name="rootPath">Raiz do escopo de análise.</param>
        /// <param name="include">Pastas ou caminhos a incluir.</param>
        /// <param name="exclude">Pastas ou caminhos a excluir.</param>
        /// <returns>Modelo estrutural gerado.</returns>
        ModeloEstrutural Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null
        );
    }
}
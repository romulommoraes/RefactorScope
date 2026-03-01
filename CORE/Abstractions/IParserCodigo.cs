using RefactorScope.Core.Model;

namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Define o contrato para parsers de código.
    /// Converte código fonte em Modelo Estrutural agnóstico.
    /// </summary>
    public interface IParserCodigo
    {
        /// <summary>
        /// Nome do parser (ex: CSharpRegex, Roslyn).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Constrói o modelo estrutural a partir do caminho alvo.
        /// </summary>
        /// <param name="rootPath">Raiz do escopo de análise.</param>
        /// <returns>Modelo estrutural gerado.</returns>
        ModeloEstrutural Parse(string rootPath);
    }
}
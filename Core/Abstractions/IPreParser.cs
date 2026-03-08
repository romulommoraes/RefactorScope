namespace RefactorScope.Core.Abstractions
{
    /// <summary>
    /// Contrato para pré-processadores de código fonte.
    ///
    /// Um PreParser é responsável por sanitizar o texto antes
    /// da etapa de parsing estrutural, removendo construções
    /// que podem gerar falsos positivos (comentários, strings,
    /// código gerado, etc.).
    ///
    /// Essa etapa funciona como um "lexer simplificado"
    /// no pipeline de parsing textual.
    /// </summary>
    public interface IPreParser
    {
        string Sanitize(string source);
    }
}
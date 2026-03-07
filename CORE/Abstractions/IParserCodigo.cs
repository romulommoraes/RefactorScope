using System.Collections.Generic;

namespace RefactorScope.Core.Abstractions;

/// <summary>
/// Contrato base para todos os parsers de código do sistema.
/// </summary>
public interface IParserCodigo
{
    /// <summary>Nome identificador do parser.</summary>
    string Name { get; }

    /// <summary>
    /// Executa a análise do código e retorna um envelope observável contendo o modelo e as métricas.
    /// </summary>
    /// <param name="rootPath">Caminho raiz do projeto a ser analisado.</param>
    /// <param name="include">Filtros de inclusão de arquivos (opcional).</param>
    /// <param name="exclude">Filtros de exclusão de arquivos (opcional).</param>
    /// <returns>Um IParserResult contendo o estado, métricas e o modelo gerado.</returns>
    IParserResult Parse(
        string rootPath,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null);
}
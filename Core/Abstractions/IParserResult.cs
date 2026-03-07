using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using System;

namespace RefactorScope.Core.Abstractions;

/// <summary>
/// Contrato global para o envelope de retorno de qualquer parser do sistema.
/// 
/// O objetivo deste contrato é fornecer observabilidade completa do processo
/// de parsing, incluindo métricas de execução, confiança do resultado e
/// tratamento seguro de falhas.
/// </summary>
public interface IParserResult
{
    /// <summary>
    /// Nome identificador do parser que gerou este resultado.
    /// Exemplo: "CSharpRegex", "CSharpTextual", "HybridParser".
    /// </summary>
    string ParserName { get; }

    /// <summary>
    /// Status final da operação de parsing.
    /// </summary>
    ParseStatus Status { get; }

    /// <summary>
    /// Índice de confiança da extração estrutural (0.0 a 1.0).
    /// </summary>
    double Confidence { get; }

    /// <summary>
    /// Indica se o modelo gerado passou na avaliação heurística de plausibilidade.
    /// </summary>
    bool IsPlausible { get; }

    /// <summary>
    /// Modelo estrutural extraído do código.
    /// Pode ser null em caso de falha total do parser.
    /// </summary>
    ModeloEstrutural? Model { get; }

    /// <summary>
    /// Estatísticas de execução do parser.
    /// Contém métricas como tempo de execução e memória estimada.
    /// </summary>
    ParserExecutionStats? Stats { get; }

    /// <summary>
    /// Exceção capturada caso o parser tenha falhado com erro crítico.
    /// </summary>
    Exception? Error { get; }

    bool UsedFallback { get; }
}
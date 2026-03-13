using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using System;

namespace RefactorScope.Core.Parsing;

/// <summary>
/// Implementação padrão imutável do envelope de resultado de parsing.
/// 
/// Este objeto encapsula todas as informações produzidas por um parser:
/// - estado da execução
/// - nível de confiança
/// - plausibilidade estrutural
/// - métricas de execução
/// - exceções capturadas
/// </summary>
public record ParserResult(
    /// <summary>
    /// Status final da operação de parsing.
    /// </summary>
    ParseStatus Status,

    /// <summary>
    /// Indica se o modelo gerado passou na verificação de plausibilidade.
    /// </summary>
    bool IsPlausible,

    /// <summary>
    /// Índice de confiança da extração (0.0 a 1.0).
    /// </summary>
    double Confidence,

    /// <summary>
    /// Nome identificador do parser que gerou este resultado.
    /// </summary>
    string ParserName,

    /// <summary>
    /// Modelo estrutural extraído do código.
    /// Pode ser null em caso de falha total.
    /// </summary>
    ModeloEstrutural? Model,

    bool UsedFallback = false,

    /// <summary>
    /// Estatísticas de execução do parser.
    /// </summary>
    ParserExecutionStats? Stats = null,

    /// <summary>
    /// Exceção capturada durante a execução do parser, se houver.
    /// </summary>
    Exception? Error = null
) : IParserResult;
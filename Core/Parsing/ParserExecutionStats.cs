using System;

namespace RefactorScope.Core.Parsing;

/// <summary>
/// Contém métricas de execução de um parser.
/// 
/// Estas métricas são utilizadas para:
/// - Observabilidade do pipeline de parsing
/// - Benchmark entre diferentes parsers
/// - Diagnóstico de performance
/// - Futuro auto-tuning de parsers
/// </summary>
public record ParserExecutionStats(
    /// <summary>
    /// Tempo total de execução do parser.
    /// </summary>
    TimeSpan ExecutionTime,

    /// <summary>
    /// Estimativa de memória utilizada durante o parsing (em bytes).
    /// </summary>
    long EstimatedMemoryBytes,

    /// <summary>
    /// Indica se foi detectada alguma anomalia estrutural durante o parsing.
    /// </summary>
    bool AnomalyDetected
);
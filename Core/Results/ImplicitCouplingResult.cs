using System.Collections.Generic;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado da análise de acoplamento implícito.
    ///
    /// Detecta possíveis hubs arquiteturais ou dependências
    /// concentradas que podem indicar:
    ///
    /// - God Classes
    /// - Orchestrators ocultos
    /// - Acoplamento estrutural forte
    ///
    /// Baseado em heurísticas:
    /// - Fan-in
    /// - Fan-out
    /// - Dominance
    /// - Volume de dependências
    /// </summary>
    public sealed class ImplicitCouplingResult : IAnalysisResult
    {
        public IReadOnlyList<CouplingSuspicion> Suspicions { get; }

        public ImplicitCouplingResult(List<CouplingSuspicion> suspicions)
        {
            Suspicions = suspicions;
        }
    }
}
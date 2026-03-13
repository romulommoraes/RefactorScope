using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Representa o relatório consolidado da execução.
    ///
    /// 🔒 Fonte Única de Verdade para classificação de Unresolveds.
    ///
    /// Separação formal:
    /// - Structural Candidates (ausência estrutural)
    /// - Confirmed Unresolveds (após threshold probabilístico)
    /// - Suspicious (candidatos abaixo do threshold)
    /// - PatternSimilarity (candidatos estruturais não confirmados)
    /// </summary>
    public class ConsolidatedReport
    {
        public IReadOnlyCollection<IAnalysisResult> Results { get; }
        public DateTime ExecutionTime { get; }
        public string TargetScope { get; }

        /// <summary>
        /// Threshold aplicado para confirmação probabilística.
        /// </summary>
        public double UnresolvedProbabilityThreshold { get; }

        public ConsolidatedReport(
            IReadOnlyCollection<IAnalysisResult> results,
            DateTime executionTime,
            string targetScope,
            double unresolvedProbabilityThreshold)
        {
            Results = results;
            ExecutionTime = executionTime;
            TargetScope = targetScope;
            UnresolvedProbabilityThreshold = unresolvedProbabilityThreshold;
        }

        // ==========================================================
        // 🔹 Acesso Genérico
        // ==========================================================

        public T? GetResult<T>() where T : class, IAnalysisResult
            => Results.OfType<T>().FirstOrDefault();

        // ==========================================================
        // 🔹 Camada 1 — Structural Candidates
        // ==========================================================

        public IReadOnlyList<string> GetStructuralCandidates()
        {
            var structural = GetResult<StructuralCandidateResult>();
            return structural?.StructuralCandidateTypes ?? new List<string>();
        }

        // ==========================================================
        // 🔹 Camada 2 — Unresolved
        // ==========================================================

        public IReadOnlyList<string> GetUnresolvedCandidates()
        {
            var probabilistic = GetResult<StructuralCandidateProbabilityResult>();

            if (probabilistic == null)
                return new List<string>();

            return probabilistic
                .Unresolved(UnresolvedProbabilityThreshold)
                .Select(x => x.TypeName)
                .ToList();
        }

        /// <summary>
        /// Compatibilidade retroativa.
        /// </summary>
        public IReadOnlyList<string> GetEffectiveUnresolvedCandidates()
            => GetUnresolvedCandidates();

        // ==========================================================
        // 🔹 Camada 3 — Suspicious
        // ==========================================================

        public IReadOnlyList<string> GetPatternSimilarityCandidates()
        {
            var probabilistic = GetResult<StructuralCandidateProbabilityResult>();

            if (probabilistic == null)
                return new List<string>();

            return probabilistic.Items
                .Where(i => i.Probability < UnresolvedProbabilityThreshold)
                .Select(i => i.TypeName)
                .ToList();
        }

        // ==========================================================
        // 🔹 Camada 4 — PatternSimilarity
        // ==========================================================

        public IReadOnlyList<string> GetPatternSimilarity()
        {
            var structural = GetStructuralCandidates();
            var confirmed = GetUnresolvedCandidates();

            return structural
                .Where(s => !confirmed.Contains(s))
                .ToList();
        }

        // ==========================================================
        // 🔹 Breakdown Oficial
        // ==========================================================

        public StructuralCandidateAnalysisBreakdown GetStructuralCandidateBreakdown()
        {
            var structural = GetStructuralCandidates();
            var confirmed = GetUnresolvedCandidates();
            var suspicious = GetPatternSimilarityCandidates();
            var patternSimilarity = GetPatternSimilarity();

            var reduction = structural.Count == 0
                ? 0
                : (structural.Count - confirmed.Count) / (double)structural.Count;

            return new StructuralCandidateAnalysisBreakdown(
                structural.Count,
                confirmed.Count,
                suspicious.Count,
                patternSimilarity.Count,
                reduction
            );
        }

        public double GetUnresolvedCandidatesRate(int totalTypes)
        {
            if (totalTypes == 0) return 0;
            return GetUnresolvedCandidates().Count / (double)totalTypes;
        }
    }
}
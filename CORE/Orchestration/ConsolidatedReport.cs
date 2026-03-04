using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Results;
using RefactorScope.Core.Model;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Representa o relatório consolidado da execução.
    ///
    /// 🔒 Fonte Única de Verdade para classificação de Zombies.
    ///
    /// Separação formal:
    /// - Structural Candidates (ausência estrutural)
    /// - Confirmed Zombies (após threshold probabilístico)
    /// - Suspicious (candidatos abaixo do threshold)
    /// - Absolved (candidatos estruturais não confirmados)
    /// </summary>
    public class ConsolidatedReport
    {
        public IReadOnlyCollection<IAnalysisResult> Results { get; }
        public DateTime ExecutionTime { get; }
        public string TargetScope { get; }

        /// <summary>
        /// Threshold aplicado para confirmação probabilística.
        /// </summary>
        public double ZombieProbabilityThreshold { get; }

        public ConsolidatedReport(
            IReadOnlyCollection<IAnalysisResult> results,
            DateTime executionTime,
            string targetScope,
            double zombieProbabilityThreshold)
        {
            Results = results;
            ExecutionTime = executionTime;
            TargetScope = targetScope;
            ZombieProbabilityThreshold = zombieProbabilityThreshold;
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
            var structural = GetResult<ZombieResult>();
            return structural?.ZombieTypes ?? new List<string>();
        }

        // ==========================================================
        // 🔹 Camada 2 — Confirmed Zombies
        // ==========================================================

        public IReadOnlyList<string> GetConfirmedZombies()
        {
            var probabilistic = GetResult<ZombieProbabilityResult>();

            if (probabilistic == null)
                return new List<string>();

            return probabilistic
                .ConfirmedZombies(ZombieProbabilityThreshold)
                .Select(x => x.TypeName)
                .ToList();
        }

        /// <summary>
        /// Compatibilidade retroativa.
        /// </summary>
        public IReadOnlyList<string> GetEffectiveZombieTypes()
            => GetConfirmedZombies();

        // ==========================================================
        // 🔹 Camada 3 — Suspicious
        // ==========================================================

        public IReadOnlyList<string> GetSuspiciousZombies()
        {
            var probabilistic = GetResult<ZombieProbabilityResult>();

            if (probabilistic == null)
                return new List<string>();

            return probabilistic.Items
                .Where(i => i.Probability < ZombieProbabilityThreshold)
                .Select(i => i.TypeName)
                .ToList();
        }

        // ==========================================================
        // 🔹 Camada 4 — Absolved
        // ==========================================================

        public IReadOnlyList<string> GetAbsolvedZombies()
        {
            var structural = GetStructuralCandidates();
            var confirmed = GetConfirmedZombies();

            return structural
                .Where(s => !confirmed.Contains(s))
                .ToList();
        }

        // ==========================================================
        // 🔹 Breakdown Oficial
        // ==========================================================

        public ZombieAnalysisBreakdown GetZombieBreakdown()
        {
            var structural = GetStructuralCandidates();
            var confirmed = GetConfirmedZombies();
            var suspicious = GetSuspiciousZombies();
            var absolved = GetAbsolvedZombies();

            var reduction = structural.Count == 0
                ? 0
                : (structural.Count - confirmed.Count) / (double)structural.Count;

            return new ZombieAnalysisBreakdown(
                structural.Count,
                confirmed.Count,
                suspicious.Count,
                absolved.Count,
                reduction
            );
        }

        public double GetZombieRate(int totalTypes)
        {
            if (totalTypes == 0) return 0;
            return GetConfirmedZombies().Count / (double)totalTypes;
        }
    }
}
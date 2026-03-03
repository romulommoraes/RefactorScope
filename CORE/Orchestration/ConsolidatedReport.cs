using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Results;
using RefactorScope.Core.Model;

namespace RefactorScope.Core.Orchestration
{
    /// <summary>
    /// Representa o relatório consolidado da execução.
    ///
    /// 🔒 Fonte única de verdade para zombies.
    /// O threshold probabilístico passa a fazer parte do estado da análise.
    /// </summary>
    public class ConsolidatedReport
    {
        public IReadOnlyCollection<IAnalysisResult> Results { get; }
        public DateTime ExecutionTime { get; }
        public string TargetScope { get; }

        /// <summary>
        /// Threshold usado para confirmar zombies probabilísticos.
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
        // 🔹 Fonte Oficial de Zombie
        // ==========================================================

        /// <summary>
        /// Retorna os zombies efetivos respeitando o threshold da análise.
        /// </summary>
        public IReadOnlyList<string> GetEffectiveZombieTypes()
        {
            var probabilistic = GetResult<ZombieProbabilityResult>();

            if (probabilistic != null)
            {
                return probabilistic
                    .ConfirmedZombies(ZombieProbabilityThreshold)
                    .Select(x => x.TypeName)
                    .ToList();
            }

            var legacy = GetResult<ZombieResult>();
            return legacy?.ZombieTypes ?? new List<string>();
        }

        /// <summary>
        /// Retorna todos os candidatos a zombie.
        /// </summary>
        public IReadOnlyList<string> GetZombieCandidates()
        {
            var probabilistic = GetResult<ZombieProbabilityResult>();

            if (probabilistic != null)
            {
                return probabilistic
                    .Items
                    .Select(x => x.TypeName)
                    .ToList();
            }

            var legacy = GetResult<ZombieResult>();
            return legacy?.ZombieTypes ?? new List<string>();
        }

        /// <summary>
        /// Taxa efetiva de zombie.
        /// </summary>
        public double GetZombieRate(int totalTypes)
        {
            if (totalTypes == 0)
                return 0;

            return GetEffectiveZombieTypes().Count / (double)totalTypes;
        }

        public ZombieAnalysisBreakdown GetZombieBreakdown()
        {
            var structural = GetResult<ZombieResult>();
            var probabilistic = GetResult<ZombieProbabilityResult>();

            var structuralCount = structural?.ZombieTypes.Count ?? 0;

            if (probabilistic == null)
            {
                return new ZombieAnalysisBreakdown(
                    structuralCount,
                    structuralCount,
                    0,
                    0
                );
            }

            var confirmed = probabilistic
                .ConfirmedZombies(ZombieProbabilityThreshold)
                .Count;

            var absolved = structuralCount - confirmed;

            var reduction = structuralCount == 0
                ? 0
                : (structuralCount - confirmed) / (double)structuralCount;

            return new ZombieAnalysisBreakdown(
                structuralCount,
                confirmed,
                absolved,
                reduction
            );
        }
    }
}
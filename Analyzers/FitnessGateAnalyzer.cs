using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Avalia a prontidão arquitetural do sistema com base em Fitness Gates.
    /// Interpreta métricas já produzidas pelos analisadores.
    /// Suporta modelo probabilístico de zombies com fallback legado.
    /// </summary>
    public class FitnessGateAnalyzer : IAnalyzer
    {
        public string Name => "fitnessGates";

        private readonly FitnessGateConfig _config;

        public FitnessGateAnalyzer(FitnessGateConfig config)
        {
            _config = config;
        }

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var gates = new List<FitnessGateStatus>();

            var isolated = context.GetResult<CoreIsolationResult>();
            var coupling = context.GetResult<CouplingResult>();
            var architecture = context.GetResult<ArchitecturalClassificationResult>();
            var zombieBinary = context.GetResult<ZombieResult>();
            var zombieProb = context.GetResult<ZombieProbabilityResult>();

            gates.Add(EvaluateCoreIsolation(isolated));
            gates.Add(EvaluateDeadCode(architecture, zombieBinary, zombieProb, context));
            gates.Add(EvaluateCoupling(coupling));

            return new FitnessGateResult(gates);
        }

        // ==============================
        // Core Isolation
        // ==============================

        private FitnessGateStatus EvaluateCoreIsolation(CoreIsolationResult? isolated)
        {
            if (_config.CoreIsolation.FailIfAny &&
                isolated != null &&
                isolated.IsolatedCoreTypes.Any())
            {
                return Fail("CoreIntegrity",
                    $"{isolated.IsolatedCoreTypes.Count} tipos Core isolados");
            }

            return Pass("CoreIntegrity");
        }

        // ==============================
        // Dead Code (Zombie)
        // ==============================

        private FitnessGateStatus EvaluateDeadCode(
            ArchitecturalClassificationResult? architecture,
            ZombieResult? zombieBinary,
            ZombieProbabilityResult? zombieProb,
            AnalysisContext context)
        {
            if (architecture == null)
                return Pass("DeadCode");

            var total = architecture.Items.Count;
            if (total == 0)
                return Pass("DeadCode");

            int zombieCount;

            if (zombieProb != null)
            {
                var threshold =
                    context.Config.ZombieDetection.MinZombieProbabilityThreshold;

                zombieCount = zombieProb
                    .ConfirmedZombies(threshold)
                    .Count;
            }
            else
            {
                // Fallback legado
                zombieCount = zombieBinary?.ZombieTypes.Count ?? 0;
            }

            var rate = zombieCount / (double)total;

            if (rate > _config.DeadCode.FailAbove)
                return Fail("DeadCode", $"ZombieRate alto: {rate:0%}");

            if (rate > _config.DeadCode.WarnAbove)
                return Warn("DeadCode", $"ZombieRate moderado: {rate:0%}");

            return Pass("DeadCode");
        }

        // ==============================
        // Coupling
        // ==============================

        private FitnessGateStatus EvaluateCoupling(CouplingResult? coupling)
        {
            if (coupling == null)
                return Pass("Coupling");

            var avg = coupling.ModuleFanOut.Values
                .DefaultIfEmpty(0)
                .Average();

            if (avg > _config.Coupling.FailAbove)
                return Fail("Coupling", $"FanOut médio alto: {avg:0.0}");

            if (avg > _config.Coupling.WarnAbove)
                return Warn("Coupling", $"FanOut moderado: {avg:0.0}");

            return Pass("Coupling");
        }

        // ==============================
        // Helpers
        // ==============================

        private FitnessGateStatus Pass(string name)
            => new() { GateName = name, Status = GateStatus.Pass };

        private FitnessGateStatus Warn(string name, string msg)
            => new() { GateName = name, Status = GateStatus.Warn, Message = msg };

        private FitnessGateStatus Fail(string name, string msg)
            => new() { GateName = name, Status = GateStatus.Fail, Message = msg };
    }
}
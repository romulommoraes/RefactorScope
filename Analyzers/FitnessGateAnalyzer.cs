using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Avalia a prontidão arquitetural do sistema com base em Fitness Gates.
    /// Não cria métricas — apenas interpreta resultados existentes.
    /// Thresholds são configuráveis via refactorscope.json.
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

            var zombies = context.GetResult<ZombieResult>();
            var isolated = context.GetResult<CoreIsolationResult>();
            var coupling = context.GetResult<CouplingResult>();
            var architecture = context.GetResult<ArchitecturalClassificationResult>();

            gates.Add(EvaluateCoreIsolation(isolated));
            gates.Add(EvaluateDeadCode(zombies, architecture));
            gates.Add(EvaluateCoupling(coupling));

            return new FitnessGateResult(gates);
        }

        // ==============================
        // Gates
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

        private FitnessGateStatus EvaluateDeadCode(
            ZombieResult? zombies,
            ArchitecturalClassificationResult? architecture)
        {
            if (zombies == null || architecture == null)
                return Pass("DeadCode");

            var total = architecture.Items.Count;
            if (total == 0) return Pass("DeadCode");

            var rate = zombies.ZombieTypes.Count / (double)total;

            if (rate > _config.DeadCode.FailAbove)
                return Fail("DeadCode", $"ZombieRate alto: {rate:0%}");

            if (rate > _config.DeadCode.WarnAbove)
                return Warn("DeadCode", $"ZombieRate moderado: {rate:0%}");

            return Pass("DeadCode");
        }

        private FitnessGateStatus EvaluateCoupling(CouplingResult? coupling)
        {
            if (coupling == null)
                return Pass("Coupling");

            var avg = coupling.ModuleFanOut.Values.DefaultIfEmpty(0).Average();

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
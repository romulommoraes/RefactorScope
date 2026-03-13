namespace RefactorScope.Core.Configuration
{
    public static class FitnessGateConfigResolver
    {
        public static FitnessGateConfig Resolve(
            FitnessGateConfig? config,
            Action<string>? warn = null)
        {
            if (config == null)
            {
                warn?.Invoke("FitnessGates config ausente. Usando valores default.");
                return new FitnessGateConfig();
            }

            var resolved = new FitnessGateConfig
            {
                DeadCode = ResolveDeadCode(config.DeadCode, warn),
                Coupling = ResolveCoupling(config.Coupling, warn),
                CoreIsolation = ResolveCoreIsolation(config.CoreIsolation, warn)
            };

            return resolved;
        }

        private static DeadCodeGateConfig ResolveDeadCode(
            DeadCodeGateConfig? cfg,
            Action<string>? warn)
        {
            if (cfg == null)
            {
                warn?.Invoke("DeadCode gate ausente. Usando default.");
                return new DeadCodeGateConfig();
            }

            if (cfg.WarnAbove >= cfg.FailAbove)
            {
                warn?.Invoke("DeadCode thresholds inválidos (warn >= fail). Usando default.");
                return new DeadCodeGateConfig();
            }

            return cfg;
        }

        private static CouplingGateConfig ResolveCoupling(
            CouplingGateConfig? cfg,
            Action<string>? warn)
        {
            if (cfg == null)
            {
                warn?.Invoke("Coupling gate ausente. Usando default.");
                return new CouplingGateConfig();
            }

            if (cfg.WarnAbove >= cfg.FailAbove)
            {
                warn?.Invoke("Coupling thresholds inválidos (warn >= fail). Usando default.");
                return new CouplingGateConfig();
            }

            return cfg;
        }

        private static CoreIsolationGateConfig ResolveCoreIsolation(
            CoreIsolationGateConfig? cfg,
            Action<string>? warn)
        {
            if (cfg == null)
            {
                warn?.Invoke("CoreIsolation gate ausente. Usando default.");
                return new CoreIsolationGateConfig();
            }

            return cfg;
        }
    }
}
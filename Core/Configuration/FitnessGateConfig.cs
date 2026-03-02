namespace RefactorScope.Core.Configuration
{
    public class FitnessGateConfig
    {
        public DeadCodeGateConfig DeadCode { get; set; } = new();
        public CouplingGateConfig Coupling { get; set; } = new();
        public CoreIsolationGateConfig CoreIsolation { get; set; } = new();
    }

    public class DeadCodeGateConfig
    {
        public double WarnAbove { get; set; } = 0.10;
        public double FailAbove { get; set; } = 0.20;
    }

    public class CouplingGateConfig
    {
        public double WarnAbove { get; set; } = 3;
        public double FailAbove { get; set; } = 5;
    }

    public class CoreIsolationGateConfig
    {
        public bool FailIfAny { get; set; } = true;
    }
}
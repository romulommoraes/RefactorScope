namespace RefactorScope.Core.Results
{
    public class FitnessGateStatus
    {
        public string GateName { get; init; } = string.Empty;
        public GateStatus Status { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public enum GateStatus
    {
        Pass,
        Warn,
        Fail
    }
}
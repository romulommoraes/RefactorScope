namespace RefactorScope.Core.Governance
{
    using RefactorScope.Core.Results;

    public static class FitnessGateEvaluator
    {
        public static bool HasFailure(ConsolidatedReport report)
        {
            var gates = report.Results
                .OfType<FitnessGateResult>()
                .FirstOrDefault();

            if (gates == null)
                return false;

            return gates.HasFailure;
        }
    }
}
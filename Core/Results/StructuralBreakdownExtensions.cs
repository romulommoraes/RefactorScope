using System.Linq;

namespace RefactorScope.Core.Results
{
    public static class StructuralBreakdownExtensions
    {
        public static StructuralCandidateAnalysisBreakdown
            GetStructuralCandidateBreakdown(this ConsolidatedReport report)
        {
            var candidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            int structuralCandidates = candidates.Count;
            int confirmed = unresolved.Count;
            int similarity = patternSimilarity.Count;

            int suspicious = structuralCandidates - confirmed - similarity;

            double reduction =
                structuralCandidates == 0
                    ? 0
                    : similarity / (double)structuralCandidates;

            return new StructuralCandidateAnalysisBreakdown(
                structuralCandidates,
                confirmed,
                suspicious,
                similarity,
                reduction
            );
        }
    }
}
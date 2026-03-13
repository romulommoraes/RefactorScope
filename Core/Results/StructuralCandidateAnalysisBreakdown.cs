using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Results
{
    public class StructuralCandidateAnalysisBreakdown
    {
        public int StructuralCandidates { get; }
        public int ProbabilisticConfirmed { get; }
        public int Suspicious { get; }
        public int PatternSimilarity { get; }
        public double ReductionRate { get; }

        public StructuralCandidateAnalysisBreakdown(
            int structuralCandidates,
            int probabilisticConfirmed,
            int suspicious,
            int patternSimilarity,
            double reductionRate)
        {
            StructuralCandidates = structuralCandidates;
            ProbabilisticConfirmed = probabilisticConfirmed;
            Suspicious = suspicious;
            PatternSimilarity = patternSimilarity;
            ReductionRate = reductionRate;
        }
    }
}

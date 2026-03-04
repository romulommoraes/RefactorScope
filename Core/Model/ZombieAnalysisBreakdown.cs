using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Model
{
    public class ZombieAnalysisBreakdown
    {
        public int StructuralCandidates { get; }
        public int ProbabilisticConfirmed { get; }
        public int Suspicious { get; }
        public int Absolved { get; }
        public double ReductionRate { get; }

        public ZombieAnalysisBreakdown(
            int structuralCandidates,
            int probabilisticConfirmed,
            int suspicious,
            int absolved,
            double reductionRate)
        {
            StructuralCandidates = structuralCandidates;
            ProbabilisticConfirmed = probabilisticConfirmed;
            Suspicious = suspicious;
            Absolved = absolved;
            ReductionRate = reductionRate;
        }
    }
}

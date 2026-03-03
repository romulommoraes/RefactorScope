using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Model
{
    public record ZombieAnalysisBreakdown(
        int StructuralCandidates,
        int ProbabilisticConfirmed,
        int Absolved,
        double ReductionRate
    );
}

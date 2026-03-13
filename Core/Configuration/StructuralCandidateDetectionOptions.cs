using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Configuration
{
    public class StructuralCandidateDetectionOptions
    {
        public bool EnableRefinement { get; set; } = true;

        public double GlobalRateThreshold_DI { get; set; } = 0.20;
        public double GlobalRateThreshold_Interface { get; set; } = 0.15;

        public double DIProbability { get; set; } = 0.15;
        public double InterfaceProbability { get; set; } = 0.25;

        /// <summary>
        /// Acima desse valor o tipo permanece classificado como Unresolved Candidate.
        /// </summary>
        public double MinUnresolvedProbabilityThreshold { get; set; } = 0.60;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Configuration
{
    public class ZombieDetectionOptions
    {
        public bool EnableRefinement { get; set; } = true;

        public double GlobalRateThreshold_DI { get; set; } = 0.20;
        public double GlobalRateThreshold_Interface { get; set; } = 0.15;

        public double DIProbability { get; set; } = 0.15;
        public double InterfaceProbability { get; set; } = 0.25;

        /// <summary>
        /// Acima desse valor o tipo continua sendo considerado Zombie Confirmado.
        /// </summary>
        public double MinZombieProbabilityThreshold { get; set; } = 0.60;
    }
}

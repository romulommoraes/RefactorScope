using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Results
{
    public class ZombieProbabilityItem
    {
        public string TypeName { get; }

        public double Probability { get; }

        public string ConfidenceLevel { get; }

        public bool DiDetected { get; }

        public bool InterfaceDetected { get; }

        public ZombieProbabilityItem(
            string typeName,
            double probability,
            string confidenceLevel,
            bool diDetected,
            bool interfaceDetected)
        {
            TypeName = typeName;
            Probability = probability;
            ConfidenceLevel = confidenceLevel;
            DiDetected = diDetected;
            InterfaceDetected = interfaceDetected;
        }
    }
}

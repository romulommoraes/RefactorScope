using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Analyzers.Solid
{
    public sealed class SolidSuspicion
    {
        public SolidPrinciple Principle { get; init; }

        public string ClassName { get; init; } = string.Empty;

        public string Namespace { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;
    }
}

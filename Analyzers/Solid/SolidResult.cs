using System;
using System.Collections.Generic;
using System.Text;
using RefactorScope.Core.Abstractions;

namespace RefactorScope.Analyzers.Solid
{
    public sealed class SolidResult : IAnalysisResult
    {
        public IReadOnlyList<SolidSuspicion> Alerts { get; }

        public SolidResult(List<SolidSuspicion> alerts)
        {
            Alerts = alerts;
        }
    }
}

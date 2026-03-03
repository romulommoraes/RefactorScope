using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Analyzers.Solid
{
    public sealed class SolidConfig
    {
        public int SrpFanOutThreshold { get; init; } = 15;

        public List<string> OrchestratorSuffixes { get; init; } =
            new() { "Controller", "Facade", "Module", "Bootstrapper" };

        public List<string> AllowedCoreDependencies { get; init; } =
            new() { "System", "Microsoft" };
    }
}

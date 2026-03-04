namespace RefactorScope.Core.Model
{
        public sealed class HygieneReport
        {
            public int TotalClasses { get; }

            public int UnreferencedCount { get; }

            public int GlobalNamespaceCount { get; }

            public int NamespaceDriftCount { get; }

            public int IsolatedCoreCount { get; }

            /// <summary>
            /// Shannon entropy normalizada (0–1)
            /// </summary>
            public double StructuralEntropy { get; }

            /// <summary>
            /// Índice composto (0–100)
            /// </summary>
            public double SmellIndex { get; }

            public string HygieneLevel =>
                SmellIndex switch
                {
                    <= 20 => "🟢 Healthy",
                    <= 40 => "🟡 Stable",
                    <= 60 => "🟠 Degrading",
                    <= 80 => "🔴 Critical",
                    _ => "🔥 Structural Risk"
                };

            public HygieneReport(
                int totalClasses,
                int unreferencedCount,
                int globalNamespaceCount,
                int namespaceDriftCount,
                int isolatedCoreCount,
                double structuralEntropy)
            {
                TotalClasses = totalClasses;
                UnreferencedCount = unreferencedCount;
                GlobalNamespaceCount = globalNamespaceCount;
                NamespaceDriftCount = namespaceDriftCount;
                IsolatedCoreCount = isolatedCoreCount;
                StructuralEntropy = structuralEntropy;

                var deadRatio = totalClasses == 0 ? 0 : unreferencedCount / (double)totalClasses;
                var driftRatio = totalClasses == 0 ? 0 : namespaceDriftCount / (double)totalClasses;
                var isolationRatio = totalClasses == 0 ? 0 : isolatedCoreCount / (double)totalClasses;

                SmellIndex =
                    (deadRatio * 40)
                    + (driftRatio * 20)
                    + (isolationRatio * 20)
                    + (structuralEntropy * 20);
            }
        }
    }

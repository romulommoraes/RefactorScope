/// <summary>
/// Aggregated architectural hygiene report for a project.
///
/// The SmellIndex (0–100) is a composite indicator representing the
/// overall structural health of the codebase.
///
/// Current calculation uses only metrics intrinsic to HygieneReport,
/// ensuring the model remains self-contained and independent from
/// external analyzers.
///
/// Current components:
/// 
/// • Dead Code Ratio (Unreferenced classes)
/// • Namespace Drift Ratio (namespace vs folder misalignment)
/// • Global Namespace Ratio (legacy structural usage)
/// • Core Isolation Ratio (core architectural boundary integrity)
/// • Structural Entropy (distribution disorder across modules)
///
/// These signals represent fundamental structural hygiene indicators.
/// The index is intentionally lightweight to provide a stable baseline
/// metric for the RefactorScope MVP.
///
/// Future extensions may incorporate additional architectural signals
/// without breaking the current implementation. Examples include:
///
/// • Coupling metrics (fan-out / instability)
/// • SOLID design alerts
/// • Pattern similarity clusters
/// • Unresolved structural candidates
/// • Architectural tension (distance from Main Sequence)
///
/// These can be integrated as additional weighted components,
/// preserving backward compatibility with the existing index.
///
/// Example future formula:
///
/// SmellIndex =
///     BaseHygieneScore
///     + CouplingScore
///     + DesignQualityScore
///
/// Where BaseHygieneScore corresponds to the current calculation.
/// </summary>


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

            StructuralEntropy = Math.Clamp(structuralEntropy, 0, 1);

            double deadRatio =
                totalClasses == 0 ? 0 :
                unreferencedCount / (double)totalClasses;

            double driftRatio =
                totalClasses == 0 ? 0 :
                namespaceDriftCount / (double)totalClasses;

            double globalRatio =
                totalClasses == 0 ? 0 :
                globalNamespaceCount / (double)totalClasses;

            double isolationRatio =
                totalClasses == 0 ? 0 :
                isolatedCoreCount / (double)totalClasses;

            SmellIndex = Math.Clamp(

                (deadRatio * 35) +
                (driftRatio * 20) +
                (globalRatio * 10) +
                (isolationRatio * 15) +
                (StructuralEntropy * 20),

                0,
                100
            );
        }
    }
}
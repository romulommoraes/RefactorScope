namespace RefactorScope.Core.Analyzers
{
    public sealed class HygieneReport
    {
        public int TotalClasses { get; init; }

        public int DeadCount { get; init; }

        public int LegacyCount { get; init; }

        public int CoreCount { get; init; }

        public int RemovalCandidates { get; init; }

        public int IsolatedCoreCount { get; init; }

        /// <summary>
        /// Shannon entropy normalizada (0–1).
        /// Mede dispersão estrutural entre estados.
        /// </summary>
        public double StructuralEntropy { get; init; }

        /// <summary>
        /// Índice composto (0–100).
        /// Quanto maior, pior a higiene arquitetural.
        /// </summary>
        public double SmellIndex { get; init; }

        /// <summary>
        /// Interpretação textual do Smell Index.
        /// </summary>
        public string HygieneLevel =>
            SmellIndex switch
            {
                <= 20 => "🟢 Healthy",
                <= 40 => "🟡 Attention",
                <= 60 => "🟠 Degrading",
                <= 80 => "🔴 Critical",
                _ => "🔥 Structural Collapse"
            };
    }
}
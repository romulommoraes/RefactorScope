using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Analyzers;
using RefactorScope.Core.Metrics;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Centraliza cálculos auxiliares da camada de exportação.
    ///
    /// Intenção arquitetural atual
    /// ---------------------------
    /// Esta classe existe para retirar dos exporters HTML a lógica de
    /// agregação, normalização e derivação de métricas executivas.
    ///
    /// Em outras palavras:
    /// - exporters devem renderizar
    /// - esta classe deve preparar números, bandas, médias e narrativas
    ///
    /// Observação importante
    /// ---------------------
    /// Esta NÃO é a arquitetura final ideal.
    ///
    /// O desenho arquitetural mais puro, no futuro, tende a migrar parte
    /// dessas responsabilidades para uma camada própria de snapshot /
    /// projection / analysis summary fora dos exporters, ou até para
    /// resultados analíticos específicos.
    ///
    /// Ainda assim, neste estágio do projeto, esta classe oferece um ganho
    /// importante:
    ///
    /// - reduz acoplamento visual + cálculo dentro dos exporters
    /// - concentra “trabalho sujo” em um único ponto
    /// - facilita revisão metodológica posterior
    /// - mantém o Core limpo
    ///
    /// Estratégia prática
    /// ------------------
    /// Esta classe calcula:
    /// - métricas executivas do Hub
    /// - métricas agregadas do dashboard arquitetural
    /// - bandas heurísticas de suporte
    /// - médias, scores e narrativas prontas para renderização
    ///
    /// Limite deliberado
    /// -----------------
    /// Esta classe não gera HTML, não conhece CSS e não escreve arquivos.
    /// Ela apenas consolida dados para consumo dos exporters.
    /// </summary>
    internal static class DashboardMetricsCalculator
    {
        // ==========================================================
        // HUB
        // ==========================================================

        public static HubDashboardMetrics BuildHubMetrics(
            ConsolidatedReport report,
            string parserName,
            double parserConfidence,
            TimeSpan parsingExecution,
            int parsingFiles,
            int parsingTypes,
            int parsingReferences)
        {
            var hygiene = new ArchitecturalHygieneAnalyzer().Analyze(report);
            var structural = report.GetStructuralCandidateBreakdown();
            var coupling = report.GetResult<CouplingResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var solid = report.GetResult<SolidResult>();
            var fitness = report.GetResult<FitnessGateResult>();
            var effortResult = report.GetResult<EffortEstimateResult>();
            var effort = effortResult?.Estimate;

            var deadCodeCandidates = structural.StructuralCandidates;
            var unresolved = structural.ProbabilisticConfirmed;
            var patternSimilarity = structural.PatternSimilarity;
            var suspicious = structural.Suspicious;

            var couplingSuspects = implicitCoupling?.Suspicions.Count ?? 0;
            var solidAlerts = solid?.Alerts.Count ?? 0;

            double avgAbstractness = 0;
            double avgInstability = 0;
            double avgDistance = 0;

            if (coupling != null && coupling.AbstractnessByModule.Any())
            {
                avgAbstractness = coupling.AbstractnessByModule.Values.Average();
                avgInstability = coupling.InstabilityByModule.Values.Average();
                avgDistance = coupling.DistanceByModule.Values.Average();
            }

            var fitStatus = fitness == null
                ? "Unknown"
                : fitness.HasFailure ? "Attention Required" : "Ready";

            // ------------------------------------------------------
            // suporte heurístico
            // ------------------------------------------------------

            var parserSupportScore = Clamp01(parserConfidence);
            var parserSupportBand = GetSupportBandFromScore(parserSupportScore);

            var structuralSupportScore = CalculateStructuralSupportScore(
                deadCodeCandidates,
                unresolved,
                patternSimilarity);

            var structuralSupportBand = GetSupportBandFromScore(structuralSupportScore);

            var couplingSupportScore = CalculateCouplingSupportScore(
                parserConfidence,
                hygiene.TotalClasses,
                parsingReferences,
                coupling);

            var couplingSupportBand = GetSupportBandFromScore(couplingSupportScore);

            var effortSupportScore = effort?.Confidence ?? 0;
            var effortSupportBand = GetSupportBandFromScore(effortSupportScore);

            var overallSupportScore = Average(
                parserSupportScore,
                structuralSupportScore,
                couplingSupportScore,
                effortSupportScore);

            var overallSupportBand = GetSupportBandFromScore(overallSupportScore);

            var supportNarrative = BuildSupportNarrative(
                parserSupportBand,
                structuralSupportBand,
                couplingSupportBand,
                effortSupportBand,
                overallSupportBand);

            // ------------------------------------------------------
            // mensagens executivas
            // ------------------------------------------------------

            var unresolvedMessage = unresolved > 0
                ? "Some dead code candidates remain unresolved after refinement and still deserve manual review."
                : "No unresolved dead code candidates remain after refinement.";

            var coreMessage = hygiene.IsolatedCoreCount > 0
                ? "Core isolation signals are present and architectural boundaries appear reasonably preserved."
                : "Core isolation signals were not detected. Review possible leakage toward outer layers.";

            var driftMessage = hygiene.NamespaceDriftCount > 0
                ? "Namespace drift was detected. Folder structure and namespace alignment deserve review."
                : "Namespace hierarchy appears aligned with the physical project structure.";

            var smellMessage = hygiene.SmellIndex <= 20
                ? "Code smell index remains in a healthy range."
                : hygiene.SmellIndex <= 40
                    ? "Code smell index suggests moderate architectural attention."
                    : "Code smell index suggests elevated structural degradation risk.";

            return new HubDashboardMetrics
            {
                ParserName = parserName,
                ParserConfidence = parserConfidence,
                ParsingExecution = parsingExecution,
                ParsingFiles = parsingFiles,
                ParsingTypes = parsingTypes,
                ParsingReferences = parsingReferences,

                Hygiene = hygiene,
                DeadCodeCandidates = deadCodeCandidates,
                Unresolved = unresolved,
                PatternSimilarity = patternSimilarity,
                Suspicious = suspicious,

                CouplingSuspects = couplingSuspects,
                SolidAlerts = solidAlerts,

                AvgAbstractness = avgAbstractness,
                AvgInstability = avgInstability,
                AvgDistance = avgDistance,

                FitStatus = fitStatus,

                ParserSupportScore = parserSupportScore,
                ParserSupportBand = parserSupportBand,

                StructuralSupportScore = structuralSupportScore,
                StructuralSupportBand = structuralSupportBand,

                CouplingSupportScore = couplingSupportScore,
                CouplingSupportBand = couplingSupportBand,

                EffortSupportScore = effortSupportScore,
                EffortSupportBand = effortSupportBand,

                OverallSupportScore = overallSupportScore,
                OverallSupportBand = overallSupportBand,

                SupportNarrative = supportNarrative,

                UnresolvedMessage = unresolvedMessage,
                CoreMessage = coreMessage,
                DriftMessage = driftMessage,
                SmellMessage = smellMessage,

                EffortDifficulty = effort?.Difficulty ?? "Unknown",
                EffortHours = effort?.EstimatedHours ?? 0,
                EffortConfidence = effort?.Confidence ?? 0,
                EffortRdi = effort?.RDI ?? 0
            };
        }

        // ==========================================================
        // ARCHITECTURAL
        // ==========================================================

        public static ArchitecturalDashboardMetrics BuildArchitecturalMetrics(
            ConsolidatedReport report)
        {
            var architecture = report.GetResult<ArchitecturalClassificationResult>();
            var isolated = report.GetResult<CoreIsolationResult>();
            var coupling = report.GetResult<CouplingResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var fitness = report.GetResult<FitnessGateResult>();

            var structuralCandidates = report.GetStructuralCandidates();
            var patternSimilarity = report.GetPatternSimilarityCandidates();
            var unresolved = report.GetEffectiveUnresolvedCandidates();

            var modules = architecture?.Items
                .GroupBy(i => i.Folder)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<IGrouping<string, ArchitecturalClassificationItem>>();

            var avgScore = CalculateAverageArchitecturalScore(
                modules,
                unresolved,
                isolated,
                coupling);

            var avgAbstractness = coupling?.AbstractnessByModule.Any() == true
                ? coupling.AbstractnessByModule.Values.Average()
                : 0;

            var avgInstability = coupling?.InstabilityByModule.Any() == true
                ? coupling.InstabilityByModule.Values.Average()
                : 0;

            var avgDistance = coupling?.DistanceByModule.Any() == true
                ? coupling.DistanceByModule.Values.Average()
                : 0;

            var fitStatus = fitness == null
                ? "Unknown"
                : fitness.HasFailure ? "Attention Required" : "Ready";

            return new ArchitecturalDashboardMetrics
            {
                Architecture = architecture,
                Isolated = isolated,
                Coupling = coupling,
                ImplicitCoupling = implicitCoupling,
                Fitness = fitness,

                StructuralCandidates = structuralCandidates,
                PatternSimilarity = patternSimilarity,
                Unresolved = unresolved,

                Modules = modules,

                AverageScore = avgScore,
                AverageAbstractness = avgAbstractness,
                AverageInstability = avgInstability,
                AverageDistance = avgDistance,

                FitStatus = fitStatus
            };
        }

        // ==========================================================
        // HELPERS PÚBLICOS DE APRESENTAÇÃO
        // ==========================================================

        public static string GetSupportBandFromScore(double score)
        {
            if (score >= 0.80) return "High";
            if (score >= 0.55) return "Moderate";
            return "Low";
        }

        public static string GetBandCssClass(double score)
        {
            if (score >= 0.80) return "good";
            if (score >= 0.55) return "warning";
            return "alert";
        }

        public static string GetBandBadgeCss(double score)
        {
            if (score >= 0.80) return "green";
            if (score >= 0.55) return "amber";
            return "red";
        }

        // ==========================================================
        // HELPERS DE CÁLCULO
        // ==========================================================

        /// <summary>
        /// Mede quão bem a hipótese de dead code foi refinada.
        ///
        /// Observação metodológica:
        /// ------------------------
        /// Este score não representa “verdade”, e sim força heurística
        /// da redução obtida ao longo do pipeline:
        ///
        /// Structural Candidates
        ///        →
        /// Pattern Similarity
        ///        →
        /// Unresolved
        ///
        /// Quanto menor o remanescente unresolved e maior a filtragem por
        /// similaridade estrutural, maior tende a ser o suporte para a leitura.
        /// </summary>
        private static double CalculateStructuralSupportScore(
            int deadCodeCandidates,
            int unresolved,
            int patternSimilarity)
        {
            if (deadCodeCandidates <= 0)
                return 0.75;

            var unresolvedRatio = unresolved / (double)deadCodeCandidates;
            var patternRatio = patternSimilarity / (double)deadCodeCandidates;

            var refinementScore = 1.0 - unresolvedRatio;
            var patternScore = Math.Min(1.0, patternRatio);

            return Clamp01((refinementScore * 0.70) + (patternScore * 0.30));
        }

        /// <summary>
        /// Mede suporte heurístico para leitura de coupling.
        ///
        /// Importante:
        /// ----------
        /// Este score NÃO tenta dizer se o sistema “está bonito”.
        /// Ele tenta dizer se existe base suficiente para interpretar
        /// a análise de coupling com confiança razoável.
        ///
        /// Por isso, ele se apoia em:
        /// - confiança do parser
        /// - tamanho do escopo
        /// - densidade de referências
        /// - cobertura modular do resultado de coupling
        ///
        /// Em outras palavras:
        /// coupling ruim ≠ support baixo
        /// coupling intenso pode ser real e bem suportado
        /// </summary>
        private static double CalculateCouplingSupportScore(
            double parserConfidence,
            int totalClasses,
            int totalReferences,
            CouplingResult? coupling)
        {
            var confidenceScore = Clamp01(parserConfidence);

            var scopeScore = totalClasses switch
            {
                >= 150 => 0.90,
                >= 80 => 0.75,
                >= 30 => 0.60,
                >= 10 => 0.45,
                _ => 0.30
            };

            var density = totalClasses <= 0 ? 0 : totalReferences / (double)totalClasses;
            var densityScore = density switch
            {
                >= 2.5 => 0.90,
                >= 1.5 => 0.75,
                >= 0.8 => 0.60,
                >= 0.3 => 0.45,
                _ => 0.25
            };

            var moduleCoverage = coupling?.ModuleFanOut?.Count ?? 0;
            var moduleCoverageScore = moduleCoverage switch
            {
                >= 8 => 0.90,
                >= 5 => 0.75,
                >= 3 => 0.60,
                >= 2 => 0.45,
                _ => 0.30
            };

            return Clamp01(
                (confidenceScore * 0.40) +
                (scopeScore * 0.20) +
                (densityScore * 0.25) +
                (moduleCoverageScore * 0.15));
        }

        private static double Average(params double[] values)
        {
            var valid = values
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .ToArray();

            if (valid.Length == 0)
                return 0;

            return valid.Average();
        }

        private static double Clamp01(double value)
            => Math.Max(0, Math.Min(1, value));

        private static string BuildSupportNarrative(
            string parserSupport,
            string structuralSupport,
            string couplingSupport,
            string effortSupport,
            string overallSupport)
        {
            return
                $"The current analysis has {overallSupport.ToLowerInvariant()} overall heuristic support. " +
                $"Parsing confidence is {parserSupport.ToLowerInvariant()}, structural classification is {structuralSupport.ToLowerInvariant()}, " +
                $"coupling interpretation is {couplingSupport.ToLowerInvariant()}, and effort estimates should be read with {effortSupport.ToLowerInvariant()} support.";
        }

        private static double CalculateAverageArchitecturalScore(
            IReadOnlyList<IGrouping<string, ArchitecturalClassificationItem>> modules,
            IReadOnlyList<string> unresolved,
            CoreIsolationResult? isolated,
            CouplingResult? coupling)
        {
            if (modules.Count == 0)
                return 0;

            var scores = new List<double>();

            foreach (var module in modules)
            {
                var total = module.Count();
                if (total == 0)
                    continue;

                var unresolvedCount = unresolved.Count(z => module.Any(m => m.TypeName == z));
                var isolatedCount = isolated?.IsolatedCoreTypes.Count(i => module.Any(m => m.TypeName == i)) ?? 0;
                var fanOut = coupling?.ModuleFanOut.GetValueOrDefault(module.Key) ?? 0;
                var coreCount = module.Count(t => t.Layer == "Core");

                var score = ArchitecturalScoreCalculator.Calculate(
                    module.Key,
                    total,
                    unresolvedCount,
                    isolatedCount,
                    fanOut,
                    coreCount);

                scores.Add(Math.Max(0, Math.Min(100, score)));
            }

            return scores.Count == 0 ? 0 : scores.Average();
        }
    }

    /// <summary>
    /// Snapshot executivo usado pelo Hub.
    ///
    /// Observação:
    /// -----------
    /// Este objeto existe para manter o exporter enxuto.
    /// No futuro, ele pode ser substituído por um snapshot mais formal,
    /// possivelmente fora da camada Exporters.
    /// </summary>
    internal sealed class HubDashboardMetrics
    {
        public string ParserName { get; init; } = "Unknown";
        public double ParserConfidence { get; init; }
        public TimeSpan ParsingExecution { get; init; }

        public int ParsingFiles { get; init; }
        public int ParsingTypes { get; init; }
        public int ParsingReferences { get; init; }

        public required HygieneReport Hygiene { get; init; }

        public int DeadCodeCandidates { get; init; }
        public int Unresolved { get; init; }
        public int PatternSimilarity { get; init; }
        public int Suspicious { get; init; }

        public int CouplingSuspects { get; init; }
        public int SolidAlerts { get; init; }

        public double AvgAbstractness { get; init; }
        public double AvgInstability { get; init; }
        public double AvgDistance { get; init; }

        public string FitStatus { get; init; } = "Unknown";

        public double ParserSupportScore { get; init; }
        public string ParserSupportBand { get; init; } = "Unknown";

        public double StructuralSupportScore { get; init; }
        public string StructuralSupportBand { get; init; } = "Unknown";

        public double CouplingSupportScore { get; init; }
        public string CouplingSupportBand { get; init; } = "Unknown";

        public double EffortSupportScore { get; init; }
        public string EffortSupportBand { get; init; } = "Unknown";

        public double OverallSupportScore { get; init; }
        public string OverallSupportBand { get; init; } = "Unknown";

        public string SupportNarrative { get; init; } = string.Empty;

        public string UnresolvedMessage { get; init; } = string.Empty;
        public string CoreMessage { get; init; } = string.Empty;
        public string DriftMessage { get; init; } = string.Empty;
        public string SmellMessage { get; init; } = string.Empty;

        public string EffortDifficulty { get; init; } = "Unknown";
        public double EffortHours { get; init; }
        public double EffortConfidence { get; init; }
        public double EffortRdi { get; init; }
    }

    /// <summary>
    /// Snapshot agregado usado pelo dashboard arquitetural.
    ///
    /// Objetivo:
    /// ---------
    /// Entregar ao exporter arquitetural tudo o que ele precisa
    /// para renderizar sem recalcular agregações localmente.
    /// </summary>
    internal sealed class ArchitecturalDashboardMetrics
    {
        public ArchitecturalClassificationResult? Architecture { get; init; }
        public CoreIsolationResult? Isolated { get; init; }
        public CouplingResult? Coupling { get; init; }
        public ImplicitCouplingResult? ImplicitCoupling { get; init; }
        public FitnessGateResult? Fitness { get; init; }

        public IReadOnlyList<string> StructuralCandidates { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> PatternSimilarity { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Unresolved { get; init; } = Array.Empty<string>();

        public IReadOnlyList<IGrouping<string, ArchitecturalClassificationItem>> Modules { get; init; }
            = Array.Empty<IGrouping<string, ArchitecturalClassificationItem>>();

        public double AverageScore { get; init; }
        public double AverageAbstractness { get; init; }
        public double AverageInstability { get; init; }
        public double AverageDistance { get; init; }

        public string FitStatus { get; init; } = "Unknown";
    }
}
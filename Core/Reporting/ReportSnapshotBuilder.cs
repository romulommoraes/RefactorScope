using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;
using RefactorScope.Exporters.Dashboards;


namespace RefactorScope.Core.Reporting
{
    public static class ReportSnapshotBuilder
    {
        public static ReportSnapshot Build(
            ConsolidatedReport report,
            IParserResult? parserResult)
        {
            var structural = report.GetStructuralCandidateBreakdown();
            var architecture = DashboardMetricsCalculator.BuildArchitecturalMetrics(report);
            var quality = BuildQualitySnapshot(report, parserResult);
            var effort = BuildEffortSnapshot(report);

            return new ReportSnapshot
            {
                TargetScope = report.TargetScope,
                ExecutionTimeUtc = report.ExecutionTime,

                Parsing = BuildParsingSnapshot(parserResult),
                Structural = new ExecutiveStructuralSnapshot
                {
                    StructuralCandidates = structural.StructuralCandidates,
                    PatternSimilarity = structural.PatternSimilarity,
                    Unresolved = structural.ProbabilisticConfirmed,
                    Suspicious = structural.Suspicious,
                    ReductionRate = structural.ReductionRate
                },
                Architectural = new ExecutiveArchitecturalSnapshot
                {
                    Modules = architecture.Modules.Count,
                    AverageScore = architecture.AverageScore,
                    AverageAbstractness = architecture.AverageAbstractness,
                    AverageInstability = architecture.AverageInstability,
                    AverageDistance = architecture.AverageDistance,
                    ImplicitCouplingSuspects = architecture.ImplicitCoupling?.Suspicions.Count ?? 0
                },
                Quality = quality,
                Effort = effort
            };
        }

        private static ExecutiveParsingSnapshot BuildParsingSnapshot(IParserResult? parserResult)
        {
            if (parserResult == null)
            {
                return new ExecutiveParsingSnapshot();
            }

            var files = parserResult.Model?.Arquivos.Count ?? 0;
            var types = parserResult.Model?.Tipos.Count ?? 0;
            var refs = parserResult.Model?.Referencias.Count ?? 0;

            double executionMs = parserResult.Stats?.ExecutionTime.TotalMilliseconds ?? 0;
            double typesPerFile = files == 0 ? 0 : types / (double)files;
            double refsPerType = types == 0 ? 0 : refs / (double)types;
            double msPerFile = files == 0 ? 0 : executionMs / files;
            double msPerType = types == 0 ? 0 : executionMs / types;

            long memoryBytes = TryGetMemoryBytes(parserResult);
            bool anomalyDetected = TryGetAnomalyDetected(parserResult);
            bool sparseExtraction = refsPerType < 0.80 || typesPerFile < 0.50;

            double extractionIndex = ComputeExtractionIndex(
                parserResult.Confidence,
                refsPerType,
                typesPerFile,
                msPerType);

            return new ExecutiveParsingSnapshot
            {
                ParserName = parserResult.ParserName,
                ParserConfidence = parserResult.Confidence,
                ConfidenceBand = GetConfidenceBand(parserResult.Confidence),

                Files = files,
                Types = types,
                References = refs,

                ExecutionMs = executionMs,
                TypesPerFile = typesPerFile,
                ReferencesPerType = refsPerType,
                MsPerFile = msPerFile,
                MsPerType = msPerType,

                MemoryBytes = memoryBytes,
                SparseExtraction = sparseExtraction,
                AnomalyDetected = anomalyDetected,
                ExtractionIndex = extractionIndex,

                ConfidenceDiagnosis = GetConfidenceDiagnosis(parserResult.Confidence),
                DensityDiagnosis = GetDensityDiagnosis(refsPerType, typesPerFile),
                PerformanceDiagnosis = GetPerformanceDiagnosis(msPerType, msPerFile),
                SparseExtractionDiagnosis = GetSparseExtractionDiagnosis(sparseExtraction),
                AnomalyDiagnosis = GetAnomalyDiagnosis(anomalyDetected)
            };
        }

        private static ExecutiveQualitySnapshot BuildQualitySnapshot(
            ConsolidatedReport report,
            IParserResult? parserResult)
        {
            var parserName = parserResult?.ParserName ?? "Unknown";
            var parserConfidence = parserResult?.Confidence ?? 0;
            var parsingExecution = parserResult?.Stats?.ExecutionTime ?? TimeSpan.Zero;
            var parsingFiles = parserResult?.Model?.Arquivos.Count ?? 0;
            var parsingTypes = parserResult?.Model?.Tipos.Count ?? 0;
            var parsingReferences = parserResult?.Model?.Referencias.Count ?? 0;

            var qualityMetrics = DashboardMetricsCalculator.BuildQualityMetrics(
                report,
                parserName,
                parserConfidence,
                parsingExecution,
                parsingFiles,
                parsingTypes,
                parsingReferences);

            return new ExecutiveQualitySnapshot
            {
                FitStatus = qualityMetrics.FitStatus,
                SolidAlerts = qualityMetrics.Hub.SolidAlerts,
                StatisticsCoverageScore = qualityMetrics.StatisticsCoverageScore,
                StatisticsCoverageBand = qualityMetrics.StatisticsCoverageBand,
                OverallReadinessScore = qualityMetrics.OverallReadinessScore,
                OverallReadinessBand = qualityMetrics.OverallReadinessBand
            };
        }

        private static ExecutiveEffortSnapshot BuildEffortSnapshot(ConsolidatedReport report)
        {
            var effortResult = report.GetResult<EffortEstimateResult>();
            var effort = effortResult?.Estimate;

            return new ExecutiveEffortSnapshot
            {
                Hours = effort?.EstimatedHours ?? 0,
                Confidence = effort?.Confidence ?? 0,
                Difficulty = effort?.Difficulty ?? "Unknown",
                Rdi = effort?.RDI ?? 0
            };
        }

        private static double ComputeExtractionIndex(
            double confidence,
            double refsPerType,
            double typesPerFile,
            double msPerType)
        {
            var densityScore = Math.Min(1.0, refsPerType / 3.0);
            var structureScore = Math.Min(1.0, typesPerFile / 4.0);
            var performanceScore = msPerType <= 0
                ? 1.0
                : Math.Max(0, 1.0 - Math.Min(1.0, msPerType / 50.0));

            return ((confidence * 0.5) + (densityScore * 0.25) + (structureScore * 0.15) + (performanceScore * 0.10)) * 100.0;
        }

        private static string GetConfidenceBand(double confidence)
        {
            if (confidence >= 0.85) return "High";
            if (confidence >= 0.65) return "Medium";
            return "Low";
        }

        private static bool TryGetAnomalyDetected(IParserResult result)
        {
            try
            {
                var stats = result.Stats;
                if (stats == null)
                    return false;

                var prop = stats.GetType().GetProperty("AnomalyDetected");
                if (prop == null)
                    return false;

                var value = prop.GetValue(stats);
                return value is bool flag && flag;
            }
            catch
            {
                return false;
            }
        }

        private static long TryGetMemoryBytes(IParserResult result)
        {
            try
            {
                var stats = result.Stats;
                if (stats == null)
                    return 0;

                var prop = stats.GetType().GetProperty("MemoryBytes")
                          ?? stats.GetType().GetProperty("AllocatedBytes")
                          ?? stats.GetType().GetProperty("MemoryUsageBytes")
                          ?? stats.GetType().GetProperty("EstimatedMemoryBytes");

                if (prop == null)
                    return 0;

                var value = prop.GetValue(stats);
                return value switch
                {
                    long l => l,
                    int i => i,
                    double d => (long)d,
                    float f => (long)f,
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        private static string GetConfidenceDiagnosis(double confidence)
        {
            if (confidence >= 0.85)
                return "Parser confidence is high. The extracted structural model appears reliable for downstream analysis.";

            if (confidence >= 0.65)
                return "Parser confidence is moderate. The extracted model appears usable, but some structural loss may exist.";

            return "Parser confidence is low. Review extraction quality before relying on downstream architectural conclusions.";
        }

        private static string GetDensityDiagnosis(double refsPerType, double typesPerFile)
        {
            if (refsPerType < 0.80)
                return "Reference density is low. The parse may still be structurally sparse, or the target codebase may expose fewer explicit dependencies.";

            if (typesPerFile < 0.50)
                return "Type density per file is low. This may indicate sparse source structure or partial extraction coverage.";

            return "Structural density appears healthy. Parsed types and references show consistent extraction volume.";
        }

        private static string GetPerformanceDiagnosis(double msPerType, double msPerFile)
        {
            if (msPerType > 25 || msPerFile > 80)
                return "Parsing cost is elevated for this scope. Compare against other parser strategies if performance becomes relevant.";

            return "Parsing cost remains in an acceptable range for the extracted structural volume.";
        }

        private static string GetSparseExtractionDiagnosis(bool sparseExtraction)
            => sparseExtraction
                ? "Sparse extraction was detected. This does not prove parser failure, but the structural graph may be thinner than expected."
                : "No sparse extraction signal was detected. Structural density appears compatible with a healthy parse.";

        private static string GetAnomalyDiagnosis(bool anomalyDetected)
            => anomalyDetected
                ? "An anomaly flag was raised during parsing. Review this execution before treating the extracted model as fully representative."
                : "No anomaly flag was raised during parsing telemetry.";
    }
}
namespace RefactorScope.Core.Reporting
{
    public sealed class ReportSnapshot
    {
        public required string TargetScope { get; init; }
        public required DateTime ExecutionTimeUtc { get; init; }

        public required ExecutiveParsingSnapshot Parsing { get; init; }
        public required ExecutiveStructuralSnapshot Structural { get; init; }
        public required ExecutiveArchitecturalSnapshot Architectural { get; init; }
        public required ExecutiveQualitySnapshot Quality { get; init; }
        public required ExecutiveEffortSnapshot Effort { get; init; }
    }

    public sealed class ExecutiveParsingSnapshot
    {
        public string ParserName { get; init; } = "Unknown";
        public double ParserConfidence { get; init; }
        public string ConfidenceBand { get; init; } = "Unknown";

        public int Files { get; init; }
        public int Types { get; init; }
        public int References { get; init; }

        public double ExecutionMs { get; init; }
        public double TypesPerFile { get; init; }
        public double ReferencesPerType { get; init; }
        public double MsPerFile { get; init; }
        public double MsPerType { get; init; }

        public long MemoryBytes { get; init; }
        public bool SparseExtraction { get; init; }
        public bool AnomalyDetected { get; init; }
        public double ExtractionIndex { get; init; }

        public string ConfidenceDiagnosis { get; init; } = string.Empty;
        public string DensityDiagnosis { get; init; } = string.Empty;
        public string PerformanceDiagnosis { get; init; } = string.Empty;
        public string SparseExtractionDiagnosis { get; init; } = string.Empty;
        public string AnomalyDiagnosis { get; init; } = string.Empty;
    }

    public sealed class ExecutiveStructuralSnapshot
    {
        public int StructuralCandidates { get; init; }
        public int PatternSimilarity { get; init; }
        public int Unresolved { get; init; }
        public int Suspicious { get; init; }
        public double ReductionRate { get; init; }
    }

    public sealed class ExecutiveArchitecturalSnapshot
    {
        public int Modules { get; init; }
        public double AverageScore { get; init; }
        public double AverageAbstractness { get; init; }
        public double AverageInstability { get; init; }
        public double AverageDistance { get; init; }
        public int ImplicitCouplingSuspects { get; init; }
    }

    public sealed class ExecutiveQualitySnapshot
    {
        public string FitStatus { get; init; } = "Unknown";
        public int SolidAlerts { get; init; }
        public double StatisticsCoverageScore { get; init; }
        public string StatisticsCoverageBand { get; init; } = "Unknown";
        public double OverallReadinessScore { get; init; }
        public string OverallReadinessBand { get; init; } = "Unknown";
    }

    public sealed class ExecutiveEffortSnapshot
    {
        public double Hours { get; init; }
        public double Confidence { get; init; }
        public string Difficulty { get; init; } = "Unknown";
        public double Rdi { get; init; }
    }
}
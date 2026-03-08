namespace RefactorScope.Core.Metrics;

public record ArchitecturalMetrics(
    double MeanCoupling,
    double UnresolvedCandidateRatio,
    double NamespaceDriftRatio
);
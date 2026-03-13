# RefactorScope Test Suite Documentation
## Objective

This document describes the current state of the RefactorScope automated test suite, consolidating the functional coverage of the MVP and the recent evolution of the protection of the system's most sensitive modules.

### The objective of the suite is to:

   - `validate the expected functional behavior of the core components`

   - `reduce regressions during refactoring`

   - `make important architectural contracts explicit`

   - `provide executable documentation of the system's behavior`

### Overview

The current suite covers, with varying levels of depth, the following main domains:

**C# Parsers**

   `CSharpRegexParser`

   `CSharpTextualParser`

   `HybridSelectiveParser`

**Lexical Protection**

   `StructuralTokenGuard`

**Arena / Parser Comparison**

   `ParserArenaProjectResult`

   `ParserArenaRunResult`

   `ParserArenaScoreCalculator`

   `ParserArenaOrchestrator`

   ParserArenaCliRunner public infrastructure

**CLI Layer**

   `StartupExecutionPlanSelector`
   
   Structural coverage of the mode and parser selection flow

**Structural Analysis**

   `StructuralCandidateAnalyzer`

   `StructuralCandidateRefinementAnalyzer`

   `ConsolidatedReport`
   
   Structural snapshots and breakdowns

**Core / Results / Reporting**

   `ConsolidatedReport`

   `ReportSnapshot`

   `ReportSnapshotBuilder`
   
   snapshot extensions and structural breakdown

**Exportation**

   `main HTML exporters in basic coverage`

   `Markdown exporters`

   `JSON dumps`

   `CSV datasets`

   `export adapters`

**Execution / Dump**

   `DumpStrategyResolver`

   `GlobalDumpStrategy`

   `SegmentedDumpStrategy`

   `Statistics`

   `ValidationEngine`

   `StatisticsReport`

   `ParsingConfidence`

   `MetricsStatisticsSummary`

   `Estimation`

   `EffortEstimator`

   `RefactorClassifier`

   `RDICalculator`

   `StructuralRiskModel`

   `CouplingPressureModel`

   `SizePressureModel`

   `RefactorDifficultyIndex`

   `EffortEstimate`

**Infrastructure**

   `ConfigLoader`

   `ConfigValidator`

   `LayerRuleEvaluator`

   `CrashLogger`

   `TerminalRenderer utility parts`

## Current Executive State of Coverage

**Global Coverage**

   `Global source code coverage: 46.53%`

   `Covered lines: 14,812`

   `Total lines: 31,832`

## Executive Reading

The current coverage of RefactorScope already well protects the analytical and mathematical engines of the system. The global number is still weighed down by extensive layers of visual exportation and HTML rendering, which concentrate a lot of volume and high cyclomatic complexity.

### In practical terms:

the analytical core of the MVP is significantly more protected

the parsers and the comparative arena already have relevant functional shielding

the Estimation and Statistics modules have reached high maturity

the global average is still flattened by visual exporters, renderers, and high-volume dashboards

Coverage by Domain (Root Level)
Domain	Lines (Total)	Lines (Covered)	Coverage (%)	Status
RefactorScope.Estimation	206	188	91.26%	🟢 Good
RefactorScope.Statistics	84	66	78.57%	🟡 Attention
RefactorScope.Execution	148	98	66.22%	🟡 Attention
RefactorScope.Parsers	2,396	1,507	62.90%	🟡 Attention
RefactorScope.Core	3,514	1,718	48.89%	🔴 Critical
RefactorScope.Exporters	22,025	10,253	46.55%	🔴 Critical
RefactorScope.Analyzers	1,763	588	33.35%	🔴 Critical
RefactorScope.CLI	1,004	284	28.29%	🔴 Critical
RefactorScope.Infrastructure	692	110	15.90%	🔴 Critical
Interpretation

This distribution shows that coverage is not "shallow and scattered", but rather concentrated in the modules of highest methodological value for the MVP:

   parsing

   arena

   estimation

   statistics

   snapshots and structural consolidation

The areas that are still fragile are mostly in:

    complex HTML rendering

    visual infrastructure

    extensive exporters

    auxiliary rules less central to the analytical core

Top 5 and Bottom 5 Namespaces
Top 5 (most secure)
Namespace	Coverage
RefactorScope.Execution.Dump.Strategies	100%
RefactorScope.Execution.Dump	100%
RefactorScope.Estimation.Scoring	100%
RefactorScope.Estimation.Classification	100%
RefactorScope.Exporters.Dumps	96.67%
Bottom 5 (immediate risk)
Namespace	Coverage
RefactorScope.Core.Structure	0%
RefactorScope.Exporters.Datasets	0%
RefactorScope.Exporters.Dashboards.Renderers...	0%
RefactorScope.Core.Reporting.Export	0%
RefactorScope.Analyzers.Solid.Rules	0%
Interpretation

The most protected namespaces today are exactly the smaller, deterministic, and easy-to-isolate blocks. Those with lower coverage, on the other hand, concentrate parts that are more visual, specialized, or not yet prioritized in the MVP.
Critical Classes by Volume

The classes below concentrate a large volume of code and continue to pull the global average down:
Class	Namespace	Lines (Total)	Coverage (%)
ModuleRouteMapRenderer	RefactorScope.Exporters.Dashboards.Renderers	1680	0.00%
SimpleStructureMapRenderer	RefactorScope.Exporters.Dashboards.Renderers	1638	0.00%
StructuralInventoryExporter	RefactorScope.Exporters.Dashboards	1418	3.95%
DashboardMetricsCalculator	RefactorScope.Exporters.Projections	958	35.49%
QualityDashboardExporter	RefactorScope.Exporters.Dashboards	888	0.00%
Interpretation

These classes represent the main current statistical bottleneck. They do not necessarily signify a proportional fragility of the MVP, but they distort the global average because they are:

    extensive

    highly branched

    rich in textual/visual assembly

    less deterministic than the analytical engines

Risk Hotspots (Complexity)
Class	Method	Cyclomatic Complexity	Risk
ParserArenaDashboardExporter	GenerateHtml(...)	108	🔥 Extreme
TableRenderer	Render(...)	108	🔥 Extreme
FileSize	GetSuffix()	79	🔥 Extreme
CSharpRegexParser	PrepareStructuralScanSource(...)	72	🔥 Extreme
TerminalRenderer	ResolveModuleColor(...)	66	🔥 Extreme
Interpretation

These hotspots are the primary candidates for:

    additional smoke tests

    textual snapshot tests

    eventual post-MVP refactoring

    decomposition by responsibility

1. C# Parsers Tests
1.1 CSharpRegexParser

Covered scenarios:

    simple project returns valid model

    multiple types in the same project

    reference detection between types

    protection against lexical false positives

    fallback to Global namespace

Architectural value:

These tests establish the minimum contract of the Regex parser and protect it especially against:

    basic extraction regressions

    lexical noise

    namespace fallback breakage

1.2 CSharpTextualParser

Covered scenarios:

    textual parser basic flow

    multiple types

    references by instance and mention

    references by typeof, nameof and generics

    protection against false positives

    fallback to Global namespace

Architectural value:

Ensures that the textual parser goes beyond pure regex and maintains consistency without sacrificing protection against noise.
1.3 HybridSelectiveParser

Covered scenarios:

    fallback to textual when regex fails

    total failure when both fail

    regex baseline preservation

    merge without type duplication

    merge without reference duplication

Architectural value:

Protects the heart of hybrid parsing:

    correct fallback

    consistent merge

    stable behavior in mixed scenarios

2. Lexical Protection
StructuralTokenGuard

Covered scenarios:

    rejection of invalid tokens as types

    acceptance of plausible identifiers

    rejection of unsafe lexical patterns

Architectural value:

Acts as a low-level barrier against textual noise improperly promoted to a structural entity.
3. Arena
Main coverages

    run sorting

    BestRun selection

    tie-breakers by score/confidence/types/references/time

    zero score for failures

    penalty for fallback

    status differentiation

    score rounding

    batch path and empty batch validation

    ParserArenaCliRunner structural validations

Known gap

The ParserArenaOrchestrator still has partial full-flow coverage because it resolves concrete parsers directly.

Classification: testability debt

Urgency: low in MVP, medium post-MVP
4. CLI
StartupExecutionPlanSelector

The CLI layer gained protection for the initialization and mode selection flow, focusing on:

    scope resolution

    execution mode selection

    behavior when the interactive selector is off

    behavior in single parser vs comparative mode

Architectural value:

This block helps to shield the system's entry infrastructure without requiring invasive changes to the MVP's frozen code.
5. Structural Analysis
StructuralCandidateAnalyzer

Covered scenarios:

    orphan class enters as a structural candidate

    referenced class does not enter

    Program does not enter

    interfaces and records do not enter

    known structural patterns do not enter

StructuralCandidateRefinementAnalyzer

Covered scenarios:

    refinement off returns empty

    architectural pattern protection

    reduction via DI

    reduction via interface and polymorphism

    reference protection in Program.cs

ConsolidatedReport and breakdowns

Also covered:

    typed access by GetResult<T>()

    structural candidates

    unresolved candidates

    pattern similarity

    structural breakdown calculation

    unresolved rate

    structural snapshots derived from the consolidated report

Architectural value:

This layer formalizes the central semantics of RefactorScope's structural pipeline:
Structural Candidates -> Refinement -> Unresolved / Pattern Similarity
6. Statistics
ValidationEngine

Covered scenarios:

    null return when statistics are disabled

    division by zero protection

    ParsingConfidence calculation

    MeanCoupling calculation

    UnresolvedCandidateRatio calculation

    NamespaceDriftRatio calculation

    non-blocking execution mode

Statistics Models

Also received coverage:

    ParsingConfidence

    MetricsStatisticsSummary

    StatisticsReport

Architectural value:

The statistical module is already sufficiently protected for the MVP and functions as a stable observational layer, without critical coupling to the main flow.
7. Estimation
Covered scenarios

    RefactorClassifier

    StructuralRiskModel

    CouplingPressureModel

    SizePressureModel

    RDICalculator

    RefactorDifficultyIndex

    EffortEstimator

    EffortEstimate

Architectural value

This was one of the biggest advancements of the current phase. The estimation domain went from almost no coverage to a highly reliable state, with protection for both mathematical models and heuristic difficulty reading.
8. Core Reporting / Snapshots
Covered scenarios

    ReportSnapshot

    ReportSnapshotBuilder

    ConsolidatedReportSnapshotExtensions

    executive snapshots of:

        Parsing

        Structural

        Architectural

        Quality

        Effort

Architectural value:

This layer is important because it serves as a bridge between:

    analytical core

    Markdown exporters

    future executive documentation

    integration with GitHub Pages and possible JSON exports

9. Exporters
What is already covered

Basic and/or structural coverage was added for:

    Markdown exporters

    JSON dumps

    execution/dump strategies

    export adapters

    visual shell

    part of the main HTML exporters

    snapshots and visualization support metrics

What still weighs it down

The biggest bottlenecks are still:

    StructuralInventoryExporter

    QualityDashboardExporter

    ModuleRouteMapRenderer

    SimpleStructureMapRenderer

    larger P5/CSS renderers

Correct reading:

The export layer still concentrates a lot of volume, so the global coverage appears lower than the actual robustness of the MVP.
10. Infrastructure
Covered scenarios

    ConfigLoader

    ConfigValidator

    LayerRuleEvaluator

    CrashLogger

    parts of TerminalRenderer

Reading

Coverage is still low in the aggregate, but it is no longer zero and the most sensitive utilities of the MVP have received minimum functional protection.
11. Current Known Limitations

    full flows of some HTML exporters are not yet deeply tested

    large visual renderers continue to pull down the global average

    some very extensive classes still lack smoke or snapshot tests

    TerminalRenderer and complex renderers have high complexity and a test cost greater than their immediate value to the MVP

    specialized rules of Analyzers.Solid.Rules are still practically outside the test mesh

12. Suggested Next Steps
Short term

    add smoke tests in large exporters

    create textual/HTML snapshot tests to reduce blind spots

    tackle QualityDashboardExporter and StructuralInventoryExporter

Post-MVP

    decompose giant renderers

    decouple more points from ParserArenaOrchestrator

    expand coverage of specific SOLID rules

    review hotspots with extreme complexity

13. Conclusion

The RefactorScope test suite already protects the methodological core of the MVP with good quality.

Today, the primary correct reading is not just "46.53% global coverage", but rather:

    protected parsing

    protected arena

    strongly protected estimation

    statistics in good state

    snapshots and executive consolidation already reliable

    visual exportation still expanding

This places the project in a healthy position for an MVP launch, with objective clarity on the strengths and post-launch debts.
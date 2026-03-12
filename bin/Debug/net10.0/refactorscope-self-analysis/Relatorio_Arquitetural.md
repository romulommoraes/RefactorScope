# 🧬 RefactorScope – Architectural Report

📅 **Execution Time:** 2026-03-12 01:02 UTC  
📂 **Target Scope:** `C:\Users\romul\source\repos\RefactorScope`  

---

## 📂 Project Structure (Clean)

```
└── RefactorScope
├── Analyzers
│   └── Solid
│       └── Rules
├── CLI
├── Core
│   ├── Abstractions
│   ├── Configuration
│   ├── Context
│   ├── Datasets
│   ├── Governance
│   ├── Metrics
│   ├── Model
│   ├── Orchestration
│   ├── Parsing
│   │   ├── Arena
│   │   └── Enum
│   ├── Patterns
│   ├── Reporting
│   ├── Results
│   ├── Scope
│   └── Structure
├── Debug
├── Estimation
│   ├── Classification
│   ├── Models
│   └── Scoring
├── Execution
│   └── Dump
│       ├── Segmentation
│       └── Strategies
├── Exporters
│   ├── Adapters
│   ├── Assets
│   │   ├── Css
│   │   └── Vendor
│   ├── Dashboards
│   │   ├── Renderers
│   │   └── Routemap
│   ├── Datasets
│   ├── Dumps
│   ├── Infrastructure
│   ├── Reports
│   ├── Styling
│   └── Trends
├── Infrastructure
├── Parsers
│   ├── Analysis
│   ├── Common
│   ├── CsharpParsers
│   │   └── Hybrid
│   └── Results
├── Properties
└── Statistics
    ├── Engines
    └── Models
```

---

## 🔎 Structural Candidate Analysis (ADR-EXP-007)

- **Structural Candidates:** 48
- **Pattern Similarity:** 47
- **Unresolved:** 1

---

## 🏥 Architectural Health by Module

### 🟡 Analyzers

- **Score:** `54,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 3,60
- **Isolation:** 0,55
- **Core Density:** 1,00

### 🟢 CLI

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 2,75
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Debug

- **Score:** `91,7`
- **Unresolved Candidates:** 🔴 1 (100%)
- **Coupling:** 0,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Estimation

- **Score:** `70,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 1,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Infrastructure

- **Score:** `82,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 1,20
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Core

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 0,11
- **Isolation:** 0,22
- **Core Density:** 0,89

### 🟢 Execution

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 2,50
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Exporters

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 2,89
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟡 Parsers

- **Score:** `50,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 3,23
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Statistics

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 4,00
- **Isolation:** 0,00
- **Core Density:** 0,00

---

## ⚠ Implicit Coupling Suspicion

| Type | Module | Target Module | Fan-Out | Fan-In | Dominance | Volume |
|------|--------|---------------|--------|--------|-----------|--------|
| ArchitecturalClassificationAnalyzer | Analyzers | Core | 7 | 0 | 1,00 | 7 |
| CoreIsolationAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| FitnessGateAnalyzer | Analyzers | Core | 8 | 0 | 1,00 | 8 |
| ImplicitCouplingAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| StatisticsValidationAnalyzer | Analyzers | Core | 7 | 0 | 1,00 | 7 |
| StructuralCandidateAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| StructuralCandidateRefinementAnalyzer | Analyzers | Core | 8 | 0 | 1,00 | 8 |
| ParserArenaCliRunner | CLI | Core | 12 | 0 | 1,00 | 12 |
| EffortEstimator | Estimation | Core | 4 | 1 | 0,80 | 4 |
| SolidAnalyzer | Analyzers | Core | 12 | 0 | 1,00 | 12 |
| AnalysisOrchestrator | Core | Estimation | 7 | 0 | 1,00 | 7 |
| ParserSelector | Core | Parsers | 9 | 3 | 0,75 | 9 |
| ReportSnapshotBuilder | Core | Exporters | 10 | 3 | 0,77 | 10 |
| RDICalculator | Estimation | Core | 9 | 1 | 0,90 | 9 |
| DumpStrategyResolver | Execution | Core | 7 | 0 | 1,00 | 7 |
| ArchitecturalDashboardExporterAdapter | Exporters | Core | 11 | 0 | 1,00 | 11 |
| ParsingDashboardExporterAdapter | Exporters | Core | 8 | 0 | 1,00 | 8 |
| QualityDashboardExporterAdapter | Exporters | Core | 7 | 0 | 1,00 | 7 |
| StructuralDashboardExporterAdapter | Exporters | Core | 7 | 0 | 1,00 | 7 |
| ArchitecturalDashboardExporter | Exporters | Core | 18 | 1 | 0,95 | 18 |
| HubDashboardMetrics | Exporters | Core | 16 | 3 | 0,84 | 16 |
| ArchitecturalDashboardMetrics | Exporters | Core | 16 | 3 | 0,84 | 16 |
| QualityDashboardMetrics | Exporters | Core | 16 | 4 | 0,80 | 16 |
| HtmlDashboardExporter | Exporters | Core | 8 | 0 | 1,00 | 8 |
| StrategyAggregateMetric | Exporters | Core | 5 | 1 | 0,83 | 5 |
| ParsingDashboardExporter | Exporters | Core | 5 | 1 | 0,83 | 5 |
| QualityDashboardExporter | Exporters | Core | 6 | 1 | 0,86 | 6 |
| StructuralInventoryExporter | Exporters | Core | 11 | 1 | 0,92 | 11 |
| DumpAnaliseExporter | Exporters | Core | 5 | 0 | 1,00 | 5 |
| MarkdownReportExporter | Exporters | Core | 8 | 1 | 0,89 | 8 |
| CSharpRegexParser | Parsers | Core | 11 | 1 | 0,92 | 11 |
| HigienizadorLexico | Parsers | Core | 13 | 1 | 0,93 | 13 |
| CSharpTextualParser | Parsers | Core | 13 | 2 | 0,87 | 13 |
| CoreDependencyAbsolutionRule | Analyzers | Core | 4 | 1 | 0,80 | 4 |
| OrchestratorAbsolutionRule | Analyzers | Core | 4 | 1 | 0,80 | 4 |
| PublicZeroUsageOmissionRule | Analyzers | Core | 4 | 1 | 0,80 | 4 |
| LayerSegmentationResolver | Execution | Core | 5 | 1 | 0,83 | 5 |
| TopFolderSegmentationResolver | Execution | Core | 4 | 1 | 0,80 | 4 |
| GlobalDumpStrategy | Execution | Core | 5 | 1 | 0,83 | 5 |
| SegmentedDumpStrategy | Execution | Core | 6 | 1 | 0,86 | 6 |
| ArchitecturalStructureRendererP5 | Exporters | Core | 7 | 1 | 0,88 | 7 |
| ChartsRendererP5 | Exporters | Core | 5 | 1 | 0,83 | 5 |
| ArchitectureVisualizationResolver | Exporters | Core | 5 | 1 | 0,83 | 5 |
| ModuleRouteMapBuilder | Exporters | Core | 9 | 2 | 0,82 | 9 |
| HybridAdaptiveParser | Parsers | Core | 4 | 1 | 0,80 | 4 |
| HybridIncrementalParser | Parsers | Core | 4 | 1 | 0,80 | 4 |
| HybridSelectiveParser | Parsers | Core | 8 | 1 | 0,89 | 8 |

Possible architectural coupling detected based on structural heuristics.
Manual inspection is recommended.

---

## 🧭 Architectural Stability Metrics (Robert Martin)

| Module | Abstractness (A) | Instability (I) | Distance (D) |
|--------|------------------|-----------------|--------------|
| Analyzers | 0,10 | 0,69 | 0,21 |
| CLI | 0,00 | 0,85 | 0,15 |
| Debug | 0,00 | 0,00 | 1,00 |
| Estimation | 0,00 | 0,38 | 0,63 |
| Infrastructure | 0,00 | 0,50 | 0,50 |
| Core | 0,10 | 0,02 | 0,88 |
| Execution | 0,25 | 0,57 | 0,18 |
| Exporters | 0,00 | 0,60 | 0,40 |
| Parsers | 0,00 | 0,63 | 0,37 |
| Statistics | 0,00 | 0,67 | 0,33 |

---

## 🚦 Fitness Gates

- 🟢 **UnreferencedTypes** Structural Unreferenced controlado: 0%

🟢 **Architecture ready for CI/CD**

---

---

## 📘 Metrics Explanation

**Structural Candidates**  
Classes detected with zero or near-zero structural references in the analyzed scope.  
These are potential dead-code candidates based purely on static structural analysis.

**Pattern Similarity**  
Structural candidates that match known architectural patterns such as:
- Dependency Injection usage
- Interface-based abstractions
- Factory / Strategy structures

Pattern similarity indicates the class likely participates in a valid architectural pattern.
However, it does **not guarantee runtime usage**.

**Unresolved**  
Candidates that could not be explained by recognized structural patterns.
These remain potential dead-code candidates after probabilistic refinement.
Manual inspection is recommended.

**Coupling**  
Average fan-out of types inside the module.
Higher values indicate stronger inter-module dependency.

**Isolation**  
Core-layer types with no incoming structural references.
These may indicate incomplete architecture integration.

**Core Density**  
Proportion of types belonging to the Core architectural layer.
Higher density generally indicates stronger domain encapsulation.

**Architectural Score**  
Composite structural health indicator (0–100) based on:
- Coupling impact
- Unresolved candidate density
- Isolation rate
- Core density bonus

The score is normalized and intended as a heuristic indicator, not a formal proof.

**Architectural Stability Metrics (Robert Martin)**  
The following metrics are derived from the architectural model proposed by Robert C. Martin.
They are intended as structural indicators rather than strict architectural rules.

**Abstractness (A)**  
Represents the proportion of abstractions within a module.

A = Na / Nc

Where:
- Na = number of abstract types (interfaces or abstract classes)
- Nc = total number of types in the module.

Higher values indicate a more abstract module.

**Instability (I)**  
Measures how dependent a module is on other modules.

I = Ce / (Ce + Ca)

Where:
- Ce = outgoing dependencies
- Ca = incoming dependencies

Values closer to 1 indicate modules that depend heavily on other modules.

**Distance from Main Sequence (D)**  

D = | A + I − 1 |

This metric measures how far a module is from the architectural equilibrium line between abstraction and stability.

Values close to 0 indicate balanced architecture.

Higher values indicate architectural tension such as:
- overly concrete and rigid modules
- overly abstract but unstable modules

These metrics should be interpreted as architectural signals, not strict violations.


**Implicit Coupling Detection**  
Implicit Coupling identifies classes whose dependencies concentrate towards a specific module or subsystem.

This heuristic analyzes structural patterns such as:
- strong directional dependency concentration
- high fan-out towards a single module
- asymmetric dependency flows between modules

A flagged class does not necessarily represent a design problem.

Typical legitimate cases include:
- orchestrators
- adapters between subsystems
- integration layers

These signals are intended to highlight areas that may benefit from architectural review.

_Generated by RefactorScope_

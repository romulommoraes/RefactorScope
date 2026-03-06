# 🧬 RefactorScope – Architectural Report

📅 **Execution Time:** 2026-03-06 05:19 UTC  
📂 **Target Scope:** `C:\Users\romul\source\repos\RefactorScope`  

---

## 📂 Project Structure (Clean)

```
│   ├── Docs
│   ├── RefactorScope
│   │   ├── Analyzers
│   │   │   ├── Solid
│   │   │   │   ├── Rules
│   │   ├── CLI
│   │   ├── Core
│   │   │   ├── Abstractions
│   │   │   ├── Configuration
│   │   │   ├── Context
│   │   │   ├── Datasets
│   │   │   ├── Governance
│   │   │   ├── Metrics
│   │   │   ├── Model
│   │   │   ├── Orchestration
│   │   │   ├── Patterns
│   │   │   ├── Reporting
│   │   │   ├── Results
│   │   │   ├── Scope
│   │   │   ├── Structure
│   │   ├── Debug
│   │   ├── Docs
│   │   ├── Execution
│   │   │   ├── Dump
│   │   │   │   ├── Segmentation
│   │   │   │   ├── Strategies
│   │   ├── Exporters
│   │   ├── Infrastructure
│   │   ├── Parsers
│   │   │   ├── CsharpParsers
```

---

## 🔎 Structural Candidate Analysis (ADR-EXP-007)

- **Structural Candidates:** 42
- **Pattern Similarity:** 41
- **Unresolved:** 1

---

## 🏥 Architectural Health by Module

### 🔴 Analyzers

- **Score:** `0,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 4,79
- **Isolation:** 0,53
- **Core Density:** 1,00

### 🟡 CLI

- **Score:** `50,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 2,50
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Debug

- **Score:** `91,7`
- **Unresolved Candidates:** 🔴 1 (100%)
- **Coupling:** 0,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🔴 Exporters

- **Score:** `0,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 6,78
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Infrastructure

- **Score:** `85,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 1,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Core

- **Score:** `100,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 0,00
- **Isolation:** 0,26
- **Core Density:** 0,98

### 🔴 Execution

- **Score:** `6,3`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 3,13
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🔴 Parsers

- **Score:** `0,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 10,00
- **Isolation:** 0,00
- **Core Density:** 0,00

---

## ⚠ Implicit Coupling Suspicion

| Type | Module | Target Module | Fan-Out | Fan-In | Dominance | Volume |
|------|--------|---------------|--------|--------|-----------|--------|
| ArchitecturalClassificationAnalyzer | Analyzers | Core | 11 | 0 | 1,00 | 11 |
| ArchitecturalHygieneAnalyzer | Analyzers | Core | 7 | 2 | 0,78 | 7 |
| CoreIsolationAnalyzer | Analyzers | Core | 7 | 0 | 1,00 | 7 |
| CouplingAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| EntryPointHeuristicAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| FitnessGateAnalyzer | Analyzers | Core | 13 | 0 | 1,00 | 13 |
| ImplicitCouplingAnalyzer | Analyzers | Core | 8 | 0 | 1,00 | 8 |
| ProjectStructureAnalyzer | Analyzers | Core | 5 | 0 | 1,00 | 5 |
| StructuralCandidateAnalyzer | Analyzers | Core | 6 | 0 | 1,00 | 6 |
| StructuralCandidateRefinementAnalyzer | Analyzers | Core | 11 | 0 | 1,00 | 11 |
| ParserSelector | CLI | Parsers | 5 | 0 | 1,00 | 5 |
| DatasetExporter | Exporters | Core | 5 | 0 | 1,00 | 5 |
| DumpAnaliseExporter | Exporters | Core | 7 | 0 | 1,00 | 7 |
| FitnessGateCsvExporter | Exporters | Core | 5 | 0 | 1,00 | 5 |
| MarkdownReportExporter | Exporters | Core | 15 | 0 | 1,00 | 15 |
| StructuralInventoryExporter | Exporters | Core | 17 | 0 | 1,00 | 17 |
| SolidAnalyzer | Analyzers | Core | 22 | 0 | 1,00 | 22 |
| DumpStrategyResolver | Execution | Core | 11 | 0 | 1,00 | 11 |
| CSharpRegexParser | Parsers | Core | 15 | 2 | 0,88 | 15 |
| CSharpTextualParser | Parsers | Core | 17 | 2 | 0,89 | 17 |
| PublicZeroUsageOmissionRule | Analyzers | Core | 6 | 2 | 0,75 | 6 |
| LayerSegmentationResolver | Execution | Core | 10 | 2 | 0,83 | 10 |
| TopFolderSegmentationResolver | Execution | Core | 8 | 2 | 0,80 | 8 |
| SegmentedDumpStrategy | Execution | Core | 7 | 2 | 0,78 | 7 |

Possible architectural coupling detected based on structural heuristics.
Manual inspection is recommended.

---

## 🧭 Architectural Stability Metrics (Robert Martin)

| Module | Abstractness (A) | Instability (I) | Distance (D) |
|--------|------------------|-----------------|--------------|
| Analyzers | 0,11 | 0,71 | 0,19 |
| CLI | 0,00 | 1,00 | 0,00 |
| Debug | 0,00 | 0,00 | 1,00 |
| Exporters | 0,00 | 0,97 | 0,03 |
| Infrastructure | 0,00 | 0,45 | 0,55 |
| Core | 0,09 | 0,00 | 0,91 |
| Execution | 0,25 | 0,51 | 0,24 |
| Parsers | 0,00 | 0,83 | 0,17 |

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

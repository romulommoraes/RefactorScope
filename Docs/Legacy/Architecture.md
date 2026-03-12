# 🏗 RefactorScope Architecture

## Overview

RefactorScope is a static structural analysis tool designed to audit the architectural health of C# codebases.

The system follows a **modular pipeline architecture** that separates:

- parsing
- structural modeling
- analysis
- consolidation
- reporting

The design prioritizes:

- deterministic execution
- low coupling
- extensibility
- language-agnostic core logic

---

# High Level Architecture


CLI
↓
Parser
↓
Structural Model
↓
Analyzer Orchestrator
↓
Independent Analyzers
↓
Consolidated Report
↓
Exporters


---

# System Components

## CLI Layer

Responsible for:

- command execution
- configuration loading
- progress rendering
- terminal visualization

Implementation example:


RefactorScope.CLI


Key component:


TerminalRenderer


---

# Parser Layer

Responsible for translating source code into the internal **structural model**.

Parsers never perform analysis.

Responsibilities:

- read files
- sanitize lexical traps
- extract namespaces
- extract type declarations
- detect structural references

Supported parsers:

- `CSharpRegexParser`
- `CSharpTextualParser`

Future:

- `RoslynParser`

---

# Structural Model

The structural model represents code in a **language-neutral format**.

Core entities:

### FileInfo

Represents a source file.

Contains:

- relative path
- namespace
- declared types
- source code

---

### TypeInfo

Represents a structural type.

Possible kinds:

- class
- interface
- struct
- record

Contains:

- name
- namespace
- file location
- references
- base types

---

### ReferenceInfo

Represents dependency relationships between types.

Example:


OrderService → IRepository


---

# Analyzer System

Analyzers are **independent diagnostic modules**.

Each analyzer:

- consumes the structural model
- produces a result object

Interface:

```csharp
public interface IAnalyzer
{
    IAnalysisResult Analyze(AnalysisContext context);
}

Examples:

ZombieAnalyzer

CouplingAnalyzer

CoreIsolationAnalyzer

NamespaceDriftAnalyzer

Analyzer Orchestrator

The orchestrator coordinates analyzer execution.

Responsibilities:

resolve enabled analyzers

execute analyzers

collect results

The orchestrator does not interpret results.

Output:


ConsolidatedReport

Consolidation Layer

Results from analyzers are aggregated into a unified report.

Structure example:


ConsolidatedReport
 ├── ZombieResults
 ├── CouplingResults
 ├── CoreIsolationResults
 └── Metadata

Exporters

Exporters transform analysis results into external artifacts.

Exporters never perform analysis.

Examples:

HtmlDashboardExporter

DatasetExporter

MarkdownReportExporter

AiDumpExporter

Dataset Layer

Dataset builders transform analyzer outputs into BI-ready datasets.

Example outputs:


dataset_structural_overview.csv
dataset_arch_health.csv
dataset_type_risk.csv


These datasets enable integration with:

QuickSight

PowerBI

Tableau

Architectural Principles

RefactorScope enforces several architectural constraints.

Analyzer Isolation

Analyzers must not depend on each other.

Deterministic Execution

The pipeline must produce the same results given identical inputs.

Language Agnosticism

The core operates on a structural model independent of the source language.

Export Decoupling

Exporters consume results but never interact with analyzers.

Future Architecture

Planned improvements include:

Roslyn Semantic Engine

Adding semantic analysis through Roslyn AST.

Multi-language Support

Potential support for:

Java

TypeScript

Python

Parallel Analyzer Execution

Since analyzers are independent, they can be executed concurrently.

Summary

RefactorScope architecture is designed to remain:

modular

deterministic

extensible

safe for continuous architectural governance
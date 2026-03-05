📜 Architectural Decision Records

This document consolidates the main architectural decisions of RefactorScope.

The goal of ADRs is to:

preserve important structural decisions

prevent architectural drift

document technical trade-offs

enable controlled system evolution

🧱 Core Architecture
ADR-001 — RefactorScope Core Architecture

Status: Accepted

Context

RefactorScope was created as a structural code analysis tool designed to support architectural refactoring processes.

Previous experience with similar tools has shown that analysis systems often suffer from:

coupling between modules

implicit execution pipelines

uncontrolled scope growth

The goal of RefactorScope 1.0 is to provide:

deterministic structural diagnostics

modular architecture

predictable analysis pipeline

Decision

RefactorScope adopts a model based on an:

Orchestrated Pipeline of Independent Analyzers

Execution flow:

Configuration
   ↓
Parser
   ↓
Structural Model
   ↓
Orchestrator
   ↓
[ Independent Analyzers ]
   ↓
Results
   ↓
Consolidation
   ↓
Export
Principles

Analyzers are independent

Analyzers do not communicate with each other

Communication occurs only through Result Objects

Execution must be deterministic

System scope must remain controlled

The core must remain language-agnostic

RefactorScope 1.0 focuses strictly on structural analysis

ADR-002 — Language-Agnostic Structural Model

Status: Accepted

Context

Analyzing C# code directly would tightly couple the system to a specific parser implementation.

To allow future evolution and potential support for other languages, an intermediate neutral representation was required.

Decision

RefactorScope uses a Language-Agnostic Structural Model.

Pipeline:

Source Code
   ↓
Parser
   ↓
Structural Model
   ↓
Analyzers
Components
FileInfo

Represents analyzed source files.

Contains:

relative path

namespace

declared types

source code

TypeInfo

Represents structural types such as:

classes

interfaces

records

structs

Contains:

name

namespace

source file

structural references

ReferenceInfo

Represents dependencies between types.

Directory Structure

Used for:

modular analysis

AI-ready structural dumps

ADR-003 — Analyzer Execution Contract

Status: Accepted

All analyzers follow the contract:

public interface IAnalyzer
{
    IAnalysisResult Analyze(AnalysisContext context);
}
Rules

An analyzer:

✔ receives an AnalysisContext
✔ executes independently
✔ returns an IAnalysisResult

An analyzer must not:

❌ access results from other analyzers
❌ access the filesystem directly
❌ modify the analysis context
❌ depend on another analyzer

ADR-004 — Orchestrator and Result Consolidation

Status: Accepted

The system includes a Central Orchestrator.

Responsibilities:

selecting enabled analyzers

executing analyzers

consolidating results

The orchestrator does not interpret analysis results.

Its role is only to produce:

ConsolidatedReport
ADR-005 — Configuration System

Status: Accepted

RefactorScope uses an external configuration file:

refactorscope.json

This configuration allows defining:

analysis scope

enabled analyzers

architectural layer rules

fitness gates

Example:

{
  "scope": {
    "include": ["src/Core"],
    "exclude": ["tests"]
  },
  "analyzers": {
    "zombie": true,
    "coupling": true
  }
}
🔬 Analysis & Detection
ADR-EXP-011 — Zombie Detection Refinement

Status: Accepted

Problem

Modern architectures using patterns such as:

Dependency Injection

Strategy

Factory

may generate false positives for dead code detection.

Decision

Adopt a three-layer probabilistic model.

Layer 0 — Structural Suspicion
UsageCount == 0

Classified as:

ZombieSuspect
Layer 1 — DI Contamination

Detect patterns such as:

AddScoped<
AddTransient<
AddSingleton<

These patterns reduce the probability of a zombie classification.

Layer 2 — Polymorphism

If:

a class implements an interface

the interface is referenced elsewhere

then the probability of zombie classification is reduced.

ADR-EXP-007 — Zombie Model Redefinition

Status: Accepted

A type is considered a Confirmed Zombie only if:

it has no structural references

it was not probabilistically absolved

it is not registered in DI

it is not used via interface polymorphism

it is not a structural component of the system itself

Final Classification Categories

Structural Candidate

Suspicious

Absolved

Confirmed Zombie

Publication Rule
SelfAnalysis → Confirmed Zombies MUST equal 0
ADR-EXP-008 — Relative SmellIndex

Status: Accepted

Problem

A SmellIndex based on absolute numbers introduces distortions.

New Formula
deadRatio = confirmedZombies / totalClasses
legacyRatio = legacyCount / totalClasses
isolationRatio = isolatedCoreCount / totalClasses

SmellIndex =
(deadRatio * 40)
+ (legacyRatio * 20)
+ (isolationRatio * 20)
+ (entropy * 20)

Range:

0–100
ADR-EXP-012 — Precision Over Total Coverage

Status: Accepted

RefactorScope prioritizes:

Structural precision > universal coverage

Implications:

fewer false positives

documented acceptance of some false negatives

ADR-EXP-013 — Detection Scope

Automatically detected patterns include:

instantiation via new

simple generics usage

explicit DI registration

interface implementations

typeof(T)

Not Automatically Detected

dynamic reflection

plugin loading

assembly scanning

runtime code generation

📊 Data & Visualization
ADR-006 — BI Dataset Export

Status: Accepted

RefactorScope generates datasets suitable for Business Intelligence tools.

Files:

dataset_structural_overview.csv
dataset_arch_health.csv
dataset_type_risk.csv
dataset_entrypoints.csv
dataset_coupling_matrix.csv

These datasets feed tools such as:

Amazon QuickSight

Power BI

analytical dashboards

ADR-007 — Architectural Observability

Status: Accepted

The system records historical architectural metrics.

File:

datasets/trend/structural_history.csv

Fields:

Timestamp
Scope
StructuralScore
Coupling
ZombieRate
IsolationRate
CoreDensity

This enables:

temporal monitoring

architectural regression detection

ADR-EXP-007 — Deterministic Offline Dashboard

Status: Proposed

Dashboards will be generated without runtime JavaScript.

Structure:

refactorscope-output/
   dashboards/
      index.html

Charts:

SVG based

pre-rendered

offline friendly

🧪 Testing Strategy
ADR-TEST-001 — Testing Strategy

Status: Proposed

RefactorScope should include a dedicated test project:

RefactorScope.Tests

Testing tools:

xUnit

FluentAssertions

Minimal Test Coverage (Quick Win)

Tests should cover:

Shannon Entropy

entropy equals zero for uniform strings

entropy increases with diversity

Analyzer Pipeline

Validate:

analyzer execution

correct execution order

report integrity

Fitness Gates

Validate:

execution after analyzers

CI pipeline blocking when FAIL occurs

ConsolidatedReport

Snapshot tests verifying report structure.

🔬 Textual Parser Tests

The textual parser presents specific risks.

Dedicated tests must exist.

1 — String Trap

Prevent URLs from being interpreted as namespaces.

Input:

string url = "http://example.com";

Expected result:

No namespace detected
2 — Line Comments

Commented code must not produce symbols.

// using Fake.Namespace
3 — Block Comments
/*
namespace Fake
class Fake
*/

Must not generate types.

4 — Multiline Strings

Strings containing code should not trigger parsing.

var code = "class Fake {}";
5 — Partial Code

Parser must tolerate:

incomplete files

broken code

syntax errors

6 — Modern C# Tokens

Ignore tokens such as:

record
init
with
🧭 Backlog ADRs
ADR-BACKLOG-002 — Snapshot Consistency

Ensure full result consistency before governance decisions.

Planned for v1.2.

ADR-BACKLOG-003 — Advisory vs CI Messaging

Separate:

diagnostic information

governance policy

Planned for v1.2.
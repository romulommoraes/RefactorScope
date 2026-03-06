# 🧪 RefactorScope Testing Strategy

This document defines the practical testing strategy required to validate RefactorScope analyzers and parsing engines.

The goal is to ensure:

- structural robustness
- parser resilience
- metric correctness
- deterministic analysis behavior

---

# Test Project Suite

RefactorScope should include a dedicated folder:

RefactorScope/TestProjects

Containing controlled architectural scenarios.

---

## Project01 — Procedural Coupling

Purpose:

Stress-test structural metrics.

Expected signals:

- high entropy
- high coupling
- structural candidates

Structure:

Single namespace with multiple classes referencing each other randomly.

Metrics expected:

High coupling  
High entropy  
StructuralCandidates > 0

---

## Project02 — Hidden Architectural Coupling

Purpose:

Validate the ImplicitCouplingAnalyzer.

Structure:

Core
Services
Infrastructure
CLI

Injected violation:

Core directly referencing Infrastructure.

Expected signals:

ImplicitCoupling flagged  
Dominance high for offending component.

---

## Project03 — Clean Architecture Baseline

Purpose:

Validate healthy architecture baseline.

Structure:

Domain
Application
Infrastructure
CLI

Expected metrics:

NamespaceDrift = 0  
Coupling minimal  
SmellIndex low  
Unresolved = 0

---

# Parser Robustness Tests

The textual parser requires dedicated validation.

Test cases must include:

---

## String Trap

Ensure URLs inside strings are not parsed as namespaces.

Example:

string url = "http://example.com";

Expected result:

No namespace detected.

---

## Comment Contamination

Line comments must not produce symbols.

Example:

// using Fake.Namespace

Expected result:

No references detected.

---

## Block Comments

Commented code must not generate types.

Example:

/*
namespace Fake
class Fake
*/

Expected result:

No types detected.

---

## Multiline Strings

Strings containing code fragments must not trigger parsing.

Example:

var code = "class Fake {}";

Expected result:

No type detection.

---

## Broken Code

Parser must tolerate incomplete code.

Example:

class Service
{
    public void Test(

Expected result:

Parser should continue scanning remaining files.

---

# Null Safety Validation

Tests must verify resilience against null values in the structural model.

Potential sources:

- missing namespace
- missing file path
- incomplete references
- partial type declarations

Expected behavior:

System must not crash.

---

# Invisible Code Breaks

Test cases should include:

- mixed line endings
- unusual whitespace
- tabs vs spaces
- UTF-8 BOM markers

Parser must remain stable.

---

# Analyzer Integrity Tests

Validate analyzer behavior.

Tests required:

Analyzer execution order

Result integrity

ConsolidatedReport structure

FitnessGate evaluation

---

# Deterministic Execution

Given the same input:

RefactorScope must produce identical outputs.

Tests should validate:

hash equality of reports

dataset stability

metric consistency

---

# Final Validation

Before release, RefactorScope should run:

SelfAnalysis on its own repository.

Expected conditions:

Confirmed Zombies = 0  
Namespace Drift = 0  
SmellIndex coherent  
No analyzer failures
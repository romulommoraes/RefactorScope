# 🔬 Analysis Methodology

## Purpose

RefactorScope performs **static structural analysis** to evaluate the architectural health of a codebase.

The system focuses on detecting **architectural signals**, not definitive violations.

---

# Scientific Approach

The methodology follows four principles:

1. Structural observation
2. Heuristic interpretation
3. Probabilistic refinement
4. Human verification

---

# Structural Analysis Model

The scanner observes structural signals including:

- type declarations
- namespace relationships
- dependency edges
- module boundaries

The system does **not execute code**.

---

# Zombie Detection

Zombie detection identifies types that appear unused.

Initial rule:


UsageCount == 0


These are classified as:


Structural Candidates


---

# Probabilistic Refinement

Modern architectures use:

- dependency injection
- interface polymorphism
- factory patterns

To reduce false positives, RefactorScope applies refinement layers.

---

## Layer 1 — Dependency Injection Detection

Patterns detected:


AddScoped<
AddTransient<
AddSingleton<


These reduce the probability of dead code.

---

## Layer 2 — Interface Usage

If:

- class implements interface
- interface is referenced

Then the class may be instantiated indirectly.

---

# Zombie Classification

Final categories:

| Category | Meaning |
|---|---|
Structural Candidate | no reference detected |
Suspicious | uncertain |
Absolved | probable indirect usage |
Confirmed Zombie | high confidence dead code |

---

# Smell Index

Architectural health is measured through a normalized index.

Formula:


SmellIndex =
(deadRatio * 40)

(legacyRatio * 20)

(isolationRatio * 20)

(entropy * 20)


Range:


0–100


Classification:

| Range | Interpretation |
|---|---|
0–20 | Healthy |
20–40 | Stable |
40–60 | Degrading |
60–80 | Critical |
80–100 | Structural Risk |

---

# Entropy Metric

Entropy measures lexical variability in the codebase.

Used as a proxy for:

- complexity
- structural disorder

Calculation based on **Shannon entropy**.

---

# Coupling Analysis

The system evaluates:

- fan-in
- fan-out
- dependency density

High coupling signals potential architectural fragility.

---

# Layer Isolation

RefactorScope checks architectural layering rules.

Example:


Core should not depend on Infrastructure


Violations produce architecture alerts.

---

# Fitness Gates

Fitness gates transform metrics into CI decisions.

Example:

| Gate | Condition |
|---|---|
DeadCode | ZombieRate > threshold |
CoreIntegrity | Core depends on Infra |

Failing gates may block CI pipelines.

---

# Limitations

RefactorScope does not detect:

- reflection-based instantiation
- plugin loading
- runtime DI scanning
- code generation

Therefore, results must always be **reviewed by humans**.

---

# Philosophy

RefactorScope is designed to:


measure architectural signals
not declare absolute truths
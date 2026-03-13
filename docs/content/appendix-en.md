# Appendix A – Methods, Metrics, Rules, and Heuristics in RefactorScope

This technical appendix details the current methodology implemented in RefactorScope. It serves as a publication-ready reference, summarizing the execution flow, defining the principal metrics, documenting thresholds, and clarifying how the system transforms raw code parsing into architectural and operational indicators.

> **Architectural Note (ADR-EXP-007): Observation vs. Decision**
> RefactorScope's analysis is purely structural and confined to the provided scope. The absence of references or the flagging of a type as "Unresolved" (formerly *Zombie*) **does not declare the definitive death of the code, nor does it make automated deletion decisions**. The system provides probabilistic indicators that must always undergo human verification.

---

## A1. Nomenclature Overview

* **Structural Candidate:** A type that is unreferenced in the primary structural analysis layer.
* **Unresolved:** The subset of structural candidates that remains suspicious (lacking an architectural justification) after probabilistic refinement heuristics.
* **Pattern Similarity:** Candidates whose lack of direct references is explained by known architectural patterns (e.g., Dependency Injection, Polymorphism, Orchestrators).

---

## A2. Execution Methodology

The RefactorScope pipeline follows a sequential and immutable execution model:

1. **Scope Discovery:** The system maps C# files in the root directory and applies deterministic include/exclude rules.
2. **Complexity Classification:** The `ClassComplexityClassifier` separates files into `SAFE` and `COMPLEX` to optimize the parsing route.
3. **Sanitization and Lexical Protection:** The source code is sanitized to neutralize strings and comments. The `StructuralTokenGuard` filters out XML documentation noise (like `summary`, `param`, `misuse`) to prevent structural false positives.
4. **Parsing:** A selected strategy (e.g., the hybrid `Selective`) builds the structural model. `COMPLEX` files use `RegexFast`, while `SAFE` files receive Textual refinement. In case of curly brace (`{ }`) imbalance, the `RegexLocalRecovery` acts as a directional fallback.
5. **Plausibility Evaluation:** Plausibility checks validate whether the parser extracted an acceptable amount of data, safeguarding against silent failures.
6. **Analysis (Analyzers):** Independent modules process the graph to extract coupling, structural candidates, layer classification, and isolation metrics.
7. **Consolidation:** Results converge into the `ConsolidatedReport`, acting as the single source of truth.
8. **Statistics and Estimation:** The observational layer calculates confidence, and the estimation engine derives the **RDI (Refactor Difficulty Index)**.
9. **Exporting:** Generation of HTML Dashboards, Markdown reports, and structured JSON artifacts.



---

## A3. Methods Overview

| Method | Purpose | Input | Output / Effect |
| :--- | :--- | :--- | :--- |
| **Lexical Protection** | Filter reserved words and XML noise | Raw tokens | Valid structural identifiers |
| **Hybrid Parsing** | Extract structure with speed and safety | Source code + Classification | Structural Model without merge duplication |
| **Arena Scoring** | Evaluate parser performance | Multiple Parser Runs | Comparative score (penalizes failures and fallback use) |
| **Plausibility Evaluation** | Detect silent parsing failures | Structural Model | *PlausibilityWarning* if extraction is low/null |
| **Structural Candidates** | Find structurally orphaned types | Reference Graph | Base list (Ignores Program, Interfaces, DTOs, etc.) |
| **Probabilistic Refinement** | Reduce false positives | Base List + Heuristics | Final classification (Unresolved vs Pattern-Similarity) |
| **Implicit Coupling** | Detect asymmetry and tension | Dependency Matrix | Architectural concentration alerts (*Hubs/Orchestrators*) |
| **Architectural Hygiene** | Aggregate structural health | Analytical Results | *SmellIndex* (based on entropy, legacy, and isolation) |

---

## A4. Structural Candidate and Refinement Methodology

Detection is intentionally conservative. The initial barrier simply checks if a type has `UsageCount == 0`. After that, noise reduction layers are applied:

### 1. Immediate Exclusions
* **Interfaces and Records:** Excluded because they natively participate in polymorphic contracts and data transport.
* **Standard Nomenclature:** Types ending in `Dto`, `Model`, `Contract`, `Request`, and `Response` are ignored.

### 2. Probabilistic Protection
Each candidate starts with a probability of `1.0`. Heuristics reduce this value:
* **Bootstrap / Top-Level:** Types referenced in `Program.cs` or native injection methods receive a probability of `0.0` (Total immunity).
* **Tooling Patterns:** Suffixes like `Analyzer`, `Exporter`, `Strategy`, and `Config` immediately drop the score to `0.0`.
* **DI Heuristic:** If the global candidate rate is higher than **`0.25`**, types showing signs of Dependency Injection registration drop to a probability of **`0.15`**.
* **Interface Heuristic:** If the global rate is higher than **`0.20`**, types implementing active interfaces drop to a probability of **`0.25`**.

The final classification is strict: if the residual probability is **`>= 0.30`** (the MVP's tight threshold), the item is classified as **Unresolved**. Otherwise, it is explained as **Pattern-Similarity**.

---

## A5. Key Metrics and Formulas

| Metric | Formula / Logic | Range | Interpretation |
| :--- | :--- | :--- | :--- |
| **ClassesPerFile** | `Types / max(Files, 1)` | `>= 0` | Density. Files with multiple classes increase refactoring friction. |
| **UnresolvedCandidateRatio** | `ConfirmedUnresolved / TotalTypes` | `0..1` | True proportion of suspicious code (replaces *ZombieRatio*). |
| **Namespace Drift** | Does `Namespace` contain the folder name? | Boolean | Evaluates if physical taxonomy (folders) diverges from logical (namespaces). |
| **Instability (I)** | `Ce / (Ce + Ca)` | `0..1` | Modular instability (*Ce* = outgoing, *Ca* = incoming). |
| **Distance (D)** | `\|A + I - 1\|` | `0..1` | Modular tension. Modules far from `0` are either too rigid or too abstract. |
| **Smell Index** | `(DeadRatio * 40) + (LegacyRatio * 20) + (IsolationRatio * 20) + (Entropy * 20)` | `0..100` | Global hygiene index. Values `> 80` indicate high structural risk. |
| **StructuralRisk (RDI)** | `min((DriftRatio * 25) + (UnresolvedRatio * 25), 25)` | `0..25` | Architectural degradation component of the Effort estimate (RDI). |
| **Estimated Hours** | `RDI * 0.4` | `>= 0` | Final heuristic estimate of refactoring effort. |



---

## A6. Coupling and Penalties

RefactorScope adjusts its evaluation of dependencies (Fan-In and Fan-Out) by recognizing the inherent nature of certain modules:
* **Implicit Coupling:** Triggered when interaction volume (*FanOut + FanIn*) is `>= 5` and the dominance ratio (`FanOut / Volume`) is `>= 0.75`.
* **Penalty Exemptions:** The architectural score reduces coupling penalties for modules like the Composition Root (`Program`), Infrastructure, or heavy utilities (CLI, Exporters, Parsing) since high *Fan-Out* is expected in their coordination duties.

---

## A7. Arena and Parsers Evaluation

The system includes a batch comparative engine (*Batch Arena*) designed to find the most efficient strategy for the codebase.

The **ParserArenaScoreCalculator** assigns scores based on:
1. Massive penalties for catastrophic errors (`Failed` results in a score of `0`).
2. Score deductions if the system is forced to trigger a *Fallback* (e.g., from Textual to Regex).
3. Proportional rewards for the strategy that discovers the highest amount of true types and references in the shortest processing time (*ExecutionTime*).

---

## A8. Governance Defaults (Fitness Gates)

Quality gates translate inspection metrics into Continuous Integration (CI/CD) postures:

* **Dead Code / Unresolved Gate:** Alerts (`Warn`) when `> 10%`; Fails (`Fail`) when `> 20%` of the codebase consists of unresolved classes.
* **Coupling Gate:** Fails if the systemic coupling pressure crosses the configured threshold (`> 5.0`).
* **Core Isolation Gate:** Immediate failure (`FailIfAny = true`) if a class belonging to the domain *Core* is found depending on restricted external libraries or direct *UI/Infra* logic.
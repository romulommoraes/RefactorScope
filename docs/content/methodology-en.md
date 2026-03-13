# Analysis Methodology & Metrics

RefactorScope performs a **static structural analysis** to evaluate the architectural health of a C# codebase. The system focuses on detecting **architectural signals**, not definitive violations. The code is never executed.

The methodology follows four fundamental principles:
1. Structural Observation
2. Heuristic Interpretation
3. Probabilistic Refinement
4. Human Verification

---

## 1. Structural Candidate Detection (Formerly "Zombie Detection")
*Architecture Note (ADR-EXP-007): The analysis is purely structural. The absence of references does not declare the definitive "death" of a class, nor does it make removal decisions.*

The process begins by mapping the structural usage of all types. If `UsageCount == 0`, the type is classified as a **Structural Candidate**. 

Since modern architectures utilize dependency injection and polymorphism, the system applies layers of **Probabilistic Refinement** to reduce false positives:
* **Layer 1 (Dependency Injection):** Detects registration patterns like `AddScoped<`, `AddTransient<`, and `AddSingleton<`.
* **Layer 2 (Interface Usage):** If the class implements an interface and that interface is referenced in the system, the probability of it being dead code drops drastically.
* **Layer 3 (Omission Rules and SOLID):** Evaluates known suffixes (e.g., Orchestrators, Handlers). A public class with no direct usage (`PublicZeroUsageOmissionRule`), but which acts as an orchestrator, is protected against improper flagging.

Candidates that survive all filters without an architectural explanation become **Unresolved Candidates** and require manual inspection.

---

## 2. Coupling and Complementary Metrics
Coupling analysis has been expanded to detect dependency concentrations that indicate fragility or architectural hubs.

* **Fan-Out:** Number of outgoing dependencies. A high Fan-Out suggests a class with a high concentration of responsibility (e.g., Orchestrators) or strong architectural coupling.
* **Fan-In:** Number of incoming dependencies. A high Fan-In indicates core services or shared infrastructure critical to system stability.
* **Dominance:** Measures the relative influence of a component in the dependency network. Calculated by `FanOut / (FanOut + FanIn)`. Values close to **1** indicate components that exert strong directional influence (coordination or integration layers).

### Implicit Coupling
Identifies classes whose dependencies converge asymmetrically towards a specific module. This is not necessarily a design error (it could be a legitimate Adapter), but it serves as an alert for structural tension.

---

## 3. Stability and "Architectural Galaxy"
The system evaluates each module's relationship with the architectural equilibrium line using Robert C. Martin's classic metrics:

* **Abstractness (A):** `A = Na / Nc` (Ratio of abstract types/interfaces to the total number of types).
* **Instability (I):** `I = Ce / (Ce + Ca)` (Ratio of outgoing vs. incoming dependencies).
* **Distance from the Main Sequence (D):** `D = | A + I − 1 |`. 



Modules with a high **D** value indicate strong tension: they are either too concrete and rigid (hard to change) or too abstract and unstable (useless).

---

## 4. Effort Estimation (Refactor Difficulty Index - RDI)
*This section details the estimation engine integrated into the analysis pipeline.*

RefactorScope not only points out problems but also calculates the estimated effort to resolve them using the **RDI (Refactor Difficulty Index)**. The calculation is based on structural pressures and risk modeling:

* **File Pressure:** Evaluates the ratio of classes per file. (Many classes in a single file increase refactoring friction).
* **Structural Risk Model:** Calculates the degradation level by weighing two main variables:
  1. `NamespaceDriftRatio`: Proportion of types with a misalignment between the physical directory and the logical namespace.
  2. `UnresolvedCandidateRatio`: Proportion of confirmed "zombies" after probabilistic refinement.

Structural risk is normalized and combined with overall complexity to generate an **Estimated Hours** metric and a **Difficulty** level (Low, Medium, High, Critical).

---

## 5. Code Hygiene and Fitness Gates
The overall health of the project is consolidated into the **Smell Index**, a normalized index (0-100) that determines the hygiene status:

`SmellIndex = (DeadRatio * 40) + (LegacyRatio * 20) + (IsolationRatio * 20) + (Entropy * 20)`

* **Entropy:** Based on Shannon Entropy, it measures the lexical variability of the code as an indicator of structural disorder.
* **Status:** Ranges from *Healthy* (0-20) to *Structural Risk* (80-100).

Finally, **Fitness Gates** act as quality barriers for CI/CD pipelines. They evaluate metrics (such as Core layer isolation, preventing it from depending on Infrastructure) and can block or approve the build based on established thresholds.
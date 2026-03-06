# 📊 Complementary Architectural Metrics

This document describes additional architectural metrics introduced after the initial RefactorScope metric set.

These metrics extend the diagnostic capabilities of the system and provide deeper insights into structural coupling and architectural stability.

---

# Implicit Coupling Metrics

Implicit coupling metrics identify hidden dependency concentration between modules.

They do not necessarily indicate architectural problems but highlight areas that may require inspection.

These metrics are derived from dependency patterns detected in the structural graph.

---

## Fan-Out

Represents the number of outgoing dependencies from a type or module.

High Fan-Out indicates that a component depends on many other components.

Possible interpretations:

- orchestrator modules
- high responsibility concentration
- architectural coupling

---

## Fan-In

Represents the number of incoming dependencies targeting a component.

High Fan-In usually indicates:

- core services
- shared infrastructure
- architectural hubs

These components are often critical for system stability.

---

## Dominance

Dominance measures the relative influence of a component in the dependency network.

A simplified interpretation:

Dominance ≈ FanOut / (FanOut + FanIn)

Values closer to **1** indicate components that exert stronger influence over others.

High dominance modules often behave as:

- orchestrators
- coordination layers
- integration hubs

---

## Volume

Volume represents the total dependency activity around a component.

Volume = FanIn + FanOut

This metric indicates **structural traffic**.

High volume components are potential **architectural hotspots**.

---

# Architectural Stability Metrics

RefactorScope also incorporates architectural metrics inspired by **Robert C. Martin's stability model**.

These metrics operate at the **module level**.

---

## Abstractness (A)

Measures the proportion of abstract types within a module.

Formula:

A = Na / Nc

Where:

Na = number of abstract types  
Nc = total number of types in the module

Higher values indicate more abstract modules.

---

## Instability (I)

Measures how dependent a module is on external modules.

Formula:

I = Ce / (Ce + Ca)

Where:

Ce = outgoing dependencies  
Ca = incoming dependencies

Values closer to **1** indicate unstable modules.

---

## Distance from Main Sequence (D)

Measures how far a module deviates from the architectural equilibrium line.

Formula:

D = | A + I − 1 |

Interpretation:

D ≈ 0 → balanced architecture

High D values indicate:

- rigid concrete modules
- overly abstract but unstable modules

---

# Architectural Galaxy Visualization

These metrics power the **Architectural Galaxy** visualization.

In this diagram:

X-axis → Instability (I)  
Y-axis → Abstractness (A)

The diagonal line represents the **Main Sequence** (A + I = 1).

Modules positioned near this line tend to exhibit healthier architectural balance.

---

# Relationship with Existing Metrics

Complementary metrics extend the original metric set:

| Metric Group | Purpose |
|---------------|--------|
Structural Candidates | potential dead code |
Pattern Similarity | heuristic pattern detection |
Namespace Drift | physical vs logical structure |
SOLID Alerts | design principle signals |
Coupling Metrics | dependency density |
Implicit Coupling | hidden architectural concentration |
A/I/D Metrics | architectural stability |

---

# Future Integration

These complementary metrics enable future architectural capabilities:

- Code DERIVA Engine (architectural phenotype)
- architectural clustering
- cross-project comparisons
- historical architectural evolution
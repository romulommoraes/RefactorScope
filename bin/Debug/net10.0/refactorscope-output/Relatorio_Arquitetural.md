# 🧬 RefactorScope – Architectural Report

📅 **Execution Time:** 2026-03-07 06:09 UTC  
📂 **Target Scope:** `C:\Users\romul\source\repos\Scriptome`  

---

## 📂 Project Structure (Clean)

```
│   ├── AvaliaRoteiro
│   │   ├── Baselines
│   │   │   ├── etograma
│   │   │   │   ├── 3-BANHO
│   │   │   │   ├── 3-BANHO v0
│   │   │   │   ├── 3-BANHO v1
│   │   │   ├── legacy
│   │   │   │   ├── 3-BANHO
│   │   ├── Data
│   │   │   ├── Config
│   │   │   ├── Output
│   │   │   │   ├── bs
│   │   │   ├── Sandbox
│   │   │   │   ├── old
│   │   │   ├── Scripts
│   │   ├── Fingerprint
│   │   │   ├── Classificadores
│   │   │   │   ├── Arquetipos
│   │   │   │   ├── Cartesianos
│   │   │   │   ├── Energia
│   │   │   │   ├── Narrativa
│   │   │   │   ├── Psique
│   │   │   ├── Dados
│   │   │   ├── Forense
│   │   │   ├── Interfaces
│   │   │   ├── Modelos
│   │   │   ├── Modulos
│   │   │   ├── Motores
│   │   │   ├── MotoresEtograma
│   │   │   ├── MotoresInterpretativos
│   │   ├── Infrastructure
│   │   │   ├── Baseline
│   │   │   ├── Exportacao
│   │   │   │   ├── Excel
│   │   │   │   │   ├── Abas
│   │   │   ├── Interfaces
│   │   │   ├── Validador
│   │   ├── Limbic
│   │   │   ├── Algoritimos
│   │   │   ├── Dados
│   │   │   ├── Interfaces
│   │   │   ├── Linguistica
│   │   │   ├── Modelos
│   │   │   ├── Motor
│   │   │   ├── Pipeline
│   │   │   ├── Testes
│   │   ├── Nucleo
│   │   │   ├── Adaptadores
│   │   │   ├── Especialistas
│   │   │   ├── Interfaces
│   │   │   ├── Leitores
│   │   │   ├── Modelos
│   │   │   │   ├── Analise
│   │   │   │   ├── Enums
│   │   │   │   ├── Fenotipo
│   │   │   │   ├── Genoma
│   │   │   │   ├── Resultados
│   │   │   │   ├── Script
│   │   │   ├── Motores
│   ├── Baselines
│   │   ├── etograma
```

---

## 🔎 Structural Candidate Analysis (ADR-EXP-007)

- **Structural Candidates:** 25
- **Pattern Similarity:** 8
- **Unresolved:** 17

---

## 🏥 Architectural Health by Module

### 🟢 Program.cs

- **Score:** `83,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 17,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟡 Fingerprint

- **Score:** `63,0`
- **Unresolved Candidates:** 🔴 3 (6%)
- **Coupling:** 1,22
- **Isolation:** 0,02
- **Core Density:** 0,10

### 🔴 Infrastructure

- **Score:** `37,6`
- **Unresolved Candidates:** 🔴 2 (7%)
- **Coupling:** 4,04
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Nucleo

- **Score:** `79,9`
- **Unresolved Candidates:** 🔴 5 (11%)
- **Coupling:** 0,96
- **Isolation:** 0,15
- **Core Density:** 0,96

### 🟡 Limbic

- **Score:** `66,5`
- **Unresolved Candidates:** 🔴 7 (19%)
- **Coupling:** 1,22
- **Isolation:** 0,24
- **Core Density:** 0,84

---

## ⚠ Implicit Coupling Suspicion

| Type | Module | Target Module | Fan-Out | Fan-In | Dominance | Volume |
|------|--------|---------------|--------|--------|-----------|--------|
| Program | Program.cs | Infrastructure | 17 | 2 | 0,89 | 17 |
| OrquestradorNucleo | Nucleo | Fingerprint | 12 | 3 | 0,80 | 12 |
| ModuloEtogramaV2 | Fingerprint | Nucleo | 7 | 0 | 1,00 | 7 |
| MotorIdentidadeRNA | Fingerprint | Nucleo | 10 | 2 | 0,83 | 10 |
| MotorEtogramaCartesiano3D | Fingerprint | Nucleo | 9 | 2 | 0,82 | 9 |
| MotorEtogramaHeuristico | Fingerprint | Nucleo | 7 | 1 | 0,88 | 7 |
| AnalisadorContextual | Limbic | Nucleo | 12 | 3 | 0,80 | 12 |
| AnalisadorLexico | Limbic | Nucleo | 10 | 3 | 0,77 | 10 |
| PipelineLimbic | Limbic | Nucleo | 16 | 3 | 0,84 | 16 |
| VerificadorLimbic | Limbic | Nucleo | 26 | 2 | 0,93 | 26 |
| AdaptadorLimbic | Nucleo | Limbic | 28 | 3 | 0,90 | 28 |
| MotorFenotipoV2 | Nucleo | Fingerprint | 21 | 2 | 0,91 | 21 |
| ClassificadorJungV2 | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |
| ClassificadorTermico | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |
| ClassificadorPsicodinamicoAvancado | Fingerprint | Nucleo | 4 | 1 | 0,80 | 4 |

Possible architectural coupling detected based on structural heuristics.
Manual inspection is recommended.

---

## 🧭 Architectural Stability Metrics (Robert Martin)

| Module | Abstractness (A) | Instability (I) | Distance (D) |
|--------|------------------|-----------------|--------------|
| Program.cs | 0,00 | 0,89 | 0,11 |
| Fingerprint | 0,26 | 0,26 | 0,48 |
| Infrastructure | 0,07 | 0,66 | 0,27 |
| Nucleo | 0,07 | 0,13 | 0,81 |
| Limbic | 0,16 | 0,24 | 0,60 |

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

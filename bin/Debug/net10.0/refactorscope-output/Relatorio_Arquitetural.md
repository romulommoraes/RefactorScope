# 🧬 RefactorScope – Architectural Report

📅 **Execution Time:** 2026-03-06 02:27 UTC  
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

- **Structural Candidates:** 32
- **Pattern Similarity:** 13
- **Unresolved:** 19

---

## 🏥 Architectural Health by Module

### 🟢 Program.cs

- **Score:** `83,0`
- **Unresolved Candidates:** 🟢 0 (0%)
- **Coupling:** 17,00
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟡 Fingerprint

- **Score:** `60,8`
- **Unresolved Candidates:** 🔴 3 (5%)
- **Coupling:** 1,28
- **Isolation:** 0,00
- **Core Density:** 0,02

### 🟡 Infrastructure

- **Score:** `40,6`
- **Unresolved Candidates:** 🔴 3 (11%)
- **Coupling:** 3,78
- **Isolation:** 0,00
- **Core Density:** 0,00

### 🟢 Nucleo

- **Score:** `81,3`
- **Unresolved Candidates:** 🔴 6 (12%)
- **Coupling:** 0,90
- **Isolation:** 0,16
- **Core Density:** 0,96

### 🟢 Limbic

- **Score:** `74,6`
- **Unresolved Candidates:** 🔴 7 (19%)
- **Coupling:** 0,95
- **Isolation:** 0,24
- **Core Density:** 0,84

---

## ⚠ Implicit Coupling Suspicion

| Type | Module | Target Module | Fan-Out | Fan-In | Dominance | Volume |
|------|--------|---------------|--------|--------|-----------|--------|
| Program | Program.cs | Infrastructure | 17 | 0 | 1,00 | 17 |
| MotorDeIdentidade | Fingerprint | Nucleo | 9 | 2 | 0,82 | 9 |
| OrquestradorNucleo | Nucleo | Fingerprint | 11 | 2 | 0,85 | 11 |
| ModuloEtogramaV2 | Fingerprint | Nucleo | 7 | 0 | 1,00 | 7 |
| MotorIdentidadeRNA | Fingerprint | Nucleo | 14 | 2 | 0,88 | 14 |
| MotorEtogramaCartesiano3D | Fingerprint | Nucleo | 9 | 2 | 0,82 | 9 |
| MotorEtogramaHeuristico | Fingerprint | Nucleo | 7 | 0 | 1,00 | 7 |
| MotorEtogramaV2 | Fingerprint | Nucleo | 9 | 2 | 0,82 | 9 |
| AnalisadorContextual | Limbic | Nucleo | 12 | 3 | 0,80 | 12 |
| AnalisadorLexico | Limbic | Nucleo | 10 | 3 | 0,77 | 10 |
| PipelineLimbic | Limbic | Nucleo | 16 | 3 | 0,84 | 16 |
| VerificadorLimbic | Limbic | Nucleo | 26 | 2 | 0,93 | 26 |
| AdaptadorLimbic | Nucleo | Limbic | 32 | 2 | 0,94 | 32 |
| MotorFenotipoV2 | Nucleo | Fingerprint | 23 | 2 | 0,92 | 23 |
| ClassificadorJungV2 | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |
| ClassificadorTermico | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |
| ClassificadorNarrativaV2 | Fingerprint | Nucleo | 15 | 5 | 0,75 | 15 |
| ClassificadorLacanianoV2 | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |
| ClassificadorMBTIV2 | Fingerprint | Nucleo | 7 | 2 | 0,78 | 7 |

Possible architectural coupling detected based on structural heuristics.
Manual inspection is recommended.

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
_Generated by RefactorScope_

# 🧬 RefactorScope

**RefactorScope** is a **static structural analysis tool** designed to audit, visualize, and maintain the architectural health of C# codebases.

It detects **structural candidates (potential dead code)**, evaluates **architectural coupling**, checks **layer isolation**, and generates **architectural dashboards and datasets for BI analysis**.

The main goal is to support **metric-driven architectural refactoring**.

---

# 💡 Project Origin

RefactorScope emerged from a real-world need: auditing and guiding the architectural refactor of **Scriptome**, a complex narrative analysis engine written in C#.

As Scriptome grew, it became necessary to:

- detect structural dead code
- verify Core layer isolation
- measure architectural coupling
- enforce SOLID adherence
- detect namespace drift
- produce architecture dashboards

RefactorScope was created as an **independent, language-agnostic architectural scanner** for C# codebases.

---

# 🎯 Goals

RefactorScope aims to provide:

- fast structural auditing of C# projects
- heuristic dead code detection
- architectural isolation analysis
- structural dependency mapping
- architecture dashboards
- BI-ready datasets
- structured exports for AI analysis
- CI/CD architecture monitoring

---

# 🏗️ Architecture

RefactorScope follows **Clean Architecture principles** with strongly separated modules.


RefactorScope
│
├── Core
│ Domain model and datasets
│
├── Parsers
│ Code structure extraction
│
├── Analyzers
│ Architectural analysis modules
│
├── Exporters
│ Dashboards and report generators
│
├── Infrastructure
│ External integrations
│
└── CLI
Command-line interface


---

# ⚙️ Execution Pipeline

The analysis pipeline follows a deterministic flow:


Parser
↓
Structural Model
↓
Analyzers
↓
Consolidated Results
↓
Exporters


---

# Parser

Extracts structural information:

- classes
- interfaces
- namespaces
- dependencies
- physical location

Produces a **structural dependency graph**.

---

# Analyzers

Analyzers are **plug-in modules** that perform architectural analysis.

Each analyzer:

- consumes the structural model
- produces a **result DTO**

Examples:


StructuralCandidateAnalyzer
CouplingAnalyzer
SolidAnalyzer
CoreIsolationAnalyzer
NamespaceDriftAnalyzer


---

# Consolidated Results

All analyzer outputs are aggregated into:


ConsolidatedReport


This represents the **architectural state of the analyzed project**.

---

# Exporters

Exporters generate output formats without performing analysis.

Supported outputs include:

- HTML dashboards
- Markdown reports
- CSV datasets
- AI-ready structural dumps

---

# ⚙️ Parsing Engines

RefactorScope prioritizes **speed and resilience**, avoiding heavy AST dependencies.

Two parsing engines are available.

---

## CSharpRegexParser (Stable)

Regex-based parser designed for performance.

Features:

- very fast
- resilient to syntax errors
- CI-friendly
- ignores modern keywords (`record`, `init`)

---

## CSharpTextualParser (Experimental)

Advanced textual parser with lexical sanitization.

Steps:

1. comment removal
2. string neutralization
3. structural extraction

Prevents traps such as URLs being interpreted as namespaces.

---

# 📊 Structural Metrics

## Structural Candidates

Classes with zero structural references.

Possible causes:

- dead code
- CLI entry points
- plugins
- DI wiring
- reflection

---

## Pattern Similarity

Classes matching known architectural patterns.

Examples:


Factory
Strategy
Handler
Repository
Dto
Adapter


---

## Unresolved

Structural candidates not explained by heuristics.

These are the **strongest indicators of dead code**.

---

## Namespace Drift

Detects mismatch between:


declared namespace
vs
physical folder structure


Clean architectures usually align both.

---

## Global Namespace

Classes declared without a namespace.

This reduces modular clarity.

---

## Isolated Core

Evaluates whether the Core layer remains isolated.

Healthy architecture:


Core → independent
Infra → depends on Core


---

## SOLID Alerts

Heuristic detection of possible violations:

- SRP
- LSP
- ISP
- DIP

These are **architectural signals**, not proofs.

---

# 📡 Architectural Radar

The dashboard includes a **radar chart** summarizing architectural health.

Values normalized between **0 and 1**.


0.00 – 0.10 healthy
0.10 – 0.25 moderate signal
0.25 – 0.50 architectural attention
0.50 – 1.00 high risk


---

# 🧠 Heuristic Refinement

Two layers reduce false positives.

## Pattern Signature Library

Detects known design patterns from class names.

Example:


OrderRepository
CommandHandler
UserFactory


---

## Structural Heuristics

Detects framework-related structural contexts.

Examples:


*.CLI
*.Infrastructure
*.Extensions
*.Options


---

# 📊 BI Datasets

RefactorScope generates datasets suitable for BI tools:

- Power BI
- QuickSight
- custom analytics

This enables **historical architecture tracking**.

---

# 🤖 AI Integration

Reports can be exported as **AI-ready structural dumps**.

Use cases:

- AI-assisted architecture review
- automated refactor suggestions
- design audits

---

# ⚙️ Configuration

Behavior is controlled through `refactorscope.json`.

```json
{
  "RootPath": "C:\\project",
  "Include": [ "src" ],
  "Exclude": [ "bin", "obj", "tests" ],
  "Parser": "CSharpRegex"
}
🚀 Usage
dotnet run --project RefactorScope.CLI

or

RefactorScope analyze
🔮 Roadmap

Future improvements:

Roslyn-based parser

ScottPlot visualizations

historical architecture analysis

plug-in library mode

advanced CI integration

multi-language analysis

⚠️ Limitations

The analysis is purely structural and static.

Runtime mechanisms may generate false positives:

Dependency Injection

Reflection

framework wiring

plugin architectures

Human review is always required.

📜 License

Open for educational use, research and architectural auditing.
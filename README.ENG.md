🧬 RefactorScope (English)

**https://romulommoraes.github.io/RefactorScope**

RefactorScope is a static structural analysis tool designed to audit, visualize, and ensure the architectural health of C# codebases.

Instead of focusing only on syntax errors or code formatting (like traditional linters), RefactorScope acts as a forensic structural hygiene scanner. It maps dependencies, evaluates adherence to architectural principles, and outputs rich visual dashboards to guide complex refactoring efforts.
💡 Origin (The Scriptome Project)

RefactorScope was born out of a practical need: to audit and guide the refactoring of Scriptome — a complex narrative analysis tool built in C#. As the system grew, it became critical to detect dead code, verify Core layer isolation, measure coupling, and identify namespace drift.

RefactorScope was then extracted as an independent, agnostic tool capable of analyzing any C# codebase.
🎯 Core Capabilities

    🔍 Fast Structural Auditing: Dependency extraction without compiling the code (independent of heavy APIs like Roslyn).

    🧠 Heuristic Dead Code Detection: Identification of dead code candidates (Zombies / Unresolved) refined by probabilistic rules (Dependency Injection, Polymorphism, etc.).

    🧱 Isolation and SOLID Analysis: Layer rule validation (e.g., Core must not depend on Infrastructure).

    🔗 Implicit Coupling: Mapping of architectural tensions, bottlenecks (Hubs), and calculation of the Distance from the Main Sequence (A/I/D).

    📊 Rich Exporting: Generation of interactive HTML Dashboards (HUD/Cyberpunk aesthetic), Markdown reports, and datasets for BI and AI integration.

📚 Documentation (Knowledge Base)

To keep this repository clean, detailed documentation has been split by domain. Refer to the guides in the docs/ folder to understand RefactorScope's inner workings:
Module	Document	Description
Overview	Architecture & Flow	The execution pipeline, engines, and domain tree.
Extraction Engine	Parser Design	How extraction strategies work (Regex, Textual, Hybrid).
Rules & Business	Methodology & Metrics	Details on Probabilistic Refinement, RDI (Effort), and the Smell Index.
Visualization	Export Artifacts	The anatomy of HTML Dashboards and the CLI interface.
Testing	Test Suite	Forensic coverage of the MVP and validated scenarios.
Governance	Decision Records (ADRs)	The project's architectural decision history.
Quick Reference	Technical Appendix	A consolidated summary of system formulas and thresholds.


⚙️ Configuration (refactorscope.json)

RefactorScope's entire behavior is governed by the refactorscope.json file, which must be placed in your execution root.
Example Configuration
```json
{
  "rootPath": "C:\\Users\\romul\\source\\repos\\Scriptome",
  "outputPath": "refactorscope-output",
  "include": [],
  "exclude": [ "bin", "obj", ".vs", "refactorscope-output" ],
  "parser": "Selective",
  "estimator": { "enabled": true },
  "analyzers": {
    "zombie": true,
    "zombieRefinement": true,
    "architecture": true,
    "coreIsolation": true,
    "entrypoints": true,
    "coupling": true,
    "solid": true,
    "statistics": true
  },
  "zombieDetection": {
    "enableRefinement": true,
    "globalRateThreshold_DI": 0.25,
    "globalRateThreshold_Interface": 0.20,
    "diProbability": 0.15,
    "interfaceProbability": 0.20,
    "minUnresolvedProbabilityThreshold": 0.30
  },
  "layerRules": {
    "UI": { "namespaceContains": [ "Reporting", "CLI" ] },
    "Core": { "namespaceContains": [ "Core", "Analyzers", "Model" ] },
    "Infra": { "namespaceContains": [ "Infrastructure" ] }
  },
  "exporters": [ "htmlDashboard", "dumpAnalysis" ],
  "dashboard": { "theme": "midnight-blue" }
}
```

Options Breakdown

    ⚠️ ARCHITECTURAL RECOMMENDATION: It is strongly advised to keep the values in the zombieDetection and analyzers sections at their factory defaults. The system has been exhaustively calibrated to reduce false positives. Altering the probabilistic thresholds can destabilize the results and the effort calculation (RDI).

    rootPath / outputPath: Absolute or relative paths for the project to be analyzed and where the reports will be generated.

    include / exclude: Arrays of strings to filter directories. (It is highly recommended to always exclude bin, obj, and test folders).

    parser: Defines the reading engine.

        Options: "CSharpRegex" (Fast), "Selective" (Hybrid and Recommended), "Adaptive" (Recovery fallback), "Incremental".

    analyzers & estimator: Toggle switches (true/false) to turn parts of the system's intelligence on/off. Keep them all active for the full experience.

    zombieDetection: (Legacy naming for Structural Candidates). Controls the weights of the Dependency Injection and Polymorphism heuristics. Keep the default values.

    layerRules: Extremely important. Here you map your architecture. Use namespaceContains, nameStartsWith, or nameEquals to teach RefactorScope which folders/namespaces belong to which layers (UI, Core, Infra, etc).

    exporters: Which formats to output.

        Options: "htmlDashboard", "dumpAnalysis" (Consolidated JSON), "dumpIA" (JSON optimized for LLM prompts).

    dashboard.theme: Defines the visual palette of the panels.

        Options: "midnight-blue" (Default), "ember-ops", "neon-grid".

🚀 Quick Start

With your refactorscope.json configured:
Bash

# Running via source code
dotnet run --project RefactorScope.CLI

# Or via installed tool
RefactorScope analyze

Human review is always required. RefactorScope points out the probabilistic evidence; the architect makes the refactoring decisions.
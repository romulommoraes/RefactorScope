# Output & Telemetry Showcase

See RefactorScope in action. The output architecture was designed to provide immediate forensic feedback in the terminal (CLI) and deep visual projections through generated dashboards.

## 01 // The CLI Experience
The analysis engine running in real time. Fast, verbose, and color-coded to highlight structural alerts based on its heuristic matrix.

<div id="cli-terminal-anchor"></div>

## 02 // Selector Modes (Scan Modes)
The CLI behavior adapts to the scope required by the analyst.

<div class="cyber-grid-2x2">
    <div class="selector-card">
        <div class="card-kicker">MODE: --fast</div>
        <h3>Heuristic Regex Scan</h3>
        <p>Ignores deep transitive dependencies and focuses on validation through structural regex rules. Ideal for continuous CI/CD pipelines.</p>
    </div>
    <div class="selector-card">
        <div class="card-kicker">MODE: --strict</div>
        <h3>Deep AST Parsing</h3>
        <p>Builds the complete syntax tree. Correlates complex data involving inheritance, dependency injection, and cyclomatic metrics.</p>
    </div>
</div>

## 03 // Parser Arena Breakdown
RefactorScope analysis consolidates all intelligence tracking into pure HTML reports, free from external dependencies.

<div class="image-showcase panel" augmented-ui="tl-clip br-clip border">
    <img src="assets/img/arenabreakdown.jpeg" alt="HTML Dashboard Example" style="width: 100%; display: block;">
</div>

## 04 // Generated Static Dashboards
RefactorScope analysis consolidates all intelligence tracking into pure HTML reports, free from external dependencies.

<div class="image-showcase panel" augmented-ui="tl-clip br-clip border">
    <img src="assets/img/arenacharts.jpeg" alt="HTML Dashboard Example" style="width: 100%; display: block;">
</div>

# 📤 Export Artifacts (Output Generation)

The `Export` module is responsible for translating the agnostic structural model into high-value artifacts for decision-making. It generates two main formats: **Interactive Dashboards (HTML)** for deep visual exploration and **Textual Reports (Markdown)** for quick auditing, PR (Pull Request) reviews, and static documentation.

## 📊 Interactive Dashboards (HTML)

The HTML panels are statically generated (do not require a web server) and use client-side rendering technologies to deliver a fluid experience. All support dynamic theme switching (e.g., *Midnight Blue*, *Ember Ops*, *Neon Grid*).

* **`index.html` (RefactorScope Hub)**
  Acts as the main entry point (Landing Page). It consolidates navigation and provides a high-level summary that redirects the user to specialized panels.

* **`ArchitecturalDashboard.html`**
  The systemic health panel. Focused on architectural macro-metrics, it displays dependency relationships between modules, instability analysis, abstraction levels, and the project's architectural tensions.

* **`StructuralDashboard.html`**
  The forensic panel. Focused on internal code structure, it displays type hierarchy, alignment between physical directories and logical namespaces (Logical/Architectural Drift), and visually maps dead code candidates (Zombies).

* **`QualityDashboard.html`**
  The rules validation panel (Gates). Shows the project's pass or fail status against configured quality rules (Fitness Gates), such as Core isolation, coupling limits, and forbidden dependencies.

* **`ParsingDashboard.html`**
  The engine telemetry panel. Displays technical execution metrics: processing time, structural extraction success rate, volume of files classified as *Safe* vs *Complex*, and possible parser plausibility alerts.

* **Comparison Panels (Arena Mode)**
  * *`ParserArenaDashboard.html`*, *`ParserComparativeDashboard.html`*, and *`ParserComparativeSelfDashboard.html`*
  Generated specifically when RefactorScope runs in `BatchArena` or `Comparative` mode. They do not analyze the project code itself, but rather the **performance of the parsing engines**. They show comparisons of time, allocation, and coverage between the Regex, Textual, and Hybrid engines, helping to choose the best strategy for the repository.

---

## 📄 Textual Reports (Markdown)

Lean and straightforward files, optimized for quick human reading, Release Notes publishing, and integration with CI/CD systems.

* **`Relatorio_Executivo.md`**
  The managerial summary of the analysis run. 
  * Provides the final operational interpretation (Overall Readiness).
  * Consolidates primary signals: how many dead code candidates were found, how many were explained by pattern similarity (DI, Interfaces), and how many require strict manual inspection.
  * Reports the parser's confidence band (e.g., *High Confidence*).

* **`Relatorio_Arquitetural.md`**
  The technical deep dive focused on formal software design metrics.
  * **Folder Structure:** Generates the clean tree of the analyzed scope.
  * **Classic Metrics:** Explains and calculates Abstraction (A), Instability (I), and Distance from the Main Sequence (D) for each module, based on Robert C. Martin's theory.
  * **Implicit Coupling:** Lists classes that exhibit a heuristic anomaly of strong directional dependency concentration (high *fan-out* to specific modules), serving as an alert for refactoring such as extracting adapters or orchestrators.
  
  🛠️ Tech Stack

RefactorScope is designed to be lightweight, self-contained, and visually striking. The technological architecture is divided into three main pillars: the analysis engine, the static frontend (Dashboards), and the terminal interface.
Analysis Engine (Backend Core)

    C# / .NET 8+: The foundation of the project. All parsing, structural classification, and metric calculations (Instability, Abstraction, Effort Estimation) are executed natively.

    Advanced Regular Expressions (Regex): Heavily utilized in the fast-parsing engines and fallback heuristics, ensuring high performance without relying on heavy compiler APIs like Roslyn.

Dashboards and Visualization (Frontend Exports)

The HTML panels generated by the Export module do not require heavy SPA libraries (like React or Angular). They rely on a lightweight visual stack focused on a Cyberpunk / Sci-Fi aesthetic and high rendering performance:

    p5.js: A canvas-based creative coding library. Used to render complex and dynamic visualizations that traditional CSS cannot handle (such as dependency graphs and free-form structural plotting).

    Charts.css: A pure CSS data visualization framework. It ensures ultra-lightweight and responsive charts (bars, lines) without the overhead of heavy JavaScript charting libraries.

    Augmented UI: A CSS library specialized in futuristic borders and "chamfered" layouts. It provides the tactical/military visual identity (cut-out panels, optical crosshairs, and angled corners) present across all dashboards.

💻 Command Line Interface (CLI)

RefactorScope is not just a background tool; it provides rich, real-time feedback in the terminal during processing. To break the visual limitations of traditional consoles, the CLI module leverages a specialized library:
🎨 Spectre.Console

All terminal rendering — from the initial header to the effort estimation tables — is orchestrated by Spectre.Console.

Through our static infrastructure class TerminalRenderer, we encapsulate Spectre's power to create a unique visual identity (Neon Matrix / Hacker aesthetics), utilizing:

    Rich Markup: Absolute hexadecimal colors (like neon green #00ff00, hotpink, and deepskyblue1) to highlight warnings, successes, and critical metrics.

    Advanced Components: Rendering of Panels (double-bordered boxes), Tables (dynamic tabular structures to list modules and health scores), and Rules (centered divider lines).

    Asynchronous Spinners: Visual progress indicators (Status().Spinner()) that keep the user informed during heavy parsing stages without freezing the console output.

    Inline Progress Bars: The CLI builds heuristic effort bars (█░░░) directly in the terminal log, allowing immediate interpretation without needing to open the final report.
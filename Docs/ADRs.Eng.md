

# Architecture Decision Records (ADRs) - RefactorScope

## ADR-001 — Fundamental Architecture and Flow Pipeline of RefactorScope
**Status:** Accepted

### Context
RefactorScope was designed as a structural analysis tool to support architectural refactoring processes, focusing on predictability, modularity, and offline diagnostics. Tools of this kind frequently suffer from recurring problems:
* Coupling between modules.
* Implicit and hard-to-audit pipelines.
* Mixing of parsing, analysis, and exporting.
* Uncontrolled scope creep.

The goal of RefactorScope 1.x is to maintain an explicit, deterministic, and modular pipeline.

### Decision
RefactorScope adopts an architecture based on an **Orchestrated Pipeline of Extraction, Analysis, and Consolidation**.

**Main Flow:**
```text
Configuration / CLI
   ↓
Parser
   ↓
Structural Model
   ↓
Orchestrator
   ↓
[ Independent Analyzers ]
   ↓
Results
   ↓
Consolidation
   ↓
Export
```

### Flow Pipeline Description

#### 1. Configuration
Execution begins at the CLI and the `refactorscope.json` file, which define:
* Analysis scope.
* Inclusion/exclusion of folders.
* Active analyzers.
* Layer rules.
* Thresholds and gates.

#### 2. Parsing
The source code is processed by a structural parser. The goal of this stage is not to compile, but to extract a structural model reliable enough for analysis.

**Typical outputs:**
* Files.
* Namespaces.
* Types.
* References.

#### 3. Structural Model
The parser generates an agnostic intermediate model, used as input for the entire analytical phase.
This model decouples:
* The actual source code.
* The analyzers.
* The exporters.

#### 4. Analysis
The analyzers operate on the structural model and calculate signals such as:
* Structural candidates.
* Zombie code risk.
* Coupling.
* Architectural metrics.
* Heuristic refinements.
* Quality gates.

#### 5. Consolidation
The results emitted by the analyzers are gathered into a consolidated report, without the analyzers depending on each other.

#### 6. Export
The export layer converts the results into human or analytical consumption artifacts, such as:
* HTML Dashboards.
* Markdown reports.
* CSV Datasets.
* Auxiliary dumps.

### Architectural Principles
* Explicit pipeline.
* Independent analyzers.
* Communication via Result Objects.
* Predictable execution.
* Core decoupled from UI.
* Separation between extraction, analysis, and output.
* 1.x scope centered on structural analysis.

### Consequences
**Positive:**
* Execution predictability.
* Ease of expansion.
* Less coupling between modules.
* Greater flow auditability.

**Negative:**
* More intermediate objects.
* Need for stable contracts.
* Greater architectural discipline required to avoid shortcuts.

---

## ADR-002 — Agnostic Structural Model as Central Contract
**Status:** Accepted

### Context
Directly analyzing C# code within the analyzers would couple the system to the parser and reduce future evolution capabilities. The project needed an intermediate contract that allowed:
* Switching parsing strategies.
* Supporting new languages in the future.
* Keeping analyzers independent of the concrete parser.

### Decision
RefactorScope uses an **Agnostic Structural Model** as the central contract between parsing and analysis.

**Flow:**
```text
Source Code
   ↓
Parser
   ↓
Structural Model
   ↓
Analyzers / Exporters
```

### Main Components

**ArquivoInfo (FileInfo)**
Represents an analyzed file. Typically contains:
* Relative path.
* Namespace.
* Declared types.
* Associated source code.

**TipoInfo (TypeInfo)**
Represents structural entities such as:
* Classes.
* Interfaces.
* Records.
* Structs.

Contains:
* Name.
* Namespace.
* Logical type.
* Declaration file.
* Associated references.

**ReferenciaInfo (ReferenceInfo)**
Represents detected dependencies or relationships between types. Examples:
* Mention.
* Instantiation.
* Generic usage.
* `typeof`.
* `nameof`.

### Principles
* Analyzers operate on structure, not raw syntax.
* Parser is replaceable.
* Model is language-neutral.
* Outputs do not depend directly on a concrete parser.

### Consequences
**Positive:**
* Decoupling between parsing and analysis.
* Future support for multiple parsers.
* Greater readability of the pipeline.
* Better testability of analyzers.

**Negative:**
* The model must be strictly maintained.
* Parser detection limitations impact the rest of the pipeline.

---

## ADR-003 — Independent Execution of Analyzers and Central Consolidation
**Status:** Accepted

### Context
One of the most common sources of architectural drift in analytical tools is the coupling between analysis rules. When analyzers start depending on each other, problems arise such as:
* Implicit execution order.
* Side effects.
* Testing difficulty.
* Accidental growth of dependencies.

### Decision
All RefactorScope analyzers must operate in isolation, receiving only the analysis context and emitting their own result.

**Contract:**
```csharp
public interface IAnalyzer
{
    IAnalysisResult Analyze(AnalysisContext context);
}
```

### Rules
**An analyzer:**
* Receives `AnalysisContext`.
* Executes in isolation.
* Returns `IAnalysisResult`.

**An analyzer must not:**
* Directly query the result of another analyzer.
* Access the filesystem as part of the analysis.
* Modify the shared context.
* Depend on an external implicit order.

### Consolidation
The responsibility of gathering the results belongs to the Central Orchestrator, which produces the `ConsolidatedReport`.

The orchestrator:
* Selects active analyzers.
* Executes analyzers.
* Collects results.
* Consolidates outputs.

The orchestrator must not contain analytical domain logic that belongs to the analyzers.

### Consequences
**Positive:**
* Independently testable analyzers.
* Predictable pipeline.
* Less lateral coupling.
* Safer system evolution.

**Negative:**
* Cross-results need to be explicitly modeled.
* Composite analyses require a clear consolidation step.

---

## ADR-004 — Structural Precision and Probabilistic Heuristics over Total Coverage
**Status:** Accepted

### Context
In static structural analysis, attempting to cover all possible scenarios usually generates an increase in false positives, especially in modern architectures with:
* Dependency Injection (DI).
* Strategy / Factory.
* Usage via interfaces.
* Bootstrap in `Program.cs`.
* Indirect activation patterns.

In the context of RefactorScope, a useful tool must be conservative enough not to flag expected architectural behavior as a problem.

### Decision
RefactorScope prioritizes:
**Structural precision over universal coverage.**

This implies consciously accepting that:
* Some false negatives will exist.
* Total coverage is not the primary goal.
* Probabilistic heuristics are preferable to aggressive inferences.

### Practical Application
This decision grounds rules such as:
* Probabilistic refinement of zombie detection.
* Suspicion reduction when there is explicit DI registration.
* Suspicion reduction when a corresponding interface and polymorphic usage exist.
* Protection for expected structural types.
* Protection for bootstrap and top-level startup.

It also grounds decisions like:
* Relative `SmellIndex` instead of an absolute one.
* Explicitly documented detection scope.
* Distinction between structural suspicion and confirmation.

### Assumed Detection Scope
The system explicitly detects signals such as:
* References via `new`.
* Generic usage.
* `typeof(T)`.
* `nameof(T)`.
* Explicit DI registrations.
* Interface usage.

The system does not intend to universally detect:
* Dynamic reflection.
* Plugin loading.
* Assembly scanning.
* Runtime code generation.

### Consequences
**Positive:**
* Fewer false positives.
* More reliable analysis.
* More useful heuristics in real-world projects.
* Better practical acceptance of the tool.

**Negative:**
* Part of the dynamic behavior falls outside the scope.
* Results must be interpreted as evidence, not absolute truth.
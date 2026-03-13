# Parsing Engines and Analysis Methodology

## Overview

RefactorScope's parser is the engine responsible for converting C# source code into an agnostic structural model **(ModeloEstrutural)**.

It was designed to be extremely fast, tolerant of syntax errors, and independent of compiler APIs (such as **Roslyn**). Instead of building a heavy AST, the system focuses on extracting vital architectural signals: namespaces, declared types, and dependency references.
Parsing Strategies

The ecosystem evolved from isolated parsers to a hybrid and adaptive system, supporting 5 main execution strategies:
1. RegexFast **(Baseline)**

Parser based exclusively on Regular Expressions. It is the structural backbone of the system.

    Advantages: Extremely high performance, ignores modern C# syntax noise (record, init, with), and extracts references with global coverage.

    Disadvantages: May lose context in highly complex nestings.

2. Selective **(Recommended Default)**

An intelligent hybrid parser that unites the best of both worlds.

    Executes the ClassComplexityClassifier to separate files into two groups: SAFE and COMPLEX.

    COMPLEX files are processed solely by the robustness of Regex.

    SAFE files receive refinement from the Textual parser.

    Safe Merge: Regex defines the global baseline, and Textual only injects additional dependencies coming from safe files, ensuring no duplication in the graph.

3. Adaptive **(Experimental)**

Focused on highly damaged repositories or generated code.

    Executes RegexFast first.

    Evaluates the strength of the extracted structure. If the extraction is too poor (e.g., < 3 detected types), it triggers the secondary parser in full recovery mode. If healthy, it extracts only additional dependencies.

4. Incremental **(Experimental)**

Focused on computational cost savings in massive codebases.

    If the primary model (Regex) hits the relational density and plausibility targets, the secondary parser is completely ignored.

5. Comparative **(Parser Arena)**

Not an isolated parser, but a batch orchestrator. It executes all the above strategies across multiple projects, generates comparative performance/coverage scores, and outputs an HTML dashboard to evaluate which engine performed best on the repository.
Extraction and Validation Methodology

The parsing pipeline follows a rigorous flow of sanitization and validation to ensure that false positives (like strings being read as classes) do not contaminate the model.
1. Lexical Sanitization (IPreParser & SanitizedSourceProvider)

Before any extraction, the raw code goes through a cleanup:

    Removal of block /* */ and line // comments.

    Neutralization of literal and verbatim strings (@"...").

    In the case of the Textual parser, the HigienizadorLexico (Lexical Sanitizer) acts at line-reading time (streaming) to preserve memory allocation performance.

2. Structural Token Guard (StructuralTokenGuard)

Captured identifiers pass through a conservative sanitary barrier:

    Filters out C# reserved keywords.

    Eliminates known false positives in XML documentation (e.g., summary, remarks, misuse, example).

    Requires compliance with the signature of a valid C# identifier.

3. Local Recovery (RegexLocalRecovery)

When the Textual parser loses scope tracking (unbalanced { } braces), instead of discarding the file or corrupting the model, it isolates the text buffer read up to that point and applies a targeted Regex recovery only for the project's known types.
4. Plausibility Evaluation (PlausibilityEvaluator)

The final step of any parsing. The evaluator detects silent failures.
For example: if the parser processes a 5,000-line file and returns 0 structural types, the model is flagged with a PlausibilityWarning. This alerts the pipeline that the parser swallowed the code without crashing, but the structural extraction was statistically implausible.
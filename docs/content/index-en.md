# RefactorScope

I have a feeling almost every developer has been through this at some point during a refactor: one change pulls another, errors start cascading, warnings show up everywhere, the project stops compiling with confidence… and at some point you no longer know what is legacy, what is still used, where coupling started to tighten, or even where namespaces stopped making sense.

I was exactly at that point with a personal project.

The refactor had become painful enough that I seriously considered the most tempting path of all: abandoning the current codebase and starting over from scratch with a new architecture.

But then a different question showed up:

**What if, instead of starting over in the dark, I had a tool that could show me where the architectural tension actually was?**

That was the beginning of **RefactorScope**.

Over the last **14 days**, from the first commit to this launch, I paused everything else to build a **static structural analysis tool** focused on auditing and guiding the refactoring of C# projects. The idea started small, but the scope expanded a lot during the process — and I grew along with it as a developer, software engineer, and architect.

I worked alone, but with strong AI support throughout the development cycle — mainly **ChatGPT** and **Gemini**, often auditing the code and my architectural decisions. In the end, it became much less about “using AI to generate code” and much more about **using AI as critical friction to think better**.

The result is a tool built to answer questions that, in large refactors, are usually painful to answer manually:

- where implicit coupling is emerging
- where legacy code is tangled with current code
- where there are real dead-code candidates
- where structural drift exists between folders, modules, and namespaces
- how healthy the architecture is, in a way that is more visual and auditable

RefactorScope approaches this through a lightweight structural parsing pipeline, without depending on full compilation, using multiple extraction strategies and heuristic refinement to generate:

- HTML dashboards
- Markdown reports
- analytical datasets
- structural support artifacts for inspection and decision-making

Some of the parts I’m most proud of in this project are:

- the **hybrid parser**, designed to balance speed and resilience
- the **heuristic layer** that reduces false positives in dead-code detection
- the **HUD-style dashboards**, which made structural analysis much more readable
- the tool’s own **architectural separation**, born from a real pain point and eventually shaped into an agnostic product

It was originally created to help me refactor one specific project.

Along the way, it became something bigger: an independent tool for inspecting the architectural health of **any C# codebase**.

It was intense, exhausting, fun, and deeply formative.

Today I’m publishing the **MVP**.

The GitHub repository, documentation, and technical details are all part of this release — and honestly, I’m happy not only with what I built, but with how much the process itself pushed me forward.
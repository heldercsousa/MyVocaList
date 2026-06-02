# S1.2 — Implementation Levels

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-28 | Researched | Content written by research agent |

---

## Overview

Spec-Driven Development (SDD) is not a binary switch — it is a spectrum. Teams adopt it at different depths depending on system risk, team maturity, AI tooling quality, and tolerance for non-determinism. Three distinct implementation levels have emerged as the practical taxonomy for this spectrum: **Spec-First**, **Spec-Anchored**, and **Spec-as-Source**.

Each level answers one core question differently: *how long does the specification remain authoritative after the code is written?*

The practical rule of thumb (from arXiv 2602.00180, "Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants", 2026) is: **use the minimum level of specification rigor that removes ambiguity for your context.** Higher is not always better — spec-as-source applied prematurely increases friction without proportional gain.

---

## Level 1: Spec-First

### Definition
A specification is written before implementation begins and is used as the primary prompt or brief for AI-assisted code generation. Once the initial code is working, the spec may be archived, discarded, or simply never updated again. Code ownership passes fully to the developer.

### Characteristics
- Spec lives for the duration of a single task or feature sprint
- Code diverges from spec over time as requirements evolve
- No tooling enforces consistency between spec and implementation
- Spec functions as a high-quality prompt, not a living contract
- Lowest barrier to adoption — no workflow change beyond "write the spec before you code"

### When to Use
- Bounded features with well-understood scope
- Throwaway or prototype code
- First adoption of SDD by a team: build the habit before adding governance
- Situations where the spec author and implementer are the same person (solo developer, AI pair)

### Examples
- Writing a `requirements.md` file before prompting Claude Code to scaffold a CRUD page
- Authoring a user story in GitHub Issues and feeding it to GitHub Copilot Workspace to generate a PR
- Creating an OpenAPI `paths` block before implementing a single endpoint
- MyVocaList's own workflow: `Docs/specs/[feature]/` files written before any implementation code

### Limitations
Spec-first provides no protection against spec-code drift. A spec written in January may bear no relation to the code running in March. For long-lived features maintained by teams, this drift becomes a liability.

---

## Level 2: Spec-Anchored

### Definition
The specification persists as a living document beyond the initial implementation. It is updated whenever behavior changes, and it remains the authoritative reference for future development, debugging, and onboarding. Code and spec co-evolve.

### Characteristics
- Spec is version-controlled alongside code (same repo, same commit discipline)
- PRs that change behavior are expected to update the spec
- Spec files serve as documentation, architectural decision records (ADRs), and AI context in future sessions
- No automated generation — humans still write and own both spec and code
- Reviewed as part of code review

### When to Use
- Core domain features that will be maintained for months or years
- Shared services consumed by multiple teams
- Complex business logic with non-obvious validation rules
- Any feature where future AI sessions need reliable context to continue work safely

### Examples
- GitHub's [spec-kit](https://github.com/github/spec-kit): a toolkit providing conventions for spec files that live alongside code and serve as AI context
- Amazon Kiro IDE (launched July 2025): built-in spec document support that keeps requirements, design, and tasks linked to implementation files
- MyVocaList's three-file spec structure (`requirements.md`, `design.md`, `tasks.md`) checked into `Docs/specs/` — these are spec-anchored because they are updated as the feature evolves and consulted by every new AI session

### The Sweet Spot
Spec-anchored is considered the practical sweet spot for most production systems. It captures the main benefits of SDD — clear requirements, verifiable acceptance criteria, AI context — without requiring code generation tooling to be mature or trusted. The JetBrains Junie team, Thoughtworks, and Augment Code all describe spec-anchored as the level most teams should target for sustainable AI-assisted development.

---

## Level 3: Spec-as-Source

### Definition
The specification is the only artifact humans directly edit. Code is fully generated from the spec and is treated as a build artifact — never manually modified. Changing behavior means changing the spec and regenerating. Generated files may carry comments like `// GENERATED FROM SPEC — DO NOT EDIT`.

### Characteristics
- Spec is the single source of truth; code is a derived artifact
- A generation pipeline (CLI tool, IDE plugin, or AI agent) translates spec to runnable code on each change
- Human code review focuses on spec changes, not generated code
- Requires high confidence in the generator's correctness and coverage
- Highest governance, highest tooling dependency

### When to Use
- Domains with well-established, stable code generation: OpenAPI → server stubs, Protobuf → client libraries, Simulink models → certified embedded C code
- Highly regulated systems where code provenance must be traceable to a formal requirement
- Large surface-area APIs where maintaining consistency by hand is error-prone
- When the generation tooling is mature, tested, and trusted by the team

### Examples
- **OpenAPI / Swagger codegen**: API server stubs and client SDKs generated from an `.yaml` spec — the canonical industry example of spec-as-source. Developers edit the OpenAPI file; code is never hand-authored.
- **Protobuf / gRPC**: `.proto` files are the spec; all language bindings are generated artifacts.
- **Simulink → Embedded C** (automotive): MATLAB/Simulink models are the spec; certified C code for ECUs is generated and never edited directly.
- **Tessl** (2025 startup): AI-native platform that aims to make spec-as-source viable for general-purpose web development; still early-stage.
- **Amazon Kiro** advanced mode: experimental workflow where the full task list is executed by an agentic loop with no human edits to generated files.

### Limitations
Spec-as-source is not yet viable for general-purpose application development. Generator quality, test coverage of generated code, and debuggability of generation failures remain barriers. The Thoughtworks Technology Radar (2025) rates it "Assess" — promising for greenfield projects in the right domains, but not recommended for broad adoption.

---

## Progression Between Levels

Teams do not jump from zero to spec-as-source. The natural progression follows maturity and trust:

### Stage 1 → Spec-First (weeks)
Start by writing a spec before every new feature. Use it as the AI prompt. Build the habit. Measure whether the spec reduces back-and-forth with the AI and produces better first-draft code.

**Gate to advance:** The team consistently writes specs before coding and reports that AI output quality improved.

### Stage 2 → Spec-Anchored (months)
Introduce the discipline of updating specs when behavior changes. Add spec review to the PR checklist. Store specs in the repo. Start using them as AI context in follow-up sessions ("read `design.md` before making changes to this service").

**Gate to advance:** Specs are reliably current. New team members use them to onboard. Future AI sessions can pick up context from the spec without requiring a full re-explanation.

### Stage 3 → Spec-as-Source (conditional)
Only attempt this for subsystems where a reliable generator exists or can be built. Do not attempt for UI, complex business logic, or any domain where the generation fidelity cannot be verified by automated tests.

**Gate to advance:** A working generator exists, output is covered by tests, and the team has a debuggable path back from generated code to spec when something goes wrong.

### Regression is Normal
Teams often operate at different levels for different parts of the codebase simultaneously. A team may use spec-as-source for its API layer (OpenAPI codegen) while using spec-anchored for its domain services and spec-first for throwaway tooling scripts.

---

## Current State of Practice (2025–2026)

As of early 2026, the majority of professional development teams using AI assistants operate at **Level 1 (Spec-First)** or are transitioning toward **Level 2 (Spec-Anchored)**.

Key data points:
- **Y Combinator Winter 2025 batch**: 25% of startups reported codebases that were 95%+ AI-generated — most using informal prompt-based approaches (pre-spec-first)
- **DORA 2025**: 90% developer AI adoption; 80%+ reported productivity benefits; teams that used structured specs with AI reported 3.6 hours/week saved over unstructured AI use (DX Research)
- **Thoughtworks Tech Radar 2025**: Spec-driven development rated "Trial" — adopt it for new projects, evaluate for existing ones
- **Amazon Kiro** (July 2025): First major AI IDE to make spec-anchored development a first-class built-in workflow, not a convention layered on top

The main barriers to Level 3 adoption remain tooling maturity and the difficulty of debugging generated code. For most application development teams in 2025–2026, spec-anchored is the practical ceiling and the recommended target.

---

## Sources

- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants (arXiv:2602.00180)](https://arxiv.org/html/2602.00180v1)
- [Spec Driven Development — Three Maturity Levels Every AI Team Should Know (Medium)](https://medium.com/@wasowski.jarek/spec-driven-development-three-maturity-levels-every-ai-team-should-know-648c93cf1e1d)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl (Martin Fowler)](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices (Thoughtworks)](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [What Is Spec-Driven Development? A Complete Guide (Augment Code)](https://www.augmentcode.com/guides/what-is-spec-driven-development)
- [How to Use a Spec-Driven Approach for Coding with AI (JetBrains Junie)](https://blog.jetbrains.com/junie/2025/10/how-to-use-a-spec-driven-approach-for-coding-with-ai/)
- [Spec-Driven Development in 2025: The Complete Guide (SoftwareSeni)](https://www.softwareseni.com/spec-driven-development-in-2025-the-complete-guide-to-using-ai-to-write-production-code/)
- [Spec-Driven Development with AI: Complete 2025 Guide (dplooy)](https://www.dplooy.com/blog/spec-driven-development-with-ai-complete-2025-guide/)
- [GitHub spec-kit repository](https://github.com/github/spec-kit)
- [Kiro AI IDE](https://kiro.dev/)

# S1.3 — SDD vs TDD/BDD/Waterfall

**Status:** Researched
**Predecessor(s) ID:** S1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; sources from arXiv:2602.00180, Thoughtworks, Martin Fowler, Marmelab, Planu, and practitioner resources |

---

## Overview

SDD is not a replacement for Test-Driven Development (TDD), Behavior-Driven Development (BDD), or Waterfall. Rather, it builds on the foundational insights of TDD and BDD while avoiding the structural failures that made Waterfall unsuitable for most modern projects. Understanding the relationship between these methodologies is essential for applying SDD effectively.

The key insight from both the academic literature (arXiv:2602.00180) and practitioners (Thoughtworks, Martin Fowler, Planu): **SDD is TDD and BDD at a higher scope**, adapted for the reality that the implementer is an AI agent rather than a human developer.

---

## SDD vs. TDD (Test-Driven Development)

### Core Relationship

Test-Driven Development is actually **SDD at the unit level**. Both share the foundational principle: "specify first, implement second."

| Aspect | TDD | SDD |
|--------|-----|-----|
| **Scope** | Function / module | Feature / system |
| **Artifact** | Test code | Specification document |
| **Before code?** | Yes (test is written first) | Yes (spec is written first) |
| **Input format** | Code (test syntax) | Natural language or structured document |
| **Verification** | Red → Green → Refactor | Spec validation → Plan approval → Task execution |
| **Time to feedback** | Per test run | Per feature generation (minutes, not seconds) |

### How SDD Extends TDD

1. **Scope elevation:** TDD verifies correctness of a single function; SDD verifies alignment of the entire feature against requirements.
2. **Time compression:** In TDD, the human writes test code and then implementation code. In SDD with AI, the human writes a spec and the AI generates both tests and implementation from it — condensing a cycle that previously took hours into one that takes minutes.
3. **AI agent context:** TDD assumes the implementer (a human) carries context across sessions. AI agents start fresh every session. SDD's spec serves as persistent context that survives session boundaries, making AI-generated code coherent across multiple implementation passes.
4. **Front-loaded human judgment:** TDD distributes human attention across every test case. SDD concentrates human judgment into a single spec-review gate before implementation begins — a more efficient use of human time when implementation is AI-driven.

### Complementary, Not Competing

The arXiv paper and Thoughtworks both emphasize: **SDD should embed TDD at its leaf nodes.** A complete SDD implementation includes:
- Spec → Plan → Tasks (SDD scope)
- Within each task: Red/Green/Refactor verification (TDD pattern)

The strongest agentic workflows wrap TDD's tight local correctness loop inside SDD's structured global direction loop.

---

## SDD vs. BDD (Behavior-Driven Development)

### Core Relationship

Behavior-Driven Development is the **most direct ancestor of modern SDD**. BDD introduced the idea that specifications can be executable (Given/When/Then scenarios). SDD inherits that insight and extends it.

| Aspect | BDD | SDD |
|--------|-----|-----|
| **Primary goal** | Bridge business and technical teams | Make specs the authoritative artifact for AI generation |
| **Spec format** | Given/When/Then (Gherkin) | Structured markdown with acceptance criteria, design, tasks |
| **Collaboration** | Business users + developers | Analysts + developers + testers (in mature practice) |
| **Code generation** | Manual (developer implements) | AI-assisted (agent generates from spec) |
| **Scope** | Behavior specification | Feature specification + architecture + implementation plan |
| **Maintenance** | Living docs via test automation | Version-controlled specs in Git |

### What SDD Adds to BDD

1. **Architecture capture:** BDD excels at behavioral scenarios. SDD adds technical design, service boundaries, and architectural decisions to the spec.
2. **AI-native generation:** BDD requires a developer to interpret Gherkin and code the implementation. SDD specs are machine-consumable prompts that AI agents can execute directly.
3. **Task decomposition:** BDD doesn't address how to decompose a feature into concrete development tasks. SDD formalizes this as the "Tasks" phase: explicit, ordered, dependency-aware work units.
4. **End-to-end artifact ownership:** In BDD, QA often "owns" the Gherkin scenarios, but developers still own the code and architecture. In SDD, a single spec document can govern both behavior validation and implementation generation, unifying ownership.

### The BDD/SDD Integration

From practitioner sources (Planu, sdd.sh): mature SDD practice often **begins with BDD-style Given/When/Then acceptance criteria** but expands them into a fuller specification that includes:
- Functional requirements (the "what" and "why")
- Technical design (architecture, stack, constraints)
- Edge cases and error handling
- Acceptance criteria (derived from scenarios)
- Task list (derived from design)

In this view, **BDD is the collaborative foundation; SDD is the execution framework.**

---

## SDD vs. Waterfall

### The Waterfall Comparison Debate

SDD is frequently compared to Waterfall because both involve upfront specification. This comparison has validity but misses critical differences.

### Why the Comparison Arises

Waterfall's structure: Requirements (weeks) → Design (weeks) → Implementation (months) → Testing (months) → Deployment (once).

SDD's structure: Specify (hours/days) → Plan (hours) → Tasks (hours) → Implement (minutes/hours per task) → Validate → Iterate.

Both start with specification. Both separate planning from execution. Both create an artifact (spec/design doc) before code is written. The surface similarity spawned critical analyses (notably Marmelab's "Waterfall Strikes Back" Nov 2025) arguing that SDD resurrects the problems Agile solved.

### The Critical Difference: Feedback Loop Duration

The arXiv paper and Alex Cloudstar (alexcloudstar.com, Mar 2026) identify the fundamental distinction:

**In Waterfall:** The feedback loop between spec and implementation is months long. You write a 200-page requirements document, hand it to developers, and wait 3–6 months for working software. By then, the spec is wrong, but the cost to regenerate is catastrophic.

**In SDD with AI:** The feedback loop is **minutes to hours.** You write a spec, the agent generates implementation in 5–15 minutes, you review it, find the spec was incomplete, update the spec, and regenerate. The cost of discovering the spec was wrong is nearly zero.

That is not a surface similarity — it is a **category difference** that fundamentally changes the economics of specification-driven work.

### Waterfall's Root Failure

Waterfall failed not because specifications are inherently bad, but because:
1. The cost of discovering a specification was wrong was prohibitively high (6+ months to rework).
2. Requirements changed during the project, making upfront specifications obsolete.
3. Developers had no feedback from working software to refine their understanding.

**SDD solves the cost problem** by regenerating code in minutes. **SDD addresses the change problem** by treating specs as living documents that are updated continuously (before code changes, not after). **SDD enables feedback** by producing working software within hours, not months, allowing refinement based on what the team actually sees.

### Waterfall vs. SDD: Key Distinctions

| Aspect | Waterfall | SDD |
|--------|-----------|-----|
| **Feedback loop** | 3–6 months | 15 minutes |
| **Regeneration cost** | Catastrophic | Negligible |
| **Spec maintenance** | Static (written once) | Living (updated continuously) |
| **Changeability** | Discouraged (too costly) | Embraced (cheap) |
| **Developer role** | Executor (read spec, code it) | Director (guide AI, validate output) |
| **Iteration** | After implementation (expensive rework) | Before implementation (cheap retry) |

### Where the Waterfall Critique Has Traction

Critics of SDD (Marmelab, BSWEN) correctly identify real SDD risks:

1. **Over-specification overhead:** Writing exhaustive specs upfront can add overhead if the team lacks discipline about what level of detail is sufficient.
2. **Spec drift:** Once a spec and code diverge, maintaining both becomes costly. Without active governance, SDD can revert to the same fragmentation Agile solved.
3. **Team entry burden:** SDD requires analysts, developers, and testers to collaborate on specs. Teams built for command-and-control (analyst writes spec, developer ignores it) struggle with this.
4. **Not universally applicable:** For small, well-understood tasks or exploratory prototypes, SDD's overhead outweighs its benefits.

The Thoughtworks position (and most sources) is pragmatic: **SDD is not Waterfall 2.0, but it is also not suitable for every task.** Use it for complex features, multi-agent work, and architecture-sensitive changes. Skip it for quick fixes and solo exploration.

---

## SDD vs. Domain-Driven Design (DDD)

While not in the title, understanding SDD's relationship to DDD clarifies the full picture.

**DDD** sits at the most upstream end of development: defining "what to build" and how to model business concepts. DDD produces a shared ubiquitous language and domain models.

**SDD** sits in the design-and-implementation phase: taking the domain model and translating it into executable specifications.

**TDD** sits in the implementation phase: verifying that individual functions and classes work correctly.

### Integration Pattern: DDD + SDD + TDD

For large systems, the strongest pattern integrates all three:

1. **DDD:** Identify bounded contexts, aggregate boundaries, and the ubiquitous language (EventStorming, domain models).
2. **SDD:** For each bounded context, write specs that define the contracts, behaviors, and technical design (this is where the domain model comes alive in spec form).
3. **TDD:** Within each context's implementation, use Red/Green/Refactor to verify correctness of business logic and infrastructure.

This layering — DDD for "what," SDD for "how it works," TDD for "is it correct" — is emerging as the gold standard for complex enterprise systems (arXiv:2602.00180, aduce.jp).

---

## Practical Integration Guidance

### When to Use SDD

From Thoughtworks, Planu, and practitioner consensus:

- **Start a new feature** — even if it seems small. A 30-minute spec-and-task breakdown prevents 2 hours of rework.
- **Work with AI coding agents** — always. Agents need the persistent context that a spec provides.
- **Multi-agent or multi-developer work** — specs are the shared contract. Without them, each agent or developer makes contradictory decisions.
- **Existing codebases with established patterns** — specs guide the agent to respect architectural conventions.
- **Complex or architecture-sensitive changes** — specs let you validate the design before implementation.
- **Async or distributed work** — specs enable agents to work independently without constant human guidance.

### When to Skip SDD

From the same sources:

- **Small, well-understood tasks** — "add a validation field to this form" doesn't need a 2-hour spec interview. Vibe coding is faster.
- **Prototyping and exploration** — when the goal is to learn what you want to build, iterate directly. Specs become obsolete too quickly.
- **Solo work on a codebase you know deeply** — you carry the context in your head. A spec adds friction without value.
- **Quick fixes** — a bug fix in a single file doesn't benefit from spec overhead.

### When to Combine TDD + BDD + SDD

From practitioner consensus (Planu, sdd.sh, BSWEN):

**The strongest development workflow combines all three, layered:**

1. **BDD for collaboration:** Write Given/When/Then acceptance criteria collaboratively with product, design, and QA.
2. **SDD for structure:** Expand those criteria into a full spec with design, architecture, and task decomposition.
3. **TDD for verification:** Within each task, use Red/Green/Refactor to confirm the generated code meets spec intent.

This layering ensures:
- **Alignment:** Specs capture business intent and team agreement before code.
- **Correctness:** TDD validates that what was generated actually works.
- **Coherence:** When requirements change, you update the spec, regenerate, and TDD confirms the new code is correct.

---

## The SDD/TDD Paradigm Shift in AI-Assisted Development

From the BSWEN and Planu sources, a significant insight emerges:

**SDD may finally solve TDD's original barrier: friction.**

TDD has been advocated for 25+ years but adoption remains low. The reason: writing tests in test framework syntax is friction. Many developers understand TDD's benefits but don't practice it because the cost of test syntax is high.

SDD, by letting AI generate tests from natural language specs, **removes that friction.** You describe behavior in natural language; the AI generates Given/When/Then specs, then generates test code. The philosophy of "specify before implementing" is identical to TDD — you've just eliminated the syntax tax.

In this view, **AI-assisted SDD is TDD with the friction removed**, and the combination represents the future of disciplined AI-assisted development.

---

## Sources

- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Spec-Driven Development 2026: AI or Waterfall? — Alex Cloudstar](https://www.alexcloudstar.com/blog/spec-driven-development-2026)
- [Spec-Driven Development: The Waterfall Strikes Back — Marmelab](https://marmelab.com/blog/2025/11/12/spec-driven-development-waterfall-strikes-back.html)
- [TDD vs BDD vs SDD - TestAutomationTools.dev](https://testautomationtools.dev/tdd-vs-bdd-vs-sdd/)
- [SDD vs TDD: Why Spec Driven Development Changes the Game for AI-Assisted Coding — Planu](https://planu.dev/en/blog/sdd-vs-tdd)
- [Spec-Driven Development (2026 Guide): Build Production AI Code — Product Builder](https://www.productbuilder.net/learn/spec-driven-development)
- [How Does Specs-Driven Development Compare to Test-Driven Development? — BSWEN](https://docs.bswen.com/blog/2026-03-24-sdd-vs-tdd-comparison/)
- [TDD vs SDD vs DDD: Comparing Three Development Methodologies — aduce.jp](https://aduce.jp/en/lab/tdd-sdd-ddd-differences)
- [SDD is BDD/TDD for the AI Era — A Guide for Software Crafters — DEV Community](https://dev.to/planu/sdd-is-bddtdd-for-the-ai-era-a-guide-for-software-crafters-592p)
- [Spec-Driven Development: Is SDD Just BDD With an AI Agent? — Andrii Cheparskyi (Medium)](https://medium.com/%40cheparsky/ai-in-testing-10-spec-driven-development-bdds-second-chance-or-just-more-docs-151e30ecc97e)
- [What Is Spec-Driven Development? — sdd.sh](https://sdd.sh/2026/03/what-is-spec-driven-development/)
- [SDD vs. TDD: An Agentic AI Perspective — sequenzia/agent-tools](https://github.com/sequenzia/agent-tools)

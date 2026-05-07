# S2 — Specification Design

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Specification design is the discipline of deciding what a spec should contain, how it should be structured, and where the boundaries of a good spec lie. In the SDD workflow a spec is not a descriptive document written after decisions are made — it is the artifact from which AI agents derive all implementation decisions. The quality of the spec directly determines the quality of the output: agents that receive precise, well-structured specs produce aligned, reviewable code; agents given vague or structurally ambiguous specs produce guesswork dressed as implementation.

This section (S2) covers the full span of specification design concerns:

| Sub-topic | Focus |
|-----------|-------|
| S2.1 — Spec Structure & Content | What elements must a spec contain to be complete |
| S2.1.1 — Tacit Knowledge Capture | Business rules that live implicitly in people's heads |
| S2.1.2 — Over-Specification Risk | When spec precision becomes a maintenance burden |
| S2.2 — Quality Characteristics | Domain language, Given/When/Then, conciseness, determinism |
| S2.2.1 — Acceptance Criteria Subjectivity | Why "done" remains ambiguous even with criteria written |
| S2.2.2 — Verbosity vs. Precision Tension | The cost of making specs complete enough to guide agents |
| S2.3 — Functional vs. Technical Separation | Keeping business intent separate from implementation decisions |
| S2.3.1 — Spec Format Selection | Choosing the right format for each layer |

---

## The Spec as Behavioral Contract

The foundational shift in SDD is treating the spec as a behavioral contract rather than a project artifact. A behavioral contract answers three questions with precision:

1. What does the system do (and in what order)?
2. What does it accept as valid input?
3. What does it do when something goes wrong?

This framing comes from multiple converging practices: formal software verification (preconditions, postconditions, invariants), BDD (Given/When/Then scenarios), and AI-native development workflows (GitHub Spec Kit, Amazon Kiro, Tessl). All three communities have independently converged on the same requirement: specs must be unambiguous enough that the reader — human or AI — cannot silently fill gaps with assumptions.

The AI-specific dimension raises the stakes. When a human developer encounters ambiguity in a spec they ask a clarifying question. When an AI agent encounters ambiguity, it makes an assumption. Every assumption is a latent defect. The goal of specification design is to answer questions before they are asked.

---

## The Three-Layer Structure

Industry practice as of 2025–2026 has converged on a three-layer structure for AI-assisted development specs. This is the structure used by GitHub Spec Kit, Amazon Kiro, and the three-file pattern in this codebase:

### Layer 1 — Requirements (what)
User stories, acceptance criteria, domain vocabulary, validation rules stated as business constraints, out-of-scope statements, and failure scenarios from the user's perspective. The product owner must be able to read this layer and confirm it represents what they asked for — without needing to understand any implementation detail.

### Layer 2 — Technical Design (how)
Architecture decisions, interface signatures, data models, technology choices, non-functional constraints, and rationale for key decisions. A senior developer must be able to read this layer and implement without returning to the requirements file. See S2.3 for the full treatment of how to maintain this separation.

### Layer 3 — Implementation Tasks (in what order)
Ordered, checkboxed steps that trace to specific design decisions. Each task must be atomic enough for an agent to implement and validate in isolation. The task list is the handoff document — it is not a summary of the spec, it is an execution plan derived from it.

The three-layer structure is documented in detail for this codebase in `Docs/specs/venues/` (the reference implementation) and governed by `.claude/rules/workflow.md`.

---

## What Makes a Spec Ready for AI Execution

The Thoughtworks analysis of SDD (Dec 2025) identifies four quality properties that distinguish an AI-ready spec from a human-oriented one:

**1. Ubiquitous language.** Specs must use the project's domain vocabulary consistently. Domain terms from the requirements layer must map directly to code-level names in the design layer. An agent briefed in domain terms produces output that aligns with the domain model; an agent briefed in mixed or technical jargon invents its own naming conventions. Ubiquitous language is a prerequisite for both layers — it must be established before either is written.

**2. Given/When/Then structure for scenarios.** Acceptance criteria and edge cases written in Given/When/Then format are both human-readable and directly consumable by agents as test generation inputs. This is the spec-by-example principle from BDD applied to AI prompting. It has the secondary benefit of reducing token consumption: structured scenarios compress intent more efficiently than prose paragraphs.

**3. Completeness on the critical path, conciseness everywhere else.** A spec that enumerates every possible case becomes unmaintainable. A spec that omits edge cases forces agents to invent behavior for them. The balance: cover every branch that has a different expected outcome; do not enumerate exhaustive input combinations that differ only in scale. If a branch changes the return value, error message, or system state, it needs a criterion. If it does not, it probably does not need its own entry.

**4. Clarity and determinism.** Vague language produces non-deterministic agent behavior. Terms like "fast," "robust," "user-friendly," and "handles gracefully" are not requirements — they are wishes. Every quality attribute must be expressed as an observable, testable outcome. "The list shall load without visible delay on a standard device" is still weak; "the list shall render within 300ms of navigation on a mid-range Android device" is testable. Determinism reduces hallucinations and makes agent output reviewable against a known standard.

---

## The Tension Between Completeness and Maintainability

Every team writing specs for AI agents encounters the same structural tension: the more complete the spec, the more reliably the agent executes it — but the more expensive it becomes to keep the spec accurate as the system evolves.

Thoughtworks (Dec 2025) notes that "experienced programmers may find that over-formalized specs can cause unnecessary trouble, and slow down change and feedback cycles — just as we encountered in the early days of heavyweight process." The warning is not to abandon precision, but to apply it selectively. Precision should be concentrated where ambiguity would cause agents to make structural decisions — architecture layer assignment, naming, error handling contracts — and relaxed where the agent's default behavior is acceptable.

This tension is explored in depth in two subtopics:
- **S2.1.2 — Over-Specification Risk**: when specs become pseudo-code and constrain the implementation to a single path
- **S2.2.2 — Verbosity vs. Precision Tension**: the cost of maintaining a spec that is complete enough to reliably guide agents across multiple sessions

---

## Tacit Knowledge: The Hardest Spec Problem

The arXiv "Code Digital Twin" paper (2503.07967, 2025) identifies tacit knowledge — architectural rationales, design trade-offs, historical incidents, and the "why not" behind past decisions — as the primary gap between what a spec says and what an experienced developer would produce. AI agents cannot reconstruct tacit knowledge from code or from shallow documentation. When it is missing from the spec, agents fill the gap with plausible defaults, which are often wrong in the specific context of the project.

In AI-native development, tacit knowledge surfaces as the distinction between a spec that produces functionally correct code and a spec that produces code that feels native to the codebase. Functional correctness is achievable with explicit acceptance criteria; architectural nativeness requires externalizing the decisions that are typically carried only in developers' heads.

The practical response in this codebase: key decisions and rejected alternatives are recorded explicitly in `design.md`; cross-cutting patterns that apply to all features are codified in rules files (`.claude/library/`); the constitutional layer (CLAUDE.md) captures the highest-level invariants. Together these layers externalize the tacit knowledge that would otherwise be silently assumed.

Tacit knowledge capture is explored further in **S2.1.1**.

---

## The Living Spec Principle

GitHub Spec Kit describes specs as "living, executable artifacts that evolve with the project. Specs become the shared source of truth. When something doesn't make sense, you go back to the spec; when a project grows complex, you refine it; when tasks feel too large, you break them down."

This is a departure from the traditional view of a spec as a document written once and filed. In an SDD workflow, the spec is the primary artifact; the code is derived from it. When requirements change, the spec changes first — and then the agent re-derives the implementation. When implementation reveals a gap in the spec, the spec is updated to close it before any code is committed.

The living spec principle has one important constraint: the spec must be version-controlled with the same discipline applied to code. A spec that cannot be diffed, reviewed, and rolled back provides the same governance guarantees as no spec at all. Spec drift — the silent divergence between spec and code — is one of the primary failure modes of SDD at scale, addressed in S9.2.

---

## Scope of This Section

S2 is a section-level overview. The subtopics each address a specific design challenge in depth:

- **S2.1** covers the structural elements that must be present in a complete spec (inputs/outputs, preconditions, invariants, integration contracts, state machines)
- **S2.1.1** covers the problem of business rules that exist only implicitly in stakeholders' heads
- **S2.1.2** covers the risk of over-specifying to the point of constraining implementation
- **S2.2** covers the quality properties that make a spec reliably executable by an agent
- **S2.2.1** covers why acceptance criteria remain subjective and how to reduce that subjectivity
- **S2.2.2** covers the practical trade-off between spec verbosity and long-term maintainability
- **S2.3** covers the separation of functional and technical concerns into distinct artifacts (covered in depth — do not duplicate here)
- **S2.3.1** covers how to choose the right format for each spec layer

---

## Sources

- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Spec-driven development — Technology Radar, Thoughtworks (Nov 2025)](https://thoughtworks.com/en-gb/radar/techniques/spec-driven-development)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler / Birgitta Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog (Sep 2025)](https://resources.github.com/increasing-collaborative-development-with-ai/)
- [How to Write AI-Ready Specs That Produce High-Quality Output — Zencoder / Zenflow (Jan 2026)](https://zencoder.ai/blog/how-to-write-ai-ready-specs-that-produce-high-quality-output)
- [How to write a good spec for AI agents — Addy Osmani](https://addyosmani.com/blog/good-spec)
- [Phase 2: Writing Effective Specifications — AI Agent Factory / Panaversity (Feb 2026)](https://agentfactory.panaversity.org/docs/General-Agents-Research/spec-driven-development/writing-effective-specs)
- [The Anatomy of a Good Spec in the Age of AI — Kinde](https://kinde.com/learn/ai-for-software-engineering/best-practice/the-anatomy-of-a-good-spec-in-the-age-of-ai/)
- [How to Write a Software Spec: A Practical Guide for Builders — MindStudio (Apr 2026)](https://www.mindstudio.ai/blog/how-to-write-a-software-spec)
- [Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents — arXiv:2603.17150](https://arxiv.org/html/2603.17150v1)
- [Code Digital Twin: Empowering LLMs with Tacit Knowledge for Complex Software Development — arXiv:2503.07967](https://arxiv.org/html/2503.07967v3)
- [Why AI Needs Humans in Requirements Engineering — V2 Solutions (Nov 2025)](https://www.v2solutions.com/whitepapers/ai-requirements-engineering-human-oversight/)

# S1 — Core Concepts

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

This section introduces the foundational concepts that define Spec-Driven Development (SDD) as a discipline. It covers what SDD is at its core, how it manifests at different levels of implementation rigor, the philosophical divide between the Spec-Anchored and Spec-as-Source levels, and how SDD relates to prior methodologies such as TDD, BDD, and waterfall.

For the precise academic and industry definition of SDD — including its emergence from "vibe coding" failures and its treatment of the spec as the primary artifact — see **S1.1 — Definition**. This section provides the connecting overview that orients the subtopics relative to one another.

---

## What SDD Is

Spec-Driven Development is a software development paradigm that inverts the traditional relationship between specifications and code. In conventional development, code is the ground truth and documentation follows (or drifts). In SDD, the specification is the authoritative artifact from which code is generated, validated, and corrected.

The Thoughtworks Technology Radar — which named SDD one of 2025's key new engineering practices — defines it as "a development paradigm that uses well-crafted software requirement specifications as prompts, aided by AI coding agents, to generate executable code." The arXiv paper that formally characterizes the paradigm (arXiv:2602.00180, February 2026) adds the structural corollary: when spec and code disagree, the spec wins.

Four characteristics distinguish SDD from looser "spec-first" aspirations:

1. **The spec is the input to implementation, not a parallel artifact.** It is not documentation that sits beside the code — it is the source from which code is derived.
2. **The spec is authoritative.** Code is corrected to match the spec, not the reverse.
3. **There is a structured workflow with review gates.** Implementation proceeds through phases (Specify → Plan → Tasks → Implement), each producing artifacts that constrain the next, with human review at transitions.
4. **Specs are maintained as the system evolves.** Changes go through the spec first; the implementation follows.

---

## The Three Implementation Levels

Not all SDD practice is equal in rigor. Three levels have emerged in literature and tooling — identified in arXiv:2602.00180 and independently in Martin Fowler's analysis of Kiro, spec-kit, and Tessl (martinfowler.com, October 2025):

| Level | Name | What it means |
|-------|------|---------------|
| **Level 1** | Spec-First | A well-crafted spec is written before coding begins. Code is maintained manually after generation. Specs may drift over time. |
| **Level 2** | Spec-Anchored | The spec is kept and maintained alongside the code throughout the feature's lifecycle. Both artifacts coexist and are kept in sync. |
| **Level 3** | Spec-as-Source | The spec is the only artifact humans edit. Code is entirely generated from the spec and should never be manually modified. |

Most teams in 2025–2026 operate at Level 1 (Spec-First). Level 2 (Spec-Anchored) is the practical sweet spot for production systems: it provides the traceability and drift-detection benefits without the regeneration constraints of Level 3. Level 3 (Spec-as-Source) is the theoretical destination that tools like Amazon Kiro and Tessl are working toward.

The arXiv paper's guidance: "use the minimum level of specification rigor that serves your needs." Level 1 is the correct entry point for most teams and the right starting point for feature additions in existing codebases.

For the philosophical divide between Levels 2 and 3, see **S1.2.1 — Level Gap: Anchored → Source**.

---

## The Foundational Shift: Spec as Primary Artifact

The animating claim of SDD is that the specification is more valuable than the code it generates, and the entire discipline is the set of practices that make that claim operationally true rather than merely aspirational.

This claim rests on a changed economics argument. The historical reason code became the ground truth — rather than the specification — was that regenerating code from specs was too slow and expensive. When AI generation takes seconds and produces functionally correct output from a well-formed spec, that reason disappears. The structural consequence: it becomes rational to review spec changes (not code changes) as the primary gate; to onboard new team members at the spec level; and to debug by comparing code behavior to spec intent.

The GEICO Tech Blog (2026) states the inversion cleanly: "The primary value of senior engineering shifts from code production to defining correctness." Specs become machine-readable contracts; code becomes a downstream projection of intent, derived on demand.

---

## The Four-Phase Workflow

Regardless of implementation level, SDD practice follows a consistent four-phase workflow — codified in arXiv:2602.00180, implemented by GitHub Spec Kit's slash commands, and reflected in Amazon Kiro's planning stages:

| Phase | Produces | Human gate |
|-------|----------|------------|
| **Specify** | Requirements document — the "what" and "why" | Review and approval before planning begins |
| **Plan** | Technical design — architecture, stack, constraints | Review before task decomposition |
| **Tasks** | Ordered, atomic implementation checklist | Review before implementation begins |
| **Implement** | Working code, task-by-task | Review after each task or batch |

Each phase constrains the next. The key discipline is that no phase is skipped: moving directly from Specify to Implement is "prompt and pray with extra steps" (Java Code Geeks, 2026). The review gate at each phase transition is what makes the workflow SDD rather than elaborate vibe coding.

This four-phase structure is the workflow adopted by MyVocaList: see `Docs/specs/venues/` for the reference implementation, and `.claude/rules/workflow.md` for the project's enforcement rules.

---

## SDD and Prior Methodologies

SDD is not a replacement for BDD, TDD, or design-first API development. It is an integration of those disciplines, adapted for an environment where the implementer is an AI agent. See **S1.3 — SDD vs TDD/BDD/Waterfall** for the full comparison.

The GEICO Tech Blog summary (2026) is precise: "Domain-Driven Design argued that a shared ubiquitous language between business and engineering is the foundation of good systems. Behavior-Driven Development formalized that language into executable specifications. Test-Driven Development established that writing the contract before the implementation produces better results. SDD extends these disciplines into an environment where the implementer is an AI agent rather than a human."

What SDD adds to these predecessors is the recognition that with AI agents, the specification itself — not the test suite, not the code — is the primary artifact that defines system correctness.

Key distinctions:
- **BDD** contributes the Given/When/Then acceptance criteria format, which is a natural spec format for SDD.
- **TDD** operates within the SDD implementation phase — the Red/Green/Refactor cycle verifies that generated code meets the spec.
- **Waterfall** shares the "plan before code" intuition but requires exhaustive upfront planning and discourages change. SDD specifications are living documents: version-controlled, iteratively refined, and updated before code changes, not after.

---

## Subtopic Map

| ID | File | What it covers |
|----|------|----------------|
| S1.1 | S1_1_Definition.md | Precise definition, emergence from vibe coding, the spec-as-primary-artifact principle, current state of practice |
| S1.2 | S1_2_Implementation_Levels.md | The three levels in depth — when each applies, tooling alignment, decision criteria |
| S1.2.1 | S1_2_1_Level_Gap_Anchored_to_Source.md | The philosophical and practical divide between Spec-Anchored and Spec-as-Source |
| S1.3 | S1_3_SDD_vs_TDD_BDD_Waterfall.md | How SDD relates to and extends TDD, BDD, DDD, and waterfall |

---

## Sources

- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [An Introduction to Spec-Driven Development — GEICO Tech Blog](https://www.geico.com/techblog/an-introduction-to-spec-driven-development/)
- [Spec-Driven Development with AI Coding Agents: The Workflow Replacing "Prompt and Pray" — Java Code Geeks](https://www.javacodegeeks.com/2026/03/spec-driven-developmentwith-ai-coding-agents-the-workflow-replacingprompt-and-pray.html)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft Developer Blog](https://developer.microsoft.com/blog/spec-driven-development-spec-kit)
- [Spec-Driven Development: Everything You Need to Know [2026] — Zencoder](https://zencoder.ai/blog/spec-driven-development)
- [Spec-Driven Development: Building Production-Ready Software with AI — orchestrator.dev](https://orchestrator.dev/blog/2025-12-16-spec_driven_dev_article/)
- [Spec-driven development — Thoughtworks Technology Radar](https://www.thoughtworks.com/radar/techniques/spec-driven-development)

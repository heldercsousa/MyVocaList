# S1.1 — Definition

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-28 | Researched | Content written by research agent |

---

## Overview

Spec-Driven Development (SDD) is a software development paradigm that places well-crafted specifications at the center of the development process, treating them as the authoritative source of truth from which code is generated, verified, or derived. Rather than writing code first and producing documentation afterward (or never), practitioners write precise, machine-consumable specifications first — and then let AI coding agents translate those specifications into implementation.

The approach inverts the traditional workflow in a meaningful way: in conventional development, code gradually becomes the de facto truth while the original requirements documents become historical artifacts that nobody trusts or maintains. SDD reverses this polarity. The specification is the living artifact; the code is a generated output. When the two conflict, the specification wins — and the code is regenerated or corrected.

SDD emerged as a disciplined response to a problem that became acute in 2025: the rise of "vibe coding," a term coined by Andrej Karpathy (co-founder of OpenAI) in February 2025 for the practice of prompting AI tools with loose, conversational instructions and accepting whatever code they produce. Vibe coding accelerates early prototyping but produces verbose, architecturally inconsistent code that is difficult to maintain. By September 2025, Fast Company was reporting a "vibe coding hangover" among senior engineers. SDD is the professional discipline that corrects this: it applies rigor to the human side of AI-assisted development, ensuring that AI generation is anchored to explicit, validated intent.

## Core Definition

Spec-Driven Development is formally defined in the February 2026 academic paper *"Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants"* (arXiv:2602.00180) as a paradigm in which software requirement specifications serve as prompts for AI coding agents that generate executable code. Thoughtworks, which named SDD one of the key new engineering practices of 2025, offers a complementary formulation: SDD is a development paradigm that uses well-crafted software requirement specifications as prompts, aided by AI coding agents, to generate executable code.

Key vocabulary:

- **Specification (spec):** A structured artifact — typically markdown, EARS notation, or a structured document — that describes what a feature must do, how it must behave, what its constraints are, and (optionally) how it should be architected. The spec is authored by a human and reviewed before any code is written.
- **Spec-as-primary-artifact:** The principle that the spec, not the code, is what teams read, review, version-control, and maintain. Code is a downstream product.
- **Generation:** The act of an AI coding agent (Claude Code, Kiro, GitHub Copilot Workspace, etc.) producing implementation from a spec. Generation is deterministic in intent even if nondeterministic in output — running generation twice against the same spec should produce functionally equivalent results.
- **Validation:** The phase that closes the loop — verifying that what was generated actually satisfies the spec. Combines automated tests with human review.

What makes SDD distinct from prior spec-first ideas is the maturity of the generation layer. Formal methods, BDD, and design-first API development all promoted specification before coding, but required developers to manually implement against those specs. In 2025–2026, AI coding agents are capable enough to perform that translation reliably, making the spec-first stance economically viable at production velocity.

## The Spec-as-Primary-Artifact Principle

In traditional development, the code base is the ground truth. Requirements documents, architecture diagrams, and design notes drift out of sync with reality over time and are eventually discarded or ignored. The developers who understand the system are those who have read the code — not the documentation.

SDD challenges this structural pattern at its root. When specs are machine-consumable and code is regenerable, there is no longer a technical reason for code to be the primary artifact. The reasons to maintain that convention (it was too slow and expensive to regenerate from specs) disappear when generation takes seconds.

The practical consequences of this shift are significant:

1. **Specifications are reviewed, not code.** Pull requests in mature SDD teams review spec changes. Code review becomes a secondary validation layer, not the primary gate.
2. **Specs are versioned alongside or instead of code.** When a feature changes, the spec changes first. The code update is derived from the spec update.
3. **Context for AI agents lives in the spec.** The spec provides the AI agent with the intent, constraints, and architectural decisions it needs to generate correctly. Without a spec, an agent must infer intent from context — a source of hallucination and drift.
4. **Onboarding and maintenance happen at the spec level.** New team members read specs to understand the system. Debugging starts with comparing code behavior to spec intent.

The three maturity levels that have emerged in practice (identified by Thoughtworks and others) reflect how deeply a team has committed to this principle:

| Level | Description |
|-------|-------------|
| **Spec-First** | Specs are written before coding begins, but code is manually maintained afterward. Specs may drift over time. |
| **Spec-Anchored** | Specs drive initial generation and are kept in sync with the code as the system evolves. Both artifacts coexist and are maintained. |
| **Spec-as-Source** | Specs are the sole source of truth. Code is always generated from specs; manual edits to generated code are prohibited or tracked as overrides. |

Most teams in 2025–2026 operate at the Spec-First or Spec-Anchored level. Spec-as-Source is the theoretical extreme that specialized tooling like Amazon Kiro and Tessl work toward.

## Current State of Practice (2025–2026)

SDD went from a theoretical concept to a recognizable industry practice within roughly twelve months, catalyzed by the simultaneous emergence of capable AI coding agents and the failure modes of vibe coding.

**Amazon Kiro** (launched July 2025) is the most visible dedicated SDD tool. It is a VS Code fork — an agentic IDE — built specifically around the spec-driven workflow. Kiro takes a natural language description and produces: user stories with acceptance criteria in EARS notation, a technical design document, and a prioritized list of implementation tasks. Those outputs drive agentic code generation. Kiro was featured at AWS re:Invent 2025 (sessions DEV314 and DVT209) and has been used in production drug discovery workflows, compressing timelines from months to weeks.

**GitHub Spec Kit** (open source, 2025) provides a lightweight toolkit for teams who want SDD without a proprietary IDE. It maps spec documents to GitHub issues, pull requests, and Copilot Workspace generation passes.

**cc-sdd** (open source) is a minimal, cross-agent harness providing a 17-skill SDLC workflow — discovery, requirements, design, tasks, and autonomous implementation — that works across eight AI coding agents including Claude Code, Codex, Cursor, Copilot, and Windsurf.

**Claude Code + custom workflows** are widely used for SDD, with a significant community of practitioners publishing their approaches. The canonical workflow involves a structured set of markdown files (requirements, design, tasks) checked into the repository, with Claude Code reading those specs and executing implementation task-by-task. O'Reilly ran a dedicated live event on this pattern in 2025. This is the model MyVocaList uses (see `Docs/specs/venues/` as reference implementation).

**Thoughtworks Technology Radar** named SDD a key technique to adopt in 2025, citing the combination of "immutable constitution" rules files, incremental validated delivery, and explicit separation of planning from execution as the hallmarks of the mature practice.

## Relationship to the Broader SDD Landscape

This definition section (S1.1) is the foundation for the entire 10-section SDD topic map. All other sections build from or apply this definition:

- **S1.2 — Why SDD Now** explains the conditions (AI capability, vibe coding failures) that made this moment the tipping point for SDD adoption.
- **S2.x — Spec Structure** covers how to write specifications that are precise enough to function as AI prompts — the practical skill this definition demands.
- **S3.x — AI Agent Integration** covers how agents like Claude Code consume specs and what determines generation quality.
- **S4.x — Validation** covers the closing loop — how to verify that generated code actually satisfies the spec, which is what makes the spec-as-primary-artifact claim meaningful rather than aspirational.
- **S5.x — Team Workflow** covers how teams organize around specs rather than code, including review, handoffs, and the Spec-Anchored maintenance discipline.
- **S6.x — Tooling** covers Kiro, cc-sdd, GitHub Spec Kit, and the emerging IDE-level support for SDD workflows.

Understanding SDD as a definition means understanding a single claim with large consequences: **the spec is more valuable than the code it generates**, and the entire discipline of SDD is the set of practices that make that claim operationally true rather than merely aspirational.

## Sources

- [What Is Spec-Driven Development? A Complete Guide — Augment Code](https://www.augmentcode.com/guides/what-is-spec-driven-development)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Spec-driven development — Thoughtworks Technology Radar](https://www.thoughtworks.com/radar/techniques/spec-driven-development)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv](https://arxiv.org/html/2602.00180v1)
- [Beyond Vibe Coding: Amazon Introduces Kiro, the Spec-Driven Agentic AI IDE — InfoQ](https://www.infoq.com/news/2025/08/aws-kiro-spec-driven-agent/)
- [Kiro: Agentic AI development from prototype to production](https://kiro.dev/)
- [Kiro and the future of AI spec-driven software development](https://kiro.dev/blog/kiro-and-the-future-of-software-development/)
- [AWS re:Invent 2025 — Spec-driven development with Kiro (DEV314)](https://www.youtube.com/watch?v=4qcWgPb-8Fk)
- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog](https://github.blog/ai-and-ml/generative-ai/spec-driven-development-with-ai-get-started-with-a-new-open-source-toolkit/)
- [Spec-Driven Development with Claude Code — O'Reilly Live Event](https://www.oreilly.com/live-events/spec-driven-development-with-claude-code/0642572319915/)
- [cc-sdd — GitHub](https://github.com/gotalab/cc-sdd)
- [Spec-Driven Development: The Waterfall Strikes Back — Marmelab](https://marmelab.com/blog/2025/11/12/spec-driven-development-waterfall-strikes-back.html)
- [AI SDD in 2026 — Medium](https://medium.com/@ioneswalter/ai-sdd-in-2026-bdbe69f2eb04)

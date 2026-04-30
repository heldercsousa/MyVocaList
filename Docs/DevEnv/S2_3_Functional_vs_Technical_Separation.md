# S2.3 — Functional vs Technical Separation

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-28 | Researched | Content written by research agent |

---

## Overview

Every software feature has two distinct faces: **what it must do** (business intent) and **how it will be done** (implementation detail). Spec-Driven Development (SDD) treats these as separate artifacts — not because it is bureaucratically tidy, but because confusing them produces lower-quality AI-generated code, harder human reviews, and specs that become stale the moment the implementation shifts.

The principle is sometimes called the separation of "what" from "how". In practice it maps directly to the two-file pattern used in this codebase: `requirements.md` owns the "what", `design.md` owns the "how".

---

## The Separation Principle

When a specification mixes business rules with implementation decisions, both audiences — human reviewers and AI agents — must mentally untangle them before they can act. The functional layer describes observable behavior: inputs, outputs, preconditions, postconditions, invariants, and acceptance criteria. The technical layer describes constraints on implementation: architecture choices, data models, API contracts, technology selections, and performance envelopes.

The boundary is intentional and enforced:

| Question | Belongs in | Example |
|---|---|---|
| What does the user accomplish? | `requirements.md` | "The admin shall be able to register a singer with a unique display name." |
| What constitutes a valid name? | `requirements.md` | "A name shall be 1–50 characters, non-empty after trim." |
| Which table stores the singer? | `design.md` | `Persons` entity with `FullNameNormalized` for case-insensitive search |
| How is uniqueness enforced at the DB layer? | `design.md` | Unique index on `FullNameNormalized` |
| Which service method creates a singer? | `design.md` | `IPersonService.CreatePersonAsync(...)` returning `(bool, string, Person?)` |

The rule of thumb: a product owner (non-technical) should be able to read `requirements.md` and confirm it matches what they asked for. A senior developer (technical) should be able to read `design.md` and implement it without revisiting the requirements file.

---

## Functional Specifications

The functional specification (`requirements.md`) answers: *what must the system do, and under what conditions?*

### What belongs here

- **User stories** — actor, goal, rationale ("As an admin, I want to…")
- **Acceptance criteria** — written as `shall` statements with a single observable behavior per line
- **Domain vocabulary** — terms used by stakeholders, not developers ("queue round", not "List<QueueEntryEntity>")
- **Validation rules** — expressed as business constraints, not code ("name must not exceed 30 characters")
- **Out-of-scope statements** — explicitly what will NOT be built in this iteration
- **Edge cases and failure scenarios** — from the user's perspective ("if the name already exists, the user shall see an error message")

### What does NOT belong here

- Database schema decisions
- Method signatures or class names
- Framework choices
- Performance targets stated in technical units (ms, MB) — use user-observable terms instead ("the list shall load without visible delay on a standard device")

### Language discipline

Functional specs must use the project's **ubiquitous language** — terms that both domain experts and developers recognize without translation. This is a DDD principle that pays dividends in AI-assisted workflows: an AI agent briefed entirely in domain terms produces output that maps cleanly to the domain model. Introducing technical jargon at the requirements layer pollutes this.

The ubiquitous language acts as a guardrail: any AI agent working on this project should not introduce new terms or concepts not already established in the domain vocabulary. If a new concept is genuinely needed, it must be proposed explicitly — not quietly invented in generated code.

---

## Technical Specifications

The technical specification (`design.md`) answers: *how will the system satisfy the requirements, within the constraints of this architecture?*

### What belongs here

- **Architecture layer assignments** — which project (Domain, Services, Infra, MAUI) owns what
- **Interfaces** — method signatures, parameter types, return types, error contracts
- **Data models** — entity definitions, EF Core configuration, indexes, nullable rules
- **API contracts** — DTOs, mapping rules, query parameters
- **Navigation and routing** — Shell routes, query parameters passed between pages
- **Technology choices** — when a choice is made (e.g., "use DXCollectionView with multiple selection always on")
- **Non-functional constraints** — page size (referencing `AppPagination.DefaultPageSize`), search collation rules, thread safety considerations
- **Key decisions and rejected alternatives** — the "why not" is as valuable as the "why"

### Language discipline

Technical specs use implementation terms precisely. A ViewModel property name in `design.md` must match what will appear in code. An interface method documented in `design.md` must match what `IPersonService` declares. This alignment is what makes AI-generated code from a `design.md` brief trustworthy — the agent is not inferring names, it is copying them.

---

## How AI Agents Consume Each Type

AI coding agents have different failure modes when given each type of specification.

**When given only functional specs**, agents tend to invent architectural decisions: they choose their own class names, layer assignments, and data models. The result may be functionally correct but structurally misaligned with the codebase. Multiple agents briefed with only functional specs will produce incompatible implementations.

**When given only technical specs**, agents lose track of the business goal. They satisfy the interface contract but may omit edge cases, return wrong error messages, or skip validation that was implied by domain knowledge. Testing against the spec becomes harder because acceptance criteria were never stated.

**When given both, in the right order**, the agent operates in a constrained search space. The `requirements.md` tells it what success looks like; the `design.md` tells it exactly how to achieve it within the project's established patterns. The agent's role is narrowed from "invent a solution" to "implement a specified solution" — a much higher-reliability task.

In this codebase, the recommended briefing sequence for a subagent is:

1. Read the relevant `requirements.md` first (understand the goal)
2. Read the relevant `design.md` (understand the architecture)
3. Read the relevant rules files (understand the cross-cutting patterns)
4. Implement — making zero architectural decisions that are not already stated

This sequence is enforced by the workflow rules in `.claude/rules/workflow.md`.

---

## Failure Modes When the Separation Breaks Down

### 1. Technical decisions buried in requirements

When a requirements file says "call `PersonService.CreatePersonAsync` when the form is submitted", the product owner can no longer validate the requirement without reading code. The requirement is now untestable at the business level. If the method name changes, the requirement is stale.

### 2. Business rules buried in design

When a design file says "validate that the name is not empty", it conflates the rule (a business constraint) with its implementation location (the service layer). The rule will be duplicated or missed. Downstream tests won't find it in the right place.

### 3. Vague language that straddles both

Requirements written as "the system should handle duplicates gracefully" give AI agents too much latitude — they will invent both the business rule (what "gracefully" means) and the implementation (how to detect the duplicate). The resulting code is untestable against any agreed criterion.

### 4. Specs written after implementation

When the implementation exists first and the spec is retrofitted, the spec describes what the code does rather than what the system should do. This breaks TDD, removes the ability to review functional correctness before implementation, and means AI agents briefed from the spec will reproduce the existing behavior — including its bugs.

### 5. Single-file "mega-specs"

A single file mixing requirements and design is readable by neither the product owner nor a focused AI agent. The agent cannot isolate acceptance criteria to test against. The product owner cannot skip the implementation details to review the business logic. Revision history becomes meaningless — a business rule change and a schema change look identical in a diff.

---

## Current Practices (2025–2026)

The industry has largely converged on a two-phase or three-phase spec structure for AI-assisted development:

**Phase 1 — Functional specification**: user stories, acceptance criteria, domain constraints. Tools such as GitHub Kiro, spec-kit, and Tessl formalize this phase explicitly, producing a `requirements.md` (Kiro) or equivalent before any planning begins.

**Phase 2 — Technical design**: architecture, data models, interfaces, technology decisions. In Kiro this is `design.md`; in spec-kit it is a planning document generated from the functional spec by an AI agent acting as a planner, not an implementer.

**Phase 3 — Implementation tasks**: ordered, checkboxed steps that trace to specific design decisions. This is the handoff document for the implementing agent — a `tasks.md`.

The three-file structure used in this codebase (`requirements.md` / `design.md` / `tasks.md` under `Docs/specs/[feature]/`) directly mirrors the industry consensus as of 2025–2026.

A key 2026 insight from practitioners: ubiquitous language — the shared domain vocabulary from DDD — must be established before either type of specification is written. Without it, functional specs use stakeholder terms and design specs use developer terms, and the two cannot be reconciled by an AI agent operating across both. A `ubiquitous-language.md` (or equivalent glossary section in `design.md`) is increasingly treated as a prerequisite, not an afterthought.

---

## Sources

- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv](https://arxiv.org/html/2602.00180v1)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Spec-Driven Development: 10 things you need to know about specs — AI Native Dev](https://ainativedev.io/news/spec-driven-development-10-things-you-need-to-know-about-specs)
- [Spec-Driven Development (SDD): A Structured Approach to AI-Assisted Software Engineering — XB Software](https://xbsoftware.com/blog/spec-driven-development-ai-assisted-software-engineering/)
- [Functional vs Technical Requirements Compared, with Examples — AltexSoft](https://www.altexsoft.com/blog/functional-vs-technical-requirements/)
- [How Creating a Ubiquitous Language Ensures AI Builds What You Actually Want — Daniel Schleicher](https://www.danielschleicher.com/software/engineering,/ai,/spec-driven/development/2026/01/04/removing-ambiguity-with-spec-driven-development.html)
- [AI Coding Assistants and the Erosion of Ubiquitous Language — DEV Community](https://dev.to/dbrown/ai-coding-assistants-and-the-erosion-of-ubiquitous-language-301a)
- [Backend Coding AI Context: DDD and Hexagonal Architecture — Bardia Khosravi / Medium](https://medium.com/@bardia.khosravi/backend-coding-rules-for-ai-coding-agents-ddd-and-hexagonal-architecture-ecafe91c753f)
- [Kiro Specs Documentation](https://kiro.dev/docs/specs/)
- [spec-kit/spec-driven.md — GitHub](https://github.com/github/spec-kit/blob/main/spec-driven.md)
- [Ubiquitous Language — Martin Fowler bliki](https://martinfowler.com/bliki/UbiquitousLanguage.html)

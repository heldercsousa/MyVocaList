# S3 — Workflow Phases

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Spec-Driven Development is not a single act of writing a document before coding; it is a structured cycle that moves through three distinct phases before any line of implementation code is written, and adds a final verification pass before the change is accepted. Understanding these phases — Planning, Implementation, and Verification — is the operational core of SDD.

The three phases are sequential by design. Each phase produces a concrete artifact that becomes the mandatory input to the next. There is no implementation without an approved plan; there is no review gate without completed implementation. The dependency chain is explicit and enforced.

| Phase | Primary artifact | Human role |
|-------|-----------------|------------|
| **S3.1 — Planning** | requirements.md, design.md, tasks.md | Author, review, approve |
| **S3.2 — Implementation** | Working code, tests | Delegate to AI agent; review focused diffs |
| **S3.3 — Verification / Review Gates** | Compliance report, green CI, human sign-off | Accept or reject; request changes |

The three phases are expanded in their own dedicated files (S3.1, S3.2, S3.3). This file provides the structural overview and the connective logic that ties them together.

---

## S3.1 — Planning Phase

### Purpose

The Planning Phase converts vague feature intent into a three-document specification set that is specific enough for an AI agent to implement without ambiguity. It is the phase in which humans do the most thinking; the Implementation Phase is intentionally designed so that humans do the least improvisation.

### Structure

Most SDD tooling and practitioner workflows converge on the same three-document output (Kiro, GitHub Spec Kit, cc-sdd, and the MyVocaList workflow all follow this structure with minor terminology variation):

| Document | Content |
|----------|---------|
| **requirements.md** | User stories, acceptance criteria, validation rules, non-goals, constraints |
| **design.md** | Architecture decisions, interface definitions, component structure, interaction flows, key trade-offs |
| **tasks.md** | Ordered, checkboxed implementation tasks derived from the design; each task is atomic, independently executable, and maps to a named deliverable |

The three documents enforce a deliberate separation of concerns. Requirements capture *what* and *why*; design captures *how*; tasks capture *in what order*. Mixing these layers into a single document is a common source of spec ambiguity that leads to agent drift during implementation.

### The planning gate

Before implementation begins, the planning artifacts go through a human review gate. This is the highest-leverage moment in the SDD cycle: changes made to a spec take minutes; the same changes made after an agent has generated 2,000 lines of code take hours or days. The review covers, in order:

1. **Feature specification review** — are user stories accurate, acceptance criteria measurable, constraints realistic?
2. **Technical decisions review** — are library choices appropriate, performance targets achievable, integration approaches feasible?
3. **Database / schema review** (if applicable) — are migrations safe, indexes sufficient, schema changes non-breaking?
4. **Scope review** — is there scope creep, are timelines realistic, are core requirements covered?

Once the human approves all three documents, implementation is authorized.

### Sub-topics

The Planning Phase carries two structural risks that are covered in dedicated files:

- **S3.1.1 — Architecture debt from early decisions:** Technology choices locked in during planning may not survive the realities of implementation. When a planning-phase decision is wrong, it is discovered late, and reversing it requires re-speccing, re-planning, and potentially re-implementing. The risk is not that planning produces bad decisions — it is that planning *locks in* decisions before the most informative evidence (working code) exists.

- **S3.1.2 — Dependency analysis incompleteness:** Hidden coupling between tasks or components surfaces only during coding, forcing re-sequencing of the task list and invalidating dependency assumptions. Dependency maps produced during planning are necessarily incomplete because full coupling is only visible in the implementation.

---

## S3.2 — Implementation Phase

### Purpose

The Implementation Phase is where AI agents execute the approved task list against the approved design. The human role shifts from author to reviewer: instead of writing code, the human delegates each task to an agent, reviews focused diffs (one task at a time), and approves or requests changes before marking the task complete.

### Key design principles

**Tasks are the unit of delegation.** A subagent is assigned exactly one task from tasks.md. It reads the relevant spec documents, implements the task, runs the build, commits its output, and signals completion. Context isolation (each subagent starts fresh) prevents accumulated errors from contaminating subsequent tasks.

**The spec is the contract.** The agent does not improvise beyond the spec. When the implementation encounters ambiguity not resolved by the spec, the correct response is to surface that ambiguity to the human (via the task log or a blocking signal) rather than to make an unilateral architectural decision.

**Build must pass before completion.** No task is marked done unless the build is green. Tests that cover the task's acceptance criteria must pass. This is enforced as a mandatory exit condition, not a best practice.

**Wave-based parallelism with a hard cap.** When tasks are independent (as identified in tasks.md with parallel markers), multiple subagents may run concurrently. The cap is four concurrent subagents. This prevents resource contention and maintains human oversight capacity. The MyVocaList workflow (`workflow.md`) enforces this cap.

### Relationship to context window management

Implementation is the phase most directly affected by AI context window limits. A large task that spans many files accumulates tool-call traces and reasoning context until the window is exhausted, at which point the agent either compacts (losing decision rationale) or halts entirely. The primary defense is task granularity: tasks must be sized so they complete within a single agent session without requiring compaction. This constraint directly influences how tasks.md is structured during planning.

### Sub-topics

- **S3.2.1 — Task granularity calibration:** Tasks that are too coarse give agents insufficient guidance and encourage improvisation. Tasks that are too fine create artificial fragmentation and waste planning overhead. The correct granularity is calibrated to: (a) fit within a single agent context window, (b) produce a reviewable diff, and (c) map to a single acceptance criterion.

- **S3.2.2 — Context window exhaustion:** Context limits are the binding constraint behind most agent failure modes during implementation. Documented cases show context limits reached within 10 minutes of active work, with compaction commands themselves failing when the context is full. The SDD response is structural: keep tasks small, keep CLAUDE.md concise, and use a hub-and-spoke context structure that indexes to spec files rather than inlining all content.

---

## S3.3 — Verification / Review Gates

### Purpose

Verification gates are the checkpoints that close the loop between the approved spec and the implemented code. They answer the question: does what was built match what was approved? Without verification gates, SDD reduces to spec-first documentation — the spec exists, but no mechanism enforces conformance.

### Gate structure

A verification gate combines automated checks and human review:

**Automated gates:**
- Build passes with zero errors
- Tests pass (unit, integration, and smoke tests as applicable)
- Spec compliance: acceptance criteria from requirements.md are traceable to passing tests
- Architecture constraint checks (layer violations, circular dependencies, naming conventions)
- Coverage thresholds (if defined in the spec)

**Human review gate:**
- Reviewer confirms the implementation matches design intent (not just technical correctness)
- Reviewer checks that the diff is bounded to the task scope — no unintended side effects
- Reviewer approves or requests changes; changes go back to the implementation phase, not the planning phase

The sequence is: automated gates pass first, then human review. Automated gates catch mechanical errors; human review catches intent drift that automated tools cannot detect.

### Verification as the proof of SDD

The insight that distinguishes mature SDD from spec-first waterfall is that verification is continuous, not terminal. Each task has its own micro-gate (build + tests + review). The final feature-level gate is a cumulative verification, not a big-bang review. This means failures are caught and corrected at task granularity, not at feature granularity.

The ctxt.dev formulation (March 2026) captures this precisely: "The primary trust artifact should not be a diff. It should be an approved specification. The primary gate should not be 'does this look right to a reviewer.' It should be 'does this pass deterministic checks against the approved intent.'"

### Sub-topics

- **S3.3.1 — Approval bottleneck:** Human review gates require synchronous human availability. When a reviewer is unavailable, the pipeline halts. This becomes a significant throughput constraint at scale, especially when tasks complete faster than a human can review them. Async approval gate patterns (time-boxed review windows, escalation policies, low-risk auto-approval lanes) can reduce the chokepoint without eliminating human oversight.

- **S3.3.2 — Authority ambiguity:** In multi-person teams, it is not always clear who is authorized to approve a given phase type. Is the planning gate approved by the product owner, the architect, or the tech lead? Is the implementation gate approved by the task author, a peer, or a senior engineer? Undefined approval authority is functionally equivalent to no approval authority: gates get bypassed informally. SDD requires that approval roles are defined per phase type and per change type (schema changes, security-sensitive changes, and UX changes may each require different approvers).

---

## How the Phases Connect

The three phases form a closed cycle, not a linear sequence. Verification outputs feed back into planning when a change is rejected. Planning failures discovered during implementation surface as blocked tasks that trigger a re-planning step. The cycle is:

```
Planning (approve) -> Implementation (execute) -> Verification (gate)
                             ^                           |
                             +-- reject / re-plan <------+
```

The key invariant: **the spec always changes before the code changes**. When implementation reveals a design flaw, the correct response is to update design.md, update the affected tasks in tasks.md, get the change reviewed, and then re-implement the affected tasks. Patching the code without updating the spec defeats the entire SDD architecture.

This invariant is what differentiates SDD from "spec as documentation": in SDD, the spec update is the gating action, not an afterthought.

---

## Sources

- [Spec-Driven Development Planning Phase Review — Valdez (2024-12-25)](https://valdezm.com/ai-engineering/spec-driven-development)
- [Spec-Driven Development: Building Production-Ready Software with AI — orchestrator.dev (2025-12-16)](https://orchestrator.dev/blog/2025-12-16-spec_driven_dev_article/)
- [The SDD Workflow — GitHub Spec-Kit (Mintlify docs)](https://www.mintlify.com/github/spec-kit/concepts/workflow)
- [GitHub Spec-Kit six-phase workflow — Zread.ai overview](https://zread.ai/github/spec-kit/3-the-spec-driven-development-workflow-from-idea-to-working-software)
- [The Four-Phase Workflow — Agent Factory / Panaversity](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/four-phase-workflow)
- [Spec-Driven Development: AI-Assisted Coding — SolGuruz (2026-03-12)](https://solguruz.com/blog/spec-driven-development-guide)
- [How Spec-Driven Development Transforms Enterprise Software Teams — Augment Code (2025-09-19)](https://www.augmentcode.com/guides/how-spec-driven-development-transforms-enterprise-software-teams)
- [Claude Code for Spec-Driven Development: Capabilities and Limits — Augment Code (2026-04-24)](https://www.augmentcode.com/guides/claude-code-spec-driven-development)
- [Spec-Driven Development — Adoption at Enterprise Scale — InfoQ (2026-02-19)](https://www.infoq.com/articles/enterprise-spec-driven-development)
- [Spec-Gated Delivery: Why PR Review Is the Wrong Trust Checkpoint for AI Code — ctxt.dev (2026-03-06)](https://ctxt.dev/posts/en/spec-gated-delivery)
- [Async Approval Gates: Reducing Coordination Drag — Operaitions.ai (2026-03-02)](https://operaitions.ai/blog/async-workflows/)
- [CODEDELEGATOR: Decoupling Planning from Implementation — arXiv:2601.14914](https://arxiv.org/pdf/2601.14914)
- [CoDA: Context-Decoupled Hierarchical Agent — arXiv:2512.12716](https://arxiv.org/pdf/2512.12716)
- [Spec-Driven Development with Coding Agents — DeepLearning.AI (2026-04-15)](https://learn.deeplearning.ai/courses/spec-driven-development-with-coding-agents/lesson/vtd82x/workflow-overview)

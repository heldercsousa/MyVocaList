# S6.3 — Review Gates

**Status:** Researched
**Predecessor(s) ID:** S6

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Review gates are mandatory human or automated checkpoints that must be passed before a workflow phase advances to the next stage. In spec-driven development, review gates serve two critical functions: they prevent defects from propagating downstream (a defect caught at the spec review gate costs one iteration; the same defect found after implementation costs a full rework cycle), and they establish a chain of accountability — the artifact that was reviewed and approved before proceeding becomes the record of why a decision was made that way.

The GitHub Spec Kit (2025–2026) and the Verified Coherence Spec-Driven Development (VCSDD) framework both implement explicit phase gates as mandatory workflow steps. Each phase requires human approval before the next begins. The DevSpark framework enforces gates mechanically via metadata: a `Status: In Progress` spec will cause the PR review command to block approval (Hazleton, 2026).

Review gates exist across multiple layers:
1. **Specification gates** — Requirement and design review before task generation
2. **Plan gates** — Technical design review before implementation begins
3. **Implementation gates** — Code and test review before merge
4. **Convergence gates** — Cross-artifact consistency verification before shipping

---

## Phase-Gate Patterns in Practice

### GitHub Spec Kit (2025–2026)

The Spec Kit architecture implements a series of optional and mandatory gates tied to the `specify → plan → tasks → implement` workflow:

| Gate | Phase | Required? | Mechanism | Function |
|------|-------|-----------|-----------|----------|
| **Approval Gates** | All phases | Optional | Extension config (`.specify/extensions/approval-gates/approval-gates-config.yml`) | Define required roles, minimum approvals, and descriptions per phase |
| **Spec Validate** | Spec | Optional | Comprehension validation, peer review SLA, hard gate before `/speckit.implement` | Staged quizzes, peer review SLA enforcement |
| **Plan Review Gate** | Plan | Optional | `before_tasks` hook | Blocks `/speckit.tasks` unless spec.md and plan.md merged to default branch via MR/PR |
| **Staff Review** | Implementation | Optional | Read-only review post-implementation | Staff-engineer-level code review validating spec compliance, security, performance, test coverage |
| **Review** (core) | Implementation | Optional | Post-implementation comprehensive code review | 7 specialized agents covering code quality, comments, tests, error handling, type design, simplification |
| **CI Guard** | Integration | Optional | CI/CD gate | Spec compliance verification: specs exist, check drift, block merges on gaps |

The **Approval Gates extension** (merged March 2026) allows teams to specify which phases require approval and from which roles. Example configuration:

```yaml
specify:
  enabled: true
  requires: [product_lead, architect]
  min_approvals: 1
  description: "Functional spec approval"

plan:
  enabled: true
  requires: [architect, tech_lead]
  min_approvals: 2
  description: "Technical spec approval"
```

Teams can bypass gates with `--skip-review` if needed, but doing so is explicit and logged.

### VCSDD: Verified Coherence SDD (2026)

The VCSDD framework (`sc30gsw/vcsdd-claude-code`) implements a stricter 6-phase pipeline with mandatory approval gates at phase transitions:

| Phase | Gate | Gate Type | Entry Requirements |
|-------|------|-----------|-------------------|
| **1a** | Spec Crystallization | AI → Human | N/A (initial) |
| **1c** | Spec Review | Human + AI | `behavioral-spec.md` + `verification-architecture.md` exist; **spec review PASS required** (mandatory in strict mode, optional in lean mode) |
| **2a** | Test-First (Red) | Mechanical | `red-phase-evidence.json` shows `new-feature-tests: FAIL` and `regression-baseline: PASS` |
| **2b** | Implementation (Green) | Mechanical | `green-phase-evidence.json` shows `target-feature-tests: PASS` and `regression-baseline: PASS` |
| **3** | Adversarial Review | AI → Human (conditional) | Tests pass; fresh-context adversary agent runs 3–5 iterations; findings binned by severity |
| **5** | Formal Hardening | Mechanical | `verification-report.md`, `security-report.md`, `purity-audit.md` exist; proof obligations evaluated |
| **6** | Convergence | Mechanical | All artifacts present; `convergenceSignals.allCriteriaEvaluated = true`; every requirement has traced implementation |

**Key difference:** VCSDD makes human approval at the spec gate a hard requirement in `strict` mode (safety-critical work) and relaxes it to optional in `lean` mode (product iteration). The plugin implements both by tracking whether approval flags exist in filesystem.

### A-SDLC: Agentic Software Development Lifecycle (2026)

The Japanese A-SDLC framework (published by gr-sw-maker, 2026) introduces **formal AI-to-AI inspection** gates where specialized quality agents (not humans) perform gate reviews before proceeding to the next stage:

| Perspective | Inspection Focus | Pass Criteria |
|-------------|------------------|--------------|
| **R1** | Specification Quality | Requirements IDs assigned, no ambiguity, edge cases covered |
| **R2** | Design Principles | No SRP/OCP/DIP/DRY/KISS/YAGNI violations |
| **R3** | Code Quality | Error handling, null safety, defensive programming |
| **R4** | State Transitions | No deadlocks, race conditions, or missing transitions |
| **R5** | Performance | Algorithmic efficiency, memory, network costs |
| **R6** | Consistency | Specs ↔ Design ↔ Implementation ↔ Tests all traceable |

If even a single **Critical/High issue** remains, the process cannot advance to the next stage. This is a **mechanical, non-negotiable gate** — unlike human review, AI-to-AI inspection finishes instantly and surfaces issues deterministically.

The insight: AI inspection agents can enforce conformance more consistently than humans because they don't suffer from fatigue, context loss, or approval bias. Multiple independent agents assigned to the same gate provide redundancy.

### cc-sdd: Autonomous Implementation with Per-Task Review

The cc-sdd framework (GitHub: gotalab, 2025) runs autonomous implementation with **per-task independent review** gates:

```
/kiro-impl:
  For each task:
    1. Fresh implementer agent (TDD: RED → GREEN)
    2. Independent reviewer agent (gates implementation)
    3. Auto-debug agent (investigates failures in clean context)
    4. Learning propagation (insights feed forward to next task)
```

The design uses **task boundary enforcement** — each task carries `_Boundary:_` and `_Depends:_` annotations. Review and validation look for boundary violations, not just style issues.

---

## Reviewer Context Loss

Despite their importance, review gates in practice suffer from three forms of context loss (described in S6, Hazleton 2026):

**1. Temporal distance:** The spec is reviewed at planning time; implementation happens days or weeks later. The reviewer approving implementation may not remember decisions made during spec review.

**2. Tacit knowledge gap:** Spec reviewers typically have domain expertise; code reviewers typically understand syntax and patterns. These are rarely the same person, creating inconsistency.

**3. Volume compression:** In high-throughput SDD teams, reviewers see many spec artifacts per sprint. Approval becomes ritual rather than substantive. The gate exists formally but not functionally.

### Mitigation

**Spec-code alignment tools** (Semcheck, SpecFact, Rigour, Asymptote, Augment Code) partially compensate by making drift visible automatically. Rather than requiring reviewers to reconstruct intent from memory, these tools flag mismatches directly. The reviewer's task shifts from "detect drift" to "adjudicate it" — still requiring domain context, but with explicit signal.

The Pockit guide (2026) recommends a **three-review workflow** to manage this:

1. **Design review** (10 minutes per feature) — Human reviews design.md for database schema, API consistency, security implications
2. **TDD workflow** (0 additional time) — AI writes tests and implementation while human reviews design
3. **Implementation review** (post-code) — Focused on patterns, performance, edge cases — not architectural correctness, which was already locked in at design gate

This shifts human effort from "review every line" to "review three structured artifacts" (design, tests, code patterns) — a 10x reduction in review volume while maintaining quality gates.

---

## Gate Configuration and Bypass

### GitHub Spec Kit Approach

Gates are configured via YAML in `.specify/extensions/approval-gates/approval-gates-config.yml`. Teams can:
- Define required roles per phase
- Set minimum approval counts (default 1)
- Write human-readable descriptions for each gate
- Use `--skip-review` to bypass (logged explicitly)

### VCSDD Approach

Gates are enforced via filesystem state:
- `spec-review.approved` file presence indicates human approval of Phase 1c
- Absence in `lean` mode allows progression; absence in `strict` mode blocks
- Sprint contract `verdict.json` gates Phase 3 entry in strict mode

### Hook-Based Enforcement (Claude Code)

Claude Code supports a `Stop` hook that executes before an agent ends its session. This can validate:
- All uncommitted changes are staged and committed
- All tests pass
- Spec files are updated to match implementation

Example (from MyVocaList's commit discipline rule):

```
Stop hook:
  if uncommitted changes exist:
    exit code 2 (block session end)
    stderr: "Uncommitted changes detected. Run git add + git commit before stopping."
```

---

## Gate Triggers and Timing

Review gates fire at distinct points in a workflow. Trigger timing affects how much work must be redone if a gate fails:

| When | Gate Type | Cost of failure | Example |
|------|-----------|-----------------|---------|
| Before phase start | Gating phase transition | Rework current phase | Spec review gate before implementation begins |
| After phase completion | Checking phase output | Rework + propagate | Implementation review gate before merge |
| After multiple phases | Cross-artifact consistency | Highest cost | Convergence gate checking all artifacts align |

The earliest gate catches a defect with the lowest downstream cost. A spec defect caught at the spec review gate requires one cycle to fix; the same defect caught during code review requires fixing the spec, then regenerating and re-testing code.

---

## Distributed Approval: Multi-Agent Scope

When multiple agents work on interdependent specs (parallel work on features that share data models or APIs), review gates must coordinate across agents to catch **multi-agent scope conflicts** — code changes that are individually valid but incompatible when combined.

Example from S6.4.1:
- Agent A changes a data model's `name` field to `displayName`
- Agent B's feature still uses `name` in three places
- No merge conflict (different files)
- Both agents' code passes individual tests
- Combined code fails at integration time

Mitigation:

1. **Cross-spec review gate (cc-sdd):** `/kiro-spec-batch` reviews multiple specs in parallel for contradictions, duplicated responsibilities, and interface mismatches before implementation begins.

2. **Multi-agent scope conflict detection (Rigour):** Real-time hooks that track file modifications across parallel agents and flag incompatible edits before they merge.

3. **Integration test gate:** Automated suite that runs with all agents' changes combined. Catches integration failures before human review.

---

## Quality Gate Severity Classification

Most modern SDD workflows use severity levels to triage gate findings:

| Level | Definition | Blocks advancement? |
|-------|-----------|-------------------|
| 🔴 **Blocker/Critical** | Violates non-negotiable constraint; spec unimplementable; security vulnerability | YES |
| 🟡 **Warning/High** | Significant deviation; performance concern; edge case uncovered | Often yes; negotiable |
| 🟢 **Suggestion/Low** | Code style, naming, documentation improvements | NO |

A gate that has **zero Blockers and zero Highs** typically passes. A gate with unresolved Highs typically escalates to maintainer judgment.

---

## Gate Automation vs. Human Review

### When to Automate
- Structural conformance (spec has all required fields, design includes architecture section)
- Deterministic checks (code compiles, tests run, no hardcoded secrets)
- Cross-artifact traceability (requirements traced through design to code)
- Resource policy violations (spec budget exceeded, database columns not indexed)

### When to Require Humans
- Trade-off decisions (performance vs. correctness; complexity vs. simplicity)
- Domain-specific judgment (does this API design match our ubiquitous language?)
- Business alignment (does this implementation match the intent behind the spec?)
- Security and compliance decisions requiring authority

The pattern from VCSDD: **AI agents handle deterministic gates (Red/Green phase checks, formal verification); humans handle judgment gates (spec approval, adversary findings triage).**

---

## Preventing Silent Task Completion

One documented failure mode in distributed SDD workflows (S5.3.1) is **phantom completions** — agents mark verification tasks done without executing them. Review gates are where this surfaces.

The cc-sdd framework addresses this with **mechanical verification before dispatch** — after an agent claims a task is complete:

1. Shell-side validation gate checks: task actually committed, working tree clean, target test files exist, tests pass
2. If shell gate fails, the agent's narrative claim is discarded; only mechanical signals matter
3. Agents cannot override shell gate results

Example protocol:

```
Agent says: "Task complete, all tests pass"
Shell gate runs: dotnet test
  ✓ Tests pass → Task marked complete
  ✗ Tests fail → Task reverted, agent re-prompted with failure output
```

---

## Gate Policies and Escalation

Large teams require policies for what happens when a gate fails and who can override:

| Scenario | Action | Authority |
|----------|--------|-----------|
| Gate detects Blocker | Block advancement; agent must fix | No override |
| Gate detects unresolved High | Block by default; escalate to maintainer | Maintainer can override with justification |
| Gate fails mechanically (e.g., test runner crashes) | Fallback to manual gate | On-call engineer |
| Human reviewer requests changes | Block advancement | Reviewer must approve changes |
| Human reviewer unavailable (SLA missed) | Escalate to backup reviewer | Escalation policy |

---

## Sources

- [Approval Gates extension — GitHub Spec Kit PR #2010](https://github.com/github/spec-kit/pull/2010)
- [GitHub Spec Kit — Community Extensions Overview](https://github.com/github/spec-kit)
- [Add /speckit.review — Staff-level code review — GitHub Spec Kit PR #2043](https://github.com/github/spec-kit/pull/2043)
- [Plan Review Gate extension — GitHub Spec Kit PR #1993](https://github.com/github/spec-kit/pull/1993)
- [Verified Coherence Spec-Driven Development — sc30gsw/vcsdd-claude-code](https://github.com/sc30gsw/vcsdd-claude-code)
- [Getting Started with DevSpark: Requirements Quality Matters — Mark Hazleton](https://markhazleton.com/articles/test-driving-githubs-spec-kit.html)
- [SPEC-Driven Development Workflow — Kehao Chen](https://gist.github.com/kehao-chen/22bc28f4c825b5f9af9c5c411f89ba89)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship Production-Ready AI-Generated Code — Pockit](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Preventing Quality Collapse in AI-Driven Development with AI Agent Collaboration and SDD — zenn.dev](https://zenn.dev/good_relax/articles/6855d0ee4a7d54?locale=en)
- [cc-sdd: Long-running spec-driven implementation for AI coding agents — GitHub](https://github.com/gotalab/cc-sdd)
- [How to Build Human-in-the-Loop Approval Gates for AI Coding Agents — DEV](https://dev.to/sahil_kat/how-to-build-human-in-the-loop-approval-gates-for-ai-coding-agents-fo6)
- [auto-sdd: Autonomous Spec-Driven Development with Quality Gates — GitHub](https://github.com/fischmanb/auto-sdd)
- [AIDLC Phase 3: Design — SwarmAI](https://github.com/xg-gh-25/SwarmAI/blob/main/docs/AIDLC-Phase3-Design.md)

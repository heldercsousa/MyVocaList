# S6 — Governance & Enforcement

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Governance in SDD is the layer that converts advisory guidance into enforceable constraints. Without it, agents treat instructions as recommendations — complying when context is fresh and reverting to training defaults as context fills or pressure mounts. The four mechanisms that make governance real are: constitutional constraints (what an agent may never do), automated hooks (shell-level enforcement that executes outside the context window), review gates (human checkpoints before phase transitions), and CI/CD integration (pipeline-level conformance checks that run on every push). Together, they move compliance from a matter of prompt quality to a matter of structural design.

---

## S6.1 — Constitutional Constraints

A project constitution is a versioned document — typically `CLAUDE.md` or `constitution.md` — that captures the non-negotiable principles governing every feature, every session, and every subagent. It differs from a spec in a precise way: specs describe what to build; the constitution defines the laws every build must obey regardless of what is being built.

The Agent Factory (Panaversity, 2026) describes the canonical content structure:

- **Architecture principles:** How components must relate. "Services communicate only through defined interfaces; never by importing from each other's internals."
- **Technology constraints:** Locked choices (language version, database, auth approach) that the agent must not override.
- **Code quality standards:** Measurable properties every generated file must satisfy — function length limits, test coverage thresholds, required documentation.
- **Security requirements:** Non-negotiables (no secrets in code, input validation at every external boundary) that must apply even when a spec omits them.
- **Workflow rules:** Procedural behavior — when to ask clarifying questions, commit message format, how to handle spec ambiguity.

The constitution loads before any spec and before any prompt. Its scope extends across parallel subagents: because all subagents read the same file, consistency is structurally guaranteed rather than dependent on inter-agent communication.

In the MyVocaList project this pattern is implemented via `CLAUDE.md` (project root) + global `~/.claude/CLAUDE.md`. The project constitution extends and overrides global defaults where they conflict. The "Non-Negotiables" section is the constitutional core — rules like "never use `DisplayAlert`" and "DevExpress first, always" are constitutional constraints, not stylistic preferences.

### S6.1.1 — Constitutional Rigidity and Staleness

A constitution that cannot evolve becomes a liability. Principles written at project inception may contradict patterns discovered during implementation. When a constitutional rule is wrong, agents either violate it (producing build failures) or obey it (producing wrong architecture). Neither outcome is acceptable.

Specific failure modes:

- **Obsolete technology constraints:** A locked framework version remains in the constitution after the team has migrated. New agents generate against the old constraint.
- **Contradictory rules:** Two constitutional principles conflict in an edge case the author never anticipated. Agents resolve ambiguity unpredictably.
- **Architectural evolution:** The spec-first principle works for greenfield; the constitution has no clause for brownfield retrofits. Agents apply spec-first rules where the codebase requires incremental integration.

The rigidity problem is not solved by making the constitution more detailed — that compounds maintenance burden. It is managed by keeping constitutional rules at the principle level (not the implementation level) and by establishing an amendment process before rules become stale.

### S6.1.2 — Amendment Governance

Constitutional rules require a change process as formal as the rules themselves. Without one, amendments happen informally (someone edits `CLAUDE.md` without review) or not at all (stale rules accumulate). The DevSpark framework (Hazleton, 2026) addresses this with a multi-tier constraint inheritance model:

- Lower tiers can extend or strengthen upper-tier rules.
- Lower tiers cannot weaken or override upper-tier rules.
- A project-level override can add a stricter test coverage requirement; it cannot reduce the framework baseline minimum.

The spec-kit-plus specification formalizes this further:

```
Section 4.2: Amendment Process
Modifications to this constitution require:
- Explicit documentation of rationale for change
- Review and approval by project maintainers
- Backwards compatibility assessment
```

Amendment governance is most commonly absent in small teams, where the author of the constitution is also its sole maintainer. The risk surfaces when the project scales: contributors edit rules without coordination, producing constitutional drift — the rules and the project's actual behavior diverge silently.

---

## S6.2 — Automated Hooks

Hooks are shell scripts that execute at defined points in the agent lifecycle, outside the context window. Their critical property: a shell process cannot be overridden by a language model. The model cannot argue with, forget, or reason around an exit code. This relocates enforcement from a prompt-compliance problem to a structural one.

Claude Code supports four hook lifecycle events (AgentPatterns.ai, 2025):

| Event | Trigger | Primary use |
|-------|---------|-------------|
| `PreToolUse` | Before any tool call | Block forbidden operations, enforce naming conventions, validate file paths |
| `PostToolUse` | After tool completion | Audit trail, validate output, inject additional context |
| `Notification` | On agent notifications | Logging, metrics |
| `Stop` | Agent about to end turn | Completion gates — tests must pass, spec must be updated before "done" |

A hook exits with code `2` to block the tool call; `stderr` becomes the reason fed back to the model. A `Stop` hook exiting `2` prevents the agent from ending its turn — it must continue until the gate passes.

Beyond blocking, hooks can rewrite inputs: printing JSON with `updatedInput` to stdout causes Claude Code to replace the original tool input before execution. This enables automatic normalization (correcting file paths, enforcing commit message format) rather than just blocking.

Hook scopes in Claude Code determine who can override them:

| Scope | Location | Override by user? |
|-------|---------|-------------------|
| User | `~/.claude/settings.json` | Yes |
| Project | `.claude/settings.json` (committed) | Yes |
| Local | `.claude/settings.local.json` | Yes |
| Managed | Enterprise MDM policy | No |

Managed hooks are the organizational enforcement floor. They cannot be disabled by project or user configuration. This is the appropriate scope for security-critical rules (no secrets in source, no force push to main).

The SmartScope guide (2025) documents a phase-gate pattern using hooks to enforce the spec → design → implementation flow. Approval flags (`.approved` files) gate each phase; hooks check for these flags before permitting tool calls that would advance the phase. Without the approval flag, `Edit`, `Write`, and `Bash` are blocked — only `Read` and `Grep` are permitted.

In the MyVocaList project, the `Stop` hook already enforces the commit discipline rule: if uncommitted changes exist when an agent session ends, the hook warns. This is a live example of constitutional enforcement via hook rather than prompt instruction.

### S6.2.1 — Enforcement Cost Overhead

Hooks add latency to every tool call. The GitHub Copilot documentation recommends keeping hook execution under 5 seconds per invocation. At scale — hundreds of tool calls per session, multiple parallel agents — enforcement overhead compounds.

Known failure modes in Claude Code hooks (AgentPatterns.ai, 2025):

- **Exit code 2 coverage gaps:** `PreToolUse` exit code 2 has been documented to fail to block `Write` and `Edit` while still blocking `Bash` (issue #13744). This means a hook written to block file writes may silently not fire for all tool types.
- **Silent hook failures:** Any exit code other than `0` or `2` is treated as a hook error and does not block. A hook with a missing dependency produces no enforcement and no warning.
- **Idle halt instead of feedback:** Exit code 2 has caused the agent to halt idle rather than continue with the stderr feedback as input (issue #24327).

The operational implication: hooks are not a complete enforcement boundary for all tool types in all agent harnesses. They reduce the attack surface significantly but do not eliminate it. The correct response is to use hooks for every binary, non-negotiable rule while accepting that coverage is strong but not absolute.

---

## S6.3 — Review Gates

Review gates are mandatory human checkpoints before a phase transition completes. Their purpose is verification that the current phase artifact (spec, design, tasks) satisfies its intended constraints before generating the next downstream artifact. A defect caught at the spec review gate costs one correction; the same defect caught after implementation costs a full rework cycle.

The GitHub Spec Kit (2025) and DevSpark both implement phase gates explicitly: constitution → spec → plan → tasks → implement. Each phase requires human approval before the next begins. The DevSpark metadata model enforces this mechanically — a `Status: In Progress` spec will cause the PR review command to block approval.

Review gates serve a second function beyond defect detection: they establish a chain of accountability. Because the spec was reviewed and approved before implementation, the question "why was this built this way?" has a traceable answer — the approved spec and plan documents.

### S6.3.1 — Reviewer Context Loss

Review gates assume the reviewer has sufficient context to judge whether the artifact is adequate. In practice, three forms of context loss degrade review quality:

**1. Temporal distance.** The spec is written at planning time; implementation happens days or weeks later. The reviewer approving the implementation may not remember the decisions made during spec review.

**2. Tacit knowledge gap.** The reviewer who approved the spec may have domain expertise the reviewers who examine the generated code lack. The code review gate is staffed by people who understand syntax and patterns; the spec review gate is staffed by people who understand domain intent. These are rarely the same person.

**3. Volume compression.** In high-throughput SDD teams, reviewers see many spec artifacts per sprint. Approval becomes a ritual rather than a substantive check. The gate exists formally but not functionally.

Tools that surface spec-code alignment (Semcheck, SpecFact, Augment Code Verifier) partially compensate for reviewer context loss by making drift visible automatically — reviewers do not need to reconstruct intent from memory when a tool flags the mismatch directly. But these tools shift the reviewer's task from detecting drift to adjudicating it, which still requires domain context.

---

## S6.4 — CI/CD Integration

CI/CD integration makes spec conformance a pipeline property rather than a session property. Where hooks enforce compliance during generation, CI enforces it at merge time — providing a last line of defense that runs on every push, independent of how the code was produced.

The Augment Code guide (2026) describes the canonical pattern:

1. **Spec validation gate:** Validates that the specification itself is structurally sound (schema validation, required fields present, EARS notation compliance) before permitting code generation or merge.
2. **Verifier agent gate:** Compares generated code against the living spec and flags behavioral contract violations that pass syntax and type checks. As the Augment Code documentation states: "A diff-level reviewer sees that the code compiles. The Verifier sees that the endpoint no longer enforces the validation contract."

The dual gate pattern — Verifier runs internally before the agent opens a PR, then CI re-runs it as a hard gate the agent cannot bypass — is the recommended implementation. The second run exists because agents can mark verification as complete without running it (see S5.3.1 — Silent Task Completion).

Tooling in this space as of 2025–2026:

| Tool | Mechanism | Integration |
|------|-----------|-------------|
| **Semcheck** (semcheck.ai) | AI-powered spec-code comparison via `semcheck.yaml` rules | Pre-commit hooks, CI pipelines |
| **SpecFact** (specfact.com) | CLI with observe → enforce mode progression | GitHub Actions, Azure DevOps |
| **Rigour** (rigour.run) | 27+ quality gates, real-time hooks, multi-agent scope conflict detection | All major AI tools + CI |
| **Asymptote** (asymptotelabs.ai) | Security-focused guardrails, hook-based inline feedback during generation | Claude Code, Cursor |
| **Augment Code / Intent** | Living spec layer + Verifier agent gate | GitHub Actions |

### S6.4.1 — Six Drift Categories

The Augment Code analysis (2026) identifies six silent divergence surfaces where spec-code alignment breaks without triggering conventional CI failures:

| # | Drift category | Description |
|---|---------------|-------------|
| 1 | **Behavioral contract violations** | Code is syntactically correct, tests pass, but the implementation no longer enforces the contract defined in the spec (e.g., a validation rule silently dropped) |
| 2 | **Resource policy drift** | Infrastructure or storage configuration diverges from the spec's resource constraints |
| 3 | **Latency / error budget erosion** | Cumulative performance degradation that does not trigger any single test failure |
| 4 | **Static analysis gaps** | Findings that static analysis catches but that are excluded from the CI gate via suppression |
| 5 | **Malicious or supply-chain drift** | Hallucinated dependencies or packages with known CVEs introduced by agent generation |
| 6 | **Multi-agent scope conflicts** | Two agents modifying interdependent code in incompatible ways without a merge conflict (because they modify different files) |

Conventional CI (build, lint, unit tests) catches none of these categories reliably. Each requires a dedicated detection layer: spec conformance tools for categories 1–2, performance monitoring for category 3, software composition analysis for category 5, and multi-agent coordination hooks for category 6.

### S6.4.2 — Continuous Conformance Requirement

Spec-code drift is not a threshold phenomenon — it does not accumulate until it crosses a visible line and then become obvious. It compounds silently. A 1% drift today that is not corrected creates a 2% drift after the next sprint, because subsequent agents generate against the already-drifted codebase rather than the spec.

The Kinde analysis (2025) makes this explicit: "If the spec doesn't keep pace with code changes, or code doesn't keep pace with spec changes, you end up with a dangerous gap between what you say your software does and what it actually does."

Continuous conformance means:
- Drift detection runs on every push, not periodically
- The spec is the source of truth; code that diverges fails the gate
- When drift is detected, the correction is applied to the code (not to the spec) unless the spec is intentionally changing
- Drift detection tools are set to `enforce` mode, not `observe` mode, in production pipelines

SpecFact's staged rollout model (observe → enforce) reflects the operational reality: teams add CI conformance checks in observe mode first to understand the false positive rate, then promote to enforce mode once confidence is established. Starting in enforce mode on an existing codebase risks blocking legitimate work during the calibration period.

---

## Sources

- [The Project Constitution — Agent Factory / Panaversity](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/the-project-constitution)
- [Getting Started with DevSpark: Requirements Quality Matters — Mark Hazleton](https://markhazleton.com/articles/test-driving-githubs-spec-kit.html)
- [Prompt Metadata: Enforcing the DevSpark Constitution — Mark Hazleton](https://markhazleton.com/blog/devspark-prompt-metadata-control)
- [Security Constitution for AI Code Generation — AgentPatterns.ai](http://agentpatterns.ai/security/security-constitution-ai-code-gen/)
- [Enforcing Agent Behavior with Hooks — AgentPatterns.ai](https://agentpatterns.ai/instructions/enforcing-agent-behavior-with-hooks/)
- [Enforcing Spec-Driven on AI Agents — SmartScope](https://smartscope.blog/en/ai-development/enforcing-spec-driven-development-claude-copilot-2025/)
- [About hooks — GitHub Copilot Docs](https://docs.github.com/copilot/concepts/agents/coding-agent/about-hooks)
- [Hooks — Codex / OpenAI Developers](https://developers.openai.com/codex/hooks/)
- [Agent Hooks in Azure SRE Agent — Microsoft Learn](https://learn.microsoft.com/en-us/azure/sre-agent/agent-hooks)
- [CI/CD for AI Agents — Augment Code](https://www.augmentcode.com/guides/cicd-ai-agents-pipeline-integration)
- [Spec Drift: The Hidden Problem AI Can Help Fix — Kinde](https://www.kinde.com/learn/ai-for-software-engineering/ai-devops/spec-drift-the-hidden-problem-ai-can-help-fix/)
- [Semcheck — AI-Powered Specification Compliance Tool](https://semcheck.ai/)
- [SpecFact — Review AI-assisted code before drift reaches PR or main](https://specfact.com/)
- [Rigour — Deterministic quality gates for AI-generated code](https://docs.rigour.run/)
- [Code Generation Guardrails — Asymptote](https://docs.asymptotelabs.ai/features/guardrails)
- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog](https://resources.github.com/increasing-collaborative-development-with-ai/)
- [Constitutional AI — Cogitator Docs](https://cogitator.app/docs/advanced/constitutional-ai)
- [spec-driven.md — panaversity/spec-kit-plus](https://github.com/panaversity/spec-kit-plus/blob/main/spec-driven.md)

# S5.3 — Subagent Delegation

**Status:** Researched  
**Predecessor(s) ID:** S5

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Subagent delegation is the architectural pattern in which a main agent (orchestrator) retains high-level coordination, decision-making, and synthesis while delegating all file creation, modification, and task execution to child agents (subagents), each operating in its own isolated context window with fresh state.

The pattern solves a fundamental problem: as code generation tasks grow beyond a single session's scope, a single agent's context window becomes insufficient to hold both the full system specification and the detailed implementation work. Worse, the same agent that designed the solution also implements it and reviews its own output — a structural vulnerability to confirmation bias and self-validation. Subagent delegation breaks this by making the main agent's role explicit: orchestrate, not execute.

---

## Core Mechanism

### Structural Properties

The pattern has three defining properties:

1. **Role separation** — the main agent never makes file writes; all edits are delegated to subagents
2. **Isolation** — each subagent starts with a fresh context window; it does not inherit the main agent's conversation history
3. **Asymmetric communication** — the main agent sends tasks to subagents; subagents report results back; no peer-to-peer subagent communication

### Three Agents: Orchestrator, Implementor, Verifier

The orchestrator-worker pattern in production typically structures work as:

- **Orchestrator (Main Agent)** — the only agent that reads the spec before work begins, analyzes what needs to be done, decomposes it into scoped subtasks, dispatches subagents, reviews results, and decides whether to proceed or replan
- **Implementor (Subagent)** — receives a scoped task (file paths, constraints, expected output), executes all file writes, and returns a status signal (done/fail) plus any artifacts or structured output
- **Verifier (Subagent, optional)** — in critical codebases, a separate subagent receives the Implementor's output and the original spec (not the Implementor's reasoning) and produces a structured pass/fail verdict with evidence

This three-role division mirrors the Coordinator–Implementor–Verifier (CIV) pattern documented extensively in academic SDD research. The orchestrator and verifier are both reasoning-heavy; the implementor is execution-focused. They can be routed to different model tiers: stronger (more expensive) models for orchestration and verification, cheaper (faster) models for scoped implementation work.

### Claude Code Subagent Mechanics

Claude Code implements subagent delegation through the **Task tool**, which:

1. **Spawns a new context window** — the subagent starts with no conversation history from the main agent
2. **Provides isolated tool access** — the subagent can be configured with specific tools or inherits the main agent's full tool set
3. **Runs to completion** — the subagent executes the task and returns a result; the main agent synthesizes that result and decides what to do next
4. **Optional worktree isolation** — with `isolation: worktree`, the subagent gets its own git worktree copy of the repository, enabling true file-level isolation for parallel work

Custom subagents in `.claude/agents/` allow teams to define reusable specialist roles — a "RefactorSpecialist," a "TestWriter," a "APIDesigner" — that the orchestrator can invoke by name when a task matches the subagent's description.

---

## MyVocaList's Implementation

MyVocaList's `workflow.md` codifies a strict variant of subagent delegation designed for small-to-medium team SDD work:

### Briefing Protocol

Subagents receive **file paths only, never inline content**:

- Correct: "Update `MyVocaList.Services/VenueService.cs` by adding a `SearchVenuesAsync` method that..."
- Wrong: "Update this file: [pasted 500 lines of code]..."

**Why:** If the main agent pastes file content, and four subagents run in parallel, the content is stored four times in the conversation history. The orchestrator's context grows by 4x. When the orchestrator summarizes or compacts (as required in long sessions), it compacts the same content four times over. The cascade cost is ~token_expense = 4x file_size + 4x compaction_overhead.

The correct approach: each subagent reads its own files independently. The main agent never reads the full file — only the spec and the task description. File reads happen in subagent context and disappear when the subagent returns.

### Return Protocol

Subagents communicate completion **only by**:

1. **Updating `Docs/task-log.md`** — entry format: `| 2026-04-30 | FeatureName | done | Brief one-line result |` or `fail | Reason`
2. **Creating a git commit** — including all file changes from this task
3. **Pushing to remote** — `git push origin HEAD` so the orchestrator and team can verify completion
4. **Stopping (exiting the session)** — the subagent does not return summaries, diffs, or explanations to the main agent

The main agent never reads the subagent's session transcript or output messages. It reads only the commit history (`git log`) and task-log entries.

### Exit Checklist (Mandatory)

Every subagent must execute this sequence before reporting done:

1. **Build** — `dotnet build` or equivalent; 0 errors required
2. **Test** — `dotnet test` if applicable; all tests passing
3. **Commit** — `git add <specific-files>` and `git commit -m "<message>"`
4. **Push** — `git push origin HEAD`
5. **Stop** — the session ends

If any step fails, the subagent updates task-log with `fail | Reason: build failed with X errors` and stops. The orchestrator reads this, does not proceed, and either retries the task or escalates.

### Parallel Execution Cap

Maximum **4 subagents in parallel per wave**, dispatched in waves:

1. Coordinator analyzes spec and identifies independent tasks
2. Groups tasks into waves (wave 1 = no dependencies, wave 2 = depends on wave 1, etc.)
3. Spawns all agents in wave N simultaneously
4. Waits for full wave to complete before starting wave N+1
5. If a task fails, the coordinator replans or retries within the same wave

This cap is chosen based on:
- Token budget per session (4 parallel = 4x context isolation overhead, but amortized across the wave)
- Operational load (5+ concurrent agents may exceed available model capacity)
- Git merge complexity (more than 4 parallel file edits increase conflict surface)

---

## Why Subagent Delegation Works

### Context Isolation

The primary benefit is **context isolation**: work done by subagents never pollutes the orchestrator's context.

Scenario: Implement a 12-file feature in a single agent.
- Agent reads all 12 files into context (150 KB total)
- Agent starts implementing, adding 200 lines to each file as it goes
- Agent's context grows to 300 KB, then 450 KB
- By the end, the agent's context is filled with file reads, edits, test output, and intermediate reasoning
- When the agent compacts (summarizes the conversation to fit future messages), it must compact all 300+ KB

Scenario: Same feature, 4 subagents, 3 files each.
- Orchestrator reads the spec (5 KB)
- Orchestrator assigns tasks and spawns subagents (4x 3-file reads = 4x 37.5 KB each)
- Each subagent works locally, edits files, and returns
- Each subagent's context grows to ~150 KB, then disappears when it returns
- Orchestrator's context stays at 5 KB + task results (50 KB total)

Token economics: Delegation is viable when `files_per_task > 8` (measured by Gentleman-Programming/agent-teams-lite in 2026). For smaller tasks, inline execution is cheaper.

### Specialization and Cheaper Models

A production team can route tasks to cheaper models by task type:
- **Orchestrator** — Claude Opus (reasoning-heavy, higher cost, better at planning)
- **Implementors** — Claude Sonnet or Haiku (code-focused, faster, cheaper)
- **Verifier** — Claude Opus (reasoning-heavy again, but for critique)

This model tiering makes delegation economically viable for large features.

### Separation of Concerns

The main agent never accumulates implementation-role context. It stays in "orchestrator mode" — reading specs, analyzing plans, making go/no-go decisions — rather than gradually shifting to "implementor mode" as context grows. This preserves the orchestrator's ability to step back and replan if a subagent fails.

### Auditability

All work is recorded as commits to task-log.md and git history. The team can see:
- Which subagent did what (by commit author or task-log entry)
- When (by timestamp)
- Status (done/fail)
- Why (one-line reason)

This is especially valuable in regulated environments or when analyzing why a parallel wave failed.

---

## Failure Modes

### Silent Task Completion (S5.3.1)

See S5.3.1 for the full treatment. In brief: a subagent marks a task done without actually executing all verification steps (build, test, commit, push). The main agent proceeds; the broken code surfaces only when the main agent runs its own build check, or worse, in integration.

**Mitigations:**
1. **Explicit exit checklist** — every briefing includes the mandatory sequence, stated as literal commands
2. **Main agent verification** — the orchestrator runs `dotnet build` and `dotnet test` after each wave, independently of subagent self-reports
3. **Structured status reporting** — task-log format enforces binary done/fail with one-line reason
4. **Automated hooks** — the Stop hook warns if uncommitted changes remain when a session ends

### Role Confusion Mid-Delegation

A subagent that was told to "implement feature X" may start reasoning about system-wide implications or security concerns that belong to the orchestrator's planning phase. The subagent then produces a more conservative or redesigned version of what was asked, violating the contract that subagents execute what they are told, not what they think should be done.

**Mitigation:** Session isolation itself partially addresses this — a fresh subagent doesn't inherit the orchestrator's reasoning process. But the subagent's prompt must be explicit: "You are implementing Feature X. You do not redesign or question the specification. You follow the provided task exactly."

### Dependency Ordering Fragility (S5.2.1)

When subagents run in parallel and hidden inter-task dependencies emerge at execution time, the failure cascades. Subagent B fails because it depends on an output from Subagent A that the orchestrator's plan did not surface.

**Mitigation:** The orchestrator must perform dependency analysis before assigning tasks to waves. Shared artifacts (interfaces, DTOs, migrations, DI registrations, configuration files) must be explicitly enumerated in the plan. Conservative wave sizing — if a dependency is uncertain, promote the task to its own wave rather than parallelizing speculatively.

### Cross-Agent Spec Conflicts (S5.2.2)

Two subagents working on interdependent tasks produce locally-correct outputs that are globally contradictory when integrated. The verifier, which validates each agent's output against the spec independently, cannot catch cross-agent semantic contradictions unless the spec defines shared contracts with precision.

**Mitigation:** Shared contracts must be defined in the spec before implementation begins. DTOs, API signatures, error codes, and event schemas are not inferred by agents — they are specified. When an implementor makes a spec-level decision (e.g., choosing a field name not explicitly specified), that decision is written back to the spec before parallel agents consume the same interface.

---

## Comparison to Related Patterns

### vs. Adversarial Agent Pattern (S5.1)

| Pattern | Main Agent | Subagents | Interaction |
|---------|-----------|-----------|------------|
| **Subagent Delegation** | Orchestrator (plan, decide) | Implementors (execute) | One-way: orchestrator → subagent → orchestrator |
| **Adversarial (CIV)** | Coordinator | Implementor + Verifier | Two-way: Coordinator → Implementor → Verifier → Coordinator; Verifier independent of Implementor |

Subagent delegation is a subset of CIV when a verifier role is added. Pure subagent delegation (main agent + implementors only) is CIV without the independent verifier.

### vs. Parallel Agent Execution (S5.2)

Parallel execution is the specific tactic of running multiple subagents simultaneously. Subagent delegation is the structural pattern — it enables parallelism but is not limited to it. A sequential wave of subagents (execute task 1, wait for result, execute task 2) is still subagent delegation.

---

## When to Use Subagent Delegation

**Use when:**
- Features span 8+ files (token crossover point where delegation saves tokens)
- Tasks must be parallelized to meet timeline (> 4 hours of sequential work compressed to < 2 hours)
- Separation of specification from implementation is a requirement (regulated industries, large teams, security-sensitive code)
- The main agent must handle multiple features simultaneously (each delegated to different subagents)
- Context preservation is critical (avoid compaction by keeping the orchestrator's context small)

**Don't use when:**
- Feature is trivial (rename, 1-file edit) — overhead of delegation exceeds benefit
- Feature is tightly coupled (all files depend on all others) — parallelism does not apply; sequential is safer
- Main agent must verify outputs in detail (defeats isolation benefit)
- Team is single-person and context cost is not a constraint

---

## Sources

### Tier 1 — Primary Sources

- [How and when to use subagents in Claude Code — Anthropic (claude.com)](https://claude.com/blog/subagents-in-claude-code) — Official Anthropic description of subagent isolation, context windows, parallel execution, and custom subagent definitions
- [Create custom subagents — Claude Code Docs](https://code.claude.com/docs/en/sub-agents) — Claude Code subagent mechanics, Task tool, isolation: worktree, fork vs named subagent
- [Orchestrate teams of Claude Code sessions — Claude Code Docs](https://code.claude.com/docs/en/agent-teams) — Agent teams vs subagents comparison, shared task list, direct agent-to-agent messaging
- [Building Effective AI Agents — Anthropic (December 2024)](https://www.anthropic.com/research/building-effective-agents) — Canonical six-pattern taxonomy including Orchestrator-Workers as the dynamic task decomposition pattern
- [Agent Teams Lite — Gentleman-Programming (GitHub)](https://github.com/Gentleman-Programming/agent-teams-lite) — Open-source reference implementation of orchestrator + 9 specialized SDD phase subagents, 2026 GitHub link

### Tier 2 — Secondary Sources

- [Orchestrator-Workers — Encyclopedia of Agentic Coding Patterns](https://aipatternbook.com/orchestrator-workers) — Detailed definition of orchestrator decision/dispatch/synthesize cycle, comparison to fixed workflows
- [Claude Code Advanced Multi-Agent Guide — Claude Lab (2026)](https://claudelab.net/en/articles/claude-code/claude-code-multi-agent-advanced) — Practical parallel subagent patterns with executor examples, worktree isolation mechanics
- [Running Multi-Agent Orchestration — Cursor Workshop (2026)](https://www.cursorworkshop.com/research/multi-agent-orchestration-2019564738649505882) — Opus-as-Conductor / Codex-as-Workers pattern, model tiering strategy, worker contract definition
- [Pattern: Orchestrator-Worker — Intelligence Patterns: Reusable Elements of Agentic Design](https://agents.kour.me/orchestrator-worker/) — Pattern definition, specialization benefits, context isolation principle ("share memory by communicating")
- [Orchestrating AI Agent Teams — Dotzlaw Consulting (2026)](https://dotzlaw.com/insights/claude-deterministic-agent-engineering/) — Integration of skills, hooks, and context flow in orchestrator patterns; PostToolUse additionalContext mechanism
- [Multi-Agent Systems: Patterns That Work Beyond the Demo — Replyant (2026)](https://replyant.com/lab/multi-agent-systems/) — Orchestrator-worker comparison to peer agent teams, circuit breaker patterns for failure handling

### Tier 3 — Tertiary Sources

- [Token Economics of Agent Teams — Gentleman-Programming (GitHub)](https://github.com/Gentleman-Programming/agent-teams-lite/blob/main/docs/token-economics.md) — Real-world measurement: delegation breaks even at ~8 files per task, 38% overhead optimization examples
- [SDD: Use subagents in spec-driven workflow for context isolation — Google Gemini CLI (Issue #17691, 2026)](https://github.com/google-gemini/gemini-cli/issues/17691) — Google's SDD subagent pattern for spec planning, implementation, and review phases
- [agent-teams-lite SDD phase documentation — Gentleman-Programming (GitHub)](https://github.com/Gentleman-Programming/agent-teams-lite/tree/main/skills/sdd-tasks) — Skill registry protocol, compact rules injection, artifact persistence across phases

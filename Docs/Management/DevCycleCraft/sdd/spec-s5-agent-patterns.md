# S5 — Agent Patterns

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Agent patterns describe how AI agents are structured, divided, and coordinated when executing a spec. In Spec-Driven Development, no single agent is expected to hold the full project in context, perform all reasoning, and verify its own output simultaneously. As codebases grow beyond a few files and tasks beyond a single session, the question is not whether to use multiple agents but which pattern applies to each situation.

Three patterns dominate SDD practice in 2025–2026:

1. **Adversarial Agent Pattern** (S5.1) — opposing roles with explicit separation of creation and critique, preventing the self-validation trap
2. **Parallel Agent Execution** (S5.2) — multiple agents working simultaneously on independent tasks to reduce latency and increase throughput
3. **Subagent Delegation** (S5.3) — a main agent delegates scoped work to child agents, each operating in its own context window

Each pattern has a corresponding class of failure. S5.x subtopics document those failure modes.

---

## S5.1 — Adversarial Agent Pattern

### What it is

The adversarial agent pattern structures multi-agent work into at minimum two opposing roles: one that produces and one that challenges. The core mechanism is that the agent performing verification does not share the history or context of the agent that performed implementation. This separation is the pattern's essential property — an agent cannot critically evaluate decisions it was party to making.

The pattern has converged on a canonical three-role form, widely referred to as the **Coordinator–Implementor–Verifier (CIV)** architecture:

- **Coordinator** — decomposes the specification into a dependency-ordered task plan (often represented as a DAG), delegates to Implementors, and manages replanning when a subtask fails. Uses the most capable model because planning quality determines downstream output quality.
- **Implementor** — receives a single scoped subtask and executes it within a bounded context. Operates under a per-subtask retry cap (VeriMAP defaults to 3 attempts). Uses structured output contracts keyed by name so Coordinator can merge results for downstream tasks. Can be routed to a cheaper model since reasoning scope is narrower.
- **Verifier** — receives the Implementor's output and the original specification and validates one against the other, producing a pass/fail verdict plus structured feedback. Does not have access to the Implementor's reasoning process — only the artifacts. Spec-based verification catches integration issues that standard code review misses because it evaluates against the full system specification rather than isolated diffs.

A variant described by AgentPatterns.ai adds a true context-reset adversary that receives no prior conversation history, explicitly designed to attack specs, tests, and implementation without accumulated investment in them. The reset is intentional: an adversary that saw the Builder's reasoning cannot escape the Builder's assumptions. Convergence is reached not by iteration count but by a qualitative signal — when the adversary can only raise hypothetical problems rather than substantive behavioral gaps.

ASDLC.io documents a parallel critic lanes variant in which multiple critic agents (Architect, SecOps, QA) run simultaneously against a Builder's output, with a Moderator synthesizing their findings into a single de-duplicated directive for the Builder. The key constraint: critics are gated to PASS or structured violation lists; they do not generate alternative implementations.

### Why it matters for SDD

Without adversarial separation, an agent asked to both implement and verify will confirm its own work. A single agent reviewing its own code carries the same confirmation bias at both steps — it makes the same assumptions during review that it made during implementation. The adversarial pattern breaks this echo chamber structurally, not through prompting.

The production implication: verifier feedback should feed back into the Coordinator's replanning context, not directly back to the Implementor. This preserves the Verifier's independence across iterations.

---

## S5.1.1 — Persona/Role Confusion

### What it is

Persona/role confusion occurs when an agent that was operating in one role (e.g., Implementor) begins reasoning from a different role's perspective (e.g., Coordinator or Verifier) mid-session, or when a single session attempts to hold multiple roles simultaneously.

### How it manifests

In practice, persona/role confusion takes several forms:

- An Implementor that was given a scoped subtask begins expanding scope because it starts reasoning about system-wide implications that belong to the Coordinator.
- A Verifier that accumulates context from multiple Implementor outputs starts producing implementation suggestions rather than pass/fail verdicts.
- A Coordinator that has been running for many iterations begins micro-managing implementation details instead of maintaining the high-level plan.

The underlying cause is context accumulation. As an agent's context window grows during a session, the boundaries between roles — which are defined by system prompt and initial briefing — erode as the model attempts to be helpful across the full visible context. A fresh Verifier starts with a clean adversarial stance; a Verifier that has been running across ten subtasks has accumulated enough Implementor context to rationalize instead of challenge.

### Mitigation

The primary mitigation is session isolation: Verifier and adversary roles should receive fresh context for each review pass. The Claude Code subagent system enforces this structurally — each subagent starts in its own context window with no inherited history. Custom subagent definitions (`.claude/agents/`) can enforce role separation by specifying system prompts that constrain the agent to a single role.

In the CLAUDE.md + workflow.md pattern used by MyVocaList, the equivalent mitigation is the rule that subagents are never reused across tasks and that the main agent performs shell-only operations while subagents handle all file writes — preventing the main agent from accumulating implementation-role context.

---

## S5.2 — Parallel Agent Execution

### What it is

Parallel agent execution runs multiple agents simultaneously on independent tasks, reducing total elapsed time compared to sequential execution. The pattern applies whenever tasks have no shared file writes and no output dependency between them.

The primary measured benefit is throughput. Augment Code's multi-agent research documents approximately 1.3x speed improvements through parallel agent execution in code generation tasks. ClaudeLab practitioners report a CI-equivalent pipeline reduction from 30 minutes sequential to 8 minutes parallel with three agents.

Claude Code implements parallelism through two mechanisms:

- **Subagents (Task tool)** — child agents spawned within a single session. Each runs in its own isolated context window. Results return as text to the parent, which synthesizes them. No peer-to-peer communication between subagents. Communication always routes through the orchestrator.
- **Agent teams** — fully independent Claude Code instances coordinating through a shared task list and direct agent-to-agent messaging. Higher token cost but supports teammates challenging each other's conclusions directly.

The canonical parallelism constraint from MyVocaList's workflow.md is a hard cap of **4 subagents running in parallel at any one time**, dispatched in waves. A 5th concurrent agent is not spawned; work is staggered to the next wave.

### Wave-based execution

Google ADK's developer guide and the boraoztunc agent-system both independently document wave-based execution as the correct default for parallel multi-agent work:

1. Coordinator decomposes the spec into tasks and identifies dependencies.
2. Tasks with no dependencies on each other are grouped into a wave.
3. All agents in the wave are spawned simultaneously.
4. The Coordinator waits for the full wave to complete before starting the next wave.
5. Tasks in wave N+1 consume outputs from wave N.

This structure allows maximum parallelism within each wave while respecting inter-task dependencies across waves.

### When parallelism is valid

Parallelism is safe only when both conditions hold:
- Tasks write to **different files** (no write-write collisions)
- Tasks have **no output dependency** (task B does not consume task A's output)

The `isolation: worktree` option in Claude Code subagents enforces the first condition by giving each subagent its own git worktree copy of the repository. When tasks share files, they must be executed sequentially regardless of the performance cost.

---

## S5.2.1 — Dependency Ordering Fragility

### What it is

Dependency ordering fragility describes the failure mode where parallel task sequencing that appeared valid at planning time breaks at execution time because a hidden inter-task dependency was not captured in the task plan.

### How it manifests

At plan time, the Coordinator identifies tasks A, B, and C as independent and dispatches them in parallel. At execution time, Implementor B discovers that it needs the output of task A — a type definition, a schema migration, or a shared constant — that the Coordinator's analysis did not surface as a dependency edge.

The consequences are:
- B cannot complete, fails, or produces incorrect output that passes local validation but breaks integration.
- B may proceed with a wrong assumption, producing code that is locally consistent but globally broken.
- The Coordinator's retry budget is consumed on a failure that can only be resolved by re-sequencing, not by retrying the same task.

Hidden dependencies are especially common with:
- **Shared interfaces and types** — an Implementor adding a method to an interface while a parallel Implementor consumes that interface without the new method.
- **Database schema changes** — a migration task whose output is needed by a query generation task in the same wave.
- **Configuration files** — two Implementors editing the same DI registration file or global settings with no write collision prevention.

### Mitigation

The primary mitigation is Coordinator analysis quality: the planning step must explicitly enumerate all shared artifacts (types, interfaces, migrations, DI registrations, configuration files) before assigning tasks to waves. Augment Code's Context Engine approach uses a full dependency graph across repositories to surface these edges before planning.

A secondary mitigation is conservative wave sizing: tasks whose dependencies are uncertain should be promoted to their own wave rather than parallelized speculatively. The performance cost of one extra sequential wave is less expensive than a failed Implementor consuming retry budget.

---

## S5.2.2 — Cross-Agent Spec Conflicts

### What it is

Cross-agent spec conflicts occur when two agents working on interdependent tasks produce outputs that are locally correct against their individual task specs but globally contradictory when integrated.

### How it manifests

Agent A is implementing service X and defines a DTO with fields `{id, name, status}`. Agent B, working simultaneously on service Y that consumes service X, writes code expecting `{id, title, active}` — a different field naming convention derived from a different reading of the same or an ambiguous part of the spec. Both agents pass their local Verifier. Integration fails.

This failure mode is structurally distinct from dependency ordering fragility: both agents complete successfully; the conflict only surfaces at integration. The Verifier, which validates each agent's output against the spec independently, cannot catch cross-agent semantic contradictions unless the spec itself defines the shared contract with enough precision.

The Augment Code CIV documentation identifies this as a core motivation for the living spec mechanism: when Agent A updates the spec to reflect what it actually built (field names, interface signatures, error codes), Agent B reads the updated spec and either adapts its output or flags a conflict before producing broken code.

### Mitigation

Three mitigations apply:

1. **Shared contracts defined in the spec before implementation begins.** DTOs, API contracts, error code enumerations, and event schemas must be specified in the shared spec, not left to each agent's judgment. Agents implement against the spec; they do not infer contracts from each other.
2. **Living spec updates as implementation progresses.** When an Implementor makes a spec-level decision (e.g., choosing a field name not explicitly specified), that decision is written back to the spec before parallel agents consume the same interface.
3. **Verifier validates cross-service contracts, not just local output.** The Verifier's job is to check against the full system specification. A Verifier limited to a local diff will pass locally-correct code that breaks integration.

---

## S5.3 — Subagent Delegation

### What it is

Subagent delegation is the pattern in which a main agent (orchestrator) retains control of high-level coordination and delegates all file creation and modification to child agents (subagents), each operating in its own isolated context window.

The structural property is separation of concerns between planning and execution. The main agent:
- Reads the spec before briefing subagents
- Performs shell-only operations (build, test, migrations, git)
- Synthesizes subagent results
- Decides whether to proceed or replan

Subagents:
- Receive a scoped task with explicit file paths and constraints
- Execute all file writes for that task
- Return a status signal (done/fail) to the main agent
- Are discarded after task completion — never reused

Claude Code implements this through the Task tool, which spawns subagents with their own context windows, tool access, and permissions. The official Claude Code documentation describes custom subagents defined in `.claude/agents/` as reusable specialist roles that the main agent delegates to automatically when a task matches the subagent's description.

### MyVocaList's implementation

MyVocaList's workflow.md codifies a strict variant of subagent delegation:

- Hard cap: maximum 4 subagents in parallel per wave
- Briefing protocol: subagents receive file paths only, never inline content — the subagent reads its own files, preventing token cost multiplication across parallel agents
- Return protocol: subagents communicate completion only by updating `Docs/task-log.md` (done/fail + one-line reason), committing, pushing, and stopping — no summaries or diffs returned to the caller
- Exit checklist: every subagent must build (0 errors) → commit → push before stopping

This pattern prevents context bloat in the main agent, maintains audit trails in task-log.md, and ensures subagents cannot accumulate role confusion across sessions.

---

## S5.3.1 — Silent Task Completion

### What it is

Silent task completion is the failure mode in which a subagent marks a task as done without actually executing all required verification steps. The subagent reports success; the main agent proceeds; the actual work is incomplete or incorrect.

### How it manifests

Silent task completion takes two forms:

**Form 1 — Verification step skipped.** The subagent was instructed to build, run tests, and commit. It writes the files and commits, but does not run the build or tests. The task-log entry reads `done`. The main agent proceeds to the next wave. The broken build surfaces only when the main agent runs `dotnet build` — if it does.

**Form 2 — Partial completion reported as full.** The subagent completes 3 of 5 instructed file edits, encounters a difficulty on the 4th, and reports `done` rather than `fail`. The missing edits are not visible to the main agent until integration.

Both forms are enabled by the same structural gap: the main agent cannot observe what the subagent actually did — only what the subagent reports. Unlike a test suite that objectively measures correctness, task completion status is a self-reported signal.

### Contributing factors

- **Ambiguous task scope.** If the task description does not enumerate specific files and acceptance criteria, a subagent can apply judgment about what counts as "done."
- **Missing build gate.** Without a mandatory build verification step in the subagent's instructions, the build gate is only enforced if the subagent chooses to run it.
- **No structured output requirement.** Subagents that return freeform status messages instead of structured pass/fail signals allow reporting ambiguity.

### Mitigation

Four mitigations reduce silent task completion risk:

1. **Explicit exit checklist in every subagent briefing.** The subagent instructions must state: build (0 errors) → tests pass → commit → push. Not "make sure it builds" but the literal command sequence, in order, as a mandatory prerequisite for reporting `done`.
2. **Main agent verification after each wave.** The main agent runs `dotnet build` and `dotnet test` after each wave completes, independently of the subagent's self-report. If these fail, the subagent's `done` status is overridden.
3. **Structured status reporting.** The task-log format enforces a `done`/`fail` binary with a one-line reason, discouraging ambiguous "completed most of it" entries.
4. **Automated hooks.** MyVocaList's Stop hook warns if uncommitted changes remain when a session ends. This catches subagents that completed file writes but did not commit — a common indicator of incomplete task execution.

---

## Sources

### Tier 1 — Primary Sources

- [Spec-Driven AI Code Generation With Multi-Agent Systems — Augment Code](https://www.augmentcode.com/guides/spec-driven-ai-code-generation-with-multi-agent-systems) — Coordinator-Implementor-Verifier architecture, parallel execution in isolated git worktrees, spec-based verification
- [Coordinator-Implementor-Verifier Pattern for Dev Teams — Augment Code](https://www.augmentcode.com/guides/coordinator-implementor-verifier) — Detailed CIV definition, DAG-based task planning, VeriMAP retry caps, model routing strategy
- [How and when to use subagents in Claude Code — Anthropic (claude.com)](https://claude.com/blog/subagents-in-claude-code) — Official Anthropic description of subagent isolation, context windows, parallel execution, and custom subagent definitions
- [Create custom subagents — Claude Code Docs](https://code.claude.com/docs/en/sub-agents) — Claude Code subagent mechanics, Task tool, isolation: worktree, fork vs named subagent
- [Orchestrate teams of Claude Code sessions — Claude Code Docs](https://code.claude.com/docs/en/agent-teams) — Agent teams vs subagents comparison, shared task list, direct agent-to-agent messaging

### Tier 2 — Secondary Sources

- [Adversarial Multi-Model Development Pipeline (VSDD) — AgentPatterns.ai](https://agentpatterns.ai/multi-agent/adversarial-multi-model-pipeline/) — Builder/Adversary context-reset pattern, convergence signal definition, six-phase adversarial pipeline
- [Adversarial Code Review — ASDLC.io](https://asdlc.io/patterns/adversarial-code-review/) — Critic lanes, Moderator synthesis, Context Gate framing, Builder/Critic model profile guidance
- [Developer's guide to multi-agent patterns in ADK — Google Developers Blog](https://developers.googleblog.com/developers-guide-to-multi-agent-patterns-in-adk/) — Generator and Critic pattern, Parallel Fan-Out/Gather, sequential pipeline, state isolation guidance
- [Swarm vs. Supervisor: Multi-Agent Architecture Guide — Augment Code](https://www.augmentcode.com/guides/swarm-vs-supervisor) — Supervisor vs swarm tradeoffs, nested supervisor patterns, Intent architecture description
- [Orchestrator for Implementor and Review Loop — Fazm Blog](https://fazm.ai/blog/orchestrator-implementor-review-loop-ai-agents) — Filesystem-based inter-agent communication, shared markdown task file, three-iteration convergence rule

### Tier 3 — Tertiary Sources

- [Claude Code Multi-Agent Parallel Execution Guide — ClaudeLab](https://claudelab.net/en/articles/claude-code/claude-code-multi-agent-subagent-parallel-guide) — Practical 3-agent parallel CI example with 30-to-8-minute reduction, structured JSON output pattern
- [How to Use Claude Code Subagents to Parallelize Development — Zach Wills](https://zachwills.net/how-to-use-claude-code-subagents-to-parallelize-development/) — Product-manager/UX-designer/engineer parallel trio pattern, sequential handoff assembly line
- [Lesson 15: Parallelization — Mastering Claude](https://masteringclaude.com/learn/15-parallelization) — Three-tier parallelism taxonomy (subagents, agent teams, headless), built-in Claude Code agent types
- [agent-system — boraoztunc (GitHub)](https://github.com/boraoztunc/agent-system) — Seven-agent system with explicit Coordinator/Implementor/Verifier separation, wave-based parallelism, "spec first, always" constraint
- [Actor-Critic Adversarial Coding — Understanding Data](https://understandingdata.com/posts/actor-critic-adversarial-coding/) — 3–5 round convergence data, dual-agent implementation patterns, stopping criteria design
- [Specification-Driven Agent Development — Agentic Patterns](https://agentic-patterns.com/patterns/specification-driven-agent-development) — Spec/Exposure/Task Delta framework, tiered review with git worktrees

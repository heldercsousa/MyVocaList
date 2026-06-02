# S5.1 — Adversarial Agent Pattern

**Status:** Researched
**Predecessor(s) ID:** S5

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content researched and written |

---

## Overview

The adversarial agent pattern structures multi-agent work into at least two opposing roles: one that produces and one that challenges. The defining mechanism is that the agent performing verification does not share the history or context of the agent that performed implementation. This separation is the pattern's essential property — an agent cannot critically evaluate decisions it was party to making.

The pattern has converged on a canonical three-role form: **Coordinator–Implementor–Verifier (CIV)** architecture, with variants that add parallel critic lanes, context resets, and domain-split reviewers. The core principle remains consistent: opposing incentives prevent the self-validation trap where a single agent confirms its own work.

---

## The Coordinator–Implementor–Verifier (CIV) Architecture

### Structure

The CIV pattern decomposes multi-agent work into three roles with isolated execution contexts:

**Coordinator** — Transforms the specification into a directed acyclic graph (DAG) where each node is a bounded subtask and each edge is a dependency. The Coordinator decomposes the spec into a dependency-ordered task plan, delegates to Implementors, and manages replanning when a subtask fails. Uses the strongest available model because planning quality determines downstream output quality. Maintains the outer control loop: plan → execute → verify → replan.

**Implementor** — Receives a single scoped subtask and executes it within a bounded context window, under two hard constraints: (1) a per-subtask retry cap (VeriMAP defaults to 3 attempts), and (2) a structured output contract keyed by name so the Coordinator can merge results for downstream tasks. Because Implementor scope is narrower than the Coordinator's, Implementor work can be routed to cheaper or faster models while reserving the strongest model for planning. Runs the inner ReAct-style reason-act-observe cycle within its isolated context.

**Verifier** — Receives the Implementor's output and the original specification and validates one against the other, producing a pass/fail verdict plus structured feedback. Does **not** have access to the Implementor's reasoning process — only the artifacts. Spec-based verification catches integration issues that standard code review misses because it evaluates against the full system specification rather than isolated diffs. Verification feedback feeds back into the Coordinator's retry context, preserving the Verifier's independence across iterations.

### Context Isolation

The essential property is that Verifier and Implementor operate in separate context windows with no inherited history from each other. An Implementor that accumulates context across ten subtasks develops investment in its own decisions. A fresh Verifier, by contrast, starts with a clean adversarial stance. This isolation can be enforced by:

- Structured prompting that limits each role to its domain
- Session boundaries — each role starts in its own context window
- Tool access control — each role has access only to artifacts it needs, not the other role's reasoning
- Architectural enforcement in agent systems like Claude Code subagents (each gets its own isolated context)

### Control Loop Structure

The CIV architecture implements two nested control loops:

1. **Inner loop** (ReAct, within Implementor): reason-act-observe on a single bounded subtask
2. **Outer loop** (spans Coordinator and Verifier): plan-execute-verify-replan across agent boundaries, using structured data contracts at each handoff

A flat single-agent ReAct system has only the inner loop. When it produces wrong output, no mechanism above exists to detect the failure, revise the plan, or route a corrected subtask to a fresh context. The outer loop is the adversarial mechanism.

---

## Adversarial Variants

### Builder–Adversary with Context Reset (AgentPatterns.ai)

A variant described by AgentPatterns.ai places the context reset at the center of the design. The architecture consists of:

- **Builder** — owns spec authorship, test generation, and code implementation. Accumulates context across phases and can develop confirmation bias toward its own decisions.
- **Adversary** — receives a context reset between each review pass. Attacks specs, tests, and implementation with no prior investment in them. The context reset is the mechanism: the adversary cannot rationalize decisions it did not make.

The loop exits when the Adversary's findings shift from genuine to invented. Convergence is signaled qualitatively:

- Spec critiques become stylistic nitpicks, not substantive behavioral gaps
- The Adversary cannot identify untested scenarios; mutation testing kill rates are high
- Implementation findings require the Adversary to invent implausible inputs, not observe actual flaws
- All formal properties pass proof; fuzzing finds nothing new

Experimentally documented convergence typically occurs within 3–5 rounds, at which point diminishing returns set in.

### Critic Lanes with Moderator (ASDLC.io)

A parallel critic variant runs multiple critic agents simultaneously against a Builder's output, with a Moderator synthesizing their findings:

- **Builder Agent** — Optimized for implementation throughput. Generates code from the spec.
- **Critic Lanes** — Multiple independent agents, each optimized for a specific validation dimension (Architect, SecOps, QA). Run in parallel. Each produces either PASS or a structured violation list.
- **Moderator** — Synthesizes critic findings into a single de-duplicated directive. Prevents alert fatigue and conflicting feedback. Acts as deduplication and prioritization layer.

Key constraint: Critics do not generate alternative implementations. They act as gatekeepers, producing only verdicts and violation lists. The Moderator is the only agent authorized to synthesize and prioritize.

This pattern is particularly effective for high-risk changes (security, architecture) where multiple independent perspectives are needed, and where the cost of parallel review is justified by risk reduction.

### Actor–Critic Iterative Loop

The actor-critic pattern simplifies the CIV architecture into a two-agent loop:

1. **Actor** — Generates or refactors code based on the task and any prior feedback
2. **Critic** — Reviews the code against acceptance criteria, produces structured feedback (critical issues, warnings, minor notes)
3. **Loop** — Actor refactors based on feedback; Critic re-reviews. Repeat until Critic approves or max iterations reached

Experimental data shows 3–5 rounds eliminate 90%+ of issues that would otherwise reach human code review, reducing subsequent review cycles from 3–5 to 1–2. The pattern is particularly effective for deterministic, well-specified work (API integrations, CRUD services, data transformations) where correctness can be evaluated objectively through tests.

---

## Why Adversarial Separation Matters

### The Self-Validation Trap

Without adversarial separation, an agent asked to both implement and verify will confirm its own work. A single agent reviewing its own code carries the same confirmation bias at both steps — it makes the same assumptions during review that it made during implementation. The adversarial pattern breaks this echo chamber structurally, not through prompting.

This is not a problem that better instructions can solve. Confirmation bias in humans is well-documented; confirmation bias in LLM-based agents is equally robust. The mechanism that works is architectural separation: an agent cannot defend decisions it was not party to making.

### Specification Compliance Verification

Verifier-based approaches catch integration issues that standard code review misses because the Verifier evaluates against the full system specification rather than isolated diffs. When Agent A produces code locally correct against task A's subtask spec but incompatible with Agent B's output (different field names, interface contracts, error codes), a Verifier reading the full spec can flag this. A self-reviewing Agent A would not.

### Multi-Agent Coordination Under Specification Gaps

In systems where specs leave certain design choices implicit, adversarial review surfaces coordination failures early. arXiv:2603.24284 documents a persistent specification gap: two-agent integration accuracy drops from 58% to 25% as spec detail is removed, while a single-agent baseline degrades more gracefully (89% to 56%). The gap is due to agents making independent, incompatible choices about internal representations. A Verifier reading a complete spec can detect these collisions before they cascade.

---

## Implementation Patterns

### CIV in Claude Code

Claude Code subagents provide structural support for CIV:

- **Coordinator** — Main agent, orchestrates task decomposition, reads specs, briefs subagents
- **Implementor** — Subagent with isolated context, executes a single bounded task, returns structured output (artifact names, status, feedback)
- **Verifier** — Optional second subagent or main agent role, validates output against spec before proceeding

The per-subagent context isolation is structural; no special prompting required.

### Filesystem-Based Coordination

For agents without native subagent support, a shared task file can coordinate Coordinator, Implementor, and Verifier:

1. Coordinator writes a task specification to a shared markdown file
2. Implementor reads the spec, writes code, updates the file with implementation notes
3. Verifier reads both code and spec, writes verification feedback back to the same file
4. If issues flagged, Coordinator sends Implementor back to fix them
5. Loop continues until Verifier approves or max iterations reached

No complex message passing or APIs required. Files are the natural interface for code-based work.

### Model Routing

The CIV pattern supports cost-effective model routing:

- **Coordinator** — Strong reasoning model (Claude Opus 4.5, if available) because planning quality determines all downstream quality
- **Implementor** — Faster model (Claude Haiku 4.5) because scope is narrower and reasoning is more local; throughput matters
- **Verifier** — High-reasoning model optimized for logic evaluation and edge case discovery (Claude Sonnet, if Opus unavailable)

This routing reduces token cost while preserving quality in the highest-impact role (planning).

---

## Failure Modes and Mitigations

### Persona/Role Confusion (S5.1.1)

Persona/role confusion occurs when an agent operating in one role begins reasoning from another role's perspective mid-session, or when a single session attempts to hold multiple roles simultaneously.

**Manifestations:**

- An Implementor expands scope because it starts reasoning about system-wide implications that belong to the Coordinator
- A Verifier that accumulates context from multiple Implementor outputs starts producing implementation suggestions rather than pass/fail verdicts
- A Coordinator that has been running for many iterations begins micro-managing implementation details instead of maintaining the high-level plan

The underlying cause is context accumulation. As an agent's context window grows during a session, the boundaries between roles — defined by system prompt and initial briefing — erode as the model attempts to be helpful across the full visible context.

**Mitigation:**

- Session isolation: Verifier and adversary roles should receive fresh context for each review pass
- Structural enforcement: custom subagent definitions can specify system prompts that constrain the agent to a single role
- In MyVocaList's pattern: the rule that subagents are never reused across tasks prevents the main agent from accumulating implementation-role context

### Convergence Stalling with Weak Adversary Prompts

If the Adversary role receives an under-specified prompt, it defaults to surface-level stylistic feedback rather than substantive behavioral attacks. Phases then cycle without meaningful signal, producing the illusion of convergence rather than the reality.

**Mitigation:**

- Define clear, orthogonal evaluation dimensions for each critic/adversary (architecture, correctness, security, test coverage)
- Require structured output (JSON with verdict and issues) rather than prose, forcing decision clarity
- Tag findings as "substantive" or "hypothetical" and track the ratio across rounds; stop when hypothetical findings dominate

### Specification Gaps Enable Coordination Failure

Missing shared contracts (DTOs, API boundaries, error codes, event schemas) force each agent to infer its own interpretation. Even with good adversarial review, agents working on different specs may produce locally correct but globally incompatible code.

**Mitigation:**

- Shared contracts must be defined in the spec before implementation begins, not left to agent judgment
- Living spec updates as implementation progresses — when an Implementor makes a spec-level decision, update the shared spec
- Verifier validates cross-service contracts, not just local output

---

## Sources

### Tier 1 — Primary Sources

- [Coordinator-Implementor-Verifier Pattern for Dev Teams — Augment Code](https://www.augmentcode.com/guides/coordinator-implementor-verifier) — CIV definition, DAG task planning, VeriMAP retry caps, model routing strategy
- [Adversarial Code Review — ASDLC.io](https://asdlc.io/patterns/adversarial-code-review/) — Builder/Critic lanes, Moderator synthesis, specialized reviewer personas, context gates
- [Adversarial Multi-Model Development Pipeline (VSDD) — AgentPatterns.ai](https://agentpatterns.ai/multi-agent/adversarial-multi-model-pipeline/) — Builder/Adversary context-reset pattern, convergence signaling, six-phase pipeline
- [Orchestrator for Implementor and Review Loop — Fazm Blog](https://fazm.ai/blog/orchestrator-implementor-review-loop-ai-agents) — Filesystem-based coordination, three-iteration sweet spot, verification as gatekeeper
- [Actor-Critic Adversarial Coding — Understanding Data](https://understandingdata.com/posts/actor-critic-adversarial-coding/) — Two-agent actor-critic loop, 3–5 round convergence, production quality lift documentation

### Tier 2 — Secondary Sources

- [Committee Review Pattern for Multi-Agent Code Review — AgentPatterns.ai](https://agentpatterns.ai/code-review/committee-review-pattern/) — Multiple specialized reviewers, domain-split critique, orchestrator aggregation
- [Generator-Evaluator — Encyclopedia of Agentic Coding Patterns](https://aipatternbook.com/generator-evaluator) — Separated generation and evaluation with independent context windows, structured feedback requirement, concrete grading criteria
- [Why AI Coding Agents Still Need Clear Specs — Markus Eisele](https://www.the-main-thread.com/p/spec-trap-agent-work) — Specification as coordination contract, handoff quality, multi-agent breakeven point calculation

### Tier 3 — Tertiary / Complementary Sources

- [The Specification Gap: Coordination Failure Under Partial Knowledge in Code Agents — arXiv:2603.24284](https://arxiv.org/abs/2603.24284v1) — Empirical data on spec completeness impact on multi-agent coordination, 25–39 percentage point gaps, single-agent baseline resilience
- [Spec-Driven Architecture — INNOQ](http://www.innoq.com/en/blog/2026/02/spec-driven-architecture-contracts-fuer-agenten/) — Architecture contracts as gatekeeper skill, dual advisor/auditor role
- [opencode-agents — GitHub](https://github.com/wildwasser/opencode-agents) — Oscar/Scout/Ivan/Jester pattern, context efficiency, parallel critic consensus for high-stakes decisions
- [When Multi-Agent Is Overkill — Augment Code](https://www.augmentcode.com/guides/when-multi-agent-ai-is-overkill) — Spec quality as single largest predictor of multi-agent success, role-based separation as structural requirement

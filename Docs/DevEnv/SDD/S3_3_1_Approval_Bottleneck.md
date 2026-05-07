# S3.3.1 — Approval Bottleneck

**Status:** Researched
**Predecessor(s) ID:** S3.3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; approval bottleneck dynamics documented |

---

## Overview

The approval bottleneck is a structural consequence of human-in-the-loop (HITL) architecture in SDD workflows. When AI agents generate code faster than humans can review it, synchronous approval gates become the limiting factor on pipeline throughput. A single unavailable reviewer or a queue of pending approvals can halt all subsequent tasks, making the human gate the critical resource constraint — the bottleneck.

Unlike most engineering bottlenecks (which can be solved by adding more hardware), the approval bottleneck is fundamentally biological. No amount of compute increases the number of qualified reviewers available in a given hour, or the cognitive speed at which they can process complex diffs and design decisions.

---

## The Core Problem: Synchronous Gates and Queue Dynamics

### Synchronous Bottleneck Pattern

In a typical SDD workflow with synchronous review gates:

```
Task 1 completes → Awaits human approval → (reviewer unavailable or backlogged)
Task 2 blocked (cannot start until Task 1 approved)
Task 3 blocked (cannot start until Task 2 approved)
Task 4 blocked (cannot start until Task 3 approved)
```

The entire pipeline halts. Tasks completed by agents are not integrated. Code sits on branches unmerged. Parallel work cannot proceed because dependencies are unknown (the spec hasn't been approved yet). Even when multiple reviewers are available, the approval queue becomes the critical path.

**Why this matters for SDD:** In traditional development, code review is the last gate before merge. In SDD, specs are reviewed before any code is generated. But implementation gates — verifying that code conforms to approved specs — still require human review, and they cannot be skipped. This shifts the bottleneck from a tertiary concern (code review) to a primary one (implementation approval), often with no pre-existing infrastructure to manage it.

### Queue Theory Applied to HITL

The research reveals that HITL systems are queues, subject to standard queueing dynamics (Tian Pan, 2026):

- **Arrival rate** — the rate at which agents generate approvable tasks
- **Service rate** — the rate at which reviewers can approve them
- **Queue depth** — tasks waiting for approval at any given moment
- **Tail latency** — how long the oldest item has been waiting

If arrival rate (agent output) exceeds service rate (human capacity), the queue grows indefinitely. Backlog accumulates. Review latency increases. Decisions become stale — the context the reviewer needs to make a judgment was true when the task completed, but may no longer be true hours later when the reviewer finally gets to it.

**The arithmetic:** If an agent system generates 1,200 approval-eligible actions per day and average time-to-decision is 4 hours, steady-state backlog is ~200 items in-flight. If arrival rate triples (a natural progression as agent deployment scales), either backlog explodes or decision time collapses — there is no third option (Tian Pan, 2026).

---

## Failure Modes and Consequences

### 1. Reviewer Fatigue and Rubber-Stamping

When approval queues grow, reviewers experience decision fatigue. Approvals that once received careful attention become routine. The review process degrades from gate to rubber-stamp.

Evidence: "If an agent system generates 1,200 approval-eligible actions per day... the deepest part of the backlog is precisely where reviewers are most fatigued, decisions matter least, and the items most likely to be approved without reading" (Tian Pan, 2026).

This is a security and correctness failure. The bottleneck gate, in trying to prevent bad code from shipping, becomes complicit in shipping bad code by making the approval process mechanical.

### 2. Staleness and Context Loss

Approvals lose decision-relevant information over time. A reviewer looking at a task three hours later may not remember the context — the requirements that led to this implementation, the alternatives considered, the integration constraints.

Staleness creates another failure mode: the approval becomes meaningless. The reviewer is approving a diff, but the diff may no longer be relevant if the broader feature context has changed (other tasks completed, requirements shifted, upstream APIs changed).

### 3. Single-Point-of-Failure Risk

When approval authority is centralized in one reviewer (the architect, the PM, the tech lead), that person becomes the critical resource. Vacation, illness, or simply being overwhelmed creates a hard stop for the entire pipeline.

Even with rotation-based review, a single unavailable reviewer can block all parallel tasks in that approval category. There is no graceful degradation.

### 4. Priority Inversion

HITL queues are often FIFO (first-in-first-out) — a reasonable default for fairness. But in SDD workflows, not all tasks have equal priority or risk. A simple documentation update should not block a critical security fix. A low-risk CRUD operation should not hold up a high-impact architectural change in the review queue.

FIFO queues create priority inversion: high-context, high-value decisions sit blocked behind low-risk, low-effort approvals (Tian Pan, 2026).

---

## Mitigation Strategies

### Strategy 1: Asynchronous Approval Windows

Instead of synchronous review-on-demand, define specific review hours. Agents are notified when the next review window is and can schedule completion for that window.

**Benefit:** Batches reviews together, reduces context switching, allows reviewers to prepare and focus.

**Trade-off:** Introduces latency up to the next review window. Not suitable for real-time domains but acceptable for SDD (where latency from review to merge is on the order of hours, not milliseconds).

**Implementation:** From MyVocaList CLAUDE.md: escalation policies define review SLAs (e.g., low-risk tasks auto-approve after 2 hours, medium-risk tasks require explicit approval within 4 hours).

### Strategy 2: Risk-Tiered Approval Lanes

Not all changes require the same level of review. Classify tasks by risk:

- **Low-risk (Auto-Approve Lane):** Pure documentation, typo fixes, well-tested CRUD operations with no schema changes, formatting — bypass human review, only require automated gates
- **Medium-risk (Monitored Lane):** Feature additions with moderate scope, database queries on proven patterns — require explicit approval within SLA, but not escalated if delayed
- **High-risk (Explicit Approval Lane):** Schema changes, security-sensitive code, architectural decisions, breaking changes — require explicit approval immediately, block indefinitely until approved

**Benefit:** Reduces load on reviewers by 70–80% (most tasks are low-risk), preserves careful attention for decisions that matter.

**Evidence:** Auto-approval rates of 70% initial target are standard; teams that go higher (>95%) often miss edge cases because the gate is too permissive (AiOps School, 2026).

### Strategy 3: Confidence-Gated Bypass

Instead of classifying by task type, classify by agent confidence. The agent outputs a confidence score for each task. High-confidence tasks auto-approve; medium and low-confidence tasks route to human review.

**Benefit:** Lets the agent self-gate. Tasks the agent is uncertain about get scrutiny; routine tasks the agent is confident about move automatically.

**Challenge:** Confidence signals can be calibrated poorly. An agent that is overconfident about flawed code produces false confidence in the approval process.

**Mitigation:** Monitor held-out error rates on the auto-approve lane. If auto-approved items later require fixes, retrain or lower the confidence threshold.

**Evidence:** Uplatz and Turing demonstrate this pattern at scale — confidence-aware routing reduced review load by 60% (Turing) while maintaining 90% expert agreement on auto-assessed submissions.

### Strategy 4: Parallel Reviewers and Escalation

Assign multiple reviewers per task category. A change is approved once ANY assigned reviewer approves. If the primary reviewer is unavailable, escalate to a named deputy.

**Benefit:** Removes single-point-of-failure. Reduces approval latency by distributing load.

**Implementation:** Requires explicit approval RACI matrix (Responsible, Accountable, Consulted, Informed) per change type, as documented in S3.3.

### Strategy 5: Async Mobile Approval

Keep approval gates active (for safety) but route approval requests to the human's phone, not just the terminal. Reviewers can approve/reject from anywhere — no need to watch the terminal or return to the office.

**Benefit:** Makes unattended agent runs practical. Agents can run overnight or in parallel; approvals are handled asynchronously via mobile notifications.

**Evidence:** Code on Grass (2026) demonstrates this pattern with sub-2-second round-trip approval, allowing agents to remain active without terminal blocking.

### Strategy 6: Approval Checkpoint Serialization

When the agent hits a required approval gate, it:
1. Serializes its full state (conversation history, plan, pending action) to durable storage
2. Notifies the reviewer with enough context to decide
3. Terminates its execution (frees the thread/session)
4. When approved/rejected, a new execution resumes from the checkpoint

**Benefit:** Decouples agent execution from approval latency. The agent isn't blocked; it's simply paused. Other tasks can run in parallel.

**Evidence:** This pattern is standard in autonomous approval systems (Tian Pan, 2026; Cordum, 2026).

### Strategy 7: Specification-Level Review, Not Code-Level Review

The highest-leverage mitigation: shift review from code to specs.

In mature SDD (Spec-as-Source), the primary review gate is approving the spec. Once the spec is approved, code generation is deterministic. Verification gates check mechanical properties (tests pass, architecture compliance, scope boundaries). Human review becomes validation of the spec and spot-checks of generated code, not full code review.

**Benefit:** Approval gates run on much smaller artifacts (specs are concise by design). Context is explicit (the spec states intent). Reviews are faster and higher-level.

**Evidence:** This is the core insight of S3.3 (Verification / Review Gates) — shifting trust from code diff to approved specification.

---

## Approval Bottleneck in SDD Maturity Levels

How the bottleneck manifests depends on the SDD level:

### Level 1: Spec-First
Specs are reviewed before coding begins. Approval gates are at spec phase + implementation phase (both synchronous). High bottleneck risk because both gates are manned by the same small team.

### Level 2: Spec-Anchored
Specs and code coexist. Approval gates are at spec phase, implementation phase, and integration phase. Bottleneck risk is distributed but still present — now three gates instead of two.

### Level 3: Spec-as-Source
Code is regenerated from specs. Approval gates are primarily at spec phase and verification phase (mechanical + spot-check). Bottleneck risk is minimal because code review is replaced by spec review + automated verification.

**Key insight:** Advancing from Level 1 to Level 3 is not just about tool capability — it's about moving the bottleneck upstream (to specs) where approvals are lighter and faster.

---

## Measurement and Monitoring

Teams that successfully mitigate the approval bottleneck track these metrics:

1. **Queue Depth** — count of pending items waiting for approval at any given moment
2. **Decision Latency (Median)** — typical time from task completion to approval
3. **Decision Latency (95th Percentile)** — tail latency; indicates backlog spikes
4. **Auto-Approval Rate** — % of tasks that skip human review; target 70–80% for low-risk lanes
5. **Rejection Rate** — % of approved items that required rework; should remain <5%; if >15%, gate is misconfigured
6. **Reviewer Throughput** — decisions per reviewer per hour; watch for fatigue (falling from 30–60/hr baseline)
7. **Staleness Window** — time between task completion and approval; beyond a threshold (1–2 hours for interactive tasks, 1 day for batch tasks), decision value decays

If queue depth is growing, decision latency is increasing, or rejection rates are spiking, the gate is the bottleneck. Response options:
- Lower the approval threshold (shift tasks to auto-approve lane)
- Add parallel reviewers (distribute load)
- Reduce arrival rate (slow down agent generation)
- Move to asynchronous approval (if latency is acceptable)

---

## The Role of Confidence Metrics

A key lever that emerges across multiple sources: confidence signals from the agent.

**Pattern:** Agent-generated tasks include a confidence score. High confidence (>95%) bypasses human review. Medium confidence (80–95%) routes to human review. Low confidence (<80%) routes to expert review with priority escalation.

**Calibration:** The confidence signal must be trained on held-out data. An agent that is not calibrated will be overconfident, making the bypass lane unreliable. Teams using this pattern must monitor rejection rates on the bypass lane and retrain thresholds quarterly.

**Evidence:** Turing's developer-submission grading system achieved 85% auto-assessment (pass/fail/needs-review) with 90% agreement to expert human assessment. This reduces the review load to 15% of submissions, solving the bottleneck without lowering quality.

---

## Bottleneck Prevention: Shift Left to Specs

The most sustainable solution is architectural: prevent the bottleneck by reviewing specs, not code.

SDD Maturity Migration Path:
1. **Write specs before code** (Spec-First) — bottleneck: spec review
2. **Keep specs in sync with code** (Spec-Anchored) — bottleneck: spec + code review
3. **Regenerate code from specs** (Spec-as-Source) — bottleneck: spec review only

At Level 3, approval gates are lean because specs are concise (typically 500–2000 words per feature) and code review is replaced by deterministic verification. A spec that passed approval cannot produce non-conforming code if generation is deterministic — hence approval gates become spot-checks, not full reviews.

---

## Approval Bottleneck in MyVocaList Context

For MyVocaList, the approval bottleneck is a near-term concern:

- **Current state:** Spec-First level. Each feature has a requirements.md, design.md, and tasks.md. After implementation, Helder (architect) reviews.
- **Bottleneck risk:** If the implementation phase runs faster than Helder can review (which is likely with Claude Code agents), tasks complete but cannot merge until reviewed.
- **Mitigation (Level 1):** Async review windows + risk-tiered approval lanes. Helder reviews each day at 10–11am and 2–3pm; low-risk tasks auto-approve after 4 hours.
- **Mitigation (Future, Level 2/3):** Specs approve before code, automated gates verify conformance, human review becomes spot-check only.

---

## Sources

- [How to Build Human-in-the-Loop Approval Gates for AI Coding Agents — Code on Grass (2026-04-25)](https://codeongrass.com/blog/how-to-build-human-in-the-loop-approval-gates-ai-coding-agents/)
- [Async Approval Gates: Reducing Coordination Drag Without Slowing Workflows — Operaitions (2026-03-02)](https://operaitions.ai/blog/async-workflows/)
- [Designing Approval Gates for Autonomous AI Agents — Tian Pan (2026-03-06)](https://tianpan.co/blog/2026-03-06-designing-approval-gates-for-autonomous-ai-agents)
- [Human-in-the-Loop Is a Queue, and Queues Have Dynamics — Tian Pan (2026-04-23)](https://tianpan.co/blog/2026-04-23-hitl-queue-dynamics-approver-fatigue)
- [What is hitl? Meaning, Architecture, Examples, Use Cases, and How to Measure It — AiOps School (2026)](https://aiopsschool.com/blog/hitl/)
- [What Is Human-in-the-Loop AI? A Clear Guide for Engineering Teams — Cordum (2026-04-07)](https://cordum.io/blog/what-is-human-in-the-loop-ai)
- [Human-in-the-Loop Governance: Oversight Without Bottlenecks — Uplatz Blog (2025-11-28)](https://uplatz.com/blog/human-in-the-loop-governance-oversight-without-bottlenecks/)
- [From Bottlenecks to Flywheels: Human-in-the-Loop AI in Practice — Turing (2025-10-09)](https://www.turing.com/resources/from-bottlenecks-to-flywheels-human-in-the-loop-ai-in-practice)
- [Verification Is the Next Bottleneck in AI-Assisted Development — Opslane (2026-04-05)](https://www.opslane.com/blog/verification-bottleneck)
- [Closing the verification loop: Observability-driven harnesses for building with agents — Datadog (2026-03-09)](https://www.datadoghq.com/blog/ai/harness-first-agents)
- [Why Your AI Coding Agent Keeps Going Off-Script — Bayseian (2025-02-26)](https://www.bayseian.com/blog/spec-driven-agentic-workflows)
- [docs/guides/why-cc-sdd.md — cc-sdd GitHub (2026)](https://github.com/gotalab/cc-sdd/blob/main/docs/guides/why-cc-sdd.md)
- [fischmanb/auto-sdd — GitHub (2026-02-20)](https://github.com/fischmanb/auto-sdd)

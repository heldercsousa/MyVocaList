# S3 — Workflow Phases: Enhancement Opportunities

> Generated from analysis of S3, S3.1, S3.2, S3.3, S3.1.1, S3.1.2, S3.2.1, S3.2.2, S3.3.1, S3.3.2
> against `.claude` current state (CLAUDE.md, workflow.md, code-principles.md, testing.md).
> Last reviewed: 2026-05-05

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (captured and already partially implemented) | 3 |
| ♻️ Refined (captured, needs update to match current workflow.md state) | 3 |
| 🆕 New (not yet captured) | 10 |
| Total | 16 |

### Validated (already in workflow.md or closely aligned)
- OPP-3-07: `blocked: spec gap` status — already in workflow.md Rule 2 and Rule 5 task statuses
- OPP-3-04: `[P]` / `[SEQUENTIAL]` markers — already in workflow.md Rule 4
- OPP-3-01: Spec-First (Rule 1) — partially present; the "spec changes before code" invariant is implied but not stated as a rule

### Needs refinement
- OPP-3-01: Rule 1 states "read design.md before code" but does not state the invariant explicitly ("when implementation reveals a gap, stop — update spec first, then re-implement")
- OPP-3-07: `blocked: spec gap` status exists but the protocol for *what to document* (question + options + recommendation) is in Rule 2 text but not in testing.md or review.md
- OPP-3-03: `[P]`/`[SEQUENTIAL]` markers exist in Rule 4, but the `produces/consumes` dependency template is absent from tasks.md format guidance

---

## Opportunities

### OPP-3-01: Spec update rule — code never changes before spec does
**Status:** ♻️ Refined
**Target:** `.claude/rules/workflow.md`
**Action:** Update Rule 1 to add explicit invariant statement
**Source topic:** S3 — Workflow Phases (core invariant), S3.1 — Planning Phase
**Gap:** Rule 1 says "read design.md before writing code" but does not state the reverse invariant: when implementation reveals a gap, the spec is updated first, then the code. Agents that discover a mismatch may patch code and continue, causing silent spec drift invisible to future agents.
**Suggested change:** Add to Rule 1 under "Spec-First":
```
### The SDD Invariant — spec changes before code changes
When implementation reveals a spec gap, design flaw, or ambiguity:
1. STOP — do not patch the code and continue
2. Update design.md (or requirements.md if a requirement is missing)
3. Update tasks.md if task ordering or scope is affected
4. Signal in the task-log: status "Spec updated — re-planning required"
5. Only then re-implement the affected task against the updated spec

A code patch without a spec update is invisible drift. Never do it.
```

---

### OPP-3-02: Planning gate checklist — constitution/governance check step
**Status:** ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add step 2a to "New feature workflow" in Rule 1
**Source topic:** S3.1 — Planning Phase (planning gate step 5: Constitution/Governance Check)
**Gap:** The SDD planning gate includes a step that checks specs against CLAUDE.md non-negotiables before implementation is authorized. This step is absent from the current workflow — specs may pass product review but violate non-negotiables (proposing DisplayAlert, wrong layer deps, unapproved dependencies).
**Suggested change:** Add after "Write spec" step in Rule 1:
```
2a. **Constitution check** — before approving the spec for planning, verify requirements.md and
    design.md respect:
    - CLAUDE.md non-negotiables (no DisplayAlert, DevExpress first, MD3 terminology, English only)
    - Architecture constraints (business logic in Services only, no reverse layer deps)
    - Stack constraints (EF Core 10 + SQLite, CommunityToolkit.Mvvm, no unapproved new deps)
    - Naming conventions (MD3 terms, DTO records in Contracts, interface/impl naming)
    If any violation is found, update the spec before proceeding.
```

---

### OPP-3-03: Task spec template — produces/consumes/dependency markers
**Status:** ♻️ Refined
**Target:** `.claude/rules/workflow.md`
**Action:** Add task entry format with produces/consumes fields to Rule 4
**Source topic:** S3.1.2 — Dependency Analysis Incompleteness, S3.2.1 — Task Granularity Calibration
**Gap:** `[P]`/`[SEQUENTIAL]` markers exist in Rule 4, but there is no `produces/consumes` documentation format for tasks. Hidden dependencies (Task C needs a method Task B creates but nobody declared it) are only discovered during implementation, causing re-sequencing rework.
**Suggested change:** Add a "Task entry format" sub-section to Rule 4:
```markdown
## Task N: [Descriptive Title]
**Acceptance Criteria:**
- [ ] Criterion from requirements.md

**Constraints:** Follow [rule] from [file path]

**Depends on:** Task M (artifact needed: e.g., IVenueRepository interface from Task 2)
**Blocks:** Task P

**Produces:** [list of new files/artifacts: e.g., VenueService.cs, IVenueService interface]
**Consumes:** [list of files/artifacts from prior tasks]

**Parallel:** [P] or [SEQUENTIAL] — reason: [e.g., "touches different layer than Task 4"]

**Estimated size:** Simple (< 5 min) | Moderate (5–15 min) | Complex (15–30 min)
```
Also add: "Use [P] only when tasks touch completely different files and modules with no shared config, migrations, or state. When uncertain, mark SEQUENTIAL."

---

### OPP-3-04: DRY Onion ordering rule for tasks.md
**Status:** ✅ Validated (partial — [P]/[SEQUENTIAL] exist; the explicit layer ordering is absent)
**Target:** `.claude/rules/workflow.md`
**Action:** Add layer ordering guidance to Rule 4
**Source topic:** S3.2 — Implementation Phase (DRY Onion pattern), S3.1.2 — Dependency Analysis Incompleteness
**Gap:** The project has a clear architectural layer order (Domain → Contracts → Infra → Services → MAUI), but Rule 4 does not mention it. Tasks that skip layers or are parallel across layers fail with hidden dependency errors. The most common failure: a UI or ViewModel task starts before its service interface exists.
**Suggested change:** Add to Rule 4:
```
### Task ordering: DRY Onion (innermost first)
Sequence tasks inside-out to match the MyVocaList layer order:

  Layer 1 (innermost): Domain entities, EF entity config, migrations
  Layer 2:             Repository interfaces (Domain) + implementations (Infra)
  Layer 3:             Service interfaces + business logic (Services)
  Layer 4:             ViewModels + navigation (MAUI)
  Layer 5 (outermost): Pages, XAML, code-behind (MAUI)

Tasks within the same layer may run in parallel [P].
Tasks across layers must run sequentially — inner layer completes before outer begins.
If a task touches multiple layers, split it.
```

---

### OPP-3-05: Context window budget rule — task sizing and subagent scope
**Status:** ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add task sizing limits sub-section to Rule 2
**Source topic:** S3.2.2 — Context Window Exhaustion, S3.2.1 — Task Granularity Calibration
**Gap:** Workflow.md has no guidance on task sizing. Research establishes: practical working window for a Claude subagent is ~50–60K usable tokens (after fixed overhead of system prompt, tools, rules). Tasks that exceed this degrade in quality without warning. Concrete size heuristics give the orchestrator a pre-dispatch checklist.
**Suggested change:** Add to Rule 2 "Subagent Delegation":
```
### Task sizing limits (prevent context window exhaustion)
Before dispatching a subagent, verify the task fits within a single session:
- Files to read + modify: 1–5 (> 5 files signals cross-layer task — split it)
- Lines of code to produce: ≤ 200 (target 50–150)
- Acceptance criteria: 1 focused criterion per task
- Estimated time: 5–15 min (> 20 min → split into two tasks)

If a task exceeds these bounds, split it before dispatching.
One acceptance criterion + one commit = one task.
```

---

### OPP-3-06: Subagent must re-read spec on every session start
**Status:** ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add mandatory spec reads to "Briefing protocol" in Rule 2
**Source topic:** S3.2.2 — Context Window Exhaustion (spec drift risk), S3.2 — Implementation Phase
**Gap:** The briefing protocol says "tell the subagent which files to read" but does not mandate reading requirements.md and design.md at the start of every task. Without this, a subagent implements against implicit training knowledge instead of the approved spec, introducing spec drift that accumulates silently.
**Suggested change:** Add to "Briefing protocol" in Rule 2:
```
### Mandatory spec reads at session start
Every subagent must, as its first actions, read:
1. Docs/specs/[feature]/requirements.md — acceptance criteria section
2. Docs/specs/[feature]/design.md — relevant architecture decisions section
3. The specific task from Docs/specs/[feature]/tasks.md
4. The task-log beside the plan (for prior task outputs if on a dependency)

The spec is the oracle. If the briefing conflicts with the spec, the spec wins.
```

---

### OPP-3-07: Spec gap escalation — subagent blocking protocol
**Status:** ♻️ Refined
**Target:** `.claude/rules/workflow.md`
**Action:** Make the `blocked: spec gap` documentation requirement explicit in the subagent exit checklist
**Source topic:** S3.2 — Implementation Phase (spec ambiguity handling)
**Gap:** The `blocked: spec gap` status exists in Rule 2 and Rule 5. However, the *content* the subagent must document when blocking is only described in Rule 2 text ("question + options + recommendation") and is not in the exit checklist. Subagents may set the status without providing enough context for the human to resolve the gap.
**Suggested change:** Add to the "Subagent exit checklist" in Rule 2, after the existing three steps:
```
When status is `blocked: spec gap`, before stopping, document in the task-log:
- (a) What the ambiguity is (exact spec line or section that is unclear)
- (b) What the implementation found that conflicts with or is missing from the spec
- (c) Two or three options for resolving it
- (d) Which option the agent recommends and why
The agent does NOT choose and implement — it stops after documenting.
```

---

### OPP-3-08: review.md — spec drift and scope gate checks
**Status:** ✅ Validated
**Target:** `.claude/commands/review.md`
**Action:** Add "Spec Conformance" section to the review checklist
**Source topic:** S3.3 — Verification / Review Gates (scope gate, intent drift), S3.2.2 — Context Window Exhaustion
**Gap:** The review command covers build quality and code patterns but does not include spec-conformance checks: (1) does the diff touch only files in the task scope, and (2) does the code match design intent, not just compile correctly. These are the two review gaps that let intent drift and scope creep through.
**Suggested change:** Add to review.md:
```
## Spec Conformance (run after every task review)
- [ ] Diff scope: Does the diff touch only the files this task was scoped to?
      Extra files: are they incidental (formatting) or material (undeclared feature)?
- [ ] Intent alignment: Does the code embody design.md intent, not a simpler interpretation?
      Example of intent drift: spec says "sort by relevance"; agent implemented alphabetical.
- [ ] Acceptance criteria traceability: Is every AC from requirements.md covered by
      (a) an implementation reference and (b) at least one test that fails if the AC is violated?
- [ ] No spec update skipped: If the implementation deviates from design.md, was design.md updated first?
      If not, this is spec drift — reject and request spec update before re-review.
```

---

### OPP-3-09: Architecture reversibility documentation in design.md
**Status:** ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add "Key decisions" format requirement for design.md in Rule 1
**Source topic:** S3.1.1 — Architecture Debt from Early Decisions
**Gap:** New features may introduce one-way-door decisions (new third-party dependencies, schema shape choices, service boundaries). Without a documented reversibility classification, future agents treat all decisions as equally revisable, misallocating review effort and delaying discovery of architectural mistakes.
**Suggested change:** Add to Rule 1 under the spec structure table:
```
design.md must include a "Key decisions" section. For each architectural decision
(library choice, schema shape, service boundary), classify it:
- One-way door: high reversal cost — document the upgrade path and trigger condition
- Two-way door: reversible with < 1 week of effort — note the reversal approach

Example:
> **SQLite for persistence** — One-way door.
> Trigger: multi-device sync or shared queue management across users.
> Upgrade path: ~4–6 weeks to cloud backend. Not in scope for v1.

Skip classification only for trivially reversible UI choices (component variants, colors).
```

---

### OPP-3-10: Checkpoint file pattern for multi-wave features
**Status:** ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add multi-wave checkpoint paragraph to Rule 2
**Source topic:** S3.2.2 — Context Window Exhaustion (checkpoint pattern), S3.2.1 — Task Granularity (Ralph Loop)
**Gap:** For features with 10+ tasks split across waves, context transfer between waves is implicit. Wave 3 agents read the codebase cold and may not understand what Wave 1 established. A lightweight checkpoint file solves this without repeating full spec content.
**Suggested change:** Add to Rule 2:
```
### Multi-wave checkpoint (features with 10+ tasks)
After completing each wave, the main agent writes a checkpoint file at:
  Docs/specs/[feature]/checkpoints/wave-N.md

Checkpoint content (keep under 500 tokens):
- Wave N tasks completed: [task titles + commit hashes]
- Artifacts produced: [file paths of new interfaces, entities, migrations]
- Known constraints discovered: [hidden dependencies or spec gaps found]
- Next wave prerequisites: [what the next subagent must read before starting]

The next wave's subagent briefing includes the checkpoint path to read.
The full spec files are still mandatory reads — the checkpoint supplements, never replaces, the spec.
```

---

### OPP-3-11: Pre-dispatch task list validation checklist
**Status:** 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add task list audit checklist to Rule 4 (before dispatching any wave)
**Source topic:** S3.1.2 — Dependency Analysis Incompleteness (Spec Kit Agents validation hooks)
**Gap:** Hidden dependencies are discovered during implementation rather than during planning, causing re-sequencing rework. Research shows that pre-dispatch validation of the task graph (cycle detection, shared-file check, migration order check) reduces dependency surprises by ~50%. The current workflow has no such pre-dispatch gate.
**Suggested change:** Add to Rule 4 a "Pre-dispatch validation" sub-section:
```
### Pre-dispatch validation (run before starting Wave 1)
Before dispatching any tasks, audit the task list for:
- [ ] No cycles: Task A → Task B → Task A does not exist; if found, re-sequence
- [ ] Parallel-marked tasks ([P]) do not share files, migrations, or global singletons
- [ ] Every produced artifact in Task N is declared as consumed in the appropriate Task N+1
- [ ] Database migrations are ordered correctly: schema before indexes, before data mutations
- [ ] DI registrations happen in declared order (singletons before scoped that depend on them)
- [ ] No [P] task assumes state that another [P] task in the same wave writes

If any check fails, update tasks.md before dispatching. Discovery during implementation is 3–5× more expensive than discovery before dispatch.
```

---

### OPP-3-12: Spike validation task for high-risk one-way-door decisions
**Status:** 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add spike task pattern to Rule 1 planning workflow
**Source topic:** S3.1.1 — Architecture Debt from Early Decisions (spike validation strategy)
**Gap:** The planning gate reviews spec correctness but has no mechanism to validate high-risk architectural decisions before the task list is finalized. When a planning-phase one-way-door decision proves wrong during implementation (a library lacks a required feature, a schema choice creates performance issues), reversing it costs 2–8 weeks of rework. A pre-implementation spike costs 1–2 days.
**Suggested change:** Add to Rule 1 "New feature workflow" after "Write plan":
```
3a. **Spike (when required)** — for any one-way-door decision in design.md with uncertain viability,
    insert a spike task as the first task in tasks.md before any implementation:

    Task 0: Spike — validate [decision name]
    Goal: Confirm [library/schema/pattern] supports [required feature].
    Deliverable: A 1-page spike report in Docs/specs/[feature]/spike-[topic].md answering:
    - Does the chosen approach support all required acceptance criteria?
    - What are the known limitations at the target scale?
    - Is there a blocking issue? If yes, what is the alternative?
    Outcome: If spike passes → proceed to Task 1. If spike fails → update design.md and tasks.md
    before implementing anything.

When to insert a spike:
- The library/framework has never been used for this access pattern in the project
- The schema design is novel (new join table, hierarchical data, full-text search)
- A performance claim in the spec has not been validated against real data
```

---

### OPP-3-13: Risk-tiered task review lanes
**Status:** 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add risk classification to task entry format and review protocol
**Source topic:** S3.3.1 — Approval Bottleneck (risk-tiered approval lanes), S3.3.2 — Authority Ambiguity
**Gap:** All tasks currently route to the same review path (Helder reviews everything). As agent output accelerates, this becomes the throughput bottleneck. Risk-tiered review lanes reduce load by routing low-risk tasks through lighter gates without eliminating human oversight for high-risk changes.
**Suggested change:** Add to Rule 4 task entry format a `Risk` field, and add to Rule 2 a review routing table:
```
**Risk:** Low | Medium | High

Risk classification:
- Low: CRUD on existing patterns, documentation, test additions, formatting
- Medium: New feature using existing patterns, queries on proven schema
- High: Schema migrations, new third-party dependencies, auth/encryption changes, new architectural patterns

Review routing:
- Low: Main agent self-approves if all automated checks pass (build + tests)
- Medium: Helder reviews within 2 days
- High: Helder reviews before next wave begins (blocking gate)
```

---

### OPP-3-14: Acceptance criteria traceability gate in review checklist
**Status:** 🆕 New
**Target:** `.claude/commands/review.md`
**Action:** Add explicit AC traceability check (not just "tests pass" but "each AC has a named test")
**Source topic:** S3.3 — Verification / Review Gates (acceptance criteria traceability gate)
**Gap:** The current review process checks "tests pass" but not "every acceptance criterion from requirements.md is traceable to a named test that would fail if that criterion were violated." These are different checks. A test suite can pass 100% while leaving ACs untested (tests cover implementation details, not acceptance criteria). OPP-3-08 adds spec conformance but does not give the reviewer a concrete tracing format.
**Suggested change:** Add to the spec conformance section in review.md a traceability format:
```
## AC Traceability (one row per acceptance criterion)
For each AC in requirements.md, confirm:
| AC | Implementation location | Test that fails if AC is violated |
|----|------------------------|-----------------------------------|
| [AC text] | [file + method] | [TestClass.TestMethod] |

If any AC row has no test, the task is INCOMPLETE — return to implementation.
If any AC row has no implementation reference, the task is INCOMPLETE — return to implementation.
A passing test suite with unmapped ACs is not verified; it is untested.
```

---

### OPP-3-15: Async review windows — explicit schedule in workflow.md
**Status:** 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Document Helder's review SLA and escalation policy in Rule 2 or a new Rule 7
**Source topic:** S3.3.1 — Approval Bottleneck (async review windows, SLA-based review routing)
**Gap:** There is no documented review SLA. Subagents complete tasks and push, then stop. The main agent has no guidance on how long to wait before a task is considered stalled, when to escalate, or how to batch tasks for efficient human review. This leaves the pipeline in an undefined state when tasks pile up.
**Suggested change:** Add a "Review SLA" section to Rule 2 or Rule 5:
```
### Review SLA (single-reviewer project)
After a subagent signals "To Review":
- Low-risk tasks: proceed to next wave if Helder has not reviewed within 1 day
  (main agent may self-approve with a note in the task-log: "Self-approved: low risk")
- Medium-risk tasks: wait for explicit Helder review before proceeding
- High-risk tasks (schema, auth, new deps): wait indefinitely — never self-approve

Batch reviews: when multiple tasks are "To Review", present all of them together
rather than reviewing one and leaving others pending. Batching reviews reduces
context-switching cost and keeps the pipeline moving.
```

---

### OPP-3-16: Authority matrix for MyVocaList approval decisions
**Status:** 🆕 New
**Target:** `.claude/rules/workflow.md` (or a new `.claude/rules/approval-authority.md`)
**Action:** Add explicit approval authority table covering all phase types and change types
**Source topic:** S3.3.2 — Authority Ambiguity (Approval Authority Matrix, RACI for SDD)
**Gap:** There is no documented authority for which types of changes Helder must approve vs. which Claude Code can self-approve. Without this, agents either wait unnecessarily for Helder on trivial changes, or self-approve changes that need human oversight. The SDD principle: undefined approval authority is functionally equivalent to no approval authority.
**Suggested change:** Add to workflow.md or a new rules file:
```markdown
## MyVocaList — Approval Authority

| Phase | Change Type | Approval Authority | SLA |
|-------|-------------|-------------------|-----|
| Planning | Spec (requirements.md + design.md) | Helder | 3 days |
| Planning | Task order refinement within approved design | Claude Code + note in task-log | Same day |
| Implementation | CRUD task on existing patterns (Low risk) | Claude Code self-approve if build + tests pass | Same day |
| Implementation | New feature, new patterns (Medium risk) | Helder | 2 days |
| Implementation | Schema/database change | Helder | 2 days |
| Implementation | New third-party dependency | Helder | 1 day |
| Implementation | Auth/encryption/security change | Helder | 1 day |
| Verification | Merge develop → main | Helder + CI green | 1 day |

Rules:
- Claude Code never self-approves schema changes, security changes, or new dependencies
- Helder's approval is implicit when he pushes a commit building on the agent's work
- Approval is logged in the task-log ("Approved by Helder on MM/DD")
```


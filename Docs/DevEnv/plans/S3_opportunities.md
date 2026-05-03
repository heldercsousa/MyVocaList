# S3 — Workflow Phases: Enhancement Opportunities

> Generated from analysis of S3, S3.1, S3.2, S3.3, S3.1.1, S3.1.2, S3.2.1, S3.2.2, S3.3.1, S3.3.2 against `.claude` current state.

---

### OPP-3-01: Spec update rule — code never changes before spec does
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3 — Workflow Phases (core invariant), S3.1 — Planning Phase
**Rationale:** The SDD invariant "the spec changes before the code changes" is not explicitly stated in workflow.md. When an agent discovers a design flaw or spec gap during implementation, the current rules do not tell it whether to patch the code and move on or stop and update the spec first. Agents that patch without updating the spec cause silent spec drift that is invisible to future agents reading design.md.
**Suggested content/change:** Add to workflow.md as Rule 7 (or append to Rule 1):
```
## Rule 7 — Spec Before Code (The SDD Invariant)
When implementation reveals a spec gap, design flaw, or ambiguity:
1. STOP the task immediately — do not patch the code and continue
2. Update design.md (or requirements.md if a requirement is missing) to reflect the correct intent
3. Update tasks.md if the change affects task ordering or scope
4. Signal the change in the task-log with status "Spec updated — re-planning required"
5. Only then re-implement the affected task against the updated spec

Never patch code without updating the spec. A code patch without a spec update is invisible drift.
```

---

### OPP-3-02: Planning gate checklist — constitution/governance check step
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S3.1 — Planning Phase (planning gate, step 5: Constitution/Governance Check)
**Rationale:** The current workflow.md spec-first rule says "read design.md before any code." The SDD planning gate includes a step (step 5) that checks specs against CLAUDE.md non-negotiables, architecture rules, and naming constraints before implementation is authorized. This check is missing from the current planning workflow — Claude Code may produce specs that pass product review but violate non-negotiables (e.g., proposing DisplayAlert, using wrong EF Core patterns, wrong layer dependencies).
**Suggested content/change:** Add to the "New feature workflow" section in Rule 1 a step between "Write spec" and "Write plan":
```
2a. **Constitution check** — before approving the spec for planning, verify that requirements.md and design.md respect:
    - CLAUDE.md non-negotiables (no DisplayAlert, DevExpress first, MD3 terminology, English only)
    - Architecture constraints (business logic in Services only, no reverse layer deps)
    - Stack constraints (EF Core 10 + SQLite, CommunityToolkit.Mvvm, no unapproved new deps)
    - Naming conventions (MD3 terms, DTO records in Contracts, interface/impl naming)
    If any violation is found, update the spec before proceeding. Never start a plan with a non-compliant spec.
```

---

### OPP-3-03: Task spec template — produces/consumes/dependency markers
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S3.1.2 — Dependency Analysis Incompleteness, S3.2.1 — Task Granularity Calibration
**Rationale:** The current tasks.md format (ordered checkboxes) has no formal dependency markers, no "produces/consumes" documentation, and no parallel markers. This means hidden dependencies are only discovered during implementation. The DRY Onion pattern (domain → infra → services → viewmodels → UI) and explicit dependency markers prevent agents from being parallelized incorrectly and reduce re-sequencing rework.
**Suggested content/change:** Add to workflow.md a "Task specification format" sub-section under Rule 4, showing the preferred tasks.md entry format:
```markdown
## Task N: [Descriptive Title]
**Acceptance Criteria:**
- [ ] Criterion from requirements.md

**Constraints:** Follow [rule] from [file path]

**Depends on:** Task M (explain what artifact is needed)
**Blocks:** Task P

**Produces:** [list of files/artifacts created]
**Consumes:** [list of files/artifacts from prior tasks]

**Parallel:** [P] or [SEQUENTIAL] with brief reason

**Estimated size:** Simple (< 5 min) | Moderate (5–15 min) | Complex (15–30 min)
```
Also add: "Use [P] only when tasks touch completely different files and modules with no shared config, migrations, or state. When uncertain, mark SEQUENTIAL."

---

### OPP-3-04: DRY Onion ordering rule for tasks.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3.2 — Implementation Phase (DRY Onion pattern), S3.1.2 — Dependency Analysis Incompleteness
**Rationale:** The MyVocaList architecture has a clear layer order: Domain → Contracts → Infra → Services → MAUI. Tasks in the same layer can be parallel; tasks across layers must be sequential (inner layer first). This ordering rule maps directly to the project's architecture and prevents the most common hidden-dependency failure mode: a UI task starting before its service interface exists.
**Suggested content/change:** Add to workflow.md Rule 4 or Rule 2 a "Dependency ordering" paragraph:
```
### Task ordering: DRY Onion (innermost first)
When creating tasks.md, sequence tasks inside-out to match the MyVocaList layer order:

  Layer 1 (innermost): Domain entities, EF entity configuration, migrations
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
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3.2.2 — Context Window Exhaustion, S3.2.1 — Task Granularity Calibration
**Rationale:** The current workflow.md has no guidance on task sizing beyond "commit after every task." Research confirms that the practical working window for a Claude Code subagent is ~50–60K usable tokens (after system prompt, tools, rules overhead). Tasks scoped beyond that budget degrade in quality without warning. Concrete size heuristics give the orchestrating agent a pre-dispatch checklist to catch over-sized tasks before they're assigned.
**Suggested content/change:** Add a "Task sizing limits" sub-section to Rule 2 (Subagent Delegation):
```
### Task sizing limits (prevent context window exhaustion)
Before dispatching a subagent, verify the task fits within a single session:
- Files to read + modify: 1–5 (touching > 5 files signals the task cuts across layers — split it)
- Lines of code to produce: ≤ 200 (target 50–150)
- Acceptance criteria: 1 focused criterion per task
- Estimated time: 5–15 minutes (> 20 min → split into two tasks)

If a task exceeds these bounds, split it before dispatching — not after the agent runs out of context.
One acceptance criterion + one commit = one task.
```

---

### OPP-3-06: Subagent must re-read spec on every session start
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S3.2.2 — Context Window Exhaustion (spec drift risk), S3.2 — Implementation Phase (ephemeral context isolation)
**Rationale:** Subagents are ephemeral — they start with a fresh context per task. The current briefing protocol says "tell subagent which files to read" but does not mandate reading requirements.md and design.md at the start of every task. Without this, a subagent implements against its implicit training knowledge instead of the approved spec, introducing spec drift that accumulates silently across tasks.
**Suggested content/change:** Add to the "Briefing protocol" section in Rule 2:
```
### Mandatory spec reads at session start
Every subagent must, as its first actions, read:
1. `Docs/specs/[feature]/requirements.md` — acceptance criteria section
2. `Docs/specs/[feature]/design.md` — relevant architecture decisions section
3. The specific task from `Docs/specs/[feature]/tasks.md`
4. The task-log file beside the plan (for prior task outputs if blocked on a dependency)

Do NOT rely on context transferred from prior sessions or from the briefing prompt alone.
The spec is the oracle. If there is a conflict between the briefing and the spec, the spec wins.
```

---

### OPP-3-07: Spec gap escalation — subagent blocking protocol
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3.2 — Implementation Phase (spec ambiguity handling), S3.2.1 — Task Granularity Calibration
**Rationale:** The SDD implementation principle is: when a subagent encounters spec ambiguity that it cannot resolve, it must escalate to the human rather than make an unilateral architectural decision. The current workflow.md subagent return protocol only covers "done" and "fail" states. There is no documented path for "spec gap found — blocked pending clarification," which means agents improvise and introduce drift.
**Suggested content/change:** Extend the "Subagent return protocol" in Rule 2 to include a third status:
```
Subagents communicate completion only by:
1. Updating task-log.md with the task status:
   - `done` — task complete, build passes
   - `fail` + one-line reason — build fails after 3 attempts, or spec error
   - `blocked: spec gap` + specific question — implementation reveals ambiguity the spec does not resolve;
     agent must document: (a) what the ambiguity is, (b) which spec section is unclear,
     (c) what two or three options are, and (d) which option the agent recommends and why.
     Agent does NOT choose and implement — it stops and waits.
2. Committing all work (including partial if blocked)
3. Stopping
```

---

### OPP-3-08: review.md — spec drift and scope gate checks
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S3.3 — Verification / Review Gates (scope gate, intent drift failure mode), S3.2.2 — Context Window Exhaustion (spec drift detection)
**Rationale:** The current review.md checklist covers build quality and code patterns but does not include spec-conformance checks: (1) does the diff touch only files declared in the task spec, and (2) does the code match the design intent in design.md, not just compile correctly? These are the two review gaps that let intent drift and scope creep through. The SDD verification principle is that every acceptance criterion must be traceable to both implementation and a test.
**Suggested content/change:** Add a "Spec Conformance" section to the review checklist:
```
## Spec Conformance (run after every task review)
- [ ] Diff scope: Does the diff touch only the files this task was scoped to touch?
      If extra files were modified, are they incidental (formatting) or material (adds features not in scope)?
- [ ] Intent alignment: Does the code embody the design intent from design.md, not a simpler interpretation?
      Example of intent drift: spec says "sort by relevance"; agent implemented alphabetical sort (both compile, only one matches intent).
- [ ] Acceptance criteria traceability: Is every acceptance criterion from requirements.md covered by
      (a) an implementation reference and (b) at least one test that would fail if the criterion were violated?
- [ ] No spec update skipped: If the implementation deviates from design.md, was design.md updated first?
      If not, this is spec drift — reject and request spec update before re-review.
```

---

### OPP-3-09: Architecture reversibility documentation in design.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3.1.1 — Architecture Debt from Early Decisions
**Rationale:** The MyVocaList stack has several one-way-door decisions already locked in (SQLite, DevExpress MAUI, EF Core 10, CommunityToolkit.Mvvm). New features may introduce additional one-way doors (new third-party dependencies, schema shape decisions, service boundaries). Without a documented reversibility classification, future agents will treat all decisions as equally revisable, which misallocates review effort and delays discovery of architectural mistakes.
**Suggested content/change:** Add to Rule 1 (Spec-First), under the "design.md" row in the spec structure table, a note:
```
design.md must include a "Key decisions" section. For each architectural decision (library choice,
schema shape, service boundary), classify it:
- One-way door: high reversal cost (document the upgrade path and trigger condition)
- Two-way door: reversible with < 1 week of effort (note the reversal approach)

Example:
> **SQLite for persistence** — One-way door.
> Trigger to reconsider: multi-device sync or shared queue management across users.
> Upgrade path: ~4–6 weeks to cloud backend. Not in scope for v1.

Skip this classification only for trivially reversible UI choices (component variants, colors).
```

---

### OPP-3-10: Checkpoint file pattern for multi-wave features
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S3.2.2 — Context Window Exhaustion (checkpoint pattern for 20+ task features), S3.2.1 (Ralph Loop pattern)
**Rationale:** When a feature has 20+ tasks split across multiple subagent waves, context transfer between waves is implicit — each new wave agent reads the codebase cold. For large features, this means agents in Wave 3 may not understand what Wave 1 established. A lightweight checkpoint file (not documentation, just a structured summary) solves this without repeating full spec content.
**Suggested content/change:** Add a "Multi-wave checkpoint" paragraph to Rule 2:
```
### Multi-wave checkpoint (features with 10+ tasks)
After completing each wave of tasks, the main agent writes a checkpoint file at:
  Docs/specs/[feature]/checkpoints/wave-N.md

Checkpoint content (keep under 500 tokens):
- Wave N tasks completed: [task titles + commit hashes]
- Artifacts produced: [file paths of new interfaces, entities, migrations]
- Known constraints discovered: [any hidden dependencies or spec gaps found]
- Next wave prerequisites: [what the next subagent must read before starting]

The next wave's subagent briefing includes the checkpoint path to read. The full spec files
are still mandatory reads — the checkpoint supplements, never replaces, the spec.
```


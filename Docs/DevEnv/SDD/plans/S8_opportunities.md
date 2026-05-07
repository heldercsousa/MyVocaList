# S8 — Project Management: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)
> Last updated: 2026-05-05

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, confirmed by re-read) | 9 |
| 🆕 New (not yet captured) | 6 |
| **Total** | **15** |

---

## ✅ Validated Opportunities

### OPP-8-01: Task atomization guidance in tasks.md authoring
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.1.1 — Task Atomization
**Rationale:** workflow.md Rule 4 says "tasks.md is the source of truth" but gives no guidance on how to write good tasks. Research shows that under- and over-atomization are the top sources of agent failure. Concretely encoding the calibration heuristics prevents consistently bad task decomposition that requires human correction.
**Suggested content/change:** Add a sub-section to Rule 4 titled "Writing tasks.md: atomization checklist":
- Target 15–45 minutes of agent work per task
- Each task must state: exact files to modify, what to implement (specific, not vague), what tests to write, what "done" looks like (acceptance criteria)
- Additive tasks (new files, new features) → safe to mark `[P]` for parallel execution
- Edit tasks (refactoring shared code, migrating global patterns, config changes) → must be sequenced; never mark `[P]`
- If a task requires >3–4 tool calls (reads, edits, builds, tests), it is too large — split it
- List prerequisites before dependents; never reverse the order
- Use `[P]` marker only when tasks are file-disjoint (no file overlap between parallel tasks)

---

### OPP-8-02: Thick-slice task format for subagent briefings
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.1.1 — Task Atomization (thick vs. thin slices)
**Rationale:** Research shows thin-slice tasks ("add validation") cause agents to lose context and produce wrong output. The workflow.md briefing protocol says "give concrete scoped instructions" but doesn't define what "concrete" means for a task unit. Encoding the thick-slice format prevents the common pattern of vague briefings that produce revision cycles.
**Suggested content/change:** Add to the "Briefing protocol" in Rule 2:

> A well-formed task briefing for a subagent is a **thick slice**: it includes (1) the objective — what decision or feature it implements, (2) the files to modify (exact paths), (3) constraints — what success looks like and what must not change, (4) the verification gate — build command, test filter, or acceptance test. A thin briefing ("implement the service") is not sufficient. The subagent must be able to complete the task without re-reading the full spec.

---

### OPP-8-03: Dependency ordering rule for tasks.md phases
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.1 — Task Tracking (dependency ordering), S8.1.1 — Atomization
**Rationale:** workflow.md Rule 4 says "never start a task that depends on an incomplete task" but doesn't show how to encode dependency ordering in tasks.md. Practitioners consistently use phase headers and `depends_on` notation. Formalizing this pattern prevents implicit dependency violations by subagents.
**Suggested content/change:** Add a "Dependency encoding" example to Rule 4:

```markdown
## Phase 1: Domain & Contracts (no dependencies)
- [ ] T-001 Create [Entity] domain model + unique constraints
- [ ] T-002 Create I[Entity]Repository interface (depends_on: T-001)

## Phase 2: Infra (depends on Phase 1 committed)
- [ ] T-003 Implement [Entity]Repository (depends_on: T-002)

## Phase 3: Services (depends on Phase 2)
- [ ] T-004 Implement [Entity]Service (depends_on: T-003)

## Phase 4: UI — can parallelize (depends on Phase 3)
- [ ] [P] T-005 Create [Entity]Page XAML (depends_on: T-004)
- [ ] [P] T-006 Create [Entity]ViewModel (depends_on: T-004)

## Phase 5: Tests (runs last)
- [ ] T-007 Unit tests for [Entity]Service (covers T-004)
- [ ] T-008 Integration tests for [Entity]Repository (covers T-003)
```

Add: Tasks in Phase N may not start until all tasks in Phase N-1 are committed to the branch.

---

### OPP-8-04: In-progress marker `[~]` for claimed tasks
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.1 — Task Tracking (marked checkpoints)
**Rationale:** When multiple subagents run in parallel, there is no signal that a task is claimed. A second subagent could attempt to claim the same task. The `[~]` marker is an established SDD convention that prevents this, and it has zero tooling overhead — it is just a checkbox variant in markdown.
**Suggested content/change:** Add to Rule 4: "When a subagent begins work on a task, it must update the checkbox from `[ ]` to `[~]` (in progress) before making its first file change, and from `[~]` to `[x]` when the task is complete and committed. This prevents a second agent from claiming an already-started task."

---

### OPP-8-05: Single-writer rule for hotspot files
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.2 — Parallel Work Coordination (S8.2.5 — Cross-Team Spec Consistency)
**Rationale:** The current workflow enforces max 4 parallel agents but has no rule about which files can be safely written in parallel. Hotspot files (MauiProgram.cs, AppShell.xaml, AppShell.xaml.cs, AppDbContext.cs, GlobalUsings.cs, Directory.Build.props) are touched by almost every feature. If two parallel subagents both modify MauiProgram.cs, the merge conflict requires human intervention. Making this explicit prevents the most common parallel-agent conflict in .NET MAUI.
**Suggested content/change:** Add a "Hotspot files — single writer" rule to Rule 2 (Subagent Delegation):

> **Hotspot files must have only one writer at a time.** These files are touched by nearly every feature and cannot be safely parallelized:
> - `MyVocaList/MauiProgram.cs` — DI registration
> - `MyVocaList/AppShell.xaml` + `AppShell.xaml.cs` — route registration
> - `MyVocaList.Infra/AppDbContext.cs` — entity configuration
> - Any `GlobalUsings.cs`
> - `Directory.Build.props`
>
> If two parallel tasks both require changes to a hotspot file, sequence them (not parallel). One agent completes and commits its hotspot changes; the second agent then begins.

---

### OPP-8-06: Kill criteria for stuck subagents
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.2 — Parallel Work Coordination (S8.2.4 — Kill criteria)
**Rationale:** workflow.md has no rule for when to abort a subagent that is stuck. Without kill criteria, a stuck agent continues consuming tokens without making progress, and the task-log entry never gets a `Build failure` status. Encoding explicit kill criteria gives subagents a clear decision rule and prevents infinite retry loops.
**Suggested content/change:** Add to Rule 2 (Subagent exit checklist or as a new sub-rule):

> **Kill criteria — abort a subagent task when:**
> 1. Same build error repeats across 3 fix attempts without progress → stop, write `Build failure` to task-log, push, exit
> 2. The task requires modifying a file outside its assigned scope → stop, write `blocked: scope conflict` to task-log, do NOT modify the out-of-scope file, push, exit
> 3. A spec ambiguity is found that requires an architectural decision → stop, write `blocked: spec gap` with options + recommendation to task-log, push, exit
>
> Never attempt more than 3 fix cycles on the same error. Retry on a different approach counts as a new cycle only if a materially different strategy is applied.

---

### OPP-8-07: Session resumption checklist — what to read at session start
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.3 — Progress Visibility (memory persistence, cross-session continuity)
**Rationale:** The current workflow.md has no guidance on how to resume after a session break or context compaction. Research shows that agents that re-read externalized state files at session start outperform agents that rely on conversational memory. For a single-developer project like MyVocaList, this translates to: at the start of every session, read the right files before taking any action. This prevents re-litigating resolved decisions.
**Suggested content/change:** Add a new Rule 7 — Session Start Protocol:

> **Rule 7 — Session Start Protocol**
> At the start of every session (or after context compaction), read these files before taking any action:
> 1. `CLAUDE.md` — architecture, stack, non-negotiables
> 2. `Docs/specs/[active-feature]/tasks.md` — current task state (which tasks are done, which are in progress)
> 3. `Docs/superpowers/plans/[active-plan]-task-log.md` — recent outcomes and any blockers
> 4. `.claude/memory/MEMORY.md` — active feature pointer and pending tasks
>
> Do not start coding without completing these reads. If `tasks.md` shows a `[~]` in-progress task, that was the last active task — resume it unless a commit is already present for it.

---

### OPP-8-08: Spec-code alignment check in review.md
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S8.1 — Task Tracking (integration with spec drift prevention), S8.3 — Progress Visibility
**Rationale:** The current review.md checklist covers build quality, MAUI specifics, architecture, and DevExpress. It does not include a check that the implementation matches the spec's acceptance criteria. Research shows spec-code drift is the dominant failure mode in SDD. Adding a spec-alignment gate to the review command closes this gap without adding tooling overhead.
**Suggested content/change:** Add a "Spec alignment" section to review.md checklist:
- For each checked task in `tasks.md`: does the committed code implement the acceptance criteria stated in that task? If acceptance criteria are not written in the task, was the spec's requirement satisfied?
- Are there any code paths or UI behaviors present in the implementation that are NOT described in `requirements.md` or `design.md`? (Scope creep indicator)
- If any drift is found: do not fail the review silently — add a follow-up task to `tasks.md` for the delta and note it in the task-log.

---

### OPP-8-09: findings.md as a session artifact alongside task-log
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.3 — Progress Visibility (external state files — findings.md)
**Rationale:** The workflow currently uses task-log.md for outcome recording and MEMORY.md for persistent facts. Neither is designed to capture technical findings discovered during implementation (e.g., "DevExpress BottomSheet HalfExpandedRatio behavior changed in v25.2.x", "EF Core migration lock workaround still needed"). These findings are currently lost between sessions unless manually promoted to CLAUDE.md. A lightweight findings.md pattern fills this gap with minimal overhead.
**Suggested content/change:** Add to Rule 5 (Task Status Registration): "When a task uncovers a non-obvious technical finding — a library behavior, a constraint discovered during implementation, a dead-end that should not be re-explored — add a one-line entry to `Docs/superpowers/plans/findings.md` (create if absent). Format: `| Date | Area | Finding | Source task |`. Findings that are confirmed across 2+ tasks should be promoted to the relevant `.claude/library/` file or CLAUDE.md via the continuous enhancement rule."

---

## 🆕 New Opportunities

### OPP-8-10: Git worktrees as the isolation primitive for parallel subagents
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.2 — Parallel Work Coordination (S8.2.1 — Git Worktrees as Isolation Primitive)
**Gap in current setup:** workflow.md Rule 2 defines the 4-agent wave cap and briefing protocol but has no mention of git worktrees. All parallel subagents currently run in the same working directory on the same branch. This means parallel subagents can overwrite each other's file changes silently during execution — not just at merge time. The SDD research is unambiguous: parallel agents in the same directory produce data loss; worktrees solve this.
**Concrete enhancement action:** Add a "Isolation: git worktrees" subsection to Rule 2 (Subagent Delegation):

> **Each parallel subagent must run in its own git worktree.**
> Before dispatching a wave of parallel subagents, create one worktree per agent:
> ```bash
> git worktree add ../myvocalist-agent-1 -b feat/[task-name-1]
> git worktree add ../myvocalist-agent-2 -b feat/[task-name-2]
> ```
> Brief each subagent with the path to its worktree directory (not the main repo directory). When the subagent completes and pushes its branch, remove the worktree:
> ```bash
> git worktree remove ../myvocalist-agent-1
> ```
> Subagents running sequentially (not in parallel) do not require separate worktrees.

Note: Claude Code supports `isolation: worktree` in subagent configuration, which automates worktree creation. If the superpowers subagent skill supports this, prefer it over manual setup.

---

### OPP-8-11: Dependency-first merge sequencing after a parallel wave
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.2.3 — Merge Sequencing and Conflict Resolution
**Gap in current setup:** workflow.md Rule 2 says "wait for all subagents to complete, then start the next wave" but gives no guidance on merge order after a wave completes. Two agents working on interdependent branches can produce branches that merge cleanly at file level but break at runtime if merged in the wrong order. The dependency-first merge strategy eliminates this class of failure.
**Concrete enhancement action:** Add a "Post-wave merge protocol" to Rule 2:

> After a parallel wave completes, merge branches in dependency order — not alphabetical or completion order:
> 1. Identify which branches have no downstream dependencies → merge those first.
> 2. After each merge, rebase remaining branches on the updated `develop` branch before merging them.
> 3. Run `dotnet build` and `dotnet test` after each merge, not only after all merges.
> 4. If a branch touches a hotspot file that another branch also touched, resolve at merge time using the hotspot single-writer rule (OPP-8-05) as the tiebreaker — the branch that should have run first wins.

---

### OPP-8-12: Pre-parallel interface contracts for shared types
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.2.1 — Cross-Team Spec Consistency (Strategy 1: Upfront Interface Contracts); S8.2 — "Write contracts before parallelizing"
**Gap in current setup:** When multiple subagents implement interdependent features (e.g., a service interface that one agent defines and another agent consumes), they may encode incompatible assumptions. The workflow has no rule requiring shared interfaces to be defined and committed on the main branch before agents branch off. This is the highest-value conflict prevention available — it eliminates contradictory-intent conflicts before they occur.
**Concrete enhancement action:** Add to Rule 2 (before dispatching parallel agents):

> **Before parallelizing, lock shared contracts on the branch.**
> If agents in a parallel wave will share interfaces (service interfaces, repository interfaces, DTO records, entity shapes), define those contracts first in a sequential pre-wave task:
> - Write and commit the interface/DTO/entity definitions to the working branch
> - All parallel agents then branch off from that commit
> - No agent may modify a shared interface during its parallel task — only implement against it
>
> If a parallel agent discovers that a shared interface is incomplete or incorrect, it must stop and write `blocked: spec gap` to the task-log rather than unilaterally changing the shared contract.

---

### OPP-8-13: DGI-informed complexity assessment before task decomposition
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.1.1 — Task Atomization (Decomposition Granularity Index)
**Gap in current setup:** OPP-8-01 captures the 15–45 minute heuristic and the additive/edit distinction. However, the SDD research adds a more actionable framework: classify the feature as simple/moderate/complex first, then target the number of tasks accordingly. Without this upfront classification step, task decomposition tends to be inconsistent across features — sometimes over-atomized (simple features split into 10 tasks), sometimes under-atomized (complex features crammed into 3 tasks). The DGI research shows the optimal window narrows as complexity increases, making calibration more critical for complex features.
**Concrete enhancement action:** Add a complexity classification step to the tasks.md authoring guidance (builds on OPP-8-01):

> Before writing tasks, classify the feature:
> - **Simple** (< 5 sequential steps, touches 1–2 files, clear I/O): target 1–3 tasks
> - **Moderate** (5–15 steps, touches 2–4 modules, multiple decisions): target 4–8 tasks
> - **Complex** (15+ steps, touches 4+ modules, state machines, cascading updates): target 8–15 tasks; consider breaking into sub-features
>
> If you write more tasks than the upper bound for the classified complexity, consolidate. If you write fewer than the lower bound, split. Mismatched granularity is the most common cause of agent context rot or over-coordination.

---

### OPP-8-14: 3-strike error recovery protocol for subagents
**Target:** `.claude/rules/workflow.md`
**Action:** Add (refines OPP-8-06 with a structured recovery sequence)
**Source topic:** S8.3 — Progress Visibility (OpenSpec tracking files — 3-strike protocol)
**Gap in current setup:** OPP-8-06 captures kill criteria (abort after 3 failed attempts). The SDD research adds a complementary pattern: the three attempts should use escalating strategies, not repetitions of the same fix. The 3-strike protocol defines what each attempt should do differently, preventing agents from wasting all three cycles on the same failed approach.
**Concrete enhancement action:** Add as a sub-section of the kill criteria rule (OPP-8-06):

> **3-strike recovery sequence (apply within the 3-attempt limit):**
> 1. **Strike 1:** Diagnose the error. Apply the most direct fix. Update task-log with diagnosis.
> 2. **Strike 2:** If strike 1 failed, try an alternative approach. Document the alternative in the task-log with the reasoning.
> 3. **Strike 3:** If strike 2 failed, the problem likely requires architectural input. Write the error, both attempted approaches, and a concrete recommendation to the task-log. Mark status `Build failure`. Push and exit. Do NOT attempt a fourth approach.

---

### OPP-8-15: ACTIVE-CONSIDERATIONS.md as the session priority stack
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S8.3 — Progress Visibility (external state files — ACTIVE-CONSIDERATIONS.md)
**Gap in current setup:** The session resumption checklist (OPP-8-07) reads tasks.md, task-log, and MEMORY.md. However, MEMORY.md is append-only and persistent — it does not model ephemeral, session-scoped state like "currently blocked on X", "waiting for Helder's decision on Y", or "next three priorities in order". The auto-sdd learnings system uses an `ACTIVE-CONSIDERATIONS.md` file specifically for this: a mutable priority stack that is updated at session end and read at session start, capturing what is actively in flight rather than what has been permanently decided.
**Concrete enhancement action:** Add to the session start protocol (OPP-8-07) and to Rule 5 (Task Status Registration):

> `Docs/superpowers/plans/ACTIVE-CONSIDERATIONS.md` is the session priority stack. It is mutable (unlike task-log which is append-only). Format:
> ```
> ## Current blockers
> - [Description of blocker — waiting for X]
>
> ## Next priorities (in order)
> 1. [Next task or decision needed]
> 2. [Second priority]
>
> ## Open questions for Helder
> - [Question + context + recommendation]
> ```
> Update this file at the end of every session. Read it at the start. It captures what is actively in flight that does not yet have a permanent home in task-log or MEMORY.md.

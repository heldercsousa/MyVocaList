# S1 — Core Concepts: Enhancement Opportunities

> Source files analyzed: S1_Core_Concepts.md, S1_1_Definition.md, S1_2_Implementation_Levels.md, S1_3_SDD_vs_TDD_BDD_Waterfall.md, S1_2_1_Level_Gap_Anchored_to_Source.md
> Compared against: Docs/DevEnv/plans/_current_state_summary.md

---

### OPP-1-1: Declare MyVocaList's SDD level explicitly
**Target:** CLAUDE.md
**Action:** Update
**Source topic:** S1.2 — Implementation Levels
**Rationale:** workflow.md enforces spec-first and spec maintenance, which is Spec-Anchored (Level 2) behavior. However, CLAUDE.md never states this explicitly. Without a declared level, Claude Code cannot reason about whether a proposed action (e.g., editing code without updating the spec, or skipping spec update on a bug fix) is within bounds for this project. A single declared level anchors all downstream decisions.
**Suggested content/change:** Add one sentence to the Workflow or Architecture section of CLAUDE.md: "MyVocaList operates at **Spec-Anchored** (Level 2) SDD: specs in `Docs/specs/` are updated whenever behavior changes and serve as authoritative context for every AI session. Code changes without a corresponding spec update are out of scope unless the change is a bug fix affecting no spec-described behavior."

---

### OPP-1-2: Add spec-update gate to workflow.md (Rule 1)
**Target:** .claude/rules/workflow.md
**Action:** Update
**Source topic:** S1.2 — Spec-Anchored definition
**Rationale:** Rule 1 says "read design.md before writing code" (spec as input). Spec-Anchored also requires updating the spec when behavior changes (spec as living document). The update side of the discipline is absent from the rules. As a result, Claude Code has no instruction to update specs after an implementation change — the project accumulates spec drift by default.
**Suggested content/change:** Extend Rule 1 with a sub-rule: "**After completing an implementation task that changes behavior described in the spec**, update the corresponding spec file (`requirements.md`, `design.md`, or both) to reflect the actual behavior. The spec is a living document — it is not filed away after initial implementation. A task that changes feature behavior is not complete until the spec reflects it."

---

### OPP-1-3: Add spec-drift detection to review.md checklist
**Target:** .claude/commands/review.md
**Action:** Update
**Source topic:** S1.1 — Spec-as-primary-artifact; S1.2 — Spec-Anchored maintenance discipline
**Rationale:** The current_state_summary.md explicitly notes that review.md "doesn't cover spec-drift detection or spec vs code consistency checks." Spec-Anchored practice requires that reviews verify code and spec are in sync. Without this check, drift accumulates silently after every implementation cycle and is never caught at review time — which is the only systematic gate where it could be.
**Suggested content/change:** Add a "Spec consistency" checklist item to review.md: "If the task changed any behavior defined in `Docs/specs/[feature]/`, confirm the corresponding `requirements.md` and/or `design.md` were updated. Flag any spec file that describes behavior that no longer matches the implementation."

---

### OPP-1-4: Add cross-session context loss recovery guidance
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S1.3 — SDD extends TDD; AI agents start fresh every session; spec as persistent context
**Rationale:** S1.3 identifies a structural difference between TDD and SDD: TDD assumes a human implementer who carries context across sessions; SDD must encode that context in specs because AI agents start fresh every session. MyVocaList already has spec files for this purpose, but workflow.md has no rule for what to do when a session starts mid-task (e.g., after a crash, after a compaction). Without guidance, a fresh session agent may make decisions inconsistent with prior work. The current_state_summary.md also lists "No rule for cross-session context loss recovery strategy" as a gap.
**Suggested content/change:** Add a new Rule (e.g., Rule 7 — Session Resume Protocol): "When resuming a task mid-way through implementation (new session, context compaction, subagent restart): (1) read the feature spec files (`requirements.md`, `design.md`, `tasks.md`) before doing anything else; (2) read the task-log to determine which tasks are complete; (3) check `git log --oneline -10` to see what was committed; (4) only then continue from the first unchecked task. Never infer task state from memory — always reconstruct from spec + log + git."

---

### OPP-1-5: Add atomicity guidance to task decomposition
**Target:** .claude/rules/workflow.md
**Action:** Update
**Source topic:** S1 — Four-phase workflow; Tasks phase produces "ordered, atomic implementation checklist"
**Rationale:** The current Rule 4 says "tasks.md is the source of truth" and Rule 3 says "commit after every task." But neither rule defines what makes a task atomic. S1's four-phase workflow specifies that the Tasks phase produces "ordered, atomic implementation checklist" — meaning each task must be independently committable and verifiable. Without this definition, tasks in tasks.md vary between "create the entire service layer" (too large) and "add one import statement" (too small), making commit discipline meaningless. The current_state_summary.md notes "No rule about task atomization guidance beyond commit after every task."
**Suggested content/change:** Add to Rule 4 (or as a sub-rule of Rule 3): "A task in `tasks.md` is atomic if: (1) it can be built independently after completion, (2) it produces a meaningful commit with no half-finished state, and (3) it takes no more than ~90 minutes of implementation work. If a tasks.md entry cannot satisfy these three conditions, split it before beginning. Tasks that span multiple files and multiple layers (e.g., 'implement Singer CRUD') must be broken into sub-tasks at the layer boundary (Domain entities → Repository interface → Infra implementation → Service → ViewModel → Page)."

---

### OPP-1-6: Add When-to-Skip-SDD guidance for small tasks
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S1.3 — When to Skip SDD
**Rationale:** S1.3 identifies that SDD is not appropriate for "small, well-understood tasks," "quick fixes," or "solo exploration." MyVocaList's workflow.md currently mandates spec-first for all features with no threshold guidance. This creates friction for trivial changes (a single-field rename, a cosmetic fix, a dependency version bump). Knowing when to skip spec-first is as important as knowing when to apply it, and helps Claude Code avoid over-engineering a bug fix.
**Suggested content/change:** Add a note to Rule 1: "**Skip spec-first for:** (a) bug fixes in a single file that do not change any specified behavior, (b) cosmetic/style changes (colors, spacing, typography), (c) dependency version bumps with no API changes, (d) refactoring that preserves observable behavior. **Always require spec-first for:** new features, behavior changes to existing features, architecture changes, any change touching Domain, Contracts, or Services interfaces."

---

### OPP-1-7: Add TDD-within-SDD integration rule to testing.md
**Target:** .claude/rules/testing.md
**Action:** Update
**Source topic:** S1.3 — SDD should embed TDD at its leaf nodes; the strongest workflows wrap TDD inside SDD
**Rationale:** testing.md describes TDD workflow (Red→Green→Refactor, mandatory from Step 4) but does not position TDD within the SDD structure. S1.3 is explicit that TDD operates within the SDD implementation phase — each task in tasks.md should use Red/Green/Refactor as its inner loop. Without this framing, a subagent implementing a task has no guidance on whether to write the test before or after the implementation, and may default to writing tests after (breaking TDD) even on tasks that are covered by testing.md.
**Suggested content/change:** Add a brief framing note at the top of the TDD Workflow section: "TDD operates inside the SDD implementation phase. Each task in `tasks.md` is the SDD unit; Red/Green/Refactor is the verification loop within that unit. When a task produces new service logic, ViewModel state, or repository queries, the test is written first (Red), then the minimum implementation to pass it (Green), then refactor. The task is not complete until the test passes."

# S1 — Core Concepts: Enhancement Opportunities

> Source files analyzed: S1_Core_Concepts.md, S1_1_Definition.md, S1_2_Implementation_Levels.md, S1_2_1_Level_Gap_Anchored_to_Source.md, S1_3_SDD_vs_TDD_BDD_Waterfall.md
> Compared against: CLAUDE.md, .claude/rules/workflow.md, .claude/rules/testing.md, .claude/rules/code-principles.md
> Last reviewed: 2026-05-05

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, still unimplemented) | 7 |
| 🆕 New (not previously captured) | 6 |
| **Total** | **13** |

All 7 previously captured opportunities remain unimplemented — confirmed by inspecting the current CLAUDE.md and workflow.md as of 2026-05-05. No existing opportunity has been superseded or made irrelevant.

---

## Previously Captured Opportunities

### ✅ OPP-1-1: Declare MyVocaList's SDD level explicitly
**Target:** CLAUDE.md
**Action:** Update
**Source topic:** S1.2 — Implementation Levels
**Rationale:** workflow.md enforces spec-first and spec maintenance, which is Spec-Anchored (Level 2) behavior. However, CLAUDE.md never states this explicitly. Without a declared level, Claude Code cannot reason about whether a proposed action (e.g., editing code without updating the spec, or skipping spec update on a bug fix) is within bounds for this project. A single declared level anchors all downstream decisions.
**Suggested content/change:** Add one sentence to the Workflow or Architecture section of CLAUDE.md: "MyVocaList operates at **Spec-Anchored** (Level 2) SDD: specs in `Docs/specs/` are updated whenever behavior changes and serve as authoritative context for every AI session. Code changes without a corresponding spec update are out of scope unless the change is a bug fix affecting no spec-described behavior."

---

### ✅ OPP-1-2: Add spec-update gate to workflow.md (Rule 1)
**Target:** .claude/rules/workflow.md
**Action:** Update
**Source topic:** S1.2 — Spec-Anchored definition
**Rationale:** Rule 1 says "read design.md before writing code" (spec as input). Spec-Anchored also requires updating the spec when behavior changes (spec as living document). The update side of the discipline is absent from the rules. As a result, Claude Code has no instruction to update specs after an implementation change — the project accumulates spec drift by default.
**Suggested content/change:** Extend Rule 1 with a sub-rule: "**After completing an implementation task that changes behavior described in the spec**, update the corresponding spec file (`requirements.md`, `design.md`, or both) to reflect the actual behavior. The spec is a living document — it is not filed away after initial implementation. A task that changes feature behavior is not complete until the spec reflects it."

---

### ✅ OPP-1-3: Add spec-drift detection to review.md checklist
**Target:** .claude/commands/review.md
**Action:** Update
**Source topic:** S1.1 — Spec-as-primary-artifact; S1.2 — Spec-Anchored maintenance discipline
**Rationale:** The review command doesn't cover spec-drift detection or spec vs code consistency checks. Spec-Anchored practice requires that reviews verify code and spec are in sync. Without this check, drift accumulates silently after every implementation cycle and is never caught at review time — which is the only systematic gate where it could be.
**Suggested content/change:** Add a "Spec consistency" checklist item to review.md: "If the task changed any behavior defined in `Docs/specs/[feature]/`, confirm the corresponding `requirements.md` and/or `design.md` were updated. Flag any spec file that describes behavior that no longer matches the implementation."

---

### ✅ OPP-1-4: Add cross-session context loss recovery guidance
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S1.3 — SDD extends TDD; AI agents start fresh every session; spec as persistent context
**Rationale:** S1.3 identifies a structural difference between TDD and SDD: TDD assumes a human implementer who carries context across sessions; SDD must encode that context in specs because AI agents start fresh every session. MyVocaList already has spec files for this purpose, but workflow.md has no rule for what to do when a session starts mid-task. Without guidance, a fresh session agent may make decisions inconsistent with prior work.
**Suggested content/change:** Add a new Rule (e.g., Rule 7 — Session Resume Protocol): "When resuming a task mid-way through implementation (new session, context compaction, subagent restart): (1) read the feature spec files (`requirements.md`, `design.md`, `tasks.md`) before doing anything else; (2) read the task-log to determine which tasks are complete; (3) check `git log --oneline -10` to see what was committed; (4) only then continue from the first unchecked task. Never infer task state from memory — always reconstruct from spec + log + git."

---

### ✅ OPP-1-5: Add atomicity guidance to task decomposition
**Target:** .claude/rules/workflow.md
**Action:** Update
**Source topic:** S1 — Four-phase workflow; Tasks phase produces "ordered, atomic implementation checklist"
**Rationale:** Rule 4 says "tasks.md is the source of truth" and Rule 3 says "commit after every task" but neither defines what makes a task atomic. S1's four-phase workflow specifies that the Tasks phase produces an "ordered, atomic implementation checklist." Without this definition, tasks in tasks.md vary wildly in scope, making commit discipline meaningless.
**Suggested content/change:** Add to Rule 4 (or as a sub-rule of Rule 3): "A task in `tasks.md` is atomic if: (1) it can be built independently after completion, (2) it produces a meaningful commit with no half-finished state, and (3) it takes no more than ~90 minutes of implementation work. If a tasks.md entry cannot satisfy these three conditions, split it before beginning. Tasks that span multiple files and layers (e.g., 'implement Singer CRUD') must be broken into sub-tasks at the layer boundary (Domain entities → Repository interface → Infra implementation → Service → ViewModel → Page)."

---

### ✅ OPP-1-6: Add When-to-Skip-SDD guidance for small tasks
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S1.3 — When to Skip SDD
**Rationale:** S1.3 identifies that SDD is not appropriate for "small, well-understood tasks," "quick fixes," or "solo exploration." MyVocaList's workflow.md currently mandates spec-first for all features with no threshold guidance. This creates friction for trivial changes (a single-field rename, a cosmetic fix, a dependency version bump).
**Suggested content/change:** Add a note to Rule 1: "**Skip spec-first for:** (a) bug fixes in a single file that do not change any specified behavior, (b) cosmetic/style changes (colors, spacing, typography), (c) dependency version bumps with no API changes, (d) refactoring that preserves observable behavior. **Always require spec-first for:** new features, behavior changes to existing features, architecture changes, any change touching Domain, Contracts, or Services interfaces."

---

### ✅ OPP-1-7: Add TDD-within-SDD integration rule to testing.md
**Target:** .claude/rules/testing.md
**Action:** Update
**Source topic:** S1.3 — SDD should embed TDD at its leaf nodes; the strongest workflows wrap TDD inside SDD
**Rationale:** testing.md describes TDD workflow (Red→Green→Refactor, mandatory from Step 4) but does not position TDD within the SDD structure. S1.3 is explicit that TDD operates within the SDD implementation phase — each task in tasks.md should use Red/Green/Refactor as its inner loop. Without this framing, a subagent implementing a task has no guidance on whether to write the test before or after the implementation.
**Suggested content/change:** Add a brief framing note at the top of the TDD Workflow section: "TDD operates inside the SDD implementation phase. Each task in `tasks.md` is the SDD unit; Red/Green/Refactor is the verification loop within that unit. When a task produces new service logic, ViewModel state, or repository queries, the test is written first (Red), then the minimum implementation to pass it (Green), then refactor. The task is not complete until the test passes."

---

## New Opportunities

### 🆕 OPP-1-8: Adopt Given/When/Then format for acceptance criteria in requirements.md
**Target:** .claude/rules/workflow.md (Spec structure table) and Docs/specs/venues/ (as reference template update)
**Action:** Add
**Source topic:** S1.3 — BDD/SDD integration; BDD is the collaborative foundation; SDD is the execution framework; mature SDD begins with Given/When/Then acceptance criteria
**Gap in current setup:** The spec structure table in workflow.md (Rule 1) lists `requirements.md` as covering "User stories, acceptance criteria, validation rules" but gives no format guidance. In practice, the acceptance criteria in existing specs (e.g., `Docs/specs/venues/requirements.md`) use freeform prose. S1.3 identifies that well-formed SDD specs start with Given/When/Then acceptance criteria, which are more machine-consumable and produce better AI generation quality — because the agent has an unambiguous behavioral contract rather than prose descriptions.
**Suggested content/change:** Add to Rule 1's spec structure description: "Acceptance criteria in `requirements.md` should use Given/When/Then format: `Given [context], When [action], Then [expected outcome]`. This format is machine-consumable, maps directly to test generation, and removes prose ambiguity from the spec. Use it for every user story. Apply retroactively to `Docs/specs/venues/requirements.md` as the reference implementation update."

---

### 🆕 OPP-1-9: Add spec-as-context briefing rule for subagents
**Target:** .claude/rules/workflow.md (Rule 2 — Subagent Delegation)
**Action:** Update
**Source topic:** S1.1 — Spec-as-primary-artifact; "Context for AI agents lives in the spec"; S1.3 — AI agents start fresh every session; spec provides persistent context
**Gap in current setup:** Rule 2's briefing protocol says "tell the subagent which files to read; let its own Read calls bring the content into its context." This is correct for preventing token duplication. However, Rule 2 does not specify that spec files (`requirements.md`, `design.md`) must always be included in the briefing file list — it currently only mentions "spec file paths" generically. S1.1 is explicit that the spec is the primary context source for agents; without the spec, the subagent infers intent from codebase patterns, leading to hallucination and architectural drift. This is a gap between the current briefing convention and the SDD principle.
**Suggested content/change:** Add to Rule 2 briefing protocol: "Every subagent briefing must include the feature's spec file paths as the first items in the file list (`requirements.md`, `design.md`, `tasks.md`). These are not optional context — they are the primary source of agent intent. Subagents that skip spec files will infer intent from code alone, which produces architecturally inconsistent output."

---

### 🆕 OPP-1-10: Introduce the rebuild test as a spec quality diagnostic
**Target:** CLAUDE.md (or .claude/rules/workflow.md as a new diagnostic tool reference)
**Action:** Add
**Source topic:** S1.2.1 — The Rebuild Test; "a concrete measure of spec quality"
**Gap in current setup:** There is no mechanism or prompt to assess whether MyVocaList's existing specs are generation-grade (complete enough to regenerate the feature from scratch). S1.2.1 defines the rebuild test: delete the codebase, provide only spec files and tests to a fresh agent, run regeneration, verify tests pass. Even informally applied (without actually deleting code), this mental model is a useful checklist for spec completeness before closing out a feature. The project has no guidance on spec quality measurement of any kind.
**Suggested content/change:** Add a note to the workflow's Spec structure section or CLAUDE.md continuous-enhancement guidance: "**Spec quality check (rebuild test):** When closing out a feature, ask: 'Could a fresh agent regenerate this feature from the spec files + test suite alone, without reading any existing implementation code?' If the answer is no, identify what is missing from the spec and fill the gaps. Common missing items: architectural decisions (why X was chosen over Y), business rule tradeoffs, integration contract details (what upstream entities return, what error shapes are expected)."

---

### 🆕 OPP-1-11: Define explicit DDD+SDD+TDD layering guidance
**Target:** CLAUDE.md (Architecture section or Roles section)
**Action:** Add
**Source topic:** S1.3 — DDD + SDD + TDD integration pattern; "the gold standard for complex enterprise systems"
**Gap in current setup:** CLAUDE.md references the `ddd-dotnet` skill for architecture patterns and `superpowers:test-driven-development` for TDD. But there is no guidance on how these three disciplines layer relative to each other on a feature. The result is that subagents may apply TDD without a spec (breaking SDD), or write a spec without respect for DDD bounded context boundaries, or run DDD-style modeling sessions after the code is already written. S1.3 provides the correct sequencing: DDD defines bounded contexts and ubiquitous language → SDD writes the spec within those boundaries → TDD verifies the generated implementation.
**Suggested content/change:** Add to CLAUDE.md under Architecture or Roles: "**Methodology layering:** (1) **DDD** defines what to build — bounded contexts, aggregate boundaries, ubiquitous language. Invoke `ddd-dotnet` skill at this layer. (2) **SDD** defines how it works — spec (requirements + design + tasks) within the DDD boundaries. Spec files in `Docs/specs/` capture this. (3) **TDD** verifies it is correct — Red/Green/Refactor within each SDD task. These layers are sequential, not interchangeable. Do not apply TDD before the SDD spec exists, and do not write an SDD spec without first confirming DDD boundaries."

---

### 🆕 OPP-1-12: Add spec change as the mandatory trigger for code change
**Target:** .claude/rules/workflow.md (Rule 1)
**Action:** Add
**Source topic:** S1.1 — Spec-as-primary-artifact: "when spec and code disagree, the spec wins"; S1.2 — Spec-Anchored: "PRs that change behavior are expected to update the spec"
**Gap in current setup:** The current Rule 1 mandates reading the spec before writing code but does not establish the causal direction: that a *spec change* must precede a *code change* for any behavior modification. This is the core SDD discipline. Without this rule, behavior changes can be requested directly as code tasks ("add validation to this field") without a spec update, producing cumulative spec drift. The project has no explicit "spec first, code second" causal rule — only "read spec before coding" (which is necessary but insufficient).
**Suggested content/change:** Add to Rule 1: "**Causal order:** A change to observable feature behavior must be expressed in the spec before it is expressed in code. The sequence is: (1) identify the spec clause that governs the behavior; (2) update that clause to reflect the intended new behavior; (3) then implement. Code that changes behavior without a preceding spec update is out of scope. This applies to additions, modifications, and removals of specified behavior. Bug fixes that restore intended behavior to match an existing spec clause do not require a spec update."

---

### 🆕 OPP-1-13: Add spec-review as a named review phase in the four-phase workflow
**Target:** .claude/rules/workflow.md (Rule 1)
**Action:** Update
**Source topic:** S1.1 — "Pull requests in mature SDD teams review spec changes. Code review becomes a secondary validation layer, not the primary gate."; S1 — Four-phase workflow human gates
**Gap in current setup:** The workflow Rule 1 describes the four phases (Brainstorm → Spec → Plan → Implement) and assigns the `/project:review` command at the end. But the review command is positioned as a post-implementation step, not as a gate at each phase transition. S1's four-phase workflow defines explicit human gates: "Review and approval before planning begins" (after Specify), "Review before task decomposition" (after Plan), "Review before implementation begins" (after Tasks). The current setup only has one review gate at the end of implementation. This misses the structural benefit of SDD: catching design mistakes before any code is written.
**Suggested content/change:** Update Rule 1 to make phase-gate reviews explicit: "Each phase transition requires Helder's review before proceeding: (1) after writing spec files — Helder reviews and approves before plan is written; (2) after writing the plan — Helder reviews and approves before tasks are decomposed; (3) after writing tasks.md — Helder reviews and approves before implementation begins; (4) after implementation — run `/project:review`. Do not proceed to the next phase without explicit approval. This is the primary mechanism that prevents spec mistakes from propagating into code."

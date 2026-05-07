# Conflict Report — review.md + testing.md Analysis
**Date:** 2026-05-06
**Analyst:** Agent B
**Scope:** `.claude/commands/review.md` and `.claude/rules/testing.md` — internal analysis + cross-file analysis + cross-reference against `conflict_report_workflow.md`

---

## Summary
**9 duplications, 4 contradictions, 5 inconsistencies, 5 structural issues**

Issues already captured in `conflict_report_workflow.md` are not repeated here. Where a finding extends or cross-references a workflow.md finding, it is noted.

---

## Duplications

### D-01: "Spec was updated when code deviates" checked in five separate review.md sections
- **Location 1:** §6 Spec Consistency — "confirm the corresponding `requirements.md` and/or `design.md` were updated" (🔴 Blocker)
- **Location 2:** §7 Spec Conformance — "No spec update skipped: If the implementation deviates from `design.md`, was `design.md` updated first?" (🔴 Blocker)
- **Location 3:** §8 Spec Alignment — "If implementation differs from spec: spec is updated through change control" (🔴 Blocker)
- **Location 4:** §10 Spec Drift Detection — "Were any design decisions reversed or altered? Update `design.md` Key Decisions if so." (🟡 Warning)
- **Location 5:** §12 Spec-Code Consistency — "If implementation diverged from the spec … has `design.md` been updated to reflect the actual design?" (🔴 Blocker)
- **Assessment:** All five check the same invariant: code change → spec update. The same rule is a Blocker in three sections and a Warning in a fourth. The fifth section (§12) is a near-duplicate of §6. A reviewer running all sections will check this at least five times.
- **Recommended action:** Consolidate into one canonical check in §6. Remove or collapse §8's and §12's redundant re-checks. In §7 and §10, add a cross-reference: "see §6 Spec Consistency."

### D-02: "Interface signatures match design.md" checked in three review.md sections
- **Location 1:** §6 — "do all interface signatures match what is in code?" (🟡)
- **Location 2:** §8 — "service interface signatures match design; no behaviors in design are absent from implementation" (🔴)
- **Location 3:** §12 — "Check key interfaces, data flow, and validation rules." (🟡)
- **Assessment:** Three sections check interface/signature alignment. The severity is inconsistent (🔴 in §8 vs 🟡 in §6 and §12) for what is effectively the same check.
- **Recommended action:** Keep one check in §6 at 🔴. Remove the redundant checks from §8 and §12.

### D-03: AC-to-test mapping required in three review.md sections
- **Location 1:** §6 — "each acceptance criterion verified by a test or a manual check?" (🟡)
- **Location 2:** §8 — "Every acceptance criterion has test coverage … not just `NotNull` assertions" (🔴)
- **Location 3:** §9 AC Traceability — full table: `| AC | Implementation location | Test that fails if AC is violated |`
- **Assessment:** The requirement to map each AC to a test appears three times, with escalating specificity. §9 is the canonical form (it includes the table format). §6 and §8 are weaker repetitions of §9's first row.
- **Recommended action:** Keep §9 as canonical. In §6 and §8, replace the AC coverage checks with a cross-reference: "see §9 AC Traceability."

### D-04: AC traceability matrix defined in THREE files with incompatible schemas
- **Location 1:** `review.md` §9 — columns: `AC | Implementation location | Test that fails if AC is violated`
- **Location 2:** `testing.md` "Traceability matrix (per feature)" — columns: `AC ID | Description | Test method`
- **Location 3:** `workflow.md` Rule 5 "AC traceability" — columns: `AC ref | Criterion (short) | Implementation evidence`
- **Assessment:** Three files define what appears to be the same artifact (the AC traceability table), but with different column schemas. A Tester subagent reading testing.md produces a table with `Test method`. The review checklist (review.md) expects `Test that fails if AC is violated`. The task-log format (workflow.md) expects `Implementation evidence`. These are different enough that none satisfies the other two automatically.
- **Recommended action:** Standardize on one schema across all three files. Suggested canonical: `| AC ID | Criterion | Implementation location | Test method |` — this satisfies all three purposes. Update all three files to use it. This supersedes and extends workflow.md I-03 (date placeholder inconsistency).

### D-05: "Builder must not modify tests" stated in two testing.md sections
- **Location 1:** "Builder Must Not Modify Tests" — dedicated section with full rationale
- **Location 2:** "Anti-Patterns — Never Do These" table — rows: "Modify a test to make it pass during Green phase" and "Delete a failing test instead of implementing the behavior"
- **Assessment:** The anti-patterns table repeats (more tersely) what the dedicated section states in full. Mild duplication; the dedicated section adds the escalation path ("stop, document the spec gap").
- **Recommended action:** In the anti-patterns table, keep the rows but add cross-reference: "see Builder Must Not Modify Tests." The rationale and escalation path should live in one place only.

### D-06: "TDD one test at a time" and exception stated in two testing.md sections
- **Location 1:** "One test at a time" section — rule + exception
- **Location 2:** "Tester/Builder Role Separation — Rules" item 1 — "Tester writes all tests for a task, confirms they compile and fail (Red)" implies all-at-once
- **Assessment:** Both sections describe the Tester's behavior when Tester/Builder split is in use — but they reach it from different starting points. The same exception (write all at once when Tester/Builder split is active) is implicit in Location 2 and explicit in Location 1. This is mild but forces readers to reconcile two descriptions.
- **Recommended action:** In "Tester/Builder Role Separation" rules, add a parenthetical reference: "(see One test at a time — exception applies here)."

### D-07: "To Review" conditions defined in review.md and testing.md independently
- **Location 1:** `review.md` — "A task is only `To Review` status when there are zero Blockers."
- **Location 2:** `testing.md` "Test Quality Audit Checklist" — "A test that fails one or more items must be fixed before the feature is marked `To Review`."
- **Assessment:** Both define preconditions for `To Review` status, but neither references the other. A subagent reading only review.md gets the zero-Blockers gate. A subagent reading only testing.md gets the test-quality gate. A subagent reading both must mentally AND the two lists. Neither file points to the other.
- **Recommended action:** review.md should explicitly note: "Test quality criteria per testing.md must also pass (see Test Quality Audit Checklist)." Alternatively, add a §13 in review.md that imports the testing.md audit checklist.

### D-08: "After Review" enhancement check in review.md vs "Session-End Spec Update Ritual" in workflow.md
- **Location 1:** `review.md` "After Review — Mandatory Enhancement Check" — asks whether to update devexpress-patterns.md, code-principles.md, dialogs-validation.md, CLAUDE.md, constraints-registry.md
- **Location 2:** `workflow.md` Rule 3a "Session-End Spec Update Ritual" — asks whether spec files (requirements.md, design.md, tasks.md) are current
- **Assessment:** Both rituals ask "what did we learn that should be persisted?" at task completion. They cover different destinations (rules/commands files vs spec files) so they are complementary, not duplicative — but neither cross-references the other. An agent performing one may not know to perform the other.
- **Recommended action:** In review.md "After Review," add a note: "Also run the Session-End Spec Update Ritual (workflow.md Rule 3a) to update spec files." In workflow.md Rule 3a, add a note: "Also run the After Review enhancement check (review.md) to update rules files."

### D-09: testing.md "Test Quality Audit Checklist — audit frequency" overlaps with review.md trigger
- **Location 1:** `testing.md` "Test Quality Audit Checklist — Audit frequency" — "Before setting a task to `To Review` in the task-log" and "During `/project:review` if test files were changed"
- **Location 2:** `review.md` preamble — "Post-task review. Run after EVERY completed task before committing."
- **Assessment:** testing.md ties the audit to two triggers; review.md triggers unconditionally on every task. The testing.md "if test files were changed" qualifier implies the audit is optional when no tests changed — but review.md runs unconditionally. These are inconsistent on when to apply the test quality audit.
- **Recommended action:** Align triggers: the test quality audit should run whenever `/project:review` runs (unconditional), not only when test files changed. The "if test files were changed" qualifier should be removed.

---

## Contradictions

### C-01: "One test at a time" vs "Tester writes ALL tests for a task" — within testing.md
- **Rule A:** "One test at a time" section — "Write and run **one test** before proceeding to the next. Do not write all tests for a service method in one batch."
- **Rule B:** "Tester/Builder Role Separation — Rules" item 1 — "The Tester subagent writes **all tests** for a task, confirms they compile and fail (Red), commits, and exits."
- **Exception:** An exception in "One test at a time" reads: "When the Tester/Builder split is used, the Tester writes all tests for a task together … The one-at-a-time discipline applies within a single-agent session."
- **Assessment:** The exception partially resolves the contradiction, but the exception appears only in the "One test at a time" section — a reader of "Tester/Builder Role Separation" who does not read the exception will believe the all-at-once rule is absolute. The two sections do not cross-reference each other.
- **Recommended action:** In "Tester/Builder Role Separation — Rules," add a cross-reference to the exception: "(Note: in a single-agent session, apply one-at-a-time discipline per 'One test at a time' section.)" This makes the conditional nature explicit in both places.

### C-02: Level C code "no test required" vs review.md §9 "any AC row with no test → task is INCOMPLETE"
- **Rule A:** `testing.md` TDD Level Guidance — Level C (DI registration, DTO records, trivial getters): "No mandatory test. Optional smoke test if needed for confidence."
- **Rule B:** `review.md` §9 — "If any AC row has no test → task is INCOMPLETE — return to implementation."
- **Assessment:** If a Level C task (e.g., DI registration) has an acceptance criterion in requirements.md — which is possible if the task was written with spec discipline — then testing.md says no test is needed but review.md says the task is incomplete without one. The contradiction arises in the intersection of Level C code and AC-bearing tasks.
- **Recommended action:** In review.md §9, add a clarification: "Exception: Level C code (see testing.md TDD Level Guidance) is exempt from mandatory test coverage. Document Level C classification in the task-log when no test exists for a listed AC." In testing.md Level C definition, add: "If a Level C task has ACs, document the no-test decision in the task-log — it will be scrutinized at review."

### C-03: workflow.md subagent exit checklist omits `/project:review` — but CLAUDE.md requires it after every task
- **Rule A:** `workflow.md` "Subagent exit checklist" — 8 steps: verification skill, build, test, post-edit re-read, living spec check, task-log, commit, push. No mention of `/project:review`.
- **Rule B:** `CLAUDE.md` Commands section — "Review: `/project:review` — run after every completed task."
- **Assessment:** The subagent exit checklist is the definitive list of what a subagent does before stopping. It does not include running `/project:review`. CLAUDE.md says run it after every task. A subagent following the exit checklist will skip the review command. A subagent following CLAUDE.md will run it. These are inconsistent.
- **Recommended action:** Add `/project:review` as step 1b (after `superpowers:verification-before-completion`) in the subagent exit checklist, or add a note: "Run `/project:review` before step 6 (task-log)." Alternatively, explicitly note that `/project:review` is a main-agent responsibility run after the subagent commits — but document this distinction.

### C-04: review.md severity model vs workflow.md "To Review" as a binary gate
- **Rule A:** `review.md` — three severity levels (🔴 Blocker, 🟡 Warning, 🟢 Suggestion). "A task is only `To Review` status when there are zero Blockers." Warnings may proceed with documented justification.
- **Rule B:** `workflow.md` "Intent verification before To Review" and "Subagent exit checklist" — build pass + demo statement verifiable + no scope bleed → `To Review`. No mention of the three-level severity system.
- **Assessment:** workflow.md treats `To Review` as a binary (build passed + demo verifiable). review.md introduces a three-level model where Warnings are allowed with justification. A subagent completing the exit checklist and setting `To Review` has not run review.md's severity-tiered analysis. The two gating models are inconsistent.
- **Recommended action:** workflow.md exit checklist step 1 (verification skill) should reference review.md's severity model explicitly: "A task with zero 🔴 Blockers per `/project:review` may be set `To Review`. Warnings must be documented in the task-log."

---

## Inconsistencies

### I-01: review.md sections 6–12 have no ownership hierarchy — which is canonical for spec consistency?
- **Occurrence:** Sections 6 (Spec Consistency), 7 (Spec Conformance), 8 (Spec Alignment), 10 (Spec Drift Detection), 11 (Drift Categories), 12 (Spec-Code Consistency) all address the same domain (does the code match the spec?) but use different framing, different severity levels for the same checks, and no cross-references.
- **Assessment:** A reviewer has no indication which section supersedes another when they overlap. For example: "spec updated" is 🔴 in §7 and §8 but 🟡 in §10 and §12 for the same type of change. This creates inconsistent outcomes depending on which sections a reviewer focuses on.
- **Recommended action:** Designate §6 as the canonical spec-consistency section (it has the cleanest scope). Convert §7, §8, §10, §12 to addendum sections that add items not in §6, and remove items already in §6. Add a header note: "Sections 7–12 are supplementary — §6 is the primary spec consistency check."

### I-02: testing.md "Mutation Testing" forward-references "TDD Level Guidance" that appears after it
- **Occurrence:** "Mutation Testing" section (line ~502) references "Level A feature (see TDD Level Guidance above)." The "TDD Level Guidance by Risk" section (line ~575) appears AFTER the Stryker section — not above it.
- **Assessment:** "Above" is factually incorrect — Level Guidance comes later in the file. A reader following the "above" instruction would search upward and not find it.
- **Recommended action:** Change "see TDD Level Guidance above" to "see TDD Level Guidance by Risk, below" — or reorder sections so Level Guidance precedes Stryker (since Stryker's behavior depends on understanding Levels A/B/C).

### I-03: testing.md status header references project phases that are now historical
- **Occurrence:** Opening status block — "Active from Step 3 (Venue CRUD Tests) onward. TDD applies to all new Services, ViewModels, and Repositories from AutocompleteField + Person CRUD forward."
- **Assessment:** The project is past Step 4. These step references are historical project-setup notes, not actionable rules. They create ambiguity: does "from Step 4+" mean TDD doesn't apply to code written before Step 4? Are there still pre-Step-4 areas where TDD is exempt?
- **Recommended action:** Remove the step-based preamble and replace with a single unconditional statement: "TDD applies to all new and modified Services, ViewModels, and Repositories." The historical context belongs in a git commit message or changelog, not in the active rules file.

### I-04: review.md spec note uses `[date]` while workflow.md standardized to `[YYYY-MM-DD]`
- **Occurrence 1:** `review.md` §8 — "spec is updated through change control … `> **Spec updated [date]:** ...`"
- **Occurrence 2:** `workflow.md` spec versioning discipline (already captured as I-03 in conflict_report_workflow.md) — standardized to `[YYYY-MM-DD]`
- **Assessment:** review.md's `[date]` placeholder is inconsistent with workflow.md's standardized `[YYYY-MM-DD]`. An agent copying from review.md will produce non-standard notes.
- **Recommended action:** Update review.md §8 to use `[YYYY-MM-DD]`. This cross-references the fix already recommended in workflow.md I-03.

### I-05: review.md "After Review" is flagged "not optional" but is not in the subagent exit checklist
- **Occurrence 1:** `review.md` "After Review — Mandatory Enhancement Check" — "This step is **not optional**."
- **Occurrence 2:** `workflow.md` subagent exit checklist (8 steps) — no mention of the enhancement check.
- **Assessment:** If the enhancement check is "not optional" but absent from the definitive exit checklist, subagents following the checklist will always skip it. The "not optional" claim is undermined by its absence from the enforcement mechanism.
- **Recommended action:** Add the enhancement check as a step in the exit checklist (e.g., step 5b, between living spec check and task-log), or explicitly assign it to the main agent post-review and note that in review.md: "The main agent performs this check; subagents complete their exit checklist and stop."

---

## Structural Issues

### S-01: review.md sections 6, 7, 8, 10, 11, 12 form an unstructured cluster with no logical progression
- **Issue:** Six consecutive numbered sections (with §9 as an island in the middle) all address spec-code consistency. There is no section that says "these are grouped" or explains why they are separate. A reader encounters them as a flat sequence, unaware that §10 and §12 largely duplicate §6.
- **Recommended action:** Group §6 through §12 under a single parent heading "## Spec and AC Verification" with sub-sections. Move §9 (AC Traceability) to immediately follow the AC-coverage checks in §6/§8 rather than stranding it between §8 and §10.

### S-02: testing.md has a double `---` separator between "Tester/Builder" and "TDD Workflow"
- **Issue:** Lines adjacent to the `---` separator between "Tester/Builder Role Separation" and "TDD Workflow (Red → Green → Refactor)" contain two consecutive `---` separators (lines 436–437 in the file as read). This is likely a copy-paste artifact.
- **Recommended action:** Remove the duplicate `---`.

### S-03: testing.md "Prerequisites Before Step 3" is historical setup guidance placed late in the file
- **Issue:** "Prerequisites Before Step 3 (Test Project Setup)" is a one-time setup checklist that references project bootstrapping. It is placed after "Running Tests" near the end of the file. A developer setting up the test project for the first time would encounter it only after reading the full testing reference. Additionally, the three listed prerequisites (AppDbContext tracking behavior, Console.WriteLine, Serilog drift) may be resolved now; the section has no status indicator.
- **Recommended action:** Either move "Prerequisites Before Step 3" to the top of the file (with a "completed — no action needed" status if resolved), or replace it with a note in git history and remove it from the active rules file. Rules files should describe current invariants, not historical setup checklists.

### S-04: review.md "After Review" section is visually separated as addendum but declared non-optional
- **Issue:** The "After Review — Mandatory Enhancement Check" section follows a `---` separator, placing it visually outside the numbered checklist. This layout communicates "optional addendum." The text immediately contradicts this by saying "This step is **not optional**." The structure and the content are in conflict.
- **Recommended action:** Move "After Review" inside the numbered checklist as a numbered section (e.g., §13 "Enhancement Check"), removing the post-separator placement that implies it is optional.

### S-05: testing.md "Mutation Testing with Stryker.NET" appears before "TDD Level Guidance by Risk" despite depending on it
- **Issue:** Stryker section references Level A features and uses Level A/B/C classification in its "Target mutation score" table. The Level Guidance section that defines A/B/C appears later in the file. A reader encountering Stryker first has no context for what "Level A method" means.
- **Recommended action:** Move "TDD Level Guidance by Risk" to before "Mutation Testing with Stryker.NET." Alternatively, add a forward reference at the top of the Stryker section: "See TDD Level Guidance by Risk (below) for A/B/C classification."

---

## Priority Summary

| ID | File(s) | Type | Severity | Recommended action |
|----|---------|------|----------|--------------------|
| D-04 | review.md + testing.md + workflow.md | Duplication | High | Standardize AC traceability matrix schema across all three files |
| C-03 | workflow.md + CLAUDE.md | Contradiction | High | Add `/project:review` to subagent exit checklist or explicitly assign to main agent |
| C-02 | testing.md + review.md | Contradiction | High | Reconcile Level C no-test policy with review.md §9 "any AC row needs a test" |
| D-01 | review.md | Duplication | High | Consolidate "spec updated when code deviates" into §6; remove from §7, §8, §10, §12 |
| C-01 | testing.md | Contradiction | High | Cross-reference one-at-a-time exception from Tester/Builder section |
| C-04 | review.md + workflow.md | Contradiction | Medium | Align three-severity review model with workflow.md binary To Review gate |
| D-03 | review.md | Duplication | Medium | Consolidate AC-to-test check; §9 is canonical — §6 and §8 should reference it |
| D-07 | review.md + testing.md | Duplication | Medium | Cross-reference To Review conditions between the two files |
| S-01 | review.md | Structural | Medium | Group §6–§12 under parent heading; reorder §9 |
| D-02 | review.md | Duplication | Medium | Consolidate interface-signatures check into one section at 🔴 severity |
| I-01 | review.md | Inconsistency | Medium | Designate §6 as canonical spec-consistency section; demote redundant checks in §7–§12 |
| I-05 | review.md + workflow.md | Inconsistency | Medium | Add enhancement check to exit checklist OR explicitly assign to main agent |
| D-08 | review.md + workflow.md | Duplication | Medium | Cross-reference After Review ↔ Session-End Spec Update Ritual |
| S-04 | review.md | Structural | Medium | Move "After Review" into numbered checklist as §13 |
| S-05 | testing.md | Structural | Medium | Move TDD Level Guidance before Mutation Testing |
| D-05 | testing.md | Duplication | Low | Cross-reference "Builder Must Not Modify Tests" from Anti-Patterns table |
| D-06 | testing.md | Duplication | Low | Cross-reference one-at-a-time exception from Tester/Builder Rules |
| D-09 | review.md + testing.md | Duplication | Low | Align audit frequency triggers: unconditional, not only when test files changed |
| I-02 | testing.md | Inconsistency | Low | Fix "above" reference in Stryker section — Level Guidance is below, not above |
| I-03 | testing.md | Inconsistency | Low | Remove project-phase preamble; replace with unconditional TDD statement |
| I-04 | review.md | Inconsistency | Low | Change `[date]` placeholder to `[YYYY-MM-DD]` (cross-ref workflow.md I-03) |
| S-02 | testing.md | Structural | Low | Remove duplicate `---` separator |
| S-03 | testing.md | Structural | Low | Move or retire "Prerequisites Before Step 3" historical setup section |

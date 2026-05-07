# Consolidated Conflict Report — MyVocaList SDD Governance Files
**Date:** 2026-05-06
**Synthesizer:** Agent D (final)
**Source reports:** conflict_report_workflow.md (Agent A), conflict_report_review_testing.md (Agent B), conflict_report_claude_principles.md (Agent C)
**Files analyzed:** workflow.md · review.md · testing.md · CLAUDE.md · code-principles.md · constraints-registry.md

---

## Decision Registry

> **How to use:** Review findings below (§ Findings by Target File). Record each decision here.
> **Decision values:** `Approved` · `Deferred` · `Rejected`
> **Status values:** `Pending` · `In Progress` · `Done`
> P11-04 subagents read this table to know what to apply. Only `Approved` + `Pending/In Progress` rows are acted on.

| ID | Sev | Target file | Decision | Rationale | Status |
|----|-----|-------------|----------|-----------|--------|
| B-01 | 🔴 | workflow.md | Pending | | Pending |
| B-02 | 🔴 | workflow.md | Pending | | Pending |
| B-03 | 🔴 | workflow.md | Pending | | Pending |
| B-04 | 🔴 | review.md + testing.md + workflow.md | Pending | | Pending |
| B-05 | 🔴 | review.md + workflow.md | Pending | | Pending |
| B-06 | 🔴 | testing.md + review.md | Pending | | Pending |
| B-07 | 🔴 | workflow.md + testing.md | Pending | | Pending |
| B-08 | 🔴 | CLAUDE.md | Pending | | Pending |
| B-09 | 🔴 | CLAUDE.md + code-principles.md | Pending | | Pending |
| W-01 | 🟡 | workflow.md | Pending | | Pending |
| W-02 | 🟡 | workflow.md | Pending | | Pending |
| W-03 | 🟡 | workflow.md | Pending | | Pending |
| W-04 | 🟡 | workflow.md | Pending | | Pending |
| W-05 | 🟡 | workflow.md | Pending | | Pending |
| W-06 | 🟡 | workflow.md | Pending | | Pending |
| W-07 | 🟡 | workflow.md | Pending | | Pending |
| W-08 | 🟡 | workflow.md | Pending | | Pending |
| W-09 | 🟡 | workflow.md | Pending | | Pending |
| W-10 | 🟡 | workflow.md | Pending | | Pending |
| W-11 | 🟡 | workflow.md + CLAUDE.md | Pending | | Pending |
| W-12 | 🟡 | workflow.md | Pending | | Pending |
| W-13 | 🟡 | review.md | Pending | | Pending |
| W-14 | 🟡 | review.md | Pending | | Pending |
| W-15 | 🟡 | workflow.md | Pending | | Pending |
| W-16 | 🟡 | review.md | Pending | | Pending |
| W-17 | 🟡 | review.md | Pending | | Pending |
| W-18 | 🟡 | review.md + workflow.md | Pending | | Pending |
| W-19 | 🟡 | review.md | Pending | | Pending |
| W-20 | 🟡 | testing.md | Pending | | Pending |
| W-21 | 🟡 | testing.md | Pending | | Pending |
| W-22 | 🟡 | code-principles.md + workflow.md | Pending | | Pending |
| W-23 | 🟡 | CLAUDE.md | Pending | | Pending |
| W-24 | 🟡 | CLAUDE.md + workflow.md | Pending | | Pending |
| W-25 | 🟡 | code-principles.md + constraints-registry.md | Pending | | Pending |
| W-26 | 🟡 | code-principles.md | Pending | | Pending |
| XF-01 | 🟡 | workflow.md | Pending | | Pending |
| XF-02 | 🟡 | workflow.md | Pending | | Pending |
| S-01 | 🟢 | workflow.md | Pending | | Pending |
| S-02 | 🟢 | workflow.md | Pending | | Pending |
| S-03 | 🟢 | workflow.md | Pending | | Pending |
| S-04 | 🟢 | workflow.md | Pending | | Pending |
| S-05 | 🟢 | workflow.md | Pending | | Pending |
| S-06 | 🟢 | workflow.md | Pending | | Pending |
| S-07 | 🟢 | workflow.md | Pending | | Pending |
| S-08 | 🟢 | workflow.md + review.md | Pending | | Pending |
| S-09 | 🟢 | workflow.md | Pending | | Pending |
| S-10 | 🟢 | workflow.md | Pending | | Pending |
| S-11 | 🟢 | workflow.md | Pending | | Pending |
| S-12 | 🟢 | workflow.md | Pending | | Pending |
| S-13 | 🟢 | workflow.md | Pending | | Pending |
| S-14 | 🟢 | review.md | Pending | | Pending |
| S-15 | 🟢 | review.md | Pending | | Pending |
| S-16 | 🟢 | testing.md + review.md | Pending | | Pending |
| S-17 | 🟢 | testing.md | Pending | | Pending |
| S-18 | 🟢 | testing.md + CLAUDE.md | Pending | | Pending |
| S-19 | 🟢 | testing.md | Pending | | Pending |
| S-20 | 🟢 | testing.md | Pending | | Pending |
| S-21 | 🟢 | testing.md | Pending | | Pending |
| S-22 | 🟢 | testing.md | Pending | | Pending |
| S-23 | 🟢 | CLAUDE.md | Pending | | Pending |
| S-24 | 🟢 | CLAUDE.md + code-principles.md | Pending | | Pending |
| S-25 | 🟢 | CLAUDE.md + code-principles.md | Pending | | Pending |
| S-26 | 🟢 | CLAUDE.md + code-principles.md | Pending | | Pending |
| S-27 | 🟢 | CLAUDE.md | Pending | | Pending |
| S-28 | 🟢 | code-principles.md + constraints-registry.md | Pending | | Pending |
| S-29 | 🟢 | code-principles.md | Pending | | Pending |
| S-30 | 🟢 | code-principles.md + constraints-registry.md | Pending | | Pending |
| S-31 | 🟢 | code-principles.md | Pending | | Pending |
| S-32 | 🟢 | constraints-registry.md | Pending | | Pending |
| S-33 | 🟢 | constraints-registry.md | Pending | | Pending |
| XF-03 | 🟢 | workflow.md | Pending | | Pending |

---

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Blocker | 9 |
| 🟡 Warning | 24 |
| 🟢 Suggestion | 28 |
| **Total (after de-dup)** | **61** |

### Breakdown by target file

| Target file | Blockers | Warnings | Suggestions | Total |
|-------------|----------|----------|-------------|-------|
| workflow.md | 3 | 12 | 13 | 28 |
| review.md | 2 | 7 | 4 | 13 |
| testing.md | 2 | 2 | 5 | 9 |
| CLAUDE.md | 2 | 3 | 5 | 10 |
| code-principles.md | 0 | 2 | 4 | 6 |
| constraints-registry.md | 0 | 0 | 2 | 2 |
| Multiple / cross-file | — (captured under primary target) | | | |

### De-duplications applied

| Merged finding | Source entries | Resolution |
|----------------|----------------|------------|
| `/project:review` missing from subagent exit checklist | review_testing C-03 + CLAUDE.md C-03 | One entry (B-07) — CLAUDE.md C-03 extends it with the "spec/plan file" trigger; both treated as one Blocker |
| Rebuild test in three locations | workflow D-12 + CLAUDE.md I-04 | One entry (W-11) — third instance in CLAUDE.md makes it more urgent than a pure workflow.md internal issue |
| Step 3 historical references | testing.md I-03 + CLAUDE.md D-09 | Separate entries (S-20, S-21) — they affect different files and must be fixed together |
| AC traceability schema | review_testing D-04 covers workflow.md Rule 5 schema | One entry (B-04) — target is all three files simultaneously |

---

## Findings by Target File

---

### workflow.md — 28 findings

#### 🔴 Blockers

**B-01 — Verbatim contracts vs. paths-only briefing (C-04 in Agent A)**
- **Rule A:** "Briefing protocol — paths only, never paste content" — "must reference file paths, not paste file content inline."
- **Rule B:** "Wave handoff — inject actual contracts for new artifacts" — "Include these extracted signatures verbatim in the next wave's briefing."
- **Impact:** A subagent following one rule and ignoring the other will either paste too much or omit critical contract signatures. These two sections directly contradict.
- **Fix:** Add an explicit exception to Briefing protocol: "Exception: committed interface/DTO signatures produced in the previous wave may be included verbatim under a `## Contracts from previous wave` block — these are bounded committed code, not rule file content." Cross-reference Wave handoff section.

**B-02 — `tasks.md` required vs. not required for small isolated changes (C-02 in Agent A)**
- **Rule A:** "When to skip SDD" table — "Small isolated change (< 1 hour, single file, no interface change) → Spec required: No → Minimum artifact: Descriptive commit message."
- **Rule B:** "Spec ceremony calibration table" — same task type → "Ceremony level: Light → Required artifacts: `tasks.md` entry + commit message."
- **Impact:** Contradictory guidance forces an agent to pick one rule and ignore the other. A subagent following the bypass rule skips tasks.md; a subagent following the calibration table adds an unnecessary entry.
- **Fix:** Reconcile to one answer: `tasks.md` entry required only when the task is part of an active feature plan. Update the calibration table's Light-ceremony row to add "(only if task is tracked in an active feature plan)."

**B-03 — Spec rot check frequency: every wave vs. every second wave (C-05 in Agent A)**
- **Rule A:** "Multi-wave checkpoint pattern" — "the main agent must perform a multi-wave checkpoint after every second wave."
- **Rule B:** "Spec rot — detection and prevention / Prevention protocol" step 1 — "After every wave: run the spec rot check (re-read spec + compare against committed code)."
- **Impact:** An agent following B-03-A will skip the check on odd-numbered waves. An agent following B-03-B will run it every wave. The frequency difference will cause undetected drift on whichever schedule is not followed.
- **Fix:** Clarify explicitly: the lightweight spec rot indicators check (5 bullets) runs after every wave; the deep multi-wave checkpoint protocol runs every second wave. Add a one-sentence distinction to each section referencing the other.

#### 🟡 Warnings

**W-01 — "Re-read spec before briefing subagent" in three locations (D-01 in Agent A)**
- Locations: "Mandatory spec reads at session start" (Rule 2), "Context reset discipline for orchestrator" (Rule 2), Rule 7 Session Start Protocol steps 4–6.
- Fix: Rule 7 is canonical. Replace content in the other two with cross-references to Rule 7.

**W-02 — Build retry cap qualifier inconsistency (D-02 in Agent A)**
- "Kill criteria" adds the qualifier "with no diagnostic improvement" to the 3-attempt cap; "Build retry cap" definition omits it. The 3-attempt cap and the 3-dispatch escalation protocol use similar "3 tries" framing for different things.
- Fix: Keep "Build retry cap" as the definition. Remove the qualifier from Kill criteria or add it to the cap definition. Rename "3-strike protocol" to "3-dispatch escalation protocol" to distinguish it from build retries.

**W-03 — Subagent spec-write permissions inconsistent across three sections (C-03 in Agent A)**
- "Spec ownership constraint" exception permits one-line spec update notes; "Living spec protocol" instructs subagents to write back to design.md Key Decisions; Rule 2a says Helder approves all spec content.
- Fix: Add the authorization qualifier to Living spec protocol: "write back only decisions within the task's authorized scope, per Spec ownership constraint." Align Rule 2a to explicitly permit the one-line note pattern as a named exception.

**W-04 — EF migration CLI ownership contradiction (C-01 in Agent A)**
- Rule 2 says "All coding is done by subagents" but the main-agent column lists `dotnet ef migrations add` (a code-generating command).
- Fix: Add a footnote: "`dotnet ef migrations add` generates the scaffold via CLI (main agent shell); the subagent then edits the migration file."

**W-05 — "Two-tier spec trigger" threshold conflict with SDD decision table (I-05 in Agent A)**
- "Spec size calibration" says ≥ 2 layers → full three-file spec. "SDD decision table" says ≥ 2 layers → write design.md only.
- Fix: Reconcile to one answer. If ≥ 2 layers always means all three files, update the decision table row. Document the authoritative threshold once.

**W-06 — Verifier optional vs. mandatory for Architectural lane (I-06 in Agent A)**
- "Verifier subagent" section says "may be dispatched" (optional). Rule 2b says Architectural tasks "require … Verifier subagent" (mandatory).
- Fix: Restate the Verifier section: "Optional except for Architectural review-lane tasks, where it is mandatory (see Rule 2b)."

**W-07 — Rule numbering non-sequential; reading order is broken (I-01 + S-01 in Agent A)**
- File section order: Rule 1, 2, 3, 2a, 2b, 3a, 4, 8, 7, 6, 5. A reader cannot navigate by number.
- Fix: Renumber sequentially and reorder sections. Absorb 2a/2b into Rule 2, 3a into Rule 3.

**W-08 — Session management sections stranded inside Rule 2 (S-03 + D-13/14 in Agent A)**
- ACTIVE-CONSIDERATIONS.md, Multi-session state handoff, Context exhaustion warning signs, Context reset discipline, Wave completion discovery briefs — all appear under Rule 2 (Subagent Delegation) but govern orchestrator/session behavior.
- Fix: Move all five sections to Rule 7 or a new "Session Management" rule. Reduces Rule 2 length by ~30% and makes it focused on subagent mechanics.

**W-09 — "Mandatory spec reads at session start" stranded inside Rule 2 (D-13 + S-02 in Agent A)**
- The section governs session-start behavior but is placed mid-Rule-2 with a horizontal rule separator, making it look orphaned.
- Fix: Move content to Rule 7 and merge with Session Start reading order.

**W-10 — Spec size calibration table and Spec ceremony calibration table overlap (D-06 + D-07 + S-05 in Agent A)**
- Both tables answer the same question (how much spec work for this task size) with different category vocabularies and different requirement columns. "When to skip SDD" further overlaps.
- Fix: Merge all three into one decision table: columns for task type, estimated effort, spec required (Y/N), ceremony level, required artifacts. Place it near the top of Rule 1 (before detailed spec guidance).

**W-11 — Rebuild test / Regeneration test in three locations (D-12 in Agent A + I-04 in Agent C)**
- workflow.md has both "Regeneration test practice" (threshold >2 contradictions) and "Rebuild test — feature close-out spec quality check" (4-level scale). CLAUDE.md has a third instance with "test suite" as a context source not mentioned in the workflow.md protocol.
- Fix: Remove "Regeneration test practice" from workflow.md. Keep "Rebuild test" as the canonical protocol; add "and the test suite" to its context sources (as CLAUDE.md correctly states). Make CLAUDE.md's entry a pointer only.

**W-12 — Rule 2a and 2b appear after Rule 3, breaking logical flow (S-06 in Agent A)**
- Rule 2a (Approval Authority Matrix) and Rule 2b (Review SLA) logically extend Rule 2 but appear after Rule 3 in the file.
- Fix: Move Rule 2a and 2b to immediately follow Rule 2, or absorb as named subsections within Rule 2.

#### 🟢 Suggestions

**S-01 — "Spec gap → blocked: spec gap" stated in two sections (D-03 in Agent A)**
- "Spec gap escalation" and "Pre-task context gate" both define the same action.
- Fix: Keep "Spec gap escalation" as canonical. In "Pre-task context gate," add: "per Spec Gap Escalation protocol."

**S-02 — findings.md defined in three places (D-05 in Agent A)**
- Spike pattern, Discovery mode, and "findings.md — session artifact" all describe the same artifact.
- Fix: Remove inline content lists from Spike pattern and Discovery mode; reference the canonical section.

**S-03 — "Subagents must not write specs" in three places (D-04 in Agent A)**
- "Spec ownership constraint," "Subagent scope constraint," and Rule 2a approval matrix all restate the prohibition.
- Fix: Keep "Spec ownership constraint" as canonical. Add cross-references from the other two.

**S-04 — Post-wave verification stated in three places (D-10 in Agent A)**
- "Post-wave verification," "When to take back control," and Rule 2b all cover main-agent post-wave build runs.
- Fix: "When to take back control" should be removed or merged into "Post-wave verification." Rule 2b can cross-reference.

**S-05 — Demo statement requirement referenced in three places (D-11 in Agent A)**
- Defined in "Demo statement requirement" (Rule 1), referenced in "Task atomization checklist" and "Task entry format."
- Fix: Acceptable as-is; add explicit cross-references from Rules 4 and the entry format to the Rule 1 definition.

**S-06 — Session resume rule stated twice (D-08 in Agent A)**
- "Multi-session state handoff protocol" and Rule 7 Session Start both say "read handoff file first."
- Fix: Remove "Session resume rule" sentence from handoff protocol; add "See Rule 7 — Session Start Protocol."

**S-07 — Commit+push duplicated in return protocol and exit checklist (D-09 in Agent A)**
- Both sections enumerate the same commit+push requirement.
- Fix: In "Subagent return protocol," replace steps 2–3 with "Follow Subagent exit checklist steps 7–8."

**S-08 — "Spec updated [YYYY-MM-DD]" date placeholder inconsistency (I-03 in Agent A)**
- "Spec versioning discipline" and "Living spec protocol" use `[YYYY-MM-DD]`; "Spec ownership constraint" uses `[date]`.
- Fix: Standardize to `[YYYY-MM-DD]` everywhere in workflow.md (see also review.md S-14).

**S-09 — "Spec updated" note authorized by subagent only for explicitly authorized decisions (C-03 in Agent A)**
- See W-03 above; structurally the "Living spec protocol" should state the qualifier.

**S-10 — Hook health verification not in Rule 7 session start reading order (S-07 in Agent A)**
- "Hook health verification" describes session-start behavior but is only in "Hook Enforcement Notes" at the top of the file.
- Fix: Add as step 0 in Rule 7 session start reading order, or add cross-reference there.

**S-11 — "Subagent exit checklist" placement mid-Rule 2 (S-08 in Agent A)**
- The checklist is the last action a subagent takes but appears mid-file before Kill criteria, Build retry cap, etc.
- Fix: Move to the last subsection of Rule 2, immediately before the Rule 3 separator.

**S-12 — "When to skip SDD" placed at end of Rule 1 material (S-04 in Agent A)**
- A bypass rule should appear early in Rule 1, before the reader invests in detailed spec guidance that may not apply.
- Fix: Move to immediately after the SDD Invariant at the start of Rule 1.

**S-13 — Rule 2a/2b placement (already captured as W-12)**

---

### review.md — 13 findings

#### 🔴 Blockers

**B-04 — AC traceability matrix incompatible schemas across three files (D-04 in Agent B)**
- **Location 1:** review.md §9 — columns: `AC | Implementation location | Test that fails if AC is violated`
- **Location 2:** testing.md "Traceability matrix (per feature)" — columns: `AC ID | Description | Test method`
- **Location 3:** workflow.md Rule 5 "AC traceability" — columns: `AC ref | Criterion (short) | Implementation evidence`
- **Impact:** A Tester subagent reading testing.md produces a table that fails to satisfy the review.md checklist. A subagent reading workflow.md produces a table that satisfies neither. Review gates that check "does a traceability matrix exist?" will pass on all three variants, but cross-file validation will fail.
- **Fix:** Standardize one schema across all three files: `| AC ID | Criterion (short) | Implementation location | Test method |`. This column set satisfies all three purposes. Update review.md §9, testing.md "Traceability matrix," and workflow.md Rule 5 to use identical column headers.

**B-05 — "After Review" enhancement check mandatory but absent from subagent exit checklist (I-05 in Agent B + C-03 extended)**
- review.md "After Review — Mandatory Enhancement Check" is explicitly "not optional." The subagent exit checklist (workflow.md, 8 steps) does not mention it. If the enhancement check belongs to the subagent, the exit checklist is incomplete. If it belongs to the main agent, review.md does not say so.
- **Impact:** Subagents following the exit checklist will always skip a "not optional" step.
- **Fix:** Either add the enhancement check as step 5b in the exit checklist (between living spec check and task-log), or explicitly assign it to the main agent with a note in review.md: "This step is performed by the main agent after the subagent commits and stops."

#### 🟡 Warnings

**W-13 — "Spec updated when code deviates" checked in five review.md sections (D-01 in Agent B)**
- Sections §6, §7, §8, §10, and §12 all check the same invariant: code change → spec update. Severity is 🔴 in §6, §7, §8 and 🟡 in §10, §12 for the same type of change.
- Fix: Keep one canonical check in §6 at 🔴. Replace the redundant checks in §7, §8, §10, §12 with cross-reference: "see §6 Spec Consistency."

**W-14 — review.md §6–§12 form an unstructured cluster with no ownership hierarchy (I-01 + S-01 in Agent B)**
- Six sections address spec-code consistency with overlapping scope and inconsistent severities. No section indicates which is canonical when they conflict.
- Fix: Group §6–§12 under a "## Spec and AC Verification" parent heading. Designate §6 as the primary check; make §7–§12 addendum sections that add non-overlapping items only. Move §9 to immediately follow AC coverage checks in §6.

**W-15 — review.md three-severity model inconsistent with workflow.md binary `To Review` gate (C-04 in Agent B)**
- review.md: 🔴/🟡/🟢 with "zero Blockers required for To Review." workflow.md exit checklist and Intent verification treat To Review as binary (build pass + demo verifiable).
- Fix: workflow.md exit checklist step 1 (verification skill) should explicitly reference review.md's severity model: "A task with zero 🔴 Blockers per `/project:review` may be set `To Review`. 🟡 Warnings must be documented in the task-log."

**W-16 — "Interface signatures match design.md" checked at inconsistent severities in three sections (D-02 in Agent B)**
- §6 (🟡), §8 (🔴), §12 (🟡) — same check, three different severities.
- Fix: Consolidate into §6 at 🔴. Remove from §8 and §12.

**W-17 — review.md and testing.md define `To Review` preconditions independently without cross-referencing (D-07 in Agent B)**
- review.md: zero Blockers. testing.md: test quality audit passed. A subagent reading only one file gets an incomplete gate.
- Fix: Add to review.md: "Test quality criteria per testing.md must also pass (see Test Quality Audit Checklist)." Or add §13 in review.md that imports the testing.md audit checklist.

**W-18 — review.md "After Review" and workflow.md Rule 3a session-end ritual don't cross-reference each other (D-08 in Agent B)**
- Both ask "what did we learn that should be persisted?" but target different files (rules/commands vs spec files). Neither references the other, so an agent performing one may not know to perform the other.
- Fix: In review.md "After Review," add: "Also run the Session-End Spec Update Ritual (workflow.md Rule 3a) to update spec files." In workflow.md Rule 3a, add: "Also run the After Review enhancement check (review.md) to update rules files."

**W-19 — AC-to-test mapping required in three review.md sections (D-03 in Agent B)**
- §6 (🟡), §8 (🔴), §9 (full table format). §9 is canonical; §6 and §8 are weaker repetitions.
- Fix: Keep §9 as canonical. In §6 and §8, replace AC coverage checks with "see §9 AC Traceability."

#### 🟢 Suggestions

**S-14 — review.md uses `[date]` placeholder vs workflow.md standardized `[YYYY-MM-DD]` (I-04 in Agent B)**
- review.md §8 uses `> **Spec updated [date]:**` while workflow.md uses `[YYYY-MM-DD]`.
- Fix: Update review.md §8 to `[YYYY-MM-DD]`. (Companion to S-08.)

**S-15 — "After Review" section visually outside numbered checklist despite being mandatory (S-04 in Agent B)**
- The section follows a `---` separator communicating "optional addendum." The text says "not optional."
- Fix: Promote "After Review" to a numbered section within the checklist (e.g., §13 "Enhancement Check").

**S-16 — review.md test quality audit trigger inconsistent (D-09 in Agent B)**
- testing.md says audit runs "if test files were changed" (conditional). review.md says run after every task (unconditional).
- Fix: Align to unconditional. Remove the "if test files were changed" qualifier from testing.md.

**S-17 — Builder Must Not Modify Tests stated in two testing.md sections (D-05 in Agent B)**
- Dedicated section + anti-patterns table repeat the same prohibition.
- Fix: In the anti-patterns table, add cross-reference to the dedicated section for escalation path.

---

### testing.md — 9 findings

#### 🔴 Blockers

**B-06 — Level C "no test required" vs review.md §9 "any AC row needs a test" (C-02 in Agent B)**
- testing.md Level C: DI registration, trivial getters — "No mandatory test." review.md §9: "If any AC row has no test → task is INCOMPLETE."
- **Impact:** A Level C task that has an acceptance criterion in requirements.md satisfies neither rule: testing.md says no test needed, review.md says the task is incomplete. The agent cannot resolve this without guidance.
- **Fix:** In review.md §9, add: "Exception: Level C code (per testing.md TDD Level Guidance) is exempt from mandatory test coverage. Document the Level C classification in the task-log when no test is written for a listed AC." In testing.md Level C definition, add: "If a Level C task has ACs, document the no-test decision in the task-log — it will be scrutinized at review."

**B-07 — `/project:review` missing from subagent exit checklist (C-03 in Agent B, extended by CLAUDE.md C-03)**
- CLAUDE.md Commands section: "`/project:review` — run after every completed task **and after creating or updating any spec or plan file**."
- workflow.md subagent exit checklist (8 steps): no mention of `/project:review`.
- The "spec or plan file" trigger (CLAUDE.md extension) is additionally not placed in any workflow mechanism.
- **Impact:** Subagents following the exit checklist will always skip `/project:review`. The "spec/plan file" trigger has no enforcement mechanism anywhere.
- **Fix:** Add `/project:review` to the exit checklist (before step 6 task-log). In workflow.md Rule 3a, add the "spec or plan file" trigger as a run condition. If `/project:review` is a main-agent responsibility only, explicitly document this distinction in both workflow.md and review.md.

#### 🟡 Warnings

**W-20 — "One test at a time" vs "Tester writes ALL tests for a task" — cross-reference missing (C-01 in Agent B)**
- The exception (write all at once in Tester/Builder split) exists in "One test at a time" but not in "Tester/Builder Role Separation." A reader of the latter section will apply all-at-once as the absolute rule.
- Fix: In "Tester/Builder Role Separation — Rules" item 1, add: "(Note: in a single-agent session, apply one-at-a-time discipline per 'One test at a time — Exception.')."

**W-21 — testing.md "Mutation Testing" forward-references TDD Level Guidance that appears after it (I-02 in Agent B)**
- Stryker section says "see TDD Level Guidance above" — the Level Guidance section appears below.
- Fix: Reorder sections so Level Guidance precedes Stryker. Or change "above" to "below."

#### 🟢 Suggestions

**S-18 — testing.md Step-based preamble is historical noise (I-03 in Agent B)**
- "Active from Step 3 (Venue CRUD Tests) onward … from AutocompleteField + Person CRUD forward." The project is past these steps.
- Fix: Replace with one unconditional statement: "TDD applies to all new and modified Services, ViewModels, and Repositories." Historical context belongs in git history.

**S-19 — testing.md "Prerequisites Before Step 3" is a historical setup checklist placed late in the file (S-03 in Agent B)**
- Three listed prerequisites (AppDbContext tracking, Console.WriteLine, Serilog drift) may already be resolved. No status indicator.
- Fix: Move to the top with a "Status: [resolved / pending]" header, or remove entirely and archive to git history.

**S-20 — testing.md duplicate `---` separator (S-02 in Agent B)**
- Double `---` between "Tester/Builder Role Separation" and "TDD Workflow" sections — copy-paste artifact.
- Fix: Remove one `---`.

**S-21 — testing.md Stryker section should follow TDD Level Guidance (S-05 in Agent B)**
- Stryker uses Level A/B/C classification but Level Guidance appears after Stryker.
- Fix: Move TDD Level Guidance before Mutation Testing / Stryker section.

**S-22 — testing.md "One test at a time" exception should appear in both sections (D-06 in Agent B)**
- "Tester/Builder Role Separation" should explicitly reference the exception in "One test at a time" for a single-agent session (companion to W-20).

---

### CLAUDE.md — 10 findings

#### 🔴 Blockers

**B-08 — Constitutional Constraints and Unamendable constraints overlap with no stated selection criterion (C-01 in Agent C)**
- Constitutional Constraints: 6 items (Language, Native dialogs, UI Component Priority, MD3 terminology, SafeAreaEdges, Incremental edits). Unamendable constraints: 3 items (Business logic in Services, Never use DisplayAlert, DevExpress first).
- The Unamendable list contains 2 of the 6 Constitutional Constraints and 1 item not in Constitutional Constraints. No rule explains the asymmetry.
- **Impact:** An agent cannot determine: (a) why Language, MD3 terminology, SafeAreaEdges, and Incremental edits are Constitutional but not Unamendable; (b) why "Business logic in Services" is Unamendable but not Constitutional. The categories are meaningless without a stated selection criterion.
- **Fix:** Define the relationship explicitly. Recommended: extend Constitutional Constraints to include "Business logic in Services." Mark the 3 Unamendable items with `[Unamendable — requires architecture review]` inline. Remove the separate Unamendable subsection (it becomes redundant once markers are added). Add one sentence explaining the Unamendable tier.

**B-09 — Architecture constraints duplicated verbatim across CLAUDE.md and code-principles.md (D-01 + D-05 in Agent C)**
- CLAUDE.md § Architecture and code-principles.md § Architecture Constraints list near-identical rules. "Business logic in Services" appears in 4 total locations (CLAUDE.md Architecture, CLAUDE.md Constitutional Role, CLAUDE.md Unamendable, code-principles.md).
- **Impact:** Any update to one file risks missing the others, causing drift between authoritative sources. Agents load both files and see redundant statements with no indication of which is canonical.
- **Fix:** CLAUDE.md § Architecture is canonical. In code-principles.md § Architecture Constraints, replace the list with: "Architecture layer constraints are defined in `CLAUDE.md § Architecture` — they apply equally to code." Apply same pattern to "Business logic in Services" in the other three locations: keep the CLAUDE.md Architecture statement; make all others explicit references.

#### 🟡 Warnings

**W-22 — "Spec Language — Determinism" is a spec-writing rule stranded in a coding standards file (C-02 in Agent C)**
- code-principles.md § "Spec Language — Determinism" governs what agents write in requirements.md and design.md — not in .cs files. workflow.md is the canonical home for spec-writing rules. An agent loading code-principles.md for coding guidance will encounter spec quality rules; an agent loading workflow.md for spec guidance will miss this rule.
- Fix: Move § "Spec Language — Determinism" to workflow.md Rule 1, adjacent to "Acceptance criteria format" or "Spec quality four-gate review." In code-principles.md, add a one-line reference: "Spec language determinism (prohibited vague terms): see `workflow.md § Spec quality four-gate review`."

**W-23 — CLAUDE.md `Continuous Enhancement` and `Amending These Rules` describe overlapping update processes without cross-referencing (I-05 in Agent C)**
- Continuous Enhancement: informal ongoing "after every task, add/update/replace/delete rules." Amending These Rules: formal process with `amend:` commit prefix and changelog entry. Neither references the other. An agent following only Continuous Enhancement will make rule changes without the required changelog entry.
- Fix: Add to Continuous Enhancement: "Note: changes to CLAUDE.md or `.claude/rules/*.md` must follow the Amending These Rules process (see §Amending These Rules below) — including `amend:` commit prefix and changelog entry."

**W-24 — CLAUDE.md `§ Spec Quality Check` describes the rebuild test inconsistently with workflow.md (I-04 in Agent C, extends workflow W-11)**
- CLAUDE.md includes "test suite" as a context source for the rebuild test; workflow.md does not. CLAUDE.md correctly says "See `workflow.md` for the full rebuild test protocol" but the workflow.md protocol is incomplete without the test suite context.
- Fix: Add "and the test suite" to workflow.md Rebuild test context sources. Make CLAUDE.md's entry a pointer only: "When closing out a feature, run the Rebuild Test (see `workflow.md § Rebuild test`). Include the test suite alongside the spec files."

#### 🟢 Suggestions

**S-23 — "DisplayAlert prohibition" and "DevExpress first" restated in Unamendable list (D-03 + D-04 in Agent C)**
- Constitutional Constraints is the full statement; the Unamendable list repeats them tersely with no added content.
- Fix: Resolved by B-08's action (merge the two sections). If kept separate, Unamendable entries should read: "See Constitutional Constraints — Native dialogs rule" and "See Constitutional Constraints — UI Component Priority."

**S-24 — "English only" rule duplicated across CLAUDE.md and code-principles.md (D-02 in Agent C)**
- CLAUDE.md § Constitutional Constraints and code-principles.md § Language say the same thing.
- Fix: Remove code-principles.md § Language. Add at top of code-principles.md: "Language rule: English only — see `CLAUDE.md § Constitutional Constraints`."

**S-25 — CLAUDE.md § Constitutional Role has no file+section pointers for verification items (S-01 in Agent C)**
- "Verify naming conventions, DI registration rules, error handling idioms" with no pointers to where these are in code-principles.md.
- Fix: Add inline references: "Naming conventions — see `code-principles.md § C# Style / Naming`", "DI registration — see `code-principles.md § DI Registration Conventions`", etc.

**S-26 — "Prefer composition over inheritance" stranded in CLAUDE.md § Architecture (I-01 in Agent C)**
- All other coding principles are in code-principles.md. This design principle is the only stylistic rule in the Architecture section.
- Fix: Move to code-principles.md § C# Style or a new "Design Principles" subsection. Remove from CLAUDE.md Architecture.

**S-27 — CLAUDE.md reference to testing.md contains obsolete "prerequisites for Step 3" phrase (D-09 in Agent C, companion to S-18)**
- CLAUDE.md § Rules Files: "testing.md — read before writing any test or setting up the test project. Covers test types, naming, TDD workflow, and **prerequisites for Step 3**."
- Fix: Update to: "testing.md — read before writing any test. Covers test types, naming, TDD workflow, and test project setup."

---

### code-principles.md — 6 findings

#### 🟡 Warnings

**W-25 — EF Core / SQLite constraints duplicated across code-principles.md and constraints-registry.md (D-06 in Agent C)**
- code-principles.md § EF Core / SQLite and constraints-registry.md § EF Core / SQLite document the same two constraints (MigrationsLock, CollationInterceptor). Neither is a complete subset of the other.
- Fix: Consolidate under constraints-registry.md (designated home for discovered runtime constraints). In code-principles.md, replace § EF Core / SQLite with: "EF Core / SQLite constraints: see `.claude/rules/constraints-registry.md § EF Core / SQLite`."

**W-26 — "Spec Language — Determinism" is the only non-coding rule in a coding standards file (C-02 + S-03 in Agent C)**
- See W-22 above. Structural dimension: code-principles.md's scope is coding standards; the spec quality rule is a misfit that may cause it to be missed by agents loading code-principles.md for coding guidance.
- Fix: Resolved by W-22's action (move to workflow.md).

#### 🟢 Suggestions

**S-28 — ObservableRangeCollection entry duplicated across code-principles.md and constraints-registry.md (D-07 in Agent C)**
- code-principles.md has the full section with code examples; constraints-registry.md has a summary that adds no new content.
- Fix: Remove the constraints-registry.md entry. Add: "ObservableRangeCollection / DXCollectionView reset events: see `code-principles.md § UI Thread Performance`."

**S-29 — Global Usings namespace lists are volatile snapshots with no verification note (I-03 in Agent C)**
- The specific namespace lists in code-principles.md § Global Usings will drift as the codebase evolves. No note warns agents to verify against the actual GlobalUsings.cs.
- Fix: Add after each list: "Verify against the project's `GlobalUsings.cs` — this list is a snapshot, not the authoritative source."

**S-30 — "Incremental edits" Constitutional Constraint has no elaboration or rationale anywhere in rules files (I-02 in Agent C)**
- All other Constitutional Constraints are elaborated in code-principles.md or constraints-registry.md or point to a skill. "Incremental edits" has only the one-sentence statement in CLAUDE.md.
- Fix: Add to constraints-registry.md: "Incremental XAML edits: Edit one XAML file → build → fix before editing the next. Rationale: XAML parser errors cascade across files, making the source of the error ambiguous when batched."

**S-31 — code-principles.md has no table of contents or section cross-references (S-01 in Agent C)**
- CLAUDE.md § Constitutional Role refers readers to code-principles.md for naming conventions, DI, error handling — but gives no section pointers. code-principles.md itself has no internal navigation.
- Fix: See S-25 (add pointers from CLAUDE.md). Optionally add a brief table of contents at the top of code-principles.md.

---

### constraints-registry.md — 2 findings

#### 🟢 Suggestions

**S-32 — DXCollectionView reset entry should reference code-principles.md instead of duplicating it (D-07 in Agent C, companion to S-28)**
- constraints-registry.md § DevExpress / UI has a summary of the ObservableRangeCollection reset constraint already documented in detail in code-principles.md.
- Fix: Replace the entry with a cross-reference: "ObservableRangeCollection / DXCollectionView reset events: see `code-principles.md § UI Thread Performance — ObservableRangeCollection`."

**S-33 — "Incremental XAML edits" constraint should be added (I-02 in Agent C, companion to S-30)**
- No entry for the Incremental edits Constitutional Constraint exists in constraints-registry.md despite it being a build-behavior constraint (XAML parser error cascade).
- Fix: Add entry as described in S-30.

---

## Cross-File Conflicts Not Captured by Partial Reports

The following cross-file conflicts were identified during synthesis and were not flagged by any partial agent:

**XF-01 — myvocalist-coding skill invocation not in workflow.md pre-dispatch checklist**
- CLAUDE.md § Skill & MCP Lookup states: "All UI / coding work: `myvocalist-coding` skill (gates DevExpress, CRUD, dialogs, EF Core rules)" — mandatory.
- workflow.md Pre-dispatch validation checklist and Subagent exit checklist both omit this skill check.
- **Severity:** 🟡 Warning
- **Target:** workflow.md
- **Fix:** Add to the pre-dispatch validation checklist: "For UI/coding tasks: confirm subagent briefing includes instruction to invoke `myvocalist-coding` skill per CLAUDE.md § Skill & MCP Lookup."

**XF-02 — CLAUDE.md MCP Availability Gate has no corresponding enforcement mechanism in workflow.md**
- CLAUDE.md § MCP Availability Gate: "If a required MCP server (Context7, SQLite) is unavailable at task start: Do NOT silently skip … Fail with an explicit message."
- workflow.md's Pre-task context gate (subagent gate) has no MCP availability check.
- **Severity:** 🟡 Warning
- **Target:** workflow.md
- **Fix:** Add to "Pre-task context gate" checklist: "Required MCPs for this task are available (per CLAUDE.md § MCP Availability Gate). If unavailable: set status `blocked: MCP unavailable`, stop."

**XF-03 — workflow.md ACTIVE-CONSIDERATIONS.md path conflicts with findings.md path convention**
- workflow.md: ACTIVE-CONSIDERATIONS.md → `Docs/DevEnv/ACTIVE-CONSIDERATIONS.md`; session-handoff.md → `Docs/superpowers/plans/<plan-name>-handoff.md`; findings.md (non-feature) → `Docs/DevEnv/findings/[date]-[topic].md`
- Rule 5 task-log: task-logs → `Docs/superpowers/plans/<plan-name>-task-log.md`
- The `Docs/DevEnv/` vs `Docs/superpowers/plans/` split for session artifacts is not stated as a deliberate division. ACTIVE-CONSIDERATIONS.md and findings.md go to DevEnv/; handoff and task-log go to superpowers/plans/.
- **Severity:** 🟢 Suggestion
- **Target:** workflow.md
- **Fix:** Add a one-sentence clarification distinguishing DevEnv/ (environment/session state files) from superpowers/plans/ (plan-execution artifacts). Or consolidate all session artifacts under one root.

---

## Prioritized Action List

### 🔴 Blockers — resolve before next implementation wave

| # | ID | Target file | Action |
|---|-----|-------------|--------|
| 1 | B-04 | review.md + testing.md + workflow.md | Standardize AC traceability matrix to one schema: `AC ID \| Criterion (short) \| Implementation location \| Test method` |
| 2 | B-07 | workflow.md | Add `/project:review` to subagent exit checklist; place "spec/plan file" trigger in workflow.md Rule 3a or exit checklist |
| 3 | B-01 | workflow.md | Add exception to Briefing protocol for verbatim wave-handoff contracts; remove contradiction with "paths only" rule |
| 4 | B-08 | CLAUDE.md | Merge Constitutional Constraints and Unamendable list; define the selection criterion; add "Business logic in Services" to Constitutional list |
| 5 | B-09 | CLAUDE.md + code-principles.md | Make code-principles.md § Architecture Constraints defer to CLAUDE.md § Architecture; consolidate "Business logic in Services" to one canonical location |
| 6 | B-02 | workflow.md | Reconcile `tasks.md` requirement for small isolated changes: one answer, not two tables |
| 7 | B-03 | workflow.md | Clarify spec rot check frequency: lightweight every-wave check vs. deep every-second-wave checkpoint |
| 8 | B-05 | review.md + workflow.md | Assign "After Review" enhancement check ownership: subagent exit checklist step or main-agent post-commit step, documented in both files |
| 9 | B-06 | testing.md + review.md | Reconcile Level C no-test exemption with review.md §9 AC coverage requirement; add task-log documentation requirement |

### 🟡 Warnings — resolve before or during next spec/rules update session

| # | ID | Target file | Action |
|---|-----|-------------|--------|
| 10 | W-22 | code-principles.md + workflow.md | Move "Spec Language — Determinism" to workflow.md Rule 1; leave reference in code-principles.md |
| 11 | W-07 | workflow.md | Renumber rules sequentially and reorder sections (1 → 2 → 2a/2b absorbed → 3 → 3a absorbed → 4 → 5 → 6 → 7 → 8) |
| 12 | W-08 | workflow.md | Move session management sections (ACTIVE-CONSIDERATIONS, handoff, context exhaustion, reset discipline, discovery briefs) from Rule 2 to Rule 7 |
| 13 | W-10 | workflow.md | Merge spec size calibration, spec ceremony calibration, and "When to skip SDD" into one consolidated decision table |
| 14 | W-11 | workflow.md + CLAUDE.md | Merge "Regeneration test practice" and "Rebuild test" into one section; add "test suite" to context sources; make CLAUDE.md entry a pointer |
| 15 | W-13 | review.md | Consolidate "spec updated when code deviates" to §6; remove from §7, §8, §10, §12 |
| 16 | W-14 | review.md | Group §6–§12 under parent heading; designate §6 canonical; reorder §9 |
| 17 | W-15 | workflow.md | Align exit checklist "To Review" gate with review.md three-severity model |
| 18 | W-18 | review.md + workflow.md | Cross-reference "After Review" ↔ Rule 3a session-end ritual |
| 19 | W-20 | testing.md | Add Tester/Builder exception cross-reference to "One test at a time" in both directions |
| 20 | W-23 | CLAUDE.md | Cross-reference "Continuous Enhancement" → "Amending These Rules" |
| 21 | W-25 | code-principles.md + constraints-registry.md | Consolidate EF Core / SQLite constraints under constraints-registry.md; code-principles.md references only |
| 22 | W-21 | testing.md | Fix "Level Guidance above" → below, or reorder Stryker after Level Guidance |
| 23 | XF-01 | workflow.md | Add myvocalist-coding skill check to pre-dispatch validation checklist |
| 24 | XF-02 | workflow.md | Add MCP availability check to pre-task context gate |
| 25 | W-03 + W-04 | workflow.md | Fix subagent spec-write permissions (Living spec protocol + Rule 2a); fix EF migration CLI ownership |
| 26 | W-05 + W-06 | workflow.md | Reconcile two-tier spec trigger threshold; clarify Verifier mandatory vs optional |
| 27 | W-16 + W-17 + W-19 | review.md | Fix severity inconsistencies for interface check; add cross-reference to testing.md for To Review gate; consolidate AC-to-test check to §9 |
| 28 | W-24 | CLAUDE.md | Make CLAUDE.md rebuild test entry a pointer only; add "test suite" to workflow.md rebuild test |

### 🟢 Suggestions — schedule for next housekeeping pass

| # | ID | Target file | Action |
|---|-----|-------------|--------|
| 29 | S-08 + S-14 | workflow.md + review.md | Standardize `[YYYY-MM-DD]` date placeholder everywhere |
| 30 | S-10 + S-11 + S-12 | workflow.md | Move hook health check to Rule 7; move exit checklist to end of Rule 2; move "When to skip SDD" to start of Rule 1 |
| 31 | S-18 + S-27 | testing.md + CLAUDE.md | Remove/replace Step 3 historical references with unconditional TDD statement |
| 32 | S-19 | testing.md | Move or retire "Prerequisites Before Step 3" — add status indicator |
| 33 | S-20 + S-21 | testing.md | Remove duplicate `---`; move TDD Level Guidance before Stryker |
| 34 | S-15 | review.md | Promote "After Review" to numbered §13 inside the review checklist |
| 35 | S-16 + S-22 | testing.md + review.md | Align test quality audit trigger to unconditional; add Tester/Builder cross-reference |
| 36 | S-24 + S-25 + S-26 | CLAUDE.md + code-principles.md | Remove "English only" from code-principles.md; add section pointers to Constitutional Role; move "Prefer composition" to code-principles.md |
| 37 | S-23 | CLAUDE.md | Simplify Unamendable list entries to reference Constitutional Constraints (after B-08 fix) |
| 38 | S-28 + S-32 | code-principles.md + constraints-registry.md | Remove ObservableRangeCollection duplicate from constraints-registry.md; add cross-reference |
| 39 | S-29 | code-principles.md | Add "verify against GlobalUsings.cs" note to Global Usings lists |
| 40 | S-30 + S-33 | code-principles.md + constraints-registry.md | Add "Incremental XAML edits" entry to constraints-registry.md with rationale |
| 41 | S-31 | code-principles.md | Add table of contents or brief navigation header |
| 42 | S-01–S-07 + S-09 + S-13 | workflow.md | Cross-references and minor consolidations (spec gap escalation, findings.md, spec-write prohibition, post-wave verification, demo statement, session resume rule, commit+push, spec-write qualifier) |
| 43 | XF-03 | workflow.md | Clarify DevEnv/ vs superpowers/plans/ artifact placement |

---
*End of consolidated conflict report.*

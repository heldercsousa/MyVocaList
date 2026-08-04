---
id: dx-autocompleteedit-replacement
title: **Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`**
status: ✅ Done
target: 2026-07-19
section: DevCycleCraft
parent: autocomplete-component
goal: mature built-in autocomplete on all form consumers; unblocks BUG-027 → Artists & Songs Catalog.
gate: "T2–T7 done and merged; T7 surfaced BUG-050, BUG-051 and BUG-052, all three now fixed and closed. Remaining: confirm no residual DX-migration defect before ✅."
pointer: DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/
closed: 2026-08
order: 190
kind: change
---

# Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`

Migrates every form consumer of the frozen custom `AutocompleteMobileField` onto DevExpress
`AutoCompleteEdit`, per Helder's 2026-07-19 adoption decision. Unblocks BUG-027 and, through
it, the Artists & Songs Catalog.

**State at migration (2026-07-22), preserved verbatim from the pre-migration BACKLOG row:**
T2–T6 complete and merged to develop 2026-07-20; the code review returned CONDITIONAL PASS
with no blockers, 501/501 green; awaiting Helder's T7 on-device checklist (items a–i,
including smoke 16C.1) before the row can go terminal.

> **Spec updated [2026-07-22]:** the review verdict and the test count trip `model._BANNED`
> (REQ-SEV-09) and are therefore recorded here rather than in the rendered row. No wording was
> changed — only relocated. Declared T12 diff hunk.
>
> **Depth note for T12:** this folder is one `changes/` segment deep, so `_depth` renders one
> `↳`, while the pre-migration row is written `↳↳` (it was hand-indented under the
> *Build new MD3-compliant autocomplete component* row). The arrow count is derived from the
> path by design (`design.md` § 3: *"`depth_arrows` … never authored"*), so this is an
> expected, declared diff hunk — not a transcription error.

Detail: `requirements.md`, `design.md`, `tasks.md`, `plan.md`, `findings.md`, `task-log.md`
in this folder.

> **Row refreshed [2026-08-03].** The previous gate read *"awaiting Helder's T7 on-device
> checklist"* — stale. T7 **was** run; it is what surfaced BUG-050, BUG-051 and BUG-052, and
> all three are now ✅ Fixed and closed. BUG-027 is also closed, so the goal's
> *"unblocks BUG-027"* clause is delivered rather than pending.
>
> This row is now the **live row** for the autocomplete work: its parent
> *Autocomplete Component — Evaluation, Rebuild & Rollout* went 🔵 Superseded on 2026-08-03,
> since the DX adoption decision replaced the build-a-new-component scope.

> **Closed ✅ Done [2026-08-04] (Helder).** The migration itself is complete: every form consumer
> runs on DevExpress `AutoCompleteEdit`, T2-T7 are merged, and the three defects T7 surfaced
> (BUG-050, BUG-051, BUG-052) plus BUG-027 are all closed.
>
> **Closed with defects outstanding, by design.** Remaining faults on the song form are not
> autocomplete-migration faults and each already owns its own row -- BUG-071 (alias BUG-068),
> the edit-mode save failure, is a persistence/EF-tracking defect, and the DbContext lifetime
> and unit-of-work item addresses its root cause. Per `bug-tracking.md` a defect is tracked by
> its own row, so holding this row open would duplicate that tracking and hide the fact that
> the migration is finished.

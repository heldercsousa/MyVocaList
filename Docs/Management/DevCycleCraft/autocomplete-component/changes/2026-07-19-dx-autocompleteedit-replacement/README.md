---
id: dx-autocompleteedit-replacement
title: "**Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`**"
status: "🟡 In Progress"
target: 2026-07-19
section: DevCycleCraft
parent: autocomplete-component
kind: change
order: 190
goal: "mature built-in autocomplete on all form consumers; unblocks BUG-027 → Artists & Songs Catalog."
gate: "T2–T6 complete and merged to develop 2026-07-20; awaiting Helder's T7 on-device checklist (items a–i, incl. smoke 16C.1) before ✅."
pointer: DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/
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

---
id: inline-artist-create
title: **Song artist field — correctness fixes + inline "create new artist"**
status: "🟡 In Progress"
target: 2026-07-21
section: BusinessFeatures
parent: artists-songs
kind: change
order: 70
goal: "make the Song Artist autocomplete correct (folding in BUG-050, BUG-051, BUG-052 and retain-text) and add inline create-new-artist (➕ row), closing BUG-027."
gate: "on-device re-run #5 failed 2026-08-02 — an EF Core tracking conflict blocks every edit-mode save; three split fix sessions plus a green re-run gate closeout."
pointer: BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/task-log.md
---

# Song artist field — correctness fixes + inline "create new artist"

Makes the Song Artist autocomplete correct and adds an inline create-new-artist (➕) row,
closing BUG-027.

**State at migration (2026-07-22), preserved verbatim from the pre-migration BACKLOG row:**
implementation T1–T9 done plus the BUG-053 XAML crash fixed (`8d33547`) on branch
`feat/inline-artist-create`, 517/517 green. T10 on-device FAILED 2026-07-22: Part A (BUG-053)
fixed; six defects remain (BUG-054…059). Root cause of b/e/j: the fixes landed in the
ViewModel but the real defects are in the DX `AutoCompleteEdit` wiring/XAML — an
unit-untested seam. Next: fix BUG-054…059 in the same worktree and re-run T10. Branch not
pushed.

> **Spec updated [2026-07-22]:** the row above is trimmed relative to its pre-migration text.
> The commit hash, the `517/517` test count and the per-step status trail all trip
> `model._BANNED` (REQ-SEV-09), so they are recorded here verbatim rather than in the rendered
> row. No wording was changed — only relocated. Declared T12 diff hunk.

Detail: `task-log.md` § T10 outcome, plus `requirements.md`, `design.md`, `tasks.md`,
`plan.md`, `handoff.md` in this folder.

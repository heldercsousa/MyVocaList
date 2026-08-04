---
id: BUG-027
title: "BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text (Critical)"
status: ✅ Fixed
severity: Critical
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
goal: make song creation possible again.
gate: fixed via the DX `AutoCompleteEdit` replacement plus the Song artist-field correctness work; all three symptoms verify clean.
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-027-songformpage-artist-field-broken/
closed: 2026-08
order: 30
kind: bug
---

# BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text

The Song form's Artist field has no validation and no autocomplete, and blurring the
field clears whatever was typed. Song registration is impossible while this stands —
which is why the parent **Artists & Songs Catalog** row is 🔴 Blocked on it.

Fix direction is owned by the DX `AutoCompleteEdit` replacement task (decision
2026-07-19), superseding autocomplete foundations ① + ②.

**History / back-link:** `BusinessFeatures/artists-songs/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11b,
> REQ-SEV-01). The row's pointer moves from the shared `artists-songs` task-log to this
> folder. **Nothing was removed from that task-log** (REQ-SEV-27); it remains the
> narrative record and is linked above.

> **Closed ✅ Fixed [2026-08-03].** All three reported symptoms verify clean on the
> 2026-08-02 on-device re-run: autocomplete present and functional, validation present,
> and typed text retained on blur (checklist item D3 ✅). Delivered by the DX
> `AutoCompleteEdit` replacement plus the Song artist-field correctness work.
>
> The parent **Artists & Songs Catalog** remains 🔴 Blocked -- on **BUG-068**
> (EF Core tracking conflict aborting every edit-mode save), a distinct defect found
> after this bug's symptoms were resolved. Do not read the parent's blocked state as
> BUG-027 still being open.

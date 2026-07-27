---
id: BUG-027
title: "BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text (Critical)"
status: "💡 Pending"
severity: Critical
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 30
goal: "make song creation possible again."
gate: "fix direction now owned by the DX `AutoCompleteEdit` replacement task (decision 2026-07-19), superseding foundations ① + ②."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-027-songformpage-artist-field-broken/
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

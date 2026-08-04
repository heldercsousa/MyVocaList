---
id: BUG-029
title: "BUG-029: ArtistFormPage search-strip icon crashes the app (Critical)"
status: 🔵 Superseded
severity: Critical
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
goal: the search-strip icon must not crash the app.
gate: the search-strip element is slated for deletion by the Form UX Redesign; re-triage only if any part of the strip survives.
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-029-artistformpage-search-strip-icon-crash/
closed: 2026-08
order: 150
kind: bug
---

# BUG-029: ArtistFormPage search-strip icon crashes the app

Tapping the search-strip icon on the Artist form crashes the app.

Deferred: the search-strip element is slated for deletion by the Form UX Redesign;
re-triage only if any part of the strip survives.

> **Goal sentence agent-authored [2026-07-22] — pending Helder's review.** The
> pre-migration row carries no `Goal:`; its Notes cell begins *"Deferred: …"*.
> `model.REQUIRED` makes `goal` mandatory, so the goal above was derived strictly from
> this row's own title. The deferral reason is transcribed verbatim as the row's `gate:`.
> Declared as a T12 diff hunk (Helder decision 1, 2026-07-22).

**History / back-link:** `BusinessFeatures/artists-songs/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11b,
> REQ-SEV-01). The row's pointer moves from the shared `artists-songs` task-log to this
> folder. **Nothing was removed from that task-log** (REQ-SEV-27); it remains the
> narrative record and is linked above.

> **Closed 🔵 Superseded [2026-08-04] -- cancelled as a standalone bug (Helder).** The row was
> already 🔵 Deferred on exactly this reasoning: the search-strip element is slated for
> deletion, so the crash disappears with the element. Ownership transfers to **Artist & Song Form UX Redesign** (`BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`).
>
> Re-open only if any part of the search strip survives that redesign.

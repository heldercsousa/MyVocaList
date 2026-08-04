---
id: BUG-031
title: "BUG-031/032: no API autocomplete while typing Artist Name / Song Title (spec gap)"
status: 🔵 Superseded
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
goal: settle whether API-backed autocomplete is required on the two name entries.
gate: "Answered by Helder 2026-07-10: autocomplete (local + API) IS required on both entries — folded into the Form UX Redesign."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-031-no-api-autocomplete-artist-name-song-title/
closed: 2026-08
order: 170
kind: bug
---

# BUG-031/032: no API autocomplete while typing Artist Name / Song Title (spec gap)

Neither the Artist Name entry nor the Song Title entry offered API-backed autocomplete
while typing, and the spec did not say whether it was required.

Answered by Helder 2026-07-10: autocomplete (local + API) IS required on both entries —
folded into the Form UX Redesign.

> **One row, one folder — BUG-031 and BUG-032 are a single BACKLOG row.** The
> pre-migration row is `BUG-031/032: …`; splitting it into two rows would change the row
> set, which REQ-SEV-25 forbids (regeneration must be row-for-row equivalent). The folder
> and `id:` therefore use `BUG-031`, and the title carries both ids verbatim so `BUG-032`
> stays grep-reachable in BACKLOG.md. Declared for T12.

> **No `severity:`** — same reasoning as BUG-030: the row carries none, and inventing one
> would be fabrication.

> **Goal sentence agent-authored [2026-07-22] — pending Helder's review.** Same class as
> BUG-029/BUG-030.

**History / back-link:** `BusinessFeatures/artists-songs/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11b,
> REQ-SEV-01). The row's pointer moves from the shared `artists-songs` task-log to this
> folder. **Nothing was removed from that task-log** (REQ-SEV-27); it remains the
> narrative record and is linked above.

> **Closed 🔵 Superseded [2026-08-04] -- cancelled as a standalone row (Helder).** Covers both
> BUG-031 and BUG-032. The spec gap this row existed to surface was already answered by
> Helder on 2026-07-10 (local + API autocomplete IS required on both name entries); the
> answer's delivery belongs to **Artist & Song Form UX Redesign** (`BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`).
>
> A spec gap that has been answered is not a bug -- it is a requirement, and it now lives
> with the redesign that must satisfy it.

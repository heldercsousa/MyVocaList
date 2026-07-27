---
id: BUG-051
title: "BUG-051: Song form — artist autocomplete returns stale results (searches prior keystroke) (Major)"
status: "💡 Pending"
severity: Major
target: 2026-07-21
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 50
goal: "dropdown must reflect the current query. Root cause: shared `ArtistSuggestions` race, no per-request cancellation in `SearchArtistsAsync`. Found in DX-AC T7 (W2 realized)."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-051-autocomplete-stale-results/
---

# BUG-051: Song form — artist autocomplete returns stale results

The artist suggestion dropdown shows results for the previous keystroke rather than
the current query.

Root cause: a shared `ArtistSuggestions` collection is raced by overlapping searches, and
`SearchArtistsAsync` has no per-request cancellation.

Found during T7 (on-device checklist) of the DX `AutoCompleteEdit` replacement — this is
risk W2 of that task, realized.

**History / back-link:** `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11a,
> REQ-SEV-01 — every Critical/Major bug owns a folder). The row's pointer moves from the
> DX `AutoCompleteEdit` replacement task-log to this folder. **Nothing was removed from that
> task-log** (REQ-SEV-27); it remains the narrative record and is linked above.

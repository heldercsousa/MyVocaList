---
id: BUG-050
title: "BUG-050: Song form — selecting an artist suggestion does not lock the field (Critical)"
status: "💡 Pending"
severity: Critical
target: 2026-07-21
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 40
goal: "picking a suggestion must lock the Artist field. Root cause: `SelectArtist` never sets `IsArtistLocked=true` (one-line omission). Found in DX-AC T7."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-suggestion-not-locked/
---

# BUG-050: Song form — selecting an artist suggestion does not lock the field

Selecting an artist from the autocomplete suggestion list leaves the Artist field
unlocked, so the typed text remains editable and the selection can be silently lost.

Root cause: `SelectArtist` never sets `IsArtistLocked = true` — a one-line omission.

Found during T7 (on-device checklist) of the DX `AutoCompleteEdit` replacement.

**History / back-link:** `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11a,
> REQ-SEV-01 — every Critical/Major bug owns a folder). The row's pointer moves from the
> DX `AutoCompleteEdit` replacement task-log to this folder. **Nothing was removed from that
> task-log** (REQ-SEV-27); it remains the narrative record and is linked above.

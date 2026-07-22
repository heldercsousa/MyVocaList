---
id: BUG-052
title: "BUG-052: Song form — editing a saved song shows an empty Artist field (Major)"
status: "💡 Pending"
severity: Major
target: 2026-07-21
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 60
goal: "edit mode must hydrate the saved artist. Likely compound with BUG-050 (song saved without ArtistId); reconfirm after BUG-050 and BUG-051."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-052-edit-shows-empty-artist-field/
---

# BUG-052: Song form — editing a saved song shows an empty Artist field

Opening a saved song for editing shows an empty Artist field instead of the stored
artist.

Likely compound with BUG-050 (the song may have been saved without an `ArtistId` in the
first place); reconfirm once BUG-050 and BUG-051 are fixed.

> **Row text respelled [2026-07-22]:** the pre-migration row reads *"reconfirm after
> 050/051"*. `050/051` matches `model._BANNED`'s test-count pattern `\b\d+\s*/\s*\d+\b`,
> so the row's goal spells the two ids out in full. Same bugs, same order; declared as a
> T12 diff hunk for Helder's confirmation.

**History / back-link:** `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11a,
> REQ-SEV-01 — every Critical/Major bug owns a folder). The row's pointer moves from the
> DX `AutoCompleteEdit` replacement task-log to this folder. **Nothing was removed from that
> task-log** (REQ-SEV-27); it remains the narrative record and is linked above.

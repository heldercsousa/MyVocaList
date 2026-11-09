---
id: BUG-085
title: Song save misses near-duplicate title when the typed title is longer than the stored one
status: 💡 Pending
severity: Major
target: 2026-11-09
section: BusinessFeatures
parent: artists-songs
goal: Saving a title with one extra letter of a stored title for the same artist shows the Possible duplicates sheet, as the reverse case already does.
gate: "Device retest: stored Patience then saving Patiencee shows the sheet."
kind: bug
---

# Song save misses near-duplicate title when the typed title is longer than the stored one

Saving a title with one extra letter of a stored title for the same artist shows the Possible duplicates sheet, as the reverse case already does.

**Found:** device run 2026-11-09 (Samsung S23). Stored "Patience" (no version) for one artist; saving "Patiencee" for the same artist saved directly with no sheet; saving "Patience" version 22 listed both.

**Root cause:** `SongResolutionService.ResolveAsync` builds its fuzzy pool with `GetFuzzyCandidatePoolAsync(artistId, firstTokenOfTypedTitle + "%")`, a forward LIKE on the stored title, so a typed title longer than the stored one can never match. The artist path already uses the trigram index (`FtsQueryBuilder.BuildTrigramMatch`).

**Fix (approved by Helder 2026-11-09):** same-artist candidates from the song-title trigram index (new repository query), scored with the unchanged 0.82 threshold. Spec: note on REQ-SFT-23. Tests that pin the old prefix pool are rewritten; add a regression test for the longer-typed-title case.


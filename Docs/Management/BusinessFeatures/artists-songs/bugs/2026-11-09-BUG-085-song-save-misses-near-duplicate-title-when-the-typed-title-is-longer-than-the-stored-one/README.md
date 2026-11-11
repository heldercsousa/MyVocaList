---
id: BUG-085
title: Song save misses near-duplicate title when the typed title is longer than the stored one
status: ✅ Done
severity: Major
target: 2026-11-09
section: BusinessFeatures
parent: artists-songs
goal: Saving a title with one extra letter of a stored title for the same artist shows the Possible duplicates sheet, as the reverse case already does.
gate: "Device retest: stored Patience then saving Patiencee shows the sheet."
closed: 2026-11
kind: bug
---

# Song save misses near-duplicate title when the typed title is longer than the stored one

Saving a title with one extra letter of a stored title for the same artist shows the Possible duplicates sheet, as the reverse case already does.

**Found:** device run 2026-11-09 (Samsung S23). Stored "Patience" (no version) for one artist; saving "Patiencee" for the same artist saved directly with no sheet; saving "Patience" version 22 listed both.

**Root cause:** `SongResolutionService.ResolveAsync` builds its fuzzy pool with `GetFuzzyCandidatePoolAsync(artistId, firstTokenOfTypedTitle + "%")`, a forward LIKE on the stored title, so a typed title longer than the stored one can never match. The artist path already uses the trigram index (`FtsQueryBuilder.BuildTrigramMatch`).

**Fix (approved by Helder 2026-11-09):** same-artist candidates from the song-title trigram index (new repository query), scored with the unchanged 0.82 threshold. Spec: note on REQ-SFT-23. Tests that pin the old prefix pool are rewritten; add a regression test for the longer-typed-title case.

**Merged `55ca9b88` 2026-11-09** (Red `c5161906`, test correction `f64e70d2`, fix `8b94d1e6`); Verifier (Standard) CONDITIONAL PASS, 0 blockers. The first Green attempt was blocked by a defect in the Tester's own test (artist resolver mocked to NoMatch, so the fuzzy step was unreachable); corrected before the fix was committed.

**Device retest still open (status stays In Progress until it passes):** with "Patience" stored for one artist, saving "Patiencee" for the same artist shows the Possible duplicates sheet; the reverse case and "Patience" with a new version still behave; saving feels as fast as before on the real library.

**Known and accepted (Verifier, non-blocking):** the title search table also holds the artist name, so an unqualified match can pull other songs of the same artist into the candidate pool; the 0.82 score drops them. A `title :` column filter would remove that noise but also changes the global search, so it is a separate change.

**Device retest passed 2026-11-10 (Helder, Samsung S23, build from develop). Closed.**

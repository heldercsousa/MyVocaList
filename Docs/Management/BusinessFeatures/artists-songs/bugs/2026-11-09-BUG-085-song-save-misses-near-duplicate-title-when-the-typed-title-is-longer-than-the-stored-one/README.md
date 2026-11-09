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


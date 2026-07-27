---
id: BUG-003
title: "~~SongsPage Filter Chips~~ → ArtistsPage Filter Chips"
status: "🔵 Duplicate"
target: 2026-06-14
section: DevCycleCraft
parent: artists-songs
kind: bug
closed: 2026-06
order: 70
goal: "misattribution corrected — SongsPage has no filter surface; duplicate of the Artists CRUD List filter fix. Closed as duplicate, no separate work."
pointer: BusinessFeatures/artists-songs/bugs/2026-06-14-BUG-003-songpage-filter-chips/
---

# ~~SongsPage Filter Chips~~ → ArtistsPage Filter Chips (BUG-003)

Misattribution corrected (Helder, 2026-06-14): the app's only filter-chip surface is
`ArtistsPage` (Author/Performer). SongsPage has no filter UI and no song-filter domain concept.
Closed as a duplicate of the **Artists CRUD List filter issue** (Author/Performer chip
regression), fixed under branch `fix/artists-filter-regression`.

> Migrated from the 2026-06 archive row (T12a Wave L, F-1a batch 2). Folder created fresh at the
> REQ-SEV-01 dated-slug shape; the pre-existing flat file `BUG-003-songpage-filter-chips.md` was
> moved in with `git mv`, nothing deleted. `status: Duplicate` uses T12-pre's extended STATUSES
> (shipped `e7b29a5`). Craft-table item filed in the Business Features bug tree (its subject is an
> Artists-page UI bug), matching the BUG-022/Wave-B cross-file precedent — `section: DevCycleCraft`
> set explicitly. Pre-scheme `severity: Medium` (found in the moved file's body) dropped from this
> frontmatter per standing ruling — preserved verbatim in the moved flat file's body.

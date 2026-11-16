---
id: song-list-item-shows-artist-name-version-and-featured-artists
title: Song list item shows artist name, version and featured artists
status: ✅ Done
target: 2026-11-09
section: BusinessFeatures
parent: artists-songs
goal: The Song list secondary line appends the artist name (truncated when needed) and the version, keeping the featured artists.
gate: "Device: list rows show artist plus version plus featured artists, truncated cleanly."
closed: 2026-11
kind: change
---

# Song list item shows artist name, version and featured artists

The Song list secondary line appends the artist name (truncated when needed) and the version, keeping the featured artists.

## Delivery (2026-11-13)
Delivered by REQ-ART-20 of `../2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/` (commits `35be849f` for `SongListItemDto.SupportingText`, `bdd6eb67` for `SongsPage.xaml`). Format: `artist · version · feat. <featured artists>`, empty parts omitted. Device check: step 1 of that item's task-log checklist.


## Observation

Closed on 2026-11-16 at Helder's explicit request during the backlog cleanup. Shipped in code; the on-device check was not recorded. Reopen it by registering a new item if the work is wanted again.

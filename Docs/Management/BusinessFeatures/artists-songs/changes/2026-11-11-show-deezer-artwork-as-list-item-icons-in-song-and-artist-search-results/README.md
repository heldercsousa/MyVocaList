---
id: show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results
title: Show Deezer artwork as list-item icons in song and artist search results
status: 💡 Pending
target: 2026-11-11
section: BusinessFeatures
parent: artists-songs
goal: "Deezer returns cover and artist pictures; show them as MD3 list-item leading or trailing images in title and artist searches and in the CRUD lists. Gate: evaluate first whether to store the image locally, store only the URL, or render another way."
kind: change
---

# Show Deezer artwork as list-item icons in song and artist search results

Deezer returns cover and artist pictures; show them as MD3 list-item leading or trailing images in title and artist searches and in the CRUD lists. Gate: evaluate first whether to store the image locally, store only the URL, or render another way.


## Origin
Helder, 2026-11-11, while testing SFT 5.5 on device: the Deezer track search returns an image per item, so it would be good to show it as a leading or trailing icon in results. Evaluation and later implementation only; nothing is built now.

## What Deezer returns (seen in a real `search/track` response, 2026-11-11)
- Per track: `album.cover_small` (56x56), `cover_medium` (250), `cover_big` (500), `cover_xl` (1000), plus `md5_image`.
- Per track artist: `artist.picture_small`, `picture_medium`, `picture_big`, `picture_xl`.
- To verify before spec: whether Deezer has an artist-only search endpoint (`search/artist`) that would allow remote artist search; REQ-SFT-16 currently says artist search is local only, so that would be a spec change.

## Questions to settle in the evaluation
1. **Storage:** store the image bytes locally, store only the Deezer URL, or neither (render from the network on demand)? Weigh offline use, app size, URL expiry/stability, Deezer terms of use for caching or redistributing artwork, and privacy (a request per row leaks what is typed).
2. **Where it shows:** Title search results (local and Deezer rows), Artist search results (if remote artist search is allowed), the Songs and Artists CRUD list searches and rows. Local rows need a stored image or fallback to be consistent with Deezer rows.
3. **Component:** check whether `ListItem` already has a leading/trailing image slot that is MD3-compliant (m3.material.io lists: leading image/avatar/thumbnail) before any component change. `ListItem` is a governed component: any change needs its own task with consumer map and Helder approval (`component-change-governance.md`).
4. **Fallback:** what a row shows when there is no image (offline, local-only song, load failure).
5. **Entities:** whether `Song` and `Artist` need a new column (migration) or whether the image belongs to the external-identity data already stored for picked results.

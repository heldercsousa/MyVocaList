---
id: deezer-browse-popular-now-cards-artist-profile-with-discography-and-top-hits-read-only-deezer-song-view
title: "Deezer browse: popular-now cards, artist profile with discography and top hits, read-only Deezer song view"
status: 💡 Pending
target: 2026-11-21
section: BusinessFeatures
parent: artists-songs
goal: First-run and everyday browsing of the Deezer catalog without saving songs one by one; Deezer data read-only, lyrics editable.
gate: Catalog is MVP scope (Helder, 2026-11-22). Helder re-decides the catalog source first, because the Deezer terms ban local copies and audiovisual combination.
kind: change
---

# Deezer browse: popular-now cards, artist profile with discography and top hits, read-only Deezer song view

First-run and everyday browsing of the Deezer catalog without saving songs one by one; Deezer data read-only, lyrics editable.

**Status (2026-11-21): idea only, post-MVP.** No requirements or design exist. Feasibility, sources and terms: `../../../music-sourcing-strategy/findings.md`.

## Helder's ideas (2026-11-21, translated and condensed)

| # | Idea |
|---|------|
| 1 | The Songs list browses the Deezer catalog directly, not only local songs, and the search bar filters it. |
| 1.1 | A "popular songs" set for the user's region on first run, as YouTube and Spotify do when they know nothing about the user. |
| 1.2 | Once the host has run events, suggest songs that were sung at past events, from the local history. |
| 1.3 | Not an endless list. Cards in the style of Deezer/Spotify: a few songs plus a few artists. A tap on a song opens the song; a tap on an artist opens the artist. |
| 1.4 | Deezer songs: no change allowed except lyrics. Local songs stay fully editable; FAB, edit button and existing features remain. |
| 1.4.1 | Possibly a separate song view page (view only). Editing is allowed only for songs not taken from Deezer. |
| 1.5 | In the song view, a tap on the artist name opens an artist profile page. In the profile, Deezer data is read-only too. |
| 1.5.2–1.5.3 | The profile lists the discography (albums with artwork and track lists) and the artist's top hits. |
| 1.5.4–1.5.5 | A tap on a song opens the song view. A tap on an album expands it inline, or opens an album page. |

## Feasibility summary (from the findings)

- **Endpoints:** `/chart/0/tracks` (region by IP), `/chart/{genreId}`, `/artist/{id}/top`, `/artist/{id}/albums`, `/album/{id}/tracks` and `/artist/{id}/related` all work without a key.
- **Terms:** non-commercial use only, a visible Deezer logo, and no stored image files. The paid phase needs a written agreement with Deezer or another source (MusicBrainz + Cover Art Archive).
- **Search depth:** a few hundred results per query, so search narrowly.
- **1.2:** uses local data only, so no Deezer terms risk. It waits for the queue/event history.

## Open choices for Helder (before any spec)

- Album: expand inline in the artist profile (fewer pages; recommended first) or a separate album page.
- Song view: a new read-only page, or the existing song form in a locked mode. "Deezer artist frozen" in `../2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/` already locks Deezer artist data.
- Browse home: a new page, or cards above the Songs list. The Songs list is the governed `CrudListView`, so cards there would need a governed component change. This is the same blocker that paused the trial in `../2026-11-16-songs-list-searches-deezer-one-tap-saves-a-thin-song-row-lyrics-copy-with-edit-mark/`.
- Whether 1.1 replaces that paused trial or both are kept.


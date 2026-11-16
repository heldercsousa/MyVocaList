---
id: songs-list-searches-deezer-one-tap-saves-a-thin-song-row-lyrics-copy-with-edit-mark
title: "Songs list searches Deezer: one tap saves a thin song row, lyrics copy with edit mark"
status: 💡 Pending
target: 2026-11-16
section: BusinessFeatures
parent: artists-songs
goal: Typing in the Songs list also shows Deezer songs; one tap stores a small row and opens it.
gate: Time-boxed throwaway trial first; Helder keeps or drops the idea after trying it on the device.
kind: change
---

# Songs list searches Deezer: one tap saves a thin song row, lyrics copy with edit mark

Typing in the Songs list also shows Deezer songs; one tap stores a small row and opens it.

**Status of the idea (Helder, 2026-11-16): not decided.** A time-boxed throwaway trial comes first; Helder tries it on the device and keeps or drops the idea. No requirements or design exist. The folder name still carries the first scope; version collapse, artist freeze and lyrics moved to the sibling item below.

## Where this came from

Helder asked whether the app should store only the Deezer id and read the rest at runtime, because the Songs and Artists lists look empty: Deezer is reachable only from the two form searches, so the library grows one hand-saved song at a time.

Outcome of the discussion:
- **Id-only storage: rejected.** Lists sort, page and search by title; duplicate checks and local search need the title on the device; Deezer offers search only; a track Deezer removes would lose its name. A song row is small (text and a picture URL; lyrics are the only heavy field).
- **Kept from the idea:** the Songs list itself could search Deezer, and a small row would be stored only when a song is taken.

## What Helder chose before pausing

| # | Choice |
|---|--------|
| 1 | Saved songs first, then a "From Deezer" section once 3 or more characters are typed; one Deezer call per search, no endless scroll. |
| 2 | A tap on a Deezer row saves the small row at once and opens the song. |
| 3 | The Artists list does not search Deezer. |

## Why it is paused

- The Songs page is entirely the shared list component (used by Songs, Artists, People and Venues). It has no place for content below the rows, so the section needs a governed component change, and its shared base view model assumes one row type.
- A one-tap save has no sheet for the cases where the form asks today (similar artist already saved, possibly duplicate song). The proposed answer, not confirmed by Helder: save in one tap when the case is clear, otherwise open the form pre-filled so the existing sheets ask.
- Helder's concern: the work may be over-engineering, and he may not like the result.

## Alternative Helder raised (not decided)

A starter set of songs fetched at first run (popular songs, possibly by region or by genres the user chooses), so the list has content from day one. Points to check before any spec: which chart and genre calls Deezer really offers; lyrics would stay fetched on first open because the lyrics source limits requests. Known weakness: a starter set still misses most requests at a karaoke night.

## Trial rules

Throwaway code on its own branch, no tests, shared components untouched on `develop`, hard time box, result written to `findings.md` in this folder. If the trial fails or runs out of time, the decision goes back to Helder.

Sibling item with the pieces that are certain: `../2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/`.

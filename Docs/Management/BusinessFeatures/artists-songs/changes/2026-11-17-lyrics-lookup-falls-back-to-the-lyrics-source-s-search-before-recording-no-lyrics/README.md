---
id: lyrics-lookup-falls-back-to-the-lyrics-source-s-search-before-recording-no-lyrics
title: Lyrics lookup falls back to the lyrics source's search before recording no lyrics
status: 💡 Pending
target: 2026-11-17
section: BusinessFeatures
parent: artists-songs
goal: When the exact title + artist lookup finds nothing, one fuzzy search of the lyrics source runs before a song is marked as having no lyrics; works in every language, no word lists.
kind: change
horizon: post-mvp
---

# Lyrics lookup falls back to the lyrics source's search before recording no lyrics

When the exact title + artist lookup finds nothing, one fuzzy search of the lyrics source runs before a song is marked as having no lyrics; works in every language, no word lists.

## Background (2026-11-17)

Helder's device check: several Deezer songs got no lyrics. The lookup (REQ-LYR, REQ-SLD-19 to 22) calls LRCLIB `api/get`, which needs an exact title + artist match; a miss is stored as *none found* and never retried. Stored Deezer titles are already Deezer's `title_short` (no version label, any language), so trimming words is not the fix and hand-made word lists ("ao vivo", "remastered", ...) must not be built: they cannot cover every language (Helder).

## Idea

Before a definite no-match is stored, run one LRCLIB search (`api/search`, tolerant of small differences) with the same title and artist, and accept a result only when its title and artist are equal under a language-neutral comparison done by the database or a documented rule. Possibly also send album and duration at pick time. Decide what to do with songs already marked *none found* (offer a one-time retry or not).

Post-MVP: no published version exists yet (Helder, 2026-11-17).

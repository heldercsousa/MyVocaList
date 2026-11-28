---
id: song-title-suggestions-come-from-the-itunes-search-api-ranked-by-popularity
title: Song title suggestions come from the iTunes Search API, ranked by popularity
status: 🟡 In Progress
target: 2026-11-28
parent: music-sourcing-strategy
goal: New Song title suggestions come from the keyless iTunes Search API (popularity order), with a visible search indicator and Apple Music attribution; MusicBrainz is used only after a pick, for the artist and release ids.
kind: change
---

# Song title suggestions come from the iTunes Search API, ranked by popularity

New Song title suggestions come from the keyless iTunes Search API (popularity order), with a visible search indicator and Apple Music attribution; MusicBrainz is used only after a pick, for the artist and release ids.

## Background (2026-11-28)

Helder: "MusicBrainz title search is unusable; resolve it or drop MusicBrainz." Live evidence (2026-11-28):
- Query "Don't Cry" and "don't cry gun": the iTunes Search API returns Guns N' Roses first.
- MusicBrainz returns irrelevant covers or nothing for the same queries.
- "Don't crie": iTunes partially corrects the typo.

Helder's approval for this change, recorded 2026-11-28 in chat: "Faça uso, e não me pergunte por autorização" (use it, do not ask me for authorization). This covers the governed `AutocompleteField` change in § Scope (f).

Risk note: the Apple page states that the Search API content is meant for promoting Apple store content. The remote rows carry an "Apple Music" attribution label (REQ (e)). Helder owns the decision to use the API for this purpose.

## Requirements

**(a) Remote title suggestions from iTunes.**
- REQ-ITS-01: Remote title suggestions come from `https://itunes.apple.com/search?entity=song&limit=15&term=<query>` (keyless). The request is a GET with no API key.
- REQ-ITS-02: The popularity order returned by the API is kept (no re-ranking of remote rows). One row per (title, artist) pair; the first occurrence is kept.
- REQ-ITS-03: Free text "title artist" works (the query is sent as the term, with the artist words included).

**(b) Search trigger and rate.**
- REQ-ITS-04: Remote search starts at 2 trimmed characters (local search keeps its own minimum).
- REQ-ITS-05: Remote search is debounced by about 350 ms after the last keystroke.
- REQ-ITS-06: Remote calls stay within 20 calls per minute (client-side limiter; a call beyond the budget is skipped and the local results stay visible).

**(c) Visible state.**
- REQ-ITS-07: While a remote search runs, the field shows a loading indicator (MD3 circular progress indicator in the trailing area of `AutocompleteField`, `IsSearching`).
- REQ-ITS-08: When a remote search finishes with no rows and no local rows, the list shows a "No results" row.

**(d) Pick behaviour.**
- REQ-ITS-09: On pick, the title and artist are put in the picked state.
- REQ-ITS-10: The artwork URL (`artworkUrl100`, rewritten to the 250 px size) is used as the suggestion row image and as the form fallback image. It is not downloaded or stored as a file.
- REQ-ITS-11: The MusicBrainz MBID lookup for the artist and the release runs AFTER the pick, non-blocking, and failure-silent. When MusicBrainz answers, REQ-MSRC-13, REQ-MSRC-15 and REQ-MSRC-16 keep working. When it does not, the pick still completes.

**(g) Typo tolerance and local re-rank (coordinator addition, 2026-11-28; no remote source tolerates typos, see `findings-song-search-alternatives-2026-11-28.md` on develop).**
- REQ-ITS-14: When iTunes returns fewer than 3 rows and the query has 2 or more words, the query is retried once with its last word dropped; retry rows are appended after the first answer's rows, duplicates removed.
- REQ-ITS-15: Typo-tolerant word match: two words match when they are equal, or when their Levenshtein distance is at most 2, the shorter word has 3 or more characters and the longer has 4 or more (case-insensitive; apostrophes removed). The local step only re-orders and filters rows iTunes returned; it never adds rows. Example: "crie" matches "cry" (distance 2, lengths 4 and 3).
- REQ-ITS-16: Remote candidates are re-ranked by token score = share of query words that match a title word (REQ-ITS-15). Higher score first; ties keep the iTunes popularity order. Acceptance example for tests: query "Don't crie" with candidates "Dance Floor" (iTunes first) then "Don't Cry" ranks "Don't Cry" first (score 2 of 2 against 0 of 2).
- Apple terms: no preview or artwork file is stored; artwork is referenced by URL only (REQ-ITS-10).

**(e) Attribution.**
- REQ-ITS-12: Remote rows show an "Apple Music" source label in place of the "MusicBrainz" label.

**(f) Governed component (`AutocompleteField`).**
- REQ-ITS-13: `AutocompleteField` gains an `IsSearching` state that shows the indicator in the trailing area while a search runs. The consumers are `ArtistFormPage`, `SongFormPage` (title and artist) and `PersonFormPage`.

## Scope and provider boundary

- `ITunesSearchProvider` implements `IMusicMetadataProvider` (`ProviderName` = "iTunes"). `SongSuggestionService` uses it for remote titles. Local songs stay first.
- MusicBrainz stays for artist search and for the post-pick MBID lookup. The MusicBrainz `SearchSongsAsync` path is no longer used for title suggestions.

## Acceptance criteria traceability

Each REQ above maps to at least one test (see `task-log.md` when it is written).

<!-- impl decision: iTunes rows have no ExternalArtistId, so the post-pick MBID lookup resolves artist and release ids from title + artist name; the 20/min budget is a client-side limiter in ITunesSearchProvider. -->

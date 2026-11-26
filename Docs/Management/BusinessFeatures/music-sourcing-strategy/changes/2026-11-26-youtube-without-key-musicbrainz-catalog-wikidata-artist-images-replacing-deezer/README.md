---
id: youtube-without-key-musicbrainz-catalog-wikidata-artist-images-replacing-deezer
title: YouTube without key + MusicBrainz catalog + Wikidata artist images, replacing Deezer
status: 💡 Pending
target: 2026-11-26
section: BusinessFeatures
parent: music-sourcing-strategy
goal: Replace Deezer and the key-based YouTube search with a MusicBrainz catalog, search-out/share-back YouTube links and Wikidata/Commons artist images, migrating existing Deezer rows in place.
gate: Helder reviews the spec before any code.
kind: change
---

# YouTube without key + MusicBrainz catalog + Wikidata artist images, replacing Deezer

Replace Deezer and the key-based YouTube search with a MusicBrainz catalog, search-out/share-back YouTube links and Wikidata/Commons artist images, migrating existing Deezer rows in place.


Origin: Helder, 2026-11-25/26 — no YouTube Data API key; "Aprovo uso do MusicBrain"; artist photos wanted. Evidence: `../../findings.md` §6 (terms, Deezer mix) and §7 (artist images). Supersedes, when approved, `../2026-11-22-deezer-vs-musicbrainz-systematic-comparison-with-pros-and-cons/` and the Deezer trials in `../../../artists-songs/changes/2026-11-16-*`.

## Why
- Deezer §III.2 forbids combining its Content with video; §III.4 forbids local copies (metadata included); §III.1 is non-commercial. The song form shows YouTube results next to Deezer data today.
- `YouTubeSearchService` needs a Data API key, which Helder will not use.

## Requirements
- **REQ-MSRC-01** The app makes no call to Deezer and shows no Deezer data or logo.
- **REQ-MSRC-02** Song/artist search and autocomplete use MusicBrainz (recording, artist, work), at most 1 request/s, debounced, with User-Agent `MyVocaList/<version> (<contact>)`.
- **REQ-MSRC-03** Saving a MusicBrainz result stores a local row (title, artist name, `ExternalProvider = "MusicBrainz"`, `ExternalId` = MBID); recordings of the same work can be grouped.
- **REQ-MSRC-04** No YouTube Data API call and no API key setting remain; `YouTubeSearchService` and the in-form results list are removed.
- **REQ-MSRC-05** "Search on YouTube" opens `youtube.com/results?search_query=<title> <artist> karaoke` outside the app.
- **REQ-MSRC-06** A YouTube link shared to the app or pasted (`youtu.be/{id}`, `watch?v={id}`, `/shorts/{id}`) stores only the 11-character video id against the local song id; invalid links are rejected with a message.
- **REQ-MSRC-07** Playback uses the official IFrame player in a WebView (`BaseUrl` = `https://<application id>`, `playsinline=1`), no overlays, YouTube branding visible, no audio extraction or background play. Video title/thumbnail are loaded at runtime, never stored.
- **REQ-MSRC-08** Artist photo: MusicBrainz → Wikidata relationship → P18 → Commons image by URL at runtime, with a credit line (author + licence + link); not stored on the device. No free image → initials placeholder.
- **REQ-MSRC-09** Artist bio = Wikidata description; discography = MusicBrainz release groups.
- **REQ-MSRC-10** Network failure on any source leaves local data usable and shows a non-blocking message.

## Migration of existing Deezer rows (never recreate)
- Rows are **edited in place**; ids, singer performance history and queue references never change (memory rule: song rows carry history).
- Existing `ExternalProvider = "Deezer"` rows: clear `ExternalProvider`/`ExternalId` and any stored Deezer image URL; keep title and artist name as the user's own local data (they become user-owned rows, not Deezer copies). Lyrics fields are untouched.
- Optional re-match: on first open (or a manual "Link to MusicBrainz" action), look up the MBID by title + artist; on a confident single match set `ExternalProvider = "MusicBrainz"` + MBID, else leave the row unlinked. Never merge or delete rows automatically.
- One EF migration (sequential-only file); idempotent; reversible.

## Out of scope
Lyrics source (Bandokê lyrics decision stays in `findings.md` §5); charts/"popular now"; Fanart.tv (until its terms are read); audio previews.

## Open questions for Helder
1. Contact string for the User-Agent (an app URL or a project email).
2. Free API during the free phase, then MetaBrainz plan or CC0 subset before any paid tier — confirm.
3. Clear Deezer-sourced titles' provenance silently, or tell the user once?

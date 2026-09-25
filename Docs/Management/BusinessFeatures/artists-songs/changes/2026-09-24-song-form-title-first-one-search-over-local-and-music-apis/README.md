---
id: song-form-title-first-one-search-over-local-and-music-apis
title: Song form title-first — one search over local and music APIs
status: 💡 Pending
target: 2026-09-24
section: BusinessFeatures
parent: artists-songs
goal: Title becomes the first field and searches local songs and music APIs by title or artist; a pick fills title and artist, or opens an existing song for editing.
gate: Helder approves the spec after the fuzzy-search and tracking-field evaluation.
kind: change
---

# Song form title-first — one search over local and music APIs

Title becomes the first field and searches local songs and music APIs by title or artist; a pick fills title and artist, or opens an existing song for editing.


## Status

Decisions captured from Helder, 2026-09-24. Spec not yet written. It waits on a read-only evaluation
(fuzzy search + API-tracking fields) and will build on the shared autocomplete component
(`DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`).

## Helder's decisions (2026-09-24)

1. **Title-first.** Title is the first field on the Song form.
2. **One search box, option B.** The Title field itself is the autocomplete. No separate search box.
   The typed text may be a title, an artist name, or both (e.g. "guns don't cry"). Results match
   **title and/or artist**.
3. **Sources: local DB and third-party music APIs** (Deezer etc.), in one list.
4. **Picking a local song, or an API song that already exists locally, opens it in edit mode.**
5. **Picking a new API song fills the form** (title, artist, and the other API fields); nothing is
   saved until Save. **The artist is set from the pick and created if it does not exist.**
6. **No pick → the text stays as typed** and becomes the title. The existing validations decide
   whether it can be saved. Helder: don't over-engineer this; no extra "unpicked text" handling.
7. **Typo tolerance wanted:** "dont cry" / "don cry" should find "Don't Cry". A similarity resource
   exists but Helder suspects it isn't working. Under evaluation.
8. **Simplicity review of the API-edit-tracking fields** on Song (manual-edit flags, merge sheet,
   save-as-new-version resolution). Helder considers them hard to maintain and a candidate for removal.
   Under evaluation, recommendation per field to follow.

## Related

- The **catalog auto-update on song save** is item CATALOG-PROP, specified in another session.
  Both touch `SongService` saves; coordinate at implementation.
- The song picker page ("Search music database") is expected to be replaced by this search.
  That makes autocomplete Q6 moot.

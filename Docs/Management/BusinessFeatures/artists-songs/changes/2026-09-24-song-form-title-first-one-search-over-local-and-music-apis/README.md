---
id: song-form-title-first-one-search-over-local-and-music-apis
title: Song form title-first — one search over local and music APIs
status: ✅ Done
target: 2026-09-24
section: BusinessFeatures
parent: artists-songs
goal: Title becomes the first field and searches local songs and music APIs by title or artist; a pick fills title and artist, or opens an existing song for editing.
gate: Helder's on-device E2E gate (plan § 7.2).
closed: 2026-11
kind: change
---

# Song form title-first — one search over local and music APIs

Title becomes the first field and searches local songs and music APIs by title or artist; a pick fills title and artist, or opens an existing song for editing.


## Status

Spec drafted, in review (2026-09-25): `requirements.md`, `design.md`, `tasks.md`. It builds on the
evaluation `findings.md` and on the shared autocomplete component
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

## Helder's decisions — second round (2026-09-24)

- **Title and Artist are always shown, in add AND edit mode.** Title is first and is the search
  box (option B). The search is optional: a user adding a local band's unrecorded song just types
  the title, types the artist (or picks an existing one), and types the lyrics.
- **No inline "create new artist" row.** An artist typed but not picked is **created on Save**,
  keeping the existing duplicate validation. An API pick whose artist is not local is handled the
  same way: created on Save.
- **Pick-lock is on for every autocomplete field** (shared component, no per-page switch).

## Helder's decisions — third round (2026-09-24)

1. **Near-duplicate artist guard on Save: YES.** If the typed (unpicked) artist does not exist but
   the existing similarity scorer finds a close one, one BottomSheet asks: use the existing artist,
   or create the typed one. It appears only in that case.
2. **Pick-lock applies to Title too.** API titles with extras ("(Remastered 2011)", "– Live") are
   not trimmed in place: the user taps X and retypes.
3. **Title is not changeable once it came from a pick, and the search is off in edit mode.**
   Helder: changing the title of an API-sourced song makes little sense. Notes such as "live" or
   "acoustic" go in **Version**. Version must be shown in search results, to tell versions of the
   same song apart. Orchestrator's reading, to be confirmed in the spec review:
   - in edit mode the Title is not a search box;
   - an API-sourced song's Title is read-only;
   - a manually entered song's Title stays editable.
4. **Tracking fields (findings.md § 3): approved as recommended.**
   - DROP `HasManualEdits` (Song + Artist) and the merge sheet.
   - SIMPLIFY the resolution sheet to "Open existing / Save as new version".
   - KEEP Song `ExternalId`/`ExternalProvider` with a unique `(Provider, ExternalId)` index.
   - Keep the Artist external columns but don't build on them.
5. **Typo tolerance: Deezer is the main API, plus a local SQLite FTS5 trigram index.** The
   ranking-step question (the similarity scorer vs the no-C#-normalisation rule) was explained
   to Helder; resolution pending.
6. **The four smaller bugs in findings.md are absorbed by this redesign**, at the orchestrator's
   discretion. The spec must carry each one as an explicit AC so none is lost:
   - artist lookup given the song's external id;
   - an accent-only title difference returns an exact match with no id;
   - the merge sheet writes with every field unchecked (removed along with the merge sheet);
   - edit-save uniqueness ignores Version.

## Helder's decisions — fourth round (2026-09-24)

- **Picker pages removed entirely.** Helder: none of them ever worked as expected (consistent with
  findings § 1, the provider DI bug). Third-party search happens only through the autocomplete fields.
  - Remove SongFormPage's "Search music database" row + SongPickerPage.
  - Remove the equivalent link on **ArtistFormPage** + ArtistPickerPage.
  - YouTubeSearchPage is not covered by this ruling; it is open.
- **Artist form:** by the same reasoning its name field should be the shared autocomplete (local
  + Deezer artists). Scope to be confirmed by Helder.
- **Typo tolerance: Deezer primary + local FTS5 trigram retrieval + FuzzySharp ranking (option a).**
  Needs a documented exception to the no-C#-normalisation HARD RULE, limited to ranking a bounded
  list the DB already retrieved (never filtering or dedup).
- The 1-minute picker check (ask A) is withdrawn: the picker is being deleted, and the DI bug is
  a prerequisite of this item regardless.

## Helder's decisions — fifth round (2026-09-24)

- **Artist form name field: CONFIRMED.** It becomes the shared autocomplete, searching local
  artists and Deezer.
- **YouTube search: out of scope for now.** YouTubeSearchPage and the "Search YouTube (API)" row stay
  as they are. The API-key requirement is a problem that needs its own discussion before any change.

## Helder's decisions — sixth round (2026-09-25)

- **HARD RULE: the app never creates two artists with the same name** (under the DB collation).
  - A typed artist that exactly matches an existing one is **reused**, with no friction (Q3 = yes).
  - To keep two real people apart, the **user** makes the names distinct, e.g.
    "Carlinhos Sanfoneiro - Grajaú". Their songs and catalog also tell them apart.
  - "Metallica" typed → the single existing Metallica is picked; if none exists, it is created.
  - A near-duplicate ("Metalica") → the recommended warning sheet.
  - Any duplicate or validation failure → a warning, or the save is refused when it is an error.
- **Q13: Title is read-only in edit mode for EVERY song** (Helder: simpler for the MVP). The
  API-sourced vs manual distinction is dropped.
- **Q14: agreed.** The Song form's Artist field searches local artists only. Rationale: a user who
  only remembers the artist types it in Title and picks a song. Showing an artist's songs or albums
  is a possible future feature.
- **Q15: agreed.** Picking a local artist on the Artist form opens that artist.

## Helder's decisions — seventh round (2026-09-26)

- **No concurrent-write handling.** The DB lives only on the user's device (single user, single
  app), so the "two saves collide" race needs no dedicated task or message. The unique name index
  stays as a safety net; drop the race-specific task and AC.
- **Q13 REVERSED: Title stays EDITABLE in edit mode, for every song** (plain text, no search).
  Having to delete and re-create a song to fix a title is unacceptable. A song row carries history
  that must be preserved: YouTube links, catalog rows, and the per-singer performance history from
  karaoke events. That history is crucial, because future AI suggestions ("songs this singer could
  try next") are based on what each singer has performed. Songs must never be deleted and
  re-created as a workaround.

## Related

- The **catalog auto-update on song save** is item CATALOG-PROP, specified in another session.
  Both touch `SongService` saves; coordinate at implementation.
- The song picker page ("Search music database") is expected to be replaced by this search.
  That makes autocomplete Q6 moot.

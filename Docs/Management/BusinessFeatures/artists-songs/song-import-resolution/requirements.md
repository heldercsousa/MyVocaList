# Song Import & Entity Resolution — Requirements

> Feature folder: `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/`
> Parent feature: Artists & Songs Catalog
> Status: 📋 Spec (in progress)
> Created: 2026-06-13

## Purpose

Provide a senior-grade pipeline for getting song/artist data into the local database — whether typed manually or imported from a 3rd-party metadata API (Deezer, MusicBrainz, and later YouTube) — that:

1. Correctly decides whether incoming data should **insert a new record** or **update an existing one**.
2. Avoids accidental duplication while allowing **deliberate version variants** (live, acoustic, remix) as distinct records.
3. Surfaces near-duplicate candidates (typos, spacing, accents) for the user to confirm rather than silently creating a second record.
4. Never silently overwrites data the user has manually edited.
5. Does all matching with **database-side collation** plus **bounded** on-device similarity scoring — no full table scans, no C#-side normalization.

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Candidate** | A prospective song or artist to be saved, originating from manual entry or an API result. Carries optional `ExternalProvider` + `ExternalId`. |
| **Resolution** | The engine's decision about a candidate: `NoMatch`, `ExactExternalMatch`, `ExactLocalMatch`, or `FuzzyCandidates`. |
| **Version / Variant** | A `Song.Version` label distinguishing recordings of the same title by the same artist (e.g. "Live", "Acoustic", "Remix 2010"). Empty string `""` = the canonical version. |
| **Exact external match** | An existing record whose `(ExternalProvider, ExternalId)` equals the candidate's. |
| **Exact local match** | An existing record whose `(ArtistId, Title, Version)` equals the candidate's under `NOCASE_NOACCENT` collation. |
| **Fuzzy candidate** | An existing record within a configured string-distance threshold of the candidate, surfaced for user confirmation. |
| **Manual edit flag** | `HasManualEdits = true` marks a record the user changed after import; protects it from silent API overwrite. |
| **Merge sheet** | A `dx:BottomSheet` showing per-field API-value-vs-current-value choices when updating a manually-edited record. |

## User Stories & Acceptance Criteria

### US-1 — Manual song save with duplicate awareness
As an admin entering a song by hand, I want to be warned when it looks like one already in the catalog, so I don't create accidental duplicates but can still create an intentional version.

- **AC-1.1** GIVEN a new song with `(ArtistId, Title, Version="")` that exactly matches an existing record under collation, WHEN the user saves, THEN the app presents a resolution sheet offering "Update existing", "Save as new version", or "Cancel" — it does NOT silently reject.
- **AC-1.2** GIVEN the user chooses "Save as new version", WHEN no `Version` is supplied, THEN the app requires a non-empty `Version` value before committing (cannot create two records with identical `(ArtistId, Title, "")`).
- **AC-1.3** GIVEN a new song whose `(ArtistId, Title, Version)` triple is unique under collation, WHEN the user saves, THEN it inserts directly with no resolution sheet.
- **AC-1.4** GIVEN a new song within the fuzzy threshold of an existing record (but not exact), WHEN the user saves, THEN the resolution sheet lists the fuzzy candidate(s) with the option to update one of them or proceed as a new record.

### US-2 — API import of a song
As an admin importing a song from Deezer/MusicBrainz, I want the app to find the right local record (or create one) and persist the external identity, so future imports are recognised.

- **AC-2.1** GIVEN an API result selected in the Song Picker, WHEN it has `(ExternalProvider, ExternalId)` matching an existing local song, THEN the app resolves to `ExactExternalMatch` and routes to the update path (not insert).
- **AC-2.2** GIVEN an API result with no external match but an exact `(ArtistId, Title, Version)` local match, THEN the app resolves to `ExactLocalMatch` and offers to attach the external identity to that record.
- **AC-2.3** GIVEN an API result with no exact match, THEN the engine surfaces fuzzy candidates (if any within threshold) for confirmation, else resolves to `NoMatch` and inserts.
- **AC-2.4** WHEN a song is inserted or updated from an API result, THEN `ExternalProvider` and `ExternalId` are persisted on the record. (Core defect: `SongService.CreateSongAsync` already accepts these params, but the call site in `SongFormViewModel` omits them, and `UpdateSongAsync` has no external-identity params at all — both are fixed here.)
- **AC-2.5** GIVEN a song candidate references an artist, WHEN the artist is resolved, THEN the artist is resolved/created FIRST (Song.ArtistId is mandatory) before the song is committed, in a single logical operation.

### US-3 — Artist resolution during import
As an admin, I want imported songs to attach to the correct existing artist rather than creating duplicate artists.

- **AC-3.1** GIVEN an API artist with `(ExternalProvider, ExternalId)` matching an existing artist, THEN resolve to that artist (no new artist row).
- **AC-3.2** GIVEN an API artist name exactly matching an existing artist under `NOCASE_NOACCENT`, THEN resolve to that artist and (if not already set) attach the external identity.
- **AC-3.3** GIVEN an API artist name within fuzzy threshold of existing artist(s), THEN surface candidate(s) for the user to confirm "same artist" vs "create new artist".
- **AC-3.4** GIVEN no match, THEN create the artist with its external identity and proceed.

### US-4 — Conflict-safe update of edited records
As an admin who has corrected a record, I want my edits protected from being silently overwritten by a later API import.

- **AC-4.1** GIVEN an update target with `HasManualEdits = false`, WHEN updating from an API result, THEN non-empty API fields overwrite local fields and `HasManualEdits` remains `false`.
- **AC-4.2** GIVEN an update target with `HasManualEdits = true`, WHEN an API import would change one or more fields, THEN the app presents a merge sheet listing each differing field with API-value and current-value, and applies only the user's per-field choices.
- **AC-4.3** GIVEN the merge sheet, WHEN the user accepts no API values, THEN the record is unchanged and no write occurs.
- **AC-4.4** WHEN the user manually edits any field of an imported record, THEN `HasManualEdits` is set to `true`.

### US-5 — Bounded, correct matching (DB care)
As the system, matching must be correct and performant at catalog scale.

- **AC-5.1** All exact matching uses DB-side `NOCASE_NOACCENT` collation via `EF.Functions.Collate` — no `ToLowerInvariant`, `RemoveDiacritics`, or `*Normalized` columns (hard rule, constraints-registry.md).
- **AC-5.2** Fuzzy scoring runs in C# only over a **bounded candidate pool** pre-filtered by a collation query (e.g. first-token / prefix), never over the full table.
- **AC-5.3** The `(ArtistId, Title, Version)` unique index enforces dedup at the database level; the engine never relies solely on application checks.
- **AC-5.5 (migration safety)** The migration that introduces `Version` and the 3-column unique index must fail loudly if a pre-existing duplicate `(ArtistId, Title)` exists rather than silently dropping the old constraint. An integration test asserts the new unique index rejects a duplicate `(ArtistId, Title, Version)` insert.
- **AC-5.4** `Version` is non-nullable with default `""` so the unique index treats canonical records as collidable (SQLite multiple-NULL behavior is thereby avoided).

### US-6 — Atomic save with YouTube URLs (BUG-009)
As an admin, I want to add karaoke URLs while creating a new song without being told to "save first".

- **AC-6.1** GIVEN New Song mode, WHEN the user adds YouTube URLs before saving, THEN URLs are buffered in memory with no blocking error.
- **AC-6.2** WHEN the song is saved, THEN the song and all buffered URLs persist in a single transaction (`CreateSongWithUrlsAsync`); a failure rolls back both.

## Blocking-bug acceptance criteria (folded in)

These pre-existing specs (`artists-songs/bugs/`) are prerequisites for an end-to-end demo and are absorbed here:

- **AC-B5 (BUG-005)** `SongFormViewModel.SaveAsync` catches exceptions and shows an error snackbar; a save failure is never silent. Regression test included.
- **AC-B6 (BUG-006)** Navigation commands to picker pages use `allowConcurrentExecutions: false`; a double-tap cannot register the messenger twice or crash.
- **AC-B7 (BUG-007)** Picker pages suppress the Shell chrome back button (`BackButtonBehavior IsVisible=False`) so only one back affordance renders.
- **AC-B8 (BUG-008)** Song form artist field: autocomplete-only (no free text), auto-clears on blur without a valid selection, pre-populates in Edit mode, locks when the artist came from an API import.
- **AC-B10 (BUG-010)** `SongPickerViewModel` exists, is DI-registered, and `SongPickerPage` injects it (not `QueueSongPickerViewModel`); selecting a result drives the resolution engine.
- **AC-B4 (BUG-004)** The `BottomSheetTitle` style exists in `MaterialStyles.xaml` (MD3 titleLarge) before any resolution/merge sheet references it — style-must-exist-before-use.

## Out of Scope

- Lyrics API provider implementation (`ILyricsProvider` stays a placeholder — separate backlog item).
- Artist↔Person identity linking (`ArtistMember`) — separate backlog item.
- AI/embedding-based semantic matching — fuzzy uses on-device string distance only.
- Background re-sync / scheduled re-import of the whole catalog.
- Cross-artist song deduplication (a song with the same title under two different artists is intentionally two records).
- YouTube share-intent upsert (tracked separately in `youtube-share/findings.md`); this feature provides the resolution engine it will later reuse.

## Invariants & Postconditions

- **INV-1** Every `Song` always has a valid non-null `ArtistId` after any insert/update.
- **INV-2** No two `Song` rows share `(ArtistId, Title, Version)` under `NOCASE_NOACCENT`.
- **INV-3** `Artist.Name` remains globally unique under `NOCASE_NOACCENT`; `Artist.ExternalId` remains unique.
- **INV-4** A record with `HasManualEdits = true` is never mutated by an API import without explicit per-field user consent.
- **INV-5** No matching path performs C#-side case/accent normalization or a full-table fuzzy scan.

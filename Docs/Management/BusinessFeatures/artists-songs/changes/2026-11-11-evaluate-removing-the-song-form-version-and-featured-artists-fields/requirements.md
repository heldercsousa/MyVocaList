# Remove the Song Version and Featured artists fields — Requirements

Decision (Helder, 2026-11-19): remove both fields entirely, from the form, the model and the database. The 2026-11-11 item was an evaluation stub; this change replaces it (the folder slug keeps the original name so the backlog ID stays stable).

Everything this change supersedes, reduces or rewords is listed row by row in `supersession.md` (same folder). Shipped specs stay immutable; this folder plus `supersession.md` is the record.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| Song identity | The pair `(Artist, Title)` compared with the `NOCASE_NOACCENT` collation (case- and accent-insensitive). |
| Exact match | An existing song with the same song identity as the song being saved or imported. |
| Near-duplicate | An existing song whose title is trigram-similar but whose identity differs (shipped behaviour, unchanged). |
| Deezer pick | The user choosing a remote Deezer result in the Song form or the Songs list search. |
| Open existing | The shipped duplicate-sheet action that opens the exact-match song (shipped term; replaces "use existing"). |
| Supporting text | The secondary line of a Songs list row (MD3 List item). |

## Invariants

- **INV-RMV-1 (replaces song-import-resolution INV-2):** at most one `Songs` row exists per song identity, enforced by the unique index `IX_Songs_ArtistId_Title`.
- **INV-RMV-2:** no song row is deleted or recreated by this change; every `Songs.Id` that existed before still exists after, with its foreign-key rows (`Catalog`, `SongKaraokeUrls`, `QueueEntries`) intact.

## Requirements (EARS / Given-When-Then)

| ID | Requirement |
|----|-------------|
| REQ-RMV-01 | **Given** the Song form in create or edit mode, **when** it is shown, **then** it has no Version field and no Featured artists field; the remaining input fields keep their shipped order (Title, then Artist, then Lyrics, as in REQ-SFT-01), plus the existing picture/image and Lyrics-origin controls that are not part of this change. |
| REQ-RMV-02 | **Given** the code base, **when** it is built, **then** `Song`, `SongConfiguration` and the model snapshot define neither `Version` nor `FeaturedArtists`, and the unique index is `IX_Songs_ArtistId_Title` on `(ArtistId, Title)` with the existing collation. |
| REQ-RMV-03 | **Given** a database at the previous migration (`AddSongLyricsOrigin`), **when** the new migration `RemoveSongVersionAndFeaturedArtists` runs, **then** both columns are dropped, the 2-column unique index exists, the `ExternalProvider`+`ExternalId` unique index and the search indexes still exist, `PRAGMA foreign_key_check` returns no rows, and the row count of `Songs` is unchanged. |
| REQ-RMV-04 | **Given** rows that share `(ArtistId, Title)` and differ in Version, **when** the migration runs, **then** (a) a row whose trimmed Version is empty keeps its title; the lowest-`Id` row keeps its title when more than one has an empty Version; (b) every other colliding row gets ` (<trimmed Version>)` appended to its title; (c) any rows still colliding after (b), or colliding with a pre-existing row, get ` #<Id>` appended; (d) when the new title would exceed `MaxTitleLength` (250), the original title is truncated so that title plus suffix is exactly at most 250 characters, the suffix is never cut; (e) the migration never fails because of duplicates. A whitespace-only Version counts as empty. |
| REQ-RMV-05 | **Given** any database, **when** the migration runs, **then** `FeaturedArtists` text is discarded without a copy (accepted loss, decided by Helder). `Down` re-adds `Version` (`TEXT(60) NOT NULL DEFAULT ''`, collation `NOCASE_NOACCENT`) and `FeaturedArtists` (nullable TEXT, same collation), drops the 2-column index and recreates the 3-column one; it does not restore data and does not undo the REQ-RMV-04 renames (lossy by design). |
| REQ-RMV-06 | **Given** a song with identity X exists, **when** the user creates a song, edits another song into identity X, or an import resolves to identity X, **then** duplicate detection reports an exact match using `(Artist, Title)` only, case- and accent-insensitively, and the save is not performed. |
| REQ-RMV-07 | **Given** an exact match, **when** the duplicate `dx:BottomSheet` is shown, **then** it offers only *Open existing* and *Cancel*, has no Version text and no Version entry, and there is no *Save as new version* action. **Given** a near-duplicate (different title), **then** the sheet is unchanged and still offers *Save as new song*. `SongDecision.SaveAsNewVersion` and `SaveField.Version` no longer exist. |
| REQ-RMV-08 | **Given** Deezer returns several tracks with the same artist and title but different `title_version`, **when** suggestions are built, **then** `title_version` is ignored and no Version-based collapse step exists. The REQ-SLD-06 grouping (Deezer artist id plus short title) stays as shipped; only the library-match check against local songs uses song identity. |
| REQ-RMV-09 | **Given** a song row in the Songs list, **when** it is rendered, **then** its Supporting text is the artist name only (no version, no "feat." part). |
| REQ-RMV-10 | **Given** the save and import paths, **when** a song is created, edited or imported, **then** the persisted song contains only fields that remain in the model, and no validation of Version length or FeaturedArtists length/trimming runs (the FeaturedArtists test in `PersistedStringTrimmingTests` is deleted; REQ-TRIM-07 itself is generic and stays). The duplicate message is the single string "This artist already has a song with this title." The 250-character title limit and its validation are unchanged. |
| REQ-RMV-11 | **Given** an existing song, **when** a Deezer pick fills the form or an edit is saved, **then** artwork, Lyrics and Lyrics-origin behave exactly as in the shipped specs (REQ-ART-12 minus the Featured artists clause, REQ-SLD-08 minus the Version clause). |

## Edge cases and error paths

| Case | Behaviour |
|------|-----------|
| Edit renames song A to the identity of song B | Exact match → duplicate sheet (REQ-RMV-06/07); A is not saved. |
| Title differs only by case or accent from an existing song of the same artist | Exact match (collation). |
| Migration: two empty-Version rows with the same identity | Lowest `Id` keeps the title, the other gets ` #<Id>` (REQ-RMV-04 c). |
| Migration: renamed title collides with an existing row | Rule (c) applies. |
| Migration: title near 250 characters | Truncation rule (d). |
| Dev/device database without collisions | Migration only drops columns and swaps the index; no title changes. |

## Acceptance criteria → test mapping

| REQ | Level | Test location (created or rewritten in the wave named in `tasks.md`) |
|-----|-------|----------------------------------------------------------------------|
| REQ-RMV-02, 03 | B | `Tests/Integration/Migrations/` real-SQLite temp file seeded at `AddSongLyricsOrigin`, then migrated; asserts schema, indexes, FK check, row count |
| REQ-RMV-04, 05 | A | same harness: one test per rule (a)–(e), plus a `Down` test |
| REQ-RMV-06 | A | `SongServiceTests`, `SongServiceUpdateIntegrationTests`, `SongResolutionServiceTests`, `SongRepositoryResolutionTests` |
| REQ-RMV-07 | A | `SongFormViewModelTests`, `SongServiceSaveFromFormTests` |
| REQ-RMV-08 | A | `DeezerProviderTests`, `SongSuggestion*` tests |
| REQ-RMV-09 | B | `SongListItemDtoTests`, `SongListItemProjectionTests` |
| REQ-RMV-01, 10, 11 | C/B | VM tests asserting the removed members are absent and unchanged behaviour holds; XAML verified by build plus Helder's on-device check |
| INV-RMV-1 / INV-RMV-2 | A | migration tests above |

Tests that encode superseded ACs (listed by `supersession.md`) are deleted or rewritten in the same wave as the production change that supersedes them, so the solution compiles and the suite is green at each wave boundary. This is spec-driven, not the Builder weakening a test.

## Product consequence (intentional)

Removing *Save as new version* means one artist can no longer have two songs with the same title (different recordings, live versions and similar must differ in the title text). Helder decided this on 2026-11-19.

## Out of scope

Song identity by lyric and richer Deezer data (separate discussion item). No change to the Artists catalog or YouTube features. No What's-new entry is added by this task (internal data-model simplification; Helder can request one).

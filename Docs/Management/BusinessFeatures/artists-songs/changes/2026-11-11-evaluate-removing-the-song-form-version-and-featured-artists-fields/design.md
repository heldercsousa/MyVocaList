# Remove the Song Version and Featured artists fields — Design

Requirements: `requirements.md` (REQ-RMV-01..11, INV-RMV-1/2). Supersession rows: `supersession.md`.

**Consumer map:** the Explore survey (2026-11-19) is not persisted; the source of truth is a fresh `Grep` of `\bVersion\b`, `SongVersion`, `FeaturedArtists`, `Featured`, `title_version`, `SaveAsNewVersion` and `ExistsByTitleVersion` over `MyVocaList*`, `Tests` and `Infra` at the start of each wave. Unrelated hits: app release version (`VersionCheckService`, `WhatsNew*`, `UpdateAvailable*`, `BuildInfo`, `About*`, `FeedbackService`, `ReleaseEntry`, `releases.json`).

## Layers

Domain (entity, repository interfaces, domain types) → Infra (EF config, migration, repositories) → Services (business logic: duplicate detection, resolution, suggestions, Deezer mapping) → Contracts (DTOs) → MAUI UI (page, ViewModel). Business logic stays in Services; the migration owns the data rename; the ViewModel only loses members and flows, no logic moves into it.

## Integration contracts (target signatures)

```csharp
// ISongRepository (Domain)
Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, int? excludeSongId, CancellationToken ct);
Task<int?> FindIdByTitleForArtistAsync(int artistId, string title, CancellationToken ct);
Task<IReadOnlyDictionary<(string Title, string ArtistName), int>> FindByTitleAndArtistNamesAsync(
    IEnumerable<(string Title, string ArtistName)> keys, CancellationToken ct);
// Implementors keep the existing return shapes, parameter order, CancellationToken and overload set;
// only the Version parameter / tuple element is removed. Do not change anything else.

// SongFormSaveRequest: remove Version, FeaturedArtists.     Candidates: remove Version, FeaturedArtists.
// SongFormSaveOutcome.SaveField: remove Version.            SongSearchHit / ResolutionResults: remove Version.
// SongDecision: remove SaveAsNewVersion (SaveAsNewSong stays, near-duplicates only).
// ISongService: remove ValidateVersionInput; remove featuredArtists/version parameters from
//   CreateSongAsync, UpdateSongAsync, CreateSongWithNewArtist*; SongService: remove MaxVersionLength.
// DTOs: SongListItemDto (remove FeaturedArtists, Version; SupportingText => artist name),
//   MusicSearchResultDto (remove FeaturedArtists, Version), SongSuggestionDto (remove Version).
```

Exact existing signatures are authoritative; the list above names what is removed.

## Duplicate sheet decision table (REQ-RMV-07)

| Match type | Options (dx:BottomSheet) |
|------------|--------------------------|
| Exact match `(Artist, Title)` | Open existing · Cancel |
| Near-duplicate (title differs) | unchanged: includes Save as new song |
| No match | save proceeds |

If a code flow does not fit this table, the implementor stops and logs `blocked: spec gap` for Helder.

## Preconditions / postconditions

- **Migration pre:** database at `AddSongLyricsOrigin`. **Post:** REQ-RMV-03, INV-RMV-1, INV-RMV-2.
- **Save pre:** artist and title valid (unchanged rules). **Post:** a single `Songs` row per identity.

## Migration `RemoveSongVersionAndFeaturedArtists` (REQ-RMV-03/04/05)

`Up` runs via `migrationBuilder.Sql` before the schema operations. `Songs.Title` and `Songs.Version` use the registered `NOCASE_NOACCENT` collation (`CollationInterceptor`); the SQL must run after the collation is registered, so the migration test must use the app's real connection setup, not a bare `SqliteConnection`. Version is never NULL (`NOT NULL DEFAULT ''`); use `TRIM(Version)`.

1. Rename pass A (rule a/b/d): for each colliding row that is not the keeper (keeper = empty-Version row with the lowest `Id`, else the lowest-`Id` row overall), set `Title = SUBSTR(Title, 1, 250 - LENGTH(suffix)) || suffix`, `suffix = ' (' || TRIM(Version) || ')'` when `TRIM(Version) <> ''`.
2. Rename pass B (rule c/d): loop until no `(ArtistId, Title)` duplicates remain: append `' #' || Id` (truncating as in pass A) to every row that still shares identity with a lower-`Id` row.
3. Drop index `IX_Songs_ArtistId_Title_Version`; drop columns `Version`, `FeaturedArtists` (EF performs the SQLite table rebuild); create unique index `IX_Songs_ArtistId_Title` on `(ArtistId, Title)` with the existing collation.
4. `Down`: per REQ-RMV-05 (lossy, does not undo the renames).

> **Spec updated [2026-11-20]:** impl decision — EF `DropColumn` rebuilds the SQLite table and breaks the FTS triggers ("no such table: main.Songs"), so both drops are native `ALTER TABLE ... DROP COLUMN` (same approach as `AddSongLyricsOrigin` Down); the 3-column index is dropped before the renames (a rename can collide under it, REQ-RMV-04 e); pass B runs three times (chains beyond that fail the unique index); "empty" whitespace is `char(32, 9, 10, 13)`. Details: task-log T2.

Sequential-only files: `Migrations/*`, `AppDbContext.cs`, `MauiProgram.cs`, spec `tasks.md`.

## Waves (DRY Onion; tests move with their wave)

1. **Domain + Infra + their tests** — T1 then T2 (T2 touches `Migrations/*` and starts after T1 lands; not parallel).
2. **Contracts + Services + their tests** — T3, T4 (parallel-safe: disjoint files).
3. **UI + VM tests** — T5 then T6 (one XAML file, build, next).
4. **Verification + docs** — T7 full test run, T8 spec reconciliation and task-log.

Wave boundary rule: solution builds and its tests pass (or the tests of a not-yet-migrated wave are compiled out only if impossible otherwise; in that case the implementor logs it).

## Spec reconciliation (T8)

Shipped specs are immutable: record the change via this folder, `supersession.md`, and a `spec-changelog.md` line in the parent `artists-songs` folder; no edits inside shipped specs.

## Risks

| Risk | Mitigation |
|------|------------|
| Row loss or FK break during the table rebuild | Real-SQLite migration test: row count, `PRAGMA foreign_key_check`, index list (REQ-RMV-03) |
| Renamed titles surprise the user | Only on real collisions; the old Version stays visible in the title |
| Title length overflow from suffixes | Truncation rule (d), tested |
| Lossy `Down` | Stated in REQ-RMV-05 |
| Remote suggestion lists show near-identical entries | Accepted; richer Deezer handling lives in the separate discussion item |
| Large test edit surface | Rewrite per wave; each removed AC has its test removed in the same commit |

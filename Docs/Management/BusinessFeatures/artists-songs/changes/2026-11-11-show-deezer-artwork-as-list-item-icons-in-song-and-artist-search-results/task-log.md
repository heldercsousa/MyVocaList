# Task log: Deezer artwork in the Song form options and the Songs list

Branch `feature/deezer-artwork` · worktree `.claude/worktrees/deezer-artwork` · started 2026-11-13.

## Authority

Helder, 2026-11-13: keep every activity tracked in the proper docs, be autonomous until done, take
decisions alone unless his participation is crucial, work in a worktree, do not ask. Every gate below
marked "pre-authorised" rests on that instruction. The decisions taken for him are `design.md § 1`
(D1…D17). The device check (T11) is his.

### Checkpoint
- **Phase:** T0 spec — spec and plan reviews running.
- **Next:** apply review fixes → commit docs → dispatch T1.
- **Context manifest:** `requirements.md`, `design.md`, `plan.md`, `tasks.md`, this file, the two component READMEs (paths in `tasks.md`).

## T0 — Evaluation and spec (2026-11-13) — Done

- **Request part 1 (shared autocomplete on the Song form):** already in place. `SongFormPage.xaml:32`
  (`titleField`) and `:66` (`artistField`) are `AutocompleteDXField` since commit `6f7b6bbf`. No change.
- **MD3 review:** official m3.material.io lists, menus, search and text-field pages captured with
  Playwright on 2026-11-13 (lists and menus reused from the 2026-11-12 capture); verbatim quotes in
  `findings.md`. Internal guideline `.claude/library/m3-lists.md` compared: sizes confirmed; image
  corner radius, placeholder and several spacing values are not in the official docs (see `findings.md` Step D).
- **Deezer check (live public API, 2026-11-13):** `search/track` returns `album.cover_small|medium|big|xl`,
  `album.title`, `artist.picture_*`, `md5_image`; `search/artist` returns `picture_*`. All square,
  on `cdn-images.dzcdn.net`, content-addressed by a hash. No lyric field.
- **Code survey (Explore subagent):** no image data anywhere today (provider, DTOs, entities, list
  projections). The option row template is fixed inside `AutocompleteDXField`. The Songs list shows
  Featured artists as supporting text, not the artist. `ArtistRepository.SearchByNameAsync` projects a
  catalog count of 0, so every Artist option reads "No catalog" (fixed here as REQ-ART-10).
- **Other findings of the survey, not part of this item** (reported to Helder at close-out):
  the Artist form never binds its `NameOptionSource` (plain `TextEdit`, owned by the pending
  ArtistFormPage item); `ArtistFormPage.xaml` declares `ArtistListItemDto` while the ViewModel holds
  `ArtistListItem`; `ProviderBadgeText` expects `"deezer"` but `"Deezer"` is stored; `IX_Artists_ExternalId`
  is unique on the id alone, not per provider; the Deezer artist id is never stored on an artist.
- **Backlog:** this item set 🟡 In Progress; two component change items registered (m3-lists,
  autocomplete-component), each holding its four gates.
- **Gates:** spec approval, component Gate 4 and plan approval: pre-authorised by Helder 2026-11-13.

- **Baseline (worktree, before any code):** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 967 passed, 0 failed, 0 skipped.
- **Spec review (fresh `spec-reviewer`):** PASS WITH FIXES, no blocker. Applied: device-check task
  number (T11); project decisions separated from MD3 rules (new D18 row height, D19 one-line text;
  D5, D8 and the § 2 table reworded); Deezer API evidence added to `findings.md`; image URL
  definition aligned with `ImageUrlRules` (host not checked); REQ-ART-14, 16 and 18 completed
  (pending images survive a refused Save and sheets; wording after X on Title; example for X on
  Artist then local pick); planned edit of four superseded tests stated in `design.md § 6`.
- **Plan review (fresh `plan-reviewer`):** FAIL with 2 blockers, both fixed in `plan.md`:
  (B1) C# expression trees cannot omit optional arguments, so every `IQueryable` projection of the
  three changed records passes all arguments, and the raw-SQL select gains the two columns;
  (B2) the two lookups that feed promoted rows (`FindByExternalIdsAsync`,
  `FindByTitleAndArtistNamesAsync`) are now in T2 with integration tests. Also applied: T5 brief made
  concrete (`SaveFormResolution.FillArtistImage`, private helper parameters, public signatures
  unchanged); REQ-ART-23 note in T6 and an offline step in T11; task interface table (consumes,
  produces, demo, review lane); the four test edits listed in T8.

### Changed files
- `requirements.md`, `design.md`, `findings.md`, `plan.md`, `tasks.md`, `task-log.md` (this folder), `README.md` (status)
- `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/README.md`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/README.md`
- `Docs/Management/BACKLOG.md` (generated), `MyVocaList.sln` (registration)

## T1 — ImageUrl on Song and Artist (2026-11-13) — To Review

**Plan:** `plan.md § T1`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### What was done
- `Song.ImageUrl`, `Artist.ImageUrl` (`string?`), new `Domain/Rules/ImageUrlRules.cs` (`MaxLength = 500`; `Normalize`: trim, null when empty, over 500, or not an absolute https URI).
- `SongConfiguration` / `ArtistConfiguration`: `ImageUrl` optional, `HasMaxLength(ImageUrlRules.MaxLength)`.
- Migration `AddImageUrlToSongsAndArtists`: `Up` = two `AddColumn` (nullable TEXT). `Down` = two raw `ALTER TABLE ... DROP COLUMN`.

### Rollback finding and decision
- The generated `Down` used `DropColumn`. On SQLite, EF rebuilds the table, which failed with `error in trigger SongSearch_artist_au: no such table: main.Songs` because the M3 FTS triggers reference Songs/Artists. This broke `SongMigrationTests.M3_Down_DropsTablesAndTriggers` (existing test, untouched).
- Decision (orchestrator, under Helder's 2026-11-13 autonomy instruction): native `ALTER TABLE ... DROP COLUMN` in `Down`. No index, trigger or view references `ImageUrl`, so the in-place drop is allowed.

### Deviations
1. `collation: "NOCASE_NOACCENT"` is on both new columns. The brief said "no collation". `AppDbContext.SetDatabaseCollation` applies it to every string property by convention; accepted, not overridden per column.
2. `Down` uses raw SQL instead of `DropColumn` (decision above).
3. `Domain/Rules/` is a new folder.

### Changed files
- `Domain/Entity/Song.cs`
- `Domain/Entity/Artist.cs`
- `Domain/Rules/ImageUrlRules.cs` (new)
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/EntityEFConfig/ArtistConfiguration.cs`
- `Infra/Migrations/20261113032549_AddImageUrlToSongsAndArtists.cs` (new)
- `Infra/Migrations/20261113032549_AddImageUrlToSongsAndArtists.Designer.cs` (new)
- `Infra/Migrations/AppDbContextModelSnapshot.cs`
- `MyVocaList.Tests/Unit/Domain/ImageUrlRulesTests.cs` (new)
- `MyVocaList.Tests/Integration/Migrations/ImageUrlMigrationTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- Red, unit: `ImageUrlRulesTests` with a stub `Normalize` (returns input): 8 failed, 4 passed (assertion failures).
- Red, migration: `ImageUrlMigrationTests` with properties but no migration: `PendingModelChangesWarning` (column missing).
- Red, rollback: `ImageUrl_RollbackToBeforeMigration_DropsColumnsKeepsTriggersAndRows` against the `DropColumn` Down: `error in trigger SongSearch_artist_au: no such table: main.Songs`.
- Green: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 984 passed, 0 failed, 0 skipped (baseline 967; +12 rules, +5 migration).
- `M3_Down_DropsTablesAndTriggers` passes unedited with the native Down.

### AC traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ART-03 | Only absolute https URLs ≤ 500 chars kept; trimmed; others null | `ImageUrlRules.Normalize`, `MaxLength` | `Normalize_HttpsUrl_IsKept`, `Normalize_HttpsUrlWithSurroundingSpaces_IsTrimmed`, `Normalize_NotAnHttpsUrl_ReturnsNull` (theory), `Normalize_UrlOfExactlyMaxLength_IsKept`, `Normalize_UrlOverMaxLength_ReturnsNull`, `MaxLength_Is500` |
| REQ-ART-05 | Nullable `ImageUrl` on Song and Artist, URL only | entities, configs, migration `Up` | `ImageUrl_ColumnsExistOnSongsAndArtists`, `ImageUrl_ColumnsAreNullableText`, `ImageUrl_SongAndArtistSavedWithoutValue_ReadBackNull`, `ImageUrl_ValueRoundTripsOnSongAndArtist` |
| REQ-ART-05 | Rollback drops columns, keeps triggers and rows | migration `Down` | `ImageUrl_RollbackToBeforeMigration_DropsColumnsKeepsTriggersAndRows` |

### Notes for T2 / T5
- `using MyVocaList.Domain.Rules;` is needed where `ImageUrlRules` is used (no global using added).
- Migration `Down` is a raw statement; any future migration that rebuilds `Songs`/`Artists` on SQLite will hit the same FTS trigger issue.

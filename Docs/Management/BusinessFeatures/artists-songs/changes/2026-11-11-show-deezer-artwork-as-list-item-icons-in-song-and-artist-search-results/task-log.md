# Task log: Deezer artwork in the Song form options and the Songs list

Branch `feature/deezer-artwork` · worktree `.claude/worktrees/deezer-artwork` · started 2026-11-13.

## Authority

Helder, 2026-11-13: keep every activity tracked in the proper docs, be autonomous until done, take
decisions alone unless his participation is crucial, work in a worktree, do not ask. Every gate below
marked "pre-authorised" rests on that instruction. The decisions taken for him are `design.md § 1`
(D1…D17). The device check (T11) is his.

### Checkpoint
- **Phase:** T3 `To Review` (see `## T3`). Worktree `.claude/worktrees/deezer-artwork`, branch `feature/deezer-artwork`.
- **Last build/test:** `dotnet test` → 1018 passed, 0 failed.
- **Next:** commit T3 (no push) → orchestrator review; then T4 (suggestion services).
- **Context manifest:** `plan.md § T4`, `design.md § 3.4`, `Contracts/DTOs/MusicSearchResultDto.cs`, `Services/DeezerProvider.cs` (all under the worktree root).

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

## T2 — Read models and projections (2026-11-13) — To Review

**Plan:** `plan.md § T2`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### What was done
- `SongListItemDto`: trailing optional `Version = null`, `ImageUrl = null`; computed `SupportingText` = non-empty parts of artist name, version, `"feat. " + featured`, joined by `" · "`. Each part is trimmed before the emptiness check (whitespace-only counts as empty).
- `SongSearchHit`: trailing optional `ImageUrl = null`, `ArtistImageUrl = null`.
- `ArtistListItem`: trailing optional `ImageUrl = null`.
- Projections (all `IQueryable` `new` calls pass every argument explicitly, CS0854):
  - `SongRepository.cs`:
    - `TitleSearchSelectSql` (raw SQL, lines 18–20): adds `s.ImageUrl AS ImageUrl, a.ImageUrl AS ArtistImageUrl` (aliases `s`/`a` are the existing `Songs s` / `Artists a` joins).
    - `GetPagedAsync` (lines 69–77): adds `s.Version`, `s.ImageUrl ?? s.OriginalArtist.ImageUrl`.
    - `SearchTitleCoreAsync` EF prefix fallback (lines 196–198): `s.ImageUrl, s.OriginalArtist.ImageUrl`.
    - `FindByExternalIdsAsync` (lines 236–238): same two values.
    - `FindByTitleAndArtistNamesAsync` (lines 258–260): same two values.
  - `CatalogRepository.cs` `GetPagedByArtistAsync` (lines 41–49): same projection change as `GetPagedAsync`.
  - `ArtistRepository.cs`:
    - `GetPagedAsync` (lines 61–66): adds `a.ImageUrl`.
    - `SearchByNameAsync` (line 88): `a.CatalogEntries.Count()` replaces the constant 0; adds `a.ImageUrl`.
- Grep for `new SongListItemDto(|new SongSearchHit(|new ArtistListItem(` found no other projection sites. The remaining sites are plain (non-expression) constructions in tests and keep the optional arguments.

### Deviations
- None against the spec. The `SupportingText` trimming of each part is an implementation detail within REQ-ART-20 ("non-empty parts"); recorded here, no spec change needed.

### Red evidence
- New tests written before the production change. `dotnet test` failed to compile with the expected missing-member errors: CS1061 (`SupportingText`, `Version`, `ImageUrl`, `ArtistImageUrl` not defined on `SongListItemDto`, `SongSearchHit`, `ArtistListItem`) and CS1739 (no parameter `Version` on `SongListItemDto`). These are compile-level reds, not assertion-level; the projection assertions were not separately observed failing.
- A test-side compile error (missing `using Microsoft.EntityFrameworkCore;` for `MigrateAsync` in `ArtistListItemProjectionTests.cs`) was fixed before the Green run.

### Verification
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 1005 passed, 0 failed, 0 skipped (baseline 984; +21 new: 6 `SongListItemDtoTests`, 6 `SongListItemProjectionTests`, 6 `SongSearchHitImageTests`, 3 `ArtistListItemProjectionTests`).
- No existing test edited; no existing test failed.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors, 27 warnings (NU1903 vulnerability warnings, pre-existing).

### Changed files
- `Contracts/DTOs/List/SongListItemDto.cs`
- `Domain/ReadModels/SongSearchHit.cs`
- `Domain/ReadModels/ArtistListItem.cs`
- `Infra/Repository/SongRepository.cs`
- `Infra/Repository/CatalogRepository.cs`
- `Infra/Repository/ArtistRepository.cs`
- `MyVocaList.Tests/Unit/Contracts/SongListItemDtoTests.cs` (new)
- `MyVocaList.Tests/Integration/Repositories/SongListItemProjectionTests.cs` (new)
- `MyVocaList.Tests/Integration/Repositories/SongSearchHitImageTests.cs` (new)
- `MyVocaList.Tests/Integration/Repositories/ArtistListItemProjectionTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### AC traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ART-06 | Title option hits carry song image and artist image (FTS raw SQL, artist-scoped FTS, EF prefix fallback) | `TitleSearchSelectSql`, `SearchTitleCoreAsync` | `SongSearchHitImageTests`: `SearchTitleCandidatesAsync_FtsPath_CarriesBothImageUrls`, `SearchTitleCandidatesForArtistAsync_FtsPath_CarriesBothImageUrls`, `SearchTitleCandidatesAsync_PrefixPath_CarriesImageUrlsAndNullSongImage`, `SearchTitleCandidatesAsync_PrefixPath_SongWithImage_CarriesSongImage` |
| REQ-ART-06 | Promoted-row lookups carry both URLs | `FindByExternalIdsAsync`, `FindByTitleAndArtistNamesAsync` | `SongSearchHitImageTests`: `FindByExternalIdsAsync_CarriesBothImageUrls`, `FindByTitleAndArtistNamesAsync_CarriesBothImageUrls` |
| REQ-ART-10 | Artist search returns real catalog count (3) and image | `ArtistRepository.SearchByNameAsync` | `ArtistListItemProjectionTests`: `SearchByNameAsync_ArtistWithThreeCatalogEntries_ReturnsRealCountAndImage`, `SearchByNameAsync_NoCatalogNoImage_ReturnsZeroAndNull`, `GetPagedAsync_CarriesArtistImage` |
| REQ-ART-19 | Row image = song image, else artist image, else null (plain list and catalog) | `SongRepository.GetPagedAsync`, `CatalogRepository.GetPagedByArtistAsync` | `SongListItemProjectionTests`: `GetPagedAsync_SongWithImage_RowImageIsSongImage`, `GetPagedAsync_SongWithoutImageArtistWithImage_RowImageIsArtistImage`, `GetPagedAsync_NeitherHasImage_RowImageIsNull`, `GetPagedByArtistAsync_SongWithImageAndVersion_CarriesImageAndVersion`, `GetPagedByArtistAsync_SongWithoutImage_FallsBackToOriginalArtistImage` |
| REQ-ART-19 | Version carried into the list item (plain and catalog) | `SongListItemDto.Version`, projections | `SongListItemProjectionTests`: `GetPagedAsync_SongWithVersion_CarriesVersion`, `GetPagedByArtistAsync_SongWithImageAndVersion_CarriesImageAndVersion` |
| REQ-ART-20 | Supporting text: artist only / artist+version / artist+featured / all three / empty artist / whitespace parts | `SongListItemDto.SupportingText` | `SongListItemDtoTests`: `SupportingText_ArtistOnly_IsArtistName`, `SupportingText_ArtistAndVersion_IsArtistDotVersion`, `SupportingText_ArtistAndFeatured_IsArtistDotFeat`, `SupportingText_AllThree_IsInOrder`, `SupportingText_EmptyArtistName_OmitsArtistPart`, `SupportingText_WhitespaceParts_AreOmitted` |

### Notes for T4 / T8 / T9
- T4: `SongSearchHit.ImageUrl` / `ArtistImageUrl` are filled by every lookup, so `SongSuggestionService.MapLocal` can copy them directly; promoted rows carry the stored images.
- T4: `ArtistListItem.ImageUrl` is filled by `SearchByNameAsync`, so the Artist suggestion local mapping can copy it (REQ-ART-24 says local stays null until the Artist form item; the decision is for T4 to apply).
- T8/T9: `SongListItemDto.SupportingText` already implements REQ-ART-20; the Songs list template can bind it directly. `ImageUrl` is the row image (song else artist), so no fallback logic is needed in XAML.
- `ArtistListItem.CatalogCountText` now reads real counts in search results.

## T3 — Deezer provider maps images and album (2026-11-13) — To Review

**Plan:** `plan.md § T3`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### Changed files
- `Contracts/DTOs/MusicSearchResultDto.cs` (trailing optional `ImageUrl`, `ArtistImageUrl`, `AlbumTitle`)
- `Services/DeezerProvider.cs` (private records gain `album`, `artist.picture_medium`, artist `picture_medium`; `ImageUrlRules.Normalize` on each URL; `NormaliseAlbumTitle` trims and nulls blanks)
- `MyVocaList.Tests/Unit/Services/DeezerProviderImageTests.cs` (new, 13 tests, own JSON builder and stub handler)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

`DeezerProviderTests.cs` is not edited. No new HTTP request is made.

### Verification
- **Red 1 (compile):** new test file against the unchanged DTO: `CS1061 'MusicSearchResultDto' does not contain a definition for 'ImageUrl'`.
- **Red 2 (assertions):** after the DTO change and before the mapping: 6 failed, 7 passed. The 6 failures were the value assertions (cover, artist picture, album title, http cover, empty cover). The 7 passing tests assert null outcomes or request counts, which the unmapped code already satisfies. They are regression guards, not new behaviour.
- One Red-phase failure came from my own test input (a track with no artist object gave an empty `ArtistName`). I corrected the test input before the mapping; the assertion was not changed.
- **Green:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 1018 passed, 0 failed, 0 skipped (1005 baseline + 13 new). Build 0 errors.
- Android build not run: no MAUI head project touched.

### AC traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ART-01 | Track carries cover, artist picture and album title | `DeezerProvider.SearchSongsAsync` | `DeezerProviderImageTests`: `SearchSongsAsync_TrackWithAlbumAndArtistPicture_MapsCoverArtistPictureAndAlbumTitle`, `SearchSongsAsync_AlbumTitleSurroundedBySpaces_IsTrimmed` |
| REQ-ART-01 | Missing album, album title, or null fields give null, search succeeds | nullable records, `?.` | `SearchSongsAsync_NoAlbumTitle_AlbumTitleIsNullAndSearchSucceeds` (3 cases), `SearchSongsAsync_NoAlbumObjectNoArtistPicture_ImagesAreNullAndSearchSucceeds`, `SearchSongsAsync_NullAlbumAndNullArtistPicture_ImagesAreNullAndSearchSucceeds` |
| REQ-ART-02 | Artist result carries `picture_medium` | `DeezerProvider.SearchArtistsAsync` | `SearchArtistsAsync_MapsPictureMediumAsImageUrl`, `SearchArtistsAsync_NoPictureMedium_ImageUrlIsNull` |
| REQ-ART-03 | Non-https or empty cover is no image; blank album title is null | `ImageUrlRules.Normalize`, `NormaliseAlbumTitle` | `SearchSongsAsync_EmptyCoverAndBlankAlbumTitle_ImageNullAndAlbumTitleNull`, `SearchSongsAsync_HttpCover_IsDroppedAsNoImage` |
| REQ-ART-04 | One request per search | unchanged request path | `SearchSongsAsync_OneSearch_MakesExactlyOneRequest`, `SearchArtistsAsync_OneSearch_MakesExactlyOneRequest` |

Every test's first line carries its `// [AC]` line. Coverage stays at the provider boundary.

### Private JSON records (verbatim)
```csharp
private record DeezerArtist(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("picture_medium")] string? PictureMedium);
private record DeezerTrack(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("title_short")] string? TitleShort,
    [property: JsonPropertyName("title_version")] string? TitleVersion,
    [property: JsonPropertyName("artist")] DeezerTrackArtist? Artist,
    [property: JsonPropertyName("album")] DeezerTrackAlbum? Album);
private record DeezerTrackArtist(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("picture_medium")] string? PictureMedium);
private record DeezerTrackAlbum(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("cover_medium")] string? CoverMedium);
```

### Design concern
None. Implementation decision (not in design.md): a whitespace-only album title maps to null (`design.md § 3.4` says "null when empty"). This is the trimmed-empty reading; no spec change made.

### Build notes
Build: passed (0 errors) | Tests: 1018 passed, 0 failed | Commit SHA: (see commit)
Files written and re-read: `Contracts/DTOs/MusicSearchResultDto.cs`, `Services/DeezerProvider.cs` (both sections), `MyVocaList.Tests/Unit/Services/DeezerProviderImageTests.cs` (test fixed after first Red; re-read the artist-field change)

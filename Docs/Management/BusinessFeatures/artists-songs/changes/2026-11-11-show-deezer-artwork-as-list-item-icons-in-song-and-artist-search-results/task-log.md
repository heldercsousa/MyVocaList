# Task log: Deezer artwork in the Song form options and the Songs list

Branch `feature/deezer-artwork` · worktree `.claude/worktrees/deezer-artwork` · started 2026-11-13.

## Authority

Helder, 2026-11-13: keep every activity tracked in the proper docs, be autonomous until done, take
decisions alone unless his participation is crucial, work in a worktree, do not ask. Every gate below
marked "pre-authorised" rests on that instruction. The decisions taken for him are `design.md § 1`
(D1…D17). The device check (T11) is his.

### Checkpoint
- **Phase:** T9 `To Review` (see `## T9`). Worktree `.claude/worktrees/deezer-artwork`, branch `feature/deezer-artwork`.
- **Last build/test:** android build 0 errors; `dotnet test` -> 1071 passed, 0 failed.
- **Next:** commit T9 (no push); orchestrator review of T9.
- **Context manifest:** `plan.md § T9`, `design.md § 3.6`, `MyVocaList/UI/Pages/Songs/SongsPage.xaml`, `Contracts/DTOs/List/SongListItemDto.cs`, `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml`.

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

## T4 — Suggestion DTOs and services carry images and album (2026-11-13) — To Review

**Plan:** `plan.md § T4`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### What was done
- `SongSuggestionDto`: trailing optional `ImageUrl = null`, `ArtistImageUrl = null`, `AlbumTitle = null`; computed `RowImageUrl => ImageUrl ?? ArtistImageUrl`. Moved to a record with a body (positional parameters unchanged).
- `ArtistSuggestionDto`: trailing optional `ImageUrl = null`.
- `SongSuggestionService.MapLocal` copies `hit.ImageUrl`, `hit.ArtistImageUrl` and passes `null` for album. Promoted rows go through `MapLocal`, so they carry the stored images (T2 fills the lookups). Remote mapping in `ResolveLocalArtistsAsync` copies `r.ImageUrl`, `r.ArtistImageUrl`, `r.AlbumTitle`.
- `ArtistSuggestionService.SearchNameRemoteAsync` remote mapping copies `r.ImageUrl`. The local mapping is unchanged, so `ImageUrl` stays null (REQ-ART-24).
- No change to dedup, promotion, ordering, limits, thresholds or status logic.

### Deviations
- None against the spec. The new test files need `using MyVocaList.Domain.Resolution;` for `SimilarityConstants` (the existing test file gets it another way); a build fix, not a design change.

### Red evidence
- New tests written first: `Unit/Services/SongSuggestionServiceImageTests.cs` (6 tests) and `Unit/Services/ArtistSuggestionServiceImageTests.cs` (3 tests).
- Red run: `dotnet test` failed to compile with CS1061 on `SongSuggestionDto` for `ImageUrl`, `ArtistImageUrl`, `AlbumTitle` and `RowImageUrl`. The captured output was cut at 40 lines, so the matching `ArtistSuggestionDto.ImageUrl` errors are not in the record. These are compile-level reds, the same kind as T2.
- Behaviour assertions were observed only after the DTO change (Green run below).

### Verification
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 1027 passed, 0 failed, 0 skipped (baseline 1018, +9 new). Build 0 errors.
- Existing tests: none edited, none failing. One intermediate run (against a stale binary, the test project did not compile at that point) showed a single failure in `SongFormViewModelTests.TitleOptionSource_SetsNoteThroughUiThreadHelper` (expected 1, actual 2). The test passed when run alone and in the next full run (1027 passed). The ViewModel is not touched by T4. Possibly flaky under parallel UI-thread helper timing; not investigated further.
- Android build not run: no MAUI head project touched.

### AC traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ART-06 | Local title option carries the hit's song image and artist image; no album | `MapLocal` | `SearchTitleLocalAsync_HitWithBothImages_OptionCarriesSongAndArtistImage` |
| REQ-ART-06 | Row image falls back to the artist image when the song has none; null when neither | `SongSuggestionDto.RowImageUrl` | `SearchTitleLocalAsync_NoSongImage_RowImageFallsBackToArtistImage`, `SearchTitleLocalAsync_NoImages_RowImageIsNull` |
| REQ-ART-06 | Remote title option carries cover, artist picture and album | `ResolveLocalArtistsAsync` mapping | `SearchTitleRemoteAsync_RemoteTitleOption_CarriesCoverArtistImageAndAlbum` |
| REQ-ART-06 | Remote option without a cover falls back to the artist picture | `RowImageUrl` | `SearchTitleRemoteAsync_RemoteTitleOptionNoCover_RowImageFallsBackToArtistPicture` |
| REQ-ART-06 | Promoted row carries the stored images, not Deezer's, and no album | `MapLocal` via `FindByExternalIdsAsync` | `SearchTitleRemoteAsync_PromotedRow_CarriesStoredImagesNotDeezerImages` |
| REQ-ART-24 | Remote artist suggestion carries the picture | `ArtistSuggestionService` remote mapping | `SearchNameRemoteAsync_RemoteArtistSuggestion_CarriesArtistImage` |
| REQ-ART-24 | Remote artist without picture carries null | `ArtistSuggestionDto.ImageUrl` default | `SearchNameRemoteAsync_RemoteArtistWithoutPicture_ImageUrlIsNull` |
| REQ-ART-24 | Local artist suggestion keeps ImageUrl null | unchanged local mapping | `SearchNameLocalAsync_StoredArtistWithImage_LocalSuggestionImageIsNull` |

### Final record declarations (verbatim)
```csharp
public sealed record SongSuggestionDto(
    int? LocalId,
    string Title,
    string ArtistName,
    int? LocalArtistId,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    string Version = "",
    string? LocalArtistName = null,
    string? ImageUrl = null,
    string? ArtistImageUrl = null,
    string? AlbumTitle = null)
{
    /// <summary>The image a row shows: the song image, else the artist image, else null (REQ-ART-06).</summary>
    public string? RowImageUrl => ImageUrl ?? ArtistImageUrl;
}

public sealed record ArtistSuggestionDto(
    int? LocalId,
    string Name,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    string? ImageUrl = null);
```

### Changed files
- `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`
- `Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs`
- `Services/SongSuggestionService.cs`
- `Services/ArtistSuggestionService.cs`
- `MyVocaList.Tests/Unit/Services/SongSuggestionServiceImageTests.cs` (new)
- `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceImageTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Notes for T8
- `SongSuggestionDto.RowImageUrl` is the row image for the Title option (REQ-ART-06). `AlbumTitle` is non-null only on remote rows.
- Local artist options (`ArtistSuggestionDto.ImageUrl`) stay null until the Artist form item, per REQ-ART-24. The ViewModel must set `Avatar` placeholder for them anyway.

## T5 — Save stores the song image and fills an empty artist image (2026-11-13) — To Review

Level A (full TDD). `SongFormSaveRequest` gains trailing optional `ImageUrl` / `ArtistImageUrl`. `SaveFromFormAsync`
normalises both with `ImageUrlRules.Normalize` first. `CreateSongWithUrlsCoreAsync` (private) gains optional
`songImageUrl`, `artistImageUrl`, `fillArtistImage`; `UpdateSongCoreAsync` and the public `ISongService` / `CreateSongWithUrlsAsync`
signatures are unchanged. Everything runs inside the existing unit of work.

Implementation note: an existing artist is filled only after every refusal in the create path has passed (artist
found, title/version duplicate check), so a refused Save does not even dirty the tracked entity (REQ-ART-18).
A created artist gets the picture before its first flush.

### Changed files
- `Domain/Resolution/SongFormSaveRequest.cs`
- `Services/SongService.cs`
- `MyVocaList.Tests/Unit/Services/SongServiceSaveFromFormImageTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- Red (request record compiling, service untouched): 7 of the 14 new tests failed on assertions with `Expected: <url> Actual: null`
  (add with image, both save-as-new cases, created artist plain + `CreateTyped`, existing artist by id, by exact name, invalid-URL test excluded since null is the expected value).
  The remaining tests assert "unchanged / null" and pass by construction before the change; they guard the rules once it exists.
- Intermediate Green: `FailedWrite_ChangesNoImage` failed because the fill ran before the duplicate refusal; the fill was moved after the refusals.
- Final: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 1041 passed, 0 failed (baseline 1027 + 14).

### AC table
| AC | Test methods |
|----|--------------|
| REQ-ART-15 | `SaveFromForm_AddWithPendingImage_StoresImageOnSong`, `SaveFromForm_AddWithoutPendingImage_StoresNull`, `SaveFromForm_AddWithExplicitSaveAsNew_NullsIdentityButKeepsImage` (2 cases) |
| REQ-ART-16 | `..._ArtistCreatedByPlainPath_GetsArtistImage`, `..._ArtistCreatedByCreateTypedDecision_GetsArtistImage`, `..._ExistingArtistByIdWithoutImage_IsFilled`, `..._ExistingArtistByExactNameWithoutImage_IsFilled`, `..._ExistingArtistWithImage_IsNeverChanged`, `..._ArtistChosenByUseExistingDecision_IsNeverChanged` |
| REQ-ART-17 | `SaveFromForm_EditModeWithImages_LeavesStoredImagesUnchanged` |
| REQ-ART-18 | `SaveFromForm_NeedsDecision_ChangesNoImage`, `SaveFromForm_FailedWrite_ChangesNoImage` |
| REQ-ART-03 | `SaveFromForm_InvalidUrls_AreStoredAsNull` |

### Artist resolution branch -> `FillArtistImage`
| Branch in `ResolveFormArtistAsync` | `FillArtistImage` |
|---|---|
| `request.ArtistId` picked | true |
| `ArtistDecision.UseExisting` | **false** |
| `ArtistDecision.CreateTyped` (new artist created) | true |
| Exact collated match (`ExactLocalMatch` / `ExactExternalMatch`) | true |
| `NoMatch` (new artist created) | true |
| `Stop` (invalid name, fuzzy choice needed) | false (no write happens) |


## T6 — ListItemLeadingImage placeholder and ListItemLeadingAvatar (2026-11-13) — To Review

**Plan:** `plan.md § T6`
**Component record (four gates):** `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/README.md`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### What was done
- `ListItemLeadingImage`: new BindableProperty `PlaceholderIcon` (`string`, default null). Content is now a `Grid` with a 24x24 `dx:DXImage` (`TintColor` = `OnSurfaceVariant`, `IsVisible` via `IsNotNullConverter`) UNDER the existing `Image`. A `DataTrigger` on the `Border` sets `BackgroundColor` = `SurfaceContainerHighest` only while `PlaceholderIcon` is not null. `ImageSource`, `Aspect`, 56x56 and `RoundRectangle 4` unchanged. No code-behind state machine.
- `ListItemLeadingAvatar` (new): 40x40 `Border` with `StrokeShape="Ellipse"` (clips content), `SurfaceContainerHighest` background, 24x24 tinted `dx:DXImage` under an `AspectFill` `Image`. BindableProperties `ImageSource` and `PlaceholderIcon` (default `account_circle_outlined`).
- Resource keys verified by Grep: `SurfaceContainerHighest` and `OnSurfaceVariant` exist in `Resources/Styles/MaterialColors.xaml`; `IsNotNullConverter` is registered in `App.xaml`; `Resources/Images/account_circle_outlined.svg` exists.
- No consumer page edited; `ListItem.xaml` untouched.

### Container decision: MAUI `Border`, not `dx:DXBorder`
Context7 `/websites/devexpress_maui` (docs.devexpress.com/MAUI/DevExpress.Maui.Core.DXViewBase.IsClippedToBounds and `.CornerRadius`): `CornerRadius` is documented only as "the corner radius", and clipping of children is a separate opt-in property, `IsClippedToBounds` (default not stated). The docs do not confirm that `DXBorder` clips content to rounded corners, so the record's fallback applies: MAUI `Border` with a round `StrokeShape`, same precedent as `ListItemLeadingImage`.

### Caching note (REQ-ART-23)
Microsoft Learn, "Image" -> "Load a remote image" / "Image caching" (learn.microsoft.com/dotnet/maui/user-interface/controls/image#load-a-remote-image): a string URI assigned to an `ImageSource` converts implicitly to a `UriImageSource`; "Caching of downloaded images is enabled by default, with cached images being stored for 1 day"; `CachingEnabled` default `true`, `CacheValidity` default 1 day.

### Consumer map and per-consumer risk (copied from the component record)
| Consumer | Usage |
|----------|-------|
| `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml:60` | `ImageSource="{Binding ThumbnailUrl}" Aspect="AspectFill"`; does not set `PlaceholderIcon` |

`ListItemLeadingAvatar` has no consumer yet. Successors: `AutocompleteDXField` option row (image and avatar) and `SongsPage` (image).

| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `YouTubeSearchPage` | Thumbnail size, crop or background changes if the default path is not identical | Device (T11): YouTube search results show thumbnails as before (56 dp, rounded 4, no grey background, no icon) |

With `PlaceholderIcon` null: Border background trigger inactive (transparent), DXImage `IsVisible` false and its `Source` null, `Image` identical to before. The only structural difference is the extra `Grid` wrapper (single full-size child); it needs the T11 device check.

### Level-C decision
Level C (pure XAML, no logic): no unit test. The ACs (REQ-ART-22/23) are verified on device in T11.

### Changed files
- `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml`
- `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml.cs`
- `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml` (new)
- `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: after `ListItemLeadingImage` -> 0 errors (27+ NU1903 warnings, pre-existing); after `ListItemLeadingAvatar` -> 0 errors.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` -> 1041 passed, 0 failed.
- Emulator/device run not done (T11 is the device check); status stays To Review.

## T7 — AutocompleteDXField option row: leading image or avatar and trailing text (2026-11-13) — To Review

Component record (four gates, approval pre-authorised): `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/README.md`.

### What was done
- `AutocompleteSuggestion`: optional `init` properties `LeadingKind` (default `None`), `ImageUrl`, `PlaceholderIcon`, `TrailingText`; computed `HasLeadingImage`, `HasLeadingAvatar`. New enum `AutocompleteLeadingKind { None, Image, Avatar }` in its own file. Constructor and `IsCreateNew` unchanged.
- `AutocompleteDXField.xaml`: only the option row `DataTemplate` rewritten as `Grid Auto,*,Auto` (`Padding="16,8"`): `ListItemLeadingImage` / `ListItemLeadingAvatar` in column 0 (12 dp right margin, centred), the two labels in column 1 (`MaxLines=1`, `TailTruncation`), `Label.Small` trailing text in column 2 (12 dp left margin, hidden when null). Added `lists` xmlns. `Label.Small` exists (`MaterialStyles.xaml:163`).
- The string -> `ImageSource` compiled binding compiled without error (same as `YouTubeSearchPage`). No code-behind or logic touched.
- Not verified here: DX `AutoCompleteEdit` rows growing to template content (72 dp for image rows); device check (T11).

### Consumer map and per-consumer risk (from the component record)
| Consumer | Options built by |
|----------|------------------|
| `SongFormPage.xaml:32` (`titleField`) | `SongFormViewModel.ToTitleOption` |
| `SongFormPage.xaml:66` (`artistField`) | `SongFormViewModel.cs:342` |
| `PersonFormPage.xaml:20` (`nameField`) | `PersonFormViewModel.cs:301` |

| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `PersonFormPage` `nameField` | Row height/text position; long text now ellipsised instead of wrapped | Device: Person menu unchanged for normal names; unit tests green |
| `SongFormPage` `titleField` | Taller rows (72 dp); pick, lock, X must work | Device (T11) |
| `SongFormPage` `artistField` | Row gains avatar (56 dp row) | Device (T11) |
| `ArtistFormViewModel` options | None (not bound to a page) | Build |

### Changed files
- `Contracts/Models/AutocompleteSuggestion.cs`
- `Contracts/Models/AutocompleteLeadingKind.cs` (new)
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- `MyVocaList.Tests/Unit/Contracts/AutocompleteSuggestionTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- Red: test file added first; build failed with CS0103/CS1061 for the missing `AutocompleteLeadingKind`, `LeadingKind`, `ImageUrl`, `PlaceholderIcon`, `TrailingText`.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`: 1045 passed, 0 failed (1041 baseline + 4 new).

### AC table
| AC | Implementation | Test |
|----|----------------|------|
| REQ-ART-06 | `LeadingKind.Image`, `HasLeadingImage`, row template col 0 | `LeadingKindImage_SetsOnlyHasLeadingImage` |
| REQ-ART-08 | `LeadingKind.Avatar`, `HasLeadingAvatar` | `LeadingKindAvatar_SetsOnlyHasLeadingAvatar` |
| REQ-ART-09 | defaults `None`/null, constructor unchanged | `Defaults_...`, `ThreeArgumentConstructor_...` |

## T8 — Song form options show images; picks carry pending images (2026-11-13) — To Review

### What was done
`SongFormViewModel` only (design.md § 3.5):
- `ToTitleOption`: `LeadingKind = Image`, `ImageUrl = RowImageUrl`, placeholder `music_note_outlined`, trailing text `Library` / `Deezer`; supporting text = non-blank artist, version, album joined by ` · `; album only for remote rows, non-blank and not equal to the title (`OrdinalIgnoreCase`).
- New `ToArtistOption`: `LeadingKind = Avatar`, `ImageUrl = ArtistListItem.ImageUrl`, placeholder `account_circle_outlined`, supporting text `CatalogCountText`, no trailing text.
- Private fields `_pendingImageUrl` / `_pendingArtistImageUrl`: set in `ApplyRemotePick` from `dto.ImageUrl` (cover) and `dto.ArtistImageUrl`; `ClearTitle` clears the song one; `ClearArtist` clears both; `BuildSaveRequest` sends both by name in add mode, null in edit mode. Not cleared by Save/sheets.
- Icon names and source words are `private const` strings.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelImageTests.cs` (new, 26 tests)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (four planned edits)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- Red (new file, before the production change): 21 failed, 5 passed of 26. Failures were assertions, e.g. Expected "Ramones · 1999 Remaster · Greatest Hits" / Actual "Ramones · 1999 Remaster · Deezer"; Expected Avatar / Actual None; Expected "Deezer" / Actual null; Expected cover URL / Actual null. The 5 that passed assert behaviour the old code already met (null images in the no-pick, X-on-Artist, local-pick, edit-mode and second-pick-with-nulls cases) or are guards. One test (`SelectTitle_RemoteOption_Leaves...`) first failed for a wrong reason (`VerifyNoOtherCalls` tripped on the incidental `ShouldShowCharacterCounter`); fixed in the test by verifying the write methods Never, as the existing REQ-SFT-12 test does.
- After the production change: the only failing tests in the suite were exactly the four planned ones (1067 passed, 4 failed), each on its supporting-text assertion only.
- Final: `dotnet test` 1071 passed, 0 failed (1045 baseline + 26 new); `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors. `ArtistFormViewModelTests` untouched and green.
- E2E emulator: not run (device check is T11).

### The four edited existing tests (`SongFormViewModelTests.cs`; names unchanged)
| Test | Before (expected) | After (expected) | Failure shown before the edit |
|------|-------------------|------------------|-------------------------------|
| `TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker` | `Radiohead · Pablo Honey · In your library` | `Radiohead · Pablo Honey` + `TrailingText == "Library"` | Expected "Radiohead · Pablo Honey · In your library" / Actual "Radiohead · Pablo Honey" |
| `TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart` | `Radiohead · In your library` | `Radiohead` + `TrailingText == "Library"` | Expected "Radiohead · In your library" / Actual "Radiohead" |
| `TitleOptionSource_RemoteHit_SupportingTextIsArtistVersionAndDeezer` | `Radiohead · Live · Deezer` | `Radiohead · Live` + `TrailingText == "Deezer"` | Expected "Radiohead · Live · Deezer" / Actual "Radiohead · Live" |
| `TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart` | `Radiohead · Deezer` | `Radiohead` + `TrailingText == "Deezer"` | Expected "Radiohead · Deezer" / Actual "Radiohead" |

The `[AC]` lines were renamed from REQ-SFT-04 to REQ-ART-07/08.

### AC table
| AC | Implementation | Test methods (`SongFormViewModelImageTests` unless noted) |
|----|----------------|------------------------------------------------------------|
| REQ-ART-06 | `ToTitleOption` | `TitleOption_LocalRowWithSongImage_...`, `TitleOption_RemoteRowWithoutCover_FallsBackToArtistImage`, `TitleOption_RowWithNoImages_...` |
| REQ-ART-07 | `ToTitleOption` | `TitleOption_RemoteRow_SupportingTextIsArtistVersionAlbum`, `..._LocalRowEmptyVersion_...`, `..._AlbumEqualToTitleIgnoringCase_OmitsAlbum` (x2), `..._AlbumDiffersByAccent_KeepsAlbum`, `..._LocalRowCarryingAlbum_IgnoresAlbum`, `..._RemoteRowBlankAlbum_OmitsAlbum`; the four edited tests |
| REQ-ART-08 | `ToTitleOption` | `TitleOption_LocalRow_TrailingText...`, `TitleOption_RemoteRow_TrailingText...`; the four edited tests |
| REQ-ART-09 | `ToArtistOption` | `ArtistOption_WithImage_...`, `ArtistOption_WithoutImage_...` |
| REQ-ART-10 | `ToArtistOption` | `ArtistOption_WithImage_...` |
| REQ-ART-11 | `ApplyRemotePick`, `BuildSaveRequest` | `Save_AfterDeezerPick_RequestCarriesCoverNotFallbackAndArtistImage`, `Save_AfterDeezerPickWithoutCover_...`, `Save_AfterTwoDeezerPicks_SecondReplacesBothImages` |
| REQ-ART-12 | `ApplyRemotePick` (touches no other field) | `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing` |
| REQ-ART-13 | `ClearTitle`, `ClearArtist` | `Save_AfterDeezerPickThenXOnTitle_...`, `Save_AfterDeezerPickThenXOnArtist_...` |
| REQ-ART-14 | `SelectArtist` unchanged | `Save_DeezerPickThenXOnArtistThenLocalArtistPick_SendsNoImages`, `Save_LocalArtistPickWithNoDeezerPick_...`, `Save_LocalArtistPickAfterDeezerPick_KeepsPendingImages` |
| REQ-ART-17 | `BuildSaveRequest` | `Save_EditModeAfterDeezerPick_SendsNullImages` |
| REQ-ART-18 | pending fields untouched by Save/sheets | `Save_AfterNeedsDecisionOutcome_NextRequestStillCarriesBothImages`, `Save_AfterFailedOutcome_NextSaveStillCarriesBothImages` |

### Spec gaps / deviations
None. REQ-ART-14 "a local Title pick starts a form with no pending image" holds by construction (a local pick navigates to a fresh edit form); no extra test.

## T9 — Songs list rows show the image and name the artist (2026-11-13) — To Review

**Plan:** `plan.md § T9`
**Status:** To Review
**Started:** 2026-11-13
**Completed:** 2026-11-13

### What was done
`SongsPage.xaml` only, both `DataTemplate`s (`ItemTemplate` and `SelectedItemTemplate`):
- Leading content: `ListItemLeadingIcon Icon="music_note_outlined"` replaced by
  `ListItemLeadingImage ImageSource="{Binding ImageUrl}" PlaceholderIcon="music_note_outlined" Margin="0,8"`
  (row image = song image, else artist image, else null, from `SongListItemDto.ImageUrl`).
- `SupportingText` changed from `{Binding FeaturedArtists}` to `{Binding SupportingText}` (artist · version · feat. featured, REQ-ART-20).
- Trailing content, `IsSelected` / `IsChecked`, commands, `CrudListView` attributes and `SongsViewModel` untouched.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/task-log.md`

### Verification
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors (warnings only, pre-existing NU1902/NU1903).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 1071 passed, 0 failed, 0 skipped (baseline 1071).
- Re-read both templates after the edits: both show the new bindings, nothing else changed.

### Level-C no-test decision
- REQ-ART-19 (row image and supporting text on screen): pure XAML binding, no logic; the data is covered by T2 projection tests. On-screen check is T11.
- REQ-ART-21 (72 dp row via `Margin="0,8"` on the 56 dp leading image): no unit test; verified on device in T11.
- REQ-ART-22 (grey placeholder container and icon when no image): no unit test; verified on device in T11.
- No emulator run here (device check is T11). Status stays `To Review`.

### Design concern
None.


## T10 — Close-out (2026-11-13)

### Commits (branch `feature/deezer-artwork`)

| Task | Commit | Tests after (passed / failed / skipped) |
|------|--------|------------------------------------------|
| T0 spec | `e2b1a1e6` | 967 / 0 / 0 (baseline) |
| T1 | `b7dd4026` | 984 / 0 / 0 |
| T2 | `35be849f` | 1005 / 0 / 0 |
| T3 | `714ca54f` | 1018 / 0 / 0 |
| T4 | `f048005b` | 1027 / 0 / 0 |
| T5 | `7278b5ae` | 1041 / 0 / 0 |
| T6 (component, own item) | `85aa8ff2` | 1041 / 0 / 0, Android build 0 errors |
| T7 (component, own item) | `24413deb` | 1045 / 0 / 0, Android build 0 errors |
| T8 | `7fe23c46` | 1071 / 0 / 0, Android build 0 errors |
| T9 | `bdd6eb67` | 1071 / 0 / 0, Android build 0 errors |

### Independent verification (fresh `verifier`, 2026-11-13)

Verdict: **PASS WITH ISSUES**, no blocker. Its own runs: `dotnet test` 1071 passed, 0 failed, 0 skipped;
`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors (27 existing NuGet audit warnings).
Confirmed: every REQ-ART has an implementation; every criterion that needs a test has one starting with
its `[AC]` line; the only edited existing tests are the four planned ones; the component commits match
their records and are separate commits with their own `Item:` trailer; `ListItem.xaml`,
`AutocompleteDXField.xaml.cs` and `AutocompleteDXFieldLogic.cs` are untouched.

Issues it raised and what was done:

| # | Issue | Outcome |
|---|-------|---------|
| D1 | Task entries lack the commit SHA | Fixed: commit table above |
| D2 | The new columns carry the `NOCASE_NOACCENT` collation (global convention), while `design.md § 3.2` said "no collation" | Fixed: note added to `design.md § 3.2`; behaviour accepted (nothing searches the columns) |
| D3 | Close-out docs not committed at verification time | Fixed by the close-out commit |
| D4 | No real-SQLite test that the fill of an existing artist's image is persisted by the real unit of work | Fixed: see `## T10a` below |
| D5 | `SongFormViewModelTests.TitleOptionSource_SetsNoteThroughUiThreadHelper` failed once during T4 on a stale binary and has passed in every full run since | Not reproduced; noted here. If it recurs, check the generation guard of the UI-thread note |
| D6 | Cosmetic: the doc comment of `AutocompleteSuggestion.TrailingText` says "e.g. album title" (it carries the source word); in `ImageUrlRulesTests` the `[AC]` comment sits above the `[Theory]` attribute | Left as is (comment-only); to tidy the next time those files are touched |

`dotnet ef migrations has-pending-changes` could not run (installed tool 9.0.6 against EF Core 10). The
migration is covered by `ImageUrlMigrationTests` (5 tests, real SQLite), including the rollback.

### Decisions taken during implementation (orchestrator, under the 2026-11-13 instruction)

- **Migration rollback (T1).** EF's `DropColumn` rebuilds the table on SQLite, which fails while the
  search triggers of `AddSearchIndexes` reference `Songs` / `Artists` (the existing test
  `M3_Down_DropsTablesAndTriggers` caught it). `Down` uses the native `ALTER TABLE … DROP COLUMN`
  instead. No existing test was edited. Any future migration that makes EF rebuild `Songs` or
  `Artists` will meet the same problem.
- **Avatar container (T6).** MAUI `Border` with an ellipse shape, not `dx:DXBorder`: the DevExpress docs
  do not state that `DXBorder` clips its content to rounded corners (the record allowed this fallback).
- **Four superseded tests (T8)** updated to REQ-ART-07/08 as planned in `design.md § 6`.

### Traceability matrix

| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-ART-01 | Track search carries cover, artist picture, album | `Services/DeezerProvider.cs` (track mapping, `NormaliseAlbumTitle`) | `DeezerProviderImageTests.SearchSongsAsync_TrackWithAlbumAndArtistPicture_MapsCoverArtistPictureAndAlbumTitle`, `…_NoAlbumObjectNoArtistPicture_…`, `…_NullAlbumAndNullArtistPicture_…`, `…_NoAlbumTitle_…`, `…_AlbumTitleSurroundedBySpaces_IsTrimmed` |
| REQ-ART-02 | Artist search carries the picture | `DeezerProvider.cs` (artist mapping) | `SearchArtistsAsync_MapsPictureMediumAsImageUrl`, `SearchArtistsAsync_NoPictureMedium_ImageUrlIsNull` |
| REQ-ART-03 | Only image URLs are kept | `Domain/Rules/ImageUrlRules.cs`; used by `DeezerProvider`, `SongService.SaveFromFormAsync` | `ImageUrlRulesTests` (12); `DeezerProviderImageTests.SearchSongsAsync_HttpCover_IsDroppedAsNoImage`, `…_EmptyCoverAndBlankAlbumTitle_…`; `SongServiceSaveFromFormImageTests.SaveFromForm_InvalidUrls_AreStoredAsNull` |
| REQ-ART-04 | No extra request | one request per search in `DeezerProvider` | `SearchSongsAsync_OneSearch_MakesExactlyOneRequest`, `SearchArtistsAsync_OneSearch_MakesExactlyOneRequest` |
| REQ-ART-05 | `ImageUrl` on `Song` and `Artist`, URL only | `Domain/Entity/Song.cs`, `Artist.cs`; EF configurations; migration `20261113032549_AddImageUrlToSongsAndArtists` | `ImageUrlMigrationTests` (5) |
| REQ-ART-06 | Title option leading image: song, else artist, else placeholder | `SongFormViewModel.ToTitleOption`; `SongSuggestionDto.RowImageUrl`; `SongRepository` (all `SongSearchHit` builders); `SongSuggestionService`; `AutocompleteDXField.xaml` option row | `SongFormViewModelImageTests.TitleOption_LocalRowWithSongImage_…`, `…_RemoteRowWithoutCover_FallsBackToArtistImage`, `…_RowWithNoImages_…`; `SongSuggestionServiceImageTests` (6, incl. promoted row); `SongSearchHitImageTests` (6); `AutocompleteSuggestionTests.LeadingKindImage_SetsOnlyHasLeadingImage` |
| REQ-ART-07 | Supporting text `artist · version · album` | `SongFormViewModel.ToTitleOption` | `TitleOption_RemoteRow_SupportingTextIsArtistVersionAlbum`, `…_LocalRowEmptyVersion_…`, `…_AlbumEqualToTitleIgnoringCase_OmitsAlbum`, `…_AlbumDiffersByAccent_KeepsAlbum`, `…_LocalRowCarryingAlbum_IgnoresAlbum`, `…_RemoteRowBlankAlbum_OmitsAlbum`; the four edited `SongFormViewModelTests` |
| REQ-ART-08 | Source is the trailing text | `SongFormViewModel.ToTitleOption`; option row trailing label | `TitleOption_LocalRow_TrailingTextIsLibrary…`, `TitleOption_RemoteRow_TrailingTextIsDeezer…`; the four edited tests |
| REQ-ART-09 | Artist option leading avatar | `SongFormViewModel` artist option; `ListItemLeadingAvatar.xaml`; option row | `ArtistOption_WithImage_IsAvatarWithPlaceholderAndCatalogCountText`, `ArtistOption_WithoutImage_HasNullImageAndStillThePlaceholder`; `AutocompleteSuggestionTests.LeadingKindAvatar_SetsOnlyHasLeadingAvatar` |
| REQ-ART-10 | Real catalog count on Artist options | `Infra/Repository/ArtistRepository.cs` (`SearchByNameAsync`, `GetPagedAsync`) | `ArtistListItemProjectionTests.SearchByNameAsync_ArtistWithThreeCatalogEntries_ReturnsRealCountAndImage`, `…_NoCatalogNoImage_ReturnsZeroAndNull`, `GetPagedAsync_CarriesArtistImage` |
| REQ-ART-11 | Deezer pick sets the pending images | `SongFormViewModel.ApplyRemotePick`, `BuildSaveRequest` | `Save_AfterDeezerPick_RequestCarriesCoverNotFallbackAndArtistImage`, `Save_AfterDeezerPickWithoutCover_SongImageStaysNullEvenThoughRowShowsArtistImage`, `Save_AfterTwoDeezerPicks_SecondReplacesBothImages` |
| REQ-ART-12 | A pick touches nothing else, writes nothing | `SongFormViewModel.ApplyRemotePick` | `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing` |
| REQ-ART-13 | X clears the pending images | `SongFormViewModel.ClearTitle`, `ClearArtist` | `Save_AfterDeezerPickThenXOnTitle_DropsSongImageKeepsArtistImage`, `Save_AfterDeezerPickThenXOnArtist_DropsBothImages` |
| REQ-ART-14 | Local pick unchanged | `SongFormViewModel.SelectArtist`; local Title pick navigates to a fresh form | `Save_LocalArtistPickAfterDeezerPick_KeepsPendingImages`, `Save_DeezerPickThenXOnArtistThenLocalArtistPick_SendsNoImages`, `Save_LocalArtistPickWithNoDeezerPick_SendsNoImages` |
| REQ-ART-15 | New song stores its image | `Services/SongService.cs` (`SaveFromFormAsync`, `CreateSongWithUrlsCoreAsync`) | `SaveFromForm_AddWithPendingImage_StoresImageOnSong`, `…_AddWithoutPendingImage_StoresNull`, `…_AddWithExplicitSaveAsNew_NullsIdentityButKeepsImage` |
| REQ-ART-16 | Artist image fills an empty artist only | `SongService.cs` (`ResolveFormArtistAsync`, `SaveFormResolution.FillArtistImage`, create helper) | `SaveFromForm_ArtistCreatedByPlainPath_GetsArtistImage`, `…_ArtistCreatedByCreateTypedDecision_GetsArtistImage`, `…_ExistingArtistByIdWithoutImage_IsFilled`, `…_ExistingArtistByExactNameWithoutImage_IsFilled`, `…_ExistingArtistWithImage_IsNeverChanged`, `…_ArtistChosenByUseExistingDecision_IsNeverChanged`; real SQLite: see T10a |
| REQ-ART-17 | Edit mode never changes the song image | `SongService.cs` edit path; `SongFormViewModel.BuildSaveRequest` | `SaveFromForm_EditModeWithImages_LeavesStoredImagesUnchanged`, `Save_EditModeAfterDeezerPick_SendsNullImages` |
| REQ-ART-18 | Failed Save stores nothing; pending images kept | `SongService.cs` (fill after refusals); `SongFormViewModel` | `SaveFromForm_NeedsDecision_ChangesNoImage`, `SaveFromForm_FailedWrite_ChangesNoImage`, `Save_AfterNeedsDecisionOutcome_NextRequestStillCarriesBothImages`, `Save_AfterFailedOutcome_NextSaveStillCarriesBothImages` |
| REQ-ART-19 | Songs list leading image | `SongRepository.GetPagedAsync`, `CatalogRepository` paged query; `SongsPage.xaml` both templates | Data: `SongListItemProjectionTests` (6). On screen: **manual E2E, T11** |
| REQ-ART-20 | Songs list supporting text | `Contracts/DTOs/List/SongListItemDto.cs` (`SupportingText`); `SongsPage.xaml` | `SongListItemDtoTests` (6) |
| REQ-ART-21 | 72 dp row | `SongsPage.xaml` (`Margin="0,8"` on the 56 dp image) | **manual E2E, T11** (Level C, no unit test) |
| REQ-ART-22 | Placeholder covers every gap | `ListItemLeadingImage.xaml`, `ListItemLeadingAvatar.xaml` | **manual E2E, T11** (Level C, no unit test) |
| REQ-ART-23 | Loaded images are reused | URL string → `UriImageSource`, caching on by default, validity 1 day (Microsoft Learn, MAUI Image, "Load a remote image") | **manual E2E, T11** |
| REQ-ART-24 | Remote artist suggestions carry the image | `ArtistSuggestionDto`; `ArtistSuggestionService` | `ArtistSuggestionServiceImageTests` (3) |

### Backlog and governance

- Governed list (`.claude/library/component-safety-gate.md`): added `ListItemLeadingImage` (now 3 consumers:
  `YouTubeSearchPage`, `AutocompleteDXField`, `SongsPage`) and `AutocompleteDXField` (3 consumers, was
  missing). `ListItemLeadingAvatar` has one consumer (`AutocompleteDXField`) and becomes governed with its second.
- Follow-up items registered (💡): "Backfill Deezer images for songs and artists saved before the artwork
  item"; "Artists list rows show the artist picture as a leading avatar".
- "Song list item shows artist name, version and featured artists" (2026-11-09): delivered by REQ-ART-20;
  set 🟡 until the device check.
- This item and the two component items stay 🟡 In Progress until Helder's device check (T11).

## Device check (T11 — Helder)

An emulator was connected during this session (`emulator-5554`). The build was **not** installed on it:
another session is testing BUG-086 on the same device, and installing this branch would replace its app
and migrate its database. Nothing below has been seen on a screen yet.

Install the `develop` build, then:

1. **Songs list** (REQ-ART-19, 20, 21): every row shows a grey square with a music note (no song has an
   image yet); the second line reads "artist · version · feat. …"; rows are taller than before (72 dp)
   and the YouTube button and checkbox still work; selecting a row keeps the image.
2. **New Song → Title** (REQ-ART-06, 07, 08): type "commando ramones". Deezer rows show a cover, the
   second line shows "Ramones · 1999 Remaster · Greatest Hits", and "Deezer" sits at the right. Local rows
   show "Library". Check that the menu rows grow to fit the cover (not confirmed against the DevExpress
   docs) and that the list is still usable above the keyboard.
3. **Pick a Deezer row and Save** (REQ-ART-11, 15, 16): back in the Songs list the new song shows its
   cover. New Song → Artist: typing that artist shows its picture in a circle; other artists show the
   person icon; the second line shows the real catalog count (REQ-ART-09, 10).
4. **X after a pick** (REQ-ART-13): pick a Deezer row, tap X on Title, type a title by hand, Save: the
   song shows the artist's picture (no cover). Pick a Deezer row, tap X on Artist, pick another artist,
   Save: the song shows that artist's picture if it has one, else the music note.
5. **Offline** (REQ-ART-22, 23): open the Songs list online, turn on airplane mode, leave and reopen the
   list: covers seen before still show; a row never seen shows the placeholder, with no error.
6. **Unchanged consumers** (component Gate 3): YouTube search results show thumbnails as before (no grey
   background, no icon); New Person → Full Name menu rows look as before (long names now end with "…").
7. **Edit a song** (REQ-ART-17): open the saved song, change the Version, Save: its cover is unchanged.

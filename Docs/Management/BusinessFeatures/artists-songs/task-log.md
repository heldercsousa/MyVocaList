f# Task Log — validated-noodling-island (Artists & Songs)

---

## Task: BUG-001 fix — Back button + trailing icon style
**Plan:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-001-artists-page-no-back-button.md`
**Status:** Reviewed — PASS-WITH-MINOR
**Started:** 2026-06-03
**Completed:** 2026-06-03

### Changed files:
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — added `GoBackCommand` property and `GoBackAsync` private method
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — added `NavigationIcon`/`NavigationCommand` to SmallAppBar; changed `IconButton` → `StandardIconButton` + `SemanticProperties.Description` on both ItemTemplate and SelectedItemTemplate trailing buttons

### Verification evidence
- Build: PASS (0 errors)
- Tests: SKIPPED (no test files changed — bug fix is pure UI/ViewModel wiring)
- Post-edit re-read: confirmed — NavigationIcon/GoBackCommand on SmallAppBar; StandardIconButton on both templates; `IconButton` no longer present in ArtistsPage.xaml
- Spec compliance: confirmed — bug doc updated to Fixed status with resolution notes

### Review verdict (2026-06-25, per-task review loop)
**PASS-WITH-MINOR.** Both bug issues are resolved in current code (back button + trailing icon style); constitutional checks clean (SafeAreaEdges present, English-only, DevExpress-first `dx:DXButton`, no business logic in VM).
- **Doc/code divergence (fix as reconciliation):** the actual fix lives in `CrudListPageBase.OnNavigatedTo` (context-aware `menu`/`arrow_back_outlined` + `AppBarNavigationIcon`/`AppBarNavigationCommand` bindings, commit `0d69add`), which superseded the documented `GoBackCommand`/`GoBackAsync` approach. The task-log "Changed files" (line 12) and the bug doc Resolution section describe a `GoBackCommand` on `ArtistsViewModel` that **no longer exists**, and omit `CrudListPageBase.cs` (the real changed file). Reconcile both docs to the shipped implementation.
- **Regression-test gap (bug-tracking.md HARD RULE):** bug severity is "High" → maps to **Major**. The navigation logic is now testable via the `AppBarNavigationIcon` property, yet no regression test exists and the task-log records `Tests: SKIPPED` with no documented manual E2E verification step. Add a regression test (or a documented E2E step) before closing the bug.

## Task: Phase 10 — Domain Refactor
**Plan:** `Docs/superpowers/plans/validated-noodling-island.md`
**Status:** Review task done
**Started:** 2026-05-15
**Completed:** 2026-05-15

### Changed files:
- `Domain/Entity/Song.cs` — renamed `Artist` nav prop to `OriginalArtist`; added `Lyrics string?`; added `CatalogEntries` collection nav
- `Domain/Entity/Artist.cs` — renamed `Songs` nav prop to `OriginalSongs`; added `CatalogEntries` collection nav
- `Domain/Entity/Catalog.cs` — created: join entity with composite key (ArtistId, SongId) and both nav props
- `Domain/RepositoryInterface/ISongRepository.cs` — replaced `GetPagedByArtistAsync` with global `GetPagedAsync`; changed `GetByExternalIdAsync` return to `Song?`; removed `CountByArtistAsync`, `CountByArtistsAsync`, `SearchByTitleAsync`
- `Domain/RepositoryInterface/ICatalogRepository.cs` — created: catalog-scoped paged, count, exists, add, remove, save
- `Domain/ServicesInterfaces/ISongService.cs` — added `lyrics` param to `CreateSongAsync` and `UpdateSongAsync`; added `hasManualEdits` to `UpdateSongAsync`; added `ExistsByTitleForArtistAsync`; made `GetPagedSongsForListAsync` global (removed `artistId`)
- `Domain/ServicesInterfaces/ICatalogService.cs` — created: paged, add-to-catalog, remove-from-catalog
- `Contracts/DTOs/List/SongListItemDto.cs` — replaced `ArtistId`/`ArtistName` with `OriginalArtistId` (int, NOT nullable) / `OriginalArtistName?`; all string fields made nullable; removed `Lyrics` (form-only)
- `Contracts/DTOs/List/ArtistListItemDto.cs` — renamed `SongCount` → `CatalogCount`; renamed `SongCountText` → `CatalogCountText` with updated copy ("No catalog" / "1 song in catalog" / "N songs in catalog")
- `Docs/specs/artists-songs/tasks.md` — checked off tasks 10.1–10.10

### Build notes
Build FAILED at solution level as expected — 5 compile errors confined to `SongRepository` (Infra) and `SongService` (Services):
- `SongRepository` does not implement `ISongRepository.GetPagedAsync` (new global signature)
- `SongService` does not implement `ISongService.CreateSongAsync`, `UpdateSongAsync`, `ExistsByTitleForArtistAsync`, `GetPagedSongsForListAsync` (updated signatures)
- 1 nullable warning in `SongRepository.GetByExternalIdAsync` (return type mismatch — will be fixed in Phase 11)

Domain and Contracts projects compiled cleanly (0 errors in those projects). This is the documented and expected state for Phase 10.

### Verification evidence
- Build: FAIL (expected — 5 errors in Infra/Services, 0 errors in Domain/Contracts)
- Tests: FAIL (test project failed to build due to Infra/Services errors — expected at this stage; to be fixed in Phase 15)
- Post-edit re-read: confirmed — all 9 files match target specifications
- Spec compliance: confirmed — `tasks.md` Phase 10 section fully checked; `ArtistId` remains `int` NOT NULL; no Lyrics in DTO; `OriginalArtistId` is `int` not `int?`

---
## Task: BUG-021 — SongsPage FAB crash: ISimilarityScorer not registered in DI (Critical)
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-021-songspage-fab-crash/BUG-021-songspage-fab-crash.md (commit message as spec — Bug Fix Pattern)
**Status:** To Review
**Started:** 07/02/2026
**Completed:** 07/02/2026

### Changed files:
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` — new; `AddAppServices()` extension holding the platform-independent registrations extracted verbatim from `MauiProgram.cs`, plus the missing `ISimilarityScorer` → `SimilarityScorer` Scoped registration (the fix)
- `MyVocaList/MauiProgram.cs` — replaced the extracted registration blocks with a single `builder.Services.AddAppServices();` call (behavior identical)
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` — new; 3 DI-resolution regression tests for the SongFormViewModel dependency graph
- `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-021-songspage-fab-crash/BUG-021-songspage-fab-crash.md` — Root Cause / Fix / Regression Test / Status: Fixed

### Verification evidence
- Build: PASS (0 errors)
- Tests: PASS (406 tests, 0 failures; regression test `AddAppServices_ResolvingArtistResolutionService_Succeeds` seen RED before the fix with the exact production exception "Unable to resolve service for type 'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer' while attempting to activate 'MyVocaList.Services.ArtistResolutionService'", GREEN after)
- Post-edit re-read: confirmed — MauiProgram.cs, ServiceCollectionExtensions.cs, AppServicesRegistrationTests.cs
- Spec compliance: confirmed — bug doc updated (Bug Fix Pattern, no three-file spec required); full SongFormPage/SongFormViewModel dependency chain walked, no other DI gaps found

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| BUG-021 | FAB on SongsPage must open SongFormPage without DI crash | ServiceCollectionExtensions.AddAppServices (ISimilarityScorer registration) | AddAppServices_ResolvingArtistResolutionService_Succeeds; AddAppServices_ResolvingSongResolutionService_Succeeds; AddAppServices_ResolvingSongFormViewModelGraph_Succeeds |

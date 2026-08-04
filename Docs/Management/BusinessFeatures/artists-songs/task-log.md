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
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-021-songspage-fab-crash/BUG-021-songspage-fab-crash.md (commit message as spec — Bug Fix Pattern)
**Status:** To Review
**Started:** 07/02/2026
**Completed:** 07/02/2026

### Changed files:
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` — new; `AddAppServices()` extension holding the platform-independent registrations extracted verbatim from `MauiProgram.cs`, plus the missing `ISimilarityScorer` → `SimilarityScorer` Scoped registration (the fix)
- `MyVocaList/MauiProgram.cs` — replaced the extracted registration blocks with a single `builder.Services.AddAppServices();` call (behavior identical)
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` — new; 3 DI-resolution regression tests for the SongFormViewModel dependency graph
- `Docs/Management/BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-021-songspage-fab-crash/BUG-021-songspage-fab-crash.md` — Root Cause / Fix / Regression Test / Status: Fixed

### Verification evidence
- Build: PASS (0 errors)
- Tests: PASS (406 tests, 0 failures; regression test `AddAppServices_ResolvingArtistResolutionService_Succeeds` seen RED before the fix with the exact production exception "Unable to resolve service for type 'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer' while attempting to activate 'MyVocaList.Services.ArtistResolutionService'", GREEN after)
- Post-edit re-read: confirmed — MauiProgram.cs, ServiceCollectionExtensions.cs, AppServicesRegistrationTests.cs
- Spec compliance: confirmed — bug doc updated (Bug Fix Pattern, no three-file spec required); full SongFormPage/SongFormViewModel dependency chain walked, no other DI gaps found

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| BUG-021 | FAB on SongsPage must open SongFormPage without DI crash | ServiceCollectionExtensions.AddAppServices (ISimilarityScorer registration) | AddAppServices_ResolvingArtistResolutionService_Succeeds; AddAppServices_ResolvingSongResolutionService_Succeeds; AddAppServices_ResolvingSongFormViewModelGraph_Succeeds |


## Moved from BACKLOG.md (2026-07-15) — Artists & Songs Catalog

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked | **Phase 16C.1 emulator smoke test run 2026-07-03 (Helder) — FAILED.** Full findings + evidence: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 and `artists-songs/tasks.md § Phase 16C`. Hard blocker: **BUG-027 (Critical)** — SongFormPage Artist field has no working required-field validation and no autocomplete; typing a non-matching name and blurring clears the entry instead of offering create-new-artist — **song registration is impossible**, which also blocked TEST-002 (BUG-023) and TEST-003 (BUG-024) from being exercised. Do not resume 16C.2–16C.5 until BUG-027 is fixed and 16C.1 is re-run green. New bugs registered below (BUG-027–032). Spec: `Docs/Management/BusinessFeatures/artists-songs/` · Findings: `artists-songs/consolidation-findings.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-027: SongFormPage Artist field — no required-field validation, no autocomplete, blur clears type…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-027: SongFormPage Artist field — no required-field validation, no autocomplete, blur clears typed text with no create-new fallback (Critical) | 💡 Pending | **Dep note 2026-07-11:** the autocomplete part of this fix depends on DevCycleCraft **①** + **②** (see *Form & Autocomplete UX Overhaul*); fix direction is owned by the parked *Artist & Song Form UX Redesign*. Blocks all song creation/edit — see Artists & Songs Catalog row above. Found during TEST-001/TEST-006/TEST-007 emulator run. Details: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 step 7. |


## Moved from BACKLOG.md (2026-07-15) — BUG-028: ArtistsPage trailing `queue_music_outlined` button no-op — regression of BUG-015/BUG-019 fi…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-028: ArtistsPage trailing `queue_music_outlined` button no-op — regression of BUG-015/BUG-019 fix (Major) | 💡 Pending | Artist name is now visible (BUG-019 fix holds) but tapping the trailing button still does not navigate to the artist's Catalog page. Details: `artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/task-log.md` (regression note), `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-009. |


## Moved from BACKLOG.md (2026-07-15) — BUG-029: ArtistFormPage — tapping leading "Search music database" icon crashes the app (Critical)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-029: ArtistFormPage — tapping leading "Search music database" icon crashes the app (Critical) | 🔵 Deferred | **Superseded 2026-07-10:** Helder decided the search-strip element itself must be removed from both forms (see ↳ *Artist & Song Form UX Redesign* below) — fixing a crash in an element slated for deletion is wasted effort. Re-triage only if the redesign keeps any part of the strip. Original details: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 step 9. |


## Moved from BACKLOG.md (2026-07-15) — BUG-030: ArtistFormPage search strip UX unclear — confusing leading/trailing icons, possibly duplica…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-030: ArtistFormPage search strip UX unclear — confusing leading/trailing icons, possibly duplicates SongFormPage search, opens Artist search when Song search may be intended (spec gap) | 🔵 Deferred | **Spec gap ANSWERED by Helder 2026-07-10:** the element's purpose is unclear even to Helder and it appears to duplicate the autocomplete goal — it must disappear from both ArtistFormPage and SongFormPage. Folded into ↳ *Artist & Song Form UX Redesign* below. Original details: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 steps 5–5.3. |


## Moved from BACKLOG.md (2026-07-15) — BUG-031/032: No 3rd-party-API autocomplete appears while typing Artist Name or Song Title (spec gap)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-031/032: No 3rd-party-API autocomplete appears while typing Artist Name or Song Title (spec gap) | 🔵 Deferred | **Spec gap ANSWERED by Helder 2026-07-10:** autocomplete (local DB + 3rd-party API) IS required on both entries. Folded into ↳ *Artist & Song Form UX Redesign* below, which defines the full behavior. First step there: check what the original artist/song specs actually predicted for autocomplete. Original details: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 steps 3 and 7. |


## Moved from BACKLOG.md (2026-07-15) — Bug: GoToSettings navigation exception

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | ↳ Bug: GoToSettings navigation exception | ✅ Fixed | `GoToAsync("settings")` called from pushed-page context (SongFormPage); FlyoutItem requires absolute route `//settings`. Single-line fix in `SongFormViewModel.cs`. |


## Moved from BACKLOG.md (2026-07-15) — Bug: Artists page missing back button + unclear trailing toggle

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: Artists page missing back button + unclear trailing toggle | ✅ Fixed | No back button on ArtistsPage AppBar; trailing pill button has no icon/label. Both fixed 2026-06-03. Details: `artists-songs/bugs/BUG-001-artists-page-no-back-button.md` |


## Moved from BACKLOG.md (2026-07-15) — Bug: Artist/Song form search strip non-MD3

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: Artist/Song form search strip non-MD3 | ✅ Fixed | Fixed via Search Picker feature. Dedicated picker pages replace inline search strips; MD3-compliant trigger row (`ListItem`) navigates to picker. Details: `artists-songs/bugs/BUG-002-artist-form-search-non-md3.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-015: ArtistsPage trailing button (ViewCatalog) does nothing when tapped (Major)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | ↳ BUG-015: ArtistsPage trailing button (ViewCatalog) does nothing when tapped (Major) | ✅ Fixed | `RelativeSource AncestorType` cannot resolve ViewModel inside DataTemplate in ContentView; replaced with `x:Reference page` binding. Commit `509a0ed`. Details: `artists-songs/bugs/BUG-015-artistspage-trailing-button-noop.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-016: SongsPage FAB crash on Add tap — route "song-picker" collision (Critical)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | ↳ BUG-016: SongsPage FAB crash on Add tap — route "song-picker" collision (Critical) | ✅ Fixed | `QueueSongPickerPage` FlyoutItem route renamed `"queue-song-picker"`; `Routes.QueueSongPicker` constant added; regression test passes (358 tests green). Commit `8e1391b`. Details: `artists-songs/bugs/BUG-016-songspage-fab-crash.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-017: ArtistFormPage + SongFormPage `navigate_next` icon missing — Glide FileNotFoundException pe…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | ↳ BUG-017: ArtistFormPage + SongFormPage `navigate_next` icon missing — Glide FileNotFoundException per render (Major) | ✅ Fixed | Replaced `navigate_next` → `arrow_forward_outlined` (SVG confirmed present). 3 occurrences: `ArtistFormPage.xaml:72`, `SongFormPage.xaml:78,171`. 357 tests green. Commit `89fc795`. Emulator-verified 2026-07-03 (TEST-008) — no Glide FileNotFoundException in logcat. Details: `artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/` |


## Moved from BACKLOG.md (2026-07-15) — BUG-018: ArtistFormPage Edit Save — fatal crash EF Core duplicate entity tracking (Critical)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | ↳ BUG-018: ArtistFormPage Edit Save — fatal crash EF Core duplicate entity tracking (Critical) | ✅ Fixed | Shared singleton `AppDbContext` + `GetPagedAsync` without `AsNoTracking()` → list loads track full `Artist` entities → concurrent `Task.Run` offloads can materialise the same entity twice → `ArtistRepository.UpdateAsync:124` throws `InvalidOperationException`. Fix: added global `QueryTrackingBehavior=NoTracking` to AppDbContext + explicit `.AsNoTracking()` on list queries + `ArtistListItem` read model. Commit `d8663f9` (2026-06-27). All 360 tests passing including regression test. Details: `artists-songs/bugs/BUG-018-artistformpage-edit-save-crash/` |


## Moved from BACKLOG.md (2026-07-15) — BUG-019: ArtistsPage list item trailing button noop + artist name invisible (Major)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-30 | ↳ BUG-019: ArtistsPage list item trailing button noop + artist name invisible (Major) | ⚠️ Partially regressed | DataTemplate `x:DataType="dto:ArtistListItemDto"` not updated when BUG-018 changed collection to `ArtistListItem` (Domain.ReadModels) — compiled binding cast failed silently; Name→null, CommandParameter→null→CanExecute=false. Fix: added `xmlns:domain` for Domain.ReadModels + changed `x:DataType` in both ItemTemplate and SelectedItemTemplate DataTemplates. 360 tests green. **Emulator retest 2026-07-03 (TEST-009): artist names ARE visible (fix holds), but the trailing queue_music button is a no-op again — tracked as BUG-028.** Details: `artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/` | 


## Moved from BACKLOG.md (2026-07-15) — BUG-020: SongsPage FAB (Add song) crashes app — unguarded SecureStorage.GetAsync in async void OnApp…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-01 | ↳ BUG-020: SongsPage FAB (Add song) crashes app — unguarded SecureStorage.GetAsync in async void OnAppearing (Critical) | ✅ Fixed | Distinct from BUG-016/017 (both confirmed already merged). `SongFormPage.OnAppearing` is `async void` and calls `RefreshApiKeyFlagAsync()` → `SecureStorage.GetAsync` with no exception handling anywhere in the chain; on Android this API can throw (corrupted/inaccessible Keystore alias, e.g. after reinstall or signing-key change without clearing app data) — exception escapes `async void` → `GlobalExceptionHandler` logs Fatal → app terminates. Unique to Songs (Artist/Venue/Person OnAppearing do no I/O). Fix: wrapped `RefreshApiKeyFlagAsync()` in try-catch, falls back to `HasYouTubeApiKey = false` on failure. Regression test added (`SongFormViewModelTests.RefreshApiKeyFlagAsync_SecureStorageThrows_DoesNotThrowAndSetsFalse`), confirmed Red→Green. 361/361 tests green, build 0 errors. Commit `3b2cb75` (merged to develop and pushed — verified 2026-07-02). **Found via static analysis, not device reproduction** — emulator-verified 2026-07-03 (TEST-010): FAB opens SongFormPage, no SecureStorage crash. Details: `artists-songs/bugs/BUG-020-songspage-fab-crash-secure-storage.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-021: SongsPage FAB (Add song) crashes app — `ISimilarityScorer` not registered in DI (Critical)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-01 | ↳ BUG-021: SongsPage FAB (Add song) crashes app — `ISimilarityScorer` not registered in DI (Critical) | ✅ Fixed | Continuation of BUG-016/BUG-020 (both fixed real but distinct issues). Debug screenshot showed the true crash: `Unable to resolve service for type 'ISimilarityScorer' while attempting to activate 'ArtistResolutionService'` — FAB tap activates `SongFormViewModel` → `ISongResolutionService`/`IArtistResolutionService`, both need `ISimilarityScorer`, never registered in `MauiProgram.cs`. Fix: registrations extracted to `AddAppServices()` extension (`MyVocaList/Extensions/ServiceCollectionExtensions.cs`) + missing Scoped registration added; full SongFormViewModel dependency chain walked — no other gaps. 3 DI-resolution regression tests (Red seen with exact production error before fix). 419/419 tests green, build 0 errors. Commit `5014d29`, merged to develop `9171f60`, pushed. Emulator-verified 2026-07-03 (TEST-011): FAB opens SongFormPage, no DI resolution error. Details: `artists-songs/bugs/BUG-021-songspage-fab-crash/BUG-021-songspage-fab-crash.md` |


## Moved from BACKLOG.md (2026-07-15) — BUG-023: SongForm resolution/merge BottomSheets can never open — `IsExpanded` bindings deleted (Crit…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-02 | ↳ BUG-023: SongForm resolution/merge BottomSheets can never open — `IsExpanded` bindings deleted (Critical) | ✅ Fixed | Root cause: commit `e743601` ("fix errors", 2026-06-23) removed `IsExpanded="{Binding IsResolutionSheetVisible/IsMergeSheetVisible, Mode=TwoWay}"` from `SongFormPage.xaml` without replacement — `dx:BottomSheet` has no bindable "open" property in the DevExpress version in use (`Show()`/`Close()` require a host `Page`), so a pure XAML two-way binding was never valid. `SongFormViewModel` continued setting the flags correctly (unchanged, confirmed by pre-existing `SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible`), but the view never observed them. Fix: restored sync via the project's confirmed BottomSheet Code-Behind Pattern (`dialogs-validation.md § BottomSheet State Management`, same approach as `ConfirmSheet.xaml.cs`) — `SongFormPage.xaml.cs` subscribes to `SongFormViewModel.PropertyChanged`, calls `resolutionSheet`/`mergeSheet` `.Show(BottomSheetState.HalfExpanded, this)` / `.Close()`, and syncs `StateChanged` back to the VM flags with re-entrancy guards. `SongFormViewModel` was not modified. Added regression guard `DismissResolutionSheetCommand_AfterExactLocalMatch_SetsIsResolutionSheetVisibleFalse` (passed immediately — Green before/after, since the VM was never broken; documented as a guard test per `bug-tracking.md`, not a Red-proving one — the actual XAML wiring bug is not unit-testable). 436/436 tests green (435 baseline + 1 new), build 0 errors. Commit `fix: SongFormPage — BUG-023 restore BottomSheet state bindings` on `develop`. Details: `artists-songs/bugs/BUG-023-songform-bottomsheet-broken/BUG-023-songform-bottomsheet-broken.md`. **Emulator smoke test BLOCKED 2026-07-03 (TEST-002) — could not exercise: song creation itself is blocked by BUG-027, so the resolution flow this bug fixes was never reached. Re-run once BUG-027 is fixed.** |


## Moved from BACKLOG.md (2026-07-15) — BUG-024: SongForm edit-mode Save wipes FeaturedArtists + Lyrics and discards Version — silent data l…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-02 | ↳ BUG-024: SongForm edit-mode Save wipes FeaturedArtists + Lyrics and discards Version — silent data loss (Critical) | ✅ Fixed | Found by Task 04 review (2026-07-02); Helder approved fix approach same day. Fixed 2026-07-02 per approved approach: `ISongService.GetSongByIdAsync` added (repository `GetByIdAsync` already returned the full entity — no Infra change); `LoadSongForEditAsync` fully hydrates Title/Version/FeaturedArtists/Lyrics + external identity under the `_isHydrating` dirty-guard; `UpdateSongAsync` gains `version` param (null = keep existing, mirrors externalId semantics) with Services-layer validation; `ExecuteEditSaveAsync` sends complete form data. 7 regression tests, core ones confirmed Red first (hydration test failed with empty FeaturedArtists; edit-save Moq verify showed `version: null`). 435/435 tests green (baseline 428). Details: `artists-songs/bugs/BUG-024-songform-edit-data-loss/BUG-024-songform-edit-data-loss.md`. Follow-up candidate documented there: edit-path uniqueness check is still title-only, blocking edit of same-title sibling versions (pre-existing). **Emulator smoke test BLOCKED 2026-07-03 (TEST-003) — could not exercise: editing an existing song was blocked by BUG-027 (same Artist-field blocker). Re-run once BUG-027 is fixed.** Opus review: **APPROVE WITH NITS** (N1: fire-and-forget hydration — if `GetSongByIdAsync` throws, Save could still send empty fields; suggested `_editHydrated` gate as follow-up hardening. N2: service-side failures route to Title field, mitigated by VM pre-validation). |


## Moved from BACKLOG.md (2026-07-15) — Bug: New Song — Save has no effect (BUG-005)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: New Song — Save has no effect (BUG-005) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4B (`dd36b58`). SongService now uses atomic save; exception handling added. Spec: `artists-songs/bugs/BUG-005-new-song-save-broken.md` |


## Moved from BACKLOG.md (2026-07-15) — Bug: New Song — double-tap on search link crashes app (BUG-006)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: New Song — double-tap on search link crashes app (BUG-006) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4A (`9b37d2a`). `AllowConcurrentExecutions=false` guard added. Spec: `artists-songs/bugs/BUG-006-search-song-double-tap-crash.md` |


## Moved from BACKLOG.md (2026-07-15) — Bug: New Song — SearchAppBar renders duplicate back arrow in picker (BUG-007)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: New Song — SearchAppBar renders duplicate back arrow in picker (BUG-007) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4A (`9b37d2a`). `Shell.BackButtonBehavior` set on picker pages. Spec: `artists-songs/bugs/BUG-007-searchappbar-duplicate-back-arrow.md` |


## Moved from BACKLOG.md (2026-07-15) — Bug/Gap: SongFormPage Artist field — must be autocomplete with blur-clear (BUG-008)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug/Gap: SongFormPage Artist field — must be autocomplete with blur-clear (BUG-008) | 🔵 Superseded | **Out-of-date 2026-07-10 — DEFERRED into the Artist & Song Form UX Redesign (above).** Originally ✅ Fixed by Song Import & Entity Resolution Wave 4B (`dd36b58`): Artist field got AutocompleteField with **blur-clear**. The redesign **reverses that very behavior** — requirement #3 ("never clear the typed name on blur/no-match") + Phase 3 task "blur-clear removal + IsArtistLocked retirement". No independent action: the redesign owns this field's final behavior. Original spec (now stale): `artists-songs/bugs/BUG-008-songform-artist-autocomplete.md`. |


## Moved from BACKLOG.md (2026-07-15) — Bug/UX: Add YouTube URL before song saved shows blocking validation (BUG-009)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug/UX: Add YouTube URL before song saved shows blocking validation (BUG-009) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4B (`dd36b58`). `SaveSongWithUrlsAsync` handles song + URLs atomically. Spec: `artists-songs/bugs/BUG-009-add-url-before-save-ux.md` |


## Moved from BACKLOG.md (2026-07-15) — Bug/Gap: Song API auto-fill (Deezer/MusicBrainz) — never functional (BUG-010)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug/Gap: Song API auto-fill (Deezer/MusicBrainz) — never functional (BUG-010) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4A (`9b37d2a`). SongPickerViewModel implemented; Deezer + MusicBrainz wired; `ExternalApiId`/`ExternalApiSource` persisted. Spec: `artists-songs/bugs/BUG-010-song-api-autofill-broken.md` |


## Moved from BACKLOG.md (2026-07-15) — Fuzzy entity matching for API import (BUG-010 follow-up)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Fuzzy entity matching for API import (BUG-010 follow-up) | ✅ Fixed | Subsumed into **Song Import & Entity Resolution** (merged). FuzzySharp bounded fuzzy matching + resolution BottomSheet implemented. |


## Moved from BACKLOG.md (2026-07-15) — Song Import & Entity Resolution

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-13 | ↳ **Song Import & Entity Resolution** | 🟡 In Progress | **MERGED to `develop` (verified 2026-06-19: 0 commits ahead of origin/develop; FuzzySharp, `Song.Version`, resolution engine all present). ⏳ Helder: emulator smoke test still pending (see `song-import-resolution/tasks.md § Wave 5`) before marking ✅ Done.** Waves 0–4 done, 354 tests pass, MAUI builds 0 errors. Senior-grade insert-vs-update engine for manual + Deezer/MusicBrainz data: version variants (`Song.Version` + 3-col unique index), bounded on-device fuzzy matching (FuzzySharp 2.0.2), conflict-safe field-level merge (protects `HasManualEdits`), external-id persistence (core BUG-010 fix). Resolution + merge BottomSheets. Folds in BUG-004/005/006/007/008/009/010. Spec: `artists-songs/song-import-resolution/`. |


## Moved from BACKLOG.md (2026-07-15) — Spec cleanup — requirements.md + design.md post-Phase-2 reconciliation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-20 | ↳ Spec cleanup — requirements.md + design.md post-Phase-2 reconciliation | ✅ Done | 6 spec-vs-code gaps fixed (2026-06-23): AC-1.16 rewritten, menu entry simplified, Page Structure row, AppShell block, Song.Version added, 3-col index. Details: `artists-songs/spec-cleanup-p2.md` |


## Moved from BACKLOG.md (2026-07-15) — YouTube Search Launch Button

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ YouTube Search Launch Button | ✅ Done | 3rd karaoke URL approach: button opens YouTube app (or browser fallback) with pre-filled search `karaoke <title> <artist>`. Available in SongFormPage, SongsPage list, and SongPickerPage. Launcher.TryOpenAsync fallback to Browser. Spec: `Docs/Management/BusinessFeatures/artists-songs/youtube-search-launch/` |


## Moved from BACKLOG.md (2026-07-15) — Bug: navigate_next icon missing SVG → Glide FileNotFoundException on form pages (BUG-017)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | ↳ Bug: navigate_next icon missing SVG → Glide FileNotFoundException on form pages ([BUG-017](BusinessFeatures/artists-songs/bugs/2026-06-27-BUG-017-artistscrud-emulator-debug-often-stops/task-log.md)) | ✅ Fixed | `navigate_next` had no SVG in Resources/Images/; replaced with `arrow_forward_outlined` (SVG confirmed present) in ArtistFormPage.xaml (1×) and SongFormPage.xaml (2×). Minor severity. Build PASS, 357 tests PASS. Commit on branch `fix/bug-017-navigate-next-icon`. |

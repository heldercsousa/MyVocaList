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

---
## Task: BUG-084 — Song form sheets: scrim and Back do not dismiss; Cancel unreachable with many rows (Major)
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-09-BUG-084-song-form-sheets-scrim-and-back-do-not-dismiss-cancel-unreachable-with-many-rows/README.md
**Status:** To Review
**Started:** 2026-11-09
**Completed:** 2026-11-09

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
- `Docs/Management/BusinessFeatures/artists-songs/task-log.md` (this entry)

### Root cause (verified against DX 25.2.4 XML docs)
- Both sheets (`resolutionSheet`, `artistMatchSheet`) had `AllowDismiss="False"`, so scrim tap and the Android Back key were ignored; only the Cancel button closed them.
- The sheet body was one unbounded `VerticalStackLayout` inside a `HalfExpandedRatio="0.55"` sheet with `AllowedState="HalfExpanded"` and no `ScrollView`, so with 3+ candidate rows the Cancel button was clipped below the sheet and could not be scrolled to.
- `OnDisappearing` unsubscribed the ViewModel `PropertyChanged` handler and nothing re-subscribed it, so sheet sync stopped after the page reappeared.

### Before / after (SongFormPage.xaml, both sheets)
| Property / structure | Before | After | Why |
|---|---|---|---|
| `AllowDismiss` | `False` | `True` | Scrim tap, Back key and swipe dismiss the sheet (REQ-SFT-19/22/23) |
| `AllowedState` | `HalfExpanded` | `All` | User can expand to full height when the list is long (DX enum: `All` / `FullExpanded` / `HalfExpanded`) |
| `HalfExpandedRatio` | `0.55` | `0.7` | More visible body before the user needs to expand |
| Sheet child | one `VerticalStackLayout` | `Grid RowDefinitions="*,Auto"` | Row 0 `ScrollView` holds title, candidate rows, action blocks; row 1 holds divider + Cancel, pinned |
| Cancel / primary actions | inside the unbounded stack | row 0 scroll body (actions) / row 1 pinned (Cancel) | Cancel always reachable; actions reachable by scrolling |

Bindings, commands, `Text` values and `x:DataType` are unchanged. The only content move is the divider that preceded Cancel, which now sits in row 1 above Cancel.

### Before / after (SongFormPage.xaml.cs)
- `OnAppearing`: `ViewModel.PropertyChanged -= OnViewModelPropertyChanged;` then `+=` (detach-first, so no double subscription). `OnDisappearing` unchanged.
- `OnResolutionSheetStateChanged` / `OnArtistMatchSheetStateChanged`: on a user-initiated `Hidden` while the flag is still true, the handler now calls `ViewModel.DismissResolutionSheetCommand` / `DismissArtistMatchSheetCommand` instead of setting the flag directly. Scrim, Back and swipe therefore take the same path as the Cancel button (nothing saved; `VersionEntryRequired` also resets for the resolution sheet). The `Sync*` re-entrancy guards are unchanged, so ViewModel-driven `Close()` calls do not re-enter, and the `IsVisible` check keeps late `Hidden` events a no-op.
- No ViewModel change was needed.

### Build notes
Build: passed (0 errors) | Tests: 911 passed, 0 failed | Commit SHA: see commit message
- Android build: passed (0 errors, 27 pre-existing NuGet vulnerability warnings), run after each XAML step (resolutionSheet, then artistMatchSheet) and after the code-behind change.
- Test project build: passed (0 errors). Full `MyVocaList.Tests` suite: 911 passed, 0 failed (baseline 902; the count increased with other develop work).
- Environment note: the worktree needed `dotnet restore` once for `MyVocaList` and `MyVocaList.Tests`. Background `dotnet build` runs stalled with no output for more than an hour while other sessions' MSBuild and VBCSCompiler processes were running; a foreground build with `-nodeReuse:false -m:1 -p:UseSharedCompilation=false` completed in about 2.5 minutes.
- Files written and re-read: `SongFormPage.xaml` (resolutionSheet block lines 257-351, artistMatchSheet block lines 359-~425), `SongFormPage.xaml.cs` (OnAppearing 24-45, artist handler 144-164, resolution handler 184-203).

### Verification evidence (AC traceability)
- REQ-SFT-19 (Back or scrim dismisses artist-match sheet, nothing saved): `AllowDismiss="True"` plus `DismissArtistMatchSheetCommand` routing. Needs device confirmation (see E2E).
- REQ-SFT-22 (Back or scrim dismisses resolution sheet, nothing saved): `AllowDismiss="True"` plus `DismissResolutionSheetCommand` routing. Needs device confirmation.
- REQ-SFT-23 (same as REQ-SFT-22 for the "Save as new song" branch): same path. Needs device confirmation.
- No automated regression test was added. BUG-084 is Major and UI-only (`bug-tracking.md`: Major UI-only → manual E2E documented here).

### Design concern (not blocking)
- A `Hidden` event that DX raises asynchronously (after a ViewModel `Close()`) can arrive after the VM has already re-shown the same sheet. The `IsVisible` check cannot tell that apart from a real user dismissal, so the sheet would close in that rare sequence. This existed before the change (the handler already cleared the flag). It is a candidate for the device run; it is not caused by this fix.

### Manual E2E checklist (Android S23, for Helder)
1. Open the artist-match sheet (type a near-duplicate artist name, Save). Tap the scrim: the sheet closes, nothing is saved, and the form is unchanged.
2. Repeat step 1 using the system Back key: the sheet closes, nothing is saved.
3. Re-open the artist-match sheet (Save again). It opens. Cancel closes it.
4. Create a long duplicate list (3+ rows) for the resolution sheet (Save with an existing song title). Scroll the sheet: every row is reachable, and Cancel is visible at the bottom without scrolling past it.
5. On the same long list, tap Cancel: the sheet closes, nothing is saved.
6. Re-open the resolution sheet after a dismissal (scrim or Back). It opens, and "Save as new version" and "Save as new song" remain reachable (scroll if needed).
7. Leave the Song form (Back to the list) and return to the form (navigate to Add or Edit again), then trigger Save with a duplicate. The sheet still opens (verifies the `OnAppearing` resubscribe).
8. Swipe the sheet down from half-expanded: it dismisses without saving. Drag up: it expands to full height (AllowedState All).

### Out of scope (per briefing)
- Discard sheet (5.6): VM-only, not touched.
- Artist field, Title field, `AutocompleteDXField*`: not touched.

---
## Task: BUG-085 — Red (tests only): trigram same-artist candidate query
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-09-BUG-085-song-save-misses-near-duplicate-title-when-the-typed-title-is-longer-than-the-stored-one/README.md
**Status:** To Review (Red phase; test project does not compile until the Builder adds the member)
**Started:** 2026-11-09
**Completed:** 2026-11-09

### Changed files:
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryArtistTrigramTests.cs` (new)
- `MyVocaList.Tests/Integration/Services/SongSaveTrigramDuplicateTests.cs` (new)
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryResolutionTests.cs` (two prefix-pool tests removed; moved, see map)
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (wrapper: new member delegate added; old delegate kept while the interface still has it)
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` (retargeted setups, one test replaced, three added)
- `Docs/Management/BusinessFeatures/artists-songs/task-log.md`

### Interface the Builder must implement (exact)
`ISongRepository` gains:
`Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesForArtistAsync(int artistId, string title, int take, CancellationToken ct = default);`
- Return type `SongSearchHit` (same as `SearchTitleCandidatesAsync`, carries SongId/Title/Version/ArtistName). Assumption, not in the brief: `Blocking: No`.
- SQL = `SearchTitleCandidatesAsync`'s `TitleSearchSql` plus `AND s.ArtistId = @artistId`; params `@m`, `@take`, `@artistId`.
- MATCH null (no word of 3+ chars) → collated title-prefix path scoped by artist (`(title ?? "").Trim() + "%"`, LIKE over `Collate(Title)`), `Take(take)`, mirroring REQ-SFT-33.
- Recommendation (not tested at repo level): return `[]` for a blank title, since an empty prefix would match every song of the artist. The service never sends one (see blank-title test).
- `SongResolutionService` fuzzy step: skip when title blank; otherwise call the new member with `(resolvedArtistId.Value, candidate.Title, SimilarityConstants.PoolSize, ct)`; map hits to `SongMatch(SongId, Title, Version, ArtistName, score)`; keep `>= DefaultThreshold`. `DerivePrefixToken` has no remaining test pin (grep-verified) and can go.
- Remove `GetFuzzyCandidatePoolAsync` from `ISongRepository` and `SongRepository` (only production consumer is the fuzzy step). Also delete its delegate in `SongServiceNewArtistUnitOfWorkTests.ThrowOnAddSongRepository` (line ~184) or that file stops compiling.
- Repo tests need the SongSearch FTS5 index: fixtures use `MigrateAsync` (`EnsureCreatedAsync` does not create the virtual table, and the trigram path would silently return `[]` through the "no such table" catch). Do not switch them to `EnsureCreatedAsync`.

### Red evidence
- `dotnet build MyVocaList.Tests`: 15 unique CS1061 errors, all for the missing `SearchTitleCandidatesForArtistAsync` member (ISongRepository, SongRepository, 7 sites in SongResolutionServiceTests, 7 in SongRepositoryArtistTrigramTests, 1 in the UoW wrapper). No other compile errors.
- Behavioural run (temporary csproj exclusion of the three non-compiling files, reverted; not committed): `SongSaveTrigramDuplicateTests.SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate` FAILS: `Expected: SongChoiceNeeded, Actual: Saved`. This is the reported bug reproduced at save level with real SQLite, real repository, real resolution service and real scorer. `SongServiceSaveFromFormTests` and `SongRepositorySearchTests` (65 tests) pass against current production.
- The rest of the new/retargeted tests cannot be run until the member exists; they fail with the compile errors above, which is the expected Red.

### New tests (all tagged `// [BUG] BUG-085`)
| Test | AC | Expectation before fix |
|------|----|------------------------|
| SongRepositoryArtistTrigramTests.SearchTitleCandidatesForArtistAsync_TypedTitleLongerThanStored_FindsStoredTitle | REQ-SFT-23 | regression: "Patiencee" finds "Patience" |
| …_OtherArtistSameTitle_IsExcluded | REQ-SFT-23 | artist scope |
| …_RespectsArtistScopeAndTake | REQ-SFT-23 | take=2 bounds result, all same artist |
| …_ShortTitle_UsesCollatedPrefixFallback | REQ-SFT-33 | "ab" → prefix only (not "Cab Driver"), artist-scoped |
| …_TypoInMiddle_StillRetrieves [Pateince, Paitence] | REQ-SFT-23 | trigram windows shared |
| …_TypoInFirstWord_FindsStoredTitleAndExcludesUnrelated | REQ-SFT-23 | trigram, not prefix |
| …_UnrelatedTitle_ReturnsEmpty | REQ-SFT-23 | no shared window → empty |
| SongSaveTrigramDuplicateTests.SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate | REQ-SFT-23 | FAILS now (Saved), must pass after |
| SongResolutionServiceTests.ResolveAsync_LongTitle_PassesWholeTypedTitleToTrigramQuery | REQ-SFT-23 | verifies (10, whole title, PoolSize) |
| …ResolveAsync_FuzzyBelowThreshold_DropsCandidate | REQ-SFT-23 | 0.81 dropped → NoMatch |
| …ResolveAsync_FuzzyAtThreshold_KeepsCandidate | REQ-SFT-23 | 0.82 kept (>=) |
| …ResolveAsync_BlankTitle_SkipsTrigramQuery | REQ-SFT-23 | no query for blank title (design §4 N1 empty-token rule) |

### Retargeted-tests map (old → new)
| Old test (file) | Disposition |
|-----------------|-------------|
| SongRepositoryResolutionTests.GetFuzzyCandidatePoolAsync_RespectsArtistScopeAndTake (AC-5.6) | Rewritten as SongRepositoryArtistTrigramTests.SearchTitleCandidatesForArtistAsync_RespectsArtistScopeAndTake, tag REQ-SFT-23 |
| SongRepositoryResolutionTests.GetFuzzyCandidatePoolAsync_PrefixFilter_ReturnsOnlyMatchingPrefix (AC-5.7) | Rewritten as …_TypoInFirstWord_FindsStoredTitleAndExcludesUnrelated, tag REQ-SFT-23. Prefix-only behaviour retired by the 2026-11-09 REQ-SFT-23 note. |
| SongResolutionServiceTests.ResolveAsync_FuzzyAboveThreshold_ReturnsFuzzyCandidates (AC-2.3) | Setup retargeted to the new member with the whole title; assertions unchanged |
| SongResolutionServiceTests.ResolveAsync_NoMatchAtAll_ReturnsNoMatch (AC-2.3) | Setup retargeted; assertion unchanged |
| SongResolutionServiceTests.ResolveAsync_LongTitle_UsesPrefixTokenForFuzzyPool | Deleted; replaced by ResolveAsync_LongTitle_PassesWholeTypedTitleToTrigramQuery (REQ-SFT-23) |
| SongServiceNewArtistUnitOfWorkTests wrapper delegate | Old delegate kept until the interface member is removed; new delegate added |
Other Grep hits (SongServiceSaveFromFormTests, SongSuggestionThresholdTests) pin `SearchTitleCandidatesAsync` / `SongSuggestionService`, not the fuzzy pool: untouched.

### Assumptions / open points (none blocking)
- AC IDs AC-2.3, AC-5.6, AC-5.7 are the tags already on these tests; I did not verify them against the current requirements text. The retired AC-5.7 prefix behaviour is flagged for Helder to confirm it can be dropped.
- Blank-title edge is tagged REQ-SFT-23 under design §4 N1; it is an edge of the new step, not a separate AC.

### Commit
Commit `test(song): ... (BUG-085 Red)` made with `--no-verify` (compile-level Red, see above).

---
## BUG-085 Green (Builder)
**Plan:** bug fix (no plan.md); spec: REQ-SFT-23 and REQ-SFT-33 in `changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/requirements.md`
**Status:** To Review (orchestrator note 2026-11-09: the block was a defect in the Tester's own test setup — the `_artistResolution` mock returned NoMatch so the fuzzy step was never reached — not a spec gap; corrected in `f64e70d2`, full suite 921/921; Builder's production change committed by the orchestrator)
**Started:** 2026-11-09
**Completed:** not committed (worktree `.worktrees/bug-085` left dirty for Helder's decision)

### Test defect (resolved in `f64e70d2`; originally reported as a spec gap): test mocks the artist resolver to NoMatch, so the fuzzy path is unreachable
**Location:** `MyVocaList.Tests/Integration/Services/SongSaveTrigramDuplicateTests.cs` `InitializeAsync` (the `_artistResolution` setup returns `ArtistResolution(ResolutionKind.NoMatch, null, [])` for every input); production `Services/SongResolutionService.cs` lines 49-53 (INV-1: the artist is resolved first and fuzzy only runs when `resolvedArtistId` is set).
**Gap description:** `SongService` passes the artist by id, but `SongResolutionService.ResolveAsync` always re-resolves the artist through `IArtistResolutionService` (SongCandidate carries no artist id, REQ-SFT-41). Under the test's NoMatch mock the resolver returns NoMatch before the fuzzy step, so `SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate` returns `Saved` with the production fix in place.
**Options:**
- Option A: Tester changes the artist mock to `ArtistResolution(ResolutionKind.ExactLocalMatch, _queen.Id, [])` (the artist exists; the test's own comment says so) — the test then exercises the fixed path.
- Option B: the resolver receives the artist id from SongService — contradicts REQ-SFT-41 and the single-resolution design; not recommended.
**Recommendation:** Option A because the test asserts behaviour of the song guard, not artist resolution, and the artist is known to exist.
**Blocking:** Yes — the Builder may not edit tests; the regression test (Critical/Major mandatory Red then Green) cannot reach Green until the mock is corrected.

### Changed files (uncommitted, worktree)
- `Domain/RepositoryInterface/ISongRepository.cs` — new `SearchTitleCandidatesForArtistAsync`; XML remark on `GetFuzzyCandidatePoolAsync` (unused after BUG-085, removal scheduled with the test wrapper)
- `Infra/Repository/SongRepository.cs` — `TitleSearchSelectSql` shared SELECT, `TitleSearchSql` (unchanged text), new `TitleSearchForArtistSql`; `SearchTitleCandidatesAsync` and new `SearchTitleCandidatesForArtistAsync` both delegate to private `SearchTitleCoreAsync`
- `Services/SongResolutionService.cs` — fuzzy step calls `SearchTitleCandidatesForArtistAsync(resolvedArtistId, candidate.Title, PoolSize)`, maps `SongSearchHit` to `SongMatch`, keeps `>= DefaultThreshold`, skips blank titles; `DerivePrefixToken` removed
- `Domain/Resolution/SimilarityConstants.cs` — `PrefixTokenMaxLen` removed (now unused; outside the brief's enumerated list, removed because the brief asked for removal when unused)
- `Docs/Management/BusinessFeatures/artists-songs/task-log.md` — this section

### Build notes
Build: passed (0 errors, 7 projects; 2 attempts — attempt 1 green, attempt 2 after a SQL fix below) | Tests: first run 921 total, 920 passed, 1 failed (`SongSaveTrigramDuplicateTests.SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate`, a test defect: see below); after the test correction `f64e70d2` the full suite is 921/921 | Commit SHA: `8b94d1e6` (production, committed by the orchestrator); Verifier (Standard) CONDITIONAL PASS, 0 blockers
Targeted run (19 tests in the three BUG-085 classes): 18 passed, 1 failed (the spec-gap test).
Files written and re-read: ISongRepository.cs (both edits), SongRepository.cs (SQL consts + search methods), SongResolutionService.cs (fuzzy block, file tail), SimilarityConstants.cs (tail). Census `DerivePrefixToken|PrefixTokenMaxLen` in `*.cs`: no remaining references.

### Defect found and fixed during Green
The shared select constant had no trailing newline, so `...s.ArtistIdWHERE SongSearch` was one token and the SQL failed with `near "SongSearch"`. This also broke the existing global `SearchTitleCandidatesAsync`. Fixed by a trailing blank line in the raw literal (`TitleSearchSelectSql`). Lesson: raw-literal concatenation needs an explicit line break.

### AC traceability (partial)
| AC | Criterion | Implementation | Test | Result |
|----|-----------|----------------|------|--------|
| REQ-SFT-23 | typed title longer than stored finds it | `SongRepository.SearchTitleCoreAsync` (artist SQL) | `SongRepositoryArtistTrigramTests.SearchTitleCandidatesForArtistAsync_TypedTitleLongerThanStored_FindsStoredTitle` | pass |
| REQ-SFT-23 | same artist only | `TitleSearchForArtistSql` `s.ArtistId = @artistId` | `..._OtherArtistSameTitle_IsExcluded`, `..._RespectsArtistScopeAndTake` | pass |
| REQ-SFT-23 | typo in first word / middle, unrelated excluded | trigram OR-match | `..._TypoInFirstWord_...`, `..._TypoInMiddle_StillRetrieves` (2 cases), `..._UnrelatedTitle_ReturnsEmpty` | pass |
| REQ-SFT-23 | fuzzy pool uses whole title, 0.82 threshold, blank skipped | `SongResolutionService` fuzzy step | `SongResolutionServiceTests` (BUG-085 cases) | pass |
| REQ-SFT-23 | save returns SongChoiceNeeded with stored near-duplicate | end-to-end | `SongSaveTrigramDuplicateTests.SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate` | PASS after test correction `f64e70d2` (first run FAIL: test defect) |
| REQ-SFT-33 | no 3-char word falls back to artist-scoped collated prefix | `SearchTitleCoreAsync` fallback with artist filter | `..._ShortTitle_UsesCollatedPrefixFallback` | pass |

### Surprises for review
- `SongSearch` indexes two columns (`title`, `artist`), and `SongSearch MATCH @m` without a column filter also matches artist-name trigrams. The artist-scoped query inherits this (bounded by the `ArtistId` filter and the 0.82 scorer). A `title : @m` column filter would tighten it but changes the global query too, so it was not done.
- Performance of the OR-of-trigrams on large libraries was not measured. The artist filter is applied after the FTS join, so FTS does the heavy lifting.
- Empty table: FTS returns no rows, so the result is an empty list. The "no such table" catch is unchanged and shared.

---
## Cleanup Red: dead repository member, shared note text, stale test comments
**Plan:** `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-09-cleanup-after-the-song-form-work-dead-repository-member-shared-note-text-stale-comments/README.md`
**Status:** To Review (test side; Builder owns production)
**Branch:** `task/sft-cleanup-dead-member-shared-notes` (worktree `.worktrees/sft-cleanup`, base `develop`)
**Started:** 2026-11-09
**Completed:** 2026-11-09

### Changed files:
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` — removed the `GetFuzzyCandidatePoolAsync` delegate from `ThrowOnAddSongRepository` (compile-level Red)
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryArtistTrigramTests.cs` — two historical comments reworded to drop the removed member name (comment-only)
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryResolutionTests.cs` — one historical comment reworded (comment-only)
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` — stale BUG-021 comment corrected (comment-only)
- `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` — stale `ExecuteEditSaveAsync` comment corrected (comment-only)
- `Docs/Management/BusinessFeatures/artists-songs/task-log.md` — this section

Commits: `0a3bb4a2` (Part 1, `--no-verify`), `dbc372f1` (Part 3, `--no-verify`).
`--no-verify` reason: the test project does not compile until the Builder removes `ISongRepository.GetFuzzyCandidatePoolAsync`; the pre-commit hook builds the test project. Allowed for compile-level Red per `testing.md`.

### Build notes
Build: FAILED as expected (1 error, 34 warnings). Command: `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj -nodeReuse:false -m:1 -p:UseSharedCompilation=false`
- Only error: `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs(156,53): error CS0535: 'SongServiceNewArtistUnitOfWorkTests.ThrowOnAddSongRepository' does not implement interface member 'ISongRepository.GetFuzzyCandidatePoolAsync(int, string, int, CancellationToken)'`
- The 34 warnings are existing nullable/NU warnings (CS8625, CS8765 in `CrudListViewModelBaseTests.cs`, NU1902/NU1903 advisories). None are caused by this change.
- Tests: not run (project does not compile). Expected to pass once the Builder deletes the interface member and the `SongRepository` implementation.
Files written and re-read: all five edited `.cs` files re-read after edit; census re-run shows no `GetFuzzyCandidatePoolAsync` or `ExecuteEditSaveAsync` in `MyVocaList.Tests/`.

### Census: `GetFuzzyCandidatePoolAsync` (Grep tool and `rtk proxy grep -rn`, both agree)
Production (Builder deletes):
- `Domain/RepositoryInterface/ISongRepository.cs:56` — interface declaration
- `Infra/Repository/SongRepository.cs:126` — implementation
- No production caller remains (`Services/`, `UI/`, `MyVocaList/` are clean).

Tests (this change):
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs:184-185` — delegate in `ThrowOnAddSongRepository`. Removed.
- No Moq `Setup(...GetFuzzyCandidatePoolAsync...)` exists in the test tree. No repository test pins the member's behaviour (the old prefix-pool tests were already rewritten as `SearchTitleCandidatesForArtistAsync` tests under REQ-SFT-23). Nothing needed a stop.
- Comment-only mentions, reworded: `SongRepositoryArtistTrigramTests.cs:73`, `:96`, `SongRepositoryResolutionTests.cs:104`.

Docs (not touched by this task, listed for the Builder): `design.md:734` says the member is "unused and queued for removal"; `task-log.md:488` and the BUG-085 README mention it historically. Shipped change folders and `song-import-resolution/` docs are historical and are not edited.

### Part 2: shared note text (findings, no test edits needed)
Literals asserted in tests (tests keep their own private consts; they do not read the VM consts):
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs:55` `OfflineNoteText` = "You're offline — showing songs from your library only."
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs:56` `UnavailableNoteText` = "Online search isn't available right now — showing your library only."
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs:258` `ArtistOfflineNote` = "You're offline — showing artists from your library only."
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs:259` `UnavailableNote` = "Online search isn't available right now — showing your library only."

Assertions on the unavailable text (must keep passing after the Builder moves the note to one shared constant):
- SongFormViewModelTests.cs:202, 239, 273, 277, 346 (`UnavailableNoteText`)
- ArtistFormViewModelTests.cs:474, 513 (`UnavailableNote`)

Assertions on the offline notes (kept separate by the Builder):
- SongFormViewModelTests.cs:214, 224, 254, 262, 296, 331 (`OfflineNoteText`)
- ArtistFormViewModelTests.cs:462, 484, 503 (`ArtistOfflineNote`)

Reflection: none reads `OfflineNote` / `UnavailableNote` from the VMs. The only reflection in the test tree is `AppServicesRegistrationTests.cs:107` reading `_httpClient` (unrelated) and `UnitOfWorkMocks.cs:54` reading `Mock.Object`.

Verdict: no test change is needed for Part 2. The literals stay as literals (the contract). The Builder may consolidate the unavailable note in production; the test-side literals must stay byte-identical (em dash U+2014, straight apostrophe in "You're").

### Part 3: stale comments
(a) `AppServicesRegistrationTests.cs:45`. Verified: the `SongFormViewModel` constructor (`UI/ViewModels/SongFormViewModel.cs:137`) takes no `ISongResolutionService`. The test still resolves `ISongResolutionService` from DI, which needs `ISimilarityScorer` (registered at `Extensions/ServiceCollectionExtensions.cs:53`). Comment rewritten to say that.
(b) `SongServiceUpdateIntegrationTests.cs:51`. The comment cited `SongFormViewModel.ExecuteEditSaveAsync`, which no longer exists; the form saves via `SongService.SaveFromFormAsync` (`SongFormViewModel.cs:664`). Comment rewritten; the test still calls `UpdateSongAsync` directly, and the comment says so.

Scan of the SFT test files for other stale names (`CreateSongWithUrlsAsync(int`, `IsExactMatch`, `GetFuzzyCandidatePoolAsync`, `DerivePrefixToken`, `HasManualEdits`, `5.3b`, `ExecuteEditSaveAsync`):
- `GetFuzzyCandidatePoolAsync` in comments: 3 hits, fixed.
- `ExecuteEditSaveAsync`: 1 hit, fixed.
- `CreateSongWithUrlsAsync(int`, `IsExactMatch`, `DerivePrefixToken`, `5.3b`: no hits.
- `HasManualEdits`: hits in `Integration/Migrations/SongMigrationTests.cs` only. These are correct: the M1 migration test asserts the column is dropped (REQ-SFT-37). Not stale; not changed.

### Design concern
`MyVocaList.Tests/Architecture/UnitOfWorkReadScopeTests.cs:73` lists `ISongResolutionService` by name. Not touched (outside this task); the Builder should confirm that list still matches production after the cleanup.

### Builder instructions
1. Delete `ISongRepository.GetFuzzyCandidatePoolAsync` (`Domain/RepositoryInterface/ISongRepository.cs:56`) and `SongRepository.GetFuzzyCandidatePoolAsync` (`Infra/Repository/SongRepository.cs:126`), along with any SQL constant used only by it. Also remove its XML remark in `ISongRepository.cs`.
2. Re-run the test-project build. It should compile with 0 errors. Then run `dotnet test` (all tests must pass).
3. Shared note text: move the unavailable note to one shared constant in production. Keep the two offline notes separate. Do not change the test literals listed under Part 2.
4. No test file edits are needed for Part 2.

## Cleanup Green: remove dead repository member, share unavailable note, verify stale comments
**Plan:** `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-09-cleanup-after-the-song-form-work-dead-repository-member-shared-note-text-stale-comments/README.md`
**Status:** To Review
**Branch:** `task/sft-cleanup-dead-member-shared-notes` (worktree `.worktrees/sft-cleanup`)
**Started:** 2026-11-09
**Completed:** 2026-11-09
**Level:** C (pure refactor, no behaviour change; no new ACs). Existing tests from Red (`0a3bb4a2`, `dbc372f1`, `3a606de9`) are the regression net and were not edited.

### Changed files:
- `Domain/RepositoryInterface/ISongRepository.cs` — removed `GetFuzzyCandidatePoolAsync` declaration and its XML doc block (19 lines, no other member touched)
- `Infra/Repository/SongRepository.cs` — removed `GetFuzzyCandidatePoolAsync` implementation (15 lines; inline query, no SQL constant or using existed solely for it)
- `MyVocaList/UI/Constants/SearchNotes.cs` — new file, `public static class SearchNotes` with `const string Unavailable` (byte-identical text to the former private consts)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — private `UnavailableNote` removed; mapping uses `SearchNotes.Unavailable`; `using MyVocaList.UI.Constants;` added. `OfflineNote` kept (songs).
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` — private `UnavailableNote` removed; mapping uses `SearchNotes.Unavailable`; `using` added; comment above `OfflineNote` reworded (offline wording stays local to artists). `OfflineNote` kept.
- `Docs/Management/BusinessFeatures/artists-songs/task-log.md` — this section (Checkpoint block replaced)

No test file, DI registration, csproj, SongFormPage*, ArtistFormPage*, AutocompleteDXField*, tasks.md, LEDGER or change README was touched.

### Stale-comment audit (production `*.cs`, Grep tool; `rtk proxy grep` returned no output for this run, so the Grep tool is the census of record)
- `GetFuzzyCandidatePoolAsync`, `DerivePrefixToken`, `PrefixTokenMaxLen`, `IsExactMatch`, `5.3b`: no hits in production `.cs`.
- `CreateSongWithUrlsAsync(int`: no hit. The only hit is `Services/SongService.cs:260` (`CreateSongWithUrlsAsync(int?, string?, …)`), which names the **current** `int?` overload (`SongService.cs:248`). Accurate; **left unchanged**.
- Verdict: no production comment needed a fix beyond the `ArtistFormViewModel` wording above.

### Shared constant: location decision
`MyVocaList/UI/Constants/SearchNotes.cs` (namespace `MyVocaList.UI.Constants`). Rationale: the text is user-facing UI copy, so it belongs in the UI layer, not `Domain/Constants/SearchConstants.cs` (domain thresholds) or `Contracts/AppConstants.cs`. The folder is not excluded by `MyVocaList.csproj` (the only compile excludes are `NovaPasta`, `Tmp`, `MauiProgram.cs`, `App.xaml.cs`, `UI\Components\AutocompleteField\**`), so the file is compiled by default globbing. No name collision with existing types.

### Census: `GetFuzzyCandidatePoolAsync` after the change
- Grep tool over the worktree (`*.cs`): zero hits in production or tests (only `Docs/` historical mentions remain, which are not edited, per the change README).
- Build and test confirm no remaining reference.

### Verification evidence
- (a) `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj -nodeReuse:false -m:1 -p:UseSharedCompilation=false`: 7 projects, **0 errors**, 31 warnings (NU/vulnerability advisories and pre-existing nullable warnings; none introduced by this change).
- (b) `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-build`: **921 passed, 0 failed, 0 skipped** (exit 0). No known flakes triggered.
- (c) `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore -v q -nodeReuse:false -m:1 -p:UseSharedCompilation=false`: **6 projects, 0 errors**, 27 warnings; APK produced (`MyVocaList/bin/Debug/net10.0-android/com.myvocalist.apk`).
- Literal check: `rtk proxy grep -rnF` confirms the unavailable note appears once in production (`SearchNotes.cs:9`) and the tests keep their own literals (`SongFormViewModelTests.cs:56`, `ArtistFormViewModelTests.cs:259`), matching byte-for-byte. Offline notes remain separate (`SongFormViewModel.cs:85`, `ArtistFormViewModel.cs:38`).
- Files written and re-read: `ArtistFormViewModel.cs:36-38` (comment edit), `task-log.md` (checkpoint replaced).

### Architecture check (read-only)
`MyVocaList.Tests/Architecture/UnitOfWorkReadScopeTests.cs:68-75` lists `ISongResolutionService` in `GovernedDataServiceTypes`. `Domain/ServicesInterfaces/ISongResolutionService.cs:9` still declares that interface, so the list matches production. No change needed. (The full test run passed, including this test.)

### Branch base note (not a blocker for this refactor)
`develop` is 2 commits ahead of this branch's base (`9f9907cd`): `dde298d7` (docs(acf) T10 merged) and `814c0630` (fix(acf) T10 root-cause fixes). `git merge-base --is-ancestor develop HEAD` returns non-zero. Not rebased; the orchestrator should decide rebase vs. merge before merging this branch. Potential overlap: `814c0630` is an ACF/autocomplete fix that may touch files in `UI/Components/`, not the files here.

### Design concern
None. The shared constant is placed in the UI layer per the rationale above.
Context manifest: Domain/RepositoryInterface/ISongRepository.cs · Infra/Repository/SongRepository.cs · MyVocaList/UI/Constants/SearchNotes.cs · MyVocaList/UI/ViewModels/SongFormViewModel.cs · MyVocaList/UI/ViewModels/ArtistFormViewModel.cs

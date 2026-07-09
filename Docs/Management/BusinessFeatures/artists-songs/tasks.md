# Artists & Songs — Implementation Tasks

> **Status:** Phase 16B complete; Phase 16C (emulator smoke test) pending Helder gate
> **Last updated:** 2026-06-20
> **Spec:** `Docs/specs/artists-songs/requirements.md` + `design.md`

Check off each task as it completes. Run `/sln-build` after every task. Run `/sln-review`
after every major task before committing.

---

## Phase 1 — Domain & Contracts ✅

- [x] **1.1** `Artist` entity
- [x] **1.2** `Song` entity
- [x] **1.3** `IArtistRepository`
- [x] **1.4** `ISongRepository`
- [x] **1.5** `ArtistListItemDto`, `SongListItemDto`, `MusicSearchResultDto`
- [x] **1.6** `IArtistService`
- [x] **1.7** `ISongService`
- [x] **1.8** Build — 0 errors

---

## Phase 2 — Tests ✅ (148 tests passing — pre-revision baseline)

- [x] **2.1** `ArtistRepositoryTests`
- [x] **2.2** `SongRepositoryTests`
- [x] **2.3** `ArtistServiceTests`
- [x] **2.4** `SongServiceTests`
- [x] **2.5** `dotnet test` — 148 tests Green

---

## Phase 3 — Infrastructure ✅

- [x] **3.1** `ArtistConfiguration`
- [x] **3.2** `SongConfiguration`
- [x] **3.3** `AppDbContext` registrations
- [x] **3.4** Migration `AddArtistAndSongCatalog`
- [x] **3.5** `ArtistRepository`
- [x] **3.6** `SongRepository`
- [x] **3.7** Build — 0 errors
- [x] **3.8** `dotnet test` Green

---

## Phase 4 — Services ✅

- [x] **4.1–4.9** All services and providers implemented; 148 tests passing

---

## Phase 5 — DI Registration ✅

- [x] **5.1–5.8** All DI, routes, AppShell registration complete

---

## Phase 6 — Artists UI ✅

- [x] **6.1–6.7** ArtistsPage, ArtistFormPage, ViewModels complete

---

## Phase 7 — Songs UI ✅

- [x] **7.1–7.7** SongsPage, SongFormPage, ViewModels complete

---

## Phase 8 — SUPERSEDED

> Phase 8 smoke test is superseded by the architectural revision (Phases 9–14).
> The original implementation is the baseline for refactoring.

- [ ] ~~8.1–8.5~~ Skipped — replaced by Phase 14 final gate

---

## Phase 9 — Spec Revision ✅

- [x] **9.1** Update `requirements.md` — Catalog model, independent songs, Lyrics, navigation revision, new user stories
- [x] **9.2** Update `design.md` — Catalog entity, nullable ArtistId, ILyricsProvider, dual-mode SongsPage, AppShell, DI
- [x] **9.3** Update `tasks.md` — add phases 10–14; mark 1–7 complete; supersede phase 8
- [x] **9.4** Update plan file at `Docs/superpowers/plans/validated-noodling-island.md`

---

## Phase 10 — Domain Refactor [SEQUENTIAL]

- [x] **10.1** Update `Song` entity — keep `ArtistId` as `int` NOT NULL (mandatory); rename nav prop `Artist` → `OriginalArtist`; add `Lyrics string?`; add `CatalogEntries` nav
  - **Files owned:** `Domain/Entity/Song.cs`
- [x] **10.2** Add `Catalog` join entity
  - **Files owned:** `Domain/Entity/Catalog.cs`
- [x] **10.3** Update `Artist` entity — rename nav prop `Songs` → `OriginalSongs`; add `CatalogEntries` nav
  - **Files owned:** `Domain/Entity/Artist.cs`
- [x] **10.4** Update `ISongRepository` — replace `GetPagedByArtistAsync` with `GetPagedAsync` (global, no artist filter); keep `ExistsByTitleForArtistAsync` (per-artist uniqueness); remove artist-scoped methods (`CountByArtistAsync`, `CountByArtistsAsync`, `SearchByTitleAsync`)
  - **Files owned:** `Domain/RepositoryInterface/ISongRepository.cs`
- [x] **10.5** Add `ICatalogRepository` interface
  - **Files owned:** `Domain/RepositoryInterface/ICatalogRepository.cs`
- [x] **10.6** Update `ISongService` — KEEP `artistId` in `CreateSongAsync` (mandatory); add `lyrics` param to Create and Update; make `GetPagedSongsForListAsync` global (remove `artistId` param)
  - **Files owned:** `Domain/ServicesInterfaces/ISongService.cs`
- [x] **10.7** Add `ICatalogService` interface
  - **Files owned:** `Domain/ServicesInterfaces/ICatalogService.cs`
- [x] **10.8** Update `SongListItemDto` — replace `ArtistId`/`ArtistName` with `OriginalArtistId` (int, NOT nullable — ArtistId is mandatory) / `OriginalArtistName?`; remove `Lyrics` from DTO (belongs in form only)
  - **Files owned:** `Contracts/DTOs/List/SongListItemDto.cs`
- [x] **10.9** Update `ArtistListItemDto` — rename `SongCount` → `CatalogCount`; update `SongCountText` → `CatalogCountText`
  - **Files owned:** `Contracts/DTOs/List/ArtistListItemDto.cs`
- [x] **10.10** Build — 0 errors (expect failures in Infra/Services/MAUI — that is expected at this stage)

---

## Phase 11 — Infrastructure [SEQUENTIAL — after Phase 10]

- [x] **11.1** Add `CatalogConfiguration` (`IEntityTypeConfiguration<Catalog>`)
  - **Files owned:** `MyVocaList.Infra/EntityEFConfig/CatalogConfiguration.cs`
- [x] **11.2** Update `SongConfiguration` — ArtistId nullable FK with `SetNull`; add Lyrics column; remove old unique index on (ArtistId, Title); add global unique index on Title
  - **Files owned:** `MyVocaList.Infra/EntityEFConfig/SongConfiguration.cs`
- [x] **11.3** Update `ArtistConfiguration` — update nav property names
  - **Files owned:** `MyVocaList.Infra/EntityEFConfig/ArtistConfiguration.cs`
- [x] **11.4** Register `Catalog` DbSet and `CatalogConfiguration` in `AppDbContext`
  - **Files owned:** `MyVocaList.Infra/AppDbContext.cs`
- [x] **11.5** Add EF Core migration `RefactorCatalogAndAddLyrics`:
  - `Up()`: `DELETE FROM Songs; DELETE FROM Artists;` (raw SQL — app not in production)
  - Keep `Songs.ArtistId` NOT NULL (mandatory)
  - Add `Songs.Lyrics TEXT NULL`
  - Keep `IX_Songs_ArtistId_Title` composite unique index (per-artist title uniqueness)
  - Create `Catalog` table with composite PK `(ArtistId, SongId)`, cascading FKs
  - `Down()`: inverse steps
  - **Files owned:** new migration file + `AppDbContext` snapshot
- [x] **11.6** Update `SongRepository` — implement updated `ISongRepository` (global paged, global title uniqueness)
  - **Files owned:** `MyVocaList.Infra/Repository/SongRepository.cs`
- [x] **11.7** Implement `CatalogRepository`
  - **Files owned:** `MyVocaList.Infra/Repository/CatalogRepository.cs`
- [x] **11.8** Build — 0 errors
- [x] **11.9** `dotnet test` — fix any broken repository tests

---

## Phase 12 — Services [SEQUENTIAL — after Phase 11]

- [x] **12.1** Update `SongService` — keep `artistId` in `CreateSongAsync` (mandatory); add `lyrics` param; update paged list to global (no artist filter)
  - **Files owned:** `MyVocaList.Services/SongService.cs`
- [x] **12.2** Implement `CatalogService`
  - **Files owned:** `MyVocaList.Services/CatalogService.cs`
- [x] **12.3** Add `ILyricsProvider` placeholder interface (no implementation class)
  - **Files owned:** `MyVocaList.Services/ILyricsProvider.cs`
- [x] **12.4** Update `ArtistService.GetDeleteConfirmationAsync` — use `ICatalogRepository.CountByArtistAsync` instead of `ISongRepository.CountByArtistAsync`
  - **Files owned:** `MyVocaList.Services/ArtistService.cs`
- [x] **12.5** Build — 0 errors
- [x] **12.6** `dotnet test` — fix any broken service tests (141 passing)

---

## Phase 13 — DI Registration [SEQUENTIAL — after Phase 12]

- [x] **13.1** Register `CatalogService` as `AddScoped<ICatalogService, CatalogService>` in `MauiProgram.cs`
- [x] **13.2** Register `CatalogRepository` as `AddScoped<ICatalogRepository, CatalogRepository>`
- [x] **13.3** Add `Songs` as top-level `FlyoutItem` in `AppShell.xaml`; add `songs` namespace import
- [x] **13.4** Update `AppShell.xaml.cs` — remove `Routes.Songs` from `Routing.RegisterRoute` (now a FlyoutItem root)
- [x] **13.5** Update `NavigationConfig.cs` — add Catalog menu group; add `Routes.Songs` to PageTypes
- [x] **13.6** `Routes.Songs` already exists — verified consistent
- [x] **13.7** Build — 0 errors; 141 tests passing

---

## Phase 14 — UI Refactor [SEQUENTIAL — after Phase 13]

- [x] **14.1** Update `ArtistsPage.xaml` — revise `TrailingContent` to `HorizontalStackLayout` with catalog icon button (`queue_music_outlined`) + `CheckEdit`; remove any tap-to-navigate logic from template
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`
- [x] **14.2** Update `ArtistsPage.xaml.cs` — remove or empty `OnItemTapped` navigation logic; add `ViewCatalogCommand` binding wiring if needed
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs`
- [x] **14.3** Update `ArtistsViewModel` — add `ViewCatalogCommand(ArtistListItemDto)` → navigate to `Routes.Songs?artistId=…&artistName=…`; add `ArtistRoleFilter` + `Mode` query param; remove `TapArtistCommand`
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`
- [x] **14.4** Update `SongsViewModel` — make `ArtistId` optional (0 = global mode); add `IsCatalogMode`; route data loading through `ICatalogService` (catalog mode) or `ISongService` (global mode); add `AddToCatalogCommand`, `RemoveFromCatalogCommand`
  - **Files owned:** `MyVocaList/UI/ViewModels/SongsViewModel.cs`
- [x] **14.5** Update `SongsPage.xaml` — update `AppBarTitle` binding to support dual-mode title; update FAB command binding to `IsCatalogMode ? AddToCatalogCommand : AddSongCommand`
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`
- [x] **14.6** Update `SongsPage.xaml.cs` — remove artist-required guard (`if (ArtistId == 0) return`)
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs`
- [x] **14.7** Update `SongFormPage.xaml` — replace read-only artist label with artist autocomplete field (TextEdit + suggestion dropdown); add `Lyrics` multi-line editor field; autocomplete disabled when `IsArtistLocked`
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- [x] **14.8** Update `SongFormViewModel` — add artist autocomplete state (`ArtistSearchText`, `SelectedArtistId`, `ArtistSuggestions`, `IsArtistLocked`); add `Lyrics` observable property; update `SaveCommand` to pass `artistId` (mandatory)
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- [x] **14.9** Build — 0 errors
- [x] **14.10** `dotnet test` — 141 passing

---

## Phase 15 — Tests Update [after Phase 14]

- [x] **15.1** Update `SongServiceTests` — remove `artistId` from create calls; update title uniqueness tests (global, not per-artist)
- [x] **15.2** Update `SongRepositoryTests` — replace artist-scoped paged tests with global paged tests; add Catalog join tests
- [x] **15.3** Add `CatalogRepositoryTests` — add/remove/exists/paged-by-artist
- [x] **15.4** Add `CatalogServiceTests` — add duplicate, remove not-found, paged list
- [x] **15.5** `dotnet test` — all pass (Green) — 157 tests, 0 failures

---

## Phase 16A — Navigation + Filter UI + Quick Fixes [SEQUENTIAL]

- [x] **16A.1** Fix `AppShellViewModel.NavigateAsync` — split route at `?`, look up base route in `PageTypes`, set query params on ViewModel after `PushAsync`
  - **Files owned:** `MyVocaList/UI/ViewModels/AppShellViewModel.cs`
- [x] **16A.2** Simplify menu — replace "Authors"/"Performers" entries with single "Artists" entry (no query param)
  - **Files owned:** `MyVocaList/Navigation/NavigationConfig.cs`
- [x] **16A.3** Add `dxe:FilterChipGroup` to ArtistsPage — Items: "Authors", "Performers"; `SelectedItems` bound to ViewModel; mapping logic: both/neither → All, one → respective filter
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`, `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`
- [x] **16A.4** Fix broken empty state icon `person_outlined` → `group_outlined` (same file as 16A.3)
- [x] **16A.5** Add `Shell.BackButtonBehavior IsVisible="False" IsEnabled="False"` to SongsPage (same fix as VenuesPage/ArtistsPage)
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`
- [x] **16A.6** `dotnet build` — 0 errors; `dotnet test` — all passing (157 tests)
- [x] **16A.7** Update `.claude/library/devexpress-patterns.md` and `.claude/library/m3-components.md` with FilterChipGroup + MD3 Filter Chip patterns

---

## Phase 16B — API Search Strip on Forms [SEQUENTIAL — after 16A]

> Services already exist and are DI-registered: `IMusicMetadataService`, `MusicBrainzProvider`, `DeezerProvider` (Phase 4, commit 53fcfb7). Only UI wiring is missing.

- [x] **16B.1** Update `ArtistFormViewModel` — inject `IMusicMetadataService`; add `ApiSearchText`, `ApiResults`, `IsApiSearching`, `ApiStatusMessage`; add `SearchApiCommand`, `SelectApiResultCommand`; add duplicate detection state (`DuplicateSuggestions`, `SelectDuplicateCommand`)
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`
- [x] **16B.2** Update `ArtistFormPage.xaml` — add duplicate suggestions field + API search strip (TextEdit + "Search" button + status label + results list) below Name field
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`
- [x] **16B.3** Update `SongFormViewModel` — inject `IMusicMetadataService`; add API state + `SearchApiCommand`; `SelectApiResultCommand` populates Title/FeaturedArtists and locks Artist field on match
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- [x] **16B.4** Update `SongFormPage.xaml` — add API search strip below Title field
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- [x] **16B.5** `dotnet build` — 0 errors; `dotnet test` — all passing

---

## Phase 16C — Final Gate [after Phase 16B]

- [~] **16C.1** End-to-end smoke test on emulator:
  - Single Artists menu item navigates to ArtistsPage; filter chips work
  - Register artist; verify no songs required
  - Register song from global Songs page; verify title uniqueness
  - API search strip works on ArtistFormPage and SongFormPage
  - Add song to artist's Catalog via trailing button → Catalog page → FAB picker
  - Remove song from Catalog; verify song still exists in global list
  - Delete artist; verify songs not deleted; Catalog entries gone
  - Delete song; verify it disappears from all Catalogs
  - Edit song; verify Lyrics field visible and saveable
  - Verify Songs menu item in flyout; search back arrow shows correctly
  - **❌ FAILED 2026-07-03 (Helder emulator run) — full steps + evidence: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001.** Blocking finding: SongFormPage Artist field has no working required-field validation and no autocomplete dropdown; typing a non-matching artist name and blurring clears the entry instead of offering "create new artist" — **song registration is impossible in the current build (BUG-027, Critical)**. This blocks TEST-002 (BUG-023 BottomSheet) and TEST-003 (BUG-024 edit data integrity) from being exercised at all. Additional findings this run: ArtistsPage trailing `queue_music_outlined` button is still a no-op (BUG-028, regression of BUG-015/BUG-019 fix); ArtistFormPage leading search icon crashes the app (BUG-029, Critical); ArtistFormPage search strip UX is confusing/possibly duplicated with SongFormPage search and opens an Artist search when Song search may be intended (BUG-030 — spec-gap, needs Helder clarification); no 3rd-party autocomplete appears while typing in either Artist Name or Song Title fields, unclear if this is a missing AC or was never in scope (BUG-031/BUG-032 — spec-gap). See BACKLOG.md Artists & Songs Catalog row for the full bug list. **Do not re-run 16C.1 until BUG-027 is fixed** (it is the hard blocker for the rest of the checklist).
- [ ] **16C.2** Build — 0 errors
- [ ] **16C.3** Run `/sln-review`
- [ ] **16C.4** Update `Docs/Changelog/changelog.md`
- [ ] **16C.5** Run `/sln-commit`

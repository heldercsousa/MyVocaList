# Artists & Songs — Implementation Tasks

> **Status:** Ready for implementation
> **Last updated:** 2026-04-12
> **Spec:** `Docs/specs/artists-songs/requirements.md` + `design.md`

Check off each task as it completes. Run `/project:build` after every task. Run `/project:review` after every major task before committing.

---

## Phase 1 — Domain & Contracts

- [ ] **1.1** Add `Artist` entity to `Domain/Entity/Artist.cs`
- [ ] **1.2** Add `Song` entity to `Domain/Entity/Song.cs`
- [ ] **1.3** Add `IArtistRepository` to `Domain/RepositoryInterface/IArtistRepository.cs`
- [ ] **1.4** Add `ISongRepository` to `Domain/RepositoryInterface/ISongRepository.cs`
- [ ] **1.5** Add `ArtistListItemDto`, `SongListItemDto`, `MusicSearchResultDto` to `MyVocaList.Contracts`
- [ ] **1.6** Build — 0 errors

---

## Phase 2 — Infrastructure

- [ ] **2.1** Add `ArtistConfiguration` (`IEntityTypeConfiguration<Artist>`) in `MyVocaList.Infra`
- [ ] **2.2** Add `SongConfiguration` (`IEntityTypeConfiguration<Song>`) in `MyVocaList.Infra`
- [ ] **2.3** Register `Artist` and `Song` `DbSet`s in `AppDbContext`; register configurations
- [ ] **2.4** Add EF Core migration: `AddArtistAndSongCatalog`
- [ ] **2.5** Implement `ArtistRepository` — CRUD + paged search + name suggestions + `GetByExternalIdAsync`
- [ ] **2.6** Implement `SongRepository` — CRUD + paged search by artist + title suggestions + `GetByExternalIdAsync` + `CountByArtistAsync`
- [ ] **2.7** Build — 0 errors

---

## Phase 3 — Services

- [ ] **3.1** Add `IMusicMetadataProvider` interface to `MyVocaList.Services`
- [ ] **3.2** Implement `MusicBrainzProvider` — `SearchArtistsAsync`, `SearchSongsAsync`; respect 1 req/sec; set `User-Agent`
- [ ] **3.3** Implement `DeezerProvider` — `SearchArtistsAsync`, `SearchSongsAsync`
- [ ] **3.4** Add `IMusicMetadataService` interface
- [ ] **3.5** Implement `MusicMetadataService` — provider chain orchestration; MusicBrainz first, Deezer fallback
- [ ] **3.6** Add `IArtistService` interface to `Domain/ServicesInterfaces/IArtistService.cs`
- [ ] **3.7** Implement `ArtistService` — validate, create, update, delete, paged list, name suggestions, delete confirmation message
- [ ] **3.8** Add `ISongService` interface to `Domain/ServicesInterfaces/ISongService.cs`
- [ ] **3.9** Implement `SongService` — validate, create, update, delete, paged list by artist, title suggestions
- [ ] **3.10** Build — 0 errors

---

## Phase 4 — Tests

- [ ] **4.1** `ArtistServiceTests` — name validation, create (valid / duplicate / too long), update, delete (with songs / without)
- [ ] **4.2** `SongServiceTests` — title validation, create (valid / duplicate title for artist / missing artist), update, delete
- [ ] **4.3** `ArtistRepositoryTests` — CRUD, paged search, case-insensitive search, unique name constraint, external ID lookup
- [ ] **4.4** `SongRepositoryTests` — CRUD, paged search by artist, case-insensitive title search, composite unique constraint (artistId + title), external ID lookup
- [ ] **4.5** `dotnet test` — all pass

---

## Phase 5 — DI Registration

- [ ] **5.1** Register `ArtistService`, `SongService`, `MusicMetadataService` as `AddScoped` in `MauiProgram.cs`
- [ ] **5.2** Register `MusicBrainzProvider`, `DeezerProvider` as `AddScoped<IMusicMetadataProvider>` (order: MusicBrainz first)
- [ ] **5.3** Register `HttpClient` for each provider via `AddHttpClient<T>` with base address and `User-Agent`
- [ ] **5.4** Register `ArtistRepository`, `SongRepository` as `AddScoped`
- [ ] **5.5** Register all pages and ViewModels as `AddTransient`
- [ ] **5.6** Add routes `ArtistForm`, `Songs`, `SongForm` to `Routes.cs`
- [ ] **5.7** Register routes in `AppShell.xaml.cs`
- [ ] **5.8** Build — 0 errors

---

## Phase 6 — Artists UI

- [ ] **6.1** Complete `ArtistsPage.xaml` — `Shell.TitleView`, `DXCollectionView`, `ListItem` rows, `FloatingToolbar` + FAB, two `EmptyState` components, confirm `dx:BottomSheet`; build after
- [ ] **6.2** Complete `ArtistsPage.xaml.cs` — all code-behind event handlers per design; build after
- [ ] **6.3** Implement `ArtistsViewModel` — all properties, commands, selection logic, confirm sheet state; build after
- [ ] **6.4** Implement `ArtistFormPage.xaml` — Name field + character counter + suggestion list + API search strip + API results + overwrite warning `dx:BottomSheet` + action buttons; build after
- [ ] **6.5** Implement `ArtistFormPage.xaml.cs` — `OnAppearing` focus, `OnBackButtonPressed`, overwrite sheet state sync; build after
- [ ] **6.6** Implement `ArtistFormViewModel` — all properties, commands, `HasManualEdits` tracking, API search + import flow, local dedup suggestions; build after
- [ ] **6.7** Run `/project:review` — 0 issues before proceeding

---

## Phase 7 — Songs UI

- [ ] **7.1** Implement `SongsPage.xaml` — mirrors Artists page; artist name in `SmallAppBar` title; song rows with `FeaturedArtists` supporting text; build after
- [ ] **7.2** Implement `SongsPage.xaml.cs`; build after
- [ ] **7.3** Implement `SongsViewModel` — receives `ArtistId` + `ArtistName` via query; same structure as `ArtistsViewModel`; build after
- [ ] **7.4** Implement `SongFormPage.xaml` — artist read-only label + Title field + FeaturedArtists field + API search strip + action buttons; build after
- [ ] **7.5** Implement `SongFormPage.xaml.cs`; build after
- [ ] **7.6** Implement `SongFormViewModel` — same structure as `ArtistFormViewModel`; API search scoped to artist hint; build after
- [ ] **7.7** Run `/project:review` — 0 issues before committing

---

## Phase 8 — Final

- [ ] **8.1** End-to-end smoke test on emulator:
  - Register an artist manually
  - Register an artist via MusicBrainz API import
  - Register an artist via Deezer fallback (simulate MusicBrainz failure)
  - Edit an artist; verify `HasManualEdits` warning fires on next API import
  - Delete an artist with songs; verify cascade + confirmation message
  - Register a song manually under an artist
  - Register a song via API import
  - Search artists and songs; verify case- and accent-insensitive results
- [ ] **8.2** Run `/project:build` — 0 errors
- [ ] **8.3** Run `/project:review` — 0 issues
- [ ] **8.4** Update `Docs/Changelog/changelog.md`
- [ ] **8.5** Run `/project:commit`

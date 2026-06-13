# Song Import & Entity Resolution — Tasks

> Source of truth for sequencing. Markers: `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`.
> Full step detail + code in `plan.md`. Read `requirements.md` + `design.md` first. DRY Onion order; worktrees mandatory for any 2+ parallel wave.

## Wave 0 — Spike
- [x] **0.1 [SPIKE] Validate fuzzy library on net10.0-android** — DONE 2026-06-13: FuzzySharp 2.0.2, Android-safe, threshold 0.82. See `findings.md`.

## Wave 1 — Domain `[P]`
- [ ] **1.1 Add Song.Version** — Produces: `Domain/Entity/Song.cs` change. Consumes: nothing.
- [ ] **1.2 Resolution contracts** — Produces: `Domain/Resolution/*` (records+enums). Consumes: Song.
- [ ] **1.3 Service + scorer interfaces** — Produces: `ISongResolutionService`, `IArtistResolutionService`, `ISimilarityScorer`; `ISongService`/`ISongRepository`/`IArtistRepository` additions. Consumes: 1.2.

## Wave 2 — Infra `[SEQUENTIAL]` (waits W1)
- [ ] **2.1 SongConfiguration: Version + 3-col unique index** — Files: `SongConfiguration.cs`. Hotspot-adjacent.
- [ ] **2.2 Migration AddSongVersion** — Files: `*_AddSongVersion.cs`, `AppDbContext` snapshot. SEQUENTIAL (migration hotspot). AC-5.5 test.
- [ ] **2.3 SongRepository query methods (TDD)** — Files: `SongRepository.cs` + integration tests.
- [ ] **2.4 ArtistRepository fuzzy pool (TDD)** — Files: `ArtistRepository.cs` + tests.
- [ ] **2.5 SimilarityScorer + constants** — Files: `Infra/Similarity/*`, `Infra.csproj` (library pinned by 0.1) + tests.

## Wave 3 — Services `[SEQUENTIAL]` (waits W2; TDD Level A)
- [ ] **3.1 ArtistResolutionService (TDD)** — Produces: `Services/ArtistResolutionService.cs` + tests. Consumes: 2.4, 2.5.
- [ ] **3.2 SongResolutionService (TDD)** — Produces: `Services/SongResolutionService.cs` + tests. Consumes: 3.1, 2.3, 2.5.
- [ ] **3.3 SongService.CreateSongWithUrlsAsync + update external-id (TDD)** — Files: `SongService.cs` + tests.

## Wave 4 — UI `[SEQUENTIAL]` (waits W3). Invoke `myvocalist-coding`. One XAML file at a time.
- [ ] **4.1 BottomSheetTitle style (BUG-004)** — Files: `MaterialStyles.xaml`.
- [ ] **4.2 SongPickerViewModel + page/DI fix (BUG-010, BUG-006)** — Files: `SongPickerViewModel.cs`, `SongPickerPage.xaml.cs`, `MauiProgram.cs` (hotspot) + tests.
- [ ] **4.3 Picker pages suppress Shell back chrome (BUG-007)** — Files: 4 picker `*.xaml`.
- [ ] **4.4 SongFormViewModel: save catch + buffered URLs + artist autocomplete (BUG-005/008/009)** — Files: `SongFormViewModel.cs`, `SongFormPage.xaml(.cs)` + tests.
- [ ] **4.5 Resolution + merge BottomSheets wiring** — Files: `SongFormPage.xaml(.cs)`, `SongFormViewModel.cs`.

## Wave 5 — Tests + gate `[SEQUENTIAL]`
- [ ] **5.1 Integration + AC traceability matrix** — full `dotnet test` green; matrix in `task-log.md`.
- [ ] **5.2 Emulator smoke (Helder gate)** — record in `task-log.md`; BACKLOG → ✅ Done on green.

## Hotspot / single-writer files (serialize)
`MauiProgram.cs` · `AppDbContext.cs` + snapshot · `*_AddSongVersion.cs` · `tasks.md` · picker `*.xaml` (edit one at a time).

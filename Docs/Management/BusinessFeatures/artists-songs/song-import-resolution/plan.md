# Song Import & Entity Resolution — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax. Read `requirements.md` and `design.md` in this folder before any task. Invoke `myvocalist-coding` skill before any UI/EF task.

**Goal:** Build a Services-layer engine that decides insert-vs-update for song/artist data (manual or 3rd-party API), supports deliberate version variants, surfaces fuzzy near-duplicates for confirmation, never silently overwrites manual edits, and persists external identity — plus fix the bugs blocking an end-to-end demo.

**Architecture:** Resolution logic lives in `ISongResolutionService` / `IArtistResolutionService` (Services). They return decision objects (`SongResolution`/`ArtistResolution`) the UI acts on via `dx:BottomSheet`. Exact matching uses DB-side `NOCASE_NOACCENT` collation; fuzzy matching runs an on-device `ISimilarityScorer` over a *bounded* collation-pre-filtered candidate pool (no full scans, no C#-side normalization).

**Tech Stack:** .NET MAUI 10, EF Core 10 + SQLite, DevExpress MAUI 25.2, CommunityToolkit.Mvvm, xUnit + Moq, on-device string-similarity library (TBD by Wave 0 spike — FuzzySharp or F23.StringSimilarity).

---

## File map

**Domain** (`MyVocaList.Domain`)
- Modify `Entity/Song.cs` — add `Version` (string, non-null).
- Create `Resolution/SongCandidate.cs`, `ArtistCandidate.cs`, `SongResolution.cs`, `ArtistResolution.cs`, `FieldDiff.cs`, `SongMatch.cs`, enums `ResolutionKind`, `ResolutionChoice`.
- Create `ServicesInterfaces/ISongResolutionService.cs`, `IArtistResolutionService.cs`, `ISimilarityScorer.cs`.
- Modify `ServicesInterfaces/ISongService.cs` — add `CreateSongWithUrlsAsync`, add external-id params to `UpdateSongAsync`.
- Modify `RepositoryInterface/ISongRepository.cs` — `ExistsByTitleVersionForArtistAsync` (+ excludeId overload), `GetFuzzyCandidatePoolAsync`.
- Modify `RepositoryInterface/IArtistRepository.cs` — `GetFuzzyCandidatePoolAsync`.

**Infra** (`MyVocaList.Infra`)
- Modify `EntityEFConfig/SongConfiguration.cs` — `Version` column + collation; replace unique index with `(ArtistId, Title, Version)`.
- Create migration `*_AddSongVersion.cs`.
- Modify `Repository/SongRepository.cs`, `Repository/ArtistRepository.cs` — new query methods.
- Create `Similarity/SimilarityScorer.cs` + `Similarity/SimilarityConstants.cs`.

**Services** (`MyVocaList.Services`)
- Create `ArtistResolutionService.cs`, `SongResolutionService.cs`.
- Modify `SongService.cs` — `CreateSongWithUrlsAsync`, external-id on update.

**UI** (`MyVocaList`)
- Create `UI/ViewModels/SongPickerViewModel.cs`; fix `UI/Pages/Songs/SongPickerPage.xaml.cs` injection.
- Modify `UI/Pages/Songs/SongFormPage.xaml(.cs)` + `UI/ViewModels/SongFormViewModel.cs` — resolution + merge sheets, artist autocomplete (BUG-008), buffered URLs (BUG-009), save catch (BUG-005).
- Modify picker pages — suppress Shell back chrome (BUG-007); concurrent-exec guard (BUG-006).
- Modify `Resources/Styles/MaterialStyles.xaml` — add `BottomSheetTitle` (BUG-004).
- Modify `MauiProgram.cs` — DI for new services, scorer, `SongPickerViewModel`.

**Tests** (`MyVocaList.Tests`)
- `Unit/Services/SongResolutionServiceTests.cs`, `ArtistResolutionServiceTests.cs`, `Similarity/SimilarityScorerTests.cs`.
- `Integration/Repositories/SongRepositoryResolutionTests.cs`.
- `Unit/ViewModels/SongPickerViewModelTests.cs`, `SongFormViewModelResolutionTests.cs`.

---

## Wave 0 — Spike: fuzzy library (throwaway)

### Task 0.1: Validate similarity library on net10.0-android
**Time-box:** 30 min hard stop. **Artifact:** `findings.md`. **Files owned:** throwaway only.

- [ ] Add FuzzySharp (and, if it fails Android restore, F23.StringSimilarity) to a scratch reference; `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`.
- [ ] Score accented samples: `("Björk","Biork")`, `("Não Sei","Nao Sei")`, `("Queen","Qween")`, identical strings → 1.0.
- [ ] **Success:** restores + builds on Android, no native dep, accent samples score ≥ 0.80. **Failure:** native dependency or Android restore error.
- [ ] Write `findings.md`: chosen library + package version + observed scores + recommended threshold. Remove scratch code.

> Main agent reads `findings.md` and pins the library/version into Task 2.5 before dispatching Wave 2.

---

## Wave 1 — Domain (no dependencies; `[P]` within wave)

### Task 1.1: Add `Song.Version`
**Files:** Modify `Domain/Entity/Song.cs`. Test: covered by Infra Task 2.2.
- [ ] Add `public string Version { get; set; } = string.Empty;` near `Title`. XML doc: "Variant label (Live/Acoustic/Remix); empty = canonical version."
- [ ] Build Domain. Commit.

### Task 1.2: Resolution contracts
**Files:** Create the records/enums in `Domain/Resolution/` exactly as in design.md §3.
- [ ] Create `ResolutionKind`, `ResolutionChoice` enums; `ArtistCandidate`, `SongCandidate`, `FieldDiff`, `SongMatch`, `ArtistResolution`, `SongResolution` records (signatures verbatim from design §3).
- [ ] Build Domain. Commit.

### Task 1.3: Service + scorer interfaces
**Files:** Create `ISimilarityScorer.cs`, `IArtistResolutionService.cs`, `ISongResolutionService.cs` (signatures from design §3). Add XML `<summary>` on every method (interface = doc source of truth).
- [ ] Modify `ISongService.cs`: add
  `Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(int artistId, string title, string version, string? featuredArtists, string? lyrics, string? externalId, string? externalProvider, IEnumerable<string> urls, CancellationToken ct = default);`
  and add `string? externalId = null, string? externalProvider = null` params to `UpdateSongAsync`.
- [ ] Modify `ISongRepository.cs`: add
  `Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, CancellationToken ct = default);`
  `Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, int excludeId, CancellationToken ct = default);`
  `Task<IReadOnlyList<Song>> GetFuzzyCandidatePoolAsync(int artistId, string titlePrefixToken, int take, CancellationToken ct = default);`
- [ ] Modify `IArtistRepository.cs`: add
  `Task<IReadOnlyList<Artist>> GetFuzzyCandidatePoolAsync(string namePrefixToken, int take, CancellationToken ct = default);`
- [ ] Build Domain. Commit.

---

## Wave 2 — Infra (`SEQUENTIAL` — waits Wave 1)

### Task 2.1: SongConfiguration — Version column + 3-col unique index
**Files:** Modify `Infra/EntityEFConfig/SongConfiguration.cs`.
- [ ] Configure `Version`: `.HasMaxLength(60).IsRequired().UseCollation(CollationConstants.Default)` (M1).
- [ ] Remove unique index on `(ArtistId, Title)`; add unique index `(ArtistId, Title, Version)` named `IX_Songs_ArtistId_Title_Version`.
- [ ] Build Infra.

### Task 2.2: Migration `AddSongVersion`
**Files:** Create migration via `dotnet ef migrations add AddSongVersion` (main agent runs EF CLI; subagent edits the generated file only if needed).
- [ ] Confirm Up: adds `Version TEXT NOT NULL DEFAULT ''`, drops old index, creates new unique index. Down reverses.
- [ ] **Integration test** `SongRepositoryResolutionTests.DuplicateTitleVersion_ThrowsDbUpdateException` (AC-5.5): inserting two rows with same `(ArtistId, Title, Version)` (incl. accent/case variant) throws. Run → Red → ensure config makes it Green.
- [ ] Build + test. Commit.

### Task 2.3: SongRepository query methods (TDD)
**Files:** Modify `Infra/Repository/SongRepository.cs`. Test: `Integration/Repositories/SongRepositoryResolutionTests.cs`.
- [ ] Test `ExistsByTitleVersionForArtist_AccentInsensitive_Matches` ("Café"/"cafe", version "Live"/"live") → Red.
- [ ] Implement `ExistsByTitleVersionForArtistAsync` (+ excludeId) using `EF.Functions.Collate(x, CollationConstants.Default)` on Title AND Version. → Green.
- [ ] Test `GetFuzzyCandidatePool_RespectsArtistAndTake` (pool ≤ take, only that artist, `LIKE token%` collation) → Red.
- [ ] Implement `GetFuzzyCandidatePoolAsync` (collation `EF.Functions.Like`, `.Take(take)`, artist-scoped, `AsNoTracking`). → Green. Commit.

### Task 2.4: ArtistRepository fuzzy pool (TDD)
**Files:** Modify `Infra/Repository/ArtistRepository.cs`. Same pattern as 2.3 (global, not artist-scoped).
- [ ] Test → Red → implement `GetFuzzyCandidatePoolAsync(namePrefixToken, take)` → Green. Commit.

### Task 2.5: SimilarityScorer
**Files:** Create `Infra/Similarity/SimilarityScorer.cs` + `SimilarityConstants.cs`. Test: `Unit/Services/Similarity/SimilarityScorerTests.cs` (lives in Tests).
- [ ] Add the library pinned by Wave 0 `findings.md` to `Infra.csproj`.
- [ ] `SimilarityConstants.DefaultThreshold = 0.82` (provisional), `PoolSize = 50`, `PrefixTokenMaxLen = 12`.
- [ ] Tests (Level A / property): identical → 1.0; accent/case variants ≥ 0.80; empty inputs → 0.0; deterministic. → Red.
- [ ] Implement `Score(a,b)` wrapping the library (token-set ratio normalized to 0..1). → Green. Commit.

---

## Wave 3 — Services (`SEQUENTIAL` — waits Wave 2; full TDD, Level A)

### Task 3.1: ArtistResolutionService (TDD)
**Files:** Create `Services/ArtistResolutionService.cs`. Test: `Unit/Services/ArtistResolutionServiceTests.cs` (Moq `IArtistRepository`, `ISimilarityScorer`).
- [ ] One test per branch → Red → implement → Green, per design §4:
  - external id hit → `ExactExternalMatch`
  - name exact (collation, mock `ExistsByNameAsync`/lookup) → `ExactLocalMatch`
  - fuzzy pool scored ≥ threshold → `FuzzyCandidates`
  - none → `NoMatch`
  - `CommitAsync` create-new sets external identity; resolve-existing returns its id.
- [ ] Commit after each Red/Green pair.

### Task 3.2: SongResolutionService (TDD)
**Files:** Create `Services/SongResolutionService.cs`. Test: `Unit/Services/SongResolutionServiceTests.cs`.
- [ ] Resolve-artist-first ordering test (artist resolved before song matching; INV-1) → Red → implement.
- [ ] External match → `ExactExternalMatch`; exact local `(ArtistId,Title,Version)` → `ExactLocalMatch`; fuzzy → `FuzzyCandidates`; none → `NoMatch`.
- [ ] `titlePrefixToken` derivation test (first token, cap 12; whitespace → empty pool / NoMatch) per N1.
- [ ] `FieldDiffs` computed only when target `HasManualEdits` true; restricted to `{Title, FeaturedArtists, Lyrics, Version}` (N4).
- [ ] `CommitAsync`: `CreateNewVersion` rejects empty Version (AC-1.2); `UpdateExisting` with manual edits applies only `acceptedFields`, else overwrites non-empty; `AttachExternalId` sets identity only.
- [ ] Commit per pair.

### Task 3.3: SongService.CreateSongWithUrlsAsync + update external-id (TDD)
**Files:** Modify `Services/SongService.cs`. Test: extend `Unit/Services/SongServiceTests.cs`.
- [ ] Test: create persists song + all URLs; one shared scoped `AppDbContext`, single `SaveChangesAsync`, failure rolls back both (N3, AC-6.2) → Red → implement → Green.
- [ ] Test: `UpdateSongAsync` persists `externalId`/`externalProvider` when provided (M2) → Red → implement → Green. Commit.

---

## Wave 4 — UI (`SEQUENTIAL` — waits Wave 3). Invoke `myvocalist-coding` first. Incremental XAML edits (one file → build → next).

### Task 4.1: BottomSheetTitle style (BUG-004)
**Files:** Modify `Resources/Styles/MaterialStyles.xaml`.
- [ ] Add `BottomSheetTitle` style: MD3 titleLarge (22sp, RobotoRegular, OnSurface), sheet padding. Build. Commit.

### Task 4.2: SongPickerViewModel + page/DI fix (BUG-010, BUG-006)
**Files:** Create `UI/ViewModels/SongPickerViewModel.cs`; modify `SongPickerPage.xaml.cs`; modify `MauiProgram.cs`. Test: `Unit/ViewModels/SongPickerViewModelTests.cs`. Pattern: mirror `ArtistPickerViewModel`.
- [ ] VM injects `IMusicMetadataService`; `SearchCommand` (`allowConcurrentExecutions:false`), `SelectResultCommand` sends `SongPickedMessage(MusicSearchResultDto)`; `IsLoading`/`HasResults`/`HasSearched`.
- [ ] Tests for state transitions (no `Shell.Current`) → Red → implement → Green.
- [ ] Fix `SongPickerPage.xaml.cs` ctor to inject `SongPickerViewModel`; register `AddTransient<SongPickerViewModel>()` in `MauiProgram.cs`. Build. Commit.

### Task 4.3: Picker pages — suppress Shell back chrome (BUG-007)
**Files:** Modify `SongPickerPage.xaml`, `ArtistPickerPage.xaml`, `PersonPickerPage.xaml`, `QueueSongPickerPage.xaml` (one at a time).
- [ ] Add `<Shell.BackButtonBehavior><BackButtonBehavior IsVisible="False" IsEnabled="False"/></Shell.BackButtonBehavior>`. Build after each. Commit.

### Task 4.4: SongFormViewModel — save catch + buffered URLs + artist autocomplete (BUG-005/008/009)
**Files:** Modify `UI/ViewModels/SongFormViewModel.cs`, `SongFormPage.xaml(.cs)`. Test: `Unit/ViewModels/SongFormViewModelResolutionTests.cs`.
- [ ] BUG-005: wrap `SaveAsync` body in try/catch; on exception show error snackbar; regression test asserts failure surfaces.
- [ ] BUG-008: artist field autocomplete-only; blur-without-valid-selection clears; Edit-mode pre-populate; lock when artist came from API import (`IsArtistLocked`).
- [ ] BUG-009: buffer URLs in an in-memory list when `SongId` is null; on save call `CreateSongWithUrlsAsync`; no "save first" error.
- [ ] Build + test. Commit.

### Task 4.5: Resolution + merge BottomSheets wiring
**Files:** Modify `SongFormPage.xaml(.cs)` + `SongFormViewModel.cs`.
- [ ] On save, build `SongCandidate`; call `ISongResolutionService.ResolveAsync`. `NoMatch` → direct create; else show Resolution sheet (Update / Save as new version [reveals Version entry] / Cancel).
- [ ] When target `HasManualEdits`, show Merge sheet (one row per `FieldDiff`, accept-API toggle); commit accepted fields via `CommitAsync`.
- [ ] Sheets use `dx:BottomSheet` + `BottomSheetTitle`. Build + test. Commit.

---

## Wave 5 — Tests + emulator gate (`SEQUENTIAL`)

### Task 5.1: Integration + traceability
- [ ] Ensure every AC in `requirements.md` maps to a test; fill gaps. Build full solution; `dotnet test` 0 failures.
- [ ] Write AC traceability matrix into `task-log.md`.

### Task 5.2: Emulator smoke (Helder gate)
- [ ] API search → resolution sheet → Update vs Save-as-new-version.
- [ ] Manual near-duplicate → fuzzy confirm. Edited-record import → merge sheet. Add-URL-before-save round trip. Delete/version integrity unchanged.
- [ ] Record results in `task-log.md`. Update BACKLOG status → ✅ Done on green.

---

## Self-review (completed)

- **Spec coverage:** US-1→4.5/3.2, US-2→3.2/4.2/4.5, US-3→3.1, US-4→3.2/4.5, US-5→2.x/2.5, US-6→3.3/4.4; AC-B4→4.1, B5→4.4, B6→4.2, B7→4.3, B8→4.4, B10→4.2. AC-5.5→2.2.
- **Placeholders:** none — Wave 0 resolves the one TBD (library) before Wave 2 consumes it.
- **Type consistency:** method names/signatures match design §3 across waves (`ExistsByTitleVersionForArtistAsync`, `GetFuzzyCandidatePoolAsync`, `CreateSongWithUrlsAsync`, `ResolveAsync`/`CommitAsync`, `ResolutionKind`/`ResolutionChoice`).

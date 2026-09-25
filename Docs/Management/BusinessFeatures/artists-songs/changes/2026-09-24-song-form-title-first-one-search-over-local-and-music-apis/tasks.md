# Tasks: Song form title-first, one search over local and music APIs

> **Status: DRAFT — not dispatchable** until Helder approves `requirements.md`/`design.md` and the
> gates in Phase 0 are met. Format: `.claude/library/workflow-rule-4.md`. Every task: ≤ 5 owned
> files (a generated migration counts as one `Migrations/*` artifact, as in the rule's template),
> ≤ 2 h, builds green, tests green.
>
> **Ordering.** Additions follow the DRY Onion (Domain → Infra → Services → UI). Removals of existing
> members run outside-in (UI → Services → Infra → Domain) so every task still builds; each removal
> task names the addition it waits for. Level A tests are written **Red first** (`Lane: RED` commit
> per `.claude/exception-registry.md`, then Green).
>
> **Sequential-only files touched:** `MauiProgram.cs`, `AppShell.xaml.cs`, every `Migrations/` file,
> this `tasks.md`. `SongFormViewModel.cs`, `SongFormViewModelTests.cs`, `SongService.cs` are
> single-writer hotspots here: tasks owning them never run in the same wave.

## Phase 0 — Gates (not tasks of this item)

- [ ] **G1: Provider DI defect fixed and merged** (prerequisite bug; `design.md` § 8). Blocks 4.1–4.4 and every E2E step that needs Deezer.
- [ ] **G2: Shared Field merged** (`DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md`, all tasks through its on-device gate), including the non-selectable option if Q12 is accepted. Blocks Phase 5.
- [ ] **G3: CATALOG-PROP coordination agreed** (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`): which item writes `SongService.cs` and adds its migration first. The second rebases onto the first. Blocks 3.1 and 3.3.
- [ ] **G4: Helder's answers** to the open questions (`requirements.md` § Open questions). Q1, Q8, Q11 block Phase 3–4; Q10 (store the Deezer artist id) blocks 6.2; the rest block Phase 5.

## Phase 1 — Removal of picker entry points and inline create (UI, outside-in)

- [ ] **1.1 Remove the Song-picker entry from the Song form** [SEQUENTIAL]
  - **Produces:** `SongFormPage` without the "Search music database" row; `SongFormViewModel` without `NavigateToSongPickerCommand`, `OnSongPicked`, `ResolveAndLockArtistAsync`, the messenger registration.
  - **Consumes:** nothing.
  - **Risk:** Low — dead path (providers never worked, findings § 0.1).
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - **Tests:** delete `:486`, `:509` (AC-11.2 / REQ-SFT-44); edit `:475`. Record the mapping in the task-log.
  - **Demo:** Admin opens New Song and sees no "Search music database" row.
  - **Review lane:** Standard

- [ ] **1.2 Remove the Artist-picker entry from the Artist form** [P with 1.1]
  - **Produces:** `ArtistFormPage` without the row; `ArtistFormViewModel` without `NavigateToArtistPickerCommand` and its message handling.
  - **Consumes:** nothing.
  - **Risk:** Low.
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, the Artist-form VM test file (census)
  - **Demo:** Admin opens New Artist and sees no "Search music database" row.
  - **Review lane:** Standard

- [ ] **1.3 Unregister both picker pages** [SEQUENTIAL — after 1.1, 1.2]
  - **Produces:** no routes, Shell registrations or DI entries for either picker.
  - **Consumes:** 1.1, 1.2.
  - **Risk:** Low.
  - **Files owned:** `MyVocaList/Navigation/Routes.cs`, `MyVocaList/AppShell.xaml.cs`, `MyVocaList/MauiProgram.cs`
  - **Demo:** App builds and runs; Songs and Artists forms open normally.
  - **Review lane:** Standard

- [ ] **1.4 Delete the Song picker** [P with 1.5 — after 1.3]
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`, `SongPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`, `Contracts/Messages/SongPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`
  - **Produces / Consumes:** deletions / 1.3. **Risk:** Low. **Tests:** delete all 12 (AC-SONG-*, REQ-SFT-44).
  - **Demo:** census (Python `os.walk`) finds no `SongPicker`/`SongPickedMessage` reference; build 0 errors.
  - **Review lane:** Standard

- [ ] **1.5 Delete the Artist picker** [P with 1.4 — after 1.3]
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`, `ArtistPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`, `Contracts/Messages/ArtistPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs`
  - **Produces / Consumes:** deletions / 1.3. **Risk:** Low. **Tests:** delete all (AC-ART-*, REQ-SFT-44).
  - **Demo:** census finds no `ArtistPicker`/`ArtistPickedMessage` reference; build 0 errors.
  - **Review lane:** Standard

- [ ] **1.6 Delete `IMusicMetadataService`** [SEQUENTIAL — after 1.4, 1.5]
  - **Files owned:** `Services/IMusicMetadataService.cs`, `Services/MusicMetadataService.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
  - **Risk:** Low — census must show zero remaining consumers first.
  - **Demo:** build 0 errors; `AppServicesRegistrationTests` green.
  - **Review lane:** Standard

- [ ] **1.7 Remove inline artist create** [SEQUENTIAL — after 1.1]
  - **Produces:** no `CreateArtistInlineCommand`, no create sentinel; blur keeps text and sets **no** error (REQ-SFT-17).
  - **Files owned:** `SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`, `SongFormViewModelTests.cs`
  - **Risk:** Medium — the Save guard still requires a picked artist until 5.3; unpicked text is blocked at Save in the meantime (acceptable interim, not shipped).
  - **Tests:** Red first — rewrite `:287` (retains text, no error); delete `:334`, `:550`, `:570` with AC mapping.
  - **Demo:** typing an unknown artist and leaving the field shows no error and keeps the text.
  - **Review lane:** Standard

## Phase 2 — Domain + Infra additions and fixes

- [ ] **2.1 Song lookup queries for duplicates and identity** [P with 2.2]
  - **Produces:** `ISongRepository.FindIdByTitleVersionForArtistAsync`, `ExistsByExternalIdentityAsync` + implementations.
  - **Consumes:** nothing.
  - **Risk:** Medium — collation must be DB-side (`EF.Functions.Collate`), no C# re-filter.
  - **Files owned:** `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`, `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs`
  - **Tests (B):** "Nao Sei" finds the stored "Não Sei" id; exclude-self; identity exists / not exists.
  - **Demo:** repository tests green on real SQLite.
  - **Review lane:** Standard

- [ ] **2.2 `ISimilarityScorer.RankScore`** [P with 2.1]
  - **Produces:** `RankScore` (FuzzySharp `TokenSortRatio` on `Normalize`d strings, 0–1).
  - **Files owned:** `Domain/ServicesInterfaces/ISimilarityScorer.cs`, `Infra/Similarity/SimilarityScorer.cs`, `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs`
  - **Risk:** Low. **Tests (A, Red first):** § 6.3 table pairs ("dont cry": "Don't Cry" > "Cry"; "don cry"; "guns cry" vs title+artist).
  - **Demo:** scorer tests green; `Score` tests unchanged.
  - **Review lane:** Elevated (exception § 6.4 applies; orchestrator files the registry row before this task starts)

## Phase 3 — Services (write path) and resolution simplification

- [ ] **3.1 `SongService`: create a typed artist inside the song's unit of work** [SEQUENTIAL — after 2.1, G3]
  - **Produces:** `CreateSongWithUrlsAsync(int? artistId, string? newArtistName, …)` with `FlushAsync`; external-identity uniqueness check (REQ-SFT-26).
  - **Consumes:** 2.1; `IUnitOfWork.FlushAsync`.
  - **Risk:** High — atomicity; CATALOG-PROP hooks the same method.
  - **Files owned:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`, `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (new; ports `NestedUnitOfWorkTests.cs:22,66,105,146`), `SongFormViewModel.cs` (call site only)
  - **Tests (A, Red first):** one artist + one song; throw after artist staged → 0/0 rows; failure tuple → 0 rows; one `AppDbContext`; duplicate artist name → failure, nothing written; identity already held → failure.
  - **Demo:** integration tests green; creating a song with a brand-new artist name persists both.
  - **Review lane:** Architectural

- [ ] **3.2 `SongService.UpdateSongAsync`: Version-aware uniqueness, new artist, no `hasManualEdits`** [SEQUENTIAL — after 3.1]
  - **Produces:** REQ-SFT-29 fix; `newArtistName`; parameter `hasManualEdits` removed (writer `SongService.cs:172` gone).
  - **Files owned:** `ISongService.cs`, `SongService.cs`, `SongServiceTests.cs`, `SongFormViewModel.cs` (call site `:606`), `Services/SongResolutionService.cs` (call sites `:240-243,264-267`, compile only)
  - **Risk:** High — BUG-067/068 territory (`SongService.cs:159-167` navigation detach must stay).
  - **Tests (A, Red first):** REQ-SFT-29 regression ("Creep"/"Acoustic"); REQ-SFT-26 on update; update `:245`, `:480` mocks. Other `UpdateSongAsync` callers in tests: census; if > 5 files, split by folder into 3.2b.
  - **Demo:** editing a song into a new Version of an existing title saves.
  - **Review lane:** Elevated

- [ ] **3.3 Simplify `SongResolutionService`** [SEQUENTIAL — after 3.1, 1.x merge-sheet removal in 5.1]
  - **Produces:** `ResolveAsync` only; exact match via `FindIdByTitleVersionForArtistAsync` (REQ-SFT-42); no `FieldDiffs`/`TargetHasManualEdits`/`ApplyUpdate`/`CommitAsync`.
  - **Files owned:** `Domain/ServicesInterfaces/ISongResolutionService.cs`, `Services/SongResolutionService.cs`, `Domain/Resolution/ResolutionResults.cs`, `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`, `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (delete, after 3.1 ported 4 tests; `:195` deleted with AC-3.4 mapping)
  - **Risk:** Medium. **Tests:** REQ-SFT-42 regression Red first; deletions per `design.md` § 10.1.
  - **Demo:** saving "Nao Sei" for Ana Carolina offers "Não Sei" (VM test) — full E2E in Phase 7.
  - **Review lane:** Elevated

- [ ] **3.4 Simplify `ArtistResolutionService`; delete `ResolutionChoice`** [SEQUENTIAL — after 3.3]
  - **Files owned:** `Domain/ServicesInterfaces/IArtistResolutionService.cs`, `Services/ArtistResolutionService.cs`, `Domain/Resolution/ResolutionChoice.cs` (delete), `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`
  - **Risk:** Low. **Tests:** delete `:200,:225,:249` (AC-3.2/3.4 → REQ-SFT-40).
  - **Demo:** build 0 errors; `AppServicesRegistrationTests` green.
  - **Review lane:** Standard

- [ ] **3.5 Drop `HasManualEdits` from read models and projections** [SEQUENTIAL — after 3.2, 3.3]
  - **Files owned:** `Contracts/DTOs/List/SongListItemDto.cs`, `Contracts/DTOs/List/ArtistListItemDto.cs`, `Domain/ReadModels/ArtistListItem.cs`, `Infra/Repository/SongRepository.cs`, `Infra/Repository/ArtistRepository.cs` (+ `CatalogRepository.cs` projection in 3.5b if the test census adds files)
  - **Risk:** Low — no XAML binds these (findings § 3.3/3.4). `ArtistService.cs:107` writer removed in 3.6.
  - **Demo:** Songs and Artists lists render unchanged.
  - **Review lane:** Standard

- [ ] **3.6 M1: drop the `HasManualEdits` columns** [SEQUENTIAL — after 3.5]
  - **Files owned:** `Domain/Entity/Song.cs`, `Domain/Entity/Artist.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/EntityEFConfig/ArtistConfiguration.cs`, `Infra/Migrations/*` (M1) — `Services/ArtistService.cs:107` line removal is the one extra edit (sizing note in task-log)
  - **Risk:** Medium — SQLite table rebuild.
  - **Tests (B):** migration test on a temp file: columns absent after `MigrateAsync`; existing rows survive.
  - **Demo:** app upgrades an existing database and lists all songs.
  - **Review lane:** Elevated

- [ ] **3.7 M2: unique `(ExternalProvider, ExternalId)`** [SEQUENTIAL — after 3.6]
  - **Files owned:** `SongConfiguration.cs`, `Infra/Migrations/*` (M2), `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (new)
  - **Risk:** Medium — data fix. **Tests (B):** migrate to M1, seed two songs with one identity, migrate → lowest Id keeps it, other row intact; duplicate insert then fails.
  - **Demo:** migration test green.
  - **Review lane:** Elevated

- [ ] **3.8 M3: FTS5 `SongSearch` + triggers** [SEQUENTIAL — after 3.7]
  - **Files owned:** `Infra/Migrations/*` (M3), `SongMigrationTests.cs`, EF config file only if Context7 (EF Core 10) says tables with triggers need `HasTrigger`/`UseSqlReturningClause(false)`
  - **Risk:** High — raw SQL; later table rebuilds drop triggers.
  - **Tests (B):** after `MigrateAsync`: table + 4 triggers exist; backfill count = song count; insert / title update / artist rename / `ExecuteDelete` keep it in step (REQ-SFT-34).
  - **Demo:** migration tests green; existing library searchable.
  - **Review lane:** Architectural
  - **Orchestrator step:** add "table rebuild drops the `SongSearch` triggers" to `constraints-reference.md § EF Core / SQLite`.

- [ ] **3.9 Title-field search query** [SEQUENTIAL — after 3.8]
  - **Produces:** `SongSearchHit`; `SearchTitleCandidatesAsync`, `FindByExternalIdsAsync`, `FindByTitleAndArtistNamesAsync`.
  - **Files owned:** `Domain/ReadModels/SongSearchHit.cs`, `ISongRepository.cs`, `SongRepository.cs`, `MyVocaList.Tests/Integration/Repositories/SongRepositorySearchTests.cs` (new, uses `MigrateAsync`)
  - **Risk:** Medium. **Tests (B):** § 6.3 corpus subset: "dont cry"/"don cry"/"guns cry" include "Don't Cry"; "nao sei" hits; 2-char query uses prefix; `"` in a query is escaped.
  - **Demo:** repository tests green.
  - **Review lane:** Elevated

## Phase 4 — Services (search)

- [ ] **4.1 Deezer plain query + failure propagation** [P with 3.x Infra — after G1]
  - **Files owned:** `Services/IMusicMetadataProvider.cs`, `Services/DeezerProvider.cs`, `Services/MusicBrainzProvider.cs` (delete if Q1 = drop), `MyVocaList.Tests/Unit/Services/DeezerProviderTests.cs` (new, fake `HttpMessageHandler`)
  - **Risk:** Medium. **Tests (B):** URL is `search/track?q=dont%20cry` with no qualifiers; HTTP 500 and bad JSON throw; caller cancellation propagates.
  - **Demo:** provider tests green.
  - **Review lane:** Standard

- [ ] **4.2 `SongSuggestionService.SearchTitleAsync`** [SEQUENTIAL — after 2.2, 3.9, 4.1]
  - **Produces:** `SongTitleSearchResult`; `SongSuggestionDto.Version`/`LocalArtistName`; ranking, dedup (a)/(b), status, 3,000 ms bound; tier (c) and C# re-filters deleted.
  - **Files owned:** `Services/ISongSuggestionService.cs`, `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`, `Contracts/DTOs/Suggestions/SongTitleSearchResult.cs` (new), `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`
  - **Risk:** High — core behaviour. **Tests (A, Red first):** REQ-SFT-04/05/06/08/09/30–32 at service level; rewrites/deletions per § 10.1.
  - **Demo:** service tests green.
  - **Review lane:** Elevated

- [ ] **4.3 Retarget threshold tests; register the service** [SEQUENTIAL — after 4.2]
  - **Files owned:** `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`, `ISongSuggestionService.cs` (remove `GetLocalAsync`/`GetRemoteAsync`)
  - **Risk:** Low. **Demo:** `ISongSuggestionService` resolves from `AddAppServices`.
  - **Review lane:** Standard

## Phase 5 — UI (Song form) [after G2; one XAML file per task, build between]

- [ ] **5.1 VM: resolution sheet → Open existing; merge sheet removed** [SEQUENTIAL]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (merge sheet deleted, labels), `MergeFieldRow` file (delete; census)
  - **Consumes:** `INavigationService`. **Risk:** Medium. **Tests (A):** Open existing navigates `../song-form?songId=N`; Save as new song; delete `:707` (AC-4.2).
  - **Demo:** saving a duplicate offers Open existing, which opens the stored song.
  - **Review lane:** Elevated
  - *Note:* runs before 3.3 (it removes the last `CommitAsync` caller).

- [ ] **5.2 VM: title search and picks** [SEQUENTIAL — after 4.3, 5.1]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** High. **Tests (A, Red first):** REQ-SFT-02…14 (option mapping, status row inert, local pick → navigate, discard sheet, remote pick fills + locks, local artist match, X clears pending identity).
  - **Demo:** VM tests green.
  - **Review lane:** Elevated

- [ ] **5.3 VM: save flow (artist create-on-save, near-duplicate sheet, bug fixes)** [SEQUENTIAL — after 5.2, 3.2, 3.4]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** High. **Tests (A, Red first):** REQ-SFT-18…26, 28; REQ-SFT-41 regression (artist candidate never carries the song identity); only `CreateSongWithUrlsAsync`/`UpdateSongAsync` are called for writes (REQ-SFT-46).
  - **Demo:** VM tests green.
  - **Review lane:** Architectural

- [ ] **5.4 VM: edit-mode title and hydration** [SEQUENTIAL — after 5.3]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** Low. **Tests (A):** `IsTitleReadOnly` true for API-sourced, false for manual; rewrite `:787`; stale docstring `:433` fixed.
  - **Demo:** VM tests green.
  - **Review lane:** Standard

- [ ] **5.5 Page: field order, Title and Artist Fields** [SEQUENTIAL — after 5.4]
  - **Files owned:** `SongFormPage.xaml`
  - **Risk:** Medium (XAML). Title and Artist are `AutocompleteDXField` (`UI/Components/TextFields/`); Title binds `IsSearchEnabled`/`IsReadOnly` to the VM.
  - **Demo:** New Song shows Title first as a search box; Edit Song shows Title without search, read-only with no X for an imported song.
  - **Review lane:** Standard

- [ ] **5.6 Page: sheets (discard, near-duplicate artist, resolution)** [SEQUENTIAL — after 5.5]
  - **Files owned:** `SongFormPage.xaml`
  - **Risk:** Medium. **Demo:** each sheet opens from its trigger and Back dismisses it with no save.
  - **Review lane:** Standard

- [ ] **5.7 Code-behind: delete the Song autocomplete glue** [SEQUENTIAL — after 5.6]
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Risk:** Low. **Demo:** REQ-ACF-11 check: no `OnArtistItemsRequested`/`OnArtistSelectionChanged`/`OnArtistUnfocused`/`OnArtistClearIconClicked`, no `artistEdit.SelectedItem`.
  - **Review lane:** Standard

## Phase 6 — Artist form name search (confirmed, fifth round)

- [ ] **6.1 `ArtistSuggestionService.SearchNameAsync`: collated local, Deezer-only remote, status; register** [SEQUENTIAL — after 4.1, 4.3]
  - **Produces:** `SearchNameAsync` + `ArtistNameSearchResult`; similarity dedup tier (c) and `FilterSimilar` deleted (`design.md` § 4.1).
  - **Consumes:** 4.1 (`DeezerProvider.SearchArtistsAsync` throws on failure; URL `search/artist?q=`).
  - **Files owned:** `Services/IArtistSuggestionService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionThresholdTests.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
  - **Risk:** Medium. **Tests (A, Red first):** local first, collated-name dedup, status on failure, thresholds; deleted tests mapped to REQ-FORMUX-03(c)/-10 → REQ-SFT-47/48.
  - **Demo:** service tests green; `IArtistSuggestionService` resolves.
  - **Review lane:** Elevated
- [ ] **6.2 `ArtistFormViewModel`: Name option source, local pick opens the artist, Deezer pick fills the name** [SEQUENTIAL — after 6.1, 1.2, G2, Q10]
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, the Artist-form VM test file
  - **Risk:** Medium. **Tests (A, Red first):** REQ-SFT-47…50 (navigation via `INavigationService`).
  - **Demo:** VM tests green.
  - **Review lane:** Standard
- [ ] **6.3 `ArtistFormPage`: Name as the Field** [SEQUENTIAL — after 6.2]
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `ArtistFormPage.xaml.cs`
  - **Risk:** Medium. **Demo:** typing "metalica" on New Artist lists "Metallica" (local, "In your library") and Deezer artists; tapping "Metallica" opens it.
  - **Review lane:** Standard

## Phase 7 — Close-out

- [ ] **7.1 Spec ritual + traceability matrix** (orchestrator): fill `requirements.md` § Traceability; task-log matrix; README frontmatter status; `backlog_gen.py regen`; `.sln` check.
- [ ] **7.2 On-device E2E gate — Helder** (Android). Stop and hand off; nothing merges before Helder's pass.
  1. Upgrade an existing install: songs, artists and YouTube links intact (M1–M3).
  2. New Song: Title is first. Type "don cry" → "Don't Cry" local first with "In your library"; version rows distinguishable.
  3. Tap the local row → the edit form of that song opens; Back returns to the Songs list.
  4. Type "sweet chld o mine" → Deezer rows marked "Deezer". Pick one not in the library → Title and Artist filled and locked; X on Title empties it.
  5. Pick a Deezer song whose artist is not local → Save → song and artist both created; artist's catalog behaviour per CATALOG-PROP if landed.
  6. Airplane mode → local rows + "Online search unavailable"; tapping it does nothing; Save works.
  7. Type "Metalica" as artist (with "Metallica" stored) → Save → near-duplicate sheet; Use and Create both behave; Back saves nothing.
  8. Edit an imported song → Title read-only; edit a manual song → Title editable; change Version of a title that exists → saves.
  9. Save a duplicate by hand → Open existing / Save as new version.
  10. No "Search music database" row on Song or Artist forms; "Search YouTube (API)" unchanged.
  11. New Artist: type "metalica" → local "Metallica" first; tap it → its edit form opens. Pick a Deezer artist → Name filled and locked; Save creates it; a collated duplicate shows the existing message.

## Coordination

- **CATALOG-PROP:** both change `SongService.cs` (3.1/3.2 here) and add migrations. Single writer:
  whichever item's task runs second rebases and re-runs the other's tests. Their propagation hooks
  go inside the same two methods; the new artist already has an id after `FlushAsync` (`design.md` § 13).
- **Shared Field item:** SongForm migration is handed to this item (its `design.md` § 3.2). Q12 adds
  one small capability there if accepted.
- **Provider DI bug (G1):** register separately if not already; 4.1+ and E2E steps 4–6 depend on it.

## Follow-ups (not this item)

- `ISongService.CreateSongAsync` has no production caller after 3.3 (title-only uniqueness); decide
  delete vs align.
- YouTube search (`YouTubeSearchPage`, "Search YouTube (API)"): out of scope, pending a separate
  discussion on the YouTube API-key requirement.
- Frozen `UI/Components/AutocompleteField/` deletion (dependency item's follow-up).

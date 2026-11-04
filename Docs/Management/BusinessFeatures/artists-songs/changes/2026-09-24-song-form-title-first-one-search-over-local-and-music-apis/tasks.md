# Tasks: Song form title-first, one search over local and music APIs

> **Spec updated 2026-09-26:** (D15; D21 added 2026-11-04, pending approval) **APPROVED 2026-09-26 and dispatchable** under Helder's pre-authorisation of 2026-09-25. Gates: G1 done (BUG-082 `fa520e3f`); G4 resolved by the pre-authorisation (every open question takes its recorded recommendation); G2 = ACF T4 merged, blocking only the page tasks 5.5, 5.6, 6.3 (D2); G3 = plan § 0.2.2; G5 = STOP G5 (blocks 3.8 onward). (D14) Testers commit test files only (`Lane: RED`, `--no-verify`, per `.claude/exception-registry.md` row 2026-09-18); no production skeleton is committed with a Red. Every deviation from this file is listed in `plan.md` § 0.3 (D1–D20) and noted at its task below.
>
> **Status: DRAFT — not dispatchable** until Helder approves `requirements.md`/`design.md` and the
> gates in Phase 0 (G1–G5) are met. Format: `.claude/library/workflow-rule-4.md`. Every task: ≤ 5 owned
> files (a generated migration counts as one `Migrations/*` artifact, as in the rule's template),
> ≤ 2 h, builds green, tests green.
>
> **Ordering.** Additions follow the DRY Onion (Domain → Infra → Services → UI). Removals of existing
> members run outside-in (UI → Services → Infra → Domain) so every task still builds; each removal
> task names the addition it waits for. Level A tests are written **Red first** (`Lane: RED` commit
> per `.claude/exception-registry.md`, then Green).
>
> **Sequential-only files touched:** `MauiProgram.cs` (1.3, and 4.1c per D21), `AppShell.xaml.cs`, every `Migrations/` file,
> this `tasks.md`. `SongFormViewModel.cs`, `SongFormViewModelTests.cs`, `SongService.cs` are
> single-writer hotspots here: tasks owning them never run in the same wave.

## Phase 0 — Gates (not tasks of this item)

- [x] **G1: BUG-082 fixed and merged** — music API providers re-registered in DI, bypassing the typed `HttpClient` (`Docs/Management/BusinessFeatures/artists-songs/bugs/2026-09-25-BUG-082-music-api-providers-re-registered-in-di-bypassing-typed-httpclient/`). Blocks 4.1–4.3, 6.1 and every E2E step that needs Deezer.
- [x] **G2: Shared Field `AutocompleteDXField` merged** (`DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md`, all tasks through its on-device gate), with `SupportingText` (REQ-ACF-12c) and `IsSearchEnabled` (`IsReadOnly` exists but is unused here), and the append capability if accepted (Q16; `design.md` § 3.4). Blocks Phase 5 and 6.2–6.3.
- [x] **G3: Migration and `SongService` coordination agreed.** — Helder 2026-11-04: SFT goes first; the CATALOG-PROP spec itself waits until SFT is done; BUG-080 is fixed with no new migration (`#if DEBUG` seed, no-op `Down()`). CATALOG-PROP (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`) writes `SongService.cs` and adds a backfill migration; BUG-080's fix for `20260211055225_venuesSeedForTest` may add a Venues-only migration (harmless: it rebuilds neither Songs nor Artists). Whichever merges second **regenerates its migration against the merged model snapshot**. Blocks 3.1, 3.2a and 3.6b.
- [ ] **G4: Helder's answers** to the open questions (`requirements.md` § Open questions). Q1, Q8, Q11 block Phases 3–4; Q4 blocks 3.10; Q10 blocks 6.2; Q16 blocks 5.2; the rest block Phase 5.
  > **Spec updated 2026-11-04:** all answered (Q5 = B, Q7 = two messages, clarified the same day); the Q5/Q7 REQ amendments are still to be written and approved. **Amendment written later on 2026-11-04 (plan § 0.3 D21; REQ-SFT-04/05/06/08/12/47 amended, REQ-SFT-53 new); awaiting spec-reviewer + Helder's approval. Gated on it: 4.1a, new 4.1c, and the D21 parts of 3.9, 4.1b, 4.2, 4.3, 5.2, 6.1, 6.2.** Q1, Q8, Q11, Q4, Q10 resolved → Phases 3–4 and 6.2 unblocked by G4, **except 4.1a** (its failure propagation must distinguish the two Q7 cases once the REQ amendment is approved).
- [x] **G5: FTS5 trigram spike on Android** — PASS 2026-11-04 (`findings.md` § G5) (time-box 1 h, throwaway code, no production files). On the device, through the app's own SQLite connection: `SELECT sqlite_compileoption_used('ENABLE_FTS5')` returns 1, and `CREATE VIRTUAL TABLE t USING fts5(x, tokenize='trigram remove_diacritics 1')` succeeds and matches "nao" to "Não". Record the output in `findings.md`. **If either fails: stop and escalate to Helder**; the named fallback is the token-prefix pool + `RankScore` rerank (`design.md` § 6.1), which removes M3 and changes 3.8, 3.9, 3.9b, 3.9c. Blocks 3.8.

## Phase 1 — Removal of picker entry points and inline create (UI, outside-in)

- [x] **1.1 Remove the Song-picker entry from the Song form** [SEQUENTIAL]
  - **Produces:** `SongFormPage` without the "Search music database" row; `SongFormViewModel` without `NavigateToSongPickerCommand`, `OnSongPicked`, `ResolveAndLockArtistAsync`, the messenger registration.
  - **Consumes:** nothing.
  - **Risk:** Low — dead path (providers never worked, findings § 0.1).
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - **Tests:** delete `:486`, `:509` (AC-11.2 / REQ-SFT-44); edit `:475`. Record the mapping in the task-log.
  - **Demo:** Admin opens New Song and sees no "Search music database" row.
  - **Review lane:** Standard

- [x] **1.2 Remove the Artist-picker entry from the Artist form** (REQ-SFT-44) [P with 1.1]
  - **Produces:** `ArtistFormPage` without the row; `ArtistFormViewModel` without `NavigateToArtistPickerCommand` and its message handling.
  - **Consumes:** nothing.
  - **Risk:** Low.
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, the Artist-form VM test file (census)
  - **Demo:** Admin opens New Artist and sees no "Search music database" row.
  - **Review lane:** Standard

- [x] **1.3 Unregister both picker pages** [SEQUENTIAL — after 1.1, 1.2]
  - **Produces:** no routes, Shell registrations or DI entries for either picker.
  - **Consumes:** 1.1, 1.2.
  - **Risk:** Low.
  - **Files owned:** `MyVocaList/Navigation/Routes.cs`, `MyVocaList/AppShell.xaml.cs`, `MyVocaList/MauiProgram.cs`
  - **Demo:** App builds and runs; Songs and Artists forms open normally.
  - **Review lane:** Standard

- [x] **1.4 Delete the Song picker** [P with 1.5 — after 1.3]
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`, `SongPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`, `Contracts/Messages/SongPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`
  - **Produces / Consumes:** deletions / 1.3. **Risk:** Low. **Tests:** delete all 12 (AC-SONG-*, REQ-SFT-44).
  - **Demo:** census (Python `os.walk`) finds no `SongPicker`/`SongPickedMessage` reference; build 0 errors.
  - **Review lane:** Standard

- [x] **1.5 Delete the Artist picker** [P with 1.4 — after 1.3]
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`, `ArtistPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`, `Contracts/Messages/ArtistPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs`
  - **Produces / Consumes:** deletions / 1.3. **Risk:** Low. **Tests:** delete all (AC-ART-*, REQ-SFT-44).
  - **Demo:** census finds no `ArtistPicker`/`ArtistPickedMessage` reference; build 0 errors.
  - **Review lane:** Standard

- [x] **1.6 Delete `IMusicMetadataService`** [SEQUENTIAL — after 1.4, 1.5]
  - **Files owned:** `Services/IMusicMetadataService.cs`, `Services/MusicMetadataService.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
  - **Risk:** Low — census must show zero remaining consumers first.
  - **Demo:** build 0 errors; `AppServicesRegistrationTests` green.
  - **Review lane:** Standard

- [x] **1.7 Remove inline artist create** [SEQUENTIAL — after 1.1]
  - **Produces:** no `CreateArtistInlineCommand`, no create sentinel; blur keeps text and sets **no** error (REQ-SFT-17).
  - **Files owned:** `SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`, `SongFormViewModelTests.cs`
  - **Risk:** Medium — the Save guard still requires a picked artist until 5.3; unpicked text is blocked at Save in the meantime (acceptable interim, not shipped).
  - **Tests:** Red first — rewrite `:287` (retains text, no error); delete `:334`, `:550`, `:570` with AC mapping.
  - **Demo:** typing an unknown artist and leaving the field shows no error and keeps the text.
  - **Review lane:** Standard

## Phase 2 — Domain + Infra additions and fixes

- [x] **2.1 Song lookup queries for duplicates and identity** [P with 2.2]
  - **Produces:** `ISongRepository.FindIdByTitleVersionForArtistAsync`, `ExistsByExternalIdentityAsync` + implementations.
  - **Consumes:** nothing.
  - **Risk:** Medium — collation must be DB-side (`EF.Functions.Collate`), no C# re-filter.
  - **Files owned:** `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`, `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs`
  - **Tests (B):** "Nao Sei" finds the stored "Não Sei" id; exclude-self; identity exists / not exists.
  - **Demo:** repository tests green on real SQLite.
  - **Review lane:** Standard

- [x] **2.2 `ISimilarityScorer.RankScore`** [P with 2.1]
  - **Produces:** `RankScore` (FuzzySharp `TokenSortRatio` on `Normalize`d strings, 0–1).
  - **Files owned:** `Domain/ServicesInterfaces/ISimilarityScorer.cs`, `Infra/Similarity/SimilarityScorer.cs`, `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs`
  - **Risk:** Low. **Tests (A, Red first):** § 6.3 table pairs ("dont cry": "Don't Cry" > "Cry"; "don cry"; "guns cry" vs title+artist).
  - **Demo:** scorer tests green; `Score` tests unchanged.
  - **Review lane:** Elevated (exception § 6.4 applies; orchestrator files the registry row before this task starts)
  > **Spec updated 2026-09-26:** (D16) also owns `Domain/Constants/SearchConstants.cs` (adds `RemoteSearchTimeoutMilliseconds = 3000`, REQ-SFT-08) and `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` (5 files). The § 6.4 exception row is filed in `.claude/exception-registry.md` (Step 0.6).

## Phase 3 — Services (write path), resolution simplification, schema

- [x] **3.1 `SongService`: create a typed artist inside the song's unit of work** [SEQUENTIAL — after 2.1, G3]
  - **Produces:** `CreateSongWithUrlsAsync(int? artistId, string? newArtistName, …)` with `FlushAsync`; external-identity uniqueness check (REQ-SFT-26).
  - **Consumes:** 2.1; `IUnitOfWork.FlushAsync`.
  - **Risk:** High — atomicity; CATALOG-PROP hooks the same method.
  - **Files owned:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`, `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (new; ports `NestedUnitOfWorkTests.cs:22,66,105,146`, the last one as a 2-level test), `SongFormViewModel.cs` (call site only)
  - **Tests (A, Red first):** one artist + one song; throw after the artist is staged → 0/0 rows; failure tuple → 0 rows; one `AppDbContext` across the 2 levels; duplicate artist name → failure, nothing written; identity already held → failure.
  - **Demo:** integration tests green; creating a song with a brand-new artist name persists both.
  - **Review lane:** Architectural
  > **Spec updated 2026-09-26:** (D3) 3.1 **adds** the new overload and keeps the old `CreateSongWithUrlsAsync(int artistId, …)` `[Obsolete]`, forwarding with `newArtistName: null`; 3.1 no longer touches `SongFormViewModel.cs` (removed from Files owned). New task 5.3b deletes the old overload after 5.3. Tester (W2) / Builder (W3, Opus) split.

- [x] **3.2a `UpdateSongAsync`: Version-aware uniqueness and `newArtistName`** [SEQUENTIAL — after 3.1]
  > **Spec updated 2026-11-04:** (merged `eab8b6b2`) also edited 12 Moq `UpdateSongAsync` expressions in `SongResolutionServiceTests.cs`/`SongFormViewModelTests.cs` — forced: expression trees cannot omit the new optional `newArtistName`, so plan § 3.2a's "no call site breaks" holds for real callers only, not for Moq setups. Lesson for 3.2b/3.2c: a signature change on `ISongService` must list every Moq expression file as owned.
  - **Produces:** REQ-SFT-29 fix; optional `newArtistName` parameter (added last, so no call site breaks); REQ-SFT-26 on update.
  - **Files owned:** `ISongService.cs`, `SongService.cs`, `SongServiceTests.cs`
  - **Risk:** High — BUG-067/068 territory (`SongService.cs:159-167` navigation detach must stay).
  - **Tests (A, Red first):** REQ-SFT-29 regression ("Creep"/"Acoustic"); REQ-SFT-27 (title edit keeps `ExternalId`/`ExternalProvider`; edited title that collides with (artist, title, version) is rejected); new artist on update; identity held by another song; update `:245`, `:480` mocks.
  - **Demo:** editing a song into a new Version of an existing title saves.
  - **Review lane:** Elevated

- [x] **3.3 Simplify `SongResolutionService`** [SEQUENTIAL — after 3.1, 5.1]
  - **Produces:** `ResolveAsync` only; exact match via `FindIdByTitleVersionForArtistAsync` (REQ-SFT-42); no `FieldDiffs`/`TargetHasManualEdits`/`ApplyUpdate`/`CommitAsync`.
  - **Files owned:** `Domain/ServicesInterfaces/ISongResolutionService.cs`, `Services/SongResolutionService.cs`, `Domain/Resolution/ResolutionResults.cs`, `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`, `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (delete, after 3.1 ported 4 tests; `:195` deleted with REQ-UOW-09 / AC-3.4 mapping)
  - **Risk:** Medium. **Tests:** REQ-SFT-42 regression Red first; deletions per `design.md` § 10.1.
  - **Demo:** resolution tests green; "Nao Sei" for Ana Carolina resolves to the stored "Não Sei" id.
  - **Review lane:** Elevated
  > **Spec updated 2026-09-26:** (D4) 3.3 keeps the `SongResolution` record shape (passes `FieldDiffs: []`, `TargetHasManualEdits: false`); `ResolutionResults.cs` moves to new task 3.3b. Runs after 5.1 (precondition census: no `CommitAsync(` in `SongFormViewModel.cs`).

- [x] **3.3b Shrink `SongResolution`; delete `FieldDiff`** [SEQUENTIAL — after 3.3] *(new, plan § 0.3 D4)*
  > **Spec updated 2026-11-04 (withdrawn the same day):** an earlier note told 3.3b to also drop `SongResolutionService`'s now-unused constructor parameters. **Withdrawn** — the permanent architecture gate `UnitOfWorkReadScopeTests.NoGovernedField_IsDereferencedAnywhere_InsideOrOutsideALambda` (REQ-UOW-36/37) requires governed constructor parameters on every service that calls `ExecuteReadAsync`, so removing them fails it, and allow-listing would weaken an approved gate. 3.3b keeps the plan's scope (record shrink only; constructor unchanged). Dead injected fields stay under READ-SCOPE open item **F4** (Helder). 3.3b ran after 3.2a's merge (shared test files).
  - **Produces:** `public sealed record SongResolution(ResolutionKind Kind, int? ExactMatchSongId, IReadOnlyList<SongMatch> FuzzyCandidates);` no `FieldDiff`.
  - **Consumes:** 3.3.
  - **Risk:** Low — mechanical.
  - **Files owned:** `Domain/Resolution/ResolutionResults.cs`, `Services/SongResolutionService.cs`, `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (constructions `:189,:218,:248,:1150`)
  - **Demo:** census finds no `FieldDiff`/`TargetHasManualEdits`; suite green, count unchanged.
  - **Review lane:** Standard

- [x] **3.2b Remove `hasManualEdits` from `UpdateSongAsync` (production + unit tests)** [SEQUENTIAL — after 3.2a, 3.3]
  - **Produces:** a new `UpdateSongAsync` overload without `hasManualEdits`; the old one kept, `[Obsolete]`, forwarding, so the build stays green; writer `SongService.cs:172` removed.
  - **Files owned:** `ISongService.cs`, `SongService.cs`, `SongFormViewModel.cs` (call site `:606`), `SongServiceTests.cs`, `SongFormViewModelTests.cs` (6 mock setups)
  - **Risk:** Low — mechanical. **Demo:** build 0 errors, 0 new warnings outside the 2 files left for 3.2c.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D6) the `SongFormViewModelTests.cs:787` rewrite (`LoadSongForEdit_ApiImportedSong_LocksArtistField`, no `HasManualEdits`) moves here from 5.4, because M1 (3.6b) removes the property. Runs after 3.3b (not only 3.3).

- [x] **3.2c Move the remaining callers; delete the obsolete overload** [SEQUENTIAL — after 3.2b]
  - **Files owned:** `ISongService.cs`, `SongService.cs`, `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs`, `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
  - **Risk:** Low. **Demo:** census finds no `hasManualEdits` argument; all tests green.
  - **Review lane:** Standard

- [x] **3.4 Simplify `ArtistResolutionService` (commit side); delete `ResolutionChoice`** [SEQUENTIAL — after 3.3]
  - **Files owned:** `Domain/ServicesInterfaces/IArtistResolutionService.cs`, `Services/ArtistResolutionService.cs`, `Domain/Resolution/ResolutionChoice.cs` (delete), `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`
  - **Risk:** Low. **Tests:** delete `:200,:225,:249` (AC-3.2/3.4 → REQ-SFT-40).
  - **Demo:** build 0 errors; `AppServicesRegistrationTests` green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D7) `ResolutionChoice` is declared in `Domain/Resolution/ResolutionEnums.cs`, not a `ResolutionChoice.cs` file: 3.4 removes the enum from that file and deletes the file only if it becomes empty.

- [ ] **3.5 Drop `HasManualEdits` from read models and projections** [SEQUENTIAL — after 3.2c, 3.3]
  - **Files owned:** `Contracts/DTOs/List/SongListItemDto.cs`, `Contracts/DTOs/List/ArtistListItemDto.cs`, `Domain/ReadModels/ArtistListItem.cs`, `Infra/Repository/SongRepository.cs`, `Infra/Repository/ArtistRepository.cs` (+ `CatalogRepository.cs` projection in 3.5b if the test census adds files)
  - **Risk:** Low — no XAML binds these (findings § 3.3/3.4).
  - **Demo:** Songs and Artists lists render unchanged.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D5) split into 3.5a (song side) and 3.5b (artist side): `CatalogRepository.cs:41-48` also projects `SongListItemDto.HasManualEdits`, and positional constructions exist in three test files, so 3.5 as written could not build. 3.5 itself is superseded by the two entries below.

- [x] **3.5a Drop `HasManualEdits` from the song read model and projections** [after 3.2c, 3.3b] *(split, plan § 0.3 D5)*
  > **Spec updated 2026-11-04:** Files owned extended by the orchestrator to `MyVocaList.Tests/Unit/Services/CatalogServiceTests.cs` and `SongServiceTests.cs` — three target-typed `new(...)` `SongListItemDto` constructions evaded the plan's `SongListItemDto(` census (lesson: censuses for record-shape changes must also catch target-typed `new(`).
  - **Files owned:** `Contracts/DTOs/List/SongListItemDto.cs`, `Infra/Repository/SongRepository.cs`, `Infra/Repository/CatalogRepository.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`
  - **Consumes:** 3.2c, 3.3b. **Risk:** Low. **Demo:** Songs list renders unchanged. **Review lane:** Standard

- [x] **3.5b Drop `HasManualEdits` from the artist read models and projections** [P with 3.5a] *(split, plan § 0.3 D5)*
  - **Produces:** `public record ArtistListItem(int Id, string Name, string ExternalProvider, int CatalogCount);`
  - **Files owned:** `Contracts/DTOs/List/ArtistListItemDto.cs`, `Domain/ReadModels/ArtistListItem.cs`, `Infra/Repository/ArtistRepository.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`
  - **Consumes:** 3.2c, 3.3b. **Risk:** Low. **Demo:** Artists list renders unchanged. **Review lane:** Standard

- [x] **3.6a Remove the `Artist.HasManualEdits` writer** [SEQUENTIAL — after 3.5]
  - **Files owned:** `Services/ArtistService.cs` (`:107`), `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs` (only if an assertion reads the flag; none found 2026-09-25)
  - **Risk:** Low. **Demo:** build and tests green.
  - **Review lane:** Standard

- [x] **3.6b M1: drop the `HasManualEdits` columns** [SEQUENTIAL — after 3.6a, G3]
  - **Files owned:** `Domain/Entity/Song.cs`, `Domain/Entity/Artist.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/EntityEFConfig/ArtistConfiguration.cs`, `Infra/Migrations/*` (M1)
  - **Risk:** Medium — SQLite table rebuild.
  - **Tests (B):** migration test on a temp file: columns absent after `MigrateAsync`; existing rows survive.
  - **Demo:** app upgrades an existing database and lists all songs.
  - **Review lane:** Elevated
  > **Spec updated 2026-09-26:** (D10) owns 6 files (4 one-line model deletions + `Migrations/*` + new `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs`); splitting would leave a non-building commit — accepted. (D11) every migration task (3.6b, 3.7, 3.8) is Part A (implementor: Red tests, model edits left uncommitted) → scaffold (orchestrator: `dotnet ef migrations add`) → Part B (implementor: check generated Up/Down first, `blocked: stale snapshot` on any surprise; Green; one commit with model + migration).

- [x] **3.7 M2: unique `(ExternalProvider, ExternalId)`** [SEQUENTIAL — after 3.6b]
  - **Files owned:** `SongConfiguration.cs`, `Infra/Migrations/*` (M2), `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (new)
  - **Risk:** Medium — data fix. **Tests (B):** migrate to M1, seed two songs with one identity, migrate → lowest Id keeps it, other row intact; duplicate insert then fails.
  - **Demo:** migration test green.
  - **Review lane:** Elevated

- [x] **3.8 M3: FTS5 `SongSearch` + `ArtistSearch`, 7 triggers, RETURNING off** [SEQUENTIAL — after 3.7, **G5**]
  - **Files owned:** `Infra/Migrations/*` (M3), `SongMigrationTests.cs`, `SongConfiguration.cs`, `ArtistConfiguration.cs` (`ToTable(tb => tb.UseSqlReturningClause(false))`, `design.md` § 6.2 consequence 3)
  - **Risk:** High — raw SQL; a later table rebuild drops the triggers.
  - **Tests (B):** after `MigrateAsync`: both tables + 7 triggers exist; backfill counts = song and artist counts; insert / title update / artist create, rename, delete / `ExecuteDelete` keep both in step (REQ-SFT-34); EF insert and update of a Song and an Artist succeed after migrating.
  - **Demo:** migration tests green; the existing library is searchable.
  - **Review lane:** Architectural
  - **Orchestrator step:** add to `constraints-reference.md § EF Core / SQLite`: "Any migration that rebuilds Songs or Artists must recreate the FTS triggers in the same migration."
  > **Spec updated 2026-11-04:** merged `eca9f708` (Verifier CONDITIONAL PASS); orchestrator step done (constraints-reference.md). **Follow-up (Verifier W1, not blocking):** `ArtistDelete_CascadeRemovesSongSearchRows` deletes EF-tracked songs, so SQLite's own `ON DELETE CASCADE` → `SongSearch_ad` path has no regression test (the Verifier probed it correct in SQLite). Add a test that seeds songs by raw SQL / a fresh context and deletes the artist via `ExecuteDeleteAsync` — candidate for the next task that owns `SongMigrationTests.cs` (none planned) or a small dedicated task; Helder to place it. **Helder 2026-11-04: not for the MVP — deferred to a future version (raw artist deletes outside EF do not happen in the MVP).**

- [x] **3.9 Title-field search query** [SEQUENTIAL — after 3.8]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** `FindByTitleAndArtistNamesAsync` takes `(Title, ArtistName, Version)` triples (REQ-SFT-05(b) amended); its two tests become `…CollatedTripleEmptyVersion_Returns` and `…VersionMustMatch`.
  > **Spec updated 2026-11-04 (orchestrator, implementor blocked: spec gap):** owned files extended to `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (its private `ThrowOnAddSongRepository : ISongRepository` decorator gains the three forwarding members only — no assertion change), and `SongRepository` gains an optional `ILogger<SongRepository>? logger = null` constructor parameter (defaulting to `NullLogger<SongRepository>.Instance`) so the index-missing path can log at Error while the existing `new SongRepository(_db)` test call sites keep compiling; DI supplies the logger.
  > **Spec updated 2026-11-04 (3.9 merge `f3770a83`, Verifier CONDITIONAL PASS):** (W1) `FtsQueryBuilder` / `BuildTrigramMatch` are `public`, not `internal` as planned — Infra has no `InternalsVisibleTo` for the test project and the plan's own tests call the builder; accepted. (W2) `FindByTitleAndArtistNamesAsync` runs one collated query per triple (dedupe by SongId) inside one read scope instead of a single batch statement; accepted for <= ~25 triples. **Follow-ups:** (W3) the index-missing filter matches any "no such table" message — tighten to `SongSearch` when a task next owns `SongRepository.cs` (3.9c/3.10 do not; candidate: 5.3b or a later Infra task); (W4) no test asserts the Error log on that path. **For 3.10:** hits do not say which triple they matched — pair them back to Deezer rows via DB collation, never C#-side folding (HARD RULE).
  - **Produces:** `SongSearchHit`; `SearchTitleCandidatesAsync`, `FindByExternalIdsAsync`, `FindByTitleAndArtistNamesAsync`.
  - **Files owned:** `Domain/ReadModels/SongSearchHit.cs`, `ISongRepository.cs`, `SongRepository.cs`, `MyVocaList.Tests/Integration/Repositories/SongRepositorySearchTests.cs` (new, uses `MigrateAsync`)
  - **Risk:** Medium. **Tests (B):** § 6.3 corpus subset: "dont cry"/"don cry"/"guns cry" include "Don't Cry"; "nao sei" hits; 2-char query uses the prefix path; `"` in a query is escaped.
  - **Demo:** repository tests green.
  - **Review lane:** Elevated

- [x] **3.9b Artist-name candidate query** [P with 3.9 — after 3.8]
  - **Produces:** `IArtistRepository.SearchNameCandidatesAsync` (FTS `ArtistSearch`, prefix fallback under 3 chars).
  - **Files owned:** `Domain/RepositoryInterface/IArtistRepository.cs`, `Infra/Repository/ArtistRepository.cs`, `MyVocaList.Tests/Integration/Repositories/ArtistRepositorySearchTests.cs` (new, uses `MigrateAsync`)
  - **Risk:** Medium. **Tests (B):** "Metalica" retrieves "Metallica"; "beatles" retrieves "The Beatles"; accents folded.
  - **Demo:** repository tests green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D9) runs **after** 3.9, not `[P]`: 3.9 creates `Infra/Repository/FtsQueryBuilder.cs`, which 3.9b consumes.
  > **Spec updated 2026-11-04 (3.9b merge `02fc76b7`, Verifier PASS):** optional `ILogger<ArtistRepository>? logger = null` (same shape as 3.9); index-missing filter narrowed to "no such table" + "ArtistSearch". Follow-ups (minor): `RespectsTake` asserts `<= 1` (passes on empty — seed two matches and assert `== 1` when a task next owns `ArtistRepositorySearchTests.cs`); user-typed `%`/`_` act as LIKE wildcards in the prefix path (same as songs, "as today").

- [ ] **3.9c `ArtistResolutionService` uses the FTS pool** [SEQUENTIAL — after 3.9b, 3.4]
  - **Produces:** fuzzy tier reads `SearchNameCandidatesAsync`; artist `GetFuzzyCandidatePoolAsync` and `DerivePrefixToken` (artist side) deleted.
  - **Files owned:** `Services/ArtistResolutionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`, `IArtistRepository.cs`, `ArtistRepository.cs`, `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryResolutionTests.cs`
  - **Risk:** Medium. **Tests (A, Red first):** "Metalica" → `FuzzyCandidates` containing "Metallica" (score ≥ 0.82); rewrites/deletions per § 10.1.
  - **Demo:** service tests green.
  - **Review lane:** Elevated

- [ ] **3.10 `SongService.SaveFromFormAsync`: every Save decision in the Service** [SEQUENTIAL — after 3.2c, 3.3, 3.9c, G4 Q3/Q4]
  - **Produces:** `SongFormSaveRequest`, `SongFormSaveOutcome` (+ `ArtistDecision`, `SongDecision`, `SaveField`), `ISongService.SaveFromFormAsync` (`design.md` § 3.5). Writes only through `CreateSongWithUrlsAsync`/`UpdateSongAsync`.
  - **Files owned:** `Domain/Resolution/SongFormSaveRequest.cs` (new), `Domain/Resolution/SongFormSaveOutcome.cs` (new), `ISongService.cs`, `SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceSaveFromFormTests.cs` (new)
  - **Risk:** High — the constitutional business-logic boundary; CATALOG-PROP shares the file.
  - **Tests (A, Red first):** REQ-SFT-52 (no second artist: an exact collated match is reused, never created), REQ-SFT-18 (collated reuse, Deezer-filled artist included), REQ-SFT-19 (`ArtistChoiceNeeded` for "Metalica"), REQ-SFT-22/23 (`SongChoiceNeeded` exact/fuzzy), REQ-SFT-25 (new artist + held identity → `SongChoiceNeeded`), REQ-SFT-26, REQ-SFT-41 regression (the artist candidate never carries the song identity), decisions skip the right step, edit mode runs no song guard, `Failed` fields.
  - **Demo:** service tests green.
  - **Review lane:** Architectural

## Phase 4 — Services (search)

- [ ] **4.1 Deezer plain query + failure propagation** [P with Phase 3 Infra — after G1]
  - **Files owned:** `Services/IMusicMetadataProvider.cs`, `Services/DeezerProvider.cs`, `Services/MusicBrainzProvider.cs` (delete if Q1 = drop), `MyVocaList.Tests/Unit/Services/DeezerProviderTests.cs` (new, fake `HttpMessageHandler`)
  - **Risk:** Medium. **Tests (B):** URL is `search/track?q=dont%20cry` with no qualifiers; artist URL `search/artist?q=`; HTTP 500, bad JSON and **HTTP 200 with an `error` object** throw; caller cancellation propagates.
  - **Demo:** provider tests green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D8) split into 4.1a and 4.1b (MusicBrainz deletion also needs its two DI lines, and `MusicProviderException` is a new file). 4.1a waits for 1.6. 4.1 itself is superseded by the two entries below.

- [x] **4.1a Deezer plain query + failure propagation** [after 1.6, G1] *(split, plan § 0.3 D8)*
  - **Files owned:** `Services/IMusicMetadataProvider.cs`, `Services/MusicProviderException.cs` (new), `Services/DeezerProvider.cs`, `MyVocaList.Tests/Unit/Services/DeezerProviderTests.cs` (new)
  - **Consumes:** 1.6. **Risk:** Medium. **Tests (B):** plain `search/track?q=` / `search/artist?q=`; HTTP 500, bad JSON, HTTP 200 error body throw; cancellation propagates.
  - **Demo:** provider tests green. **Review lane:** Standard
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** also owns `Contracts/DTOs/MusicSearchResultDto.cs` (trailing `string? Version = null`); the provider maps `title_short` (fallback `title`) and the normalised `title_version` (REQ-SFT-12); +3 tests. The provider does not check connectivity; the Q7 offline/unavailable split lives in 4.1c + 4.2/6.1.
  > **Spec updated 2026-11-04 (4.1a merge `bc81f9e4`, Verifier CONDITIONAL PASS):** the implementor's extra `{}` stripping (beyond REQ-SFT-12's `(`/`[`) was removed by orchestrator ITF `de13e317` before merge. Interim: the provider now throws; the current `SongSuggestionService`/`ArtistSuggestionService` already catch and return `[]`, so nothing reaches the UI before 4.2/6.1 map failures to `Unavailable`. A Deezer failure is logged twice (provider + service) until then — cosmetic.

- [x] **4.1c Network status abstraction** [P with 4.1a — sole writer of `MauiProgram.cs` in flight; after D21 approval] *(new 2026-11-04, plan § 0.3 D21)*
  - **Produces:** `INetworkStatus` (Domain), `RemoteSearchStatus { Ok, Offline, Unavailable }` (Contracts), `MauiNetworkStatus` (app project), registrations in `MauiProgram.cs` (REQ-SFT-53, `design.md` § 3.6).
  - **Files owned:** `Domain/ServicesInterfaces/INetworkStatus.cs` (new), `Contracts/DTOs/Suggestions/RemoteSearchStatus.cs` (new), `MyVocaList/UI/Services/MauiNetworkStatus.cs` (new), `MyVocaList/MauiProgram.cs`, `MyVocaList.Tests/Unit/Services/MauiNetworkStatusTests.cs` (new)
  - **Risk:** Low. **Tests:** Level B theory for the pure access-level mapping (REQ-SFT-53); the rest Level C; airplane-mode check in 7.2. **Review lane:** Standard

- [x] **4.1b Delete `MusicBrainzProvider` (Q1 = drop)** [after 4.1a] *(split, plan § 0.3 D8/D18)*
  - **Files owned:** `Services/MusicBrainzProvider.cs` (delete), `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs` (new, consumed by 6.1)
  - **Consumes:** 4.1a. **Risk:** Low. **Demo:** build 0 errors; the BUG-082 DI test passes with Deezer only. **Review lane:** Standard
  > **Spec updated 2026-11-04 (D21):** also after 4.1c; `ArtistRemoteSearchResult(Remote, RemoteSearchStatus Status)`.
  > **Spec updated 2026-11-04 (4.1b merge `1aee99f6`, Verifier PASS):** dead `"MusicBrainz"`-first branches remain in `SongSuggestionService.cs:19/98` and `ArtistSuggestionService.cs:18/152` (+ test literals in `ArtistSuggestionServiceTests.cs`); harmless (falls through to Deezer). **Retirement is explicit:** 4.2/4.3 remove the song-side `MusicBrainzProviderName` branch and literals; 6.1 removes the artist-side one and the `ArtistSuggestionDto.cs:10` doc example.

- [ ] **4.2 `SongSuggestionService` local and remote title search** [SEQUENTIAL — after 2.2, 3.9, 4.1]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** also after 4.1c; `SongRemoteSearchResult.Status` (`RemoteSearchStatus`) replaces `RemoteUnavailable`; `INetworkStatus` ctor dependency — offline → no Deezer call, `Offline` (REQ-SFT-53); dedup (b) uses (title_short, artist, version) triples; +5 tests (plan § Task 4.2).
  - **Produces:** `SearchTitleLocalAsync`, `SearchTitleRemoteAsync`, `SongRemoteSearchResult`; `SongSuggestionDto.Version`/`LocalArtistName`; ranking, dedup (a)/(b) with promotion, 3,000 ms bound; tier (c) and the C# re-filters deleted.
  - **Files owned:** `Services/ISongSuggestionService.cs`, `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`, `Contracts/DTOs/Suggestions/SongRemoteSearchResult.cs` (new), `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`
  - **Risk:** High — core behaviour. **Tests (A, Red first):** REQ-SFT-04/05/06/08/09/30–32 at service level; local call makes no provider call (REQ-SFT-07); rewrites/deletions per § 10.1.
  - **Demo:** service tests green.
  - **Review lane:** Elevated
  > **Spec updated 2026-09-26:** (D16) consumes `SearchConstants.RemoteSearchTimeoutMilliseconds` (2.2) for the 3,000 ms bound. (D1) returns one list: local, then promoted, then remote (no append capability).

- [ ] **4.3 Retarget threshold tests; register the service** [SEQUENTIAL — after 4.2]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** DI tests resolving the service add `Mock.Of<INetworkStatus>()` (registered in `MauiProgram.cs`, not `AddAppServices`).
  - **Files owned:** `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`, `ISongSuggestionService.cs` (remove `GetLocalAsync`/`GetRemoteAsync`)
  - **Risk:** Low. **Demo:** `ISongSuggestionService` resolves from `AddAppServices`.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D12) also owns `Services/SongSuggestionService.cs` (removing `GetLocalAsync`/`GetRemoteAsync` from the interface also removes their implementation; 5 files).

## Phase 5 — UI (Song form) [after G2; one XAML file per task, build between]

- [x] **5.1 VM: resolution sheet → Open existing; merge sheet removed** [SEQUENTIAL]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (merge sheet deleted, labels), `MergeFieldRow` file (delete; census)
  - **Consumes:** `INavigationService`. **Risk:** Medium.
  - **Tests (A, Red first):** Open existing navigates `../song-form?songId=N`; delete `:707` (AC-4.2).
  - **Demo:** saving a duplicate offers Open existing, which opens the stored song.
  - **Review lane:** Elevated
  - *Note:* runs before 3.3 (it removes the last `CommitAsync` caller). Its Save-flow code is replaced again in 5.3.
  > **Spec updated 2026-09-26:** (D2) runs before G2: it consumes no component surface. (D13) also owns `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` (adds `services.AddSingleton(Mock.Of<INavigationService>());` to the SongFormViewModel graph test; Level C).
  > **Spec updated 2026-11-04:** also owned `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (merge-sheet sync/handlers removed; the page did not compile otherwise). VM + XAML + code-behind were built together because the merge sheet's compiled bindings broke the one-file-then-build order (Verifier: acceptable). Merged `f687e72d` 2026-11-04; Verifier PASS; develop 668/668.

- [ ] **5.2 VM: title search, picks, online-search note** [SEQUENTIAL — after 4.3, 5.1, G4 Q16]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** `TitleSupportingText` maps `Status` to the two approved notes (REQ-SFT-06); a remote pick sets Version from Deezer (REQ-SFT-12); remote option supporting text "artist · version · Deezer" (REQ-SFT-04); +4 tests.
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** High. **Tests (A, Red first):** REQ-SFT-02…14 (option supporting texts incl. "In your library", local options before remote or the Q16 fallback, `TitleSupportingText` set/cleared, local pick → navigate, discard sheet, remote pick fills + locks, local artist match, X clears pending identity).
  - **Demo:** VM tests green.
  - **Review lane:** Elevated
  > **Spec updated 2026-09-26:** (D1) Q16 settled: `TitleOptionSource` awaits local, then remote (≤ 3,000 ms), and returns one list. (D2) runs before G2 (needs only `IMenuOptionSource`/`MenuOptionSource`, ACF T3 `83065e07`). (D19) 5.2 also creates `ArtistOptionSource` (`IMenuOptionSource` over the existing local artist search, local only, REQ-SFT-16).

- [ ] **5.3 VM: Save maps service outcomes to sheets** [SEQUENTIAL — after 5.2, 3.10]
  - **Produces:** `SaveCommand` builds `SongFormSaveRequest` and maps `SongFormSaveOutcome`; artist-match and resolution sheets re-invoke with the user's decision; `ISongResolutionService` removed from the VM constructor; old decision code deleted.
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** High. **Tests (A, Red first):** one test per outcome kind → the right sheet/field/navigation; each sheet action re-invokes with the right decision; no write call other than `SaveFromFormAsync` (REQ-SFT-46/51). Rewrites of `:174,:208,:238` per § 10.1.
  - **Demo:** VM tests green.
  - **Review lane:** Architectural

- [ ] **5.3b Delete the forwarding `CreateSongWithUrlsAsync(int, …)` overload** [SEQUENTIAL — after 5.3, G3] *(new, plan § 0.3 D3)*
  - **Files owned:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, and the remaining old-overload callers found by census (`SongResolutionService.cs`, `Bug068RegressionTests.cs` if still present)
  - **Consumes:** 5.3. **Risk:** Low. **Demo:** census finds no call to the obsolete overload; build 0 errors, no `CS0618`. **Review lane:** Standard

- [ ] **5.4 VM: edit-mode title and hydration** [SEQUENTIAL — after 5.3]
  - **Files owned:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`
  - **Risk:** Low. **Tests (A, Red first):** in edit mode Title is editable (no read-only flag) for API-sourced **and** manual songs, and an edited title is sent to `UpdateSongAsync` with `null` external identity (keep); `IsTitleSearchEnabled` false in edit mode; rewrite `:787`; stale docstring `:433` fixed.
  - **Demo:** VM tests green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D2) runs before G2. (D6) the `:787` rewrite moved to 3.2b.

- [ ] **5.5 Page: field order, Title and Artist Fields** [SEQUENTIAL — after 5.4]
  - **Files owned:** `SongFormPage.xaml`
  - **Risk:** Medium (XAML). Title and Artist are `AutocompleteDXField` (`UI/Components/TextFields/`); Title binds `IsSearchEnabled` and `SupportingText` to the VM (`IsReadOnly` unused).
  - **Demo:** New Song shows Title first as a search box; Edit Song shows Title as editable text with no search menu, for every song.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D17) 5.5 also owns `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (former 5.7): replacing the Artist `AutoCompleteEdit` breaks the build until the code-behind glue is deleted (XAML → build → code-behind). Waits for ACF T4 (G2).

- [ ] **5.6 Page: sheets (discard, near-duplicate artist, resolution)** [SEQUENTIAL — after 5.5]
  - **Files owned:** `SongFormPage.xaml`
  - **Risk:** Medium. **Demo:** each sheet opens from its trigger and Back dismisses it with no save.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** waits for ACF T4 (G2) via 5.5.

- [CANCELLED: folded into 5.5 (plan § 0.3 D17)] **5.7 Code-behind: delete the Song autocomplete glue** [SEQUENTIAL — after 5.6]
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Risk:** Low. **Demo:** REQ-ACF-11 check: no `OnArtistItemsRequested`/`OnArtistSelectionChanged`/`OnArtistUnfocused`/`OnArtistClearIconClicked`, no `artistEdit.SelectedItem`.
  - **Review lane:** Standard

## Phase 6 — Artist form name search (confirmed, fifth round)

- [ ] **6.1 `ArtistSuggestionService` local and remote name search; register** [SEQUENTIAL — after 4.1, 4.3]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** `ArtistRemoteSearchResult.Status`; `INetworkStatus` ctor dependency; offline → no call, `Offline`; +1 test.
  - **Produces:** `SearchNameLocalAsync`, `SearchNameRemoteAsync` + `ArtistRemoteSearchResult`; similarity dedup tier (c) and `FilterSimilar` deleted (`design.md` § 4.1).
  - **Consumes:** 4.1 (`DeezerProvider.SearchArtistsAsync` throws on failure; URL `search/artist?q=`).
  - **Files owned:** `Services/IArtistSuggestionService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionThresholdTests.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
  - **Risk:** Medium. **Tests (A, Red first):** local first, collated-name dedup, status on failure, thresholds; deleted tests mapped to REQ-FORMUX-03(c)/-10 → REQ-SFT-47/48.
  - **Demo:** service tests green; `IArtistSuggestionService` resolves.
  - **Review lane:** Elevated
  > **Spec updated 2026-09-26:** (D20) runs **after 3.9b** too: `SearchNameLocalAsync` retrieves from `IArtistRepository.SearchNameCandidatesAsync` (FTS5 `ArtistSearch`) ranked by `ISimilarityScorer.RankScore`, take 5 (§ 6.4 exception extended to `ArtistSuggestionService`). (D18) `ArtistRemoteSearchResult` is created in 4.1b.
- [ ] **6.2 `ArtistFormViewModel`: Name option source, local pick opens the artist, Deezer pick fills the name** [SEQUENTIAL — after 6.1, 1.2, G2, Q10]
  > **Spec updated 2026-11-04 (plan § 0.3 D21, approved by Helder 2026-11-04):** `NameSupportingText` maps `Status` to the artist offline note (Q18, approved) / the unavailable note; +1 test.
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, the Artist-form VM test file
  - **Risk:** Medium. **Tests (A, Red first):** REQ-SFT-47…50 (navigation via `INavigationService`; `NameSupportingText` on remote failure).
  - **Demo:** VM tests green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** (D2) runs before G2 (VM only; consumes `IMenuOptionSource`).
- [ ] **6.3 `ArtistFormPage`: Name as the Field** [SEQUENTIAL — after 6.2]
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `ArtistFormPage.xaml.cs`
  - **Risk:** Medium. **Demo:** typing "metalica" on New Artist lists "Metallica" (local, "In your library") and Deezer artists; tapping "Metallica" opens it.
  - **Review lane:** Standard
  > **Spec updated 2026-09-26:** waits for ACF T4 (G2).

## Phase 7 — Close-out

- [ ] **7.1 Spec ritual + traceability matrix** (orchestrator): fill `requirements.md` § Traceability; task-log matrix; README frontmatter status; `backlog_gen.py regen`; `.sln` check.
- [ ] **7.2 On-device E2E gate — Helder** (Android). Stop and hand off; nothing merges before Helder's pass.
  1. Upgrade an existing install: songs, artists and YouTube links intact (M1–M3).
  2. New Song: Title is first. Type "don cry" → "Don't Cry" local first, "Guns N' Roses · In your library"; version rows distinguishable; local options appear before Deezer's.
  3. Tap the local row → the edit form of that song opens; Back returns to the Songs list.
  4. Type "sweet chld o mine" → Deezer rows marked "Deezer". Pick one not in the library → Title and Artist filled and locked; X on Title empties it.
  5. Pick a Deezer song whose artist is not local → Save → song and artist both created; artist's catalog behaviour per CATALOG-PROP if landed.
  6. Airplane mode → local options appear at once (no 3 s wait), and the text under Title reads "You're offline — showing songs from your library only."; Save works. Airplane mode off → the next query clears the note (REQ-SFT-06/53, D21).
6b. Type "creep live" → pick the Deezer "Creep" row whose supporting text reads "Radiohead · Live · Deezer" → Title "Creep", Version "Live"; Save stores both (REQ-SFT-04/12, D21).
  7. Type "Metalica" as artist (with "Metallica" stored) → Save → near-duplicate sheet offers "Use 'Metallica'"; Use and Create both behave; Back saves nothing. Repeat with a Deezer pick whose artist is a near-duplicate of a local one.
  8. Edit an imported song: fix its Title → saves; reopen → new title, still linked to Deezer (same YouTube links). Edit a manual song's Title to a title that artist already has with the same Version → duplicate message. Change Version of a title that exists → saves.
  12. Type an existing artist's name in another case/accent ("metallica") → Save → the song attaches to "Metallica"; no second artist appears in the Artists list.
  9. Save a duplicate by hand → Open existing / Save as new version.
  10. No "Search music database" row on Song or Artist forms; "Search YouTube (API)" unchanged.
  11. New Artist: type "metalica" → local "Metallica" first; tap it → its edit form opens. Pick a Deezer artist → Name filled and locked; Save creates it; a collated duplicate shows the existing message.

## Coordination

- **G3 two-way rule (2026-09-26):** do not dispatch a task touching `SongService.cs`/`ISongService.cs`/`Infra/Migrations/` while a CATALOG-PROP branch (or, for migrations, a BUG-080 migration branch) is live; and CATALOG-PROP/BUG-080 must not dispatch such a task while a `task/sft-*` branch owning those files (3.1, 3.2a–c, 3.10, 5.3b, 3.6b, 3.7, 3.8) is live. Check LEDGER + `git worktree list` + `git branch -a` (plan § 0.2.2).

- **CATALOG-PROP:** both change `SongService.cs` (3.1/3.2 here) and add migrations. Single writer:
  whichever item's task runs second rebases and re-runs the other's tests. Their propagation hooks
  go inside the same two methods; the new artist already has an id after `FlushAsync` (`design.md` § 13).
- **Shared Field item:** SongForm migration is handed to this item (its `design.md` § 3.2). This item
  uses its `SupportingText` (REQ-ACF-12c) and asks for one capability, appending options (Q16).
- **BUG-082 (G1):** registered; 4.1–4.3, 6.1 and E2E steps 4–6 depend on it.

## Follow-ups (not this item)

- `ISongService.CreateSongAsync` has no production caller after 3.3 (title-only uniqueness); decide
  delete vs align.
- YouTube search (`YouTubeSearchPage`, "Search YouTube (API)"): out of scope, pending a separate
  discussion on the YouTube API-key requirement.
- Frozen `UI/Components/AutocompleteField/` deletion (dependency item's follow-up).

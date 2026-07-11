# Artist & Song Form UX Redesign — Tasks

> Feature folder: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`
> Spec: `requirements.md` (REQ-FORMUX-01…33) · Design: `design.md`
> Ordering: DRY Onion (Contracts/Infra → Services → ViewModels → UI → cleanup). No Domain schema change,
> **no migration** (Artist already has `ExternalId`/`ExternalProvider` — verified 2026-07-10).
> XAML rule: one file → build → fix → next (never batch UI edits).
>
> **⏸ PARKED 2026-07-10 — status & merge map:** feature is 🔵 Deferred. All progress is on branch
> `feature/form-ux-redesign` (pushed). Tasks 1–5 + Phase 0 are merged to `develop`; **Task 6
> (REQ-FORMUX-07) is done but lives ONLY on `feature/form-ux-redesign` — NOT yet merged to develop.**
> Next task = *DI registration for suggestion services*. Full merge map + resume steps: `handoff.md`.
>
> **🔗 NEW DEPENDENCY 2026-07-11 (Helder) — apply before resume.** This feature now sits under *Form & Autocomplete UX Overhaul* (BACKLOG) and gains two HARD predecessors:
> 1. **DevCycleCraft ① — Autocomplete Mobile UX Pattern guideline:** phone = full-screen expansion (entire page + search AppBar + filter term docked at the very screen bottom + results fill the rest); desktop = keep exposed-dropdown. **Phase 2 (AutocompleteField change), Phase 3 (VMs), and Phase 4 (pages) are GATED on ① — do not resume those phases until ① lands.** The DI task (Phase 1, non-UI) is NOT gated and may proceed.
> 2. **DevCycleCraft ② — AutocompleteField Component Evaluation:** adjust or replace the component. **Phase 5 (ArtistPickerPage/SongPickerPage deletion) is GATED on ②** — those full-screen pick pages may be repurposed as the small-screen autocomplete component; do NOT delete until ② decides.
> Before resuming UI work, adapt this spec's `design.md`/`requirements.md` autocomplete-UI sections to the ① pattern.

## Phase 0 — Spec supersession notes (docs only)

- [x] **Add dated supersession notes to the two original requirements files** [SEQUENTIAL]
  - **Produces:** `> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — <one line>` notes on: `artists-songs/requirements.md` (at AC-10.2/10.3, AC-11.1/11.2/11.2a, AC-4.1/4.3/4.5/4.6, AC-4.7 create-path-only) and `artists-songs/song-import-resolution/requirements.md` (at AC-B8). Original text stays untouched (immutable history) — notes only.
  - **Consumes:** `requirements.md § Supersession` (this feature)
  - **Risk:** Low — additive doc notes
  - **Files owned:** `Docs/Management/BusinessFeatures/artists-songs/requirements.md`, `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/requirements.md`
  - **Demo:** Both originals show the dated note beside each superseded AC; git diff shows no deleted lines.
  - **Review lane:** Standard

## Phase 1 — Contracts / Infra / Services

- [x] **Suggestion DTOs (Contracts)** [P]
  - **Produces:** `ArtistSuggestionDto`, `SongSuggestionDto` records per `design.md § Interfaces`
  - **Consumes:** nothing new
  - **Risk:** Low — pure DTO records (Level C: no mandatory test; no-test decision documented in task-log)
  - **Files owned:** `Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`
  - **Demo:** Solution builds; both records match the `design.md` shapes exactly.
  - **Review lane:** Standard · TDD Level C

- [x] **Repository collation batch lookups + integration tests** [P — parallel with DTOs, different files]
  - **Produces:** `IArtistRepository.GetByNamesCollatedAsync` + `ISongRepository.GetByTitlesCollatedAsync` + implementations; integration tests (real SQLite, accent/case cases)
  - **Consumes:** nothing new
  - **Risk:** Medium — collation query must use `EF.Functions.Collate`, single batch query (no per-candidate round-trips), no C# normalization (HARD RULE)
  - **Files owned:** `Domain/RepositoryInterface/IArtistRepository.cs`, `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/ArtistRepository.cs`, `Infra/Repository/SongRepository.cs`, new integration test file(s)
  - **Demo:** Integration test proves "Café" resolves against stored "cafe" via one SQL query.
  - **Review lane:** Standard · TDD Level B

- [x] **IArtistSuggestionService + ArtistSuggestionService (TDD Level A)** [SEQUENTIAL — after DTOs/repos]
  - **Produces:** `Services/IArtistSuggestionService.cs`, `Services/ArtistSuggestionService.cs` per `design.md § Interfaces`; unit tests covering: local max-5, remote 3-tier dedup order (external-id → collation name → similarity), `FilterSimilar` threshold branches (≥ 0.82 non-exact only), provider failure → empty list + log, cancellation
  - **Consumes:** suggestion DTOs, repo methods, `IMusicMetadataProvider`, `ISimilarityScorer`, `SimilarityConstants`
  - **Risk:** High — core business logic; every dedup branch is a test case
  - **Files owned:** the two service files + `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`
  - **Demo:** `dotnet test` shows all dedup/threshold branch tests green; providers fully mocked.
  - **Review lane:** Elevated · TDD Level A (tests first, Red seen)

- [x] **ISongSuggestionService + SongSuggestionService (TDD Level A)** [P — parallel with ArtistSuggestionService, different files]
  - **Produces:** `Services/ISongSuggestionService.cs`, `Services/SongSuggestionService.cs`; unit tests (local title match, remote dedup, artistHint pass-through, LocalArtistId resolution for remote rows, failure → empty)
  - **Consumes:** same as above + `ISongRepository`
  - **Risk:** High — core business logic
  - **Files owned:** the two service files + `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`
  - **Demo:** `dotnet test` green for all branches.
  - **Review lane:** Elevated · TDD Level A

- [x] **ArtistService external-identity persistence fix (REQ-FORMUX-07)** [SEQUENTIAL] — done in commit `5c510e5` on **`feature/form-ux-redesign`** · ⚠️ **NOT yet merged to develop** (code lives on that branch only)
  - **Produces:** `CreateArtistAsync(string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default)` persisting both fields; unit tests: identity persisted when supplied, null when manual, existing validation untouched
  - **Consumes:** existing `Artist` entity fields (no migration)
  - **Risk:** Medium — signature change; existing callers compile via optional params
  - **Files owned:** `Domain/ServicesInterfaces/IArtistService.cs` (verified location 2026-07-10), `Services/ArtistService.cs`, `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`
  - **Demo:** Test proves a created artist row carries the supplied `ExternalId`/`ExternalProvider`.
  - **Review lane:** Standard · TDD Level A

- [ ] **DI registration for suggestion services** [SEQUENTIAL — hotspot file]
  - **Produces:** Scoped registrations in `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (`AddAppServices()`); DI-resolution regression test updated
  - **Consumes:** both suggestion services committed
  - **Risk:** Low — plumbing (Level C; no-test decision for the registration itself documented in task-log; resolution regression test covers the chain)
  - **Files owned:** `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, DI regression test file
  - **Demo:** DI-resolution test resolves `SongFormViewModel` full chain with the new services.
  - **Review lane:** Standard

## Phase 2 — Governed component (dedicated task — no bundling, HARD RULE)

> **⛔ GATED 2026-07-11 on DevCycleCraft ① + ②.** The additive change below assumes the exposed-dropdown model. Under ①, phone autocomplete is a full-screen SearchView, and ② may adjust/replace `AutocompleteField` entirely — either could change or void this task's shape. Do NOT implement until ① defines the pattern and ② concludes the component's fate.

- [ ] **[COMPONENT] AutocompleteField — remote section marker + loading-hint row (additive)** [SEQUENTIAL]
  - **Produces:** additive `AutocompleteField` capability — remote-section header row ("From music database") + loading-hint row bound to `IsRemoteLookupRunning`; `AutocompleteSuggestion` model carries a `Data` payload (the source DTO) and a section/kind marker. Purely additive (REQ-FORMUX-30) — existing consumers render identically when the new properties are unbound.
  - **Consumes:** nothing new (the DTO payload is mapped by the ViewModels in Phase 3; the component only renders `AutocompleteSuggestion`)
  - **Risk:** Architectural — governed component (2+ consumers); change MUST stay additive or STOP and escalate (`blocked: spec gap`)
  - **MD3 review:** list section header / subheader anatomy per m3.material.io (menus + lists); loading indicator row per MD3 progress-indicator-in-list guidance — record findings in task-log
  - **Consumer map:** grep `<autocomplete:AutocompleteField` before editing — expected: `PersonFormPage.xaml`, `SongFormPage.xaml` (ArtistFormPage becomes a consumer in Phase 4; verify no others)
  - **Per-consumer risk:**
    | Consumer | What could break | Verification |
    |----------|------------------|-------------|
    | PersonFormPage | rendering change when new properties unused; blur behavior change | with new properties unbound, visual + behavior identical (emulator check) |
    | SongFormPage | suggestion template change breaks existing artist rows | artist suggestions render as before until Phase 4 rewires them |
  - **Helder approval:** required before implementation — record date in task-log
  - **Additive-only gate:** if implementation cannot keep the change purely additive (REQ-FORMUX-30), STOP and escalate (`blocked: spec gap`) before editing
  - **Files owned:** `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml(.cs)`, `AutocompleteSuggestion` model file ONLY
  - **Demo:** Component test page / consumer shows local rows, a "From music database" header with remote rows beneath, and a loading-hint row while `IsRemoteLookupRunning` is true; PersonFormPage unchanged.
  - **Review lane:** Architectural

## Phase 3 — ViewModels (TDD Level A; Moq'd services; no Shell.Current in tests)

- [ ] **BUG-027 regression test (Red) + SongFormViewModel blur-clear removal + IsArtistLocked retirement** [SEQUENTIAL — first VM task]
  - **Produces:** regression test `ArtistFieldBlur_WithTypedNonMatchingText_KeepsText` (name per convention) seen to FAIL against current blur-clear code, then: blur-clear handler deleted, `IsArtistLocked` property + assignments removed, typing-clears-selection-id-only logic (REQ-FORMUX-15/16)
  - **Consumes:** nothing from Phases 1–2 (behavior deletion)
  - **Risk:** High — Critical-severity bug; Red-first is MANDATORY (bug-tracking HARD RULE)
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - **Demo:** Test run log shows the regression test failing before the change and passing after; typed artist text survives blur.
  - **Review lane:** Elevated

> **Artist-logic split refinement 2026-07-11 (Helder):** the Artist form carries several layers of complexity that must NOT ship as one task. Split the ArtistForm work so that **local autocomplete + name-entry logic ships FIRST** (part 1a: local suggestions, blur-keep, name entry), and **3rd-party-API suggestion retrieval + remote dedup/similar-warn is a SEPARATE follow-up task** (part 1b, sequential after 1a). Save-flow (part 2) stays as its own task. Apply the same "split only where complexity justifies" rule to any other over-large task in this spec when it resumes.

- [ ] **ArtistFormViewModel (part 1) — suggestion orchestration + similar-warn state** [SEQUENTIAL]
  - **Produces:** local-immediate + staggered-remote suggestion orchestration (400 ms injectable timer, cancellation on new keystroke/pick/navigation), loading-hint state, repurposed `DuplicateSuggestions` → similar-warn state fed by `FilterSimilar` (no refetch), pending-identity stash on remote pick + clear on manual edit; tests for staging, cancellation, warn state, identity stash/clear
  - **Consumes:** `IArtistSuggestionService`, DI task
  - **Risk:** High — user-facing suggestion behavior
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`
  - **Demo:** Tests prove: local rows immediate, remote appended after stagger, stale results discarded, warn state populated from cache only, manual edit after remote pick → identity cleared.
  - **Review lane:** Elevated · TDD Level A (+ Level B staging tests)

- [ ] **ArtistFormViewModel (part 2) — save-flow + confirm-sheet state machine + external-identity save path** [SEQUENTIAL — after part 1]
  - **Produces:** confirm-sheet observable state + commands (pick local → navigate edit; pick remote → fill + identity, user saves again; create), save-flow branches per `design.md § ArtistFormPage` (exact → uniqueness error; similar → sheet; none → create), `CreateArtistAsync(name, externalId, provider)` call; tests for every save-flow branch
  - **Consumes:** part 1, updated `ArtistService`
  - **Risk:** High — user-facing save logic
  - **Files owned:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs` (same files — strictly sequential with part 1, never parallel)
  - **Demo:** Tests prove: no-match → create called with identity; similar → sheet flag set, no create; exact → uniqueness error; remote-candidate pick on sheet fills form without saving.
  - **Review lane:** Elevated · TDD Level A

> **Refinement 2026-07-10 (plan review):** the single SongFormViewModel checkbox below was split into **12A** (autocomplete/autofill) and **12B** (save-resolution ladder) — granularity only, no scope change. This isolates the GAP-1-blocked save step (12B) from the unblocked autocomplete work (12A). Same two files; strictly sequential (12B after 12A), never parallel.

- [ ] **SongFormViewModel (12A) — artist + title autocomplete + remote-pick autofill** [SEQUENTIAL — after BUG-027 task]
  - **Produces:** artist entry local+remote suggestions via `IArtistSuggestionService`; title suggestions via `ISongSuggestionService`; remote title pick autofill (Title + Artist + pending external identity, nothing persisted); similar-warn state fed by `FilterSimilar` (no refetch); tests for suggestion staging, autofill state, cancellation
  - **Consumes:** both suggestion services, DI task, BUG-027 task committed
  - **Risk:** High — user-facing suggestion behavior on the song form
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - **Demo:** Tests prove local rows immediate, remote appended after stagger, remote title pick autofills Title+Artist+pending identity with nothing persisted.
  - **Review lane:** Elevated · TDD Level A

- [ ] **SongFormViewModel (12B) — artist save-resolution ladder + transparent atomic create** [SEQUENTIAL — after 12A]
  - **Produces:** save resolution (exact → auto-attach; similar → sheet; none → transparent atomic create incl. marked-for-create identity); Save routes into existing resolution/merge flow unchanged; tests per `design.md § SongFormPage` flows for every branch
  - **Consumes:** 12A committed, both suggestion services, updated `ArtistService`
  - **Risk:** High — touches the primary blocked flow (song creation). ✅ **GAP-1 RESOLVED — Option A (Helder 2026-07-10):** transparent-create routes via `ISongResolutionService.CommitAsync(CreateNew)` + post-create `_pendingRawUrls` attach through `ISongKaraokeUrlService` (URL-attach failure non-fatal). No `blocked: spec gap` remains; all branches (transparent create, exact auto-attach, similar → sheet, empty-artist validation) are dispatchable.
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (same files as 12A — strictly sequential, never parallel)
  - **Demo:** Tests prove all three artist-resolution branches + resolution-flow invocation unchanged; transparent-create test (`Save_NoMatch_...`) implemented per Option A (`CommitAsync(CreateNew)` + post-create URL attach).
  - **Review lane:** Elevated · TDD Level A

## Phase 4 — UI / XAML (one file per task; build between)

- [ ] **ArtistFormPage.xaml — AutocompleteField, strip removal, warn hint, confirm sheet** [SEQUENTIAL]
  - **Produces:** Name → `autocomplete:AutocompleteField`; "Search music database" `ListItem` row removed; `DuplicateSuggestions` block rebound as similar-warn hint; confirm `dx:BottomSheet` (code-behind Show/Close pattern, `BottomSheetTitle` style); `SafeAreaEdges="Container"` preserved
  - **Consumes:** Phase 2 component, ArtistFormViewModel task
  - **Risk:** Medium — XAML wiring; BottomSheet code-behind pattern (`dialogs-validation.md`)
  - **Files owned:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml.cs`
  - **Demo:** Emulator: typing "black sab" shows local rows instantly, remote rows under "From music database" ≈ 0.7 s after pause; no search strip; saving a similar name opens the confirm sheet.
  - **Review lane:** Standard · E2E emulator gate before To Review

- [ ] **SongFormPage.xaml — Title AutocompleteField, artist entry updates, strip removal, confirm sheet** [SEQUENTIAL — after ArtistFormPage builds green]
  - **Produces:** Title → `AutocompleteField`; artist entry `IsEnabled` lock binding removed; search strip removed; confirm sheet added; YouTube strip untouched
  - **Consumes:** Phase 2 component, SongFormViewModel task
  - **Risk:** Medium — the page hosts the resolution/merge sheets (BUG-023 pattern) — do not disturb their code-behind wiring
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Demo:** Emulator: blur keeps typed artist text; remote title pick autofills Title+Artist; save with a brand-new artist name creates song+artist in one go; no search strip.
  - **Review lane:** Standard · E2E emulator gate before To Review

## Phase 5 — Picker deletion cleanup

> **⛔ GATED 2026-07-11 on DevCycleCraft ②.** `ArtistPickerPage` + `SongPickerPage` are full-screen pick pages and are candidates to be REPURPOSED as the small-screen ("mobile") autocomplete full-screen component (per ① pattern). Do NOT delete them until ② decides. If ② repurposes them, this phase is cancelled/rewritten instead of a deletion.

- [ ] **Delete ArtistPickerPage + SongPickerPage and all wiring** [SEQUENTIAL — hotspot files: MauiProgram.cs, AppShell.xaml]
  - **Sizing exception (explicit):** this task exceeds the 5-file cap by design — deleting a dead subgraph is atomic; splitting source-deletion from route/DI/test cleanup would leave intermediate commits that do not build. The build MUST go green in this single commit.
  - **Produces:** deletion of `ArtistPickerPage.xaml(.cs)`, `SongPickerPage.xaml(.cs)`, `ArtistPickerViewModel.cs`, `SongPickerViewModel.cs`; route entries removed from `Routes.cs` + `AppShell.xaml(.cs)`; DI registrations removed; picked-message classes + their `ArtistFormViewModel`/`SongFormViewModel` handlers removed; picker VM test files deleted; DI regression tests updated
  - **Consumes:** Phases 3–4 committed (forms no longer navigate to pickers)
  - **Risk:** Medium — irreversible-action class (route removal + file deletion); authorization = Helder decision 2026-07-10 (`design.md § Key Decisions`); MUST verify `YouTubeSearchPage` + `QueueSongPickerPage` untouched (grep before/after)
  - **Files owned:** the deleted files, `MyVocaList/Navigation/Routes.cs`, `MyVocaList/AppShell.xaml(.cs)`, `MyVocaList/MauiProgram.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, affected test files, `MyVocaList.sln` (if pickers referenced in docs entries — none expected for source files)
  - **Demo:** `dotnet build` 0 errors; repo grep for `ArtistPicker|SongPicker` (excluding `QueueSongPicker`) returns only docs/history; app navigates both forms without crash.
  - **Review lane:** Elevated

## Phase 6 — Docs, guidelines, close-out

> **.sln registration status:** the three spec files of this folder were registered in `MyVocaList.sln` in the spec commit (solution folder GUID `{FA1234BC-0001-4000-8000-000000000045}`, nested under `artists-songs`). The Phase 0 supersession-note edits touch existing, already-registered files — no further `.sln` change is needed unless a task below creates a new `Docs/` file (`task-log.md` / `spec-changelog.md` must be registered when created).

- [ ] **Deprecation note in `.claude/library/search-picker-pattern.md`** [P]
  - **Produces:** dated note marking the artist/song picker portions superseded by in-field autocomplete (this feature); YouTube picker portion explicitly still valid
  - **Consumes:** Phase 5 committed
  - **Risk:** Low
  - **Files owned:** `.claude/library/search-picker-pattern.md`
  - **Demo:** File opens with the supersession note at the top of the affected sections.
  - **Review lane:** Standard

- [ ] **E2E emulator verification + BACKLOG + spec close-out** [SEQUENTIAL — final]
  - **Produces:** emulator run of the two Demo scenarios (Phase 4) + BUG-027 TEST-001 step 7 re-run; BACKLOG rows updated (Form UX Redesign → ✅; BUG-027 → ✅ Fixed; BUG-029/030/031-032 → closed-superseded per `requirements.md § Supersession`); `spec-changelog.md` created here if any post-approval spec change occurred; AC traceability matrix (REQ-FORMUX-NN → tests) completed in `task-log.md`
  - **Consumes:** all prior phases
  - **Risk:** Low — verification and bookkeeping
  - **Files owned:** `Docs/Management/BACKLOG.md`, this folder's `task-log.md` (+ `spec-changelog.md` if needed)
  - **Demo:** BACKLOG shows the feature ✅ with BUG dispositions; task-log matrix has one row per REQ-FORMUX AC.
  - **Review lane:** Standard

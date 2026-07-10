# Artist & Song Form UX Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> Feature folder: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`
> Spec: `requirements.md` (REQ-FORMUX-01…33) · Design: `design.md` · Tasks: `tasks.md` (this plan is 1:1 with it — same phases, same ordering. **Refinement 2026-07-10, plan review:** the single SongFormViewModel checkbox was split into two — Tasks 12A/12B below — granularity only, no scope change; `tasks.md` updated in the same commit)
> Created: 2026-07-10 · Spec approved by Helder 2026-07-10 (downstream gates pre-approved — recorded per task below)

**Goal:** In-field local+remote autocomplete on ArtistForm Name / SongForm Artist / SongForm Title, blur-clear removal (BUG-027), similar-match warn-before-save via confirm BottomSheet, search-strip removal, ArtistPickerPage/SongPickerPage deletion, and the ArtistForm external-identity persistence fix.

**Architecture:** No Domain schema change and **no migration** (`Artist` already has `ExternalId`/`ExternalProvider` — verified 2026-07-10). New suggestion DTOs in Contracts; one additive collation batch-lookup method per repository in Infra; two new suggestion services in the Services layer owning all lookup/dedup/similarity logic; ViewModels rewired; pages converted to `AutocompleteField` + confirm `dx:BottomSheet`; picker pages deleted.

**Tech Stack:** .NET MAUI 10 · C# 13 · CommunityToolkit.Mvvm · EF Core 10 + SQLite (`EF.Functions.Collate` + `CollationConstants.Default`) · DevExpress MAUI v25.2.4 (`dx:BottomSheet`, `dxe:TextEdit`) · xUnit + Moq (unit) / real SQLite temp DB (integration).

## Global Constraints

Every task's requirements implicitly include this section (constitutional constraints + spec thresholds):

- **Business logic in Services only** `[Unamendable]` — all dedup/similarity/save-resolution logic lives in `ArtistSuggestionService` / `SongSuggestionService` / existing services; ViewModels orchestrate and hold observable state only.
- **No native dialogs** `[Unamendable]` — confirm sheet is a `dx:BottomSheet` (ConfirmSheet-style, `BottomSheetTitle` style, code-behind Show/Close pattern per `.claude/library/dialogs-validation.md`). Never `DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync`.
- **DevExpress first** `[Unamendable]` — `AutocompleteField` builds on DX `TextEdit`; no new stock-MAUI components.
- **No C#-side string normalization** `[HARD RULE]` — all exact matching via `EF.Functions.Collate(column, CollationConstants.Default)`; never `ToLowerInvariant()`/`RemoveDiacritics()`/`*Normalized` columns.
- **MD3 terminology** — section header = MD3 list *subheader*; loading row per MD3 progress-indicator-in-list guidance.
- **SafeAreaEdges** — every touched `ContentPage` keeps/declares `SafeAreaEdges="Container"`.
- **Incremental XAML edits** — one XAML file → build → fix → next. Phases 4–5 enforce this per task.
- **English only** — all code, comments, UI text, log messages.
- Timing/matching thresholds (verbatim from `requirements.md § Validation Rules`): component debounce **300 ms** (`AutocompleteField.DebounceDelay` default) · remote stagger **400 ms** after local dispatch (≈ 700 ms after last keystroke) · min chars **2** · max **5** rows per source · similar threshold **`SimilarityConstants.DefaultThreshold` = 0.82**.
- Provider order: **MusicBrainz first, Deezer fallback on empty/error (AC-4.2)** — never both in parallel.
- Testing: risk tiers per `.claude/rules/testing.md` (Level A = full TDD Red→Green→Refactor; B = happy path + key edges; C = no mandatory test, decision documented in task-log). Every REQ-FORMUX AC → ≥ 1 test tagged `// [AC] REQ-FORMUX-NN: <criterion>`; AC traceability matrix in `task-log.md` at review.
- Subagent exit checklist applies to every task: verification-before-completion → `dotnet build` (0 errors, 3-attempt cap) → `dotnet test` (if `.cs` changed) → post-edit re-read → `.sln` registration (new `Docs/` files only) → living-spec check → task-log entry (`### Changed files` mandatory) → commit → push.
- **Test project:** `MyVocaList.Tests/MyVocaList.Tests.csproj`. Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~<ClassName>"`.

## Wave map (Rule 2 — max 4 parallel, worktrees for 2+ concurrent)

| Wave | Tasks | Parallel? |
|------|-------|-----------|
| 0 | Task 1 (supersession notes) | solo |
| 1a | Task 2 (DTOs) `[P]` + Task 3 (repo batch lookups) `[P]` | yes — disjoint files |
| 1b | Task 4 (ArtistSuggestionService) + Task 5 (SongSuggestionService) `[P]` | yes — disjoint files |
| 1c | Task 6 (ArtistService fix) → Task 7 (DI — hotspot, sequential) | no |
| 2 | Task 8 (governed component) | solo — Architectural lane |
| 3 | Task 9 (BUG-027) → Task 10 (ArtistFormVM p1) → Task 11 (ArtistFormVM p2) → Task 12A (SongFormVM autocomplete/autofill) → Task 12B (SongFormVM save-resolution ladder) | strictly sequential (Tasks 10/11 share files; Tasks 12A/12B share files; 12A after Task 9) |
| 4 | Task 13 (ArtistFormPage) → Task 14 (SongFormPage) | sequential — one XAML per build cycle |
| 5 | Task 15 (picker deletion) | solo — hotspot files |
| 6 | Task 16 (deprecation note) → Task 17 (E2E + close-out) | sequential |

*(Task numbers below are plan-local; each maps to the identically-titled `tasks.md` checkbox.)*

> **task-log.md concurrency rule (parallel waves 1a and 1b):** `task-log.md` is listed in every task's commit — two concurrent worktree subagents appending to it WILL merge-conflict. Mechanism (single, mandatory): during a parallel wave, subagents do **not** edit `task-log.md`; each returns its task-log entry text at the top of its final commit message body, and the **orchestrator appends both entries to `task-log.md` in one commit immediately after serially merging the wave's worktrees**. Sequential (solo) tasks append to `task-log.md` directly as written in their steps.

## Open spec gaps

### GAP-1 — Transparent-create path vs. pending YouTube URLs (affects Task 12B only)

- **Location:** `design.md § SongFormPage` save flow ("transparent create: artist + song persisted in ONE transaction (existing atomic-save lever)") vs. REQ-FORMUX-20 vs. BUG-009/AC-6.2 (song + buffered URLs atomic via `CreateSongWithUrlsAsync`).
- **Gap:** The named lever is `ISongResolutionService.CommitAsync(candidate, ResolutionChoice.CreateNew, null, null)` → `ResolveOrCreateArtistIdAsync` → `_songService.CreateSongAsync(...)` (verified in `Services/SongResolutionService.cs` lines 160–172). That path (a) does **not** carry the VM's `_pendingRawUrls` buffer (BUG-009), and (b) executes artist create and song create as two `SaveChangesAsync` calls on the shared scoped `AppDbContext` — not literally one transaction. The current VM NoMatch path (`CommitNewSongAsync`) instead calls `CreateSongWithUrlsAsync(SelectedArtistId!.Value, …, _pendingRawUrls)`, which requires an already-persisted artist id. No existing seam satisfies REQ-FORMUX-20 ("single transaction — a failure rolls back both") + URL atomicity + "resolution engine consumed unchanged" simultaneously.
- **Options:**
  - **Option A** — route the no-selected-artist create through `CommitAsync(CreateNew)` (spec's named lever), then attach `_pendingRawUrls` post-create via `ISongKaraokeUrlService.AddUrlAsync` (edit-mode semantics; URL attach failure non-fatal). Consequence: matches the design text literally; weakens BUG-009 URL atomicity for this one path; artist/song remain two saves inside one scoped context (same guarantee level the import flow already ships with — AC-2.5 precedent).
  - **Option B** — add an optional `IEnumerable<string> urls` parameter (default `[]`) to `ISongResolutionService.CommitAsync`, threaded to `CreateSongWithUrlsAsync`. Consequence: true song+URL atomicity, but touches the resolution engine, which `requirements.md § Out of Scope` declares consumed unchanged.
- **Recommendation:** Option A — it uses exactly the lever the approved design names, and BUG-009 buffering predates artists-created-on-save; the resolution engine stays untouched.
- **✅ RESOLVED 2026-07-10 (Helder): Option A.** The no-selected-artist transparent-create path routes through `ISongResolutionService.CommitAsync(candidate, ResolutionChoice.CreateNew, null, null)` (the approved design's named lever), then attaches the VM's `_pendingRawUrls` buffer post-create via `ISongKaraokeUrlService.AddUrlAsync` (edit-mode semantics; URL-attach failure is non-fatal — logged, save still succeeds). Accepted consequence: BUG-009 song+URL atomicity is intentionally weakened for this one path (URLs attach after the song commits, matching the AC-2.5 import-flow precedent of artist-then-song saves inside one scoped `AppDbContext`); the resolution engine is NOT modified (Out-of-Scope honored). This is a plan/implementation refinement of REQ-FORMUX-20 — no acceptance criterion changes; the design's "existing atomic-save lever" text is unchanged and consistent with Option A.
- **Blocking:** ~~Task 12B Step 5 blocked until Helder confirms A or B.~~ **CLEARED** — Option A confirmed. Task 12B is fully unblocked; implement Step 6 (`Save_NoMatch_TransparentCreateWithMarkedForCreateIdentity`) per Option A above. No `blocked: spec gap` remains on any task.

No other spec gaps found: interfaces, thresholds, flows, deletion list, and edit-mode behavior (REQ-FORMUX-32/33) are all fully specified.

---

## Phase 0 — Spec supersession notes (docs only)

### Task 1: Add dated supersession notes to the two original requirements files

**tasks.md:** "Add dated supersession notes to the two original requirements files" `[SEQUENTIAL]` · Risk Low · Review lane Standard

**Files:**
- Modify: `Docs/Management/BusinessFeatures/artists-songs/requirements.md`
- Modify: `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/requirements.md`

**Interfaces:** Consumes `requirements.md § Supersession` (this feature). Produces nothing consumed by code tasks.

- [ ] **Step 1:** In `artists-songs/requirements.md`, locate each superseded AC and add a note line **directly below it** (never delete or edit the original text). Exact notes, one per location:
  - Below **AC-10.3**: `> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — free text always allowed; local + remote autocomplete; no-match → transparent create on save (REQ-FORMUX-15…20).`
  - Below **AC-10.2**: `> **Spec updated 2026-07-10:** partially superseded by changes/2026-07-10-form-ux-redesign — the API search strip element is removed (REQ-FORMUX-26); the rest of this AC stands.`
  - Below **AC-11.1**, **AC-11.2**, **AC-11.2a** (one note each): `> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — title autocomplete + autofill replace the search strip (REQ-FORMUX-22…24); artist lock retired (REQ-FORMUX-16). AC-11.3–11.5 remain in force.`
  - Below **AC-4.1**, **AC-4.5**, **AC-4.6** (one note each): `> **Spec updated 2026-07-10:** partially superseded by changes/2026-07-10-form-ux-redesign — delivery mechanism replaced by remote autocomplete rows (REQ-FORMUX-02/06/07); AC-4.2 provider order and AC-4.4 semantics remain in force.`
  - Below **AC-4.3**: `> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — blocking error message replaced by silent local-only degradation (REQ-FORMUX-05).`
  - Below **AC-4.7**: `> **Spec updated 2026-07-10:** superseded for the ArtistForm CREATE path only by changes/2026-07-10-form-ux-redesign — manual edit after a remote pick clears the pending external identity (REQ-FORMUX-08). AC-4.7 remains in force for edit mode (REQ-FORMUX-32).`
- [ ] **Step 2:** In `song-import-resolution/requirements.md`, below **AC-B8**: `> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — blur-clear deleted, artist lock retired (REQ-FORMUX-15/16). Pre-populate-in-Edit-mode remains in force.`
- [ ] **Step 3:** Verify with `git diff` that the diff contains **only added lines** (no deletions — immutable history).
- [ ] **Step 4:** No `.sln` change (both files already registered). Task-log entry, then commit:

```bash
git add Docs/Management/BusinessFeatures/artists-songs/requirements.md Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/requirements.md Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "docs: supersession notes on shipped ACs — form-ux-redesign Phase 0"
```

**Demo:** Both originals show the dated note beside each superseded AC; git diff shows no deleted lines.

---

## Phase 1 — Contracts / Infra / Services

### Task 2: Suggestion DTOs (Contracts) `[P]`

**tasks.md:** "Suggestion DTOs (Contracts)" `[P]` · Risk Low · TDD Level C (no mandatory test — document the no-test decision in the task-log) · Review lane Standard

**Files:**
- Create: `Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs`
- Create: `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces (verbatim from `design.md § Interfaces` — Tasks 4, 5, 10, 11, 12 consume these exact shapes):

- [ ] **Step 1:** Create `Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs`:

```csharp
namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// A single artist autocomplete row. <see cref="LocalId"/> non-null means a local DB record;
/// <see cref="IsRemote"/> true means the row came from an IMusicMetadataProvider.
/// </summary>
/// <param name="LocalId">Local Artist id; non-null => local record.</param>
/// <param name="Name">Display/artist name.</param>
/// <param name="ExternalId">External provider id (remote rows; also local rows that carry one).</param>
/// <param name="ExternalProvider">Provider name (e.g. "MusicBrainz", "Deezer").</param>
/// <param name="IsRemote">True when sourced from a music metadata provider.</param>
/// <param name="IsExactMatch">Collation-equal to the search term (computed at fetch time).</param>
public sealed record ArtistSuggestionDto(
    int? LocalId,
    string Name,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    bool IsExactMatch);
```

- [ ] **Step 2:** Create `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`:

```csharp
namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// A single song-title autocomplete row. <see cref="LocalId"/> non-null means a registered song;
/// <see cref="LocalArtistId"/> non-null means the suggestion's artist exists locally.
/// </summary>
/// <param name="LocalId">Local Song id; non-null => local record.</param>
/// <param name="Title">Song title.</param>
/// <param name="ArtistName">Artist name shown as supporting text.</param>
/// <param name="LocalArtistId">Non-null when the suggestion's artist exists locally.</param>
/// <param name="ExternalId">External provider song id (remote rows).</param>
/// <param name="ExternalProvider">Provider name.</param>
/// <param name="IsRemote">True when sourced from a music metadata provider.</param>
public sealed record SongSuggestionDto(
    int? LocalId,
    string Title,
    string ArtistName,
    int? LocalArtistId,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote);
```

- [ ] **Step 3:** Run `dotnet build` — expected: 0 errors.
- [ ] **Step 4:** Task-log entry (include the Level C no-test decision), commit:

```bash
git add Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs Contracts/DTOs/Suggestions/SongSuggestionDto.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: suggestion DTOs (Contracts) — form-ux-redesign"
```

**Demo:** Solution builds; both records match the `design.md` shapes exactly.

### Task 3: Repository collation batch lookups + integration tests `[P — parallel with Task 2, different files]`

**tasks.md:** "Repository collation batch lookups + integration tests" `[P]` · Risk Medium · TDD Level B · Review lane Standard
**Sizing exception (explicit):** 6 files (2 interfaces + 2 implementations + 2 test files) exceeds the 5-file cap — accepted because the two methods are mirror twins sharing one collation pattern (< 2 h total); splitting artist/song halves would duplicate the identical pattern review across two tasks for no isolation benefit.

**Files:**
- Modify: `Domain/RepositoryInterface/IArtistRepository.cs`
- Modify: `Domain/RepositoryInterface/ISongRepository.cs`
- Modify: `Infra/Repository/ArtistRepository.cs`
- Modify: `Infra/Repository/SongRepository.cs`
- Test: `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs` (extend), `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs` (extend or create beside it, same pattern)

**Interfaces:**
- Consumes: existing `CollationConstants` (Infra), `TestDbContextFactory`.
- Produces (Tasks 4 and 5 consume these exact signatures):

```csharp
// IArtistRepository — additive
Task<IReadOnlyList<Artist>> GetByNamesCollatedAsync(IEnumerable<string> names, CancellationToken ct = default);
// ISongRepository — additive
Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(IEnumerable<string> titles, CancellationToken ct = default);
```

- [ ] **Step 1 (Red):** Add the failing integration test to `ArtistRepositoryTests` (real SQLite temp DB — never the in-memory provider):

```csharp
[Fact]
// [AC] REQ-FORMUX-03: remote dedup tier (b) — collation-equal name via batch DB lookup
public async Task GetByNamesCollatedAsync_AccentAndCaseVariants_ResolvesInOneQuery()
{
    _db.Artists.AddRange(new Artist { Name = "cafe" }, new Artist { Name = "Metallica" });
    await _db.SaveChangesAsync();

    var found = await _repo.GetByNamesCollatedAsync(["Café", "METALLICA", "Nobody"]);

    Assert.Equal(2, found.Count);
    Assert.Contains(found, a => a.Name == "cafe");
    Assert.Contains(found, a => a.Name == "Metallica");
}

[Fact]
// [AC] REQ-FORMUX-03: batch lookup with no matches returns empty (no exception)
public async Task GetByNamesCollatedAsync_NoMatches_ReturnsEmpty()
{
    var found = await _repo.GetByNamesCollatedAsync(["Ghost"]);
    Assert.Empty(found);
}
```

- [ ] **Step 2:** Run `dotnet test --filter "FullyQualifiedName~GetByNamesCollatedAsync"` — expected: FAIL (compile error: method not defined). Add the interface method + implementation, then confirm Red→Green.
- [ ] **Step 3 (Green):** Interface method on `IArtistRepository` (with `<summary>` XML doc — interfaces own documentation) and implementation in `ArtistRepository` using the established collation pattern (single SQL query — **never** per-candidate round-trips, never C#-side normalization):

```csharp
/// <inheritdoc />
public async Task<IReadOnlyList<Artist>> GetByNamesCollatedAsync(
    IEnumerable<string> names, CancellationToken ct = default)
{
    var list = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList() ?? [];
    if (list.Count == 0) return [];

    return await _db.Artists
        .AsNoTracking()
        .Where(a => list.Contains(EF.Functions.Collate(a.Name, CollationConstants.Default)))
        .ToListAsync(ct);
}
```

  *(If EF Core 10 does not translate `Contains` over a collated column for this provider, fall back to an `EF.Functions.Collate(a.Name, …) == n` OR-chain built with `LinqKit`-free expression union — or per the existing repo patterns, a `Like`-free union of equality predicates. Verify the generated SQL is ONE query via the test + logging.)*

- [ ] **Step 4:** Repeat Steps 1–3 for `ISongRepository.GetByTitlesCollatedAsync` / `SongRepository` (same shape over `_db.Songs` and `s.Title`; tests `GetByTitlesCollatedAsync_AccentAndCaseVariants_ResolvesInOneQuery`, `GetByTitlesCollatedAsync_NoMatches_ReturnsEmpty` tagged `// [AC] REQ-FORMUX-03`).
- [ ] **Step 5:** Run `dotnet build` then `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` — expected: all green, 0 build errors.
- [ ] **Step 6:** Task-log entry, commit:

```bash
git add Domain/RepositoryInterface/IArtistRepository.cs Domain/RepositoryInterface/ISongRepository.cs Infra/Repository/ArtistRepository.cs Infra/Repository/SongRepository.cs MyVocaList.Tests/Integration/Repositories/ Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: collation batch name/title lookups (Infra) — form-ux-redesign"
```

**Demo:** Integration test proves "Café" resolves against stored "cafe" via one SQL query.

### Task 4: IArtistSuggestionService + ArtistSuggestionService (TDD Level A) `[SEQUENTIAL — after Tasks 2–3]`

**tasks.md:** "IArtistSuggestionService + ArtistSuggestionService (TDD Level A)" · Risk High · Review lane Elevated · tests first, Red seen

**Files:**
- Create: `Services/IArtistSuggestionService.cs`
- Create: `Services/ArtistSuggestionService.cs`
- Test: `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`

**Interfaces:**
- Consumes: `ArtistSuggestionDto` (Task 2), `IArtistRepository.GetByNamesCollatedAsync` + existing `SearchByNameAsync`/`GetByNameAsync` (Task 3 / existing), `IMusicMetadataProvider` (`ProviderName`, `SearchArtistsAsync(term, ct)` returning `MusicSearchResultDto(ExternalId, Provider, ArtistName, SongTitle?, FeaturedArtists?)`), `ISimilarityScorer.Score(a, b)`, `SimilarityConstants.DefaultThreshold`, `ILogger<ArtistSuggestionService>`.
- Produces (verbatim from `design.md` — Tasks 7, 10, 11, 12 consume):

```csharp
// Services/IArtistSuggestionService.cs
namespace MyVocaList.Services;

public interface IArtistSuggestionService
{
    /// <summary>Local artist suggestions for a term (DB collation match, max 5). Immediate path.</summary>
    Task<IReadOnlyList<ArtistSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — MusicBrainz first, Deezer fallback on empty/error, per AC-4.2.
    /// Deduplicated against localResults per REQ-FORMUX-03
    /// (external-id → collation-equal name via batch DB lookup → similarity ≥ threshold). Max 5.
    /// Returns an empty list on provider failure (logged) — never throws for provider errors.</summary>
    Task<IReadOnlyList<ArtistSuggestionDto>> GetRemoteAsync(
        string term, IReadOnlyList<ArtistSuggestionDto> localResults, CancellationToken ct = default);

    /// <summary>Classifies cached suggestions against the typed term: similar = score ≥
    /// SimilarityConstants.DefaultThreshold AND not exact. Pure in-memory — no I/O, no refetch.</summary>
    IReadOnlyList<ArtistSuggestionDto> FilterSimilar(string typedName, IReadOnlyList<ArtistSuggestionDto> fetched);
}
```

Implementation notes (constructor + provider selection — `ArtistSuggestionService`):
- Inject `IArtistRepository`, `IEnumerable<IMusicMetadataProvider>` (both are DI-registered; select by `ProviderName == "MusicBrainz"` first, `"Deezer"` fallback when MusicBrainz returns empty **or throws** — never parallel), `ISimilarityScorer`, `ILogger<ArtistSuggestionService>`.
- `GetLocalAsync`: `< 2` trimmed chars → `[]`; else repository name search (`SearchByNameAsync(term, 5, ct)`), map to DTOs with `IsRemote = false`; `IsExactMatch` computed via `GetByNameAsync(term, ct)` id comparison (collation-equal — never `string.Equals` variants for exactness).
- `GetRemoteAsync` dedup order (REQ-FORMUX-03, exact tiers): (a) drop remote result whose `(Provider, ExternalId)` equals a local suggestion's `(ExternalProvider, ExternalId)`; (b) drop those whose name is collation-equal to a local artist — via **one** `GetByNamesCollatedAsync(remoteNames)` batch call; (c) drop those with `ISimilarityScorer.Score(remoteName, localName) >= SimilarityConstants.DefaultThreshold` against any local suggestion. Cap at 5. Wrap provider calls in try-catch → log + `[]` (allowed pattern 4, error recovery with logging); `OperationCanceledException` rethrows/propagates (cancellation is not a provider failure).
- `FilterSimilar`: pure in-memory; `score >= DefaultThreshold && !IsExactMatch`-style classification against `typedName`; exactness for remote rows = scorer score of 1.0 is still *similar* only if not collation-equal — use the DTO's `IsExactMatch` flag for local rows and treat remote rows as never exact.

Test-first sequence (one test at a time — Red → Green each; all tests use Moq, no real DB, no provider HTTP):

- [ ] **Step 1 (Red):** `GetLocalAsync_TermUnderTwoChars_ReturnsEmpty` — `// [AC] REQ-FORMUX-01: suggestions require ≥ 2 chars`. Run filter `FullyQualifiedName~ArtistSuggestionServiceTests` — FAIL (type missing). Create interface + minimal class, confirm Green.
- [ ] **Step 2:** `GetLocalAsync_ManyMatches_ReturnsAtMostFive` — `// [AC] REQ-FORMUX-01: up to 5 local rows`. Red → Green.
- [ ] **Step 3:** `GetRemoteAsync_MusicBrainzReturnsResults_DeezerNeverCalled` — `// [AC] REQ-FORMUX-02: provider order (AC-4.2 in force)`. Verify with `Times.Never` on the Deezer mock. Red → Green.
- [ ] **Step 4:** `GetRemoteAsync_MusicBrainzEmpty_FallsBackToDeezer` and `GetRemoteAsync_MusicBrainzThrows_FallsBackToDeezer` — `// [AC] REQ-FORMUX-02`. Red → Green.
- [ ] **Step 5:** `GetRemoteAsync_ResultSharesExternalIdWithLocal_IsExcluded` — `// [AC] REQ-FORMUX-03: dedup tier (a)`. Red → Green.
- [ ] **Step 6:** `GetRemoteAsync_ResultNameCollationEqualToLocalDb_IsExcluded` — `// [AC] REQ-FORMUX-03: dedup tier (b)` (repo mock: `GetByNamesCollatedAsync` returns the matching artist; assert single batch call with `Times.Once`). Red → Green.
- [ ] **Step 7:** `GetRemoteAsync_ResultSimilarAboveThresholdToLocal_IsExcluded` (scorer mock returns 0.9) and `GetRemoteAsync_ResultBelowThreshold_IsKept` (0.5) — `// [AC] REQ-FORMUX-03: dedup tier (c)`. Red → Green.
- [ ] **Step 8:** `GetRemoteAsync_AllProvidersFail_ReturnsEmptyAndLogs` — `// [AC] REQ-FORMUX-05: silent local-only degradation, logged`. Red → Green.
- [ ] **Step 9:** `FilterSimilar_ScoreAtThresholdNonExact_IsSimilar`, `FilterSimilar_ScoreBelowThreshold_IsNotSimilar`, `FilterSimilar_ExactMatch_IsNotSimilar` — `// [AC] REQ-FORMUX-10: similar = ≥ 0.82 AND not exact, cache-only`. Red → Green.
- [ ] **Step 10:** `GetRemoteAsync_Cancelled_ThrowsOperationCanceled` — `// [AC] REQ-FORMUX-02: stale lookups cancellable (Failure modes: stale results discarded)`. Red → Green.
- [ ] **Step 11:** `dotnet build` + full `dotnet test` — 0 errors, all green. Task-log entry, commit:

```bash
git add Services/IArtistSuggestionService.cs Services/ArtistSuggestionService.cs MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: ArtistSuggestionService — local/remote suggestions + 3-tier dedup (TDD A)"
```

**Demo:** `dotnet test` shows all dedup/threshold branch tests green; providers fully mocked.

### Task 5: ISongSuggestionService + SongSuggestionService (TDD Level A) `[P — parallel with Task 4, different files]`

**tasks.md:** "ISongSuggestionService + SongSuggestionService (TDD Level A)" `[P]` · Risk High · Review lane Elevated

**Files:**
- Create: `Services/ISongSuggestionService.cs`
- Create: `Services/SongSuggestionService.cs`
- Test: `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`

**Interfaces:**
- Consumes: `SongSuggestionDto` (Task 2), `ISongRepository.GetByTitlesCollatedAsync` (Task 3) + existing song paged/search queries, `IArtistRepository.GetByNamesCollatedAsync` (to resolve `LocalArtistId` for remote rows), `IMusicMetadataProvider.SearchSongsAsync(term, artistHint, ct)`, `ISimilarityScorer`, `SimilarityConstants`.
- Produces (verbatim from `design.md` — Tasks 7 and 12 consume):

```csharp
// Services/ISongSuggestionService.cs
namespace MyVocaList.Services;

public interface ISongSuggestionService
{
    /// <summary>Local song-title suggestions for a term (registered songs; title + artist name), max 5.</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — same provider order and dedup/failure semantics as
    /// IArtistSuggestionService.GetRemoteAsync (MusicBrainz first, Deezer fallback per AC-4.2).</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetRemoteAsync(
        string term, string? artistHint, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct = default);
}
```

Implementation notes: local suggestions map registered songs (title match under collation via existing paged/search query with `pageSize: 5`, or a `Like`-collated title query mirroring `ArtistRepository.SearchByNameAsync`); `ArtistName` filled from the song's `OriginalArtist`. Remote dedup mirrors Task 4's three tiers over titles (`GetByTitlesCollatedAsync` batch); `LocalArtistId` for remote rows resolved via **one** `IArtistRepository.GetByNamesCollatedAsync` batch over the remote artist names (external-id equality checked first where the provider result carries one). `artistHint` passes through to `SearchSongsAsync` verbatim.

Test-first sequence (Red → Green each; Moq only):

- [ ] **Step 1 (Red):** `GetLocalAsync_TermMatchesRegisteredSongs_ReturnsTitleAndArtistName` — `// [AC] REQ-FORMUX-22: local title suggestions with artist supporting text`. FAIL first (type missing) → minimal Green.
- [ ] **Step 2:** `GetLocalAsync_ManyMatches_ReturnsAtMostFive` — `// [AC] REQ-FORMUX-22`. Red → Green.
- [ ] **Step 3:** `GetRemoteAsync_ArtistHintProvided_PassedToProvider` — `// [AC] REQ-FORMUX-22: artistHint pass-through` (verify mock arg). Red → Green.
- [ ] **Step 4:** `GetRemoteAsync_ResultTitleCollationEqualToLocal_IsExcluded` — `// [AC] REQ-FORMUX-03` (batch call `Times.Once`). Red → Green.
- [ ] **Step 5:** `GetRemoteAsync_RemoteArtistExistsLocally_LocalArtistIdResolved` — `// [AC] REQ-FORMUX-23: local artist resolved for remote rows`. Red → Green.
- [ ] **Step 6:** `GetRemoteAsync_AllProvidersFail_ReturnsEmptyAndLogs` — `// [AC] REQ-FORMUX-05`. Red → Green.
- [ ] **Step 7:** `GetRemoteAsync_MusicBrainzEmpty_FallsBackToDeezer` — `// [AC] REQ-FORMUX-02`. Red → Green.
- [ ] **Step 8:** `dotnet build` + `dotnet test` all green. Task-log, commit:

```bash
git add Services/ISongSuggestionService.cs Services/SongSuggestionService.cs MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: SongSuggestionService — title suggestions + dedup + artistHint (TDD A)"
```

**Demo:** `dotnet test` green for all branches.

### Task 6: ArtistService external-identity persistence fix (REQ-FORMUX-07) `[SEQUENTIAL]`

**tasks.md:** "ArtistService external-identity persistence fix (REQ-FORMUX-07)" · Risk Medium · TDD Level A · Review lane Standard

**Files:**
- Modify: `Domain/ServicesInterfaces/IArtistService.cs`
- Modify: `Services/ArtistService.cs`
- Test: `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs` (extend)

**Interfaces:**
- Consumes: existing `Artist.ExternalId` / `Artist.ExternalProvider` entity fields (no migration).
- Produces (Tasks 7 caller-compile, 11 consume). Replace the current signature `CreateArtistAsync(string name, CancellationToken ct = default)` with:

```csharp
/// <summary>Creates an artist with the given name and optional external identity
/// (REQ-FORMUX-07 — persisted when a remote suggestion pick supplied them).</summary>
Task<(bool success, string message, Artist? artist)> CreateArtistAsync(
    string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);
```

  Existing callers (`ArtistFormViewModel.SaveAsync` uses `CreateArtistAsync(name)`; `ArtistResolutionService` create path) compile unchanged via the optional parameters — verify with a solution-wide grep for `CreateArtistAsync(` after the change.

- [ ] **Step 1 (Red):** Add failing test:

```csharp
[Fact]
// [AC] REQ-FORMUX-07: external identity persisted on create after remote pick
public async Task CreateArtistAsync_WithExternalIdentity_PersistsBothFields()
{
    _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
    Artist? captured = null;
    _repoMock.Setup(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()))
             .Callback<Artist, CancellationToken>((a, _) => captured = a)
             .Returns(Task.CompletedTask);
    var sut = CreateSut();

    var (success, _, _) = await sut.CreateArtistAsync("Black Sabbath", "mbid-123", "MusicBrainz");

    Assert.True(success);
    Assert.Equal("mbid-123", captured!.ExternalId);
    Assert.Equal("MusicBrainz", captured.ExternalProvider);
}
```

  Run — expected FAIL (no such overload / fields null).
- [ ] **Step 2 (Green):** Update interface + `ArtistService.CreateArtistAsync` to set `ExternalId`/`ExternalProvider` on the created entity when supplied (null-safe: whitespace → null). Confirm test passes.
- [ ] **Step 3 (Red→Green):** `CreateArtistAsync_ManualNoIdentity_FieldsStayNull` — `// [AC] REQ-FORMUX-08: manual create carries no identity`. Then re-run the *existing* validation tests untouched (name-too-long, duplicate) to prove behavior is preserved.
- [ ] **Step 4:** `dotnet build` + `dotnet test` — green. Task-log, commit:

```bash
git add Domain/ServicesInterfaces/IArtistService.cs Services/ArtistService.cs MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "fix: ArtistService persists ExternalId/ExternalProvider on create (REQ-FORMUX-07)"
```

**Demo:** Test proves a created artist row carries the supplied `ExternalId`/`ExternalProvider`.

### Task 7: DI registration for suggestion services `[SEQUENTIAL — hotspot file]`

**tasks.md:** "DI registration for suggestion services" · Risk Low · Level C (registration itself: no mandatory test — document; the DI-resolution regression test covers the chain) · Review lane Standard

**Files:**
- Modify: `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (sequential-only registry adjacent — single writer)
- Modify: `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`

**Interfaces:** Consumes Tasks 4 + 5 committed. Produces resolvable `IArtistSuggestionService` / `ISongSuggestionService` for Tasks 10–12.

- [ ] **Step 1:** In `AddAppServices()`, after the existing `ISongResolutionService` registration add (Scoped per `code-style-reference.md § DI Registration Conventions` — services are Scoped):

```csharp
// Form UX redesign — in-field suggestion services (form-ux-redesign 2026-07-10)
services.AddScoped<IArtistSuggestionService, ArtistSuggestionService>();
services.AddScoped<ISongSuggestionService, SongSuggestionService>();
```

- [ ] **Step 2:** Extend `AppServicesRegistrationTests` with resolution assertions for both new interfaces (mirror the existing per-service resolution test pattern in that file).
- [ ] **Step 3:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~AppServicesRegistrationTests"` — green.
- [ ] **Step 4:** Task-log (Level C no-test note for the registration lines), commit:

```bash
git add MyVocaList/Extensions/ServiceCollectionExtensions.cs MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: DI registration for suggestion services — form-ux-redesign"
```

**Demo:** DI-resolution test resolves `SongFormViewModel` full chain with the new services.

---

## Phase 2 — Governed component (dedicated task — no bundling, HARD RULE)

### Task 8: [COMPONENT] AutocompleteField — remote section marker + loading-hint row (additive) `[SEQUENTIAL]`

**tasks.md:** identical title · Review lane **Architectural** · All four component-change-governance gates apply.

> **Gate 4 note:** Helder pre-approved downstream gates for this feature (BACKLOG 2026-07-10, commit 5a84503). Record that pre-approval date in the task-log as the Gate 4 artifact — the MD3 review (Gate 1), consumer map (Gate 2), and per-consumer risk table (Gate 3) must still be produced and logged before editing, or the task-log entry is invalid.

**Files:**
- Modify: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml`
- Modify: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs`
- Modify: `Contracts/Models/AutocompleteSuggestion.cs` (the `AutocompleteSuggestion` model file — currently `record AutocompleteSuggestion(string Headline, string SupportingText, object Data)`)
- **ONLY these files** (Files owned constraint from tasks.md).

**Interfaces:**
- Consumes: nothing from Phase 1 (component is Contracts/UI-only — keep it decoupled from suggestion DTOs; ViewModels map DTOs → `AutocompleteSuggestion`).
- Produces (Tasks 10–14 consume):
  - `AutocompleteSuggestion` gains an **optional** origin/section member with a default preserving current rendering, e.g. `public sealed record AutocompleteSuggestion(string Headline, string SupportingText, object Data, bool IsRemote = false);` — remote rows render under a "From music database" MD3 list subheader (or per-row supporting text carrying provider origin; pick ONE per the MD3 review and record it).
  - New BindableProperty `IsRemoteLookupRunning` (`bool`, default `false`) rendering a single non-selectable loading-hint row below the local rows.
  - `BlurredWithoutSelectionCommand` **remains** — PersonFormPage may still use it. If implementation finds the component itself forces clearing behavior (not just the VM handler), that is non-additive → STOP, `blocked: spec gap`.

- [ ] **Step 1 (Gate 1 — MD3 review):** Check m3.material.io menus + lists: section header uses list *subheader* anatomy; loading row per progress-indicator-in-list guidance. Record findings (style keys, typography role for the subheader) in the task-log BEFORE editing.
- [ ] **Step 2 (Gate 2 — consumer map):** grep the **entire repository**, not just pages: `grep -rn "AutocompleteField" --include="*.xaml" --include="*.cs" .` (catches `<autocomplete:AutocompleteField` usages, xmlns declarations, `x:Reference`, code-behind and test references anywhere in the solution). Expected XAML consumers: `PersonFormPage.xaml`, `SongFormPage.xaml`. Record the actual grep output in the task-log (never from memory). Any unexpected consumer → add it to the risk table before editing.
- [ ] **Step 3 (Gate 3 — per-consumer risk):** Record in task-log:

| Consumer | What could break | Verification |
|----------|------------------|-------------|
| PersonFormPage | rendering change when new properties unused; blur behavior change | with new properties unbound, visual + behavior identical (emulator check) |
| SongFormPage | suggestion template change breaks existing artist rows | artist suggestions render as before until Phase 4 rewires them |

- [ ] **Step 4:** Add the optional member to `AutocompleteSuggestion` (default `false` — existing construction sites compile unchanged; verify with grep for `new AutocompleteSuggestion(`).
- [ ] **Step 5:** Edit `AutocompleteField.xaml(.cs)`: grouped rendering (local rows → subheader → remote rows) driven by the suggestion flag; `IsRemoteLookupRunning` BindableProperty + loading-hint row (non-selectable — excluded from `OnSuggestionTapped`); keep overlay visible while the loading row shows even if remote rows have not arrived. **One XAML file — build immediately after.**
- [ ] **Step 6:** `dotnet build` — 0 errors. Emulator check on PersonFormPage (new properties unbound): rendering and blur behavior identical (Gate 3 verification, E2E emulator gate — component change is user-facing).
- [ ] **Step 7:** Task-log (MD3 findings + consumer map + risk table + Gate 4 pre-approval note — entry is invalid without them), commit:

```bash
git add MyVocaList/UI/Components/AutocompleteField/ Contracts/Models/AutocompleteSuggestion.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: [COMPONENT] AutocompleteField — remote section subheader + loading-hint row (additive, REQ-FORMUX-29/30)"
```

**Demo:** A consumer shows local rows, a "From music database" header with remote rows beneath, and a loading-hint row while `IsRemoteLookupRunning` is true; PersonFormPage unchanged.

---

## Phase 3 — ViewModels (TDD Level A; Moq'd services; no Shell.Current in tests)

> ViewModel tests never call `Shell.Current` (null in test context). Navigation-dependent branches are tested up to the state transition; navigation itself is covered by the Phase 4/6 emulator gates. Where a test must assert "navigates to Edit Artist", assert the observable pre-navigation state (e.g. selected candidate captured) and document the navigation as emulator-verified.

### Task 9: BUG-027 regression test (Red) + SongFormViewModel blur-clear removal + IsArtistLocked retirement `[SEQUENTIAL — first VM task]`

**tasks.md:** identical title · Risk High · Review lane Elevated · **Red-first is MANDATORY** (Critical severity, bug-tracking HARD RULE)

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- Test: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`

**Interfaces:** Consumes nothing from Phases 1–2 (pure behavior deletion). Produces the unlocked artist-entry state machine Tasks 12/14 build on.

- [ ] **Step 1 (Red — MANDATORY first):** Write the regression test against the CURRENT code and see it FAIL:

```csharp
[Fact]
// [AC] REQ-FORMUX-15: typed artist text survives blur (BUG-027 regression)
public void ArtistFieldBlur_WithTypedNonMatchingText_KeepsText()
{
    var sut = CreateSut();
    sut.ArtistSearchText = "Blk Sabb";           // typed, no selection

    sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

    Assert.Equal("Blk Sabb", sut.ArtistSearchText);   // FAILS today: blur-clear wipes it
}
```

  Run `dotnet test --filter "FullyQualifiedName~ArtistFieldBlur_WithTypedNonMatchingText_KeepsText"` — **expected: FAIL** (current `OnArtistBlurredWithoutSelection` sets `ArtistSearchText = string.Empty`). Record the failing output in the task-log (proof of Red).
- [ ] **Step 2 (Green):** In `SongFormViewModel`:
  - Delete the body logic of `OnArtistBlurredWithoutSelection` (the method documented "BUG-008: blur-clear rule") and its command wiring `ArtistBlurredWithoutSelectionCommand` — the field must undergo NO transition and NO text change on blur (design's artist state machine: `any state → same state (blur)`). Removing the command entirely is correct only if the XAML binding is removed in Task 14 — since XAML edits are Phase 4, keep the command but make it a no-op with a comment `// REQ-FORMUX-15: blur never mutates text (BUG-027). Binding removed in SongFormPage task.` (temporary until Task 14 deletes the binding; the component keeps `BlurredWithoutSelectionCommand` for PersonFormPage).
  - Remove `IsArtistLocked` `[ObservableProperty]` and ALL assignments (`LoadSongForEditAsync`, `ResolveAndLockArtistAsync`) — the Artist field is always editable (REQ-FORMUX-16). The XAML `IsEnabled` binding is removed in Task 14; property removal will break the XAML build only if the binding is compiled — if `dotnet build` fails on the XAML binding, remove that single binding line as part of this task and note it in Changed files (narrow exception to keep the build green; the full page rework stays in Task 14).
  - Add typing-clears-selection-identity-only: in a `partial void OnArtistSearchTextChanged(string value)` hook, when the change is user-typed (not a pick/hydration — guard with a `_isApplyingArtistPick` flag set by `SelectArtist` and hydration), clear `SelectedArtistId`/`SelectedArtistName` and the pending external artist identity — never the text (REQ-FORMUX-16).
- [ ] **Step 3:** Confirm Step 1's test passes. Add and see Red→Green one at a time:
  - `ArtistTextTyped_AfterLocalSelection_ClearsSelectionIdKeepsText` — `// [AC] REQ-FORMUX-16: keystroke clears selection identity only`.
  - `ArtistTextTyped_AfterEditModePrePopulation_ClearsSelectionIdKeepsText` — `// [AC] REQ-FORMUX-33: edit mode behaves identically`.
- [ ] **Step 4:** Full `dotnet test` — all pre-existing SongFormViewModel tests still green EXCEPT any test that encoded the old blur-clear/lock behavior: those tests encode superseded ACs (AC-B8/AC-11.2a — spec-superseded in Task 1). Deleting/updating them is NOT builder-test-tampering **only if** each removed test is listed in the task-log with the superseding REQ-FORMUX id. Any other failing test → stop, `blocked: spec gap`.
- [ ] **Step 5:** `dotnet build` 0 errors. Task-log (include Red evidence + superseded-test list). Commit:

```bash
git add MyVocaList/UI/ViewModels/SongFormViewModel.cs MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "fix: SongFormViewModel — BUG-027 blur-clear deleted, IsArtistLocked retired (REQ-FORMUX-15/16)

Root cause: BUG-008 blur handler wiped unmatched typed artist text, making song registration impossible.
Fix: blur is a no-transition event; typing clears selection identity only, never text.
Regression risk: Low — regression test seen Red before fix; superseded-AC tests removed per Task 1 supersession notes."
```

**Demo:** Test run log shows the regression test failing before the change and passing after; typed artist text survives blur.

### Task 10: ArtistFormViewModel (part 1) — suggestion orchestration + similar-warn state `[SEQUENTIAL]`

**tasks.md:** identical title · Risk High · Review lane Elevated · TDD Level A (+ Level B staging tests)

**Files:**
- Modify: `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`
- Test: `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`

**Interfaces:**
- Consumes: `IArtistSuggestionService` (Task 4, injected — extend the constructor; update `CreateSut()` in tests), DI (Task 7), component contract (Task 8: `AutocompleteSuggestion(Headline, SupportingText, Data, IsRemote)`).
- Produces (Task 11 and Task 13 consume — exact member names):
  - `IAsyncRelayCommand<string> SearchRequestedCommand` (bound to the component; term is the debounced text)
  - `IRelayCommand<AutocompleteSuggestion> SuggestionSelectedCommand`
  - `[ObservableProperty] IEnumerable<AutocompleteSuggestion> _nameSuggestions`
  - `[ObservableProperty] bool _isRemoteLookupRunning`
  - Repurposed similar-warn state: keep `DuplicateSuggestions`/`HasDuplicateSuggestions` property names (existing XAML block rebinds cheaply in Task 13) but feed them from `FilterSimilar(typed, _cachedSuggestions)` — element type becomes `ArtistSuggestionDto`.
  - Pending identity: reuse `SelectedExternalId`/`SelectedProvider` (existing stash properties) — set on remote pick, cleared on manual edit.
  - `IAsyncRelayCommand<ArtistSuggestionDto> PickInlineHintCandidateCommand` — tap handler for the inline similar-warn hint rows; applies the host form's pick semantics (REQ-FORMUX-11: local → REQ-FORMUX-09 navigate to Edit Artist; remote → REQ-FORMUX-06 fill + stash identity). Task 13 binds the hint rows to exactly this command.
  - Internal: `_cachedSuggestions : List<ArtistSuggestionDto>` (local + remote as fetched), remote-stagger timer with **injectable delay** (constructor param `Func<TimeSpan, CancellationToken, Task>? staggerDelay = null` defaulting to `Task.Delay` — Level B testability per design § Remote staging), `CancellationTokenSource _remoteCts` cancelled on: new search request, suggestion pick, navigation (`CancelAsync`), Save.

Orchestration per `design.md § Remote staging`: `SearchRequestedCommand(term)` → cancel previous remote CTS → `GetLocalAsync(term)` → render local rows immediately (single `RunOnUiThread` block; map DTO→`AutocompleteSuggestion` with `Data` = the DTO) → start 400 ms stagger (injectable) → set `IsRemoteLookupRunning = true` → `GetRemoteAsync(term, localResults, ct)` → append remote rows / on failure-empty just clear the loading flag (log only) → recompute warn state from `_cachedSuggestions` via `FilterSimilar`. Stale completions (token cancelled) render nothing.

Test-first (Red→Green each; `IArtistSuggestionService` mocked; stagger delay injected as immediate/controllable `TaskCompletionSource`):

- [ ] **Step 1 (Red):** `SearchRequested_LocalResults_RenderedBeforeRemoteCompletes` — `// [AC] REQ-FORMUX-01: local rows immediate, no wait on remote` (remote mock blocked on a TCS; assert `NameSuggestions` already has local rows). → Green.
- [ ] **Step 2:** `SearchRequested_RemoteResults_AppendedAfterStagger` — `// [AC] REQ-FORMUX-02: remote appended under section marker` (assert appended rows carry `IsRemote = true`). Red → Green.
- [ ] **Step 3:** `SearchRequested_WhileRemoteInFlight_LoadingHintShown_ThenCleared` — `// [AC] REQ-FORMUX-04` (assert `IsRemoteLookupRunning` true during, false after complete/fail/empty). Red → Green.
- [ ] **Step 4:** `SearchRequested_NewTerm_CancelsPreviousRemoteLookup` — `// [AC] REQ-FORMUX-02 / Failure modes: stale results discarded` (first lookup completes late with old-term rows; assert they are never rendered). Red → Green.
- [ ] **Step 5:** `SearchRequested_RemoteFails_LocalOnlyNoError` — `// [AC] REQ-FORMUX-05` (service returns `[]`; assert no error state, local rows intact). Red → Green.
- [ ] **Step 6:** `RemoteSuggestionPicked_FillsNameAndStashesIdentity` — `// [AC] REQ-FORMUX-06`. Red → Green.
- [ ] **Step 7:** `NameEdited_AfterRemotePick_ClearsPendingIdentityKeepsText` — `// [AC] REQ-FORMUX-08` (create mode). And `NameEdited_AfterRemotePick_EditMode_KeepsIdentity` — `// [AC] REQ-FORMUX-32: AC-4.7 governs in edit mode` (identity retained; `HasManualEdits` semantics handled at save). Red → Green.
- [ ] **Step 8:** `SimilarMatches_InCachedSuggestions_PopulateWarnState` — `// [AC] REQ-FORMUX-10: warn fed from cache, no refetch` (verify `FilterSimilar` called with cached list; `Times.Never` extra `GetLocalAsync`/`GetRemoteAsync`). And `LocalSuggestionPicked_OnCreateForm_CapturesNavigationTargetToEditArtist` — `// [AC] REQ-FORMUX-09` (assert pre-navigation state; navigation emulator-verified in Task 13). Red → Green.
- [ ] **Step 9:** Full `dotnet build` + `dotnet test` green (existing ArtistFormViewModel tests must stay green — picker-message registration stays untouched until Task 15). Task-log, commit:

```bash
git add MyVocaList/UI/ViewModels/ArtistFormViewModel.cs MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: ArtistFormViewModel — suggestion orchestration + similar-warn state (REQ-FORMUX-01..06,08,10)"
```

**Demo:** Tests prove: local rows immediate, remote appended after stagger, stale results discarded, warn state populated from cache only, manual edit after remote pick → identity cleared.

### Task 11: ArtistFormViewModel (part 2) — save-flow + confirm-sheet state machine + external-identity save path `[SEQUENTIAL — after part 1, same files, never parallel]`

**tasks.md:** identical title · Risk High · Review lane Elevated · TDD Level A

**Files:** same two files as Task 10 (strictly sequential — single writer).

**Interfaces:**
- Consumes: Task 10 state, `IArtistService.CreateArtistAsync(name, externalId, provider, ct)` (Task 6), `FilterSimilar` (Task 4).
- Produces (Task 13 consumes — exact member names):
  - `[ObservableProperty] bool _isConfirmSheetVisible`
  - `[ObservableProperty] IReadOnlyList<ArtistSuggestionDto> _confirmSheetCandidates`
  - `string ConfirmSheetCreateLabel => $"Create '{ArtistName?.Trim()}'"`
  - `IAsyncRelayCommand<ArtistSuggestionDto> PickConfirmCandidateCommand` (local → navigate to Edit Artist for that record; remote → fill name + pending identity, close sheet, NO save — user taps Save again per REQ-FORMUX-12 asymmetry)
  - `IAsyncRelayCommand ConfirmCreateTypedCommand` ("Create '<typed>'" → proceed with creation)
  - `IRelayCommand DismissConfirmSheetCommand` (hardware back/scrim → no save, form state unchanged)

Save flow branches (create mode, per `design.md § ArtistFormPage`): validation fail → inline error (unchanged) · exact local match → existing uniqueness error (unchanged — service already returns it) · similar (from `FilterSimilar` over cache) → open sheet, save does NOT complete · none → `CreateArtistAsync(name, SelectedExternalId?, SelectedProvider?)` → back + snackbar. Edit mode (REQ-FORMUX-32): same warn/sheet on rename, self excluded from candidates; `UpdateArtistAsync` path otherwise unchanged.

Test-first (Red→Green each):

- [ ] **Step 1:** `Save_NoExactNoSimilar_CreatesWithPendingIdentity` — `// [AC] REQ-FORMUX-07` (verify `CreateArtistAsync("name", "extId", "MusicBrainz", …)` called). Red → Green.
- [ ] **Step 2:** `Save_NoMatchManualEntry_CreatesWithNullIdentity` — `// [AC] REQ-FORMUX-14: no sheet, no hint, direct save`. Red → Green.
- [ ] **Step 3:** `Save_SimilarMatchNoExact_OpensConfirmSheetAndDoesNotCreate` — `// [AC] REQ-FORMUX-12` (`Times.Never` on create). Red → Green.
- [ ] **Step 4:** `ConfirmSheet_PickRemoteCandidate_FillsFormClosesSheetNoSave` — `// [AC] REQ-FORMUX-12: ArtistForm asymmetry — user must save again`. Red → Green.
- [ ] **Step 5:** `ConfirmSheet_CreateTyped_ProceedsWithTypedName` — `// [AC] REQ-FORMUX-13`. Red → Green.
- [ ] **Step 6:** `ConfirmSheet_Dismissed_NoSaveFormStateUnchanged` — `// [AC] REQ-FORMUX-12 / Failure modes: sheet dismissed`. Red → Green.
- [ ] **Step 7:** `InlineHint_TapCandidate_AppliesHostFormPickSemantics` — `// [AC] REQ-FORMUX-11`. And edit-mode: `Save_EditModeRename_SimilarMatch_OpensSheet_SelfExcluded` — `// [AC] REQ-FORMUX-32`. Red → Green.
- [ ] **Step 8:** Full build + test green. Task-log (AC matrix rows for REQ-FORMUX-07/08/11/12/13/14/32), commit:

```bash
git add MyVocaList/UI/ViewModels/ArtistFormViewModel.cs MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: ArtistFormViewModel — save flow + confirm-sheet state machine (REQ-FORMUX-07,11..14,32)"
```

**Demo:** Tests prove: no-match → create called with identity; similar → sheet flag set, no create; exact → uniqueness error; remote-candidate pick on sheet fills form without saving.

### Task 12: SongFormViewModel — artist save resolution + title autocomplete/autofill `[SEQUENTIAL — after Task 9]`

**tasks.md:** identical title · Risk High · Review lane Elevated · TDD Level A
**✅ GAP-1 RESOLVED — Option A** (Helder 2026-07-10, see § Open spec gaps). Step 6 is unblocked; implement the transparent-create path via `CommitAsync(CreateNew)` + post-create `_pendingRawUrls` attach.

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- Test: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: `IArtistSuggestionService` + `ISongSuggestionService` (inject both — extend constructor + test `CreateSut()`), DI (Task 7), Task 9 committed, component contract (Task 8).
- Produces (Task 14 consumes — exact member names):
  - Artist entry: replace `SearchArtistsCommand` internals with the same local-immediate + 400 ms-staggered-remote orchestration as Task 10 (shared pattern, per-VM implementation; injectable stagger delay); `[ObservableProperty] bool _isArtistRemoteLookupRunning`; remote artist pick records name + external identity as **marked-for-create** (`_pendingArtistExternalId`/`_pendingArtistProvider` internal state exposed for tests via save behavior).
  - Title entry: `IAsyncRelayCommand<string> TitleSearchRequestedCommand`, `IRelayCommand<AutocompleteSuggestion> TitleSuggestionSelectedCommand`, `[ObservableProperty] IEnumerable<AutocompleteSuggestion> _titleSuggestions`, `[ObservableProperty] bool _isTitleRemoteLookupRunning` — via `ISongSuggestionService` with `artistHint:` = current artist text when non-empty.
  - Confirm sheet (same pattern/member names as Task 11): `IsConfirmSheetVisible`, `ConfirmSheetCandidates` (`ArtistSuggestionDto`), `PickConfirmCandidateCommand` (**SongForm semantics: picking attaches the candidate as the song's artist and the save CONTINUES** — REQ-FORMUX-12/19 asymmetry), `ConfirmCreateTypedCommand`, `DismissConfirmSheetCommand`.

Save-resolution ladder (replaces the current hard `SelectedArtistId` requirement in `SaveAsync`), applied when no `SelectedArtistId` (create AND edit mode — REQ-FORMUX-33):
1. Empty/whitespace artist → `ArtistHasError = true`, `"Artist is required"`, no save (REQ-FORMUX-21 — replaces the current "Search and select an artist from the list" message for the non-empty case).
2. Exact local match (collation — via `IArtistSuggestionService.GetLocalAsync` exact flag or the cached exact suggestion) → auto-attach silently, continue save (REQ-FORMUX-18).
3. Similar (from `FilterSimilar` over cached artist suggestions) → confirm sheet; pick attaches + save continues; "Create '<typed>'" → transparent create path (REQ-FORMUX-19).
4. No match → transparent create (REQ-FORMUX-20 — **GAP-1 RESOLVED: Option A**): `_songResolution.CommitAsync(candidate, ResolutionChoice.CreateNew, null, null)` with the ArtistCandidate carrying typed name + marked-for-create identity, then attach `_pendingRawUrls` via `ISongKaraokeUrlService.AddUrlAsync` (URL-attach failure non-fatal — logged, save still succeeds).

Title remote pick autofill (REQ-FORMUX-23/24/31): fill `SongTitle`, artist (local artist if `LocalArtistId` non-null → attach id; else remote artist as marked-for-create), stash song pending external identity (`SelectedExternalId`/`SelectedProvider`) — persist NOTHING; save runs the existing resolution/merge flow **unchanged** (`ExecuteNewSongSaveAsync` → `ResolveAsync`/sheets — do not touch `ISongResolutionService`). Manual edit after remote title pick retains pending identity; `HasManualEdits` (AC-11.4) governs (REQ-FORMUX-31). Local title pick fills Title text only (REQ-FORMUX-25).

Test-first (Red→Green each):

- [ ] **Step 1:** `ArtistSearch_LocalImmediate_RemoteStaggered` — `// [AC] REQ-FORMUX-17: same autocomplete behavior as REQ-FORMUX-01…05` (+ cancellation variant). Red → Green.
- [ ] **Step 2:** `Save_NoSelectionExactLocalMatch_AutoAttachesSilently` — `// [AC] REQ-FORMUX-18` (assert save continues into resolution with the matched artist id; `Times.Never` on sheet). Red → Green.
- [ ] **Step 3:** `Save_NoSelectionSimilarMatch_OpensConfirmSheet` — `// [AC] REQ-FORMUX-19` and `ConfirmSheet_PickCandidate_AttachesArtistAndSaveContinues` — `// [AC] REQ-FORMUX-12: SongForm asymmetry — save continues`. Red → Green.
- [ ] **Step 4:** `Save_EmptyArtist_ShowsArtistRequiredAndDoesNotSave` — `// [AC] REQ-FORMUX-21`. Red → Green.
- [ ] **Step 5:** `Save_EditModeArtistTextChanged_ResolutionLadderApplies` — `// [AC] REQ-FORMUX-33`. Red → Green.
- [ ] **Step 6 (✅ GAP-1 RESOLVED — Option A):** `Save_NoMatch_TransparentCreateWithMarkedForCreateIdentity` — `// [AC] REQ-FORMUX-20: artist + song in one atomic save; external identity persisted` (assert `CommitAsync(candidate, ResolutionChoice.CreateNew, null, null)` is invoked with an ArtistCandidate carrying the typed name + pending external identity, then `_pendingRawUrls` attached post-create via `ISongKaraokeUrlService.AddUrlAsync`; a failure surfaces via the existing tuple/snackbar path, never an escaping exception; URL-attach failure is non-fatal). Red → Green.
- [ ] **Step 7:** `TitleRemotePick_AutofillsTitleArtistAndPendingIdentity_PersistsNothing` — `// [AC] REQ-FORMUX-23` (`Times.Never` on every create/update mock); `TitleRemotePick_ThenSave_RunsExistingResolutionFlowUnchanged` — `// [AC] REQ-FORMUX-24`; `TitleLocalPick_FillsTitleTextOnly` — `// [AC] REQ-FORMUX-25`; `TitleManualEditAfterRemotePick_RetainsPendingIdentity` — `// [AC] REQ-FORMUX-31`. Red → Green each.
- [ ] **Step 8:** `TitleSearch_PassesArtistHintWhenNonEmpty` — `// [AC] REQ-FORMUX-22`. Red → Green.
- [ ] **Step 9:** Full build + test green (Task 9's regression test must remain green). Task-log + AC matrix rows, commit:

```bash
git add MyVocaList/UI/ViewModels/SongFormViewModel.cs MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: SongFormViewModel — artist save resolution + title autocomplete/autofill (REQ-FORMUX-17..25,31,33)"
```

**Demo:** Tests prove all three artist-resolution branches + autofill state + resolution-flow invocation unchanged.

---

## Phase 4 — UI / XAML (one file per task; build between)

### Task 13: ArtistFormPage.xaml — AutocompleteField, strip removal, warn hint, confirm sheet `[SEQUENTIAL]`

**tasks.md:** identical title · Risk Medium · Review lane Standard · **E2E emulator gate before To Review**

**Files:**
- Modify: `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`
- Modify: `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml.cs`

**Interfaces:** Consumes Task 8 component + Tasks 10–11 VM members (exact names listed there). Produces the finished Artist form UI.

- [ ] **Step 1:** Read `.claude/library/dialogs-validation.md § BottomSheet State Management` and `devexpress-patterns.md` before editing (myvocalist-coding gate).
- [ ] **Step 2:** Edit `ArtistFormPage.xaml` (ONE file):
  - Replace the Name `dxe:TextEdit` with `autocomplete:AutocompleteField` — bind `Text="{Binding ArtistName, Mode=TwoWay}"`, `SearchRequestedCommand="{Binding SearchRequestedCommand}"`, `SuggestionSelectedCommand="{Binding SuggestionSelectedCommand}"`, `Suggestions="{Binding NameSuggestions}"`, `IsRemoteLookupRunning="{Binding IsRemoteLookupRunning}"`, `HasError`/`ErrorText` as before. Do NOT bind `BlurredWithoutSelectionCommand`.
  - Remove the "Search music database" `ListItem` row (REQ-FORMUX-26).
  - Rebind the existing `DuplicateSuggestions` inline block as the similar-warn hint: "Similar: X, Y — tap to pick" rows bound to `DuplicateSuggestions`, tap → `PickConfirmCandidateCommand` semantics via `SuggestionSelectedCommand`-equivalent (Task 10 Step 8 command).
  - Add the confirm `dx:BottomSheet` (ConfirmSheet-style, `BottomSheetTitle` style): candidate rows (tap → `PickConfirmCandidateCommand`) + primary `dx:DXButton` bound to `ConfirmCreateTypedCommand` with text `{Binding ConfirmSheetCreateLabel}`; `IsConfirmSheetVisible` drives Show/Close from code-behind per the BottomSheet pattern (hardware Back dismisses without saving → `DismissConfirmSheetCommand`).
  - Confirm `SafeAreaEdges="Container"` is present on the `ContentPage`.
- [ ] **Step 3:** Edit `ArtistFormPage.xaml.cs`: BottomSheet Show/Close code-behind wiring (subscribe to VM `IsConfirmSheetVisible` changes per the documented pattern). Build: `dotnet build` — 0 errors before proceeding (3-attempt cap).
- [ ] **Step 4 (E2E emulator gate):** Deploy (`dotnet build -t:Run -f net10.0-android`). Verify: typing "black sab" shows local rows instantly, remote rows under "From music database" ≈ 0.7 s after pause; no search strip; saving a similar name opens the confirm sheet; hardware Back dismisses it without saving. If emulator unavailable → status `Check build` + note.
- [ ] **Step 5:** Task-log (emulator evidence), commit:

```bash
git add MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: ArtistFormPage — AutocompleteField + confirm sheet, search strip removed (REQ-FORMUX-26,29)"
```

**Demo:** Emulator: typing "black sab" shows local rows instantly, remote rows under "From music database" ≈ 0.7 s after pause; no search strip; saving a similar name opens the confirm sheet.

### Task 14: SongFormPage.xaml — Title AutocompleteField, artist entry updates, strip removal, confirm sheet `[SEQUENTIAL — after Task 13 builds green]`

**tasks.md:** identical title · Risk Medium · Review lane Standard · **E2E emulator gate before To Review**

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`

**Interfaces:** Consumes Task 8 component + Task 12 VM members. Produces the finished Song form UI.

**Caution:** this page hosts the resolution/merge BottomSheets (BUG-023 pattern) — do NOT disturb their code-behind wiring. The YouTube strip and its section stay untouched (REQ-FORMUX-28).

- [ ] **Step 1:** Edit `SongFormPage.xaml` (ONE file):
  - Artist entry (existing `AutocompleteField`): remove the `BlurredWithoutSelectionCommand="{Binding ArtistBlurredWithoutSelectionCommand}"` binding and the `IsEnabled` lock binding (`IsArtistLocked`); bind `IsRemoteLookupRunning="{Binding IsArtistRemoteLookupRunning}"`.
  - Title entry: replace the Title `dxe:TextEdit` with `autocomplete:AutocompleteField` — `Text="{Binding SongTitle, Mode=TwoWay}"`, `SearchRequestedCommand="{Binding TitleSearchRequestedCommand}"`, `SuggestionSelectedCommand="{Binding TitleSuggestionSelectedCommand}"`, `Suggestions="{Binding TitleSuggestions}"`, `IsRemoteLookupRunning="{Binding IsTitleRemoteLookupRunning}"`, keep `HasError`/`ErrorText`/counter bindings.
  - Remove the "Search music database" `ListItem` row (REQ-FORMUX-26).
  - Add the artist confirm `dx:BottomSheet` (same ConfirmSheet pattern as Task 13, bound to Task 12's sheet members) — beside, not inside, the existing resolution/merge sheets.
  - Confirm `SafeAreaEdges="Container"`.
- [ ] **Step 2:** Edit `SongFormPage.xaml.cs`: confirm-sheet Show/Close wiring; remove any code-behind references to the deleted lock binding. Build — 0 errors (3-attempt cap).
- [ ] **Step 3 (E2E emulator gate):** Verify on emulator: blur keeps typed artist text (BUG-027 / TEST-001 step 7 scenario); remote title pick autofills Title+Artist; save with a brand-new artist name creates song+artist in one go; no search strip; YouTube strip intact; resolution/merge sheets still open for duplicate titles.
- [ ] **Step 4:** Task-log (emulator evidence), commit:

```bash
git add MyVocaList/UI/Pages/Songs/SongFormPage.xaml MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "feat: SongFormPage — Title AutocompleteField, lock removed, confirm sheet (REQ-FORMUX-16,22,26)"
```

**Demo:** Emulator: blur keeps typed artist text; remote title pick autofills Title+Artist; save with a brand-new artist name creates song+artist in one go; no search strip.

---

## Phase 5 — Picker deletion cleanup

### Task 15: Delete ArtistPickerPage + SongPickerPage and all wiring `[SEQUENTIAL — hotspot files: MauiProgram.cs, AppShell.xaml]`

**tasks.md:** identical title · Risk Medium · Review lane Elevated
**Sizing exception (explicit, carried from tasks.md):** exceeds the 5-file cap by design — deleting a dead subgraph is atomic; splitting would leave intermediate commits that do not build. The build MUST go green in this single commit.
**Irreversible-action authorization:** route removal + file deletion authorized by Helder decision 2026-07-10 (`design.md § Key Decisions — Delete ArtistPickerPage + SongPickerPage`). Record this in the task-log.

**Files:**
- Delete: `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`, `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs`, `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`, `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`, `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`, picker VM test files, picked-message class files (`ArtistPickedMessage`, canonical `SongPickedMessage` in `Contracts/Messages/` — verify by grep that no surviving consumer remains before deleting each).
- Modify: `MyVocaList/Navigation/Routes.cs` (remove `ArtistPicker = "artist-picker"`, `SongPicker = "song-picker"` — **keep `QueueSongPicker`**), `MyVocaList/AppShell.xaml(.cs)` (route registrations), `MyVocaList/MauiProgram.cs` + `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (DI registrations), `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` (`NavigateToArtistPickerCommand`, `NavigateToArtistPickerAsync`, `ArtistPickedMessage` handler), `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (`NavigateToSongPickerCommand`, `NavigateToSongPickerAsync`, `CanonicalSongPickedMessage` registration + `OnSongPicked` + `ResolveAndLockArtistAsync`), affected tests (`RouteCollisionTests`, DI regression tests, form VM tests referencing picker commands).

- [ ] **Step 1 (pre-deletion grep):** `grep -rn "ArtistPicker\|SongPicker" --include="*.cs" --include="*.xaml" .` (excluding `QueueSongPicker` matches) — record the full consumer list in the task-log. Also snapshot `grep -rln "YouTubeSearch\|QueueSongPicker"` to verify untouched afterward (REQ-FORMUX-28).
- [ ] **Step 2:** Delete the six source files + picker VM test files.
- [ ] **Step 3:** Remove wiring in dependency order: form VM navigation commands/handlers → routes (`Routes.cs`, `AppShell.xaml(.cs)`) → DI registrations → message classes (only once zero references remain) → fix affected tests (route-collision + DI regression updated, never weakened).
- [ ] **Step 4:** `dotnet build` — 0 errors (3-attempt cap). `dotnet test` — all green.
- [ ] **Step 5 (post-deletion verification):** Re-run Step 1 grep — only docs/history matches remain (POST-F2). Diff the `YouTubeSearch`/`QueueSongPicker` grep snapshot — identical (REQ-FORMUX-28). Emulator: navigate to both forms — no crash.
- [ ] **Step 6:** `.sln` check: picker source files are project-compiled (not `.sln` solution items) — no `.sln` edit expected; confirm. Task-log (grep evidence + authorization note), commit:

```bash
git add -A
git commit -m "feat: delete ArtistPickerPage + SongPickerPage and all wiring (REQ-FORMUX-27)

Authorized: Helder decision 2026-07-10 (design.md § Key Decisions). YouTubeSearchPage and QueueSongPickerPage verified untouched (REQ-FORMUX-28)."
```

**Demo:** `dotnet build` 0 errors; repo grep for `ArtistPicker|SongPicker` (excluding `QueueSongPicker`) returns only docs/history; app navigates both forms without crash.

---

## Phase 6 — Docs, guidelines, close-out

> **.sln registration status:** the three spec files of this folder are registered under solution folder GUID `{FA1234BC-0001-4000-8000-000000000045}`. `plan.md` was registered when this plan was committed. `task-log.md` (and `spec-changelog.md` if created) must be `.sln`-registered in the commit that creates them.

### Task 16: Deprecation note in `.claude/library/search-picker-pattern.md` `[P]`

**tasks.md:** identical title · Risk Low · Review lane Standard

**Files:**
- Modify: `.claude/library/search-picker-pattern.md`

- [ ] **Step 1:** At the top of the artist/song picker sections add: `> **Superseded 2026-07-10:** the artist/song picker portions of this pattern are superseded by in-field autocomplete (changes/2026-07-10-form-ux-redesign — REQ-FORMUX-26/27; pages deleted). The YouTube picker portion remains valid.`
- [ ] **Step 2:** `.claude/library/*` is not `.sln`-registered (constraints-registry) — no `.sln` edit. Task-log, commit:

```bash
git add .claude/library/search-picker-pattern.md Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/task-log.md
git commit -m "docs: deprecation note — search-picker pattern superseded by in-field autocomplete"
```

**Demo:** File opens with the supersession note at the top of the affected sections.

### Task 17: E2E emulator verification + BACKLOG + spec close-out `[SEQUENTIAL — final]`

**tasks.md:** identical title · Risk Low · Review lane Standard

**Files:**
- Modify: `Docs/Management/BACKLOG.md`, this folder's `task-log.md` (+ `spec-changelog.md` if any post-approval spec change occurred — e.g. the GAP-1 resolution; register new `Docs/` files in `MyVocaList.sln`).

- [ ] **Step 1 (E2E):** Emulator run of both Phase 4 Demo scenarios + BUG-027 re-run of `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 step 7 (typed artist text survives blur → song saves). Record evidence in task-log.
- [ ] **Step 2:** BACKLOG rows: Form UX Redesign → ✅ · BUG-027 → ✅ Fixed · BUG-029 → closed-superseded · BUG-030 → closed-answered · BUG-031/032 → closed-answered-implemented (per `requirements.md § Supersession` bug dispositions).
- [ ] **Step 3:** If GAP-1 (or any other post-approval change) altered the spec: create `spec-changelog.md` in this folder recording it + add a dated `> **Spec updated:**` note in the affected spec file + register `spec-changelog.md` in `MyVocaList.sln` under GUID `{FA1234BC-0001-4000-8000-000000000045}`.
- [ ] **Step 4:** Complete the **AC traceability matrix** in `task-log.md` — one row per REQ-FORMUX-01…33 (AC ID | Criterion | Implementation location | Test method / emulator-verified note). Missing rows = incomplete feature.
- [ ] **Step 5:** Run the Rebuild Test check (spec-writing-guide) as part of close-out review. Commit:

```bash
git add Docs/Management/BACKLOG.md Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/ MyVocaList.sln
git commit -m "docs: form-ux-redesign close-out — E2E verified, BACKLOG + bug dispositions, AC matrix"
```

**Demo:** BACKLOG shows the feature ✅ with BUG dispositions; task-log matrix has one row per REQ-FORMUX AC.

---

## Coverage check (plan self-review, done at plan time)

- **Spec coverage:** REQ-FORMUX-01…05 → Tasks 4/5/10/12(+8 rendering) · 06–08 → Tasks 6/10/11 · 09 → Task 10/13 · 10–14 → Tasks 4 (FilterSimilar), 10, 11, 13 · 15–16 → Task 9 (+14 bindings) · 17–21 → Task 12 (+5) · 22–25, 31 → Tasks 5/12/14 · 26 → Tasks 13/14 · 27 → Task 15 · 28 → Task 15 verification · 29–30 → Task 8 · 32 → Tasks 10/11 edit-mode tests · 33 → Task 12 edit-mode test. Every `design.md` interface has a producing task (DTOs → T2; repo methods → T3; suggestion services → T4/T5; `CreateArtistAsync` overload → T6; DI → T7; component additions → T8).
- **Type consistency:** DTO shapes, service signatures, and VM member names are quoted once in their producing task and referenced (not re-invented) by consuming tasks.
- **Open items:** ~~GAP-1 (Task 12 Step 6) — Helder decision A/B required at plan review.~~ ✅ RESOLVED 2026-07-10 (Helder): Option A. No open items remain — all 14 tasks unblocked.

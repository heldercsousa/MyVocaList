# Song Import & Entity Resolution — Design

> Parent: Artists & Songs Catalog · Status: 📋 Spec (in progress) · Created: 2026-06-13
> Read `requirements.md` first. Honors `code-principles.md`, `constraints-registry.md`, and CLAUDE.md constitutional constraints.

## 1. Architecture overview

The resolution logic lives entirely in the **Services layer** (constitutional: business logic in Services only). The UI presents decisions and collects confirmations via `dx:BottomSheet` (constitutional: no native dialogs). Matching uses **DB-side collation** for exact comparisons and **bounded on-device string distance** for fuzzy candidates.

```
SongPickerVM / SongFormVM (UI)
        │ candidate (manual or API result)
        ▼
ISongResolutionService.ResolveAsync(candidate) ──► SongResolution (decision + candidates + field diff)
        │                                                  │
        │ (resolves artist first)                          ▼
ISongResolutionService.CommitAsync(decision)        UI shows resolution / merge BottomSheet
        │                                                  │ user choice
        ▼                                                  ▼
SongService / ArtistService (existing) ── repositories ── AppDbContext (collation + unique index)
```

Layers affected: **Domain** (entity field, contracts, records), **Infra** (migration, repo queries, similarity impl), **Services** (resolution engine), **UI** (picker VM, sheets, form wiring, bug fixes).

## 2. Data model changes

### Song.Version (new)
- `public string Version { get; set; }` — non-null, default `""`, max length 60.
- **Collation (M1):** the `Version` column MUST be configured with `.UseCollation(CollationConstants.Default)` in `SongConfiguration`, exactly as `Title` is. Without this, the unique index dedups `Version` byte-exactly and "Live"/"live" become distinct rows, contradicting AC-5.4 and INV-2.
- Replaces unique index `IX_Songs_ArtistId_Title` → `IX_Songs_ArtistId_Title_Version` (unique; `ArtistId` plus `Title` and `Version` both under `NOCASE_NOACCENT` via column-level `UseCollation`).
- `""` = canonical version; non-empty = a deliberate variant (live/acoustic/remix).
- Rationale for non-null default `""`: SQLite treats multiple NULLs as distinct in a unique index, which would silently permit duplicate canonical rows. A non-null default makes the index enforce INV-2.

### Migration
- Add `Version TEXT NOT NULL DEFAULT ''` to `Songs`.
- Drop `IX_Songs_ArtistId_Title`; create `IX_Songs_ArtistId_Title_Version` unique with collation on all three.
- **Migration precondition (M3):** switching from `(ArtistId, Title)` → `(ArtistId, Title, Version='')` is safe ONLY because the old unique index already forbade duplicate `(ArtistId, Title)`. The migration must fail loudly (not silently drop the constraint) if a pre-existing duplicate is somehow present. Wave 2 includes an integration test asserting the new 3-column unique index rejects duplicate `(ArtistId, Title, Version)`.
- Follows the established migration pattern (clear `__EFMigrationsLock` first; see constraints-registry.md). No data wipe required — existing rows get `Version=''`.
- **Note (N5):** `Artist.ExternalId`'s unique index already exists (`ArtistConfiguration.cs`); INV-3 is pre-existing schema — no new migration work for it.

No change to `Song.ExternalId` index (stays non-unique: many songs legitimately have NULL external id). External match is by `(ExternalProvider, ExternalId)` lookup, already supported by `GetByExternalIdAsync`.

## 3. Contracts (Domain)

```csharp
// Candidate carries everything needed to resolve, source-agnostic.
public sealed record SongCandidate(
    string Title,
    string Version,                 // "" if canonical
    string? FeaturedArtists,
    string? Lyrics,
    ArtistCandidate Artist,         // mandatory — resolved before the song
    string? ExternalProvider,
    string? ExternalId);

public sealed record ArtistCandidate(
    string Name,
    string? ExternalProvider,
    string? ExternalId);

public enum ResolutionKind { NoMatch, ExactExternalMatch, ExactLocalMatch, FuzzyCandidates }

public sealed record FieldDiff(string Field, string? ApiValue, string? CurrentValue);

public sealed record SongMatch(int SongId, string Title, string Version, string ArtistName, double Score);

public sealed record SongResolution(
    ResolutionKind Kind,
    int? ExactMatchSongId,                       // set for Exact* kinds
    IReadOnlyList<SongMatch> FuzzyCandidates,    // set for FuzzyCandidates
    IReadOnlyList<FieldDiff> FieldDiffs,         // populated when target HasManualEdits
    bool TargetHasManualEdits);

public sealed record ArtistResolution(
    ResolutionKind Kind,
    int? ExactMatchArtistId,
    IReadOnlyList<(int ArtistId, string Name, double Score)> FuzzyCandidates);
```

### Service interfaces

```csharp
public interface ISimilarityScorer
{
    // 0.0 (no match) .. 1.0 (identical). Pure managed, no DB, no I/O.
    double Score(string a, string b);
}

public interface IArtistResolutionService
{
    Task<ArtistResolution> ResolveAsync(ArtistCandidate candidate, CancellationToken ct = default);
    Task<(bool success, string message, int artistId)> CommitAsync(
        ArtistCandidate candidate, ResolutionChoice choice, int? targetArtistId, CancellationToken ct = default);
}

public interface ISongResolutionService
{
    Task<SongResolution> ResolveAsync(SongCandidate candidate, CancellationToken ct = default);
    // Applies the user's decision. For updates with manual edits, acceptedFields lists fields to overwrite.
    Task<(bool success, string message, Song? song)> CommitAsync(
        SongCandidate candidate,
        ResolutionChoice choice,            // CreateNew, CreateNewVersion, UpdateExisting, AttachExternalId
        int? targetSongId,
        IReadOnlyCollection<string>? acceptedFields,
        CancellationToken ct = default);
}

public enum ResolutionChoice { CreateNew, CreateNewVersion, UpdateExisting, AttachExternalId }
```

## 4. Resolution algorithm

`ResolveAsync(SongCandidate)`:
1. **Resolve artist first** via `IArtistResolutionService.ResolveAsync`. If artist is unresolved fuzzy/no-match, the song resolution returns a state that prompts artist confirmation before song matching (UI orchestrates; Song.ArtistId is mandatory — INV-1).
2. **External match:** if candidate has `(provider, externalId)`, call `SongRepository.GetByExternalIdAsync`. Hit → `ExactExternalMatch` (target id set; compute `FieldDiffs` if target `HasManualEdits`).
3. **Exact local match:** `ExistsByTitleVersionForArtistAsync(artistId, title, version)` under collation. Hit → `ExactLocalMatch` (+ diffs as above).
4. **Fuzzy:** retrieve a **bounded pool** via `GetFuzzyCandidatePoolAsync(artistId, titlePrefixToken, take)` — a collation `LIKE titlePrefixToken%` query limited to `take` (default 50) rows for that artist — then score each with `ISimilarityScorer`; keep those ≥ threshold (default 0.82, **provisional pending the Wave 0 spike** — N2). Non-empty → `FuzzyCandidates`. Else `NoMatch`.
   - **`titlePrefixToken` derivation (N1):** the first whitespace-delimited token of the trimmed candidate title, capped at 12 chars (e.g. `"Bohemian Rhapsody (Live)"` → `"Bohemian"`). This keeps the pool query index-friendly while tolerant of trailing variant suffixes. Empty/whitespace title → empty pool (resolves `NoMatch`).

`CommitAsync`:
- `CreateNew` / `CreateNewVersion` → `SongService.CreateSongAsync(... externalId, externalProvider ...)` with the resolved `artistId` and chosen `Version` (CreateNewVersion requires non-empty Version — AC-1.2).
- `UpdateExisting` → load target; if `HasManualEdits` apply only `acceptedFields`, else overwrite non-empty API fields; persist external identity if absent.
  - **Mergeable field set (N4):** `Title`, `FeaturedArtists`, `Lyrics`, `Version`. **`ArtistId` is NOT mergeable here** — changing a song's copyright artist is a distinct operation handled by `IArtistResolutionService`, never by the song merge sheet (protects INV-1). `FieldDiff`/merge sheet rows are restricted to this set.
- `AttachExternalId` → set `ExternalProvider`/`ExternalId` on the exact-local target without altering other fields.

`HasManualEdits` semantics: set `true` only on user-driven field edits (form), never by import commit. Import commits preserve the existing flag.

## 5. Fuzzy library (Wave 0 spike)

**RESOLVED by Wave 0 spike (`findings.md`):** use **FuzzySharp 2.0.2** (FuzzyWuzzy port, `Fuzz.TokenSetRatio` normalized to 0..1). Pure managed, builds clean on `net10.0-android`, no native dependency. `ISimilarityScorer` wraps it so it stays swappable.

**Mandatory in-memory normalization inside `SimilarityScorer` (spike finding):** FuzzySharp compares raw code points, so `Score(a,b)` MUST NFD-normalize internally — `String.Normalize(NormalizationForm.FormD)` → drop `NonSpacingMark` chars → `ToLowerInvariant` — before scoring (raw "Björk"/"Biork" = 0.60; normalized = 0.80). This does **not** violate the constraints-registry "no C#-side normalization" rule: that rule governs DB search/uniqueness/dedup queries (full-scan + accent-correctness rationale). Here normalization is in-memory only, over a bounded pool already retrieved by DB collation, used solely to surface advisory candidates the user confirms; the DB unique index + collation remain the sole authority for the insert/update decision, and nothing normalized is persisted. See `findings.md` for the full justification. Default threshold `0.82` lives in a `SimilarityConstants` constant; it is **provisional** until the Wave 0 spike + Wave 5 smoke test validate it against real accented samples (N2) — the value may be tuned without changing any contract.

> **Spec updated 2026-06-14 (Wave 3B):** `SimilarityConstants` lives in `Domain/Resolution/SimilarityConstants.cs` (NOT Infra) — Services consumes it and cannot reference Infra (layer constraint). The `SimilarityScorer` impl stays in Infra (FuzzySharp is there); Infra re-exports the constants via a `global using` alias for backward compatibility.

## 6. UI

- **SongPickerViewModel** (new, fixes BUG-010): injects `IMusicMetadataService`; `SearchCommand` (`allowConcurrentExecutions:false`, BUG-006), `SelectResultCommand` sends `SongPickedMessage(MusicSearchResultDto)`. `SongPickerPage` code-behind injects this VM (not `QueueSongPickerViewModel`). DI-registered in `MauiProgram.cs`.
- **Resolution BottomSheet:** shown when `Kind != NoMatch`. Title "This looks like an existing song". Lists exact/fuzzy target(s). Actions: Update existing · Save as new version (reveals Version entry) · Cancel. MD3 styling. **(N6)** Per the project's "style-must-exist-before-use" rule, Wave 4 has a hard dependency on the `BottomSheetTitle` style existing in `MaterialStyles.xaml`; BUG-004 (missing style) is therefore folded into Wave 4 scope — add the style (MD3 titleLarge: 22sp RobotoRegular, OnSurface) before any sheet references it.
- **Merge BottomSheet:** shown when target `HasManualEdits` and diffs exist. One row per `FieldDiff`: field name, current value, API value, a toggle to accept API. Apply commits accepted fields only.
- **SongFormViewModel:** on save, build `SongCandidate`, call `ResolveAsync`; `NoMatch` → direct create, else present sheet. Artist field per BUG-008 (autocomplete-only, blur-clear, edit pre-pop, lock on API origin). Buffer YouTube URLs; `CreateSongWithUrlsAsync` atomic save (BUG-009).

### SaveAsync orchestration (Wave 4B, implemented 2026-06-14)

`SongFormViewModel.SaveAsync` new-song flow:
1. Validate title (via `ISongService.ValidateTitleInput`) and artist selection. Return early with field errors if invalid.
2. Build `SongCandidate(Title, SongVersion, FeaturedArtists, Lyrics, ArtistCandidate(SelectedArtistName, SelectedProvider, SelectedExternalId), SelectedProvider, SelectedExternalId)`.
3. `var res = await _songResolution.ResolveAsync(candidate)`.
4. **`res.Kind == NoMatch`** → call `CreateSongWithUrlsAsync(SelectedArtistId, Title, Version, FeaturedArtists, Lyrics, ExternalId, Provider, _pendingRawUrls)` → snackbar + nav-back on success.
5. **`res.Kind` is any non-NoMatch** → stash `_pendingCandidate` + `_pendingResolution`; set `IsResolutionSheetVisible = true`; populate `ResolutionCandidates` from ExactMatchSongId or FuzzyCandidates.
   - **"Select" (Update existing):** `SelectResolutionCandidateCommand(SongMatch)` sets `SelectedResolutionTargetId` and calls `ConfirmUpdateExistingAsync`.
     - If `TargetHasManualEdits` and `FieldDiffs` non-empty → populate `MergeFieldRows`; set `IsMergeSheetVisible = true`.
     - Else → `CommitAsync(UpdateExisting, targetId, null)` → snackbar + nav-back.
   - **"Save as new version":** `ConfirmSaveAsNewVersionCommand` — validates non-empty `SongVersion` (AC-1.2 block) → `CreateSongWithUrlsAsync(... version ...)`.
   - **"Cancel":** `DismissResolutionSheetCommand` — clears pending context.
   - **Merge confirmed:** `ConfirmMergeCommand` — collects `acceptedFields` from toggled rows → `CommitAsync(UpdateExisting, targetId, acceptedFields)`.
6. Any exception in save paths is caught; log + show error snackbar (BUG-005, never silent).

Edit mode does NOT go through resolution — calls `UpdateSongAsync` directly (preserves existing behavior).

**BUG-008 (artist field):** `ArtistBlurredWithoutSelectionCommand` clears field when no selection; restores prior selection name when user had one. `InitializeArtistField()` called from `OnAppearing` after all query props set. `IsArtistLocked = true` when `SelectedExternalId` is populated (API import).

**BUG-009 (buffered URLs):** `_pendingRawUrls` list holds raw URL strings in new-song mode. `AddFromPasteAsync`/`NavigateToYouTubeSearchAsync` validate and buffer when `!SongId.HasValue`. `RemoveUrlCommand` removes from buffer directly (no DB call). `SaveAsync` passes `_pendingRawUrls` to `CreateSongWithUrlsAsync`.

**SongPickedMessage wiring:** VM registers `CanonicalSongPickedMessage` (alias for `Contracts.Messages.SongPickedMessage`) to avoid ambiguity with legacy `QueueSongPickerViewModel.SongPickedMessage`. On receipt: populate title/featured; stash `ExternalId`/`Provider`; call `ResolveAndLockArtistAsync` for exact-match artist lock.
- **Picker pages:** suppress Shell back chrome (BUG-007).

## 7. Service additions

- `ISongService.CreateSongWithUrlsAsync(artistId, title, version, featured, lyrics, externalId, externalProvider, IEnumerable<string> urls, ct)` — single transaction over `SongRepository` + URL repository. **(N3)** Both repositories MUST share the same scoped `AppDbContext` so one `SaveChangesAsync` commits atomically; the Builder must not inject a second context. A failure rolls back song + URLs together (AC-6.2).
- `ISongService.UpdateSongAsync` gains `externalId`/`externalProvider` params (M2) so the update path can persist external identity — currently it cannot.
- `ISongRepository`:
  - `ExistsByTitleVersionForArtistAsync(artistId, title, version, ct)` and overload with `excludeId`.
  - `GetFuzzyCandidatePoolAsync(artistId, titlePrefixToken, take, ct)` — bounded collation query.
- `IArtistRepository`: `GetFuzzyCandidatePoolAsync(namePrefixToken, take, ct)`.

## 8. Testing strategy

- **Services (Level A, full TDD):** resolution kind selection (all 4 branches × external/local/fuzzy), CreateNewVersion empty-version rejection, conflict merge applies only accepted fields, artist-resolved-first ordering, external-id persistence.
- **Infra (Level B):** migration produces the 3-column unique index; `ExistsByTitleVersionForArtistAsync` collation correctness ("Café"/"cafe"); bounded pool query returns ≤ take and respects artist scope; unique-index violation on duplicate `(ArtistId, Title, Version)`.
- **Similarity (Level A / property-based):** identical strings → 1.0; accent/case variants score high; threshold boundary; deterministic (no I/O).
- **ViewModel (Level A):** state transitions for resolution sheet visibility, buffered-URL behavior, double-tap guard. No `Shell.Current` in tested paths.
- **Emulator smoke gate (Wave 5):** API search → resolution sheet → update vs new-version; manual near-duplicate → fuzzy confirm; edited-record import → merge sheet; add-URL-before-save round trip.

Every AC in `requirements.md` maps to ≥1 test (traceability matrix in `task-log.md`).

## 9. Wave plan (DRY Onion · worktrees mandatory · ≤4 parallel)

| Wave | Layer | Produces |
|------|-------|----------|
| 0 | Spike | Fuzzy library validation on Android → `findings.md` |
| 1 | Domain | `Song.Version`; candidate/resolution records + enums; `ISimilarityScorer`, `IArtistResolutionService`, `ISongResolutionService` interfaces |
| 2 | Infra | Migration (Version + 3-col unique index); repo methods (exact-version, bounded fuzzy pool); `ISimilarityScorer` impl; `SimilarityConstants` |
| 3 | Services (TDD) | `ArtistResolutionService`, `SongResolutionService`, conflict-merge, `CreateSongWithUrlsAsync` + unit tests |
| 4 | UI | `SongPickerViewModel` + page/DI fix (BUG-010); resolution & merge BottomSheets; form wiring; BUG-005/006/007/008/009 |
| 5 | Tests + gate | Integration tests, AC traceability matrix, emulator smoke test |

Hotspot files (single-writer): `MauiProgram.cs`, `AppDbContext.cs`, `Routes.cs`/`AppShell.xaml.cs`, `*Migration.cs`, `tasks.md`. Sequence these.

## 10. Key decisions (locked 2026-06-13 with Helder)

1. **Version variants** are first-class via `Song.Version`; near/exact matches prompt a confirm sheet rather than hard-rejecting (US-1).
2. **Matching = exact (collation) + bounded fuzzy** with user confirm; no AI embeddings (US-5).
3. **Manual edits are never silently overwritten** — field-level merge sheet (US-4).
4. **Blocking bugs (005/006/007/008/010, plus 009 atomic save) are folded in** so the engine is end-to-end demoable.

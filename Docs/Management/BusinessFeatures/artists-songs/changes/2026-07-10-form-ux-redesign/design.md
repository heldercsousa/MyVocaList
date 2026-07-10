# Artist & Song Form UX Redesign — Design (dated change spec)

> Feature folder: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`
> Requirements: `requirements.md` in this folder (REQ-FORMUX-01…33)
> Cross-references: `Docs/Management/BusinessFeatures/artists-songs/design.md` (original),
> `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/design.md` (resolution engine — consumed unchanged)
> Created: 2026-07-10 · Encodes Helder-approved design of 2026-07-10

## Architecture

| Layer | Change |
|-------|--------|
| **Domain** | No entity/schema change. `Artist` already carries `ExternalId` + `ExternalProvider` (`Domain/Entity/Artist.cs`) — verified 2026-07-10; **no migration is needed**. New suggestion DTOs live in Contracts. |
| **Infra** | One additive repository method per repository (`ArtistRepository`, `SongRepository`) for collation-based batch name/title lookup used by remote dedup. No schema change. |
| **Services** | New `IArtistSuggestionService` + `ISongSuggestionService` (all lookup/dedup/similarity business logic). `ArtistService.CreateArtistAsync` gains optional external-identity parameters. Existing `IMusicMetadataProvider` implementations (DeezerProvider, MusicBrainzProvider) and `ISimilarityScorer` are reused as-is. |
| **UI — ViewModels** | `ArtistFormViewModel` and `SongFormViewModel` rewired to the suggestion services; blur-clear handler and `IsArtistLocked` removed; confirm-sheet state added. ViewModels stay thin — they orchestrate calls and hold observable state only. |
| **UI — Pages/Components** | ArtistFormPage Name → `AutocompleteField`; SongFormPage Title → `AutocompleteField`; search strips removed; confirm `dx:BottomSheet` added to both forms. `AutocompleteField` gets minor **additive** capabilities (governed component — four gates). |
| **Deleted** | `ArtistPickerPage`(.xaml/.cs), `SongPickerPage`(.xaml/.cs), `ArtistPickerViewModel`, `SongPickerViewModel`, their routes/DI/picked-messages. |

Constitution check: business logic in Services only (suggestion services own dedup/similarity); no native
dialogs (`dx:BottomSheet` for the confirm sheet); DevExpress-first (existing `AutocompleteField` builds on
DX `TextEdit`; no new stock-MAUI components); no C#-side string normalization (all exactness via DB collation).

## Interfaces (Services layer — new)

```csharp
// Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs
public sealed record ArtistSuggestionDto(
    int? LocalId,              // non-null => local record
    string Name,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    bool IsExactMatch);        // collation-equal to the search term (computed at fetch time)

// Contracts/DTOs/Suggestions/SongSuggestionDto.cs
public sealed record SongSuggestionDto(
    int? LocalId,
    string Title,
    string ArtistName,
    int? LocalArtistId,        // non-null when the suggestion's artist exists locally
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote);

// Services/IArtistSuggestionService.cs
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

// Services/ISongSuggestionService.cs
public interface ISongSuggestionService
{
    Task<IReadOnlyList<SongSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — same provider order and dedup/failure semantics as
    /// IArtistSuggestionService.GetRemoteAsync (MusicBrainz first, Deezer fallback per AC-4.2).</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetRemoteAsync(
        string term, string? artistHint, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct = default);
}
```

Provider order (both services): **MusicBrainz first, Deezer fallback when MusicBrainz returns empty or
errors** — AC-4.2 remains in force (see `requirements.md § Supersession`). Never query both in parallel.

Modified existing signature (Services):

```csharp
// ArtistService — external identity persisted on create (REQ-FORMUX-07)
Task<(bool success, string message, Artist? artist)> CreateArtistAsync(
    string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);
```

Additive repository methods (Infra, used by remote dedup — one collation query per lookup batch, never per-candidate):

```csharp
// IArtistRepository
Task<IReadOnlyList<Artist>> GetByNamesCollatedAsync(IEnumerable<string> names, CancellationToken ct = default);
// ISongRepository
Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(IEnumerable<string> titles, CancellationToken ct = default);
```

DI (Scoped, per convention): `IArtistSuggestionService`, `ISongSuggestionService` in
`MyVocaList/Extensions/ServiceCollectionExtensions.cs` (`AddAppServices()`).

## Remote staging (timing model)

```
keystroke ──▶ AutocompleteField debounce 300 ms ──▶ SearchRequestedCommand
      VM: GetLocalAsync(term)  ──▶ render local rows immediately
      VM: schedule remote timer 400 ms (cancel on next SearchRequested)
            └─▶ show loading-hint row ──▶ GetRemoteAsync(term, localResults)
                  ├─ results  ──▶ append under "From music database" section marker
                  └─ failure/empty ──▶ remove loading-hint row, keep local-only (log)
Total nominal latency for remote rows: ≈ 700 ms after the last keystroke.
```

- The remote timer and any in-flight remote call are cancelled by: a new keystroke/search request, a
  suggestion pick, page navigation, or Save.
- Stale remote responses (term changed) are discarded via `CancellationToken` — never rendered.
- Debounce staging is Level B testable (timer scheduling extracted so the delay is injectable in tests).

## AutocompleteField — additive component change (governed)

`AutocompleteField` (`MyVocaList/UI/Components/AutocompleteField/`) is a governed component with two
consumers (PersonFormPage, SongFormPage — soon three with ArtistFormPage). Changes are **additive only**:

- `AutocompleteSuggestion` gains an origin/section notion so remote rows render a "From music database"
  section header (or supporting text) — default value keeps current rendering for existing consumers.
- A loading-hint row capability (e.g. `IsRemoteLookupRunning` BindableProperty rendering one hint row).
- `BlurredWithoutSelectionCommand` remains on the component (PersonFormPage may still use it); the Song
  form simply stops binding a clearing handler to it. If implementation finds the component itself forces
  clearing behavior, that is a non-additive change → full component-change-governance gates apply before edit.

All four governance gates are encoded as a dedicated task (`tasks.md` Phase 2) — consumer map, per-consumer
risk table, MD3 review (m3.material.io menus/lists — section headers use list subheader anatomy), Helder approval.

## Page structure & interaction flows

### ArtistFormPage

- `Name` `dxe:TextEdit` → `autocomplete:AutocompleteField` (Text two-way, SearchRequestedCommand,
  SuggestionSelectedCommand, Suggestions, loading-hint binding).
- Search strip ("Search music database" `ListItem` row) removed.
- The existing dead `DuplicateSuggestions` inline block ("Artist already exists", never populated) is
  **repurposed** as the similar-match inline warn: "Similar: X, Y — tap to pick", fed from
  `FilterSimilar(typed, cachedSuggestions)` — no refetch (REQ-FORMUX-10).
- New confirm `dx:BottomSheet` (ConfirmSheet-style; `BottomSheetTitle` style; code-behind Show/Close
  pattern per `dialogs-validation.md § BottomSheet State Management`): candidate rows (tap to pick) +
  "Create '<typed>'" primary button.

Save flow (create mode):

```
Save tapped
 ├─ validation fails (empty/short/long) ──▶ inline error (unchanged)
 ├─ exact local match (collation)       ──▶ existing uniqueness error "already registered" (unchanged)
 ├─ similar match (cached, ≥ threshold) ──▶ confirm sheet
 │     ├─ pick local candidate  ──▶ navigate to Edit Artist for that record
 │     ├─ pick remote candidate ──▶ fill name + external identity, sheet closes (user saves again)
 │     └─ "Create '<typed>'"    ──▶ CreateArtistAsync(name, externalId, provider) ──▶ back + snackbar
 └─ no match                            ──▶ CreateArtistAsync(name, externalId, provider) ──▶ back + snackbar
```

External identity: remote pick stores pending `ExternalId`/`ExternalProvider`; manual name edit after the
pick clears them (REQ-FORMUX-08); create passes them through (REQ-FORMUX-07 — fixes the drop-on-save gap).

### SongFormPage

- Artist entry: keeps `AutocompleteField`; **blur-clear handler deleted** (the VM callback wired to
  `BlurredWithoutSelectionCommand` that clears text — see `SongFormViewModel` "Invoked by AutocompleteField
  when user blurs without selecting a suggestion (BUG-008)"); `IsArtistLocked` property, its `IsEnabled`
  binding, and all assignments retired. Suggestions become local + remote via `IArtistSuggestionService`.
- Title entry: `dxe:TextEdit` → `AutocompleteField` bound to `ISongSuggestionService` (artistHint = current
  artist text when non-empty).
- Search strip removed. YouTube strip and its section stay untouched.
- Same confirm sheet pattern as ArtistFormPage for the artist similar-match case.

Artist state machine (SongForm):

```
ArtistEntry states:
  FreeText           → LocalSelected      (tap local suggestion — LocalArtistId set)
  FreeText           → RemotePicked       (tap remote suggestion — name + pending external identity, marked-for-create)
  LocalSelected      → FreeText           (any keystroke — LocalArtistId cleared, TEXT KEPT)
  RemotePicked       → FreeText           (any keystroke — external identity cleared, TEXT KEPT)
  any state          → same state         (blur — NO transition, NO text change)   ← BUG-027 kill
```

Save flow (artist resolution, when no `LocalArtistId` selected):

```
Save tapped (title/artist validation passed)
 ├─ exact local artist match (collation) ──▶ auto-attach silently ──▶ continue song save
 ├─ similar artist match (cached)        ──▶ confirm sheet
 │     ├─ pick candidate ──▶ attach as song's artist ──▶ continue song save
 │     └─ "Create '<typed>'" ──▶ transparent create path
 └─ no match ──▶ transparent create: artist (with external identity if marked-for-create)
                 + song persisted in ONE transaction (existing atomic-save lever,
                 mirrors song-import-resolution AC-2.5 artist-first ordering)
```

Title remote pick (autofill only — REQ-FORMUX-23/24): fills Title + Artist (local if exists, else
marked-for-create) + pending song external identity. Save then enters the **existing** resolution/merge
flow from `song-import-resolution/design.md` unchanged (`ISongResolutionService` /
`IArtistResolutionService` untouched).

### Deletion cleanup

Delete and de-register (verified consumer list, 2026-07-10):

| Artifact | Files |
|----------|-------|
| Pages | `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml(.cs)`, `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml(.cs)` |
| ViewModels | `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`, `MyVocaList/UI/ViewModels/SongPickerViewModel.cs` |
| Routes | entries in `MyVocaList/Navigation/Routes.cs`, `MyVocaList/AppShell.xaml(.cs)` |
| DI | registrations in `MyVocaList/MauiProgram.cs` / `ServiceCollectionExtensions.cs` |
| Messages | artist/song picked-result messenger messages + their handlers in `ArtistFormViewModel` / `SongFormViewModel` |
| Tests | picker VM test files removed; DI-resolution regression tests updated |

Stays: `YouTubeSearchPage` (+ VM/route/messages), SongForm YouTube strip, `QueueSongPickerPage` (+ VM/route/messages).
`.claude/library/search-picker-pattern.md` gets a deprecation note (artist/song picker portions superseded;
YouTube picker portion still valid) — `tasks.md` Phase 6.

## Invariants & Postconditions

- **INV-F1** User-typed text in ArtistForm Name, SongForm Artist, and SongForm Title is never mutated or
  cleared by focus changes or failed lookups — only by the user or an explicit suggestion pick.
- **INV-F2** No suggestion interaction persists anything; the first write is Save.
- **INV-F3** Remote provider availability never gates saving; the worst case is local-only suggestions.
- **INV-F4** All exact matching is DB-collation-based; no C#-side normalization anywhere in the new code (HARD RULE).
- **INV-F5** After any successful song save, `Song.ArtistId` is non-null and points to a persisted artist (existing INV-1 upheld).
- **POST-F1** After creating an artist from a remote pick (name untouched), the Artist row has that pick's `ExternalId` + `ExternalProvider`.
- **POST-F2** After this feature ships, no code path references `ArtistPickerPage`/`SongPickerPage` routes, types, or messages.

## Lyrics entry assessment (recorded 2026-07-10 — no behavior change)

The song add/update lyric-entry definition was reviewed 2026-07-10 and **kept as-is**: a plain multi-line
editor, max 10 000 characters, optional, plain text. The complexity correctly lives in the
resolution/merge flow, where lyrics is one diff-row protected by `HasManualEdits` — that design is sound.
Noted soft spots, deliberately not addressed here: (a) there is no lyrics *source* yet — manual paste only,
so the lyrics merge path is untestable end-to-end until a lyrics API exists (`ILyricsProvider` placeholder);
(b) the 10 000-char `MaxLength` is arbitrary and not validated service-side (UI-only cap). Both stay as
recorded observations for the future Lyrics API spec.

## Key Decisions (Helder, 2026-07-10)

### Decision: Remote trigger = local-first, remote-on-pause
**Chosen approach:** Local suggestions render immediately on the component's 300 ms debounce; the remote lookup is staggered 400 ms later (≈ 700 ms after the last keystroke).
**Alternatives considered:** (a) parallel local+remote on every debounce — rejected: hammers providers on every pause, most keystrokes never need remote data; (b) explicit user tap to search remote — rejected: reintroduces the search-strip friction this feature removes.
**Reversibility:** Easily reversible (timer constant + scheduling live in one VM/service seam).
**Rationale:** Zero perceived latency for the common local case; remote enrichment arrives only when the user actually pauses.

### Decision: Delete ArtistPickerPage + SongPickerPage
**Chosen approach:** Full deletion (pages, VMs, routes, DI, messages). YouTubeSearchPage stays.
**Alternatives considered:** Keep the pages as a secondary entry point — rejected: no remaining entry point after the search strips go; dead routes and dead messenger wiring are maintenance liabilities (BUG-029 crash lived in exactly that dead strip).
**Reversibility:** Reversible with effort (git history preserves the pages; routes/DI would need re-registering). Flagged: route removal is on the implementor irreversible-actions list — authorization is this Helder decision.
**Rationale:** One way to do a thing; the autocomplete supersedes the picker flow entirely.

### Decision: Unified similar-match resolution pattern
**Chosen approach:** Inline warn hint (repurposed `DuplicateSuggestions` block) + confirm `dx:BottomSheet` on Save, applied identically to ArtistForm Name and SongForm Artist. The song-title resolution/merge sheet from song-import-resolution stays unchanged.
**Alternatives considered:** (a) blocking validation error on similar names — rejected: similar ≠ duplicate, admins legitimately register near-identical names; (b) reusing the song resolution sheet for artists — rejected: that sheet's semantics (update/new-version) don't apply to a name-only entity.
**Reversibility:** Easily reversible.
**Rationale:** Same mental model on both forms; warn-then-confirm keeps duplicates deliberate without ever blocking the typed name.

### Decision: Bottom-sheet form conversion is a separate follow-up feature
**Chosen approach:** Out of scope here; tracked as its own BACKLOG row (*Form presentation — bottom-sheet/modal conversion*, 2026-07-10).
**Alternatives considered:** Bundling it into this redesign — rejected: independent blast radius, and the autocomplete dropdown-in-sheet interaction needs its own design pass.
**Reversibility:** Not applicable (scoping decision).
**Rationale:** Keeps this feature shippable and reviewable; avoids compounding two UX overhauls.

### Decision: Remote title pick = autofill only, persist on Save
**Chosen approach:** Picking a remote song fills Title/Artist/external identity in the form; nothing is written until Save, which runs the existing resolution/merge flow unchanged.
**Alternatives considered:** Immediate upsert on pick — rejected: violates the form's mental model (Save is the write), bypasses the resolution engine's user confirmations, and breaks INV-F2.
**Reversibility:** Easily reversible.
**Rationale:** One write path (the resolution engine) keeps dedup/merge guarantees intact; the pick is just fast data entry.

## Testing strategy (per `.claude/rules/testing.md` risk tiers)

| Area | Level | Tests |
|------|-------|-------|
| Suggestion services — remote/local merge, 3-tier dedup order, similarity threshold branches, provider-failure → empty list | **A** | Full TDD, Moq'd `IMusicMetadataProvider` + `ISimilarityScorer`; every dedup branch |
| VM save paths — create-on-no-match, similar-triggers-sheet, exact auto-attach, remote-pick autofill state, external-identity pass-through | **A** | Full TDD on `ArtistFormViewModel` / `SongFormViewModel` (Moq'd services) |
| Blur-keeps-text regression (BUG-027) | **A** | MANDATORY Red-first against current behavior, then Green (Critical-severity rule) |
| Remote staging / debounce scheduling | **B** | Injectable delay; happy path + cancellation-on-new-keystroke |
| Repository collation batch lookups | **B** | Real SQLite temp DB (never in-memory provider), accent/case cases |
| DI registrations, DTO records | **C** | No mandatory test (decision documented in task-log) |

Providers are always mocked via `IMusicMetadataProvider`; no test calls Deezer/MusicBrainz. `Shell.Current`
is never called in VM tests (navigation behind interface). AC traceability matrix (REQ-FORMUX-NN → test)
is produced in the task-log at review per `testing.md`.

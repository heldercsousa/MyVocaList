# Design: Song form title-first, one search over local and music APIs

> **Status: DRAFT, in review (2026-09-25).** Requirements: `requirements.md` (REQ-SFT-01…52).
> Evidence: `findings.md` (same folder) and a scratchpad probe run on 2026-09-24 against the app's
> bundled SQLite build on desktop (§ 6.1; Android is gated by spike G5). Current-behaviour claims cite
> `file:line` on `develop` at `f9a67f62`.

## 1. Architecture

| Layer | Change |
|-------|--------|
| **Domain** | Drop `HasManualEdits` (Song, Artist). New read model `SongSearchHit`. `ISongRepository`/`IArtistRepository` gain search/lookup methods. `ISimilarityScorer.RankScore`. Save request/outcome records (§ 3.5). `ISongResolutionService`/`IArtistResolutionService` shrink to `ResolveAsync`. `ResolutionChoice` and `FieldDiff` deleted. |
| **Infra** | Migrations M1–M3 (§ 7). FTS5 trigram tables `SongSearch` and `ArtistSearch` + triggers. New repository queries. `SimilarityScorer.RankScore`. `UseSqlReturningClause(false)` on Songs and Artists. |
| **Services** | `SongService.SaveFromFormAsync` holds every Save decision (§ 3.5) and delegates writes to `CreateSongWithUrlsAsync`/`UpdateSongAsync`, which create a typed artist inside the song's unit of work. `SongSuggestionService` local and remote title search. `ArtistResolutionService` retrieves near-duplicate candidates from `ArtistSearch`. `DeezerProvider` plain query. Resolution `CommitAsync` paths deleted. |
| **UI** | `SongFormViewModel`/`SongFormPage` rebuilt on the shared Field `AutocompleteDXField` (Title + Artist). `ArtistFormViewModel`/`ArtistFormPage` Name becomes the Field (§ 4.1). The VM maps save outcomes to sheets and navigates; it decides nothing. Picker pages, inline create and merge sheet deleted. |

Business rules stay in Services (dedup, ranking, artist reuse/creation, duplicate guards,
uniqueness), per the unamendable constraint in `CLAUDE.md`. Today the VM runs the resolution ladder
itself (`SongFormViewModel.cs:623-665`); this design moves that decision into the Service.

## 2. What exists today (evidence)

- Title is a plain `dxe:TextEdit` (`SongFormPage.xaml:87-88`); Artist is the first field (`:28-41`).
- The only live third-party entry is the Song picker (`SongFormPage.xaml:122-137` →
  `SongFormViewModel.cs:899-902`), which cannot work while BUG-082 stands (findings § 0.1).
- `ISongSuggestionService`/`SongSuggestionService` exist, are tested and are **not registered**
  (`ServiceCollectionExtensions.cs:19-67` has no line for them). Local search is a title-prefix LIKE
  (`SongSuggestionService.cs:54-60` → `SongRepository.GetPagedAsync`). Remote is MusicBrainz-first
  (`SongSuggestionService.cs:95-105`).
- New-song Save: the VM calls `ResolveAsync` and opens sheets (`SongFormViewModel.cs:623-665`);
  "Update existing" overwrites the stored song with typed values (`:702-748`, `SongResolutionService`
  `ApplyUpdate`); merge sheet (`:798-837`, `SongFormPage.xaml:353-410`).
- Edit Save bypasses resolution and checks title-only uniqueness (`SongService.cs:155`), while create
  checks title + version (`SongService.cs:219`).
- `CreateSongWithUrlsAsync` needs an existing `artistId` (`SongService.cs:212-214`); inline create writes
  the artist immediately, outside the song's unit of work (`SongFormViewModel.cs:347-361`).
- Artist near-duplicate retrieval is `LIKE '<first token>%'` (`ArtistResolutionService.cs:183-195`,
  `DerivePrefixToken`), so "Metalica" can never retrieve "Metallica".

## 3. Interfaces

Tuple returns follow `code-style-reference.md § Service Return Patterns`. Every repository used inside
a unit-of-work lambda is resolved from the lambda's `sp` (REQ-UOW-28/37; enforced by
`MyVocaList.Tests/Architecture/UnitOfWorkReadScopeTests.cs`).

### 3.1 Domain

```csharp
// Domain/ReadModels/SongSearchHit.cs (new)
public sealed record SongSearchHit(
    int SongId, string Title, string Version, int ArtistId, string ArtistName,
    string? ExternalProvider, string? ExternalId);

// Domain/RepositoryInterface/ISongRepository.cs (added members)
/// Local title-field search (REQ-SFT-02, 30-34). Infra chooses the strategy:
/// FTS5 trigram OR-match when the query has a word of >= 3 chars, else collated title prefix.
/// Returns at most `take` rows in DB relevance order (bm25, then shorter title).
Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(string query, int take, CancellationToken ct);
/// REQ-SFT-05(a): songs holding any of the given external ids for one provider.
Task<IReadOnlyList<SongSearchHit>> FindByExternalIdsAsync(string provider, IReadOnlyCollection<string> externalIds, CancellationToken ct);
/// REQ-SFT-05(b): collated (title, artist name) matches with the given version (""), batch.
Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(IReadOnlyCollection<(string Title, string ArtistName)> pairs, string version, CancellationToken ct);
/// REQ-SFT-42: the id of the collated (artist, title, version) match, or null. Replaces the
/// Exists + pool re-match + OrdinalIgnoreCase path (SongResolutionService.cs:96-127).
Task<int?> FindIdByTitleVersionForArtistAsync(int artistId, string title, string version, int? excludeId, CancellationToken ct);
/// REQ-SFT-26: true when another song holds this external identity.
Task<bool> ExistsByExternalIdentityAsync(string provider, string externalId, int? excludeId, CancellationToken ct);

// Domain/RepositoryInterface/IArtistRepository.cs (added; replaces the prefix pool used by
// ArtistResolutionService, ArtistResolutionService.cs:183-195)
/// REQ-SFT-19: up to `take` artists from the ArtistSearch trigram index (OR of the query's 3-char
/// windows), bm25 order. Names shorter than 3 chars fall back to the collated prefix query.
Task<IReadOnlyList<Artist>> SearchNameCandidatesAsync(string name, int take, CancellationToken ct);

// Domain/ServicesInterfaces/ISimilarityScorer.cs (added)
/// Display-order score in [0,1] for a bounded, DB-retrieved candidate list (§ 6.3, exception § 6.4).
/// Never used to filter, deduplicate or test uniqueness.
double RankScore(string query, string candidate);
```

`ISongResolutionService` keeps only `ResolveAsync`. `SongResolution` loses `FieldDiffs` and
`TargetHasManualEdits`; `IArtistResolutionService` keeps only `ResolveAsync`, whose fuzzy tier now
reads `SearchNameCandidatesAsync` (pool `SimilarityConstants.PoolSize`) and keeps the existing
`Score ≥ 0.82` threshold. "Metalica" vs "Metallica" retrieves on shared trigrams ("met", "eta",
"tal", "lic", "ica") and scores 0.94 (`TokenSetRatio`, one inserted letter). `ResolutionChoice`
(`Domain/Resolution`) and `FieldDiff` (`ResolutionResults.cs:8-11`) are deleted.

### 3.2 `ISongService` write methods (modified)

```csharp
/// artistId XOR newArtistName: exactly one must be supplied, else (false, "Artist is required", null).
/// newArtistName: the artist is created in THIS unit of work via IArtistService.CreateArtistAsync
/// resolved from sp (ambient join), then IUnitOfWork.FlushAsync materialises its Id (REQ-UOW-35)
/// before the song is staged. Any later failure rolls both back (REQ-SFT-18).
/// Rejects an external identity already held by another song (REQ-SFT-26).
Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
    int? artistId, string? newArtistName, string title, string version,
    string? featuredArtists, string? lyrics, string? externalId, string? externalProvider,
    IEnumerable<string> urls, CancellationToken ct = default);

/// hasManualEdits removed (REQ-SFT-37). artistId / newArtistName: at most one; both null = keep
/// the stored artist. Uniqueness = (effective artist, title, effective version) excluding self
/// (REQ-SFT-29; today title-only, SongService.cs:155).
Task<(bool success, string message)> UpdateSongAsync(
    int id, string title, string? featuredArtists, string? lyrics,
    string? externalId = null, string? externalProvider = null, string? version = null,
    int? artistId = null, string? newArtistName = null, CancellationToken ct = default);
```

Both remain the only song write paths (REQ-SFT-46). `CreateSongAsync` (title-only uniqueness,
`SongService.cs:90`) loses its last production caller when `SongResolutionService.CommitAsync` goes;
it is left in place for its test fixtures and flagged as a follow-up, not changed here.

### 3.3 Suggestion services

```csharp
// Contracts/DTOs/Suggestions/SongSuggestionDto.cs: add Version ("" for remote) and
// LocalArtistName (stored spelling; null when the artist is not local).
// Contracts/DTOs/Suggestions/SongRemoteSearchResult.cs (new)
public sealed record SongRemoteSearchResult(
    IReadOnlyList<SongSuggestionDto> Promoted,  // Deezer hits that exist locally, as local rows (REQ-SFT-05)
    IReadOnlyList<SongSuggestionDto> Remote,    // <= MaxSearchResults, not in the library
    bool RemoteUnavailable);                    // REQ-SFT-06

// Services/ISongSuggestionService.cs: GetLocalAsync/GetRemoteAsync are replaced by two calls, so
// local options can be shown before Deezer answers (REQ-SFT-07)
Task<IReadOnlyList<SongSuggestionDto>> SearchTitleLocalAsync(string query, CancellationToken ct = default);
Task<SongRemoteSearchResult> SearchTitleRemoteAsync(
    string query, IReadOnlyCollection<int> shownLocalSongIds, CancellationToken ct = default);
```

`SearchTitleLocalAsync`:
1. Normalise with `NormalizeSearchQuery` (trim + collapse; not case/accent folding). Below 2 → empty.
2. `ExecuteReadAsync`: `SearchTitleCandidatesAsync(q, SimilarityConstants.PoolSize)` (50).
   Rank with `RankScore(q, max over {title, title + " " + artist})`, stable over DB order; take 5.

`SearchTitleRemoteAsync`:
1. Below 3 characters → empty, no call.
2. Deezer `SearchSongsAsync(q)` **outside** any unit-of-work lambda (no open scope during HTTP,
   existing test `SongSuggestionThresholdTests.cs:128`), bounded by a linked CTS with
   `CancelAfter(3000)`. Caller cancellation → rethrow (stale query). Any other exception, the
   timeout, or a provider-reported error (§ 3.4) → `RemoteUnavailable = true`, log Warning.
3. Dedup in one `ExecuteReadAsync`: `FindByExternalIdsAsync("Deezer", ids)`, then
   `FindByTitleAndArtistNamesAsync(pairs, "")` for the rest. A hit whose song id is not in
   `shownLocalSongIds` goes to `Promoted`; either way the remote row is dropped. Tier (c) similarity
   dedup (`SongSuggestionService.cs:130-175`) is deleted: it hid other artists' songs.
4. For remaining remote rows, resolve `LocalArtistId/LocalArtistName` by collated artist name in the
   same read; the C# `OrdinalIgnoreCase` re-filters (`SongSuggestionService.cs:162,204`) are removed.

### 3.4 Providers and the Field dependency

- `IMusicMetadataProvider` contract change: `SearchSongsAsync`/`SearchArtistsAsync` **throw** on
  transport failure (`HttpRequestException`, `JsonException`, timeout) and on a Deezer error body.
  Deezer answers some failures, such as over quota, with **HTTP 200 and an `error` object** instead of
  `data`; the provider reads it and throws `MusicProviderException`. Today every failure is swallowed
  and returns empty (`DeezerProvider.cs:60-64`), which makes REQ-SFT-06 impossible. The suggestion
  services are the catch boundary; no exception escapes a service.
- `DeezerProvider.SearchSongsAsync`: `search/track?q={Uri.EscapeDataString(term)}` with no `track:`/
  `artist:` qualifiers (REQ-SFT-35; `DeezerProvider.cs:47-50` today). `artistHint` is ignored and
  removed from the title-search path. Maps `id`, `title`, `artist.name` (Q5 may add `title_short`/
  `title_version`).
- **Field surface used, no new option property:** the "In your library"/"Deezer" labels are inside
  `AutocompleteSuggestion.SupportingText`; "Online search unavailable" is `AutocompleteDXField.SupportingText`
  (REQ-ACF-12c), bound to `TitleSupportingText` / `NameSupportingText`. There is no status row.
- **Append capability (REQ-SFT-07), requested from the dependency item.** DX `AsyncItemsSourceProvider`
  takes one result list per `ItemsRequested`, so the Field cannot show local options and add Deezer
  options later on its own. Request: `IMenuOptionSource` gains an optional second phase, e.g.
  `IAsyncEnumerable<IReadOnlyList<AutocompleteSuggestion>> SearchPhasesAsync(query, ct)`, and the Field
  replaces its menu items with each yielded list while the query is current and the menu is open.
  **If rejected (Q16):** `TitleOptionSource` awaits local and remote together and returns one list; the
  3,000 ms bound (REQ-SFT-08) caps how long local options wait.

### 3.5 Save decisions in the Service (`ISongService.SaveFromFormAsync`, REQ-SFT-51)

```csharp
// Domain/Resolution/SongFormSaveRequest.cs (new)
public sealed record SongFormSaveRequest(
    int? SongId,                 // null = add mode
    string Title, string Version, string? FeaturedArtists, string? Lyrics,
    int? ArtistId,               // picked/stored local artist
    string? ArtistText,          // typed or Deezer-filled name when ArtistId is null
    string? ExternalProvider, string? ExternalId,
    IReadOnlyList<string> PendingUrls,
    ArtistDecision ArtistDecision,   // None | UseExisting(int id) | CreateTyped
    SongDecision SongDecision);      // None | SaveAsNewVersion | SaveAsNewSong

// Domain/Resolution/SongFormSaveOutcome.cs (new)
public enum SongFormSaveKind { Saved, Failed, ArtistChoiceNeeded, SongChoiceNeeded }
public sealed record SongFormSaveOutcome(
    SongFormSaveKind Kind,
    string Message,                                  // Saved / Failed text
    SaveField FailedField,                           // Title | Version | Artist | None
    int? SavedSongId,
    IReadOnlyList<ArtistMatch> ArtistCandidates,     // ArtistChoiceNeeded: <= 3, best first
    bool IsExactSongMatch,                           // SongChoiceNeeded
    IReadOnlyList<SongMatch> SongCandidates);        // SongChoiceNeeded

Task<SongFormSaveOutcome> SaveFromFormAsync(SongFormSaveRequest request, CancellationToken ct = default);
```

Four outcomes do not fit the `(bool, string, T?)` tuple, so this is a result record; `Failed` carries
the message exactly as the tuple convention would. `SaveFromFormAsync` itself opens no write unit of
work: it reads through `ExecuteReadAsync` (resolution) and then calls `CreateSongWithUrlsAsync` or
`UpdateSongAsync`, which own the only writes (REQ-SFT-46, CATALOG-PROP). The unique DB indexes are the
backstop for a race between the read and the write.

Rules, in order (each a Level A test in `SongServiceSaveFromFormTests`):
1. Validate title, version, artist required → `Failed(field)`.
2. Artist (REQ-SFT-18/19): `ArtistId` set → use it. Else by `ArtistDecision`: `UseExisting(id)` → id;
   `CreateTyped` → new; `None` → `IArtistResolutionService.ResolveAsync(new ArtistCandidate(ArtistText, null, null))`
   — never the song's identity (REQ-SFT-41). ExactLocalMatch → that id, silently (REQ-SFT-18.1, REQ-SFT-52); FuzzyCandidates →
   return `ArtistChoiceNeeded`; NoMatch → new.
3. Song guard (REQ-SFT-22/23/25), add mode only, skipped when `SongDecision != None`:
   `ISongResolutionService.ResolveAsync` with the external identity and, for an existing artist, its id.
   ExactExternalMatch runs whatever the artist's origin (REQ-SFT-25); the artist-scoped tiers run only
   with an existing artist. Exact → `SongChoiceNeeded(exact: true)`; fuzzy → `SongChoiceNeeded(exact: false)`.
4. `SaveAsNewVersion` → `ValidateVersionInput(version, isRequired: true)` (AC-1.2) or `Failed(Version)`.
5. Write: add → `CreateSongWithUrlsAsync(artistId | null, newName | null, …)`; edit →
   `UpdateSongAsync(…, artistId | null, newName | null)`. Tuple `(false, msg)` → `Failed`; else `Saved`.

## 4. ViewModel contract (`SongFormViewModel`)

| Member | Kind | Purpose / REQ |
|--------|------|---------------|
| `TitleOptionSource` | `IMenuOptionSource` | Phase 1: `SearchTitleLocalAsync` → options (Headline title, SupportingText "artist · version · In your library"). Phase 2: `SearchTitleRemoteAsync` → promoted rows appended to local, then remote options ("artist · Deezer"). REQ-SFT-02…09 |
| `TitleSupportingText` | observable → Field `SupportingText` | "Online search unavailable" while the current query's remote call failed; `""` otherwise (REQ-SFT-06) |
| `SongTitle` / `IsTitleLocked` | observable, TwoWay to Field | Title text and Locked state |
| `IsTitleSearchEnabled` | `=> !IsEditMode` | REQ-SFT-27 |
| `IsTitleReadOnly` | `=> IsEditMode` | Field `IsReadOnly` (REQ-SFT-27: every song). One `AutocompleteDXField` serves both modes |
| `SelectTitleCommand(AutocompleteSuggestion)` | command | Local → `OpenExistingAsync` (REQ-SFT-10/11); remote → `ApplyRemotePick` (REQ-SFT-12/13) |
| `ClearTitleCommand` | command | Clears pending external identity (REQ-SFT-14) |
| `ArtistOptionSource`, `ArtistSearchText`, `SelectedArtistId`, `SelectedArtistName`, `IsArtistLocked`, `SelectArtistCommand`, `ClearArtistCommand` | existing/changed | Local artists only. `ClearArtist` also clears the pending external identity. `ArtistBlurredWithoutSelection` keeps text and sets no error (REQ-SFT-17) |
| `SelectedExternalId` / `SelectedProvider` | existing | Pending (add) or stored (edit) external identity |
| `SaveCommand` | changed | Builds `SongFormSaveRequest`, calls `SaveFromFormAsync`, maps the outcome: `Saved` → snackbar + back; `Failed` → field error; `ArtistChoiceNeeded` → artist-match sheet; `SongChoiceNeeded` → resolution sheet. The sheet's choice re-invokes with `ArtistDecision`/`SongDecision` set |
| `IsDiscardSheetVisible`, `ConfirmOpenExistingCommand`, `KeepEditingCommand` | new | REQ-SFT-11 |
| `IsArtistMatchSheetVisible`, `ArtistMatchCandidates`, `UseExistingArtistCommand(ArtistMatch)`, `CreateTypedArtistCommand`, `DismissArtistMatchSheetCommand` | new | REQ-SFT-19 (display + re-invoke only) |
| `ResolutionCandidates`, `IsExactDuplicate`, `OpenExistingSongCommand(SongMatch)`, `ConfirmSaveAsNewVersionCommand`, `ConfirmSaveAsNewSongCommand`, `DismissResolutionSheetCommand` | changed | REQ-SFT-22/23 (display + re-invoke only) |
| `INavigationService` | new ctor dependency | Replaces `Shell.Current` in the new navigation paths (testing.md: never call `Shell.Current` in VM tests) |
| `ISongResolutionService` | **removed** ctor dependency | The VM no longer resolves |

Deleted: `NavigateToSongPickerCommand`, `OnSongPicked`, `ResolveAndLockArtistAsync`, the messenger
registration (`:156-158`), `CreateArtistInlineCommand`, `ExecuteNewSongSaveAsync`/`ExecuteEditSaveAsync`/
`CommitNewSongAsync` decision logic, `ConfirmUpdateExistingCommand`, `SelectResolutionCandidateCommand`
(replaced by `OpenExistingSongCommand`), `IsMergeSheetVisible`, `MergeFieldRows`, `ConfirmMergeCommand`,
`DismissMergeSheetCommand`, `_selectedMergeTargetId`, `_pendingCandidate`/`_pendingResolution`,
`MergeFieldRow`. The stale docstring at `:433` is rewritten.

### 4.1 Artist form (`ArtistFormViewModel`, REQ-SFT-47…50)

`IArtistSuggestionService` is reshaped like the song service: `SearchNameLocalAsync(query, ct)` and
`SearchNameRemoteAsync(query, ct) → ArtistRemoteSearchResult(Remote, RemoteUnavailable)`.
Local = the existing collated prefix search (`IArtistRepository.SearchByNameAsync`, ≤ 5; no ranking,
so § 6.4 does not apply). Remote = Deezer `search/artist?q=` (≤ 5), with the same 3,000 ms bound and
failure handling as § 3.3. Dedup: a Deezer artist whose name is a collated match for a local artist is
dropped (the local row stays). The similarity dedup tier (c) in `ArtistSuggestionService` and
`FilterSimilar` lose their callers and are deleted: C# similarity must not deduplicate (§ 6.4 scope).

| Member | Purpose |
|--------|---------|
| `NameOptionSource` (`IMenuOptionSource`) | Local options (SupportingText "In your library"), then Deezer options (SupportingText "Deezer") |
| `NameSupportingText` | Field `SupportingText`: "Online search unavailable" on remote failure |
| `ArtistName`, `IsNameLocked`, `IsNameSearchEnabled => !IsEditMode` | Field bindings |
| `SelectNameCommand` | Local → `INavigationService.GoToAsync($"../{Routes.ArtistForm}?artistId={id}&artistName={escaped}")` (query shape as `ArtistFormViewModel.cs:203`); Deezer → fill `ArtistName`, lock |
| `ClearNameCommand` | X: empty and unlock |
| Deleted | `NavigateToArtistPickerCommand`, the `SelectedExternalId`/`SelectedProvider` stash (`:30-31`, `:188-198`) unless Q10 decides to store the id |

Save is unchanged: `CreateArtistAsync(name)` with no external identity (REQ-SFT-49 recommendation).

### Decision: do not store the Deezer artist id (recommended; Q10 open)
**Recommended approach:** a Deezer artist pick fills the name only.
**Alternatives considered:** pass `("Deezer", id)` to `CreateArtistAsync` (the parameters exist,
`IArtistService.cs:15-16`).
**Reversibility:** Easily reversible (one argument; the columns already exist).
**Rationale:** nothing reads `Artist.ExternalId` after this item (REQ-SFT-40); the song flow dedups
artists by collated name; a write-only value invites the "build on it" drift Helder ruled out.

## 5. Interaction flows

**F1: add mode, pick a new Deezer song** (REQ-SFT-07, 12, 13)
1. User types "don cry"; after 300 ms the Field calls `TitleOptionSource`.
2. Phase 1: local options render. Phase 2: when Deezer answers for the same query, promoted and remote
   options are appended (fallback per § 3.4).
3. User taps "Don't Cry (Original)" / "Guns N' Roses · Deezer". The Field locks Title.
4. `SelectTitleCommand` → `ApplyRemotePick`: `SongTitle` = provider title; `SelectedProvider/ExternalId`
   = ("Deezer", id); if `LocalArtistId` is set → `LockArtist(LocalArtistId, LocalArtistName)`; else
   `ArtistSearchText` = provider artist, `SelectedArtistId = null`, `IsArtistLocked = true`,
   dropdowns closed. No service write.

**F2: add mode, pick an existing song** (REQ-SFT-10/11)
1. User taps a local option (song id N).
2. If no other field holds input → `INavigationService.GoToAsync($"../{Routes.SongForm}?songId={N}")`
   (Shell pops the add form and pushes the edit form; `LoadSongForEditAsync` hydrates everything,
   including the artist from the entity, `SongFormViewModel.cs:469-477`).
3. Else show the discard sheet. Open → step 2. Keep editing → `SongTitle` = the typed query,
   `IsTitleLocked = false` (a programmatic unlock; REQ-ACF-05 constrains user actions only).

**F3: Save, existing artist** (REQ-SFT-22/23/51)
1. VM builds the request (`ArtistId` set, decisions `None`) and calls `SaveFromFormAsync`.
2. Service: rule 3 → `SongChoiceNeeded` or write. VM: `Saved` → back; `SongChoiceNeeded` → resolution
   sheet (exact: Open existing / Save as new version; fuzzy: rows / Save as new song).
3. Open existing → F2 step 2 navigation. Save as new version/song → VM re-invokes with that
   `SongDecision`; the Service skips rule 3 and writes.

**F4: Save, artist with no local id** (REQ-SFT-18, 25)
1. Artist "Banda Local" typed (or filled by a Deezer pick with no local match). VM calls
   `SaveFromFormAsync` with `ArtistText`.
2. Service rule 2: ExactLocalMatch → that id; NoMatch → new artist. Rule 3 still runs its
   external-identity tier (REQ-SFT-25).
3. Write: `CreateSongWithUrlsAsync(null, "Banda Local", …)`. Inside one unit of work:
   `CreateArtistAsync` (validates, collated duplicate check) → `FlushAsync` → song staged with the
   new `ArtistId` → URLs staged → one commit. A failure tuple skips the save (REQ-UOW-24); an exception
   rolls back.

**F5: Save, near-duplicate artist** (REQ-SFT-19)
1. Rule 2 returns FuzzyCandidates ("Metalica" → "Metallica", via `ArtistSearch`) → `ArtistChoiceNeeded`.
2. VM shows the artist-match sheet. Use → re-invoke with `UseExisting(id)`. Create → re-invoke with
   `CreateTyped`. Dismiss → nothing. Either re-invocation may still return `SongChoiceNeeded` (F3).

**F6: edit mode**
1. Page opens with `songId`; `LoadSongForEditAsync` hydrates, Artist locked.
2. Title: the same `AutocompleteDXField` with `IsSearchEnabled = false` and `IsReadOnly = true` for
   every song (no X, no typing). The stored title is sent back unchanged on Save.
3. Save: `SaveFromFormAsync` with `SongId` set: rules 1, 2, 5 (no song guard; `UpdateSongAsync`
   enforces (artist, title, version) uniqueness, REQ-SFT-29).

**F7: offline**
1. Deezer throws, times out or returns an error body → `RemoteUnavailable = true`; local options stay;
   `TitleSupportingText` = "Online search unavailable".
2. Save paths use no network and are unaffected.

**F8: Artist form, add mode**
1. User types "metalica"; the Field shows local "Metallica" (if stored) and then Deezer artists.
2. Local pick → the add form is replaced by that artist's edit form. Deezer pick → Name filled and
   locked. No pick → the typed text stays.
3. Save → `CreateArtistAsync(name)`; a collated duplicate returns the existing message on Name.

## 6. Local typo-tolerant search

### 6.1 Availability (desktop verified 2026-09-24; Android gated by spike G5)

The app bundles SQLitePCLRaw 3.0.3 → SourceGear.sqlite3 (`Directory.Packages.props:16-22`). A
scratchpad console app with the same packages printed `sqlite_version: 3.50.4` and
`sqlite_compileoption_used('ENABLE_FTS5') = 1`, and created
`fts5(title, artist, tokenize='trigram remove_diacritics 1')` successfully ("nao sei" matched "Não Sei").
Trigram needs SQLite ≥ 3.34, `remove_diacritics` for trigram ≥ 3.45: both met on desktop.
**Not yet proven on Android:** the probe ran against the Windows native library. Spike gate G5
(`tasks.md`, before 3.8) runs `SELECT sqlite_compileoption_used('ENABLE_FTS5')` and creates a trigram
table on the device. If it fails, work stops and Helder chooses; the named fallback is the
token-prefix pool + rerank (findings § 2 "Hybrid"): collated `LIKE '<first 2–3 chars of each word>%'`
OR-ed, bounded to 50, ordered by `RankScore`, which needs no migration but full-scans through the
managed `like()` UDF.

### 6.2 Index and sync

```sql
CREATE VIRTUAL TABLE SongSearch USING fts5(title, artist, tokenize='trigram remove_diacritics 1');
-- rowid = Songs.Id. Backfill:
INSERT INTO SongSearch(rowid, title, artist)
  SELECT s.Id, s.Title, a.Name FROM Songs s JOIN Artists a ON a.Id = s.ArtistId;
CREATE TRIGGER SongSearch_ai AFTER INSERT ON Songs BEGIN
  INSERT INTO SongSearch(rowid, title, artist)
  VALUES (new.Id, new.Title, (SELECT Name FROM Artists WHERE Id = new.ArtistId)); END;
CREATE TRIGGER SongSearch_au AFTER UPDATE OF Title, ArtistId ON Songs BEGIN
  DELETE FROM SongSearch WHERE rowid = old.Id;
  INSERT INTO SongSearch(rowid, title, artist)
  VALUES (new.Id, new.Title, (SELECT Name FROM Artists WHERE Id = new.ArtistId)); END;
CREATE TRIGGER SongSearch_ad AFTER DELETE ON Songs BEGIN
  DELETE FROM SongSearch WHERE rowid = old.Id; END;
CREATE TRIGGER SongSearch_artist_au AFTER UPDATE OF Name ON Artists BEGIN
  UPDATE SongSearch SET artist = new.Name
  WHERE rowid IN (SELECT Id FROM Songs WHERE ArtistId = new.Id); END;

-- Artist near-duplicate retrieval (REQ-SFT-19). rowid = Artists.Id.
CREATE VIRTUAL TABLE ArtistSearch USING fts5(name, tokenize='trigram remove_diacritics 1');
INSERT INTO ArtistSearch(rowid, name) SELECT Id, Name FROM Artists;
CREATE TRIGGER ArtistSearch_ai AFTER INSERT ON Artists BEGIN
  INSERT INTO ArtistSearch(rowid, name) VALUES (new.Id, new.Name); END;
CREATE TRIGGER ArtistSearch_au AFTER UPDATE OF Name ON Artists BEGIN
  DELETE FROM ArtistSearch WHERE rowid = old.Id;
  INSERT INTO ArtistSearch(rowid, name) VALUES (new.Id, new.Name); END;
CREATE TRIGGER ArtistSearch_ad AFTER DELETE ON Artists BEGIN
  DELETE FROM ArtistSearch WHERE rowid = old.Id; END;
```

Seven triggers in total: four maintain `SongSearch`, three maintain `ArtistSearch`. Adding
`ArtistSearch` is proportionate: one extra virtual table and three one-line triggers in the same
migration, with the same query builder as songs. The alternative, a C# scan of all artist names,
would break AC-5.2 and the HARD RULE.

Query (in `SongRepository`, and the same builder in `ArtistRepository.SearchNameCandidatesAsync`, via
`FromSql`/raw ADO on the context's connection): build an OR of the
distinct 3-character windows of every query word of ≥ 3 characters, each double-quoted with `"`
escaped (query construction, not normalisation: the tokenizer folds case and diacritics in SQLite).
`SELECT … FROM SongSearch JOIN Songs … WHERE SongSearch MATCH @m ORDER BY bm25(SongSearch, 10.0, 1.0), length(Songs.Title) LIMIT @take`.
No word of ≥ 3 characters → the existing collated title prefix query (REQ-SFT-33).

### Decision: sync by triggers, not by repository code
**Chosen approach:** SQLite triggers created by the migration.
**Alternatives considered:** maintaining `SongSearch` in `SongRepository.AddAsync/UpdateAsync/DeleteAsync`
and `ArtistRepository.UpdateAsync`. Rejected: `DeleteAsync` is `ExecuteDeleteAsync`-based
(REQ-UOW-33) and bypasses change tracking; artist cascade deletes (`ArtistConfiguration.cs:47-50`)
and future write paths would each need remembering. Triggers fire for every write, including FK
cascades.
**Reversibility:** Reversible with effort (migration `Down` drops table and triggers).
**Rationale:** REQ-SFT-34 must hold for writes this item does not own (CATALOG-PROP, backup restore).
**Three consequences to carry:**
1. EF Core's SQLite table rebuild (used for some column drops) recreates `Songs`/`Artists` and **drops
   their triggers**. M3 therefore runs after M1, and a migration test asserts all seven triggers exist
   after a full migrate. Constraint note for `constraints-reference.md § EF Core / SQLite` (orchestrator
   files it): "Any migration that rebuilds Songs or Artists must recreate the FTS triggers in the same
   migration."
2. Tests build schemas with `EnsureCreated` (e.g. `SongRepositoryTests`), which does not run raw-SQL
   migrations. FTS tests must call `Database.MigrateAsync()` on a temp file.
3. **RETURNING clause (settled, Context7 `/dotnet/entityframework.docs`, EF Core 10 line, 2026-09-25).**
   The SQLite provider saves with `RETURNING` by default, which "is not supported on SQLite if the
   target table has database AFTER triggers" (EF Core 7 breaking change #29916; mitigation
   `UseSqlReturningClause`, EF Core 8+, current in 10). `HasTrigger` is the SQL Server mechanism and does
   not apply. Therefore `SongConfiguration` and `ArtistConfiguration` add
   `builder.ToTable(tb => tb.UseSqlReturningClause(false))` in the M3 task, and the migration test
   inserts and updates a Song and an Artist through EF after migrating.

### 6.3 Ranking: option (a), chosen by Helder 2026-09-24

The FTS5 query retrieves ≤ 50 candidates (DB index, DB folding). C# only **orders** them, through
`SimilarityScorer` (same class, same `Normalize` at `Infra/Similarity/SimilarityScorer.cs:30-40`).

**Measured problem with the existing scoring function.** `SimilarityScorer.Score` is FuzzySharp
`TokenSetRatio` (`:20-21`). `TokenSetRatio` scores 100 whenever one side's words are a subset of the
other's, so the one-word title "Cry" beats "Don't Cry" for "dont cry" and fails REQ-SFT-30.
`RankScore` therefore uses FuzzySharp **`TokenSortRatio`** on the same normalised strings, scored
against both the title and "title artist", keeping the higher. `Score` (used by resolution) is
unchanged. Q8 asks Helder to confirm.

Worked examples. Probe corpus (3,014 rows): 14 real songs (Don't Cry, Sweet Child O' Mine – Guns N'
Roses; Boys Don't Cry – The Cure; Cry – Cyndi Lauper; Crying – Roy Orbison; Cry Me a River; Cry Baby;
Crystal Ball; Dont Look Back in Anger – Oasis; Don't Speak; Don't Stop Me Now; Don't Let Me Down;
London Calling; Não Sei) plus 3,000 synthetic titles built from a list of common English song words. FTS5 trigram,
bm25 weights title 10 / artist 1. Scores are 0–100.

| Query | FTS5 candidates (bm25 order, top 5) | (a) existing `TokenSetRatio` order | (a) `TokenSortRatio` order (chosen) |
|---|---|---|---|
| `dont cry` | 1 Dont Look Back in Anger · 2= Don't Cry · 2= Boys Don't Cry · 4 Cry · 5 Crying | **Cry 100**, Don't Cry 94, Boys Don't Cry 73 | **Don't Cry 94**, Boys Don't Cry 73, Crying 57 |
| `don cry` | 1= Don't Cry · 1= Boys Don't Cry · 3 Cry · 4 Crying · 5 Crystal Ball | **Cry 100**, Don't Cry 88 | **Don't Cry 88**, Boys Don't Cry 67, Crying 62 |
| `guns cry` | 1 Don't Cry · 2 Cry · 3 Crying · 4 Crystal Ball · 5 Cry Baby | Cry 100 = Don't Cry 100 (tie) | **Don't Cry 59**, Crying 57, Cry 55 |
| `guns n roses dont cry` | 1 Don't Cry · 2 Sweet Child O' Mine … | Cry 100, Don't Cry 95 | **Don't Cry 95**, Sweet Child O' Mine 56 |

Why "dont cry" needs OR, not AND: the trigram "ont" is not in "don't" (its trigrams are "don",
"on'", "n't"), so an AND match returns nothing; OR over trigrams finds it through "don" and "cry".
Honest limits: `guns cry` wins by 2 points only; one-word queries tie many titles (bm25 order then
decides); the probe corpus is synthetic, so the orders are indicative.

### 6.4 Exception entry (proposed text; the orchestrator files it in `.claude/exception-registry.md`)

| Date | Constraint violated | Reason for exception | Code location | Expires |
|------|---------------------|----------------------|---------------|---------|
| 2026-09-24 | `constraints-registry.md § EF Core / SQLite` — "No C#-side string normalization for search/deduplication — HARD RULE"; and `song-import-resolution/requirements.md` INV-5 ("No matching path performs C#-side case/accent normalization") | Song title search (item `song-form-title-first-one-search-over-local-and-music-apis`, REQ-SFT-30…32). The database does all **retrieval**: an FTS5 trigram index with `remove_diacritics` returns a candidate list bounded by `SimilarityConstants.PoolSize` (50). C# (`SimilarityScorer.Normalize` + FuzzySharp `TokenSortRatio`, via `ISimilarityScorer.RankScore`) only **orders** that bounded list for display. It never decides which rows exist, never filters out a row, never deduplicates and never checks uniqueness. Both reasons behind the rule are absent: correctness (the DB, not C#, decides matches, so "café"/"cafe" is still matched by SQLite) and performance (retrieval uses the index; C# work is at most 50 scores per query). Helder chose this option on 2026-09-24. | `Services/SongSuggestionService.cs` (ranking step of `SearchTitleLocalAsync`); `Infra/Similarity/SimilarityScorer.cs` (`RankScore`). Not `ArtistSuggestionService`: artist search keeps the collated prefix query and does not rank | When the ranking moves into SQL, or if any code path uses `RankScore` to filter, deduplicate or test uniqueness, or if the candidate list stops being DB-bounded. |

### Decision: FTS5 trigram over the token-prefix hybrid
**Chosen approach:** FTS5 trigram retrieval (Helder, README third round item 5).
**Alternatives considered:** token-prefix LIKE pool + rerank (findings § 2): no migration, but every
LIKE is a full scan through the managed `like()` UDF (`CollationInterceptor.cs:65-69`), and
"dont cry" needs the `don%` token to survive. In-memory scoring of all titles: breaks AC-5.2.
**Reversibility:** Reversible with effort (drop migration; repository falls back to prefix).
**Rationale:** real index, typo-tolerant retrieval, matches title and artist in one query.

## 7. Migrations (sequential; `Migrations/` is sequential-only)

| # | Name | Content | Data effect |
|---|------|---------|-------------|
| M1 | `DropHasManualEdits` | `DropColumn("HasManualEdits", "Songs")`, `DropColumn("HasManualEdits", "Artists")` | Values discarded (they meant "saved from edit screen at least once", findings § 3.3). `Down` re-adds with default `false`. |
| M2 | `SongExternalIdentityUnique` | 1. `UPDATE Songs SET ExternalId = NULL, ExternalProvider = NULL WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL AND Id NOT IN (SELECT MIN(Id) FROM Songs WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL GROUP BY ExternalProvider, ExternalId);` 2. drop `IX_Songs_ExternalId`. 3. create unique `IX_Songs_ExternalProvider_ExternalId`. | Duplicates keep the identity on the lowest `Id`; no row deleted (REQ-SFT-39). Expected count on real data: 0 (the providers never returned data, findings § 0.1); the step makes it safe regardless. `Down` restores the non-unique single-column index; cleared identities are not restored. |
| M3 | `AddSearchIndexes` | § 6.2 DDL for `SongSearch` and `ArtistSearch` + backfill + 7 triggers (raw SQL); `UseSqlReturningClause(false)` on Songs and Artists (model annotation only, no schema change). | None. `Down` drops triggers and tables. |

EF config: `SongConfiguration.cs:48,61-62` and `ArtistConfiguration.cs:33-34` change accordingly.
Order M1 → M2 → M3 is mandatory (§ 6.2 consequence 1). Other in-flight migrations:
- CATALOG-PROP's D4 backfill (inserts `Catalog` rows) is independent. Whichever item merges second
  **regenerates its migration against the merged model snapshot** (not just a later timestamp).
- BUG-080's fix for `20260211055225_venuesSeedForTest` touches only `Venues` and rebuilds neither Songs
  nor Artists, so it cannot drop the FTS triggers. Same snapshot rule applies.

## 8. DI changes (`MyVocaList/Extensions/ServiceCollectionExtensions.cs`, `MauiProgram.cs`)

- **Prerequisite BUG-082** (`bugs/2026-09-25-BUG-082-music-api-providers-re-registered-in-di-bypassing-typed-httpclient/`):
  providers resolved through their typed `HttpClient` registrations (replace `:44-45`). Not designed here.
- Add `services.AddScoped<ISongSuggestionService, SongSuggestionService>()` and
  `services.AddScoped<IArtistSuggestionService, ArtistSuggestionService>()`.
- Remove `IMusicMetadataService`/`MusicMetadataService` (their only consumers are the two picker VMs).
- If Q1 = drop: remove `MusicBrainzProvider` registration and class.
- `MauiProgram.cs:130` and the Artist-picker equivalents: remove picker pages/VMs.
- `AppShell.xaml.cs:32` (+ Artist picker route), `Routes.cs:19-20`: remove.

## 9. Deletions (census, Python `os.walk`, 2026-09-24; re-run before each deletion task)

| Symbol | Files referencing it |
|--------|----------------------|
| `SongPicker*` | `AppShell.xaml.cs`, `MauiProgram.cs`, `Routes.cs`, `SongFormPage.xaml`, `SongPickerPage.xaml(.cs)`, `SongFormViewModel.cs`, `SongPickerViewModel.cs`, `SongPickerViewModelTests.cs` |
| `ArtistPicker*` | `AppShell.xaml.cs`, `MauiProgram.cs`, `Routes.cs`, `ArtistFormPage.xaml`, `ArtistPickerPage.xaml(.cs)`, `ArtistFormViewModel.cs`, `ArtistPickerViewModel.cs`, `ArtistPickerViewModelTests.cs` |
| `SongPickedMessage` / `ArtistPickedMessage` | `Contracts/Messages/*.cs`, both form VMs, both picker VMs, `SongFormViewModelTests.cs`, both picker tests |
| `IMusicMetadataService` | `ServiceCollectionExtensions.cs`, both picker VMs + tests, `Services/(I)MusicMetadataService.cs` |
| `HasManualEdits` | entities, both configs, `SongListItemDto`, `ArtistListItemDto`, `ArtistListItem`, `SongRepository`, `ArtistRepository`, `CatalogRepository`, `SongService`, `ArtistService`, `SongResolutionService`, `ResolutionResults`, `SongFormViewModel`, `SongFormPage.xaml`, 2 test files; migration Designer files keep historical mentions |
| `FieldDiff` / `MergeFieldRow` | `ResolutionResults.cs`, `SongResolutionService.cs`, `SongFormViewModel.cs`, `SongFormPage.xaml`, 2 test files |
| `CreateArtistInline` | `SongFormPage.xaml.cs`, `SongFormViewModel.cs`, `SongFormViewModelTests.cs` |

Docs that mention the pickers (`search-picker/*`, `song-import-resolution/*`, …) are shipped history
and are **not** edited; § Supersedes in `requirements.md` records the effect. `NavigationConfig.cs`
has no picker reference (checked).

## 10. Test strategy

| Tier | Scope | Tests |
|------|-------|-------|
| **A** (Red first) | `SongService.SaveFromFormAsync` rules 1–5 (collated reuse, near-duplicate → `ArtistChoiceNeeded`, new artist still runs the identity guard, decisions skip the right steps, REQ-SFT-41 regression); `SongService` new-artist write path, version-aware update (REQ-SFT-29 regression), external identity uniqueness; `SongSuggestionService` local/remote (order, dedup tiers, Deezer error body, thresholds, cancellation); `SongResolutionService` accent-only id (REQ-SFT-42 regression); `ArtistResolutionService` with FTS pool; VM outcome→sheet mapping, picks, edit-mode title; ranking examples of § 6.3 against `RankScore` | Moq for services/VM; real SQLite for the UoW atomicity tests |
| **B** | Repository search/lookups on real SQLite **after `MigrateAsync`** (both FTS tables + 7 triggers: insert, title update, artist rename, bulk delete; "Metalica" retrieves "Metallica"); migrations M1–M3 (columns gone, unique index rejects a duplicate, M2 data fix via migrate-to-M1 → seed → migrate); `DeezerProvider` URL shape and failure propagation (HTTP 500, bad JSON, HTTP 200 + `error` object) with a fake `HttpMessageHandler` | real SQLite temp file (testing.md anti-patterns) |
| **C** | Deletions, DI registration, XAML | build + census recorded in task-log; `AppServicesRegistrationTests` resolves `ISongSuggestionService` |
| **E2E** | Helder on device | `tasks.md` final gate |

### 10.1 Existing tests: deleted or rewritten (each tied to a superseded AC; none silently removed)

| Test (file:line) | Action | Superseded AC → replacing REQ |
|---|---|---|
| `SongPickerViewModelTests.cs` (all 12) | delete | AC-SONG-01…06, AC-LOAD-* (song picker) → REQ-SFT-44 |
| `ArtistPickerViewModelTests.cs` (all) | delete | AC-ART-01…06 → REQ-SFT-44 |
| `SongFormViewModelTests.cs:486,509` `ResolveAndLockArtistAsync_*_ClosesDropdown` | delete; replaced by `RemotePick_*_ClosesDropdowns` | AC-11.2 / REQ-ACF-24 picker clause → REQ-SFT-12/13 |
| `SongFormViewModelTests.cs:475` | edit (drop `SongPickedMessage` use) | — |
| `SongFormViewModelTests.cs:550,570` `CreateArtistInline_*` | delete | REQ-ACREATE-04/05 → REQ-SFT-45/18/20 |
| `SongFormViewModelTests.cs:287` `…RetainsTextAndSetsError` | rewrite: retains text, **no** error | REQ-ACREATE-03 error clause → REQ-SFT-17 |
| `SongFormViewModelTests.cs:334` `…SetsErrorText` | delete | same |
| `SongFormViewModelTests.cs:707` `ConfirmUpdateExisting_TargetHasManualEdits_PopulatesMergeRows` | delete | AC-4.2 → REQ-SFT-38 |
| `SongFormViewModelTests.cs:787` `LoadSongForEdit_ApiImportedWithoutManualEdits_LocksArtistField` | rewrite without `HasManualEdits`, rename | BUG-008 setup → REQ-SFT-28 |
| `SongFormViewModelTests.cs:174` `SaveAsync_NoMatch_CallsCreateSongWithUrls` | rewrite: VM calls `SaveFromFormAsync` and navigates on `Saved` | rule moved to the Service (REQ-SFT-51) |
| `SongFormViewModelTests.cs:208,238` (resolution sheet shown/dismissed) | rewrite: outcome `SongChoiceNeeded` → sheet visible; dismiss | REQ-SFT-22/51 |
| `SongFormViewModelTests.cs:317` `ArtistBlurredWithoutSelection_WithPriorSelection_RestoresName`, `:532` `…RestoresPriorSelection_ClosesDropdown` | **keep unchanged**: with pick-lock the user cannot type over a locked artist, so the restore branch is unreachable by the user, but it stays as the defensive path the dependency item keeps (its Supersedes row on REQ-ACREATE-15) | REQ-ACF-17/24 carry-over |
| `SongResolutionServiceTests.cs:100,271` (FieldDiffs) | delete | AC-4.2 → REQ-SFT-38 |
| `SongResolutionServiceTests.cs:312,327` (`CommitAsync_CreateNewVersion_*`) | delete (AC-1.2 still covered by `SongFormViewModelTests.cs:265` and `ValidateVersionInput` tests) | `CommitAsync` removed |
| `SongResolutionServiceTests.cs:343,389` (`UpdateExisting`) | delete | AC-4.1/4.2, AC-1.1 "Update existing" → REQ-SFT-24 |
| `SongResolutionServiceTests.cs:435` (`AttachExternalId`) | delete | AC-2.2 → REQ-SFT-05 |
| `SongResolutionServiceTests.cs:481` (`CreateNew_ResolvesArtistThenCreatesSong`) | delete; replaced by `SongService` new-artist tests | AC-2.5 → REQ-SFT-18 |
| `ArtistResolutionServiceTests.cs:200,225,249` (`CommitAsync_*`) | delete | AC-3.2/3.4 → REQ-SFT-40 |
| `NestedUnitOfWorkTests.cs:22,66,105` | **port** to `CreateSongWithUrlsAsync(null, newArtistName, …)` with the same assertions (one row each; all-or-nothing on a throw; all-or-nothing on a failure tuple) | REQ-UOW-22/24 re-homed (Q11) |
| `NestedUnitOfWorkTests.cs:146` (one `AppDbContext` across **three** nested levels) | **port, weakened honestly**: the new chain has **two** levels (`SongService.CreateSongWithUrlsAsync` → `ArtistService.CreateArtistAsync`), so the ported test proves one context across two levels. No production path nests three levels after this item | REQ-UOW-22 re-homed |
| `NestedUnitOfWorkTests.cs:195` | delete | REQ-UOW-09 (**removed**, not re-homed) and AC-3.4 artist external identity → REQ-SFT-40 |
| `SongSuggestionServiceTests.cs:35,57,94,119,145` | rewrite against `SearchTitleLocalAsync`/`SearchTitleRemoteAsync` (94: dedup now artist- and version-scoped; 145: `RemoteUnavailable = true`, local still returned) | REQ-FORMUX-03/05 → REQ-SFT-05/06 |
| `SongSuggestionServiceTests.cs:76` (artist hint) | delete | REQ-SFT-35 (no hint) |
| `SongSuggestionServiceTests.cs:170` (MusicBrainz → Deezer fallback) | delete if Q1 = drop | AC-4.2 provider order → REQ-SFT-36 |
| `SongSuggestionThresholdTests.cs` (6) | retarget to `SearchTitleLocalAsync`/`SearchTitleRemoteAsync`, same assertions | REQ-UOW-51/52 kept |
| `SongServiceTests.cs` `UpdateSongAsync_*` (13) | mechanical: drop the `hasManualEdits` argument; `:245,:480` mock `ExistsByTitleVersionForArtistAsync` | REQ-SFT-29 |
| Other `UpdateSongAsync` callers (census 2026-09-25): `SongServiceUpdateIntegrationTests.cs` (2), `Bug068RegressionTests.cs` (1), `SongFormViewModelTests.cs` (6), `SongResolutionServiceTests.cs` (6, mostly deleted in 3.3) | mechanical signature update, `tasks.md` 3.2b/3.2c | — |
| `ArtistResolutionServiceTests.cs:73,101,128` (prefix pool mocks) | rewrite: mock `SearchNameCandidatesAsync` | AC-5.2 pre-filter now FTS → REQ-SFT-19 |
| `ArtistResolutionServiceTests.cs:152` `LongMultiWordName_UsesFirstTokenCapped`; `ArtistRepositoryResolutionTests.cs` tests of the artist `GetFuzzyCandidatePoolAsync` | delete with the artist prefix pool and `DerivePrefixToken` (artist side only); replaced by `SearchNameCandidatesAsync` repository tests | AC-5.2 → REQ-SFT-19 |

## 11. Other key decisions

### Decision: MusicBrainz (Q1; recommendation, not decided)
**Recommended approach:** drop MusicBrainz from every Field search and delete the provider.
**Alternatives considered:** keep it behind Deezer as a fallback when Deezer returns nothing.
**Reversibility:** Easily reversible (re-register a class kept in git history).
**Rationale:** (1) its code waits 1.1 s before every call (`MusicBrainzProvider.cs:25,48`), which a
type-ahead cannot absorb; (2) its public limit is about 1 request/second per IP, which debounced typing
exceeds; (3) Lucene search is not typo-tolerant without `~` syntax (probe: "don cry" → "Cry Cry Cry");
(4) first-non-empty-wins let it shadow Deezer (findings § 1). As a fallback it would only run when
Deezer finds nothing, i.e. for the queries it handles worst.

### Decision: open an existing song by replacing the add form
**Chosen approach:** one relative Shell navigation `../song-form?songId=N`.
**Alternatives considered:** push the edit form on top (Back would return to a half-filled add form);
pop then push in two calls (visible flicker, two awaits).
**Reversibility:** Easily reversible. Verified on device in the E2E gate.

### Decision: create the typed artist inside `SongService`
**Chosen approach:** `newArtistName` on `CreateSongWithUrlsAsync`/`UpdateSongAsync`; nested
`IArtistService.CreateArtistAsync` joins the ambient unit of work; `FlushAsync` gives the id.
**Alternatives considered:** VM creates the artist then saves the song (two units of work, orphan
artist on song failure: REQ-FORMUX-20 forbids); keep `SongResolutionService.CommitAsync` (a second write
path CATALOG-PROP would have to hook, findings § 4).
**Reversibility:** Reversible with effort (signature change).
**Rationale:** one write path per operation (REQ-SFT-46) and all-or-nothing (REQ-SFT-18).

### Decision: Title is read-only in edit mode for every song (Helder, sixth round)
**Chosen approach:** `IsReadOnly = true` on Title whenever the form edits a stored song.
**Alternatives considered:** read-only only for API-sourced songs (the earlier draft); editable always.
**Reversibility:** Easily reversible (one binding).
**Consequences:** a typo in a stored title is fixed by deleting and re-creating the song, which loses
its YouTube links and non-derived catalog entries. **Flagged to Helder** by the orchestrator.
`UpdateSongAsync` keeps its `title` parameter (other callers and tests use it); the form always sends
the stored value.

### Decision: artist identity columns are ignored, not dropped
Dropping needs a SQLite table rebuild and the unique `IX_Artists_ExternalId` removal for no runtime
gain (findings § 3.2). Helder approved keeping them.

## 12. Invariants and postconditions

- **INV-SFT-1:** no song write from the Song form bypasses `CreateSongWithUrlsAsync`/`UpdateSongAsync`.
- **INV-SFT-2:** no song or artist row is written before the user taps Save (picks write nothing).
- **INV-SFT-3:** after any committed write, `SongSearch` has exactly one row per song, holding the
  current title and artist name.
- **INV-SFT-4:** no two songs share a non-null `(ExternalProvider, ExternalId)`.
- **INV-SFT-6 (HARD RULE, REQ-SFT-52):** no two artists share a name under `NOCASE_NOACCENT`.
  Layers: (1) `SaveFromFormAsync` rule 2 reuses an exact collated match and never asks to create it;
  (2) `ArtistService.CreateArtistAsync`/`UpdateArtistAsync` reject a collated duplicate
  (`ArtistService.cs:66-67,102-103`), and they are the only artist create/rename code (the song path
  reaches creation only through `CreateArtistAsync`, § 3.2); (3) `IX_Artists_Name`, unique on the
  collated column (`ArtistConfiguration.cs:17-21,39-41`). Race: a unique violation on
  `IX_Artists_Name` surfaces as `DbUpdateException` from the unit of work's save; `SongService` and
  `ArtistService` catch that specific case around their write call and return
  `(false, "An artist with this name already exists")` (`Failed(Artist)` from `SaveFromFormAsync`).
  The transaction is already rolled back, so nothing is written. The catch must use one of the
  allowed patterns in `code-style-reference.md § Exception Handling` and match only this index;
  other exceptions propagate unchanged.
- **INV-SFT-5:** in the search/suggestion paths (`SongSuggestionService`, `ArtistSuggestionService`),
  C# similarity code only orders DB-retrieved lists; it never filters or deduplicates (exception
  § 6.4). The resolution services keep their sanctioned `Score ≥ 0.82` threshold over a DB-bounded
  pool (AC-5.2), which does filter; that is not covered by this exception and is unchanged.
- Existing INV-1…INV-3 (`song-import-resolution`) hold; INV-4 is removed; INV-5 is narrowed by § 6.4.

## 13. Coordination with CATALOG-PROP

CATALOG-PROP (D1–D4, `changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/README.md`)
will add a `Catalog(Song.ArtistId, Song.Id)` row on create and move it on artist reassignment inside
the same two methods. This design is compatible: the new artist has a real `Id` after `FlushAsync`
before the song is staged, so the catalog hook needs no special case; an edit that creates a new
artist is an ordinary reassignment to it. `SaveFromFormAsync` writes nothing itself, so it adds no
path CATALOG-PROP must hook. No conflict with D1–D4 was found. Both items write
`SongService.cs` and add migrations, so they are serialized (single writer); see `tasks.md`.

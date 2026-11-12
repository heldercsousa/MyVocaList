# Design: Song form title-first, one search over local and music APIs

> **Status: DRAFT, in review (2026-09-25).** Requirements: `requirements.md` (REQ-SFT-01…53; Q5/Q7 amendment 2026-11-04 approved by Helder 2026-11-04).
> Evidence: `findings.md` (same folder) and a scratchpad probe run on 2026-09-24 against the app's
> bundled SQLite build on desktop (§ 6.1; Android is gated by spike G5). Current-behaviour claims cite
> `file:line` on `develop` at `f9a67f62`.

## 1. Architecture

| Layer | Change |
|-------|--------|
| **Domain** | Drop `HasManualEdits` (Song, Artist). New read model `SongSearchHit`. `ISongRepository`/`IArtistRepository` gain search/lookup methods. `ISimilarityScorer.RankScore`. Save request/outcome records (§ 3.5). `ISongResolutionService`/`IArtistResolutionService` shrink to `ResolveAsync`. `ResolutionChoice` and `FieldDiff` deleted. |
| **Infra** | Migrations M1–M3 (§ 7). FTS5 trigram tables `SongSearch` and `ArtistSearch` + triggers. New repository queries. `SimilarityScorer.RankScore`. `UseSqlReturningClause(false)` on Songs and Artists. |
| **Services** | `SongService.SaveFromFormAsync` holds every Save decision (§ 3.5) and delegates writes to `CreateSongWithUrlsAsync`/`UpdateSongAsync`, which create a typed artist inside the song's unit of work. `SongSuggestionService` local and remote title search. `ArtistResolutionService` retrieves near-duplicate candidates from `ArtistSearch`. `DeezerProvider` plain query. Resolution `CommitAsync` paths deleted. |
| **Q5/Q7 additions (2026-11-04)** | Domain: `INetworkStatus`. Contracts: `RemoteSearchStatus`, `MusicSearchResultDto.Version`. Services: offline check + status in both suggestion services; `DeezerProvider` maps `title_short`/`title_version`. App project: `UI/Services/MauiNetworkStatus` + `MauiProgram.cs` registration (§ 3.6) |
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
/// REQ-SFT-05(b): collated (title, artist name, version) matches, batch. Amended 2026-11-04 (Q5 = B):
/// the version comes per item from the Deezer row, no longer a single "" for the whole batch.
Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(IReadOnlyCollection<(string Title, string ArtistName, string Version)> items, CancellationToken ct);
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

> **Spec updated 2026-11-04 (3.2a):** both `artistId` and a non-blank `newArtistName` → `(false, "Artist is required")` (no new UI string; blank name = not supplied, REQ-SFT-21 note). The update duplicate message is now the create path's "A song with this title and version already exists for this artist" (one message for both paths; the title-only text is gone). Effective version = `version ?? stored Version ?? ""`.

Both remain the only song write paths (REQ-SFT-46). `CreateSongAsync` (title-only uniqueness,
`SongService.cs:90`) loses its last production caller when `SongResolutionService.CommitAsync` goes;
it is left in place for its test fixtures and flagged as a follow-up, not changed here.

### 3.3 Suggestion services

```csharp
// Contracts/DTOs/Suggestions/SongSuggestionDto.cs: add Version (remote: the normalised Deezer
// title_version, "" when none — REQ-SFT-12, amended 2026-11-04) and
// LocalArtistName (stored spelling; null when the artist is not local).
// Contracts/DTOs/Suggestions/RemoteSearchStatus.cs (new, 2026-11-04, REQ-SFT-06/53)
public enum RemoteSearchStatus { Ok, Offline, Unavailable }
// Contracts/DTOs/Suggestions/SongRemoteSearchResult.cs (new)
public sealed record SongRemoteSearchResult(
    IReadOnlyList<SongSuggestionDto> Promoted,  // Deezer hits that exist locally, as local rows (REQ-SFT-05)
    IReadOnlyList<SongSuggestionDto> Remote,    // <= MaxSearchResults, not in the library
    RemoteSearchStatus Status);                 // REQ-SFT-06/53 (was bool RemoteUnavailable)

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
1. Below 3 characters → empty, no call, `Status = Ok`.
1a. *(2026-11-04, REQ-SFT-53)* `INetworkStatus.HasInternetAccess` (§ 3.6) is `false` → empty,
   **no provider call**, `Status = Offline`, log Information. Read once per call, never cached.
2. Deezer `SearchSongsAsync(q)` **outside** any unit-of-work lambda (no open scope during HTTP,
   existing test `SongSuggestionThresholdTests.cs:128`), bounded by a linked CTS with
   `CancelAfter(3000)`. Caller cancellation → rethrow (stale query). Any other exception, the
   timeout, or a provider-reported error (§ 3.4) → `Status = Unavailable`, log Warning.
3. Dedup in one `ExecuteReadAsync`: `FindByExternalIdsAsync("Deezer", ids)`, then
   `FindByTitleAndArtistNamesAsync(triples)` for the rest, each triple = (Deezer `title_short`,
   artist name, normalised version) per REQ-SFT-05(b)/12 *(2026-11-04: was pairs with version "")*. A hit whose song id is not in
   `shownLocalSongIds` goes to `Promoted`; either way the remote row is dropped. Tier (c) similarity
   dedup (`SongSuggestionService.cs:130-175`) is deleted: it hid other artists' songs.
4. For remaining remote rows, resolve `LocalArtistId/LocalArtistName` by collated artist name in the
   same read; the C# `OrdinalIgnoreCase` re-filters (`SongSuggestionService.cs:162,204`) are removed.

> **Spec updated 2026-11-04 (4.2 Red `008ef44d`):** constructor order fixed by the Tester (design left it open): `SongSuggestionService(ISongRepository, IArtistRepository, IUnitOfWork, IEnumerable<IMusicMetadataProvider>, ISimilarityScorer, INetworkStatus, ILogger<SongSuggestionService>)`. The provider is picked by `ProviderName == "Deezer"`; the song-side `MusicBrainz` branch is removed from the new path (4.3 removes the legacy methods).

> **Spec updated 2026-11-04 (4.2 merge `b100ceb7`, Verifier CONDITIONAL PASS):** steps 3–4 are implemented per item, not as one batch: one `FindByTitleAndArtistNamesAsync([triple])` per unmatched Deezer row (all inside the single dedup `ExecuteReadAsync`), then one `GetByNamesCollatedAsync([name])` per distinct remaining artist in a **second** read scope after take-5. Reason: mapping batch hits back to rows would need C#-side case/accent matching (HARD RULE); positional per-item calls keep the DB the only judge. Accepted (≤ ~25 + 5 indexed reads, single device). The defensive "no Deezer provider registered" branch returns `Unavailable` + Warning — accepted as Level-C plumbing (DI always registers Deezer; BUG-082 test). A batch repository method returning the input index is a possible later optimisation.

> **Spec updated 2026-11-04 (6.1 Red `66a1e8fb`):** artist side mirrors the song side — `ArtistSuggestionService(IArtistRepository, IUnitOfWork, IEnumerable<IMusicMetadataProvider>, ISimilarityScorer, INetworkStatus, ILogger<ArtistSuggestionService>)`; `SearchNameLocalAsync(query, ct)` (FTS `SearchNameCandidatesAsync`, `RankScore(q, name)`, take 5) and `SearchNameRemoteAsync(query, ct)` → `ArtistRemoteSearchResult(Remote, Status)`; Deezer-only, offline/unavailable as § 3.6; the REQ-SFT-48 collated drop is one `GetByNamesCollatedAsync([name])` per Deezer row inside one read scope (same HARD-RULE reason as songs). `GetLocalAsync`/`GetRemoteAsync`/`FilterSimilar` and the MusicBrainz branch are deleted. `IsExactMatch` on local rows is not defined by the spec (left false; 6.2 decides whether the VM needs it). **Update 2026-11-06 (Helder):** nothing reads it, so 5.3b removes `ArtistSuggestionDto.IsExactMatch`; 6.2 adds a flag only if it needs one.

### 3.4 Providers and the Field dependency

- `IMusicMetadataProvider` contract change: `SearchSongsAsync`/`SearchArtistsAsync` **throw** on
  transport failure (`HttpRequestException`, `JsonException`, timeout) and on a Deezer error body.
  Deezer answers some failures, such as over quota, with **HTTP 200 and an `error` object** instead of
  `data`; the provider reads it and throws `MusicProviderException`. Today every failure is swallowed
  and returns empty (`DeezerProvider.cs:60-64`), which makes REQ-SFT-06 impossible. The suggestion
  services are the catch boundary; no exception escapes a service.
- `DeezerProvider.SearchSongsAsync`: `search/track?q={Uri.EscapeDataString(term)}` with no `track:`/
  `artist:` qualifiers (REQ-SFT-35; `DeezerProvider.cs:47-50` today). `artistHint` is ignored and
  removed from the title-search path. Maps `id`, `artist.name`, `title_short` (fallback `title`) and `title_version` (Q5 = B, decided 2026-11-04; see the note below).
  > **Spec updated 2026-11-04 (Q5 = B, REQ-SFT-12):** the provider is the anti-corruption boundary
  > for Deezer's shape. `MusicSearchResultDto` (`Contracts/DTOs/MusicSearchResultDto.cs`) gains a
  > trailing optional `string? Version = null` (optional so existing constructions, e.g.
  > `MusicBrainzProvider` until 4.1b and test fixtures, still compile). `DeezerProvider` maps
  > `SongTitle` = `title_short` (fallback `title` when `title_short` is missing or empty) and
  > `Version` = `NormalizeVersion(title_version)`: trim, then remove the outer brackets only when the value starts with `(` or `[` and that bracket's matching closer is the value's last character; trim again; otherwise keep it as is ("matching" counts nesting depth of the same bracket type only; the other type is ignored, so `"(Live [2011])"` → `"Live [2011]"`); `""` when missing or empty
  > (`"(Live) (2011)"` and `"(Live]"` stay unchanged). It is a `private static` helper in `DeezerProvider`. This is
  > provider data shaping at the boundary (removing Deezer's display brackets), not case/accent folding
  > and not a `*Normalized` column. The shaped value is what gets stored and what REQ-SFT-05(b) compares,
  > and that comparison's case/accent handling stays in the DB (`EF.Functions.Collate`), so the
  > `constraints-registry.md` HARD RULE is respected. Deezer exposes no lyrics text (verified on the live API
  > 2026-11-04; requirements REQ-SFT-12 note), so nothing maps to `Lyrics`.
- **Field surface used, no new option property:** the "In your library"/"Deezer" labels are inside
  `AutocompleteSuggestion.SupportingText`; the online-search note (offline or unavailable, REQ-SFT-06, amended 2026-11-04) is `AutocompleteDXField.SupportingText`
  (REQ-ACF-12c), bound to `TitleSupportingText` / `NameSupportingText`. There is no status row.
- **Append capability (REQ-SFT-07), requested from the dependency item.** DX `AsyncItemsSourceProvider`
  takes one result list per `ItemsRequested`, so the Field cannot show local options and add Deezer
  options later on its own. Request: `IMenuOptionSource` gains an optional second phase, e.g.
  `IAsyncEnumerable<IReadOnlyList<AutocompleteSuggestion>> SearchPhasesAsync(query, ct)`, and the Field
  replaces its menu items with each yielded list while the query is current and the menu is open.
  **If rejected (Q16):** `TitleOptionSource` awaits local and remote together and returns one list; the
  3,000 ms bound (REQ-SFT-08) caps how long local options wait.

  > **Spec updated 2026-09-26:** Q16 RESOLVED — the append capability is **not** requested; the fallback above is
  > the design (`TitleOptionSource` and `NameOptionSource` await local, then remote, and return one list).
  > Orchestrator decision under Helder's pre-authorisation of 2026-09-25; plan § 0.3 D1.

  > **Spec updated 2026-11-11 (ACF close-out):** the reason given above ("DX `AsyncItemsSourceProvider` takes one result
  > list per `ItemsRequested`") no longer holds: since ACF T12 the `AutocompleteDXField` runs the search itself and DX's
  > async provider is not used. The decision is unchanged and now confirmed by Helder (2026-11-11): no loading indicator
  > while Deezer answers; the options are shown when the response arrives (one list per `SearchAsync`, REQ-SFT-08 bound).
  > Consequences of the shared component for this form: a search with no options shows nothing (no "no results" row,
  > REQ-ACF-31); while a new query runs, an already open menu keeps the previous query's options until the new list
  > replaces them (ACF design § 3.1 T12), which on the Title field can last up to the 3,000 ms bound.

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
`UpdateSongAsync`, which own the only writes (REQ-SFT-46, CATALOG-PROP). The unique DB indexes are a
safety net only; concurrency is not designed for (one device, one user).

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

> **Spec updated 2026-11-04 (3.10 Red `f4fd7de1`) — choices fixed by the Tester where this section was open; PENDING Helder's Architectural-lane review of 3.10:**
> 1. Rule 1 order: `ValidateTitleInput` → `Failed(Title)`; `ValidateVersionInput(v, false)` (incl. > 60) → `Failed(Version)`; no `ArtistId` and blank `ArtistText` → `Failed(Artist)` "Artist is required"; when an artist would be created (no `ArtistId`, decision ≠ `UseExisting`) `IArtistService.ValidateNameInput(ArtistText)` → `Failed(Artist)` with its message. All before any resolution or write.
> 2. Artist step: `ArtistId` / `UseExisting(id)` → id, no resolver call; `CreateTyped` → create, no resolver call; `None` → `ResolveAsync(new ArtistCandidate(text, null, null))` (trim allowed); `FuzzyCandidates` → `ArtistChoiceNeeded` with the top 3 sorted by score descending (the service sorts); returns before the song guard.
> 3. **Song-guard artist (deviation from "for an existing artist, its id"):** `SongCandidate` carries no artist id, so the artist goes in as `ArtistCandidate(name, null, null)` — the typed text for a new/text-resolved artist, the **stored** name (`IArtistRepository.GetByIdAsync`) for a picked / `UseExisting` / resolver-exact artist. The song's provider/id go on the `SongCandidate`, never on its `Artist` (REQ-SFT-41).
> 4. Exact `SongChoiceNeeded`: `ExactLocalMatch` and `ExactExternalMatch` both exact; `SongCandidates` = one `SongMatch` loaded via `ISongRepository.GetByIdAsync(ExactMatchSongId)`; fuzzy → resolver candidates in order, `IsExactSongMatch = false`; nothing written.
> 5. Song guard only in add mode with `SongDecision == None`; `SaveAsNewVersion` requires a non-empty Version; `SaveAsNewSong` does not.
> 6. **Edit mode** runs no song guard and always passes `null` external identity to `UpdateSongAsync` (stored identity kept, even if the request carries a different one); artist rules still apply.
> 7. Write failure `(false, msg)` → `Failed`, message as returned, `SavedSongId = null`; success → `Saved`, `SavedSongId` = new id, `FailedField = None`, non-empty message.
> `SaveFromForm_Metalica_RealResolutionAndScorer_ReturnsArtistChoiceNeeded` uses the real `ArtistResolutionService` + `SimilarityScorer` and closes the 3.9c W1 follow-up.

> **Spec updated 2026-11-05 (3.10 follow-up; Helder rulings D1, D2, D4; Helder approved all 14 choices and decisions below on 2026-11-05):**
> - **Tester choices 1–7** (the 2026-11-04 note above, items 1–7) are approved as written. The follow-up tests add: (D2) the failure origin is observed through the collaborator mocks, not a changed tuple: an artist-create refusal is `Failed(Artist, msg)`; a song-level refusal after the artist was created is `Failed(None, msg)`; (D6-1) a picked, `UseExisting` or resolver-exact artist id with no stored row is `Failed(Artist, "Artist not found")`, nothing written; (D6-2) an exact duplicate song that cannot be loaded is treated as no match (Warning logged) and the write decides; (D6-4) defensive kinds (exact artist or exact song without an id, fuzzy artist with an empty list) fall through to the write.
> - **Ruling D1 (SaveAsNew* writes a NULL identity):** `SaveFromFormAsync` rule 5 sets `ExternalId` and `ExternalProvider` to null before the create write when `SongDecision` is `SaveAsNewVersion` or `SaveAsNewSong`. The write-time identity refusal (REQ-SFT-26) therefore runs only on the no-decision path. This holds for a new artist too: the artist is created and the song saved.
> - **Ruling D2 (failure origin, shared private core):** the public `CreateSongWithUrlsAsync` (both overloads) and `UpdateSongAsync` keep their signatures. Their bodies moved to private `CreateSongWithUrlsCoreAsync` / `UpdateSongCoreAsync`, which return `(success, message, failedField)` from the same unit-of-work body. The public methods delegate to the cores, so the cores are the only write bodies (REQ-SFT-46). `SaveFromFormAsync` calls the cores and maps the field into `Failed(field, msg)`. Field mapping: title and version validation give `Title` / `Version`; artist-create refusal and artist-not-found give `Artist`; identity held, title/version duplicate and song not found give `None`.
> - **Ruling D4:** `SongFormSaveOutcome.TypedName` (string) is the trimmed typed artist name, with no case or accent folding, set on `ArtistChoiceNeeded`. It is a non-positional `init` property with default `""`, so the record's primary constructor and every existing call site are unchanged.
> - **Builder decisions (3.10 Green `5250d0fc`, items 1–7) are approved as written** in the task-log Green entry.
> - **Decisions not fixed by design (follow-up, for review):** (a) the edit path also returns the failing field from `UpdateSongCoreAsync`, so D2 covers artist creation on update as well as on create; (b) the "Artist is required" guard in both cores reports `Artist`; (c) the D1 nulling lives in the caller (`SaveFromFormAsync`), so direct callers of the public `CreateSongWithUrlsAsync` keep the identity check unchanged; (d) the `TypedName` member is an `init` property rather than a positional parameter, to keep the constructor signature.

### 3.6 Network status (new 2026-11-04, REQ-SFT-53)

```csharp
// Domain/ServicesInterfaces/INetworkStatus.cs (new) — no MAUI type in Domain or Services
public interface INetworkStatus
{
    /// false only when the platform reports no internet route (NetworkAccess None or Local).
    /// Unknown and ConstrainedInternet return true: the remote call is attempted, and a failure
    /// yields RemoteSearchStatus.Unavailable (REQ-SFT-53). Read live on every call; never cached.
    bool HasInternetAccess { get; }
}

// MyVocaList/UI/Services/MauiNetworkStatus.cs (new, app project, beside INavigationService) — the only MAUI-aware piece
public sealed class MauiNetworkStatus(IConnectivity connectivity) : INetworkStatus
{
    public bool HasInternetAccess => IsInternetReachable(connectivity.NetworkAccess);

    /// Pure mapping (REQ-SFT-53 / Q19), unit-tested as a Level B theory over all five values.
    public static bool IsInternetReachable(NetworkAccess access) =>
        access is not (NetworkAccess.None or NetworkAccess.Local);
}
```

- `IConnectivity` / `NetworkAccess` are `Microsoft.Maui.Networking` (MAUI Essentials). Verified via Context7
  against MAUI 10.0.20's `PublicAPI.Shipped.txt` 2026-11-04: `NetworkAccess` = `Unknown(0)`, `None(1)`,
  `Local(2)`, `ConstrainedInternet(3)`, `Internet(4)`. The implementor re-checks against the `.csproj` pin.
- DI in **`MauiProgram.cs`**, beside the other MAUI-only services (`INavigationService`, `MauiProgram.cs:107`),
  **not** in `ServiceCollectionExtensions.AddAppServices`, as a test-isolation choice: the off-device DI tests build
  `AddAppServices`, and resolving anything that reaches `Connectivity.Current` there would throw off-device (the
  `AppInfo` failure class seen in COPY-YR); keeping platform services in `MauiProgram.cs` keeps that graph MAUI-free.
  `builder.Services.AddSingleton<IConnectivity>(_ => Connectivity.Current)` (factory: lazy) and
  `builder.Services.AddSingleton<INetworkStatus, MauiNetworkStatus>()`. Singleton: stateless, reads the live
  platform value. DI graph tests that resolve a suggestion service add `Mock.Of<INetworkStatus>()`, as they
  already do for `INavigationService`.
- Android needs the `ACCESS_NETWORK_STATE` permission for `Connectivity`; it is already declared
  (`Platforms/Android/AndroidManifest.xml:14`, spec-review 2026-11-04), so no manifest change.
- Consumers: `SongSuggestionService` and `ArtistSuggestionService` (constructor). Service tests use
  `Mock<INetworkStatus>` (Level A). The access-level mapping is a pure static method tested as a Level B
  theory over all five `NetworkAccess` values (the test project references the app project, so the enum is
  available); only the one-line `HasInternetAccess` delegation is untested (Level C). The on-device gate
  checks airplane mode (None).
- **Why not test-and-call (just let `HttpClient` fail)?** Offline would then wait for the DNS/connect
  failure (often seconds on Android) and show the unavailable note instead of the offline note, which is
  exactly the confusion Helder asked to remove (Q7).

## 4. ViewModel contract (`SongFormViewModel`)

| Member | Kind | Purpose / REQ |
|--------|------|---------------|
| `TitleOptionSource` | `IMenuOptionSource` | Phase 1: `SearchTitleLocalAsync` → options (Headline title, SupportingText "artist · version · In your library"). Phase 2: `SearchTitleRemoteAsync` → promoted rows appended to local, then remote options ("artist · version · Deezer", version part omitted when empty; 2026-11-04). REQ-SFT-02…09 |
| `TitleSupportingText` | observable → Field `SupportingText` | Mapped from the latest remote-step query's `RemoteSearchStatus` (REQ-SFT-06, amended 2026-11-04): `Offline` → "You're offline — showing songs from your library only."; `Unavailable` → "Online search isn't available right now — showing your library only."; `Ok` → `""`. A query below 3 characters leaves it unchanged; X clears it, and so does Title becoming empty (`OnSongTitleChanged` partial — the Field never queries an empty string). The two texts are `const string`s in the VM (UI text; one place) |
| `ApplyRemotePick` (Version) | private | *(2026-11-04, Q5 = B)* sets `SongTitle` = the DTO title (`title_short`) and **`Version` = the DTO's normalised version** (overwrites, including with `""`); Version is not locked (REQ-SFT-12). Remote option SupportingText = "artist · version · Deezer", version part omitted when empty (REQ-SFT-04) |
| `SongTitle` / `IsTitleLocked` | observable, TwoWay to Field | Title text and Locked state |
| `IsTitleSearchEnabled` | `=> !IsEditMode` | REQ-SFT-27 |
| (no `IsTitleReadOnly`) | — | Title is never read-only (REQ-SFT-27). The component's `IsReadOnly` has **no consumer in this spec**; it stays in the component (generic, already approved) for future use. One `AutocompleteDXField` serves both modes |
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

> **Spec updated 2026-11-04 (5.2 Red `7b8e27a2`) — choices fixed by the Tester:** `SongFormViewModel` takes `ISongSuggestionService` as its **last** constructor parameter (after `INavigationService`); option `Data` is the `SongSuggestionDto` (dispatch on `IsRemote` / `LocalId`); supporting text "artist · version · In your library" (local/promoted) or "artist · version · Deezer" (remote), version part omitted when empty; the note is set only for queries ≥ `MinimumRemoteQueryLength`, through one `RunOnUiThread`, after `ct.ThrowIfCancellationRequested()`; "other input" for the discard sheet (REQ-SFT-11) = non-empty Artist text, Version, Featured, Lyrics, or any YouTube URL; Keep editing restores the **last query** passed to the option source (the Field may already show the headline), unlocks Title, changes nothing else; X on Title resets Version only if it still equals the value the last pick wrote (Q21).

### 4.1 Artist form (`ArtistFormViewModel`, REQ-SFT-47…50)

`IArtistSuggestionService` is reshaped like the song service: `SearchNameLocalAsync(query, ct)` and
`SearchNameRemoteAsync(query, ct) → ArtistRemoteSearchResult(Remote, Status)` *(2026-11-04: `RemoteSearchStatus Status`
replaces `bool RemoteUnavailable`; the offline check of § 3.3 step 1a applies)*.
Local = the existing collated prefix search (`IArtistRepository.SearchByNameAsync`, ≤ 5; no ranking,
so § 6.4 does not apply).

> **Spec updated 2026-09-26:** superseded. Local = `IArtistRepository.SearchNameCandidatesAsync(q,
> SimilarityConstants.PoolSize)` (FTS5 `ArtistSearch`, § 6.2; collated prefix fallback under 3 characters), ranked
> by `ISimilarityScorer.RankScore(q, name)` stable over DB order, take 5. The prefix query could not list "Metallica"
> for "metalica" (E2E line 11/12, REQ-SFT-47/48). § 6.4 now also covers `ArtistSuggestionService` (ordering only).
> The Deezer-row dedup by collated name is unchanged. Plan § 0.3 D20. Remote = Deezer `search/artist?q=` (≤ 5), with the same 3,000 ms bound and
failure handling as § 3.3. Dedup: a Deezer artist whose name is a collated match for a local artist is
dropped (the local row stays). The similarity dedup tier (c) in `ArtistSuggestionService` and
`FilterSimilar` lose their callers and are deleted: C# similarity must not deduplicate (§ 6.4 scope).

> **Spec updated 2026-11-05 (6.1 Green):** the Deezer-row dedup runs as one `GetByNamesCollatedAsync([name])` per distinct remaining name inside a single read scope after the HTTP call (same positional-lookup decision as § 3.3 step 4, 4.2 merge note); a missing Deezer provider returns `Unavailable` + Warning (accepted Level-C plumbing, as in § 3.3).

| Member | Purpose |
|--------|---------|
| `NameOptionSource` (`IMenuOptionSource`) | Local options (SupportingText "In your library"), then Deezer options (SupportingText "Deezer") |
| `NameSupportingText` | Field `SupportingText` mapped from `Status` as on the Song form; offline text "You're offline — showing artists from your library only." (Q18, approved 2026-11-04), unavailable text verbatim from the Song form (REQ-SFT-47 note, 2026-11-04) |
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
3. User taps "Don't Cry" / "Guns N' Roses · Original · Deezer" (Deezer `title` "Don't Cry (Original)"). The Field locks Title.
4. `SelectTitleCommand` → `ApplyRemotePick`: `SongTitle` = `title_short` ("Don't Cry"); `Version` = normalised `title_version` ("Original"; 2026-11-04, REQ-SFT-12); `SelectedProvider/ExternalId`
   = ("Deezer", id); if `LocalArtistId` is set → `LockArtist(LocalArtistId, LocalArtistName)`; else
   `ArtistSearchText` = provider artist, `SelectedArtistId = null`, `IsArtistLocked = true`,
   dropdowns closed. No service write.

**F2: add mode, pick an existing song** (REQ-SFT-10/11)
1. User taps a local option (song id N).
2. Always (REQ-SFT-11 withdrawn 2026-11-11; no discard sheet) → `INavigationService.GoToAsync($"../{Routes.SongForm}?songId={N}")`
   (Shell pops the add form and pushes the edit form; `LoadSongForEditAsync` hydrates everything,
   including the artist from the entity, `SongFormViewModel.cs:469-477`).
3. ~~Discard sheet~~ removed 2026-11-11 (REQ-SFT-11 withdrawn).

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
2. Title: the same `AutocompleteDXField` with `IsSearchEnabled = false`: Editable plain text for every
   song (no options, no menu). Save sends the edited title to `UpdateSongAsync`, which applies
   REQ-SFT-15/29 and leaves `ExternalId`/`ExternalProvider` as stored (the form passes `null` for both,
   "keep existing", `SongService.cs:180-183`).
3. Save: `SaveFromFormAsync` with `SongId` set: rules 1, 2, 5 (no song guard; `UpdateSongAsync`
   enforces (artist, title, version) uniqueness, REQ-SFT-29).

**F7: offline**
1. *(2026-11-04)* `INetworkStatus.HasInternetAccess` is false → no Deezer call, `Status = Offline`;
   local options at once; `TitleSupportingText` = the offline note.
2. Deezer throws, times out or returns an error body → `Status = Unavailable`; local options stay;
   `TitleSupportingText` = the unavailable note.
3. Save paths use no network and are unaffected.

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
| 2026-09-24 | `constraints-registry.md § EF Core / SQLite` — "No C#-side string normalization for search/deduplication — HARD RULE"; and `song-import-resolution/requirements.md` INV-5 ("No matching path performs C#-side case/accent normalization") | Song title search (item `song-form-title-first-one-search-over-local-and-music-apis`, REQ-SFT-30…32). The database does all **retrieval**: an FTS5 trigram index with `remove_diacritics` returns a candidate list bounded by `SimilarityConstants.PoolSize` (50). C# (`SimilarityScorer.Normalize` + FuzzySharp `TokenSortRatio`, via `ISimilarityScorer.RankScore`) only **orders** that bounded list for display. It never decides which rows exist, never filters out a row, never deduplicates and never checks uniqueness. Both reasons behind the rule are absent: correctness (the DB, not C#, decides matches, so "café"/"cafe" is still matched by SQLite) and performance (retrieval uses the index; C# work is at most 50 scores per query). Helder chose this option on 2026-09-24. | `Services/SongSuggestionService.cs` (ranking step of `SearchTitleLocalAsync`); `Infra/Similarity/SimilarityScorer.cs` (`RankScore`). Not `ArtistSuggestionService`: artist search keeps the collated prefix query and does not rank. **Spec updated 2026-09-26:** also `Services/ArtistSuggestionService.cs` (ranking step of `SearchNameLocalAsync`, which now retrieves from `ArtistSearch`; ordering only; plan § 0.3 D20) | When the ranking moves into SQL, or if any code path uses `RankScore` to filter, deduplicate or test uniqueness, or if the candidate list stops being DB-bounded. |

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
- *(2026-11-04, § 3.6)* `MauiProgram.cs`: `IConnectivity` (factory over `Connectivity.Current`) and `INetworkStatus` → `MauiNetworkStatus`, both singletons; never in `AddAppServices`.
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
| **B (2026-11-04)** | `DeezerProvider` field mapping (`title_short` fallback, `NormalizeVersion` theory); `MauiNetworkStatus.IsInternetReachable` theory over all five `NetworkAccess` values | fake `HttpMessageHandler`; pure static |
| **A (2026-11-04)** | suggestion services: Offline (no provider call, Information log), Unavailable vs Offline, network status read per call; VM note mapping (two texts), pick fills Version, X resets pick-filled Version (Q21) | `Mock<INetworkStatus>` |
| **C** | Deletions, DI registration, XAML; `MauiNetworkStatus.HasInternetAccess` delegation | build + census recorded in task-log; `AppServicesRegistrationTests` resolves `ISongSuggestionService` |
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
| `SongSuggestionServiceTests.cs:35,57,94,119,145` | rewrite against `SearchTitleLocalAsync`/`SearchTitleRemoteAsync` (94: dedup now artist- and version-scoped; 145: `Status = Unavailable` (2026-11-04), local still returned) | REQ-FORMUX-03/05 → REQ-SFT-05/06 |
| `SongSuggestionServiceTests.cs:76` (artist hint) | delete | REQ-SFT-35 (no hint) |
| `SongSuggestionServiceTests.cs:170` (MusicBrainz → Deezer fallback) | delete if Q1 = drop | AC-4.2 provider order → REQ-SFT-36 |
| `SongSuggestionThresholdTests.cs` (6) | retarget to `SearchTitleLocalAsync`/`SearchTitleRemoteAsync`, same assertions | REQ-UOW-51/52 kept |
| `SongServiceTests.cs` `UpdateSongAsync_*` (13) | mechanical: drop the `hasManualEdits` argument; `:245,:480` mock `ExistsByTitleVersionForArtistAsync` | REQ-SFT-29 |
| Other `UpdateSongAsync` callers (census 2026-09-25): `SongServiceUpdateIntegrationTests.cs` (2), `Bug068RegressionTests.cs` (1), `SongFormViewModelTests.cs` (6), `SongResolutionServiceTests.cs` (6, mostly deleted in 3.3) | mechanical signature update, `tasks.md` 3.2b/3.2c | — |
| `ArtistResolutionServiceTests.cs:73,101,128` (prefix pool mocks) | rewrite: mock `SearchNameCandidatesAsync` | AC-5.2 pre-filter now FTS → REQ-SFT-19 |
| `ArtistResolutionServiceTests.cs:152` `LongMultiWordName_UsesFirstTokenCapped`; `ArtistRepositoryResolutionTests.cs` tests of the artist `GetFuzzyCandidatePoolAsync` | delete with the artist prefix pool and `DerivePrefixToken` (artist side only); replaced by `SearchNameCandidatesAsync` repository tests | AC-5.2 → REQ-SFT-19 |

## 11. Other key decisions

### Decision: MusicBrainz (Q1; decided — drop, Helder 2026-11-04)
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

### Decision: Title is editable in edit mode for every song (Helder, seventh round)
**Chosen approach:** Title stays an Editable text field in edit mode (`IsSearchEnabled = false`).
**Alternatives considered:** read-only for every song (sixth round, reversed: a typo would force
delete + re-create); read-only only for API-sourced songs (first draft).
**Reversibility:** Easily reversible (one binding).
**Rationale:** a song's history (YouTube links, catalog rows, per-singer karaoke performance history
feeding future AI suggestions) must survive a title fix. An API-sourced song keeps its external
identity; only the local title changes.

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
  collated column (`ArtistConfiguration.cs:17-21,39-41`), as a safety net. No concurrent-write
  handling: the database is local to one device and one user (Helder, seventh round).
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

> **Update 2026-11-09 (BUG-085):** the Save-time fuzzy song step no longer uses the first-word prefix pool. `ISongRepository.SearchTitleCandidatesForArtistAsync(artistId, title, take, ct)` retrieves same-artist candidates from the `SongSearch` trigram index (same `FtsQueryBuilder.BuildTrigramMatch` as the artist path; collated-prefix fallback for titles under 3 characters); `SongResolutionService` scores them with the unchanged 0.82 threshold. `GetFuzzyCandidatePoolAsync` is unused and queued for removal.

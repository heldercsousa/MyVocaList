# Song form: title-first redesign evaluation (read-only analysis, 2026-09-24)

Repo `C:\Users\helde\source\repos\MyVocaList`, branch `develop`. No files were edited. Censuses used a Python
`os.walk` substring scan over `.cs`/`.xaml` (bin/obj excluded), not the rtk-rewritten grep. Paths are relative to the repo root.

Two things were checked outside the repo, in the scratchpad:
- A throwaway console app (`scratchpad/dicheck`) reproduced the DI registration and ran FuzzySharp 2.0.2 scores.
- Live HTTP GETs went to Deezer and MusicBrainz on 2026-09-24, using the app's query shapes.

---

## 0. Headline findings (read these first)

1. **Both remote providers probably return nothing in the real app (DI defect, reproduced).** The registration
   code (`MyVocaList/Extensions/ServiceCollectionExtensions.cs:34-45`) configures `BaseAddress` and `User-Agent`
   on typed clients (`AddHttpClient<MusicBrainzProvider>` / `<DeezerProvider>`). The providers are then
   *also* registered as `AddScoped<IMusicMetadataProvider, MusicBrainzProvider>()` / `<…, DeezerProvider>()`.
   Consumers resolve `IEnumerable<IMusicMetadataProvider>`: `MusicMetadataService.cs:10`,
   `SongSuggestionService.cs:33` and `ArtistSuggestionService.cs:30`. That scoped registration builds the provider
   with the *default* `HttpClient`, which has no `BaseAddress` and no User-Agent. The scratchpad repro printed
   `via interface BaseAddress: <null>` and `via typed BaseAddress: https://musicbrainz.org/ws/2/`. The relative URLs
   (`"recording?query=…"`, `"search/track?q=…"`) then throw. Each provider catches the exception
   (`MusicBrainzProvider.cs:67-71`, `DeezerProvider.cs:60-64`) and returns empty. So today the Song Picker most
   likely always shows "No songs found", with only a log warning. No test covers the real registration: every
   provider test uses `Mock<IMusicMetadataProvider>`. To confirm on a device, look for "MusicBrainz song search failed" in the logs.
2. **The title autocomplete Helder wants is already specified and half-built, but parked.**
   - Spec: `changes/2026-07-10-form-ux-redesign/requirements.md:94-98`, REQ-FORMUX-22…25 and 31. Status is
     🔵 Deferred (`README.md` frontmatter).
   - Code: `ISongSuggestionService` / `SongSuggestionService` exist and have tests. They are **not DI-registered**.
     The census found no reference outside their own files and tests. Memory notes that this is deliberate.
3. **The BUG-008 lock rule `!song.HasManualEdits && song.ExternalId != null` no longer exists in code.** Edit mode now
   locks the artist unconditionally (`SongFormViewModel.cs:477`, REQ-ACREATE-14). The docstring at `:433` is stale.
   The test at `SongFormViewModelTests.cs:787-796` still sets `ExternalId="dz-99", HasManualEdits=false`, but that
   setup has no effect: the lock no longer depends on it.
4. **`HasManualEdits` doesn't record what its name says** (details in §3). Every edit-save sets it `true`, create
   never sets it, and nothing resets it.

---

## 1. Third-party music search today

### Providers, interfaces, DTOs
| Element | Location | Notes |
|---|---|---|
| `IMusicMetadataProvider` | `Services/IMusicMetadataProvider.cs:5-22` | `ProviderName`, `SearchArtistsAsync`, `SearchSongsAsync(term, artistHint)`; "Up to 5 results" |
| `MusicBrainzProvider` | `Services/MusicBrainzProvider.cs` | `await Task.Delay(1100, ct)` before **every** call (`:25`, `:48`) — crude 1 req/s courtesy delay, not a shared limiter; query `recording:{term}[ AND artist:{hint}]` (`:49-52`); `Take(5)` |
| `DeezerProvider` | `Services/DeezerProvider.cs` | query `track:"{term}"[ artist:"{hint}"]` (`:47-50`); `Take(5)` |
| `IMusicMetadataService` / `MusicMetadataService` | `Services/MusicMetadataService.cs:41-62` | iterates providers in **registration order** (MusicBrainz, then Deezer — `ServiceCollectionExtensions.cs:44-45`); first **non-empty** list wins |
| `MusicSearchResultDto` | `Contracts/DTOs/MusicSearchResultDto.cs:3-8` | `ExternalId, Provider, ArtistName, SongTitle?, FeaturedArtists?` |
| HTTP config | `ServiceCollectionExtensions.cs:34-43` | BaseAddress + UA `MyVocaList/1.0 (…github…)`; **no API key** for either provider; no timeout/Polly configured |
| `ISongSuggestionService` / `SongSuggestionService` | `Services/ISongSuggestionService.cs`, `Services/SongSuggestionService.cs` | local + remote title suggestions; **unregistered** |
| `SongSuggestionDto` | `Contracts/DTOs/Suggestions/SongSuggestionDto.cs:14-21` | `LocalId?, Title, ArtistName, LocalArtistId?, ExternalId?, ExternalProvider?, IsRemote` |
| `SearchConstants` | `Domain/Constants/SearchConstants.cs:12,20,28` | `MinimumLocalQueryLength = 2` (REQ-UOW-51), `MinimumRemoteQueryLength = 3` (REQ-UOW-52), `MaxSearchResults = 5` |

`FeaturedArtists` in the DTO is always `null`: both providers pass `null` (`DeezerProvider.cs:53`, `MusicBrainzProvider.cs:60`).

### Entry points (the UI that is live today)
- `SongFormPage.xaml:123-135`: a `ListItem` labelled "Search music database". Its tap runs `NavigateToSongPickerCommand`
  (`SongFormViewModel.cs:138`, `:899-902`), which calls `GoToAsync(Routes.SongPicker)` (`Routes.cs:20`, `AppShell.xaml.cs:32`).
- `SongPickerViewModel` (`MyVocaList/UI/ViewModels/SongPickerViewModel.cs`):
  - `SearchCommand` (`:54-91`) fires on the app-bar action (`SongPickerPage.xaml:25`). It is an explicit submit:
    there is **no debounce and no minimum length** beyond `IsNullOrWhiteSpace` (`:57`). It calls
    `IMusicMetadataService.SearchSongsAsync(SearchText, artistHint: null)` (`:70`).
  - `SelectResultCommand` (`:93-98`; there is no `SelectApiResultCommand`) sends `SongPickedMessage(MusicSearchResultDto)`
    (`Contracts/Messages/SongPickedMessage.cs:5`) and navigates back.
  - `LaunchYouTubeSearchCommand` (`:100-106`).
- `SongFormViewModel.OnSongPicked` (`:488-504`) fills **Title** and **FeaturedArtists** (always empty, see above).
  It stashes `SelectedExternalId` / `SelectedProvider` (`:77-78`), then calls `ResolveAndLockArtistAsync`
  (`:506-545`). That method runs a local artist search for `maxResults: 1` and applies a C# `OrdinalIgnoreCase`
  exact compare. On a hit it locks the artist. On a miss it pre-fills the artist text, leaves it unselected, and
  **does not create the artist**. The API artist's own id is never used.
- The artist field `dxe:AutoCompleteEdit` (`SongFormPage.xaml:28-41`) uses `AsyncItemsSourceProvider RequestDelay="300"`.
  It searches **local artists only** (`SongFormViewModel.cs:298-316` → `IArtistService.SearchArtistsByNameAsync`).
  The title field is a plain `dxe:TextEdit` (`SongFormPage.xaml:87-88`).

### Suggestion service semantics (built but unused)
- `GetLocalAsync` (`SongSuggestionService.cs:48-61`) calls `songRepository.GetPagedAsync(1, 5, term)`, a title **prefix** LIKE.
- `GetRemoteAsync` (`:65-82`) returns nothing below 3 characters. It then:
  - fetches from MusicBrainz first, falling back to Deezer only if MusicBrainz is empty (`:95-105`);
  - de-duplicates in three tiers: (a) same `(Provider, ExternalId)` as a local row; (b) title collation-equal to any
    local song (`GetByTitlesCollatedAsync`, *not* artist-scoped); (c) `ISimilarityScorer ≥ 0.82` against local
    suggestion titles (`:130-175`);
  - resolves the local artist id by collated artist name (`:177-208`).
- Debounce values are in the spec, not the code: 300 ms local and remote ≈ +400 ms (REQ-FORMUX-01/02,
  `form-ux-redesign/requirements.md:53-54`).

### Rate limits and offline behaviour
- **MusicBrainz:** the public policy is about 1 request/second per IP and a required User-Agent. The code only delays
  each call by 1.1 s (`MusicBrainzProvider.cs:25,48`). That adds 1.1 s of latency to every search and does not
  serialise concurrent calls.
- **Deezer:** public, no key. The public quota is 50 requests / 5 s. Nothing in the code enforces it.
- **Offline:** every provider exception is caught and returns empty (`MusicBrainzProvider.cs:67-71`,
  `DeezerProvider.cs:60-64`, `MusicMetadataService.cs:56-59`). The picker shows "No songs found", not "Search failed",
  because the exception never reaches `SongPickerViewModel.cs:80-86`. REQ-FORMUX-05 wants silent local-only
  degradation, which the suggestion service already provides.

### Live probe of the app's query shapes (2026-09-24, one sample; indicative only)
| Query | Deezer, app shape `track:"…"` | Deezer, plain `q=` | MusicBrainz, app shape `recording:…` |
|---|---|---|---|
| `dont cry` | noisy: "…(Bonus Track)", "Boys Don't Cry (Instrumental)"… Spike's "Don't Cry" 5th | **"Don't Cry (Original)" – Guns N' Roses** 1st | 5× "Dont Cry" (other artists), score 100 |
| `don cry` | noisy, no GNR in top 5 | **"Don't Cry (Original)" – GNR** 1st | garbage ("Cry Cry Cry"…) |
| `sweet chld o mine` | covers/backing tracks | **GNR original** 1st | garbage ("Chld") |

Two things stand out. First, the app's Deezer `track:"…"` shape appears to match the literal word "track" as text,
because results are dominated by "Backing Track" and "Bonus Track". Second, MusicBrainz is first in the chain and
almost always returns *something*, so the Deezer fallback would rarely run. **Only Deezer's plain `q=` search is
typo-tolerant.** MusicBrainz is Lucene-based and not fuzzy unless you use the `~` operator, which the code doesn't use.

---

## 2. Similarity / fuzzy machinery

| Component | Algorithm / normalisation | Called from | Reachable from UI? | Tests |
|---|---|---|---|---|
| `ISimilarityScorer` (`Domain/ServicesInterfaces/ISimilarityScorer.cs`) → `SimilarityScorer` (`Infra/Similarity/SimilarityScorer.cs`) | FuzzySharp 2.0.2 `Fuzz.TokenSetRatio`/100 (`:20-21`). C#-side normalisation: NFD, strip non-spacing marks, `ToLowerInvariant` (`:30-40`). FuzzySharp's default preprocessor also lowercases and turns non-alphanumerics into spaces | `SongResolutionService.cs:142`, `ArtistResolutionService.cs:79`, `SongSuggestionService.cs:168`, `ArtistSuggestionService.cs:121,144` | Yes, via `SongResolutionService` on **new-song Save** only | `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs` (identity, case, Björk/Biork, Não Sei, unrelated, null/empty) |
| `SimilarityConstants` (`Domain/Resolution/SimilarityConstants.cs`) | `DefaultThreshold = 0.82` ("provisional"), `PoolSize = 50`, `PrefixTokenMaxLen = 12` | as above | — | indirectly |
| `SongResolutionService.ResolveAsync` (`Services/SongResolutionService.cs:45-163`) | Ladder: resolve the artist first (INV-1); (1) `GetByExternalIdAsync(ExternalId, Provider)`; (2) `ExistsByTitleVersionForArtistAsync` (collated); (3) fuzzy pool = songs of **that artist** whose title `LIKE '<firstToken≤12>%'` (`DerivePrefixToken`, `:377-389`), scored ≥ 0.82 | `SongFormViewModel.cs:638` (new-song Save only; edit Save bypasses it, `:597-621`) | Yes (Save → resolution sheet) | `SongResolutionServiceTests.cs` (14 tests: `:47,70,100,140,172,208,238,271,312,327,343,389,435,481`); `SongRepositoryResolutionTests.cs:107,126`; `NestedUnitOfWorkTests.cs` (5) |
| `ArtistResolutionService.ResolveAsync` (`Services/ArtistResolutionService.cs:35-93`) | (1) external id, (2) `GetByNameAsync` (collated), (3) prefix pool `LIKE 'tok%'`, scored | only from `SongResolutionService.cs:54` and `:294` | ResolveAsync: yes (indirectly). **CommitAsync: no** (see §3) | `ArtistResolutionServiceTests.cs` (10); `ArtistRepositoryResolutionTests.cs:31,47,63` |
| Resolution records (`Domain/Resolution/*.cs`) | `ArtistCandidate`, `SongCandidate`, `ResolutionKind`, `ResolutionChoice`, `FieldDiff`, `SongMatch`, `SongResolution`, `ArtistResolution` | services + VM | yes | via the above |
| `CollationInterceptor` (`Infra/Interceptor/CollationInterceptor.cs`) | registers `NOCASE_NOACCENT` and **overrides `like()` with a managed UDF** (`:65-69`) that normalises both sides in C# (`:84-99`) | every `EF.Functions.Like` | yes | — |

No Levenshtein, Jaro or trigram code exists. There is no FTS and no spellfix; the census found zero hits.

### Would "dont cry" / "don cry" find "Don't Cry"?
FuzzySharp scores, measured:

| Query | Target | Score |
|---|---|---|
| `dont cry` | "Don't Cry" | 94 |
| `don cry` | "Don't Cry" | 88 |
| `dnt cry` | "Don't Cry" | 88 |
| `dont cr` | "Don't Cry" | 88 |
| `sweet chld o mine` | "Sweet Child O' Mine" | 94 |
| `dont` (single word) | "Don't Cry" | 62 |

Every multi-word example clears the 0.82 threshold. **The scorer is fine; the retrieval step is what fails.**

- **Resolution engine:** it needs a *resolved artist* first (`SongResolutionService.cs:85-90`), so it is useless when
  the title comes first.
  - With a known artist, "dont cry" gives prefix `dont`. `LIKE 'dont%'` on "don't cry" fails at the apostrophe, so
    the pool is empty and the result is NoMatch.
  - "don cry" gives prefix `don`. `LIKE 'don%'` matches, then the score of 0.88 gives FuzzyCandidates.
  - So one example works and one doesn't, and only at Save time.
- **Local suggestion (`GetLocalAsync`)** is a title *prefix* LIKE (`SongRepository.cs:25-31`). "dont cry%" and
  "don cry%" both fail against "don't cry". **Neither example matches.**
- **Remote dedup tier (c)** scores remote titles only against the ≤5 local suggestions. It is not a search.

### HARD RULE compliance (`constraints-registry.md § EF Core/SQLite`)
- **Complies:** exact matching uses `EF.Functions.Collate`. See `SongRepository.cs:66-94` and
  `GetByTitlesCollatedAsync` (`:112-122`). There are no `*Normalized` columns; `20260602034045_RemoveNormalizedColumns` removed them.
- **Tensions:**
  1. `SimilarityScorer.Normalize` lowercases and strips accents in C#. The spec sanctions scoring over a bounded pool
     (AC-5.2, `song-import-resolution/requirements.md:70`). But INV-5 (`:108`) literally says "No matching path
     performs C#-side case/accent normalization", so the spec contradicts itself.
  2. After DB queries, the code filters again with C# `OrdinalIgnoreCase`:
     - `SongResolutionService.cs:104-106`, `:357-367`
     - `SongSuggestionService.cs:162`, `:204`
     - `SongFormViewModel.cs:513`

     This is case-only folding, so accent-only differences slip through. Example: `ExistsByTitleVersionForArtistAsync`
     says "Não Sei" exists for "Nao Sei", but the C# re-match fails and returns `ExactLocalMatch` with a null id
     (`SongResolutionService.cs:123-127`). The VM then shows a resolution sheet with no candidates (`SongFormViewModel.cs:650-654`),
     and "Update existing" silently returns because there is no target id (`:709`).
  3. The rule's performance argument ("use DB indexes") already fails for LIKE. The overridden `like()` is a managed
     UDF, so every prefix search is a full scan with a managed callback per row. The spec never mentions this.

### What typo-tolerant local search would need on SQLite (options)
The app ships SQLitePCLRaw `bundle_e_sqlite3` 3.0.3, which is SQLite 3.50.x (`Directory.Packages.props:16-22`),
not Android's system SQLite.

| Option | Feasibility | "dont cry" → "Don't Cry"? | Trade-offs |
|---|---|---|---|
| **FTS5 `trigram` tokenizer** (SQLite ≥ 3.34; `remove_diacritics` for trigram ≥ 3.45) | e_sqlite3 builds include FTS5. EF Core has no mapping, so it needs a raw-SQL migration, sync triggers, and `FromSql` | An **AND** match fails: "ont" is not a trigram of "don't". An **OR-of-trigrams** candidate query plus FuzzySharp rerank would find it | Real index, scales. More migration and trigger plumbing. Queries need ≥ 3 characters. The ranking is not typo-aware by itself |
| **spellfix1** | **Not available.** It is a loadable extension, not compiled into e_sqlite3. It would need a custom native build per ABI (Android and iOS) | — | Too costly to be realistic |
| **In-memory scoring over a bounded set** | Trivial with the existing `SimilarityScorer` | Yes (0.94 / 0.88) | Easiest. A karaoke library is probably a few thousand rows, but "bounded" would mean "all titles". That breaks AC-5.2 and INV-5 as written, so it needs a spec amendment or an `exception-registry.md` entry. Memory cost is O(n) per keystroke unless cached |
| **Hybrid:** collated prefix/contains on **each token's first 2–3 chars** (OR) → pool ≤ 50 → FuzzySharp | Works with today's UDF `like()` | Yes for "don cry" / "dont cry" (the `don%` token hits) | Still a full scan (UDF), but no new infrastructure. Arguably fits AC-5.2 ("pre-filtered… first-token / prefix") |
| **Remote does the fuzzy part** | Deezer plain `q=` is typo-tolerant (probe above) | Yes, online only | The query shape must change from `track:"…"` to plain `q=`, or be combined with it. MusicBrainz would need `~` fuzzy syntax or to be demoted behind Deezer for title search |

---

## 3. API-edit-tracking fields and mechanisms

### Where each came from
- **Migration:** `Infra/Migrations/20260426010205_AddArtistAndSongCatalog.cs`.
  - Artists columns: `:21-23`.
  - Songs columns: `:42-44`.
  - `IX_Artists_ExternalId` (unique): `:60-62`.
  - `IX_Songs_ExternalId` (**non-unique**, single column): `:78-80`.
  - Later migrations only carry these fields in their Designer snapshots.
- **EF config:**
  - Song: `SongConfiguration.cs:39-48`, index at `:61-62`.
  - Artist: `ArtistConfiguration.cs:24-34`, unique index at `:43-45`.
- **Spec vs code mismatch:** the spec says `IX_Songs_ExternalId` is **Unique** (`artists-songs/requirements.md:381`),
  but the code has a non-unique index. The index is on `ExternalId` alone, not `(ExternalProvider, ExternalId)`.

### 3.1 `Song.ExternalId` + `Song.ExternalProvider`
- **Purpose:** the provider identity of an imported song, so a re-import is recognised (US-2, AC-2.1/2.4 in
  `song-import-resolution/requirements.md:44-47`).
- **Writers:**
  - `SongService.CreateSongAsync` (`Services/SongService.cs:100-101`)
  - `CreateSongWithUrlsAsync` (`:230-231`)
  - `UpdateSongAsync` (`:180-183`; null keeps the existing value)
  - `SongResolutionService.CommitAsync`: UpdateExisting `:237-243`, AttachExternalId `:262-267`
  - VM: `CommitNewSongAsync` (`SongFormViewModel.cs:667-676`) and the edit round-trip `:447-448` → `:607-608`
- **Readers:**
  - `SongRepository.GetByExternalIdAsync` (`Infra/Repository/SongRepository.cs:61-63`), used at `SongResolutionService.cs:63-64`
  - List projections: `SongRepository.cs:45`, `CatalogRepository.cs:47`, and the `SongListItemDto`
    (`Contracts/DTOs/List/SongListItemDto.cs:9`). No XAML binds it.
  - `SongSuggestionService.cs:92,135`
- **Tests:**
  - `SongRepositoryTests.cs:198,214`
  - `SongResolutionServiceTests.cs:70,100,271,435`
  - `SongServiceTests.cs` (5 references)
  - `NestedUnitOfWorkTests.cs:195`
  - `SongSuggestionServiceTests.cs`
- **Recommendation: KEEP, and SIMPLIFY the index.** It is the cheapest way to answer "is this API pick already in my
  DB?", which is the core of "picking an API song that already exists → open it in edit mode". It is not sufficient
  on its own: MusicBrainz and Deezer give the same song different ids, and the provider that answers changes per
  query (first non-empty wins). So the pick flow needs **two tiers**: (a) `(Provider, ExternalId)`; (b) collated
  `(Artist name → ArtistId, Title, Version="")`. Make the index composite `(ExternalProvider, ExternalId)`, and decide
  whether the spec (unique) or the code (non-unique) wins.

### 3.2 `Artist.ExternalId` + `Artist.ExternalProvider`
- **Writers:**
  - `ArtistService.CreateArtistAsync` (`Services/ArtistService.cs:72-73`), but **no UI caller passes them**:
    - `ArtistFormViewModel.cs:156` calls `CreateArtistAsync(name)` and discards the id stashed at `:193-194`;
    - `SongFormViewModel.cs:343` inline-create passes a name only.
  - `ArtistResolutionService.CommitAsync` (`:126-132`, `:158-164`) is only reachable through
    `SongResolutionService.CommitAsync` CreateNew/CreateNewVersion. **The UI never calls those**: the VM only uses
    `UpdateExisting` (`SongFormViewModel.cs:733-734`, `:814-815`), and a new-song Save goes straight to
    `SongService.CreateSongWithUrlsAsync` (`:667-676`).
- **Readers:**
  - `ArtistRepository.GetByExternalIdAsync` (`:83-85`), called from `ArtistResolutionService.cs:45-46`
  - `ArtistListItem(Dto).ProviderBadgeText` (`Domain/ReadModels/ArtistListItem.cs:20`, `Contracts/DTOs/List/ArtistListItemDto.cs:14-19`).
    It matches lowercase `"musicbrainz"` / `"deezer"`, but the providers write `"MusicBrainz"` / `"Deezer"`.
    No XAML binds it either.
- **Defect:** `SongFormViewModel.cs:631-634` builds the `ArtistCandidate` with the **song's**
  `SelectedProvider` / `SelectedExternalId`. The artist lookup `GetByExternalIdAsync` therefore searches artists by a track id.
- **Tests:**
  - `ArtistRepositoryTests.cs:196,211`
  - `ArtistResolutionServiceTests.cs:27,200,225,249`
  - `ArtistServiceTests.cs` (4 references)
  - `NestedUnitOfWorkTests.cs:195`
- **Specs:** AC-3.1/3.2/3.4 (`song-import-resolution/requirements.md:53-56`), INV-3 (`:106`), AC-4.6
  (`artists-songs/requirements.md:148`), REQ-FORMUX-06/07.
- **Recommendation: SIMPLIFY.** In production these columns are effectively never populated. "Create the artist if
  missing" works with a collated name lookup plus `CreateArtistAsync`, which is what `ResolveAndLockArtistAsync`
  already half-does. Keep the columns: they are already migrated and cost nothing at runtime. Do not build the
  title-first flow on them. Fix or remove the song-id-as-artist-id candidate construction. Dropping the columns
  would need a migration (SQLite table rebuild plus dropping the unique index) and would supersede AC-3.1 and the
  INV-3 second clause, which gains little.

### 3.3 `Song.HasManualEdits`
- **Declared purpose:** "True if any field was manually changed after API import". It protects the record from
  silent API overwrite (AC-4.1–4.4, `song-import-resolution/requirements.md:61-64`; INV-4 `:107`; AC-11.4/11.5
  `artists-songs/requirements.md:280-281`; REQ-FORMUX-31).
- **Actual behaviour:**
  - **Writer:** `SongService.UpdateSongAsync:172` (`song.HasManualEdits = hasManualEdits`).
    - The edit Save always passes `true` (`SongFormViewModel.cs:606`), even for a song typed by hand and saved with
      no changes.
    - The resolution commits re-pass the stored value (`SongResolutionService.cs:242,266`).
    - Create never sets it (default `false`), and nothing resets it.
  - So in practice it means "was saved from the edit screen at least once".
- **Readers:**
  - `SongResolutionService.cs:71-80` and `:110-120` (whether to compute FieldDiffs); `ApplyUpdate` `:316-346`
  - `SongFormViewModel.cs:716` (merge sheet trigger)
  - List projections `SongRepository.cs:46`, `CatalogRepository.cs:48` and `SongListItemDto.cs:10`, none bound in XAML
  - It no longer drives `IsArtistLocked` (see §0.3)
- **Tests:**
  - `SongResolutionServiceTests.cs:100,271,343,389`
  - `SongFormViewModelTests.cs:707` (merge rows), `:787-796` (setup only, no effect)
- **Recommendation: DROP.** In the title-first flow, "API pick of an existing song → open it in edit mode" means an
  API value never overwrites a stored record at all. The user sees the stored record and edits it by hand, so INV-4
  holds by construction and needs no flag.
  - **Replace with:** nothing.
  - **Data/migration:** `DropColumn(HasManualEdits, Songs)`, which EF performs as a SQLite table rebuild. Remove
    `SongConfiguration.cs:48`, `Song.cs:14`, the DTO/projection fields, and the `hasManualEdits` parameter of
    `ISongService.UpdateSongAsync`. That signature change touches `SongFormViewModel.cs:606`,
    `SongResolutionService.cs:240-243,264-267`, and the tests.
  - **Specs superseded:** AC-4.1–4.4, INV-4, AC-11.4, AC-11.5 (and AC-5.7 for the artist), REQ-FORMUX-31.
    AC-BUG008-05 is already superseded.
  - **Tests to delete:** the four `SongResolutionServiceTests` above, `SongFormViewModelTests.cs:707`, and the setup at `:787-796`.

### 3.4 `Artist.HasManualEdits`
- **Writer:** `ArtistService.UpdateArtistAsync:107` (always `true` on edit).
- **Readers:** list projections `ArtistRepository.cs:51,74` → `ArtistListItem(Dto)`. No XAML binds it, and no merge
  or warning sheet exists for artists. AC-5.7 (`artists-songs/requirements.md:169`) was never implemented.
- **Tests:** none that check behaviour.
- **Recommendation: DROP**, together with 3.3, in the same migration. It supersedes AC-4.7 and AC-5.7.

### 3.5 Merge sheet: `FieldDiff`, `MergeFieldRow(s)`, `ConfirmMergeCommand`, `DismissMergeSheetCommand`
- **Code:**
  - `Domain/Resolution/ResolutionResults.cs:8-11`
  - `SongResolutionService.ComputeFieldDiffs` `:353-370`, `MergeableFields` `:14-15`
  - VM `:88-89`, `:716-729`, `:798-844`
  - XAML `SongFormPage.xaml:353-410`
- **Existing defects:**
  - Unchecking every field passes `acceptedFields = null` (`:816`). For a manually edited song, `ApplyUpdate` then
    applies nothing, but `UpdateSongAsync` still writes `UpdatedAt` and the external id. That contradicts AC-4.3
    ("no write occurs").
  - The comment at `:346` says the opposite of what the code does.
- **Tests:** `SongResolutionServiceTests.cs:100,271,343`; `SongFormViewModelTests.cs:707`.
- **Recommendation: DROP.** It exists only because of 3.3.

### 3.6 Resolution sheet: `ResolutionCandidates`, `SongMatch`, "Update existing", "Save as new version"
- **Code:**
  - VM state `:81-86`; commands `:147-151`; flow `:638-665`, `:694-798`
  - XAML `SongFormPage.xaml:274-345`
  - Service: `ResolveAsync` plus `CommitAsync(UpdateExisting)`
- **Specs:** AC-1.1–1.4, AC-2.1–2.3.
- **Tests:** `SongFormViewModelTests.cs:174,208,238,265,1132`; `SongResolutionServiceTests` (all ResolveAsync tests).
- **Recommendation: SIMPLIFY.**
  - Keep a **save-time duplicate guard**: typed titles can still collide, and the DB unique index
    `(ArtistId, Title, Version)` will reject them anyway.
  - Keep "**Save as new version**", because Version is part of the identity.
  - Replace "**Update existing**" with "**Open existing**", i.e. navigate to edit. Today it overwrites the stored
    song with the typed values (`ApplyUpdate` `:333-345`), which the user never saw.
  - Fuzzy candidates at Save become largely redundant once the title autocomplete is typo-tolerant. Keep them only as
    a "possible duplicates" list with an Open action.
  - Delete from `SongResolutionService.CommitAsync` the choices the UI never uses (CreateNew, CreateNewVersion,
    AttachExternalId, `:192-223`, `:253-275`). Delete `ArtistResolutionService.CommitAsync` as well. Both are
    reachable only from tests, including the `NestedUnitOfWorkTests` 3-level chain, which is the UoW pilot's showcase
    for REQ-UOW-22. **Check with Helder whether that test is worth keeping for UoW reasons alone.**

### 3.7 `IsArtistLocked`
- **Current logic:**
  - Locked on a pick or inline-create (`LockArtist` `:327-337`)
  - Locked in edit-mode hydration (`:423`, `:477`)
  - Locked on an API-pick exact match (`:526`)
  - Unlocked by the X icon (`ClearArtist` `:385-397`, REQ-ACREATE-15/BUG-081)
  - Read by `SongFormPage.xaml.cs:70` to suppress the "create new" sentinel
  - Tests: `SongFormViewModelTests.cs` (15 references)
- **The BUG-008 rule** (`bugs/2026-06-01-BUG-008…/BUG-008-songform-artist-autocomplete.md:148,199,222`) was
  superseded by REQ-FORMUX-16 and REQ-ACREATE-14. It no longer depends on `HasManualEdits` or `ExternalId`.
- **Recommendation: KEEP** as pure UI pick-lock state; it is unaffected by dropping the fields. Fix the stale
  docstring at `SongFormViewModel.cs:433` and the vestigial test setup at `:787-796`.
- **Note:** a DRAFT change spec is in progress for the same interaction:
  `DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`.
  A title autocomplete should consume that shared component, not fork it.

---

## 4. Title-first feasibility: what the redesign touches

### Existing SongFormViewModel flows affected
1. **Song picker navigation.**
   - Code: `NavigateToSongPickerCommand` `:138,899-902`, `OnSongPicked` `:488-504`, `ResolveAndLockArtistAsync`
     `:506-545`, `SongPickerPage/ViewModel`, `Routes.SongPicker`, `AppShell.xaml.cs:32`,
     `MauiProgram.cs:130`, `SongPickedMessage`.
   - REQ-FORMUX-26/27 already plan to delete all of these; a title autocomplete replaces them.
2. **Artist lock and inline create.**
   - Code: `LockArtist`, `CreateArtistInlineAsync` `:339-356`, `ClearArtist`, blur rules `:363-379`, the create
     sentinel in `SongFormPage.xaml.cs:55-75`.
   - Picking a title fills and locks the artist, or marks it "to create". Creating the artist must move to **Save**,
     inside the same unit of work as the song, per REQ-FORMUX-20/23 ("SHALL NOT persist anything before Save").
     Today inline-create writes the artist immediately (`:341`).
3. **Resolution and merge sheets:** see §3.5–3.6.
4. **Edit-mode hydration:** `LoadSongForEditAsync` `:435-484`. "Pick an existing song → open edit" needs a
   navigation of the form `GoToAsync($"{Routes.SongForm}?songId={id}")`, the same shape as `SongsViewModel.cs:110`.
   From inside the SongForm itself, that means replacing the current page, not stacking a second form.
5. **Save split:** `ExecuteEditSaveAsync` `:597-621` bypasses resolution; `ExecuteNewSongSaveAsync` `:623-665` goes
   through `ResolveAsync`.

### SongService and Catalog touch points
- **`SongService.CreateSongWithUrlsAsync`** (`:193-…`) and **`UpdateSongAsync`** (`:111-190`) are the only write paths.
  - Neither writes `Catalog` rows. The `Catalog` entity is `(ArtistId, SongId)` (`Domain/Entity/Catalog.cs`);
    `CatalogRepository.cs:25-49` reads it.
  - CATALOG-PROP (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`, 📋 Spec) will hook exactly
    these two methods, plus the artist change in `UpdateSongAsync` (`:139-161`). A title-first "create artist + song"
    at Save must go through the same methods, or catalog propagation will be bypassed.
  - If `ArtistResolutionService.CommitAsync` survives, it is a second artist-creation path that CATALOG-PROP must
    also account for.
- **Inconsistency to flag:** `UpdateSongAsync` checks uniqueness with `ExistsByTitleForArtistAsync` (title only,
  `:153`), while create uses title+version (`:207`). Editing a "Live" variant whose canonical title exists is rejected.
- **`hasManualEdits` parameter:** removed from `UpdateSongAsync` if §3.3 is adopted.

### What the title autocomplete needs that doesn't exist yet
1. **DI registration** of `ISongSuggestionService` (and `IArtistSuggestionService`). Fix the provider HttpClient
   registration first (§0.1): `AddHttpClient<IMusicMetadataProvider, MusicBrainzProvider>()` style, or resolve the typed clients.
2. **Typo-tolerant local retrieval.** `GetPagedAsync` is prefix-only. It needs a new repository/service method (see §2
   options) that returns `LocalId + Title + Version + ArtistName`.
3. **A remote query shape that tolerates typos.** Use Deezer plain `q=` and/or reorder or merge providers for title
   search. With first-non-empty-wins, MusicBrainz shadows Deezer.
4. **A "find existing local song for this API pick" service method.** `GetByExternalIdAsync` exists only at repository
   level and inside `ResolveAsync`. There is no service method that returns a song id by (a) `(Provider, ExternalId)`
   or (b) collated `(artist name, title, version)`. `ExistsByTitleVersionForArtistAsync` returns only a bool, and
   `SongSuggestionService` dedup tier (b) is not artist-scoped. That tier would wrongly hide a remote "Don't Cry"
   by another artist when any local song is titled "Don't Cry".
5. **Artist "resolve-or-mark-for-create" by name at pick time**, with creation deferred to Save in one unit of work.
6. **An autocomplete row model** that can carry `LocalId` / `IsRemote` / artist and route a pick to either "open edit"
   or "fill form". The shared exposed-dropdown component under draft spec is the natural host.
7. **Spec work:** un-park `form-ux-redesign`, or open a new dated change. REQ-FORMUX-25 ("local title pick fills
   Title only") **contradicts** the new "local pick → open in edit mode" and must be superseded. REQ-FORMUX-24 ("run
   resolution unchanged") and REQ-FORMUX-31 (HasManualEdits) fall if §3.3 and §3.5 are adopted.

---

## Suggested simplification (opinion)

- **Keep:** `Song.ExternalId/ExternalProvider` (composite index), `SimilarityScorer` (it already scores the target
  typos at 0.88–0.94), `IsArtistLocked` as UI state, "Save as new version", the DB unique index.
- **Drop:** `Song.HasManualEdits`, `Artist.HasManualEdits`, `FieldDiff`, the merge sheet, `ApplyUpdate`'s
  partial-merge branch, the unused `ResolutionChoice` branches, `ArtistResolutionService.CommitAsync`, and the Song Picker page.
- **Simplify:** the resolution sheet becomes "This song exists → Open it / Save as new version". Artist resolution
  becomes a collated name lookup plus create at Save. Leave `Artist.ExternalId` in place but don't build on it.
- **Fix first, independent of the redesign:**
  1. The provider HttpClient DI defect.
  2. The Deezer `track:"…"` query shape.
  3. The song-id passed as the artist external id (`SongFormViewModel.cs:631-634`).
  4. The null-id `ExactLocalMatch` for accent-only differences.
- **For typo tolerance:** start with the token-prefix pool (≤ 50) plus FuzzySharp rerank, which fits AC-5.2 as
  written, together with Deezer plain `q=` remotely. Move to FTS5 trigram only if the library grows beyond what a
  managed-UDF full scan handles comfortably. Record the INV-5 wording conflict (C# normalisation inside the scorer)
  as a spec amendment rather than leave it contradictory.

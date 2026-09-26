# Song Form Title-First (one search over local and music APIs) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Plan status:** written 2026-09-26 against the spec **APPROVED 2026-09-26** under Helder's pre-authorisation of 2026-09-25 (`requirements.md` header). Every open question takes its recorded recommendation (Q1 drop MusicBrainz, Q2 discard sheet yes, Q4 "Save as new song" yes, Q5 full `title`, Q6 X on Artist clears identity, Q7 3,000 ms, Q8 `TokenSortRatio`, Q10 do not store the Deezer artist id, Q11 port 4 tests + delete 1, Q16 see § 0.3 D1). `tasks.md` is the authoritative task list; this plan turns it into executable steps and does not redesign. Every place where the plan sequences or splits a task differently from `tasks.md` is listed in § 0.3 with its reason.

**Goal:** On the Song form, Title comes first and, in add mode, is one search over the local library (FTS5 trigram, typo-tolerant) and Deezer. A local pick opens the song; a Deezer pick fills Title and Artist. Every Save decision lives in `SongService.SaveFromFormAsync`. The Artist form's Name gets the same local + Deezer search. `HasManualEdits`, the merge sheet, inline artist create and both picker pages are removed.

**Architecture:** DRY Onion for additions (Domain → Infra → Services → UI); removals run outside-in so every commit builds. Three strictly serial migrations (M1 drop `HasManualEdits`, M2 unique `(ExternalProvider, ExternalId)` with dedupe, M3 FTS5 `SongSearch` + `ArtistSearch` + 7 triggers). The Song form and Artist form consume the shared `AutocompleteDXField` (dependency item) through `IMenuOptionSource`; the ViewModels map service outcomes to BottomSheets and decide nothing.

**Tech Stack:** .NET MAUI 10, C#, CommunityToolkit.Mvvm, EF Core 10 + SQLite (SQLitePCLRaw bundle, FTS5 trigram), FuzzySharp (existing `SimilarityScorer`), DevExpress MAUI 25.2.x, xUnit + Moq. **Versions come from `Directory.Packages.props` / the `.csproj`, never from this file.**

**Spec (read all of them, in this order):** `README.md` (seven decision rounds), `findings.md`, `requirements.md`, `design.md`, `tasks.md`, all in this folder:
`Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/`

**Dependency spec:** `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/` (`design.md` § 3.1 component surface, § 3.2 `IMenuOptionSource`).

---

## Global Constraints

Every task implicitly includes these. Values are copied from the spec or the project rules.

- **Business logic in Services only** (CLAUDE.md, unamendable). ViewModels map outcomes to sheets and navigate; every Save rule is in `SongService.SaveFromFormAsync` (REQ-SFT-51).
- **One write path** (REQ-SFT-46, INV-SFT-1): song creates/updates from the Song form go only through `ISongService.CreateSongWithUrlsAsync` / `ISongService.UpdateSongAsync`. `SaveFromFormAsync` itself writes nothing.
- **Never two artists with the same name** under `NOCASE_NOACCENT` (REQ-SFT-52, INV-SFT-6). An exact collated match is reused silently; no C# string comparison decides a match.
- **No C#-side normalisation for search/dedup** (`constraints-registry.md § EF Core / SQLite`, HARD RULE). The only exception is `design.md` § 6.4 (filed in `.claude/exception-registry.md` at Step 0): `ISimilarityScorer.RankScore` may **order** a DB-retrieved list bounded by `SimilarityConstants.PoolSize` (50); it never filters, deduplicates or tests uniqueness. Code locations: `SongSuggestionService` (title search) and `ArtistSuggestionService` (Artist-form local name search, § 0.3 D20). Collated matches use `EF.Functions.Collate(..., CollationConstants.Default)`.
- **Unit-of-work read scope** (REQ-UOW-36/37/50, `MyVocaList.Tests/Architecture/UnitOfWorkReadScopeTests.cs`): every repository and every governed data service (`IArtistService`, `ISongService`, `IArtistResolutionService`, `ISongResolutionService`, `ISongKaraokeUrlService`) used by a service is resolved from the `ExecuteAsync`/`ExecuteReadAsync` lambda's own `sp`, never dereferenced from a constructor field. No `ExecuteReadAsync` lambda may reference `IMusicMetadataProvider`/`_providers` (no HTTP inside a read scope). The architecture floors stay met: ≥ 25 files under `Services/`, ≥ 15 governed constructor parameters.
- **Service return pattern:** tuples `(bool success, string message, T? entity)`; `SongFormSaveOutcome` is the one designed exception (`design.md` § 3.5). No exceptions for business failures.
- **Constants, never literals:** `SearchConstants.MinimumLocalQueryLength` (2), `SearchConstants.MinimumRemoteQueryLength` (3), `SearchConstants.AutocompleteDebounceMilliseconds` (300), `SearchConstants.MaxSearchResults` (5), `SimilarityConstants.PoolSize` (50), `SimilarityConstants.DefaultThreshold` (0.82). The 3,000 ms remote bound (REQ-SFT-08) becomes a named constant `SearchConstants.RemoteSearchTimeoutMilliseconds = 3000`, added in Task 2.2 (it is a value from the spec, not a new decision).
- **Exact UI strings** (English only): "In your library", "Deezer", "Online search unavailable", "This music-database song is already in your library", "Similar artist found", "Use '<existing>'", "Create '<typed>'", "Open '<title>'? What you entered here will be discarded.", "Open", "Keep editing", "Open existing", "Save as new version", "Save as new song", "Artist is required". Option supporting text: local = `"artist · version · In your library"` (the ` · version` part omitted when Version is empty); remote = `"artist · Deezer"`.
- **Deezer only** (Q1 = drop, REQ-SFT-36). Plain query `search/track?q=` / `search/artist?q=` with `Uri.EscapeDataString`, no `track:`/`artist:` qualifiers (REQ-SFT-35). Title = the provider's full `title` (Q5 default). The Deezer artist id is **not** stored (Q10, REQ-SFT-49).
- **No native dialogs** (`DisplayAlert` etc.): `dx:BottomSheet` only. DevExpress first. `SafeAreaEdges="Container"` on pages. **Incremental XAML:** edit ONE XAML file → build → fix → next file.
- **Navigation from ViewModels** only through `INavigationService` (never `Shell.Current` in new paths; `testing.md`). Open an existing song: `$"../{Routes.SongForm}?songId={id}"`; open an existing artist: `$"../{Routes.ArtistForm}?artistId={id}&artistName={Uri.EscapeDataString(name)}"`.
- **Tests:** every test covering an AC starts with `// [AC] REQ-SFT-NN: <criterion>`, one AC per test. Repository FTS tests and migration tests use a real SQLite temp file via `TestDbContextFactory.Create()` **plus `await db.Database.MigrateAsync()`** (never `EnsureCreated` for FTS; never the in-memory provider; never a mocked `DbContext`). Service tests use Moq.
- **Red commits** (`.claude/exception-registry.md`, row 2026-09-18): test files only, trailer `Lane: RED`, committed with `--no-verify`; the Green lands in the next commit; both outputs are pasted verbatim into the task-log. A Red that is a compile error records the `CSxxxx` lines verbatim. Testers **never** commit production stubs.
- **Builder never edits a test** (`testing.md`). A test that looks wrong → `blocked: spec gap`, stop.
- **Superseded tests are never silently deleted:** every deletion or rewrite is listed in its task below with the AC it supersedes and the replacing REQ (`design.md` § 10.1), and recorded in the task-log.
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` (`/sln-build`). **Tests:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`. 3-attempt build cap, then `Build failure` and stop.
- **Censuses and "zero hits" checks use a Python `os.walk` substring scan, never `grep`** (the rtk proxy drops matches; memory `rtk-rewrites-grep`). Exclude `bin`, `obj`, `.git`, `.worktrees`; exclude `Migrations/` only where the task says so (Designer files keep historical mentions).
- **MCPs:** Context7 pinned to the `.csproj` versions (EF Core 10.x, DevExpress 25.2.x, MAUI 10.x); `devexpress-maui` MCP for DX example code; if a required MCP is unavailable: `blocked: MCP unavailable`, do not guess.

## Review Focus

The five input classes or failure modes the spec implies but no AC-level test pins, most likely to bite first. Each is assigned to the task that owns the code, with its pinning test named in that task.

1. **FTS5 query syntax in user text.** A typed `"`, `*`, `:`, `(`, `-`, `/`, `NEAR` or `AND` must never raise an FTS5 syntax error or change the query's meaning; the user just gets results (or none). Every trigram window is double-quoted with `"` doubled. **Owner: Task 3.9** (`SearchTitleCandidatesAsync_FtsSyntaxCharacters_DoNotThrow`) and **Task 3.9b** (`SearchNameCandidatesAsync_FtsSyntaxCharacters_DoNotThrow`).
2. **Padded or differently-cased typed artist.** `"  métallica "` typed with "Metallica" stored must reuse the stored artist, never create a second one or ask. **Owner: Task 3.10** (`SaveFromForm_TypedArtistPaddedCaseAccentVariant_ReusesExistingArtist`).
3. **M1's SQLite table rebuild loses an index or a collation.** After `DropColumn` rebuilds `Songs` and `Artists`, the unique collated `IX_Artists_Name` and the unique `(ArtistId, Title, Version)` song index must still exist and still reject duplicates. **Owner: Task 3.6b** (`M1_TableRebuild_KeepsUniqueCollatedIndexes`).
4. **A stale remote failure sets the note after a newer success.** A slow, failing Deezer call for an old query must not write "Online search unavailable" after a newer query succeeded. **Owner: Task 5.2** (`TitleOptionSource_CancelledQuery_DoesNotChangeTitleSupportingText`).
5. **Double-tap Save while an outcome is pending.** Two taps must produce one `SaveFromFormAsync` call and at most one write. **Owner: Task 5.3** (`Save_DoubleTapWhilePending_CallsServiceOnce`).

---

## 0. Orchestration

### 0.1 Wave structure (one worktree + task branch per task)

Every code task gets its **own** worktree `.worktrees/sft-<task>` on branch `task/sft-<task>-<slug>`, created from `develop` **only after its dependencies are merged**. Tester and Builder of one task run in sequence on one branch. Migration tasks have an orchestrator scaffold step between Part A and Part B (§ 0.3 D11). Wave cap: at most **4** subagents in flight, **counting post-merge Verifiers** (§ 0.5): a wave of 3 implementors leaves room for one Verifier; with 4 implementors in flight, the Verifier of a finished merge waits.

| Wave | Tasks (parallel within a wave) | Depends on (merged) | Model |
|------|-------------------------------|---------------------|-------|
| **Step 0** | Orchestrator doc prep on `develop` (§ 0.4) | — | orchestrator |
| **W1** | **G5a** FTS5 spike build (throwaway) · **1.1** · **1.2** · **2.1** | Step 0 | Sonnet · Sonnet · Sonnet · Sonnet |
| **STOP G5** *(runs in parallel with W2…W11)* | **G5b** Helder on device · **G5c** orchestrator records verdict | G5a | Helder / orchestrator |
| **W2** | **1.3** · **1.7** · **2.2** · **3.1-Tester** | 1.3 ← 1.1+1.2 · 1.7 ← 1.1 · 3.1 ← 2.1 + G3 | Haiku · Sonnet · Sonnet · Sonnet |
| **W3** | **1.4** · **1.5** · **3.1-Builder** · **5.1** | 1.4/1.5 ← 1.3 · 5.1 ← 1.7 | Haiku · Haiku · **Opus** · Sonnet |
| **W4** | **1.6** · **3.2a** · **3.3** | 1.6 ← 1.4+1.5 · 3.2a ← 3.1 · 3.3 ← 3.1+5.1 | Haiku · Sonnet · Sonnet |
| **W5** | **4.1a** · **3.3b** · **3.4** | 4.1a ← 1.6 + G1 · 3.3b ← 3.3 · 3.4 ← 3.3 | Sonnet · Haiku · Sonnet |
| **W6** | **3.2b** · **4.1b** | 3.2b ← 3.2a+3.3b · 4.1b ← 4.1a | Sonnet · Haiku |
| **W7** | **3.2c** | 3.2b | Sonnet |
| **W8** | **3.5a** · **3.5b** | 3.2c + 3.3b | Haiku · Haiku |
| **W9** | **3.6a** | 3.5a+3.5b | Haiku |
| **W10** | **3.6b** M1 (Part A → scaffold → Part B) | 3.6a + G3 + § 0.2.2 check | Sonnet → orchestrator → Sonnet |
| **W11** | **3.7** M2 (Part A → scaffold → Part B) | 3.6b + G3 + § 0.2.2 check | Sonnet → orchestrator → Sonnet |
| **HARD STOP** | **G5 verdict must read `PASS`** before W12 | G5c | — |
| **W12** | **3.8** M3 (Part A → scaffold → Part B) | 3.7 + G5 PASS + G3 | Sonnet → orchestrator → **Opus** |
| **W13** | **3.9** | 3.8 | Sonnet |
| **W14** | **3.9b** · **4.2-Tester** → **4.2-Builder** | 3.9b ← 3.9 · 4.2 ← 2.2+3.9+4.1b | Sonnet · Sonnet → Sonnet |
| **W15** | **3.9c** · **4.3** | 3.9c ← 3.9b+3.4 · 4.3 ← 4.2 | Sonnet · Sonnet |
| **W16** | **3.10-Tester** → **3.10-Builder** · **5.2-Tester** → **5.2-Builder** | 3.10 ← 3.2c+3.3b+3.9c + G3 · 5.2 ← 4.3+3.5b | Sonnet → **Opus** · Sonnet → Sonnet |
| **W17** | **5.3-Tester** → **5.3-Builder** · **6.1-Tester** → **6.1-Builder** | 5.3 ← 5.2+3.10 · 6.1 ← 4.1b+4.3+3.9b | Sonnet → Sonnet · Sonnet → Sonnet |
| **W18** | **5.3b** · **5.4** · **6.2** | 5.3b ← 5.3 + G3 · 5.4 ← 5.3 · 6.2 ← 6.1+1.2 | Haiku · Sonnet · Sonnet |
| **GATE G2** | **ACF T4 (`AutocompleteDXField`) merged into `develop`** | ACF plan | — |
| **W19** | **5.5** (+ former 5.7) · **6.3** | 5.4 + ACF T4 · 6.2 + ACF T4 | Sonnet · Sonnet |
| **W20** | **5.6** | 5.5 | Sonnet |
| **7.1** | Spec ritual + traceability (orchestrator) | all merged | orchestrator |
| **STOP E2E** | **7.2** Helder on-device E2E gate | 7.1 (recommended: ACF T7 passed) | Helder |

**What runs before the component's T4 lands:** everything from W1 to W18: all removals (Phase 1), all Domain/Infra (Phase 2, migrations, FTS queries), all Services (Phase 3, 4, 6.1) and all ViewModel tasks (5.1–5.4, 6.2). ViewModel tasks need only `IMenuOptionSource`/`MenuOptionSource` and `AutocompleteSuggestion`, which ACF T3 already merged (`83065e07`) (§ 0.3 D2). **What waits for ACF T4:** the page tasks 5.5 (with the former 5.7), 5.6 and 6.3, then 7.1 and 7.2.

**Merge order into `develop`:** the wave order above; within a wave, any order, one at a time. Each merge is `git merge --no-ff <branch>` from the main working tree on `develop`, followed by § 0.5 **before** the next merge. The G5 spike branch is **never merged**.

### 0.2 File-overlap check (single-writer)

#### 0.2.1 Hotspot files and their writers (serialised by the wave table)

| File | Writers, in order | Note |
|------|-------------------|------|
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | 1.1 → 1.7 → 5.1 → 3.2b → 5.2 → 5.3 → 5.4 | single-writer chain |
| `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` | 1.1 → 1.7 → 5.1 → 3.3b → 3.2b → 3.5b → 5.2 → 5.3 → 5.4 | single-writer chain |
| `Services/SongService.cs`, `Domain/ServicesInterfaces/ISongService.cs` | 3.1 → 3.2a → 3.2b → 3.2c → 3.10 → 5.3b | **G3: CATALOG-PROP rule** (§ 0.2.2) |
| `Infra/Migrations/*`, `Infra/EntityEFConfig/SongConfiguration.cs`, `ArtistConfiguration.cs` | 3.6b → 3.7 → 3.8 | sequential-only; § 0.2.2 |
| `MyVocaList/MauiProgram.cs`, `MyVocaList/AppShell.xaml.cs`, `MyVocaList/Navigation/Routes.cs` | 1.3 only | sequential-only |
| `MyVocaList/Extensions/ServiceCollectionExtensions.cs` | 1.6 → 4.1b → 4.3 → 6.1 | serial |
| `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` | 5.1 → 4.3 | serial |
| `Infra/Repository/SongRepository.cs`, `Domain/RepositoryInterface/ISongRepository.cs` | 2.1 → 3.5a (repo only) → 3.9 | serial |
| `Infra/Repository/ArtistRepository.cs`, `Domain/RepositoryInterface/IArtistRepository.cs` | 3.5b (repo only) → 3.9b → 3.9c | serial |
| `Services/ArtistResolutionService.cs` + its tests | 3.4 → 3.9c | serial |
| `Services/SongResolutionService.cs` + `SongResolutionServiceTests.cs` | 3.3 → 3.3b (→ 3.2c tests only if the census hits) | serial |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | 1.1 → 5.1 → 5.5 → 5.6 | serial, one XAML file per task |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` | 1.7 → 5.5 | serial |
| `ArtistFormViewModel.cs` + `ArtistFormViewModelTests.cs` | 1.2 → 6.2 | serial |
| `ArtistFormPage.xaml(.cs)` | 1.2 → 6.3 | serial |
| `SongSuggestionServiceTests.cs` | 3.5a → 4.2 | serial |
| `ArtistSuggestionServiceTests.cs` | 3.5b → 6.1 | serial |
| `Infra/Repository/FtsQueryBuilder.cs` (new) | 3.9 (create) → 3.9b (consume only) | 3.9b after 3.9 (§ 0.3 D9) |
| Docs: `tasks.md`, `task-log.md` headers, `findings.md`, `MyVocaList.sln`, `LEDGER.md`, `README.md` frontmatter, `BACKLOG.md`, `.claude/exception-registry.md`, `.claude/library/constraints-reference.md` | **orchestrator only, on `develop`** | implementors write only their own pre-created `task-log.md` section in their worktree copy |

Within every wave of § 0.1, no two tasks share a file (checked row by row against the table above and each task's **Files** block). The orchestrator re-runs this check before each dispatch against the task's actual `Files` block.

#### 0.2.2 Cross-item rule (G3): CATALOG-PROP and BUG-080

**Rule `[HARD]`: never dispatch a task that touches `Services/SongService.cs`, `Domain/ServicesInterfaces/ISongService.cs` or anything under `Infra/Migrations/` while a CATALOG-PROP branch is live.** Before dispatching 3.1, 3.2a, 3.2b, 3.2c, 3.10, 5.3b, 3.6b, 3.7 or 3.8, the orchestrator runs **three** checks and records their output: (1) `Docs/Management/LEDGER.md`, any row whose branch/worktree column is non-empty; (2) `git worktree list`; (3) `git branch -a` (a branch or worktree whose name contains `catalog`, `catalog-prop`, `bug-080` or `venue` counts as live until proven otherwise). It checks for:

- **CATALOG-PROP** (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`): it writes `SongService.cs` and adds a backfill migration. If live → do not dispatch; wait for its merge (or ask Helder to pause it).
- **BUG-080** (`venues/bugs/2026-09-21-BUG-080-…`): its fix may add a Venues-only migration. It rebuilds neither `Songs` nor `Artists`, so it cannot drop the FTS triggers (`design.md` § 7), but it **does** change the model snapshot. For the migration tasks 3.6b, 3.7 and 3.8 only: if a BUG-080 branch with a migration is live → do not dispatch; wait for its merge.
- **Any other row** whose `Files owned` include `SongService.cs` or `Migrations/` (e.g. a READ-SCOPE follow-up) → same treatment.

**Snapshot rule** (`design.md` § 7): every migration is scaffolded in a worktree created from the **current** `develop`, so it is generated against the merged model snapshot. Whichever item merges second regenerates its migration against the merged snapshot (never only a later timestamp). If CATALOG-PROP or BUG-080 merges a migration while one of M1–M3 is in flight, the in-flight task's Part A worktree is discarded and re-created from the new `develop`, and its scaffold is re-run.

Record the three checks (LEDGER rows read, `git worktree list`, `git branch -a`, result) in the task's `### Checkpoint` block before dispatch.

**Two-way rule.** The same constraint is written on the other side at Step 0 (item 0.8): the CATALOG-PROP and BUG-080 LEDGER rows, and this item's `tasks.md` Coordination section, carry the note "Do not dispatch a task touching `SongService.cs`/`ISongService.cs`/`Infra/Migrations/` while a `task/sft-*` branch owning those files (3.1, 3.2a–c, 3.10, 5.3b, 3.6b, 3.7, 3.8) is live; check LEDGER + `git worktree list` + `git branch -a`".

### 0.3 Sequencing deviations from `tasks.md` (not redesign)

Each item is recorded **on `develop` by the orchestrator at Step 0** (§ 0.4 item 0.7) as a `> **Spec updated 2026-09-26:**` note in `tasks.md` (the spec is not shipped, so in-place notes are allowed). No implementor edits `tasks.md` or `design.md`.

- **D1 (Q16, REQ-SFT-07) — SETTLED 2026-09-26.** The approved Q16 recommendation applies: local options wait up to 3,000 ms for Deezer. The `AutocompleteDXField` component does **not** gain an append capability in this pipeline (orchestrator decision under Helder's pre-authorisation of 2026-09-25; recorded as resolved in `requirements.md` REQ-SFT-07 and § Open questions at Step 0.7). `IMenuOptionSource.SearchAsync` returns one list: `TitleOptionSource` (and `NameOptionSource`) await the local search, then the remote search (bounded by REQ-SFT-08's 3,000 ms), and return one list (local, then promoted, then remote).
- **D2 (VM tasks before G2).** `tasks.md` puts Phase 5 "after G2". ViewModel tasks 5.1–5.4 and 6.2 consume only `IMenuOptionSource`/`MenuOptionSource`/`AutocompleteSuggestion` (ACF T3, merged `83065e07`), not the component, so they run before ACF T4. Only the page tasks 5.5, 5.6 and 6.3 wait for ACF T4. If ACF T1c escalates to option (ii)/(iii) and that changes `IMenuOptionSource`, the orchestrator re-plans 5.2, 5.3 and 6.2 before dispatching them.
- **D3 (3.1 keeps a forwarding overload).** Changing `CreateSongWithUrlsAsync(int artistId, …)` in place breaks 3 VM-test mocks, 2 `Bug068RegressionTests` calls and the `SongResolutionService` caller, none of which 3.1 owns. 3.1 therefore **adds** the new overload `(int? artistId, string? newArtistName, …)` and keeps the old one, `[Obsolete]`, forwarding with `newArtistName: null`. 3.1 no longer touches `SongFormViewModel.cs` (the VM keeps calling the old overload until 5.3 replaces the call with `SaveFromFormAsync`). New task **5.3b** deletes the old overload after 5.3.
- **D4 (3.3 split into 3.3 + 3.3b).** Removing `FieldDiffs`/`TargetHasManualEdits` from the `SongResolution` record breaks 4 positional constructions in `SongFormViewModelTests.cs` (`:189, :218, :248, :1150`). 3.3 simplifies the service and keeps the record shape (passing `[]`/`false`); **3.3b** shrinks the record, deletes `FieldDiff`, and updates the constructions in `SongResolutionService.cs`, `SongResolutionServiceTests.cs` and `SongFormViewModelTests.cs`.
- **D5 (3.5 split into 3.5a + 3.5b).** Census 2026-09-26: `CatalogRepository.cs:41-48` also projects `SongListItemDto` with `HasManualEdits`, and positional constructions exist in `SongSuggestionServiceTests.cs:60`, `ArtistSuggestionServiceTests.cs:50` and `SongFormViewModelTests.cs:424,448,491,1177,1179,1220`. 3.5 as written (6 files, no test files) cannot build. **3.5a** = song side, **3.5b** = artist side (§ Phase 3).
- **D6 (`:787` rewrite moves from 5.4 to 3.2b).** `SongFormViewModelTests.cs:796` sets `HasManualEdits = false` on a `Song`; M1 (3.6b) removes that property, so the rewrite must be merged before 3.6b. 3.2b already owns the VM test file for the `hasManualEdits` removal.
- **D7 (3.4 path).** `ResolutionChoice` is declared in `Domain/Resolution/ResolutionEnums.cs`, not in a `ResolutionChoice.cs` file. 3.4 removes the enum from that file (and deletes the file only if it becomes empty).
- **D8 (4.1 split into 4.1a + 4.1b; 4.1a after 1.6).** Deleting `MusicBrainzProvider.cs` (Q1) also needs its two registrations in `ServiceCollectionExtensions.cs:34,48`, and the new `MusicProviderException` is a new file, which puts 4.1 at 6 files. **4.1a** = Deezer contract + exception + provider tests; **4.1b** = MusicBrainz deletion + DI lines. 4.1a waits for 1.6 so the dead picker path (`MusicMetadataService`) is gone before providers start throwing.
- **D9 (3.9b after 3.9).** Both repositories use one FTS match builder (`design.md` § 6.2 "the same builder"). 3.9 creates `Infra/Repository/FtsQueryBuilder.cs`; 3.9b consumes it, so it runs after 3.9 instead of `[P]`.
- **D10 (3.6b sizing).** 3.6b owns 6 files (the 4 entity/config edits are one-line deletions, plus `Migrations/*` and the new `SongMigrationTests.cs`). Splitting would leave a non-building commit (entity property removed while its configuration still maps it). Accepted and recorded.
- **D11 (migration scaffold by the orchestrator).** `workflow-rule-2.md` assigns `dotnet ef migrations add` to the main agent, and EF 10 fails `MigrateAsync` on pending model changes, so model edits and the scaffold must land in one commit. Each migration task is: **Part A** (implementor: Red tests committed `Lane: RED`; model/config edits left **uncommitted**, build green, handback) → **scaffold** (orchestrator, in the worktree: `dotnet ef migrations add <Name> -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj`; the design-time factory is `Infra/AppDbContextFactory.cs`) → **Part B** (implementor: **first** checks the generated `Up`/`Down` against the task's expected operations and stops with `blocked: stale snapshot` if anything else appears; then the hand-written migration body where the task says so, Green, exit checklist, one commit with model + migration). The orchestrator runs the scaffold command only and never opens the generated `.cs` files (orchestrator read-scope HARD RULE).
- **D12 (4.3 owns `SongSuggestionService.cs` too).** Removing `GetLocalAsync`/`GetRemoteAsync` from the interface also removes their implementation (5 files).
- **D13 (5.1 owns `AppServicesRegistrationTests.cs`).** Adding `INavigationService` to `SongFormViewModel`'s constructor breaks `AddAppServices_ResolvingSongFormViewModelGraph_Succeeds` (it mocks the MAUI-only services; `INavigationService` is registered in `MauiProgram.cs:107`). 5.1 adds `services.AddSingleton(Mock.Of<INavigationService>());` to that test's setup (a mechanical, Level-C edit; no AC changes).
- **D14 (Red commits).** Testers commit test files only (`Lane: RED`, `--no-verify`, per the registry row). Unlike ACF T3, no production skeleton is committed with the Red; a missing member is a compile-error Red.
- **D15 (header and gates).** `tasks.md` still says "DRAFT — not dispatchable". Note: approved 2026-09-26; G1 done (BUG-082 `fa520e3f`); G4 resolved by the pre-authorisation (recommendations); G2 = ACF T4 merged (page tasks only, D2); G3 = § 0.2.2; G5 = STOP G5.
- **D16 (`SearchConstants.RemoteSearchTimeoutMilliseconds`).** REQ-SFT-08's 3,000 ms is added as a named constant in **2.2** (Global Constraints "no literals"), consumed by 4.2 and 6.1. 2.2 owns 5 files; 4.2 stays at 5.
- **D17 (5.7 folded into 5.5).** Replacing the Artist `AutoCompleteEdit` in XAML breaks the build until the code-behind glue that references it is deleted, so a XAML-only 5.5 commit cannot build. 5.5 owns `SongFormPage.xaml` + `.xaml.cs` (XAML → build → code-behind, as ACF T6 did); 5.7 is cancelled as folded.
- **D18 (`ArtistRemoteSearchResult` created in 4.1b).** The new one-line record `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs` is created in 4.1b (3 files), so 6.1 stays at 5 files. D10 is the only accepted sizing break.
- **D20 (Artist-form local search uses the FTS pool) — spec gap resolved 2026-09-26.** `design.md` § 4.1 said the Artist form's local search keeps the collated prefix query (`ArtistRepository.SearchByNameAsync`, `:59-68`), which cannot find "Metallica" for "metalica" and so cannot satisfy E2E line 11 or REQ-SFT-48's dedup of the Deezer row. Orchestrator decision (under the pre-authorisation): `SearchNameLocalAsync` uses `IArtistRepository.SearchNameCandidatesAsync` (3.9b, FTS5 `ArtistSearch`) ranked by `ISimilarityScorer.RankScore`, take 5. 6.1 depends on 3.9b. The § 6.4 exception's code location is extended to `Services/ArtistSuggestionService.cs` (ranking only). Recorded in `requirements.md` REQ-SFT-47 (with an example) and `design.md` § 4.1/§ 6.4 at Step 0.7.
- **D19 (`ArtistOptionSource` in 5.2).** `design.md` § 4 lists `ArtistOptionSource` as an existing/changed member, but today the Artist search is page glue (`OnArtistItemsRequested`). 5.2 creates `ArtistOptionSource` (`IMenuOptionSource` over the existing local artist search, local only, REQ-SFT-16) so 5.5 can bind it.

### 0.4 Step 0: orchestrator prep on `develop` (docs only, before W1)

- [ ] **0.1** Session-start protocol (Rule 7) and hook health check (`.claude/settings.json` is valid JSON; `.worktrees/` is git-ignored: `git check-ignore -q .worktrees`). Record the baseline: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` on `develop` → write the pass count into `task-log.md` § Baseline (679 at `744ea7d6` per the ACF task-log; re-measure).
- [ ] **0.2** Status: `python .claude/scripts/backlog/backlog_gen.py status song-form-title-first-one-search-over-local-and-music-apis "🟢"` (spec approved, plan ready); update the README `gate:` to "Helder's on-device E2E gate (plan § 7.2)". Move to 🟡 at W1 dispatch. Run `regen`; inspect the diff (`workflow-rule-2.md` § Generated artifacts).
- [ ] **0.3** Create `task-log.md` in this folder with pre-created sections, one per task, each holding only a `### Checkpoint` placeholder: `## Baseline`, `## G5 — FTS5 spike`, `## 1.1` … `## 1.7`, `## 2.1`, `## 2.2`, `## 3.1`, `## 3.2a`, `## 3.3`, `## 3.3b`, `## 3.4`, `## 3.2b`, `## 3.2c`, `## 3.5a`, `## 3.5b`, `## 3.6a`, `## 3.6b`, `## 3.7`, `## 3.8`, `## 3.9`, `## 3.9b`, `## 3.9c`, `## 3.10`, `## 4.1a`, `## 4.1b`, `## 4.2`, `## 4.3`, `## 5.1` … `## 5.4`, `## 5.3b`, `## 5.5`, `## 5.6`, `## 6.1` … `## 6.3`, `## 7.1`, `## 7.2 — Device gate`.
- [ ] **0.4** Append to `findings.md` a section `## G5 — FTS5 trigram on Android (spike, 2026-09-26)` holding: an empty `### Environment` (device model, Android version, app commit), the **G5 run sheet** (§ G5b below, verbatim, empty Result cells), and an empty `### Verdict`.
- [ ] **0.5** Register `task-log.md` in `MyVocaList.sln` with the `sln_add.py` helper used for `plan.md` (`findings.md` is already registered).
- [ ] **0.6** File the `design.md` § 6.4 exception row in `.claude/exception-registry.md` verbatim, with the code location extended to `Services/ArtistSuggestionService.cs` (ranking step of `SearchNameLocalAsync`, D20) (required **before 2.2** starts; Review lane Elevated).
- [ ] **0.7** Record **every** § 0.3 deviation, **D1 through D20 (including D17–D20)**, in `tasks.md` as `> **Spec updated 2026-09-26:**` notes at the affected tasks; mark 5.7 `[CANCELLED: folded into 5.5]` (D17); add the new task entries 3.3b, 3.5a/3.5b, 4.1a/4.1b, 5.3b in `tasks.md` format (`workflow-rule-4.md`: Produces/Consumes/Risk/Files owned/Demo/Review lane) copied from this plan; add "after 3.9b" to 6.1 (D20). (The spec-level notes for D1 and D20 in `requirements.md`/`design.md` were written 2026-09-26 with this plan revision; check they are present.)
- [ ] **0.7b** ACF spec note: in `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/design.md` at `:384` ("… `IsReadOnly` (an API-sourced song's Title in edit mode) …"), add `> **Spec updated 2026-09-26:** the song-form item's seventh-round ruling reverses this: in edit mode Title is Editable with `IsSearchEnabled = false` for every song; the song form binds no `IsReadOnly`. `IsReadOnly` stays in the component for future use.`
- [ ] **0.8** LEDGER rows for W1 (`/sln-ledger`): branch, worktree, phase, status. G3 two-way (§ 0.2.2): note in this item's row "G3: no SongService/Migrations task while CATALOG-PROP or a BUG-080 migration branch is live"; add the mirror note to the **CATALOG-PROP** row and the **BUG-080** row (add a BUG-080 row if none exists); add the same two-way note to `tasks.md` § Coordination.
- [ ] **0.9** Commit on `develop`: `docs(sft): plan prep — task-log skeleton, G5 run sheet, deviation notes, §6.4 exception row`.
- [ ] **0.10** Create **only the W1 worktrees** (`sft-g5-spike` on `spike/sft-g5-fts5`, `sft-1.1`, `sft-1.2`, `sft-2.1`) with `git worktree add .worktrees/<name> -b <branch> develop`, then verify `git -C .worktrees/<name> merge-base --is-ancestor develop HEAD`. Every later worktree is created only after its dependencies are merged, and its `git log --oneline -5` must show the dependency's merge commit.

### 0.5 Post-merge verification (after EVERY merge into develop)

Run from the main working tree on `develop`:

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android     # expect: Build succeeded, 0 Error(s)
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj              # expect: Failed: 0; Passed = previous + new − deleted (task-log states the delta)
```

After a **migration** merge (3.6b, 3.7, 3.8) also run:

```
dotnet ef migrations has-pending-changes -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj --exit-code   # expect exit 0
dotnet ef database update -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj --connection "Data Source=.claude/MyVocaList.db"   # keeps the SQLite MCP copy current
```

Then:

- Review the merged task's `task-log.md` section: `### Changed files`, Verification evidence (Red + Green output verbatim for Level A/B), the AC matrix for user-facing tasks, and the **superseded-test table** (deleted/rewritten test → superseded AC → replacing REQ) for every task that deletes or rewrites a test.
- Sync its docs to develop (`/sln-docs-sync`). Tick the task's box in `tasks.md`. Update the LEDGER.
- Dispatch a **Verifier** subagent (Sonnet) on the merge diff (spec-vs-implementation, AC coverage, Global Constraints, the task's census if any).
- Remove the worktree: `git worktree remove .worktrees/<name>`; delete the merged branch.

**Known flaky test (BUG-076).** If the **only** failure is the known-flaky SQLite test that throws `ObjectDisposedException`, re-run `dotnet test` **once**. Record both runs (the failing test name and the pass/fail counts of each) in the merged task's `task-log.md` section.
- The re-run is fully green → continue.
- It fails again, or any **other** test fails in either run → treat it as red (below).

If the build or the tests go red: **do not merge the next branch.** Roll back with `git revert -m 1 <merge-sha>`, then diagnose on the task branch (`superpowers:systematic-debugging`). For a migration merge, also run `dotnet ef database update <previous-migration-name> … --connection "Data Source=.claude/MyVocaList.db"` to roll the MCP copy back (see each migration's rollback note).

### 0.6 Implementor briefing template (every task)

Each briefing contains:

- **Role scope:** files owned (from the task's **Files** block), files off-limits (`tasks.md`, `MyVocaList.sln`, `LEDGER.md`, `BACKLOG.md`, any `README.md` frontmatter, `.claude/**`, every file not owned). Tester vs Builder role where split.
- **Permitted MCPs:** Context7 (version-pinned) always; `devexpress-maui` for 5.5, 5.6, 6.3.
- **Skills to invoke first:** `myvocalist-coding` (always), `maui-current-apis` (UI/VM tasks), `dotnet-skills:efcore-patterns` (Infra/migration tasks), `maui-unit-testing` (VM tests).
- The worktree path. The implementor edits **the worktree's copies**, never the main repo paths (memory `worktree-edit-path-discipline`).
- The task section of this plan, the Global Constraints, and the spec sections the task cites.
- The census command, when the task has one (Python `os.walk`, never grep).
- The checkpoint duty: the `### Checkpoint` block in its own `task-log.md` section (`session-ops.md` § Checkpoint Ping): write-ahead before each step, after each build/test, and at least every ~10 minutes.
- The **exit checklist** (`workflow-rule-2.md`): verification-before-completion → build 0 errors → test → post-edit re-read → (no `.sln` work; the orchestrator owns it) → living-spec check (`> **Spec updated YYYY-MM-DD:**` notes are requested from the orchestrator in the handback, never written) → task-log (`### Changed files`, evidence, AC matrix, superseded-test table) → commit → `git push origin HEAD` (attempt and verify; memory `subagent-cannot-push`). **No `tasks.md` tick.**

### 0.7 Commands used by every task

```
# focused test run
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~<ClassName>"
# full suite
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
# build
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
# Red commit (test files only)
git add <test files> && git commit --no-verify -m "test(sft): <task> Level-A tests (Red)" -m "Lane: RED"
```

Census template (substitute `SYMBOLS`; add `'Migrations'` to `SKIP` where the task says so):

```bash
python -c "import os,sys; SYMBOLS=['X','Y']; SKIP={'.git','bin','obj','.worktrees','Docs','.claude'}; hits=[]
for r,d,f in os.walk('.'):
    d[:]=[x for x in d if x not in SKIP]
    for n in f:
        if n.endswith(('.cs','.xaml')):
            p=os.path.join(r,n); t=open(p,encoding='utf-8',errors='ignore').read()
            hits+=[(p,s) for s in SYMBOLS if s in t]
print('\n'.join(f'{p} {s}' for p,s in hits) or 'ZERO HITS'); sys.exit(1 if hits else 0)"
```

---

## Gate G5: FTS5 trigram on Android (spike), HARD STOP for Helder

**Split:** G5a = an implementor builds a throwaway probe page (Sonnet, ≤ 45 min). G5b = Helder runs it on his Android device (spec time-box: 1 h). G5c = the orchestrator records the output and verdict in `findings.md` on `develop`.

> **HARD STOP.** Task 3.8 (M3) and everything after it that uses FTS (3.9, 3.9b, 3.9c, 4.2's local search, 3.10's near-duplicate retrieval) does not start until **(1)** Helder has run G5b and **(2)** the orchestrator has written `G5: PASS` in `findings.md` § G5 Verdict. W2–W11 continue during the wait. **On FAIL:** stop and escalate to Helder with the spec's named fallback (`design.md` § 6.1: token-prefix pool + `RankScore` rerank, no M3; changes 3.8, 3.9, 3.9b, 3.9c). The orchestrator **picks nothing**: a FAIL means a `requirements.md`/`design.md` spec update (reviewed), then a re-plan of 3.8–3.9c, before any of them is dispatched.

**Files (spike branch `spike/sft-g5-fts5` only, never merged, never pushed):**
- Create: `MyVocaList/UI/Pages/Spike/SftFtsSpikePage.xaml`, `MyVocaList/UI/Pages/Spike/SftFtsSpikePage.xaml.cs`
- Modify (temporary entry point): `MyVocaList/AppShell.xaml`: add `<FlyoutItem Route="sftftsspike" FlyoutItemIsVisible="False"><ShellContent ContentTemplate="{DataTemplate spike:SftFtsSpikePage}" /></FlyoutItem>` next to the other hidden `FlyoutItem`s, plus `xmlns:spike="clr-namespace:MyVocaList.UI.Pages.Spike"`.
- Modify (temporary entry point): `MyVocaList/Navigation/NavigationConfig.cs`: add `new MenuItemDescription("FTS5 Spike", "close_outlined", "sftftsspike", navigateCommand),` at the end of the list.

`AppShell.xaml` is sequential-only, but the branch is never merged, so no product conflict. The branch is deleted after G5c: `git worktree remove --force .worktrees/sft-g5-spike` then `git branch -D spike/sft-g5-fts5`. **No file from this branch reaches `develop`.**

### G5a: build the probe page (implementor, Sonnet)

- [ ] **Step 1.** Invoke `myvocalist-coding`. Context7 (EF Core 10): `DatabaseFacade.GetDbConnection`, `DbConnection.CreateCommand`. The page resolves `IDbContextFactory<AppDbContext>` (registered by `AddDbContextFactory` in `MauiProgram.cs:66`) from `IPlatformApplication.Current!.Services`, so the probe runs **through the app's own SQLite connection** (same native library, same `CollationInterceptor`).
- [ ] **Step 2.** Build `SftFtsSpikePage.xaml` (`SafeAreaEdges="Container"`, `dx:DXScrollView`): one `dx:DXButton` "Run FTS5 probe", one `dx:DXButton` "Copy results" (`Clipboard.Default.SetTextAsync`), and a `Label` (monospace, selectable if available) showing the results. The code-behind runs these statements **in order on one opened connection**, each in its own try/catch that records `EXCEPTION <type>: <message>` and continues, and appends one line per statement: `P<n> | <result>`:

```sql
-- P1 (expect >= 3.45.0: trigram remove_diacritics needs it)
SELECT sqlite_version();
-- P2 (expect 1)
SELECT sqlite_compileoption_used('ENABLE_FTS5');
-- P3 (expect: no exception)
CREATE VIRTUAL TABLE temp.sft_probe USING fts5(title, artist, tokenize='trigram remove_diacritics 1');
-- P4 (expect 3 rows inserted)
INSERT INTO temp.sft_probe(rowid, title, artist) VALUES (1,'Não Sei','Ana Carolina'),(2,'Don''t Cry','Guns N'' Roses'),(3,'Cry','Cyndi Lauper');
-- P5 (expect exactly: 1)
SELECT group_concat(rowid) FROM temp.sft_probe WHERE sft_probe MATCH '"nao"';
-- P6 (informational; expect it to contain 2)
SELECT group_concat(rowid) FROM (SELECT rowid FROM temp.sft_probe WHERE sft_probe MATCH '"don" OR "cry"' ORDER BY bm25(sft_probe, 10.0, 1.0));
-- P7 (cleanup; expect: no exception)
DROP TABLE temp.sft_probe;
```

The first line of the output is `device: <DeviceInfo.Manufacturer> <DeviceInfo.Model>, Android <DeviceInfo.VersionString>`. Also log each line with Serilog at Information (`SftFtsSpike`). The probe uses the `temp` schema only: the user's database is never modified.
- [ ] **Step 3.** Add the temporary `AppShell.xaml` and `NavigationConfig.cs` entries. Build after each file → 0 errors.
- [ ] **Step 4.** Emulator smoke: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run`; flyout → "FTS5 Spike" → Run. Record the emulator output (it is indicative only; the gate is the device). If no emulator: note "not run".
- [ ] **Step 5.** Commit locally: `spike: SFT G5 FTS5 probe page (never merge)`. **Do not push.**
- [ ] **Step 6: Handback, not a worktree file.** Return in the handback message: branch + local SHA, build result, emulator output (P1–P7 or "not run"), Context7 result. Status `Check build`. Stop.

### G5b: Helder runs the probe on device (HARD STOP)

**Deploy:** from `.worktrees/sft-g5-spike`, device connected over USB: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run` (or open that worktree's `MyVocaList.sln` in VS and deploy). Open the flyout, tap **FTS5 Spike**, tap **Run FTS5 probe**, then **Copy results** and paste the text to the orchestrator (or a screenshot of the label).

**Run sheet (copied into `findings.md` at Step 0; the orchestrator fills Result from Helder's paste):**

| # | Check | Pass iff | Result |
|---|-------|----------|--------|
| G5-1 | P1 `sqlite_version()` | ≥ 3.45.0 | |
| G5-2 | P2 `ENABLE_FTS5` | `1` | |
| G5-3 | P3 create trigram table with `remove_diacritics 1` | no exception | |
| G5-4 | P5 `"nao"` matches "Não Sei" | exactly `1` | |
| G5-5 | P6 `"don" OR "cry"` (informational) | contains `2` | |
| G5-6 | P7 drop | no exception | |

**Verdict rule:** G5-2, G5-3 and G5-4 pass → `G5: PASS` (G5-1 explains a G5-3 failure; G5-5 is informational). Otherwise → `G5: FAIL — escalated`.

### G5c: orchestrator records the verdict (docs only, on develop)

- [ ] Write the G5a handback and Helder's pasted output verbatim into `findings.md` § G5 (Environment, run sheet Results) and the `## G5 — FTS5 spike` section of `task-log.md`. Add the verdict line: `G5: PASS (Helder YYYY-MM-DD, <device>)` or `G5: FAIL — escalated YYYY-MM-DD`.
- [ ] Commit on develop: `docs(sft): G5 FTS5 spike findings — verdict <PASS|FAIL>`. Delete the spike worktree and branch. Update the LEDGER.
- [ ] On FAIL: `blocked: spike failed` in the LEDGER; present the `design.md` § 6.1 fallback to Helder; pick nothing; 3.8–3.9c are not dispatched until the spec is updated and re-planned.

---

## Phase 1: Removal of picker entry points and inline create (UI, outside-in)

### Task 1.1: Remove the Song-picker entry from the Song form [W1]

**Model:** Sonnet. **Level:** C (removal; census + build recorded). **Review lane:** Standard. **Estimate:** 45 min.

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (delete the "Search music database" row, `:122-137`)
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (delete `NavigateToSongPickerCommand` + its method `:899-902`, `OnSongPicked`, `ResolveAndLockArtistAsync`, the `SongPickedMessage` messenger registration `:156-158`)
- Modify: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`

**Interfaces:** Consumes nothing. Produces a `SongFormViewModel` with no reference to `SongPicker*`, `SongPickedMessage`, `ResolveAndLockArtistAsync`.

**Superseded tests:**

| Test (file:line) | Action | Superseded AC → replacing REQ |
|---|---|---|
| `SongFormViewModelTests.cs:486` `ResolveAndLockArtistAsync_*_ClosesDropdown` | delete | AC-11.2 / REQ-ACF-24 picker clause → REQ-SFT-12/13 (replacement `RemotePick_ClosesDropdowns`, Task 5.2) |
| `SongFormViewModelTests.cs:509` `ResolveAndLockArtistAsync_*_ClosesDropdown` | delete | same |
| `SongFormViewModelTests.cs:475` | edit: drop the `SongPickedMessage` use, keep the assertion | — |

- [ ] **Step 1.** Record the pre-change census (`SYMBOLS=['SongPicker','SongPickedMessage','ResolveAndLockArtistAsync','NavigateToSongPicker']`) in the task-log.
- [ ] **Step 2.** Edit `SongFormPage.xaml` only: delete the row. **Build** → the only errors may be missing command bindings (none expected, XAML bindings are runtime). 0 errors.
- [ ] **Step 3.** Edit the VM: delete the members listed. **Build** → errors only in `SongFormViewModelTests.cs`.
- [ ] **Step 4.** Edit the tests per the table. **Build** 0 errors; full suite `Failed: 0` (count = baseline − 2).
- [ ] **Step 5.** Census for `SongPickedMessage`, `ResolveAndLockArtistAsync`, `NavigateToSongPicker` restricted to `SongFormPage.xaml`, `SongFormViewModel.cs`, `SongFormViewModelTests.cs` → zero hits (the picker files themselves go in 1.4).
- [ ] **Step 6.** Exit checklist. Commit: `refactor(song-form): remove Song-picker entry point (REQ-SFT-44)`.

**Demo:** New Song shows no "Search music database" row. **Merge risk / rollback:** Low (dead path, findings § 0.1). `git revert -m 1 <merge-sha>`.

### Task 1.2: Remove the Artist-picker entry from the Artist form [W1, P with 1.1]

**Model:** Sonnet. **Level:** C. **Review lane:** Standard. **Estimate:** 45 min.

**Files:**
- Modify: `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` (delete the "Search music database" row)
- Modify: `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` (delete `NavigateToArtistPickerCommand` and the `ArtistPickedMessage` handling; **keep** the `SelectedExternalId`/`SelectedProvider` stash, which 6.2 deletes)
- Modify: `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`

**Superseded tests:** none. Census 2026-09-26 (Python scan of `ArtistFormViewModelTests.cs` for `Picker`, `Picked`, `SelectedExternalId`, `SelectedProvider`): **zero hits**, so no test in that file exercises the picker entry point. The test file is edited only if the VM change breaks its build (e.g. a constructor argument); any such edit is mechanical and listed in the task-log. If the implementor's census finds a hit, it lists the test by name and line with AC-ART-01…06 → REQ-SFT-44 before deleting it.

- [ ] **Step 1.** Pre-change census (`['ArtistPicker','ArtistPickedMessage','NavigateToArtistPicker']`) into the task-log.
- [ ] **Step 2.** XAML edit → build. **Step 3.** VM edit → build. **Step 4.** Tests → build 0 errors, suite `Failed: 0`.
- [ ] **Step 5.** Census restricted to the 3 owned files → zero hits. Exit checklist. Commit: `refactor(artist-form): remove Artist-picker entry point (REQ-SFT-44)`.

**Demo:** New Artist shows no "Search music database" row. **Merge risk:** Low.

### Task 1.3: Unregister both picker pages [W2, SEQUENTIAL]

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 20 min. **Sequential-only files.**

**Files:**
- Modify: `MyVocaList/Navigation/Routes.cs` (delete `ArtistPicker` `:19`, `SongPicker` `:20`)
- Modify: `MyVocaList/AppShell.xaml.cs` (delete both `Routing.RegisterRoute` lines, `:32` + the Artist-picker line)
- Modify: `MyVocaList/MauiProgram.cs` (delete the picker page + VM registrations, `:130` + the Artist-picker lines)

- [ ] **Step 1.** Delete the lines, one file at a time, build after each → 0 errors (the picker classes still exist; nothing else references the routes after 1.1/1.2).
- [ ] **Step 2.** Census `['Routes.SongPicker','Routes.ArtistPicker']` → zero hits. Full suite `Failed: 0`.
- [ ] **Step 3.** Emulator smoke (`-t:Run`): Songs → New Song and Artists → New Artist open normally. Exit checklist. Commit: `refactor(nav): unregister Song and Artist picker routes and DI (REQ-SFT-44)`.

**Merge risk:** Low.

### Task 1.4: Delete the Song picker [W3, P with 1.5]

**Model:** Haiku. **Level:** C. **Estimate:** 20 min.

**Files (delete):** `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`, `SongPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`, `Contracts/Messages/SongPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`

**Superseded tests:** `SongPickerViewModelTests.cs`, all 12 → delete; AC-SONG-01…06, AC-LOAD-* (song picker), AC-TRIGGER-01/02 → REQ-SFT-44: `SearchCommand_EmptyText_DoesNotCallService`, `SearchCommand_SetsIsLoadingBeforeAwait`, `SearchCommand_ClearsPriorResults`, `SearchCommand_OnSuccess_PopulatesResults`, `SearchCommand_OnEmptyResult_SetsHasSearchedNoResults`, `SearchCommand_OnException_SetsHasSearchedNoResults`, `SelectResultCommand_SendsSongPickedMessage`, `SelectResultCommand_CallsNavigationGoBack`, `BackCommand_CallsNavigationGoBack`, `IsShowEmptyState_OnEmptyResult_IsTrue`, `IsShowEmptyState_OnSuccess_IsFalse`, `SearchCommand_SongTitleNull_DoesNotThrow`.

**Demo:** census finds no `SongPicker`/`SongPickedMessage` reference; build 0 errors. **Review lane:** Standard.

- [ ] Delete the 5 files → build 0 errors → full suite `Failed: 0` (count − 12) → census `['SongPicker','SongPickedMessage']` → **ZERO HITS**. Exit checklist. Commit: `refactor(songs): delete SongPickerPage, VM, message and tests (REQ-SFT-44)`.

### Task 1.5: Delete the Artist picker [W3, P with 1.4]

**Model:** Haiku. **Level:** C. **Estimate:** 20 min.

**Files (delete):** `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`, `ArtistPickerPage.xaml.cs`, `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`, `Contracts/Messages/ArtistPickedMessage.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs`

**Superseded tests:** `ArtistPickerViewModelTests.cs`, all 11 → delete; AC-ART-01…06 → REQ-SFT-44: `SearchCommand_EmptyText_DoesNotCallService`, `SearchCommand_SetsIsLoadingBeforeAwait`, `SearchCommand_ClearsPriorResults`, `SearchCommand_OnSuccess_PopulatesResults`, `SearchCommand_OnEmptyResult_SetsHasSearchedNoResults`, `SearchCommand_OnException_SetsHasSearchedNoResults`, `SelectResultCommand_SendsArtistPickedMessage`, `SelectResultCommand_CallsNavigationGoBack`, `BackCommand_CallsNavigationGoBack`, `IsShowEmptyState_OnEmptyResult_IsTrue`, `IsShowEmptyState_OnSuccess_IsFalse`.

**Demo:** census finds no `ArtistPicker`/`ArtistPickedMessage` reference; build 0 errors. **Review lane:** Standard.

- [ ] Delete → build → suite → census `['ArtistPicker','ArtistPickedMessage']` → **ZERO HITS**. Commit: `refactor(artists): delete ArtistPickerPage, VM, message and tests (REQ-SFT-44)`.

`YouTubeSearchPage`, its VM, route and the "Search YouTube (API)" row are **untouched** (README fifth round).

### Task 1.6: Delete `IMusicMetadataService` [W4, after 1.4 + 1.5]

**Model:** Haiku. **Level:** C. **Estimate:** 20 min.

**Files:** Delete `Services/IMusicMetadataService.cs`, `Services/MusicMetadataService.cs`. Modify `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (delete `:57` `AddScoped<IMusicMetadataService, MusicMetadataService>()`).

- [ ] **Step 1.** Census `['IMusicMetadataService','MusicMetadataService']` **before** deleting: hits only in the 3 owned files. Otherwise stop (`blocked: consumer found`).
- [ ] **Step 2.** Delete + edit → build → full suite `Failed: 0`, including `UnitOfWorkReadScopeTests` (`ServicesDirectory_IsFoundAndNonEmpty` needs ≥ 25 files under `Services/`: record the new count) and `AppServicesRegistrationTests`.
- [ ] **Step 3.** Census → ZERO HITS. Commit: `refactor(services): delete IMusicMetadataService (picker-only consumer) (REQ-SFT-44)`.

**Demo:** build 0 errors; `AppServicesRegistrationTests` green. **Review lane:** Standard.

### Task 1.7: Remove inline artist create [W2, after 1.1]

**Model:** Sonnet (single agent, Red first). **Level:** A for the blur rule. **Review lane:** Standard. **Estimate:** 60 min.

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (delete `CreateArtistInlineCommand`, its method `:347-361`, the create sentinel; blur keeps text and sets no error)
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (delete the create-row glue)
- Modify: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`

**Superseded tests:**

| Test (file:line) | Action | Superseded AC → replacing REQ |
|---|---|---|
| `:287` `ArtistBlurredWithoutSelection_…_RetainsTextAndSetsError` | rewrite → `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsNoError` | REQ-ACREATE-03 error clause → REQ-SFT-17 |
| `:334` `…SetsErrorText` | delete | REQ-ACREATE-03 error clause → REQ-SFT-17 |
| `:550` `CreateArtistInline_*` | delete | REQ-ACREATE-04/05 → REQ-SFT-45/18/20 |
| `:570` `CreateArtistInline_*` | delete | same |
| `:317`, `:532` (restore prior selection) | **keep unchanged** | REQ-ACF-17/24 carry-over (`design.md` § 10.1) |

- [ ] **Step 1: Rewrite `:287` (Red).**

```csharp
[Fact]
// [AC] REQ-SFT-17: when Artist loses focus holding unpicked text, the text stays and no error is shown.
public void ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsNoError()
{
    var sut = CreateSut();
    sut.ArtistSearchText = "Banda Local";

    sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

    Assert.Equal("Banda Local", sut.ArtistSearchText);
    Assert.False(sut.ArtistHasError);          // use the VM's real error members (read the existing test)
    Assert.True(string.IsNullOrEmpty(sut.ArtistErrorText));
}
```

- [ ] **Step 2: Run, confirm Red.** Filter `SongFormViewModelTests.ArtistBlurredWithoutSelection_NoPriorSelection`. Expected: `Assert.False() Failure` (today the error "Search and select an artist from the list" is set). Commit the test file only with `Lane: RED`.
- [ ] **Step 3: Green.** Remove the error assignment from the blur handler; delete the inline-create members (VM), the glue (code-behind), and the tests `:334`, `:550`, `:570`. Build after each file.
- [ ] **Step 4.** Filter → Pass; full suite `Failed: 0`; census `['CreateArtistInline','Search and select an artist from the list']` → ZERO HITS in `.cs`/`.xaml`.
- [ ] **Step 5.** Exit checklist (AC matrix row REQ-SFT-17; superseded-test table). Commit: `feat(song-form): remove inline artist create; blur keeps text with no error (REQ-SFT-17/45)`.

**Risk:** Medium. Until 5.3, the Save guard still requires a picked artist, so unpicked text is refused at Save (interim, not shipped). **Rollback:** `git revert -m 1 <merge-sha>`.

---

## Phase 2: Domain + Infra additions

### Task 2.1: Song lookup queries for duplicates and identity [W1]

**Model:** Sonnet. **Level:** B (test-first). **Review lane:** Standard. **Estimate:** 60 min.

**Files:**
- Modify: `Domain/RepositoryInterface/ISongRepository.cs`
- Modify: `Infra/Repository/SongRepository.cs`
- Modify: `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs` (append a `// ── Lookups (SFT) ──` region; `EnsureCreated` is fine here: no FTS)

**Interfaces — Produces (`design.md` § 3.1, verbatim):**

```csharp
/// REQ-SFT-42: the id of the collated (artist, title, version) match, or null.
Task<int?> FindIdByTitleVersionForArtistAsync(int artistId, string title, string version, int? excludeId, CancellationToken ct);
/// REQ-SFT-26: true when another song holds this external identity.
Task<bool> ExistsByExternalIdentityAsync(string provider, string externalId, int? excludeId, CancellationToken ct);
```

Both compare with `EF.Functions.Collate(..., CollationConstants.Default)` in SQL (title/version) and plain equality for provider/id; **no** C# re-filter (Global Constraints).

- [ ] **Step 1: Write the failing tests.**

| Test | `// [AC]` | Asserts |
|---|---|---|
| `FindIdByTitleVersionForArtistAsync_AccentOnlyDifference_ReturnsStoredId` | REQ-SFT-42 | "Nao Sei" for Ana Carolina returns the id of stored "Não Sei" |
| `FindIdByTitleVersionForArtistAsync_DifferentVersion_ReturnsNull` | REQ-SFT-29 | "Creep"/"Acoustic" when only ""/"Live" exist → null |
| `FindIdByTitleVersionForArtistAsync_ExcludeSelf_ReturnsNull` | REQ-SFT-29 | the only match is `excludeId` → null |
| `ExistsByExternalIdentityAsync_HeldByAnotherSong_ReturnsTrue` | REQ-SFT-26 | `("Deezer","3135556")` held by song 1, excludeId null → true |
| `ExistsByExternalIdentityAsync_HeldOnlyBySelf_ReturnsFalse` | REQ-SFT-26 | excludeId = the holder → false |
| `ExistsByExternalIdentityAsync_Unknown_ReturnsFalse` | REQ-SFT-26 | false |

- [ ] **Step 2: Red.** Filter `SongRepositoryTests`. Expected: build failure `CS1061: 'ISongRepository' does not contain a definition for 'FindIdByTitleVersionForArtistAsync'` (and `ExistsByExternalIdentityAsync`). Commit tests only, `Lane: RED`.
- [ ] **Step 3: Green.** Implement both (Context7 EF Core 10: `EF.Functions.Collate`, `AnyAsync`, `Select(s => (int?)s.Id).FirstOrDefaultAsync`). Filter → all pass; full suite `Failed: 0`.
- [ ] **Step 4.** Build 0 errors. Exit checklist (AC matrix). Commit: `feat(song-repo): collated title/version lookup and external-identity check (REQ-SFT-26/29/42)`.

**Merge risk:** Low (additive).

### Task 2.2: `ISimilarityScorer.RankScore` [W2]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** **Elevated** (§ 6.4 exception; the orchestrator filed the registry row at Step 0.6). **Estimate:** 45 min.

**Files:**
- Modify: `Domain/ServicesInterfaces/ISimilarityScorer.cs`
- Modify: `Infra/Similarity/SimilarityScorer.cs`
- Modify: `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs` (append; existing `Score` tests unchanged)
- Modify: `Domain/Constants/SearchConstants.cs` (append `public const int RemoteSearchTimeoutMilliseconds = 3000;` with an XML doc citing REQ-SFT-08; § 0.3 D16)
- Modify: `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` (append one test)

**Interfaces — Produces:**

```csharp
/// Display-order score in [0,1] for a bounded, DB-retrieved candidate list (design § 6.3, exception § 6.4).
/// Never used to filter, deduplicate or test uniqueness.
double RankScore(string query, string candidate);
// Implementation: Fuzz.TokenSortRatio(Normalize(query), Normalize(candidate)) / 100.0 — same Normalize as Score (SimilarityScorer.cs:30-40).
```

- [ ] **Step 1: Write the failing tests** (scores from `design.md` § 6.3; assert **order**, not exact values, except the range).

| Test | `// [AC]` | Asserts |
|---|---|---|
| `RankScore_DontCry_RanksDontCryAboveCry` | REQ-SFT-30 | `RankScore("dont cry","Don't Cry") > RankScore("dont cry","Cry")` |
| `RankScore_DonCry_RanksDontCryAboveCry` | REQ-SFT-30 | same with "don cry" |
| `RankScore_DontCry_RanksDontCryAboveBoysDontCry` | REQ-SFT-30 | `> RankScore("dont cry","Boys Don't Cry")` |
| `RankScore_GunsCry_TitleArtistOfDontCryAboveCryByCyndiLauper` | REQ-SFT-32 | `RankScore("guns cry","Don't Cry Guns N' Roses") > RankScore("guns cry","Cry Cyndi Lauper")` |
| `RankScore_AccentAndCaseVariant_ScoresOne` (Theory `"nao sei"`, `"NAO SEI"`) | REQ-SFT-31 | `RankScore(q,"Não Sei") == 1.0` |
| `RankScore_Range_IsBetweenZeroAndOne` (Theory over 4 pairs incl. `""`) | REQ-SFT-30 | `0 <= s && s <= 1` |
| `RemoteSearchTimeoutMilliseconds_Is3000` (in `SearchConstantsTests`) | REQ-SFT-08 | `Assert.Equal(3000, SearchConstants.RemoteSearchTimeoutMilliseconds)` |

- [ ] **Step 2: Red.** Filter `SimilarityScorerTests`. Expected `CS1061 … 'SimilarityScorer' does not contain a definition for 'RankScore'` and `CS0117 … 'SearchConstants' does not contain a definition for 'RemoteSearchTimeoutMilliseconds'`. Commit tests only, `Lane: RED`.
- [ ] **Step 3: Green.** Implement; XML doc cites the § 6.4 exception and "ordering only". Filter → pass; the existing `Score` tests still pass unchanged; full suite `Failed: 0`.
- [ ] **Step 4.** Exit checklist. Commit: `feat(similarity): RankScore (TokenSortRatio) for display ordering only (REQ-SFT-30..32, exception §6.4)`.

**Merge risk:** Low (additive).

---

## Phase 3: Services (write path), resolution simplification, schema

### Task 3.1: `SongService`: create a typed artist inside the song's unit of work [W2 Tester, W3 Builder; G3]

**Model:** Tester = Sonnet; Builder = **Opus** (UoW atomicity, ambient join, `FlushAsync`, architecture-test compliance). **Level:** A. **Review lane:** Architectural. **Estimate:** 45 min + 90 min.

**Pre-dispatch:** § 0.2.2 LEDGER check (CATALOG-PROP).

**Files:**
- Modify: `Domain/ServicesInterfaces/ISongService.cs`
- Modify: `Services/SongService.cs`
- Modify: `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (the 10 existing `CreateSongWithUrlsAsync` calls move to the new overload)
- Create: `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (ports `NestedUnitOfWorkTests.cs:22,66,105,146`; the original file is deleted in 3.3)

**Interfaces — Produces (`design.md` § 3.2 + § 0.3 D3):**

```csharp
/// artistId XOR newArtistName: exactly one must be supplied, else (false, "Artist is required", null).
/// newArtistName: created in THIS unit of work via IArtistService.CreateArtistAsync resolved from sp
/// (ambient join), then IUnitOfWork.FlushAsync materialises its Id (REQ-UOW-35) before the song is staged.
/// Any later failure rolls both back (REQ-SFT-18). Rejects an external identity already held by another song (REQ-SFT-26).
Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
    int? artistId, string? newArtistName, string title, string version,
    string? featuredArtists, string? lyrics, string? externalId, string? externalProvider,
    IEnumerable<string> urls, CancellationToken ct = default);

[Obsolete("Use the (int? artistId, string? newArtistName, …) overload — REQ-SFT-18. Removed in task 5.3b.")]
Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
    int artistId, string title, string version, string? featuredArtists, string? lyrics,
    string? externalId, string? externalProvider, IEnumerable<string> urls, CancellationToken ct = default);
    // forwards: CreateSongWithUrlsAsync((int?)artistId, null, title, …)
```

Failure message for REQ-SFT-26: `"This music-database song is already in your library"` (checked via `ISongRepository.ExistsByExternalIdentityAsync` from 2.1, resolved from `sp`, **before** the artist is created).

#### W2: Tester (tests only, then stops)

- [ ] **Step 1.** Move the 10 existing `CreateSongWithUrlsAsync(artistId, …)` calls in `SongServiceTests.cs` to the new overload (`artistId, null, …`), same assertions. Write the new tests:

| Test | File | `// [AC]` | Asserts |
|---|---|---|---|
| `CreateSongWithUrlsAsync_NeitherArtistIdNorNewName_FailsArtistRequired` | SongServiceTests | REQ-SFT-21 | `(false, "Artist is required", null)`; no repository write |
| `CreateSongWithUrlsAsync_BothArtistIdAndNewName_FailsArtistRequired` | SongServiceTests | REQ-SFT-21 | same |
| `CreateSongWithUrlsAsync_ExternalIdentityHeldByAnotherSong_FailsWithMessage` | SongServiceTests | REQ-SFT-26 | message = "This music-database song is already in your library"; `AddAsync` never called |
| `CreateSongWithUrlsAsync_NewArtistName_PersistsOneArtistAndOneSong` | NewArtistUoW (port `:22`) | REQ-SFT-18 | 1 artist row + 1 song row, song.ArtistId = new artist id |
| `CreateSongWithUrlsAsync_ThrowAfterArtistStaged_RollsBackBoth` | NewArtistUoW (**rewritten** from `:66`, assertion inverted: the original asserted the 3-level `CommitAsync` path; this asserts 0/0 rows on the 2-level path; REQ-UOW-24 → REQ-SFT-18) | REQ-SFT-18 | 0 artists, 0 songs after the exception (same failure injection as the original) |
| `CreateSongWithUrlsAsync_FailureTupleAfterArtistStaged_WritesNothing` | NewArtistUoW (**rewritten** from `:105`, assertion inverted as above; REQ-UOW-24 → REQ-SFT-18) | REQ-SFT-18 | 0/0 rows (same injection as the original) |
| `CreateSongWithUrlsAsync_NewArtist_UsesOneAppDbContextAcrossTwoLevels` | NewArtistUoW (port `:146`, weakened to 2 levels) | REQ-SFT-18 | the `AppDbContext` seen by `ArtistService` and `SongService` is the same instance (REQ-UOW-22 re-homed; doc comment states "2 levels; no 3-level path remains") |
| `CreateSongWithUrlsAsync_NewArtistNameCollatedDuplicate_FailsAndWritesNothing` | NewArtistUoW | REQ-SFT-52 | "Metallica" stored; `newArtistName = "métallica"` → failure with the `IArtistService` duplicate message; artist count unchanged; 0 songs |
| `CreateSongWithUrlsAsync_NewArtistAndIdentityHeld_CreatesNoArtist` | NewArtistUoW | REQ-SFT-26 | identity held → failure; artist count unchanged |

- [ ] **Step 2: Red.** `dotnet test … --filter "FullyQualifiedName~SongServiceTests|FullyQualifiedName~SongServiceNewArtistUnitOfWorkTests"`. Expected: build failure `CS1501: No overload for method 'CreateSongWithUrlsAsync' takes 10 arguments` (and `CS1503` argument conversions). Record verbatim. Commit tests only, `Lane: RED`: `test(song-service): new-artist unit-of-work tests (Red) — REQ-SFT-18/21/26/52`. Push. Stop.

#### W3: Builder (same branch, fresh subagent, Opus)

- [ ] **Step 3.** Read the tests, `design.md` § 3.2/§ 5 F4, `NestedUnitOfWorkTests.cs` (for the injection mechanics), `code-style-reference.md § Service Return Patterns`, and the architecture test rules (Global Constraints). Implement the new overload: validate inputs → inside `IUnitOfWork.ExecuteAsync(sp => …)`: resolve `ISongRepository` and (when `newArtistName` is set) `IArtistService` from `sp`; check identity (REQ-SFT-26); create the artist (a failure tuple returns its message and skips the save, REQ-UOW-24); `await uow.FlushAsync(ct)`; stage the song with the new id; stage URLs; commit. Add the `[Obsolete]` forwarding overload. **Do not edit the tests.**
- [ ] **Step 4: Green.** Same filter → all pass. Full suite `Failed: 0` including `UnitOfWorkReadScopeTests` (no governed field dereferenced outside `sp`). Build: 0 errors; new `CS0618` warnings allowed only at the old-overload call sites (VM, `SongResolutionService`, `Bug068RegressionTests`); list them in the task-log.
- [ ] **Step 5.** Exit checklist (AC matrix: REQ-SFT-18, -21, -26, -52). Commit: `feat(song-service): create a typed artist inside the song's unit of work (REQ-SFT-18/26)`.

**Merge risk / rollback:** High (atomicity; CATALOG-PROP hooks this method). `git revert -m 1 <merge-sha>`; the old overload is untouched, so no caller is affected by a revert.

### Task 3.2a: `UpdateSongAsync`: Version-aware uniqueness and `newArtistName` [W4; G3]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** Elevated. **Estimate:** 75 min.

**Files:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`

**Interfaces — Produces:** `UpdateSongAsync(…, int? artistId = null, string? newArtistName = null, CancellationToken ct = default)`: `newArtistName` added as the last optional parameter before `ct` (no call site breaks). At most one of `artistId`/`newArtistName`; both null = keep the stored artist. Uniqueness = collated (effective artist, title, effective version) excluding self, via `FindIdByTitleVersionForArtistAsync` (2.1) (today title-only, `SongService.cs:155`). External identity: `ExistsByExternalIdentityAsync(…, excludeId: id)`. `hasManualEdits` stays for now (3.2b). **`SongService.cs:159-167` navigation detach must stay** (BUG-067/068).

- [ ] **Step 1: Write the failing tests one at a time** (write → Red → implement → Green):

| Test | `// [AC]` | Red reason today |
|---|---|---|
| `UpdateSongAsync_VersionChangedToNewVersionOfExistingTitle_Succeeds` | REQ-SFT-29 | Radiohead "Creep" ("") + "Creep" ("Live"); edit "Live" → "Acoustic" fails today (title-only check) |
| `UpdateSongAsync_VersionChangedToExistingVersion_FailsDuplicate` | REQ-SFT-29 | expected to pass once the check uses Version; assert the existing duplicate message |
| `UpdateSongAsync_TitleEditedOnApiSourcedSong_KeepsExternalIdentity` | REQ-SFT-27 | call with `externalId: null, externalProvider: null`; the stored `("Deezer","3135556")` is unchanged |
| `UpdateSongAsync_EditedTitleCollidesWithArtistTitleVersion_Rejected` | REQ-SFT-27 | "creep" (Version "") onto another Radiohead song → duplicate message |
| `UpdateSongAsync_NewArtistName_CreatesArtistInSameUnitOfWork` | REQ-SFT-28 | `CreateArtistAsync` (from `sp`) called; song gets the new id |
| `UpdateSongAsync_ExternalIdentityHeldByAnotherSong_Fails` | REQ-SFT-26 | message "This music-database song is already in your library" |

Also update the mocks at `SongServiceTests.cs:245` and `:480` to set up the new collated lookup (`FindIdByTitleVersionForArtistAsync`) instead of the title-only one (mechanical; record it).
- [ ] **Step 2.** For each: Red output recorded (the REQ-SFT-29 regression Red is `Assert.True() Failure` on `success`); the first Red commit is tests-only `Lane: RED`, then Green commits per test or one Green commit at the end.
- [ ] **Step 3.** Full suite `Failed: 0` (Bug068 regression tests included). Exit checklist. Commit: `fix(song-service): version-aware update uniqueness; newArtistName on update (REQ-SFT-26/27/28/29)`.

**Merge risk:** High (BUG-067/068 territory). Rollback: `git revert -m 1 <merge-sha>`.

### Task 3.3: Simplify `SongResolutionService` [W4, after 3.1 + 5.1]

**Model:** Sonnet (single agent). **Level:** A (REQ-SFT-42 regression). **Review lane:** Elevated. **Estimate:** 90 min.

**Files:**
- Modify: `Domain/ServicesInterfaces/ISongResolutionService.cs` (keep only `ResolveAsync`)
- Modify: `Services/SongResolutionService.cs` (exact match via `FindIdByTitleVersionForArtistAsync`; delete `ApplyUpdate`, `CommitAsync`, every `HasManualEdits` read; construct `SongResolution` with `FieldDiffs: []`, `TargetHasManualEdits: false` until 3.3b)
- Modify: `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`
- Delete: `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (its 4 behaviours were ported in 3.1)

**Precondition census:** `['CommitAsync(']` on `SongFormViewModel.cs` → zero (5.1 merged). Otherwise stop.

**Superseded tests (`design.md` § 10.1):**

| Test (file:line) | Action | Superseded AC → replacing REQ |
|---|---|---|
| `SongResolutionServiceTests.cs:100`, `:271` (FieldDiffs) | delete | AC-4.2 → REQ-SFT-38 |
| `:312`, `:327` `CommitAsync_CreateNewVersion_*` | delete | `CommitAsync` removed; AC-1.2 still covered by `SongFormViewModelTests.cs:265` and `ValidateVersionInput` tests |
| `:343`, `:389` `UpdateExisting` | delete | AC-4.1/4.2, AC-1.1 "Update existing" → REQ-SFT-24 |
| `:435` `AttachExternalId` | delete | AC-2.2 → REQ-SFT-05 |
| `:481` `CreateNew_ResolvesArtistThenCreatesSong` | delete | AC-2.5 → REQ-SFT-18 (replaced by 3.1's tests) |
| kept tests with `HasManualEdits = …` initialisers (`:81`, `:154`, `:445` if kept) | edit: drop the initialiser and any `TargetHasManualEdits` assert | REQ-SFT-37 |
| `NestedUnitOfWorkTests.cs:22,146` | deleted here, **ported in 3.1** | REQ-UOW-22 re-homed |
| `NestedUnitOfWorkTests.cs:66,105` | deleted here, **rewritten in 3.1, assertion inverted** | REQ-UOW-24 → REQ-SFT-18 |
| `NestedUnitOfWorkTests.cs:195` | delete | REQ-UOW-09 (**removed**) and AC-3.4 → REQ-SFT-40 |

- [ ] **Step 1: Red.** Write:

```csharp
[Fact]
// [AC] REQ-SFT-42: an accent-only title difference returns an exact match carrying the stored song's id.
public async Task ResolveAsync_AccentOnlyTitleDifference_ReturnsExactLocalMatchWithId()
{
    _songRepo.Setup(r => r.FindIdByTitleVersionForArtistAsync(7, "Nao Sei", "", null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(42);
    var sut = CreateSut();

    var result = await sut.ResolveAsync(/* candidate: Title "Nao Sei", Version "", artist id 7 — use the real candidate type */);

    Assert.Equal(ResolutionKind.ExactLocalMatch, result.Kind);
    Assert.Equal(42, result.ExactMatchSongId);
}
```

Expected Red: `Assert.Equal() Failure: Expected 42, Actual (null)` (today `SongResolutionService.cs:123-127` returns no id). Commit test only, `Lane: RED`.
- [ ] **Step 2: Green.** Replace the exists + pool re-match + `OrdinalIgnoreCase` path (`:96-127`) with the single collated lookup; delete the members listed; apply the superseded-test table. Build after each file.
- [ ] **Step 3.** Full suite `Failed: 0`; census `['ApplyUpdate','NestedUnitOfWorkTests']` → ZERO HITS; `['CommitAsync(']` → hits only in `IArtistResolutionService.cs`, `ArtistResolutionService.cs`, `ArtistResolutionServiceTests.cs` (3.4) and `UnitOfWork.cs` (the UoW's own method).
- [ ] **Step 4.** Exit checklist (superseded-test table + AC matrix REQ-SFT-42). Commit: `fix(song-resolution): collated exact match carries the id; remove CommitAsync/ApplyUpdate (REQ-SFT-24/38/42)`.

**Demo:** "Nao Sei" for Ana Carolina resolves to the stored "Não Sei" id. **Merge risk:** Medium.

### Task 3.3b: Shrink `SongResolution`; delete `FieldDiff` [W5, after 3.3] *(new, § 0.3 D4)*

**Model:** Haiku. **Level:** C (mechanical). **Review lane:** Standard. **Estimate:** 30 min.

**Files:** `Domain/Resolution/ResolutionResults.cs` (delete `FieldDiff`; `SongResolution` loses `FieldDiffs` and `TargetHasManualEdits`), `Services/SongResolutionService.cs` (constructors), `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (constructions `:189`, `:218`, `:248`, `:1150` → 3 positional args)

**Produces:** `public sealed record SongResolution(ResolutionKind Kind, int? ExactMatchSongId, IReadOnlyList<SongMatch> FuzzyCandidates);`

- [ ] Edit → build after each file → full suite `Failed: 0` (count unchanged) → census `['FieldDiff','TargetHasManualEdits']` → ZERO HITS. Commit: `refactor(resolution): drop FieldDiffs/TargetHasManualEdits from SongResolution (REQ-SFT-38)`.

### Task 3.4: Simplify `ArtistResolutionService` (commit side); delete `ResolutionChoice` [W5, after 3.3]

**Model:** Sonnet. **Level:** C (deletions). **Review lane:** Standard. **Estimate:** 45 min.

**Files:** `Domain/ServicesInterfaces/IArtistResolutionService.cs` (keep only `ResolveAsync`), `Services/ArtistResolutionService.cs` (delete `CommitAsync`), `Domain/Resolution/ResolutionEnums.cs` (delete `ResolutionChoice`; delete the file only if it becomes empty, § 0.3 D7), `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`

**Superseded tests:** `ArtistResolutionServiceTests.cs:200`, `:225`, `:249` (`CommitAsync_*`) → delete; AC-3.2/3.4 → REQ-SFT-40.

- [ ] Census `['ResolutionChoice']` first: hits only in the 4 owned files (5.1 and 3.3 removed the others), else stop. Edit → build → suite `Failed: 0` (incl. `AppServicesRegistrationTests`) → census `['ResolutionChoice']` ZERO HITS and `['CommitAsync(']` → only `UnitOfWork.cs`. Commit: `refactor(artist-resolution): remove CommitAsync and ResolutionChoice (REQ-SFT-40)`.

### Task 3.2b: Remove `hasManualEdits` from `UpdateSongAsync` (production + unit tests) [W6, after 3.2a + 3.3b; G3]

**Model:** Sonnet. **Level:** C (mechanical) + the moved `:787` rewrite. **Review lane:** Standard. **Estimate:** 45 min.

**Files:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs` (delete the writer `:172`), `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (call site `:606`), `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (13 `UpdateSongAsync_*` calls), `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (6 mock setups + the `:787` rewrite)

**Produces:** new overload `UpdateSongAsync(int id, string title, string? featuredArtists, string? lyrics, string? externalId = null, string? externalProvider = null, string? version = null, int? artistId = null, string? newArtistName = null, CancellationToken ct = default)`; the old one (with `bool hasManualEdits`) kept `[Obsolete]`, forwarding and ignoring the flag.

**Superseded / rewritten tests:**

| Test | Action | Superseded AC → replacing REQ |
|---|---|---|
| `SongFormViewModelTests.cs:787` `LoadSongForEdit_ApiImportedWithoutManualEdits_LocksArtistField` | rewrite without `HasManualEdits` (`:796`), rename `LoadSongForEdit_ApiImportedSong_LocksArtistField`, `// [AC] REQ-SFT-28: the stored artist loads in the picked state` (moved from 5.4, § 0.3 D6) | BUG-008 setup → REQ-SFT-28 |
| `SongServiceTests.cs` 13 `UpdateSongAsync_*`, VM tests 6 setups | mechanical: drop the `hasManualEdits` argument | REQ-SFT-37 |

- [ ] Edit one file at a time, build after each → suite `Failed: 0` → census `['hasManualEdits']` → hits only in `ISongService.cs`/`SongService.cs` (the obsolete overload), `SongServiceUpdateIntegrationTests.cs`, `Bug068RegressionTests.cs` (3.2c). Build: 0 errors; `CS0618` warnings only in those 2 test files. Commit: `refactor(song-service): UpdateSongAsync without hasManualEdits (REQ-SFT-37)`.

### Task 3.2c: Move the remaining callers; delete the obsolete `UpdateSongAsync` overload [W7; G3]

**Model:** Sonnet. **Level:** C. **Review lane:** Standard. **Estimate:** 30 min. **Demo:** census finds no `hasManualEdits` argument; all tests green.

**Files:** `ISongService.cs`, `SongService.cs`, `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (2 calls), `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` (1 `UpdateSongAsync` call; also move its 2 `CreateSongWithUrlsAsync` calls to the new overload), and `SongResolutionServiceTests.cs` **only if** the census finds a remaining `UpdateSongAsync` mock with the flag.

- [ ] Census `['hasManualEdits']` → edit the hits → delete the obsolete overload → build 0 errors → suite `Failed: 0` → census ZERO HITS (outside `Migrations/`). Commit: `refactor(song-service): delete obsolete UpdateSongAsync(hasManualEdits) overload (REQ-SFT-37)`.

### Task 3.5a: Drop `HasManualEdits` from the song read model and projections [W8] *(split, § 0.3 D5)*

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 30 min. **Demo:** the Songs list renders unchanged.

**Files:** `Contracts/DTOs/List/SongListItemDto.cs`, `Infra/Repository/SongRepository.cs` (`:39-46`), `Infra/Repository/CatalogRepository.cs` (`:41-48`), `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs` (`:60` positional construction)

- [ ] Edit → build after each → suite `Failed: 0` → census `['HasManualEdits']` in the 4 files → zero. Emulator smoke: the Songs list renders. Commit: `refactor(songs): drop HasManualEdits from SongListItemDto and projections (REQ-SFT-37)`.

### Task 3.5b: Drop `HasManualEdits` from the artist read models and projections [W8, P with 3.5a]

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 30 min. **Demo:** the Artists list renders unchanged.

**Files:** `Contracts/DTOs/List/ArtistListItemDto.cs`, `Domain/ReadModels/ArtistListItem.cs`, `Infra/Repository/ArtistRepository.cs` (`:47-51`, `:74`), `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (`:424, :448, :491, :1177, :1179, :1220`), `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs` (`:50`)

**Produces:** `public record ArtistListItem(int Id, string Name, string ExternalProvider, int CatalogCount);`

- [ ] Census `['ArtistListItem(','ArtistListItemDto(','HasManualEdits']` first (a mapper using target-typed `new(` is caught by the `HasManualEdits` census). Edit → build → suite → census zero in owned files. Emulator smoke: Artists list renders. Commit: `refactor(artists): drop HasManualEdits from ArtistListItem(Dto) and projections (REQ-SFT-37)`.

### Task 3.6a: Remove the `Artist.HasManualEdits` writer [W9]

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 15 min. **Demo:** build and tests green; editing an artist saves.

**Files:** `Services/ArtistService.cs` (`:107`). (`ArtistServiceTests.cs` has no assertion on the flag; census 2026-09-26.)

- [ ] Delete the line → build → suite → census `['HasManualEdits']` outside `Migrations/` → hits only in `Song.cs`, `Artist.cs`, `SongConfiguration.cs`, `ArtistConfiguration.cs` (3.6b). Commit: `refactor(artist-service): stop writing HasManualEdits (REQ-SFT-37)`.

### Task 3.6b: M1 — drop the `HasManualEdits` columns [W10; SEQUENTIAL; G3 + § 0.2.2]

**Model:** Part A Sonnet · scaffold orchestrator · Part B Sonnet. **Level:** B. **Review lane:** Elevated. **Estimate:** 45 + 10 + 30 min. **Sizing:** 6 files (§ 0.3 D10).

**Files:**
- Modify: `Domain/Entity/Song.cs` (`:14`), `Domain/Entity/Artist.cs` (`:9`), `Infra/EntityEFConfig/SongConfiguration.cs` (`:48`), `Infra/EntityEFConfig/ArtistConfiguration.cs` (`:33-34`)
- Create (scaffold): `Infra/Migrations/<ts>_DropHasManualEdits.cs` + `.Designer.cs`; modify `AppDbContextModelSnapshot.cs`
- Create: `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (namespace `MyVocaList.Tests.Integration.Migrations`)

Test helpers (in the new test class):

```csharp
private static async Task<AppDbContext> MigratedToAsync(string? target = null)
{
    var db = TestDbContextFactory.Create();
    var migrator = db.GetService<IMigrator>();     // Microsoft.EntityFrameworkCore.Infrastructure / .Migrations
    await migrator.MigrateAsync(target);            // null = latest
    return db;
}
private static async Task<IReadOnlyList<string>> ColumnsAsync(AppDbContext db, string table)
    => await db.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}')").ToListAsync();
```

#### Part A (Sonnet)

- [ ] **Step 1: Write the failing tests.**

| Test | `// [AC]` | Asserts |
|---|---|---|
| `M1_DropsHasManualEditsFromSongsAndArtists` | REQ-SFT-37 | after full migrate, `HasManualEdits` ∉ columns of `Songs` and `Artists` |
| `M1_ExistingRowsSurvive` | REQ-SFT-37 | migrate to the migration **before** `DropHasManualEdits` (take `db.Database.GetMigrations()` and the element preceding it), seed 1 artist + 2 songs with raw SQL, migrate to latest → same rows, same titles/versions/ids |
| `M1_TableRebuild_KeepsUniqueCollatedIndexes` | REQ-SFT-52 (Review Focus 3) | after full migrate: `IX_Artists_Name` exists and is unique (`pragma_index_list`), inserting "metallica" beside "Metallica" throws `SqliteException` (constraint); the `(ArtistId, Title, Version)` unique song index exists and rejects a duplicate |

- [ ] **Step 2: Red.** Filter `SongMigrationTests`. Expected: `M1_DropsHasManualEdits…` fails (`Assert.DoesNotContain() Failure … HasManualEdits`); `M1_ExistingRowsSurvive` fails because the target migration name is not found (`InvalidOperationException … 'DropHasManualEdits'`); `M1_TableRebuild…` may already pass (it is a guard; record it). Commit the test file only, `Lane: RED`.
- [ ] **Step 3.** Delete the four model lines. Build 0 errors. **Do not commit.** Handback: "model edited, awaiting scaffold".

#### Scaffold (orchestrator, in `.worktrees/sft-3.6b`)

- [ ] `dotnet ef migrations add DropHasManualEdits -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj`. The orchestrator runs the command only and does **not** open the generated `.cs` files (orchestrator read-scope HARD RULE); the check of the generated code is Part B's first step. Then dispatch Part B.

#### Part B (Sonnet)

- [ ] **Step 3b: Check the generated migration (first step).** Open `Infra/Migrations/<ts>_DropHasManualEdits.cs`. Expected `Up`: exactly two `DropColumn("HasManualEdits", …)` calls (`Songs`, `Artists`); `Down`: exactly two `AddColumn<bool>("HasManualEdits", … defaultValue: false)`. Any other operation means the snapshot is stale → status `blocked: stale snapshot`, stop, hand back (the orchestrator recreates the worktree from the current `develop` per § 0.2.2 and re-runs Part A's model edit and the scaffold).
- [ ] **Step 4: Green.** Filter → all pass; full suite `Failed: 0`; `dotnet ef migrations has-pending-changes … --exit-code` → 0. Census `['HasManualEdits']` excluding `Migrations/` → ZERO HITS.
- [ ] **Step 5.** Exit checklist (AC matrix REQ-SFT-37). Commit (model + migration together): `feat(db): M1 drop HasManualEdits from Songs and Artists (REQ-SFT-37)`.

**Rollback:** `git revert -m 1 <merge-sha>` + `dotnet ef database update <previous-migration-name> … --connection "Data Source=.claude/MyVocaList.db"`. `Down` re-adds the columns with `false`; the old values are lost (they meant "saved from the edit screen at least once", findings § 3.3). A device that already ran M1 needs a reinstall or a forward migration, never a deleted migration. **Risk:** Medium (SQLite table rebuild).

### Task 3.7: M2 — unique `(ExternalProvider, ExternalId)` with dedupe [W11; SEQUENTIAL; G3 + § 0.2.2]

**Model:** Part A Sonnet · scaffold orchestrator · Part B Sonnet. **Level:** B. **Review lane:** Elevated. **Estimate:** 45 + 10 + 30 min.

**Files:** `Infra/EntityEFConfig/SongConfiguration.cs` (`:61-62`: replace the single-column `IX_Songs_ExternalId` with `HasIndex(s => new { s.ExternalProvider, s.ExternalId }).IsUnique()`), `Infra/Migrations/*` (M2), `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs`

#### Part A

- [ ] **Step 1: Write the failing tests.**

| Test | `// [AC]` | Asserts |
|---|---|---|
| `M2_DuplicateIdentities_LowestIdKeepsIdentity_OthersClearedNotDeleted` | REQ-SFT-39 | migrate to `DropHasManualEdits`; seed songs 1, 2, 3 with `("Deezer","3135556")` and song 4 with `("Deezer","999")`; migrate to latest → song 1 keeps the identity; songs 2, 3 have `ExternalId`/`ExternalProvider` null and keep title/version/artist; song 4 unchanged; 4 rows exist |
| `M2_UniqueIndex_RejectsSecondSongWithSameIdentity` | REQ-SFT-39 | after full migrate, inserting a second `("Deezer","3135556")` throws `SqliteException` (UNIQUE) |
| `M2_NullIdentities_AllowMany` | REQ-SFT-39 | two songs with null identity insert fine |
| `M2_IndexName_IsComposite` | REQ-SFT-39 | `IX_Songs_ExternalProvider_ExternalId` exists and is unique; `IX_Songs_ExternalId` does not exist |

- [ ] **Step 2: Red** (`M2_*` fail: target migration `SongExternalIdentityUnique` not found / duplicate insert succeeds). Commit tests only, `Lane: RED`.
- [ ] **Step 3.** Edit `SongConfiguration.cs`. Build. Do not commit. Handback.

#### Scaffold (orchestrator)

- [ ] `dotnet ef migrations add SongExternalIdentityUnique -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj`. The orchestrator runs the command only and does **not** open the generated `.cs` files (orchestrator read-scope HARD RULE); the check of the generated code is Part B's first step.

#### Part B

- [ ] **Step 3b: Check the generated migration (first step).** Expected `Up`: `DropIndex("IX_Songs_ExternalId")` and `CreateIndex("IX_Songs_ExternalProvider_ExternalId", … unique: true)` only; `Down`: the reverse. Anything else → `blocked: stale snapshot`, stop, hand back.
- [ ] **Step 4.** In `Up`, insert **before** `CreateIndex` (and after `DropIndex("IX_Songs_ExternalId")`) the dedupe step, verbatim from `design.md` § 7:

```csharp
migrationBuilder.Sql(
    "UPDATE Songs SET ExternalId = NULL, ExternalProvider = NULL " +
    "WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
    "AND Id NOT IN (SELECT MIN(Id) FROM Songs WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
    "GROUP BY ExternalProvider, ExternalId);");
```

`Down`: drop the composite index, re-create the non-unique `IX_Songs_ExternalId`. Cleared identities are not restored (comment it).
- [ ] **Step 5: Green.** Filter → pass; suite `Failed: 0`; `has-pending-changes` → 0. Exit checklist. Commit: `feat(db): M2 unique (ExternalProvider, ExternalId) with lowest-Id dedupe (REQ-SFT-39)`.

**Rollback:** as M1; `Down` restores the single-column non-unique index. Expected dedupe count on real data: 0 (providers never returned data, findings § 0.1). **Risk:** Medium (data fix).

### Task 3.8: M3 — FTS5 `SongSearch` + `ArtistSearch`, 7 triggers, RETURNING off [W12; SEQUENTIAL; **G5 PASS**; G3 + § 0.2.2]

**Model:** Part A Sonnet · scaffold orchestrator · Part B **Opus** (raw SQL, trigger correctness, RETURNING interaction). **Level:** B. **Review lane:** Architectural. **Estimate:** 45 + 10 + 90 min.

**Pre-dispatch:** `findings.md` § G5 Verdict reads `G5: PASS`. Otherwise do not dispatch.

**Files:** `Infra/Migrations/*` (M3), `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/EntityEFConfig/ArtistConfiguration.cs` (`builder.ToTable(tb => tb.UseSqlReturningClause(false))`; merge into an existing `ToTable(...)` lambda if one exists; Context7 EF Core 10 `UseSqlReturningClause`)

#### Part A

- [ ] **Step 1: Write the failing tests.** The trigger-presence test is the guard behind the constraint note: any future migration that rebuilds `Songs` or `Artists` without re-creating the triggers fails it.

```csharp
private static readonly string[] FtsTriggers =
[
    "SongSearch_ai", "SongSearch_au", "SongSearch_ad", "SongSearch_artist_au",
    "ArtistSearch_ai", "ArtistSearch_au", "ArtistSearch_ad",
];

[Fact]
// [AC] REQ-SFT-34: after a full migrate, all seven FTS sync triggers exist (a table rebuild that drops them fails here).
public async Task FullMigrate_AllSevenFtsTriggersExist()
{
    await using var db = await MigratedToAsync();
    var triggers = await db.Database
        .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'trigger'")
        .ToListAsync();
    foreach (var name in FtsTriggers) Assert.Contains(name, triggers);
}
```

| Test | `// [AC]` | Asserts |
|---|---|---|
| `FullMigrate_AllSevenFtsTriggersExist` | REQ-SFT-34 | above |
| `FullMigrate_CreatesSongSearchAndArtistSearch` | REQ-SFT-34 | both names in `sqlite_master` (`type='table'`) |
| `M3_Backfill_OneRowPerSongAndArtist` | REQ-SFT-34 / INV-SFT-3 | migrate to `SongExternalIdentityUnique`, seed 2 artists + 3 songs, migrate to latest → `SELECT count(*) FROM SongSearch` = 3, `ArtistSearch` = 2, `SongSearch.artist` = the artist's name |
| `SongInsertViaEf_AddsSongSearchRow` | REQ-SFT-34 | EF `Add` + `SaveChanges` of a Song succeeds (RETURNING off) and `SongSearch` has its rowid |
| `SongTitleUpdateViaEf_ReplacesSongSearchRow` | REQ-SFT-34 | EF update of the title succeeds; the old title no longer matches, the new one does |
| `SongExecuteDelete_RemovesSongSearchRow` | REQ-SFT-34 | `ExecuteDeleteAsync` on Songs → row gone |
| `ArtistInsertViaEf_AddsArtistSearchRow` | REQ-SFT-34 | EF insert of an Artist succeeds; row present |
| `ArtistRenameViaEf_UpdatesSongSearchAndArtistSearch` | REQ-SFT-34 | rename → both tables hold the new name |
| `ArtistDelete_CascadeRemovesSongSearchRows` | REQ-SFT-34 | delete the artist (cascade, `ArtistConfiguration.cs:47-50`) → its songs' rows gone, `ArtistSearch` row gone |
| `M3_Down_DropsTablesAndTriggers` | REQ-SFT-34 (rollback) | migrate back to `SongExternalIdentityUnique` → no FTS table, no trigger |

- [ ] **Step 2: Red** (tables/triggers absent; EF writes may already pass). Commit tests only, `Lane: RED`.
- [ ] **Step 3.** Add `UseSqlReturningClause(false)` to both configurations. Build. Do not commit. Handback.

#### Scaffold (orchestrator)

- [ ] `dotnet ef migrations add AddSearchIndexes -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj` (expected: an empty or annotation-only `Up`). The orchestrator runs the command only and does **not** open the generated `.cs` files (orchestrator read-scope HARD RULE); the check of the generated code is Part B's first step.

#### Part B (Opus)

- [ ] **Step 3b: Check the generated migration (first step).** Expected `Up`/`Down`: empty, or annotation-only changes for `Songs`/`Artists` (`UseSqlReturningClause`); no table, column or index operation. Anything else → `blocked: stale snapshot`, stop, hand back.
- [ ] **Step 4.** Write `Up` as `migrationBuilder.Sql(...)` calls with the `design.md` § 6.2 DDL **verbatim** (two `CREATE VIRTUAL TABLE … tokenize='trigram remove_diacritics 1'`, two backfill `INSERT … SELECT`, seven `CREATE TRIGGER`, one statement per `Sql` call). `Down`: `DROP TRIGGER IF EXISTS` × 7, then `DROP TABLE IF EXISTS SongSearch`, `DROP TABLE IF EXISTS ArtistSearch`. Header comment: "Any migration that rebuilds Songs or Artists must recreate these triggers in the same migration (constraints-reference.md § EF Core / SQLite)".
- [ ] **Step 5: Green.** Filter → all pass; full suite `Failed: 0` (every EF write test in the suite now runs against the trigger-free `EnsureCreated` schema or the migrated one; both must pass); `has-pending-changes` → 0.
- [ ] **Step 6.** Exit checklist. Commit: `feat(db): M3 FTS5 trigram SongSearch + ArtistSearch with 7 sync triggers; RETURNING off (REQ-SFT-19/30-34)`.

**Orchestrator post-merge step (spec, `tasks.md` 3.8):** add to `.claude/library/constraints-reference.md § EF Core / SQLite`: "Any migration that rebuilds Songs or Artists must recreate the FTS triggers in the same migration." (Guarded by `FullMigrate_AllSevenFtsTriggersExist`.) Commit on develop: `docs(constraints): FTS triggers must be recreated by any Songs/Artists table rebuild`.

**Rollback:** `git revert -m 1 <merge-sha>` + `dotnet ef database update SongExternalIdentityUnique …`; `Down` drops triggers and tables; no user data lives in the FTS tables. **Risk:** High (raw SQL; a later rebuild drops triggers — guarded by the test).

### Task 3.9: Title-field search query [W13]

**Model:** Sonnet. **Level:** B (test-first). **Review lane:** Elevated. **Estimate:** 90 min.

**Files:**
- Create: `Domain/ReadModels/SongSearchHit.cs`
- Modify: `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`
- Create: `Infra/Repository/FtsQueryBuilder.cs` (`internal static`, shared with 3.9b, § 0.3 D9)
- Create: `MyVocaList.Tests/Integration/Repositories/SongRepositorySearchTests.cs` (uses `MigrateAsync`)

**Interfaces — Produces (`design.md` § 3.1 verbatim + the builder):**

```csharp
public sealed record SongSearchHit(
    int SongId, string Title, string Version, int ArtistId, string ArtistName,
    string? ExternalProvider, string? ExternalId);

Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(string query, int take, CancellationToken ct);
Task<IReadOnlyList<SongSearchHit>> FindByExternalIdsAsync(string provider, IReadOnlyCollection<string> externalIds, CancellationToken ct);
Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(IReadOnlyCollection<(string Title, string ArtistName)> pairs, string version, CancellationToken ct);

internal static class FtsQueryBuilder
{
    /// OR of the distinct 3-char windows of every whitespace-separated word of length >= 3,
    /// each double-quoted with '"' doubled. Returns null when no word has >= 3 chars (caller uses the prefix path).
    /// Query construction only — case/diacritic folding is done by the SQLite tokenizer.
    internal static string? BuildTrigramMatch(string query);
}
```

SQL (`design.md` § 6.2): `SELECT s.Id, s.Title, s.Version, s.ArtistId, a.Name, s.ExternalProvider, s.ExternalId FROM SongSearch JOIN Songs s ON s.Id = SongSearch.rowid JOIN Artists a ON a.Id = s.ArtistId WHERE SongSearch MATCH @m ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title) LIMIT @take` via `Database.SqlQueryRaw<…>` with parameters (Context7 EF Core 10: `SqlQuery`/`SqlQueryRaw` unmapped types). `BuildTrigramMatch` returns null → the existing collated title-prefix query (REQ-SFT-33). A `SqliteException` "no such table: SongSearch" → log **Error** and return `[]` (failure-modes table; use the allowed try-catch pattern of `code-style-reference.md § Exception Handling`; if none fits: `blocked: spec gap`).

- [ ] **Step 1: Write the failing tests.** Seed (via EF, after `MigrateAsync`): "Don't Cry – Guns N' Roses", "Sweet Child O' Mine – Guns N' Roses", "Cry – Cyndi Lauper", "Boys Don't Cry – The Cure", "Dont Look Back in Anger – Oasis", "Não Sei – Ana Carolina", "Abba Song – ABBA".

| Test | `// [AC]` | Asserts |
|---|---|---|
| `BuildTrigramMatch_DontCry_ReturnsQuotedWindowsOred` | REQ-SFT-30 | `"\"don\" OR \"ont\" OR \"cry\""` (distinct, order of appearance) |
| `BuildTrigramMatch_NoWordOfThreeChars_ReturnsNull` (Theory `"ab"`, `"a b"`, `"  "`) | REQ-SFT-33 | null |
| `BuildTrigramMatch_QuoteInWord_IsDoubled` | REQ-SFT-02 | `don"t` → windows containing `""` inside quotes |
| `SearchTitleCandidatesAsync_DontCry_IncludesDontCry` | REQ-SFT-30 | contains "Don't Cry" |
| `SearchTitleCandidatesAsync_DonCry_IncludesDontCry` | REQ-SFT-30 | contains "Don't Cry" |
| `SearchTitleCandidatesAsync_GunsCry_IncludesDontCryViaArtist` | REQ-SFT-32 | contains "Don't Cry" |
| `SearchTitleCandidatesAsync_NaoSeiAnyCase_MatchesAccentedTitle` (Theory `"nao sei"`, `"NAO SEI"`) | REQ-SFT-31 | contains "Não Sei" |
| `SearchTitleCandidatesAsync_ShortQuery_UsesCollatedTitlePrefix` (Theory `"ab"`, `"AB"`) | REQ-SFT-33 | returns "Abba Song" |
| `SearchTitleCandidatesAsync_FtsSyntaxCharacters_DoNotThrow` (Theory `"\""`, `"don\"t cry"`, `"ac/dc"`, `"rock*"`, `"a:b cry"`, `"(cry)"`, `"NEAR cry"`, `"cry AND"`, `"-cry"`) | REQ-SFT-02 (Review Focus 1) | no exception; result is a list |
| `SearchTitleCandidatesAsync_RespectsTake` | REQ-SFT-04 | `take: 2` → ≤ 2 rows |
| `SearchTitleCandidatesAsync_IndexMissing_ReturnsEmptyAndPrefixStillWorks` | failure modes (REQ-SFT-33) | on an `EnsureCreated` DB: "dont cry" → `[]` without throwing; "ab" → "Abba Song" |
| `FindByExternalIdsAsync_ReturnsHoldersOnly` | REQ-SFT-05(a) | ids ["3135556","x"] → the one holder |
| `FindByTitleAndArtistNamesAsync_CollatedPairWithEmptyVersion_Returns` | REQ-SFT-05(b) | ("dont cry" ≠ …) use ("DON'T CRY","guns n' roses") → "Don't Cry" |
| `FindByTitleAndArtistNamesAsync_NonEmptyVersion_NotReturned` | REQ-SFT-05(b) | a "Live" version is not returned for version "" |

- [ ] **Step 2: Red** (`CS0246 SongSearchHit` / `CS0103 FtsQueryBuilder` / `CS1061`). Commit tests only, `Lane: RED`.
- [ ] **Step 3: Green.** Implement. Filter → pass; suite `Failed: 0`.
- [ ] **Step 4.** Exit checklist. Commit: `feat(song-repo): FTS5 trigram title/artist candidate search + dedup lookups (REQ-SFT-02/05/30-33)`.

**Merge risk:** Medium.

### Task 3.9b: Artist-name candidate query [W14, after 3.9]

**Model:** Sonnet. **Level:** B. **Review lane:** Standard. **Estimate:** 60 min.

**Files:** `Domain/RepositoryInterface/IArtistRepository.cs`, `Infra/Repository/ArtistRepository.cs`, `MyVocaList.Tests/Integration/Repositories/ArtistRepositorySearchTests.cs` (new, `MigrateAsync`)

**Produces:** `Task<IReadOnlyList<Artist>> SearchNameCandidatesAsync(string name, int take, CancellationToken ct);` — `ArtistSearch MATCH FtsQueryBuilder.BuildTrigramMatch(name)` ordered by `bm25(ArtistSearch)`; null match → the existing collated prefix query.

- [ ] **Step 1: Tests.**

| Test | `// [AC]` | Asserts |
|---|---|---|
| `SearchNameCandidatesAsync_Metalica_RetrievesMetallica` | REQ-SFT-19 | contains "Metallica" |
| `SearchNameCandidatesAsync_Beatles_RetrievesTheBeatles` | REQ-SFT-19 | contains "The Beatles" |
| `SearchNameCandidatesAsync_Unaccented_RetrievesAccentedName` | REQ-SFT-19 | "beyonce" → "Beyoncé" |
| `SearchNameCandidatesAsync_ShortName_UsesCollatedPrefix` | REQ-SFT-19 | "u2" with "U2" stored → "U2" |
| `SearchNameCandidatesAsync_FtsSyntaxCharacters_DoNotThrow` (Theory as 3.9) | REQ-SFT-19 (Review Focus 1) | no exception |
| `SearchNameCandidatesAsync_AfterRename_ReflectsNewName` | REQ-SFT-34 | rename via EF → the new name is retrieved, the old is not |

- [ ] Red (`CS1061`) → commit `Lane: RED` → Green → suite `Failed: 0` → exit checklist → commit: `feat(artist-repo): FTS5 trigram artist-name candidates (REQ-SFT-19/34)`.

### Task 3.9c: `ArtistResolutionService` uses the FTS pool [W15, after 3.9b + 3.4]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** Elevated. **Estimate:** 60 min.

**Files:** `Services/ArtistResolutionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`, `Domain/RepositoryInterface/IArtistRepository.cs`, `Infra/Repository/ArtistRepository.cs`, `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryResolutionTests.cs`

**Produces:** the fuzzy tier reads `SearchNameCandidatesAsync(name, SimilarityConstants.PoolSize, ct)` (resolved from `sp`) and keeps `Score ≥ SimilarityConstants.DefaultThreshold`. Artist-side `GetFuzzyCandidatePoolAsync` and `DerivePrefixToken` (artist side only) deleted. The song-side `ISongRepository.GetFuzzyCandidatePoolAsync` stays.

**Superseded tests:**

| Test | Action | Superseded AC → replacing REQ |
|---|---|---|
| `ArtistResolutionServiceTests.cs:73`, `:101`, `:128` (prefix-pool mocks) | rewrite: mock `SearchNameCandidatesAsync` | AC-5.2 pre-filter now FTS → REQ-SFT-19 |
| `ArtistResolutionServiceTests.cs:152` `LongMultiWordName_UsesFirstTokenCapped` | delete | AC-5.2 → REQ-SFT-19 |
| `ArtistRepositoryResolutionTests.cs` tests of the artist `GetFuzzyCandidatePoolAsync` | delete (replaced by 3.9b) | AC-5.2 → REQ-SFT-19 |

- [ ] **Step 1: Red.** `ResolveAsync_Metalica_ReturnsFuzzyCandidateMetallica` // `[AC] REQ-SFT-19`: mock `SearchNameCandidatesAsync("Metalica", 50, …)` → [Metallica]; assert `Kind == FuzzyCandidates` and the candidate "Metallica" with score ≥ 0.82. Expected Red: `CS1061` (member missing on the mock target until 3.9b's interface is consumed) or `Assert.Equal() Failure` (the service still calls the prefix pool). Commit test only, `Lane: RED`.
- [ ] **Step 2: Green**; apply the superseded table; census `['DerivePrefixToken']` → only `SongResolutionService.cs` (song side); `['GetFuzzyCandidatePoolAsync']` → only song-side files. Suite `Failed: 0`. Commit: `feat(artist-resolution): near-duplicate candidates from the ArtistSearch index (REQ-SFT-19)`.

### Task 3.10: `SongService.SaveFromFormAsync`: every Save decision in the Service [W16; G3]

**Model:** Tester Sonnet · Builder **Opus** (constitutional boundary, rule ordering, re-invocation semantics). **Level:** A. **Review lane:** Architectural. **Estimate:** 60 + 120 min (pre-planned split below).

**Files:**
- Create: `Domain/Resolution/SongFormSaveRequest.cs` (`SongFormSaveRequest`, `ArtistDecision`, `SongDecision`)
- Create: `Domain/Resolution/SongFormSaveOutcome.cs` (`SongFormSaveKind`, `SaveField`, `ArtistMatch`, `SongFormSaveOutcome`)
- Modify: `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`
- Create: `MyVocaList.Tests/Unit/Services/SongServiceSaveFromFormTests.cs`

**Interfaces — Produces (`design.md` § 3.5; the three small types are defined here so the design's names compile):**

```csharp
public sealed record SongFormSaveRequest(
    int? SongId, string Title, string Version, string? FeaturedArtists, string? Lyrics,
    int? ArtistId, string? ArtistText, string? ExternalProvider, string? ExternalId,
    IReadOnlyList<string> PendingUrls, ArtistDecision ArtistDecision, SongDecision SongDecision);

public abstract record ArtistDecision
{
    public sealed record None : ArtistDecision;
    public sealed record UseExisting(int ArtistId) : ArtistDecision;
    public sealed record CreateTyped : ArtistDecision;
}
public enum SongDecision { None, SaveAsNewVersion, SaveAsNewSong }

public enum SongFormSaveKind { Saved, Failed, ArtistChoiceNeeded, SongChoiceNeeded }
public enum SaveField { None, Title, Version, Artist }
public sealed record ArtistMatch(int ArtistId, string Name, double Score);
public sealed record SongFormSaveOutcome(
    SongFormSaveKind Kind, string Message, SaveField FailedField, int? SavedSongId,
    IReadOnlyList<ArtistMatch> ArtistCandidates, bool IsExactSongMatch, IReadOnlyList<SongMatch> SongCandidates);

// ISongService
Task<SongFormSaveOutcome> SaveFromFormAsync(SongFormSaveRequest request, CancellationToken ct = default);
```

Rules in order (`design.md` § 3.5, verbatim intent): 1 validate (title, version, artist required → `Failed(field)`; **and, when the artist would be created (no `ArtistId`, or `CreateTyped`), the typed name is validated first with the artist-name rules `IArtistService` applies (non-empty after trim, ≤ 60), so a violation returns `Failed(Artist)` with the service message before any resolution or write (REQ-SFT-20)**. The Builder reads `IArtistService.cs`: if it exposes a name validator, use it (resolved from `sp`); if it exposes none, stop with `blocked: spec gap` (adding a public member to `IArtistService` is a design change) — never duplicate the rules in `SongService`); 2 artist (`ArtistId` → use; else by decision; `None` → `IArtistResolutionService.ResolveAsync(new ArtistCandidate(ArtistText, null, null))` — **never** the song's identity; ExactLocalMatch → id silently; FuzzyCandidates → `ArtistChoiceNeeded` (≤ 3, best first); NoMatch → new); 3 song guard, add mode only, skipped when `SongDecision != None` (external-identity tier always; artist-scoped tiers only with an existing artist); 4 `SaveAsNewVersion` → `ValidateVersionInput(version, isRequired: true)`; 5 write through `CreateSongWithUrlsAsync(artistId|null, newName|null, …)` / `UpdateSongAsync(…, artistId|null, newName|null)` (edit mode passes `null` external identity = keep). Resolution services are resolved from `sp` inside `ExecuteReadAsync`; the write methods open their own unit of work; `SaveFromFormAsync` opens no write scope.

#### W16a: Tester

- [ ] **Step 1.** Write the tests (Moq for `IUnitOfWork` read scope, resolution services and repositories — follow the existing `SongServiceTests` harness). Each starts with its `// [AC]` line:

| Test | `// [AC]` |
|---|---|
| `SaveFromForm_EmptyTitle_FailsOnTitle` | REQ-SFT-15 |
| `SaveFromForm_EmptyArtist_FailsArtistRequired` | REQ-SFT-21 |
| `SaveFromForm_NewArtistNameInvalid_FailsOnArtist` (61-character typed name, no `ArtistId`) | REQ-SFT-20 | → `Failed`, `FailedField == SaveField.Artist`, the `IArtistService` message; no resolution call, no write |
| `SaveFromForm_TypedArtistExactCollatedMatch_ReusesExistingNeverCreates` | REQ-SFT-52 |
| `SaveFromForm_DeezerFilledArtistExactCollatedMatch_ReusesExisting` | REQ-SFT-18 |
| `SaveFromForm_TypedArtistPaddedCaseAccentVariant_ReusesExistingArtist` (`"  métallica "`) | REQ-SFT-52 (Review Focus 2) |
| `SaveFromForm_TypedArtistNearDuplicate_ReturnsArtistChoiceNeededTopThreeBestFirst` ("Metalica") | REQ-SFT-19 |
| `SaveFromForm_UseExistingDecision_SkipsArtistResolution_UsesId` | REQ-SFT-19 |
| `SaveFromForm_CreateTypedDecision_CreatesNewArtistViaWritePath` | REQ-SFT-19 |
| `SaveFromForm_NoArtistMatch_CreatesNewArtistInSameWrite` | REQ-SFT-18 |
| `SaveFromForm_ArtistResolution_NeverReceivesSongExternalIdentity` | REQ-SFT-41 |
| `SaveFromForm_ExactDuplicateSong_ReturnsSongChoiceNeededExact` | REQ-SFT-22 |
| `SaveFromForm_FuzzyDuplicateSong_ReturnsSongChoiceNeededNotExact` | REQ-SFT-23 |
| `SaveFromForm_NewArtistWithHeldIdentity_ReturnsSongChoiceNeededExact_CreatesNothing` | REQ-SFT-25 |
| `SaveFromForm_SaveAsNewVersionEmptyVersion_FailsOnVersion` | REQ-SFT-22 |
| `SaveFromForm_SaveAsNewVersionDecision_SkipsSongGuard_Writes` | REQ-SFT-22 |
| `SaveFromForm_SaveAsNewSongDecision_SkipsSongGuard_NoVersionRequired` | REQ-SFT-23 |
| `SaveFromForm_EditMode_RunsNoSongGuard_CallsUpdateWithNullIdentity` | REQ-SFT-27 |
| `SaveFromForm_WriteReturnsFailureTuple_ReturnsFailedWithMessage` | REQ-SFT-51 |
| `SaveFromForm_IdentityHeldByAnotherSong_FailsWithMessage` | REQ-SFT-26 |
| `SaveFromForm_Success_ReturnsSavedWithId` | REQ-SFT-51 |
| `SaveFromForm_WritesOnlyThroughCreateOrUpdate` (Verify no repository `AddAsync`/`UpdateAsync` from this method) | REQ-SFT-46 |

Example (the REQ-SFT-41 regression):

```csharp
[Fact]
// [AC] REQ-SFT-41: artist lookup for a song save never passes the song's external identity as the artist's.
public async Task SaveFromForm_ArtistResolution_NeverReceivesSongExternalIdentity()
{
    var request = NewRequest(artistId: null, artistText: "Guns N' Roses", provider: "Deezer", externalId: "3135556");
    _artistResolution.Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
    var sut = CreateSut();

    await sut.SaveFromFormAsync(request);

    _artistResolution.Verify(r => r.ResolveAsync(
        It.Is<ArtistCandidate>(c => c.ExternalId == null && c.ExternalProvider == null),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

(Use the real `ArtistCandidate` member names; the Tester reads `Domain/Resolution/`.)
- [ ] **Step 2: Red.** Filter `SongServiceSaveFromFormTests` → `CS0246` (`SongFormSaveRequest`) / `CS1061`. Commit tests only, `Lane: RED`. Push. Stop.

#### W16b: Builder (Opus)

- [ ] **Step 3.** Create the two record files verbatim; implement `SaveFromFormAsync` rules 1–5. **Do not edit tests.**
- [ ] **Step 4: Green.** Filter → all pass; full suite `Failed: 0` including `UnitOfWorkReadScopeTests`.
- [ ] **Step 5.** Exit checklist; AC matrix (REQ-SFT-15/18/19/21/22/23/25/26/27/41/46/51/52). Commit: `feat(song-service): SaveFromFormAsync holds every Song-form Save decision (REQ-SFT-51)`.

**Pre-planned split (if the Builder exceeds 2 h):** stop at a green commit after rules 1–2 (`feat(song-service): SaveFromFormAsync rules 1-2`), checkpoint; the orchestrator dispatches rules 3–5 on the same branch (Opus). Merge only after both halves.

**Merge risk:** High (constitutional boundary; CATALOG-PROP shares the file). Rollback: `git revert -m 1 <merge-sha>` (no caller until 5.3).

---

## Phase 4: Services (search)

### Task 4.1a: Deezer plain query + failure propagation [W5; G1 done; after 1.6]

**Model:** Sonnet. **Level:** B. **Review lane:** Standard. **Estimate:** 60 min.

**Files:** `Services/IMusicMetadataProvider.cs` (contract doc: `SearchSongsAsync`/`SearchArtistsAsync` **throw** on transport failure, bad JSON, timeout and a Deezer error body; the `artistHint` parameter stays in the signature and is ignored — removing it would break `MusicBrainzProvider` before 4.1b), `Services/MusicProviderException.cs` (new), `Services/DeezerProvider.cs`, `MyVocaList.Tests/Unit/Services/DeezerProviderTests.cs` (new; fake `HttpMessageHandler` + `HttpClient { BaseAddress = new("https://api.deezer.com/") }`)

- [ ] **Step 1: Tests.**

| Test | `// [AC]` | Asserts |
|---|---|---|
| `SearchSongsAsync_SendsPlainQueryWithoutQualifiers` | REQ-SFT-35 | request URI path+query = `search/track?q=dont%20cry` (no `track:`/`artist:`), even when an artist hint is passed |
| `SearchArtistsAsync_SendsPlainArtistQuery` | REQ-SFT-47 | `search/artist?q=metalica` |
| `SearchSongsAsync_Http500_Throws` | REQ-SFT-06 | `HttpRequestException` |
| `SearchSongsAsync_MalformedJson_Throws` | REQ-SFT-06 | `JsonException` (or `MusicProviderException` wrapping it; assert the chosen type) |
| `SearchSongsAsync_Http200WithErrorObject_ThrowsMusicProviderException` | REQ-SFT-06 | body `{"error":{"type":"Exception","message":"Quota limit exceeded","code":4}}` |
| `SearchSongsAsync_CallerCancelled_PropagatesOperationCanceled` | REQ-SFT-09 | `OperationCanceledException` |
| `SearchSongsAsync_EmptyData_ReturnsEmpty` | REQ-SFT-06 | `{"data":[]}` → empty, no throw |
| `SearchSongsAsync_MapsIdTitleArtistName` | REQ-SFT-12 | `id`, full `title` (e.g. "Don't Cry (Original)"), `artist.name` |

- [ ] Red (today every failure returns empty, `DeezerProvider.cs:60-64`; the URL has qualifiers `:47-50`) → commit tests `Lane: RED` → Green → suite `Failed: 0` (the BUG-082 DI test stays green) → commit: `feat(deezer): plain query; failures throw (HTTP, JSON, error body) (REQ-SFT-06/35)`.

### Task 4.1b: Delete `MusicBrainzProvider` (Q1 = drop) [W6, after 4.1a]

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 20 min. **Demo:** build 0 errors; the BUG-082 DI test passes with Deezer only.

**Files:** delete `Services/MusicBrainzProvider.cs`; modify `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (delete `AddHttpClient<MusicBrainzProvider>` `:34-38` and the `IMusicMetadataProvider` forward `:48`); create `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs` (§ 0.3 D18; consumed by 6.1):

```csharp
public sealed record ArtistRemoteSearchResult(IReadOnlyList<ArtistSuggestionDto> Remote, bool RemoteUnavailable);
```

- [ ] Census `['MusicBrainzProvider']` → only the 2 owned files (the suggestion services use the provider **name** string, not the type). Delete/edit → build → suite `Failed: 0` (`AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient` passes with Deezer only; `ServicesDirectory_IsFoundAndNonEmpty` ≥ 25: record the count) → census ZERO HITS. Commit: `refactor(providers): delete MusicBrainzProvider — Deezer is the only remote provider (REQ-SFT-36)`.

### Task 4.2: `SongSuggestionService` local and remote title search [W14; after 2.2 + 3.9 + 4.1b]

**Model:** Tester Sonnet · Builder Sonnet. **Level:** A. **Review lane:** Elevated. **Estimate:** 60 + 90 min.

**Files:** `Services/ISongSuggestionService.cs` (add the two methods; **keep** `GetLocalAsync`/`GetRemoteAsync` until 4.3), `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (add `Version` ("" for remote) and `LocalArtistName`), `Contracts/DTOs/Suggestions/SongRemoteSearchResult.cs` (new), `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`. `SearchConstants.RemoteSearchTimeoutMilliseconds` already exists (added in 2.2, § 0.3 D16).

**Interfaces — Produces (`design.md` § 3.3 verbatim):**

```csharp
public sealed record SongRemoteSearchResult(
    IReadOnlyList<SongSuggestionDto> Promoted, IReadOnlyList<SongSuggestionDto> Remote, bool RemoteUnavailable);
Task<IReadOnlyList<SongSuggestionDto>> SearchTitleLocalAsync(string query, CancellationToken ct = default);
Task<SongRemoteSearchResult> SearchTitleRemoteAsync(string query, IReadOnlyCollection<int> shownLocalSongIds, CancellationToken ct = default);
```

Local: `NormalizeSearchQuery`; < 2 → `[]`; `ExecuteReadAsync` → `SearchTitleCandidatesAsync(q, SimilarityConstants.PoolSize)`; rank by `max(RankScore(q, title), RankScore(q, title + " " + artist))`, stable over DB order; take `MaxSearchResults`. Remote: < 3 → empty, no call; Deezer only, **outside** any UoW lambda, linked CTS `CancelAfter(SearchConstants.RemoteSearchTimeoutMilliseconds)`; caller cancellation rethrows; any other exception/timeout → `RemoteUnavailable = true` + Warning log; dedup in one `ExecuteReadAsync` (identity, then collated (title, artist, "")) → `Promoted` if not in `shownLocalSongIds`, dropped from `Remote` either way; remaining rows resolve `LocalArtistId`/`LocalArtistName` by collated name in the same read. Tier (c) similarity dedup and the `OrdinalIgnoreCase` re-filters (`:130-175`, `:162`, `:204`) are deleted.

**Superseded tests (`design.md` § 10.1):**

| Test | Action | Superseded AC → replacing REQ |
|---|---|---|
| `SongSuggestionServiceTests.cs:35`, `:57`, `:119` | rewrite against `SearchTitleLocalAsync`/`SearchTitleRemoteAsync` | REQ-FORMUX-03/05 → REQ-SFT-05/06 |
| `:94` (similarity dedup) | rewrite: dedup is artist- and version-scoped (`SearchTitleRemoteAsync_OtherArtistSameTitle_StaysRemote`) | REQ-FORMUX-03(c) → REQ-SFT-05 |
| `:145` (offline) | rewrite: `RemoteUnavailable = true`, local still returned | REQ-FORMUX-05 → REQ-SFT-06 |
| `:76` (artist hint) | delete | REQ-SFT-35 |
| `:170` (MusicBrainz → Deezer fallback) | delete (Q1 = drop) | AC-4.2 provider order → REQ-SFT-36 |

- [ ] **Step 1 (Tester): new tests.** Use the **real** `SimilarityScorer` for ranking tests (it is a pure class) and mocks for repositories/UoW/provider.

| Test | `// [AC]` |
|---|---|
| `SearchTitleLocalAsync_BelowTwoTrimmedChars_ReturnsEmptyNoRepositoryCall` | REQ-SFT-03 |
| `SearchTitleLocalAsync_DontCry_DontCryFirst` (repo returns bm25 order: Dont Look Back…, Don't Cry, Boys Don't Cry, Cry, Crying) | REQ-SFT-30 |
| `SearchTitleLocalAsync_DonCry_DontCryFirst` | REQ-SFT-30 |
| `SearchTitleLocalAsync_GunsCry_DontCryFirstViaArtist` | REQ-SFT-32 |
| `SearchTitleLocalAsync_TakesAtMostFive` | REQ-SFT-04 |
| `SearchTitleLocalAsync_MakesNoProviderCall` | REQ-SFT-07 |
| `SearchTitleLocalAsync_MapsVersionAndArtist` | REQ-SFT-04 |
| `SearchTitleRemoteAsync_BelowThreeTrimmedChars_NoProviderCall` | REQ-SFT-03 |
| `SearchTitleRemoteAsync_ExternalIdentityMatchNotShown_Promoted` | REQ-SFT-05 |
| `SearchTitleRemoteAsync_CollatedTitleArtistMatch_Promoted` | REQ-SFT-05 |
| `SearchTitleRemoteAsync_MatchAlreadyShownLocally_DroppedNotPromoted` | REQ-SFT-05 |
| `SearchTitleRemoteAsync_OtherArtistSameTitle_StaysRemote` | REQ-SFT-05 |
| `SearchTitleRemoteAsync_ProviderThrows_RemoteUnavailable_LogsWarning` | REQ-SFT-06 |
| `SearchTitleRemoteAsync_ProviderSlowerThanBound_RemoteUnavailable` (provider awaits `Task.Delay(Timeout.Infinite, ct)`; test ≈ 3 s) | REQ-SFT-08 |
| `SearchTitleRemoteAsync_CallerCancelled_Rethrows` | REQ-SFT-09 |
| `SearchTitleRemoteAsync_ZeroResults_NotUnavailable` | REQ-SFT-06 |
| `SearchTitleRemoteAsync_RemoteRowArtistIsLocal_CarriesLocalArtistIdAndName` | REQ-SFT-13 |
| `SearchTitleRemoteAsync_QueriesDeezerOnlyWithoutHint` | REQ-SFT-35/36 |
| `SearchTitleRemoteAsync_TakesAtMostFiveRemote` | REQ-SFT-04 |

- [ ] **Step 2 (Tester): Red** (`CS1061`), commit tests only `Lane: RED`, stop.
- [ ] **Step 3 (Builder): Green**; apply the superseded table (the Builder deletes/rewrites only the rows listed there, which the Tester already rewrote as new tests; the Builder only deletes the listed obsolete ones and records it). Suite `Failed: 0` including `UnitOfWorkReadScopeTests` (no provider inside `ExecuteReadAsync`). Commit: `feat(song-suggestions): local FTS + ranked title search; Deezer remote with dedup and failure status (REQ-SFT-02..09/30-32)`.

**Merge risk:** High (core behaviour); service still unregistered until 4.3.

### Task 4.3: Retarget threshold tests; register the service [W15, after 4.2]

**Model:** Sonnet. **Level:** C (+ retargeted tests). **Review lane:** Standard. **Estimate:** 45 min.

**Files:** `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs` (6 tests retargeted to `SearchTitleLocalAsync`/`SearchTitleRemoteAsync`, same assertions, REQ-UOW-51/52 kept), `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (`services.AddScoped<ISongSuggestionService, SongSuggestionService>();`), `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`, `Services/ISongSuggestionService.cs` + `Services/SongSuggestionService.cs` (delete `GetLocalAsync`/`GetRemoteAsync`, § 0.3 D12)

- [ ] Add `AddAppServices_ResolvingSongSuggestionService_Succeeds` (`// [AC] REQ-SFT-02: the title search service resolves from AddAppServices`). Red (`Unable to resolve service`) → register → Green. Retarget the 6 threshold tests (record old → new name). Census `['GetLocalAsync','GetRemoteAsync']` on `ISongSuggestionService.cs`/`SongSuggestionService.cs` → zero (the artist service keeps its own until 6.1). Suite `Failed: 0`. Commit: `feat(di): register ISongSuggestionService; retarget threshold tests (REQ-SFT-02, REQ-UOW-51/52)`.

---

## Phase 5: Song form (ViewModel tasks before ACF T4; page tasks after)

### Task 5.1: VM: resolution sheet → Open existing; merge sheet removed [W3, after 1.7]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** Elevated. **Estimate:** 90 min.

**Files:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (includes the nested `MergeFieldRow`), `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (delete the merge sheet `:353-410`; relabel the resolution sheet actions "Open existing" / "Save as new version"), `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` (§ 0.3 D13)

**Produces:** `INavigationService` constructor dependency; `OpenExistingSongCommand(SongMatch)` navigating `$"../{Routes.SongForm}?songId={id}"`; the VM no longer references `CommitAsync`, `ResolutionChoice`, `FieldDiff`, `MergeFieldRow`, `TargetHasManualEdits`, `ConfirmUpdateExistingCommand`, `SelectResolutionCandidateCommand`, `IsMergeSheetVisible`, `MergeFieldRows`, `ConfirmMergeCommand`, `DismissMergeSheetCommand`, `_selectedMergeTargetId`. (The rest of the Save flow is replaced again in 5.3.)

**Superseded tests:** `SongFormViewModelTests.cs:707` `ConfirmUpdateExisting_TargetHasManualEdits_PopulatesMergeRows` → delete; AC-4.2 → REQ-SFT-38. Every test of `ConfirmUpdateExistingCommand`/`SelectResolutionCandidateCommand` → rewrite to `OpenExistingSongCommand` (list them), AC-1.1 "Update existing" → REQ-SFT-22/24.

- [ ] **Step 1: Red.**

```csharp
[Fact]
// [AC] REQ-SFT-22: Open existing on the resolution sheet opens the stored song's edit form in place of the add form.
public async Task OpenExistingSong_FromResolutionSheet_NavigatesToEditFormReplacingAddForm()
{
    var sut = CreateSut();                       // CreateSut now passes _navigationMock.Object
    var match = new SongMatch(77, "Creep", "", "Radiohead", 1.0);

    await sut.OpenExistingSongCommand.ExecuteAsync(match);

    _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=77"), Times.Once);   // match the real INavigationService signature
    Assert.False(sut.IsResolutionSheetVisible);
}
```

Expected Red: `CS1061 … 'OpenExistingSongCommand'`. Commit test only `Lane: RED`.
- [ ] **Step 2: Green.** VM first (build), then `SongFormPage.xaml` (build), then the DI test mock line (build), then the superseded tests. Full suite `Failed: 0`.
- [ ] **Step 3.** Census `['CommitAsync(','ResolutionChoice','FieldDiff','MergeFieldRow','TargetHasManualEdits','ConfirmUpdateExisting','IsMergeSheetVisible']` on `SongFormViewModel.cs`, `SongFormPage.xaml`, `SongFormViewModelTests.cs` → ZERO HITS (3.3 depends on this).
- [ ] **Step 4.** Emulator smoke: saving a duplicate by hand offers "Open existing", which opens the stored song. Exit checklist. Commit: `feat(song-form): resolution sheet offers Open existing; merge sheet removed (REQ-SFT-22/24/38)`.

### Task 5.2: VM: title search, picks, online-search note [W16; after 4.3 + 3.5b]

**Model:** Tester Sonnet · Builder Sonnet. **Level:** A. **Review lane:** Elevated. **Estimate:** 60 + 90 min.

**Files:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`

**Interfaces — Produces (`design.md` § 4, with § 0.3 D1 fallback):**

```csharp
public IMenuOptionSource TitleOptionSource { get; }   // new MenuOptionSource(SearchTitleOptionsAsync)
[ObservableProperty] string titleSupportingText = "";  // → Field SupportingText
public bool IsTitleSearchEnabled => !IsEditMode;        // REQ-SFT-27 (edit-mode tests in 5.4)
[ObservableProperty] bool isTitleLocked;
IAsyncRelayCommand<AutocompleteSuggestion> SelectTitleCommand;   // local → OpenExistingAsync (REQ-SFT-10/11); remote → ApplyRemotePick (REQ-SFT-12/13)
IRelayCommand ClearTitleCommand;                                 // clears pending identity + TitleSupportingText (REQ-SFT-06/14)
public IMenuOptionSource ArtistOptionSource { get; }  // local artists only, over the VM's existing artist search (REQ-SFT-16, § 0.3 D19)
bool IsDiscardSheetVisible; IAsyncRelayCommand ConfirmOpenExistingCommand; IRelayCommand KeepEditingCommand;   // REQ-SFT-11
// ISongSuggestionService added to the constructor.
private async Task<IReadOnlyList<AutocompleteSuggestion>> SearchTitleOptionsAsync(string query, CancellationToken ct)
{
    var local = await _suggestions.SearchTitleLocalAsync(query, ct);
    var remote = await _suggestions.SearchTitleRemoteAsync(query, local.Select(l => l.LocalSongId!.Value).ToList(), ct);
    ct.ThrowIfCancellationRequested();                         // a stale query never touches the note (Review Focus 4)
    if (query.Trim().Length >= SearchConstants.MinimumRemoteQueryLength)   // below 3 chars no remote call ran: keep the note as it is (REQ-SFT-06)
    {
        var note = remote.RemoteUnavailable ? "Online search unavailable" : "";
        RunOnUiThread(() => TitleSupportingText = note);       // the VM's existing UI-thread helper; inline in tests
    }
    return [.. local.Select(ToLocalOption), .. remote.Promoted.Select(ToLocalOption), .. remote.Remote.Select(ToRemoteOption)];
}
```

(Use the DTO's real id member names; the Tester reads `SongSuggestionDto`. `RunOnUiThread` stands for the dispatch helper the VM already uses for UI-bound state (`code-style-reference.md § UI Thread Performance`); the option source runs on DX's request thread, so the observable is never set off the UI thread. If the VM has no such helper, use the injected dispatcher abstraction the VM tests already fake; never `MainThread` directly in VM code under test.)

- [ ] **Step 1 (Tester): tests.** `ToLocalOption`: Headline = title, SupportingText = `"artist · version · In your library"` (omit ` · version` when empty); `ToRemoteOption`: `"artist · Deezer"`.

| Test | `// [AC]` |
|---|---|
| `TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker` | REQ-SFT-04 |
| `TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart` | REQ-SFT-04 |
| `TitleOptionSource_RemoteHit_SupportingTextIsArtistAndDeezer` | REQ-SFT-04 |
| `TitleOptionSource_OrdersLocalThenPromotedThenRemote` | REQ-SFT-05/07 |
| `TitleOptionSource_PassesShownLocalIdsAndToken` | REQ-SFT-05/09 |
| `TitleOptionSource_RemoteUnavailable_SetsTitleSupportingText` | REQ-SFT-06 |
| `TitleOptionSource_RemoteSucceedsAfterFailure_ClearsTitleSupportingText` | REQ-SFT-06 |
| `TitleOptionSource_CancelledQuery_DoesNotChangeTitleSupportingText` | REQ-SFT-09 (Review Focus 4) |
| `TitleOptionSource_QueryBelowRemoteThreshold_KeepsExistingNote` (note set by a failed "don" query; then "do" → note unchanged) | REQ-SFT-06 |
| `TitleOptionSource_SetsNoteThroughUiThreadHelper` (the fake dispatcher records one invocation) | REQ-SFT-06 |
| `ClearTitle_EmptiesNote_AndClearsPendingIdentity_KeepsArtist` | REQ-SFT-06/14 |
| `SelectTitle_LocalOptionNoOtherInput_NavigatesToEditForm` | REQ-SFT-10 |
| `SelectTitle_LocalOptionWithOtherInput_ShowsDiscardSheetNoNavigation` (Theory over which field holds input: Artist, Version, Featured, Lyrics, YouTube list) | REQ-SFT-11 |
| `ConfirmOpenExisting_FromDiscardSheet_Navigates` | REQ-SFT-11 |
| `KeepEditing_RestoresTypedQueryUnlocked_NothingElseChanges` | REQ-SFT-11 |
| `SelectTitle_RemoteOption_FillsTitleAndArtistAndPendingIdentity_WritesNothing` | REQ-SFT-12 |
| `SelectTitle_RemoteOptionArtistIsLocal_LocksLocalArtist` | REQ-SFT-13 |
| `SelectTitle_RemoteOptionArtistNotLocal_LocksTypedArtistWithNoId` | REQ-SFT-13 |
| `RemotePick_ClosesDropdowns` (replaces the deleted `:486/:509`) | REQ-SFT-12 |
| `ClearArtist_AfterRemotePick_ClearsPendingIdentity_KeepsTitle` | REQ-SFT-14 |
| `ArtistOptionSource_ReturnsLocalArtistsOnly_NoRemoteCall` | REQ-SFT-16 |

- [ ] **Step 2 (Tester): Red** (`CS1061`), commit tests only `Lane: RED`, stop.
- [ ] **Step 3 (Builder): Green.** Suite `Failed: 0` (`AddAppServices_ResolvingSongFormViewModelGraph_Succeeds` green: `ISongSuggestionService` registered in 4.3). Commit: `feat(song-form): title option source (local + Deezer), picks, online-search note (REQ-SFT-02..14)`.

### Task 5.3: VM: Save maps service outcomes to sheets [W17; after 5.2 + 3.10]

**Model:** Tester Sonnet · Builder Sonnet. **Level:** A. **Review lane:** Architectural. **Estimate:** 60 + 90 min.

**Files:** `SongFormViewModel.cs`, `SongFormViewModelTests.cs`

**Produces:** `SaveCommand` builds `SongFormSaveRequest` and maps `SongFormSaveOutcome`: `Saved` → snackbar + back; `Failed` → error on `FailedField`; `ArtistChoiceNeeded` → artist-match sheet (`IsArtistMatchSheetVisible`, `ArtistMatchCandidates`, `UseExistingArtistCommand(ArtistMatch)`, `CreateTypedArtistCommand`, `DismissArtistMatchSheetCommand`); `SongChoiceNeeded` → resolution sheet (`ResolutionCandidates`, `IsExactDuplicate`, `OpenExistingSongCommand`, `ConfirmSaveAsNewVersionCommand`, `ConfirmSaveAsNewSongCommand`, `DismissResolutionSheetCommand`). Sheet actions re-invoke `SaveFromFormAsync` with the decision. `ISongResolutionService` **removed** from the constructor; `ExecuteNewSongSaveAsync`/`ExecuteEditSaveAsync`/`CommitNewSongAsync`, `_pendingCandidate`/`_pendingResolution` deleted. After 5.3 the VM calls neither `CreateSongWithUrlsAsync` nor `UpdateSongAsync`. Remove the now-unused `ISongResolutionService` mock and `AddTransient`-graph dependency in the VM tests' `CreateSut`.

**Superseded / rewritten tests (`design.md` § 10.1):**

| Test | Action | Superseded AC → replacing REQ |
|---|---|---|
| `:174` `SaveAsync_NoMatch_CallsCreateSongWithUrls` | rewrite → `Save_OutcomeSaved_ShowsSnackbarAndNavigatesBack` | rule moved to the Service (REQ-SFT-51) |
| `:208` (resolution sheet shown) | rewrite → `Save_OutcomeSongChoiceNeededExact_ShowsResolutionSheetExact` | REQ-SFT-22/51 |
| `:238` (resolution sheet dismissed) | rewrite → `DismissResolutionSheet_NothingSaved` | REQ-SFT-22/51 |
| `:128` `SaveAsync_ServiceThrows_ShowsErrorSnackbar` | rewrite → `Save_ServiceThrows_ShowsErrorSnackbar` (`SaveFromFormAsync` throws) | AC-B5 kept → REQ-SFT-51 |
| `:153` `SaveAsync_NoArtist_SetsArtistError_NoServiceCall` | rewrite → covered by `Save_OutcomeFailed_SetsErrorOnFailedField(Artist)`: the required-artist rule is now the Service's rule 1; the VM maps `Failed(Artist)` | AC-B5 / AC-10.4 → REQ-SFT-21/51 |
| `:816` `SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion` | rewrite → `EditSave_RequestCarriesHydratedFieldsAndEditedVersion` (asserts the `SongFormSaveRequest`) | BUG-024 kept → REQ-SFT-51 |
| `:860` `SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService` | rewrite → `EditSave_ArtistChanged_RequestCarriesNewArtistId` | REQ-ACREATE-16 → REQ-SFT-28 |
| `:1005` `SaveAsync_TitleNeverDirty_SafetyNetSetsTitleError` | rewrite → `Save_OutcomeFailedOnTitle_SetsTitleErrorEvenIfNeverDirty` (the safety net is the Service's rule 1; the VM maps `Failed(Title)`) | safety net → REQ-SFT-15/51 |
| `:1132` `SaveCommand_DoubleInvokedWhileSaving_CallsResolveOnlyOnce` | rewrite → `Save_DoubleTapWhilePending_CallsServiceOnce` (below) | BUG-049 kept → REQ-SFT-51 |
| `:1270` `SaveAsync_EditMode_AfterHydration_PreservesArtistLink` | rewrite → `EditSave_AfterHydration_RequestCarriesHydratedArtistId` | REQ-SFT-28 |
| any other remaining mock of `CreateSongWithUrlsAsync`/`UpdateSongAsync`/`ISongResolutionService` in VM tests (census before and after) | rewrite to `SaveFromFormAsync` or delete; list each by name and line | REQ-SFT-51 |

- [ ] **Step 1 (Tester): tests.**

| Test | `// [AC]` |
|---|---|
| `Save_BuildsRequestFromFormState_CallsSaveFromFormAsyncOnce` | REQ-SFT-51 |
| `Save_OutcomeSaved_ShowsSnackbarAndNavigatesBack` | REQ-SFT-51 |
| `Save_OutcomeFailed_SetsErrorOnFailedField` (Theory over `SaveField.Title`, `.Version`, `.Artist` — public enum, allowed as a parameter) | REQ-SFT-20/21 |
| `Save_OutcomeArtistChoiceNeeded_ShowsArtistMatchSheetWithCandidates` | REQ-SFT-19 |
| `UseExistingArtist_ReinvokesWithUseExistingDecision` | REQ-SFT-19 |
| `CreateTypedArtist_ReinvokesWithCreateTypedDecision` | REQ-SFT-19 |
| `DismissArtistMatchSheet_DoesNotReinvoke_FormUnchanged` | REQ-SFT-19 |
| `Save_OutcomeSongChoiceNeededExact_ShowsResolutionSheetExact` | REQ-SFT-22 |
| `Save_OutcomeSongChoiceNeededFuzzy_ShowsCandidateRows` | REQ-SFT-23 |
| `ConfirmSaveAsNewVersion_ReinvokesWithSaveAsNewVersion` | REQ-SFT-22 |
| `ConfirmSaveAsNewSong_ReinvokesWithSaveAsNewSong` | REQ-SFT-23 |
| `DismissResolutionSheet_NothingSaved` | REQ-SFT-22 |
| `Save_NeverCallsCreateOrUpdateDirectly` | REQ-SFT-46 |
| `Save_DoubleTapWhilePending_CallsServiceOnce` (rewrite of `:1132`; a `TaskCompletionSource` gate; two `ExecuteAsync` calls) | REQ-SFT-51 (Review Focus 5) |

- [ ] **Step 2 (Tester): Red** (`CS1061` / Moq verify failures), commit `Lane: RED`, stop.
- [ ] **Step 3 (Builder): Green.** Census `['ISongResolutionService','CreateSongWithUrlsAsync','UpdateSongAsync','CommitNewSongAsync','ExecuteNewSongSaveAsync']` on `SongFormViewModel.cs` → ZERO HITS. Suite `Failed: 0`. Commit: `feat(song-form): Save maps SaveFromFormAsync outcomes to sheets; VM decides nothing (REQ-SFT-19/22/23/46/51)`.

### Task 5.3b: Delete the forwarding `CreateSongWithUrlsAsync(int, …)` overload [W18; G3] *(new, § 0.3 D3)*

**Model:** Haiku. **Level:** C. **Review lane:** Standard. **Estimate:** 15 min. **Demo:** build 0 errors with 0 `CS0618`; all tests green.

**Files:** `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`

- [ ] Census for callers of the old 9-parameter overload (`CS0618` warnings list from the build, plus a Python scan for `CreateSongWithUrlsAsync(` followed by a non-null first argument in `.cs` files) → none outside the two owned files. Delete it → build 0 errors, **0 `CS0618`** → suite `Failed: 0`. Commit: `refactor(song-service): delete obsolete CreateSongWithUrlsAsync(int artistId, …) overload (REQ-SFT-46)`.

### Task 5.4: VM: edit-mode title and hydration [W18, after 5.3]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** Standard. **Estimate:** 45 min.

**Files:** `SongFormViewModel.cs` (also rewrite the stale docstring at `:433` to describe the current load behaviour), `SongFormViewModelTests.cs`

| Test | `// [AC]` |
|---|---|
| `EditMode_IsTitleSearchEnabled_False` | REQ-SFT-27 |
| `AddMode_IsTitleSearchEnabled_True` | REQ-SFT-02 |
| `EditSave_ApiSourcedSong_EditedTitle_RequestCarriesTitleAndNullIdentity` | REQ-SFT-27 |
| `EditSave_ManualSong_EditedTitle_RequestCarriesTitle` | REQ-SFT-27 |
| `EditMode_TitleNotLocked_AfterLoad` (Title is Editable, never read-only) | REQ-SFT-27 |

(The `:787` rewrite moved to 3.2b, § 0.3 D6.) Red → commit `Lane: RED` → Green → suite → commit: `feat(song-form): Title editable with search off in edit mode (REQ-SFT-27)`.

### GATE G2: ACF T4 merged

> **Do not dispatch 5.5, 5.6 or 6.3** until the ACF item's T4 (`AutocompleteDXField` component, `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml(.cs)`) is merged into `develop` (check the LEDGER row AC-FIELD / ACF-T4 and `git log --oneline develop -- MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`). Recommended, not blocking: ACF T6/T7 passed first, so component defects surface on the simpler Person page.

### Task 5.5 (+ former 5.7): Page: field order, Title and Artist Fields, glue removed [W19]

**Model:** Sonnet. **Level:** C (XAML + code-behind). **Review lane:** Standard. **Estimate:** 75 min.

**Files:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (one XAML file; XAML → build → code-behind, as in ACF T6).

- [ ] **Step 1.** Invoke `myvocalist-coding` (`devexpress-patterns.md`, `m3-components.md`), `maui-current-apis`; read the ACF `design.md` § 3.1 surface table and ACF `findings.md` (verdict). `devexpress-maui` MCP only if a DX question arises.
- [ ] **Step 2.** Reorder to Title, Artist, Version, Featured Artists, Lyrics, then the YouTube section unchanged (REQ-SFT-01). Replace the Title `dxe:TextEdit` (`:87-88`) and the Artist `AutoCompleteEdit` (`:28-41`) with:

```xml
<textfields:AutocompleteDXField x:Name="titleField"
    LabelText="Title"
    Text="{Binding SongTitle, Mode=TwoWay}"
    IsLocked="{Binding IsTitleLocked, Mode=TwoWay}"
    IsSearchEnabled="{Binding IsTitleSearchEnabled}"
    SupportingText="{Binding TitleSupportingText}"
    HasError="{Binding TitleHasError}"
    ErrorText="{Binding TitleErrorText}"
    OptionSource="{Binding TitleOptionSource}"
    SelectCommand="{Binding SelectTitleCommand}"
    ClearCommand="{Binding ClearTitleCommand}" />
<textfields:AutocompleteDXField x:Name="artistField"
    LabelText="Artist"
    Text="{Binding ArtistSearchText, Mode=TwoWay}"
    IsLocked="{Binding IsArtistLocked, Mode=TwoWay}"
    HasError="{Binding ArtistHasError}"
    ErrorText="{Binding ArtistErrorText}"
    OptionSource="{Binding ArtistOptionSource}"
    SelectCommand="{Binding SelectArtistCommand}"
    ClearCommand="{Binding ClearArtistCommand}"
    BlurWithoutPickCommand="{Binding ArtistBlurredWithoutSelectionCommand}" />
```

Use the VM's real error-property names. `IsReadOnly` is **not** bound (REQ-SFT-27). Add `xmlns:textfields="clr-namespace:MyVocaList.UI.Components.TextFields"`. `ArtistOptionSource` is produced by Task 5.2.
- [ ] **Step 3. Build.** Expected: the **only** errors are in `SongFormPage.xaml.cs` (handlers and `artistEdit.SelectedItem` uses for the removed `AutoCompleteEdit`; `ContentView` has no `SelectedItem`). Any other error is fixed before Step 4 (3-attempt cap).
- [ ] **Step 4 (former Task 5.7, folded in, § 0.3 D17).** Edit `SongFormPage.xaml.cs`: delete `OnArtistItemsRequested`, `OnArtistSelectionChanged`, `OnArtistUnfocused`, `OnArtistClearIconClicked` and every `artistEdit.SelectedItem` use; initial focus calls `titleField.FocusEditor()`. Build → 0 errors; suite `Failed: 0`.
- [ ] **Step 5: REQ-ACF-11 check** (Python, not grep):

```bash
python -c "import pathlib,sys; x=pathlib.Path('MyVocaList/UI/Pages/Songs/SongFormPage.xaml').read_text(encoding='utf-8'); cs=pathlib.Path('MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs').read_text(encoding='utf-8'); bad=[t for t in ['OnArtistItemsRequested','OnArtistSelectionChanged','OnArtistUnfocused','OnArtistClearIconClicked','artistEdit.SelectedItem'] if t in cs]+[t for t in ['AutoCompleteEdit','RequestDelay','CharacterCountThreshold','Search music database'] if t in x]; print('FAIL',bad) if bad else print('PASS'); sys.exit(1 if bad else 0)"
```

Expected `PASS`; paste it into the task-log.
- [ ] **Step 6: E2E emulator gate** (`implementor.md`): New Song → Title first; type "don cry" → options; tap a local option → its edit form replaces the page; Edit Song → Title editable with no menu. If no emulator: `Check build` with the note. Exit checklist. Commit: `feat(song-form): Title first; Title and Artist on AutocompleteDXField; page glue removed (REQ-SFT-01/02/16/27, REQ-ACF-11)`.
- [ ] **Step 7 (orchestrator, at the 5.5 merge on develop).** `SongFormPage` is the **second consumer** of `AutocompleteDXField` (after ACF T6's `PersonFormPage`), so the component becomes governed (`component-change-governance.md` § Scope trigger; ACF `design.md` § 11). In the **same commit** as the 5.5 merge (a follow-up docs commit on develop immediately after the merge commit, before any other merge), add `AutocompleteDXField` to the governed-component list in `.claude/library/component-safety-gate.md`, with its consumers (`PersonFormPage`, `SongFormPage`; `ArtistFormPage` joins at 6.3). If ACF T6 has not merged yet, 5.5 is the first consumer and this step moves to whichever of 5.5/6.3/ACF T6 merges second.

**Merge risk:** Medium (first Song-page consumer of the component). Rollback: `git revert -m 1 <merge-sha>` restores the old page; the merged VM tasks stay (their new bindings are simply unused by the old XAML).

### Task 5.6: Page: sheets (discard, near-duplicate artist, resolution) [W20, after 5.5 merged]

**Model:** Sonnet. **Level:** C (XAML). **Review lane:** Standard. **Estimate:** 60 min.

**Files:** `SongFormPage.xaml` only.

- [ ] Add/adjust three `dx:BottomSheet`s bound to 5.2/5.3 members: discard (`IsDiscardSheetVisible`; text "Open '<title>'? What you entered here will be discarded."; actions **Open** / **Keep editing**); near-duplicate artist (`IsArtistMatchSheetVisible`; title "Similar artist found"; one row "Use '<existing>'" per `ArtistMatchCandidates` (≤ 3); action "Create '<typed>'"); resolution (`IsExactDuplicate` → **Open existing** / **Save as new version**; fuzzy → candidate rows (tap → `OpenExistingSongCommand`) + **Save as new song**). Back/scrim → the dismiss commands. Per `dialogs-validation.md`. Build → 0 errors; suite `Failed: 0`; re-run the 5.5 REQ-ACF-11 check → `PASS`.
- [ ] Emulator: each sheet opens from its trigger (discard: type an artist, then pick a local title; near-duplicate: "Metalica" with "Metallica" stored; resolution: save a duplicate) and Back dismisses it with no save. Commit: `feat(song-form): discard, near-duplicate artist and resolution sheets (REQ-SFT-11/19/22/23)`.

### Task 5.7: folded into 5.5 (§ 0.3 D17)

The orchestrator marks `tasks.md` 5.7 `[CANCELLED: folded into 5.5 — a XAML-only commit that removes the AutoCompleteEdit cannot build while the code-behind still references it]`.

---

## Phase 6: Artist form name search

### Task 6.1: `ArtistSuggestionService` local and remote name search; register [W17; after 4.1b + 4.3 + 3.9b]

**Model:** Tester Sonnet · Builder Sonnet. **Level:** A. **Review lane:** Elevated. **Estimate:** 45 + 60 min.

**Files:** `Services/IArtistSuggestionService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`, `MyVocaList.Tests/Unit/Services/ArtistSuggestionThresholdTests.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (`AddScoped<IArtistSuggestionService, ArtistSuggestionService>()`)

**Produces (`design.md` § 4.1 as updated 2026-09-26, § 0.3 D20):** `SearchNameLocalAsync(query, ct)` (< 2 trimmed chars → `[]`; else inside `ExecuteReadAsync` `IArtistRepository.SearchNameCandidatesAsync(q, SimilarityConstants.PoolSize, ct)` from 3.9b (FTS5 `ArtistSearch`, prefix fallback under 3 chars), ranked by `ISimilarityScorer.RankScore(q, name)` stable over DB order, take `MaxSearchResults` — **not** the collated prefix `SearchByNameAsync` at `ArtistRepository.cs:59-68`; ranking only, § 6.4 exception); `SearchNameRemoteAsync(query, ct) → ArtistRemoteSearchResult(IReadOnlyList<ArtistSuggestionDto> Remote, bool RemoteUnavailable)` (Deezer `search/artist?q=`, ≤ 5, `RemoteSearchTimeoutMilliseconds`, same failure handling; a Deezer name that is a collated match of a local artist is dropped, via a collated DB query inside one `ExecuteReadAsync`, never C#). Tier (c) and `FilterSimilar` deleted. Old `GetLocalAsync`/`GetRemoteAsync` deleted. Create `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs` (`public sealed record ArtistRemoteSearchResult(IReadOnlyList<ArtistSuggestionDto> Remote, bool RemoteUnavailable);`), mirroring `SongRemoteSearchResult` (6th file, § 0.3 D18).

**Superseded tests:** every `ArtistSuggestionServiceTests` test of tier (c) / `FilterSimilar` → delete; REQ-FORMUX-03(c) / -10 → REQ-SFT-47/48. `ArtistSuggestionThresholdTests` → retarget to the new methods (same assertions). List each in the task-log.

| Test | `// [AC]` |
|---|---|
| `SearchNameLocalAsync_BelowTwoTrimmedChars_ReturnsEmpty` | REQ-SFT-03 |
| `SearchNameLocalAsync_UsesFtsCandidatesRankedMaxFive` (mock `SearchNameCandidatesAsync`, real scorer; `SearchByNameAsync` never called) | REQ-SFT-47 |
| `SearchNameLocalAsync_Metalica_ReturnsLocalMetallicaFirst` | REQ-SFT-47 |
| `SearchNameRemoteAsync_DeezerMetallica_DroppedWhenLocalMetallicaExists` | REQ-SFT-48 |
| `SearchNameRemoteAsync_BelowThreeTrimmedChars_NoProviderCall` | REQ-SFT-03 |
| `SearchNameRemoteAsync_DeezerNameCollatedMatchOfLocal_Dropped` | REQ-SFT-48 |
| `SearchNameRemoteAsync_ProviderThrows_RemoteUnavailable` | REQ-SFT-06 |
| `SearchNameRemoteAsync_ProviderSlowerThanBound_RemoteUnavailable` | REQ-SFT-08 |
| `SearchNameRemoteAsync_CallerCancelled_Rethrows` | REQ-SFT-09 |
| `SearchNameRemoteAsync_ZeroResults_NotUnavailable` | REQ-SFT-06 |
| `AddAppServices_ResolvingArtistSuggestionService_Succeeds` (in `ArtistSuggestionServiceTests`, building `AddAppServices` like the DI test) | REQ-SFT-47 |

Red (Tester, `Lane: RED`) → Green (Builder) → census `['FilterSimilar']` ZERO HITS → suite → commit: `feat(artist-suggestions): local + Deezer name search with failure status; register (REQ-SFT-47/48)`.

### Task 6.2: `ArtistFormViewModel`: Name option source, local pick opens the artist, Deezer pick fills the name [W18; after 6.1 + 1.2]

**Model:** Sonnet (single agent, Red first). **Level:** A. **Review lane:** Standard. **Estimate:** 75 min.

**Files:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`

**Produces (`design.md` § 4.1):** `NameOptionSource` (local options SupportingText "In your library", then Deezer "Deezer"; one list, § 0.3 D1), `NameSupportingText`, `IsNameLocked`, `IsNameSearchEnabled => !IsEditMode`, `SelectNameCommand` (local → `INavigationService.GoToAsync($"../{Routes.ArtistForm}?artistId={id}&artistName={Uri.EscapeDataString(name)}")`; Deezer → fill `ArtistName`, lock), `ClearNameCommand`. Delete the `SelectedExternalId`/`SelectedProvider` stash (`:30-31`, `:188-198`) (Q10 = not stored). Save unchanged: `CreateArtistAsync(name)` with no external identity. **Constructor:** add `IArtistSuggestionService` (registered in 6.1) explicitly as a constructor dependency, and update the test file's `CreateSut` with its mock. Add `INavigationService` to the constructor if absent.

| Test | `// [AC]` |
|---|---|
| `NameOptionSource_OrdersLocalThenDeezer_WithSupportingTexts` | REQ-SFT-47 |
| `NameOptionSource_RemoteUnavailable_SetsNameSupportingText` | REQ-SFT-47 |
| `SelectName_LocalOption_NavigatesToArtistEditForm` | REQ-SFT-48 |
| `SelectName_DeezerOption_FillsNameAndLocks_WritesNothing` | REQ-SFT-49 |
| `ClearName_EmptiesAndUnlocks` | REQ-SFT-49 |
| `Save_AfterDeezerPick_CreatesArtistWithoutExternalIdentity` | REQ-SFT-49 |
| `Save_TypedCollatedDuplicate_ShowsExistingMessageOnName` | REQ-SFT-50 |
| `EditMode_IsNameSearchEnabled_False` | REQ-SFT-47 |

Red → `Lane: RED` → Green → suite → commit: `feat(artist-form): Name option source; local pick opens the artist; Deezer pick fills the name (REQ-SFT-47..50)`.

### Task 6.3: `ArtistFormPage`: Name as the Field [W19; after 6.2 + ACF T4]

**Model:** Sonnet. **Level:** C. **Review lane:** Standard. **Estimate:** 60 min.

**Files:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `ArtistFormPage.xaml.cs`

- [ ] Replace the Name field with `textfields:AutocompleteDXField` bound to `ArtistName` (TwoWay), `IsNameLocked` (TwoWay), `IsNameSearchEnabled`, `NameSupportingText`, `NameOptionSource`, `SelectNameCommand`, `ClearNameCommand`, and the VM's name error members. Build (XAML) → then delete any old name-autocomplete glue in the code-behind → build 0 errors → suite `Failed: 0`. The duplicate-suggestions stub (`:46-51`) is out of scope, untouched. Emulator: New Artist, type "metalica" → local "Metallica" (if stored) then Deezer rows; tap "Metallica" → its edit form. Commit: `feat(artist-form): Name on AutocompleteDXField (REQ-SFT-47..49)`.

---

## Phase 7: Close-out

### Task 7.1: Spec ritual + traceability matrix (orchestrator)

- [ ] Fill `requirements.md` § Traceability seed with the real test names (from the task-log AC matrices); every REQ-SFT-01…52 (except the removed race AC) has ≥ 1 test or a recorded Level-C census/E2E step.
- [ ] Session-end spec ritual: `> **Spec updated YYYY-MM-DD:**` notes where the build differs from the spec (the § 0.3 deviations are already noted). Follow-ups registered with `backlog_gen.py register` (not hand-edited): `CreateSongAsync` decision (`tasks.md` § Follow-ups), frozen `UI/Components/AutocompleteField/` deletion. (No append-capability follow-up: D1 is settled.)
- [ ] README frontmatter `status:`/`gate:`; `backlog_gen.py regen`; `.sln` check (every new doc registered). Rebuild Test (`spec-writing-guide.md § rebuild test`). Commit on develop.

### Task 7.2: On-device E2E gate (Helder, Android), HARD STOP

> **HARD STOP.** Nothing is declared done before Helder's pass. The item does not move to ✅ until Helder has run every line below on his device and given his verdict. The orchestrator records pass/fail per line in the `## 7.2 — Device gate` section of `task-log.md`, then sets the status (`backlog_gen.py status … "✅"`, or back to 🟡 with each failing line registered as a bug via `backlog_gen.py register --kind bug` **before** any fix).

**Deploy:** from `develop` (main working tree), device on USB: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run`. Line 1 needs an **existing install with data** from before M1 (install the pre-item build first, add songs/artists/YouTube links, then deploy this build over it without uninstalling).

**Checklist (from `tasks.md` 7.2, renumbered 1–12 in order — `tasks.md` has line 12 between 8 and 9 — plus the BUG-082 confirmation line B82):**

- **B82. BUG-082 on device:** on New Song, type "sweet child" → at least one option marked "Deezer" with a real track title appears (the providers resolve with their configured `HttpClient`; BUG-082 `fa520e3f`). Repeat on New Artist with "metallica" → at least one "Deezer" row.
1. Upgrade an existing install: songs, artists and YouTube links intact (M1–M3).
2. New Song: Title is first. Type "don cry" → "Don't Cry" local first, "Guns N' Roses · In your library"; version rows distinguishable; local options appear before Deezer's.
3. Tap the local row → the edit form of that song opens; Back returns to the Songs list.
4. Type "sweet chld o mine" → Deezer rows marked "Deezer". Pick one not in the library → Title and Artist filled and locked; X on Title empties it.
5. Pick a Deezer song whose artist is not local → Save → song and artist both created; artist's catalog behaviour per CATALOG-PROP if landed.
6. Airplane mode → local options still appear, and the text under Title reads "Online search unavailable"; Save works.
7. Type "Metalica" as artist (with "Metallica" stored) → Save → near-duplicate sheet offers "Use 'Metallica'"; Use and Create both behave; Back saves nothing. Repeat with a Deezer pick whose artist is a near-duplicate of a local one.
8. Edit an imported song: fix its Title → saves; reopen → new title, still linked to Deezer (same YouTube links). Edit a manual song's Title to a title that artist already has with the same Version → duplicate message. Change Version of a title that exists → saves.
9. Type an existing artist's name in another case/accent ("metallica") → Save → the song attaches to "Metallica"; no second artist appears in the Artists list.
10. Save a duplicate by hand → Open existing / Save as new version.
11. No "Search music database" row on Song or Artist forms; "Search YouTube (API)" unchanged.
12. New Artist: type "metalica" → local "Metallica" first (FTS5 artist search, § 0.3 D20; passes by design); tap it → its edit form opens. Pick a Deezer artist → Name filled and locked; Save creates it; a collated duplicate shows the existing message.

Note for lines 2 and 6 (§ 0.3 D1, settled): local options appear together with Deezer's once Deezer answers, at most 3,000 ms after typing stops ("local options appear before Deezer's" is satisfied by the order within the list, not by timing). This is the approved Q16 behaviour, not a defect.

- [ ] Orchestrator: record results; commit on develop `docs(sft): 7.2 device gate — <PASS|FAIL lines n,m>`; LEDGER; status.

**Risk:** High (the only check of the full flow on a real device).

---

## Model routing and estimates

| Task | Model | Estimate | Review lane | Why this model |
|------|-------|----------|-------------|----------------|
| Step 0 | Orchestrator | 45 min | — | Docs only |
| G5a spike page | Sonnet | 45 min | Architectural (via G5) | Fully specified probe |
| G5b device run | Helder | ≤ 1 h (time-box) | — | On-device |
| G5c verdict | Orchestrator | 15 min | — | Docs only |
| 1.1 / 1.2 | Sonnet / Sonnet | 45 / 45 min | Standard | Removal across VM + XAML + tests |
| 1.3 / 1.4 / 1.5 / 1.6 | Haiku | 20 min each | Standard | Mechanical deletions with census |
| 1.7 | Sonnet | 60 min | Standard | One Red rewrite + deletions |
| 2.1 | Sonnet | 60 min | Standard | Two collated queries |
| 2.2 | Sonnet | 45 min | Elevated | One scorer method, prescribed tests |
| 3.1 Tester / Builder | Sonnet / **Opus** | 45 / 90 min | Architectural | UoW atomicity, ambient join, architecture test |
| 3.2a | Sonnet | 75 min | Elevated | Regression + BUG-067/068 territory |
| 3.3 | Sonnet | 90 min | Elevated | Simplification + many test deletions |
| 3.3b | Haiku | 30 min | Standard | Record shrink, 4 construction sites |
| 3.4 | Sonnet | 45 min | Standard | Deletions |
| 3.2b | Sonnet | 45 min | Standard | Mechanical + one rewrite |
| 3.2c | Sonnet | 30 min | Standard | Mechanical, but spans 3–5 files with a census |
| 3.5a / 3.5b | Haiku / Haiku | 30 / 30 min | Standard | Positional record edits |
| 3.6a | Haiku | 15 min | Standard | One line |
| 3.6b M1 A / B | Sonnet / Sonnet | 45 / 30 min | Elevated | Migration tests; Part B first verifies the generated scaffold |
| 3.7 M2 A / B | Sonnet / Sonnet | 45 / 30 min | Elevated | Hand-inserted dedupe SQL |
| 3.8 M3 A / B | Sonnet / **Opus** | 45 / 90 min | Architectural | Raw DDL, triggers, RETURNING interaction |
| 3.9 | Sonnet | 90 min | Elevated | FTS query + builder |
| 3.9b / 3.9c | Sonnet / Sonnet | 60 / 60 min | Standard / Elevated | Reuses the builder; service swap |
| 3.10 Tester / Builder | Sonnet / **Opus** | 60 / 120 min | Architectural | Constitutional boundary, rule ordering |
| 4.1a / 4.1b | Sonnet / Haiku | 60 / 15 min | Standard | Provider tests / deletion |
| 4.2 Tester / Builder | Sonnet / Sonnet | 60 / 90 min | Elevated | Prescribed algorithm (`design.md` § 3.3) |
| 4.3 | Sonnet | 45 min | Standard | Registration + retargets |
| 5.1 | Sonnet | 90 min | Elevated | VM + one XAML + DI test |
| 5.2 Tester / Builder | Sonnet / Sonnet | 60 / 90 min | Elevated | Prescribed members |
| 5.3 Tester / Builder | Sonnet / Sonnet | 60 / 90 min | Architectural | Mapping only; service holds the rules |
| 5.3b | Haiku | 15 min | Standard | Delete one overload |
| 5.4 | Sonnet | 45 min | Standard | Small |
| 5.5 (+ former 5.7) / 5.6 | Sonnet / Sonnet | 75 / 60 min | Standard | Prescribed XAML + glue deletion |
| 6.1 Tester / Builder | Sonnet / Sonnet | 45 / 60 min | Elevated | Mirrors 4.2 (FTS pool + ranking, D20) |
| 6.2 / 6.3 | Sonnet / Sonnet | 75 / 60 min | Standard | VM + page |
| 7.1 | Orchestrator | 45 min | — | Docs |
| 7.2 | Helder | 60–90 min | Architectural | On-device |
| Verifier (after every merge) | Sonnet | 15 min each | — | Diff vs spec checklist |

## Self-review (writing-plans checklist)

- **Spec coverage:** REQ-SFT-01 → 5.5, 7.2 · 02 → 3.9, 4.2, 4.3, 5.2, 5.4, 5.5 · 03 → 4.2, 6.1 · 04 → 3.9, 4.2, 5.2 · 05 → 3.9, 4.2, 5.2 · 06 → 4.1a, 4.2, 5.2, 6.1, 6.2 · 07 → 4.2, 5.2 (fallback D1), 7.2 · 08 → 4.2, 6.1 · 09 → 4.1a, 4.2, 5.2, 6.1 · 10/11 → 5.2, 5.6 · 12/13 → 4.1a, 4.2, 5.2 · 14 → 5.2 · 15 → 3.10 · 16 → 5.2, 5.5 · 17 → 1.7 · 18 → 3.1, 3.10 · 19 → 3.8, 3.9b, 3.9c, 3.10, 5.3, 5.6 · 20/21 → 3.1, 3.10, 5.3 · 22/23 → 3.10, 5.1, 5.3, 5.6 · 24 → 3.3, 5.1 · 25 → 3.10 · 26 → 2.1, 3.1, 3.2a, 3.10 · 27 → 3.2a, 3.10, 5.2, 5.4, 5.5 · 28 → 3.2a, 3.2b · 29 → 2.1, 3.2a · 30–33 → 2.2, 3.9, 4.2 · 34 → 3.8, 3.9b · 35/36 → 4.1a, 4.1b, 4.2 · 37 → 3.2b, 3.2c, 3.5a/b, 3.6a, 3.6b · 38 → 3.3, 3.3b, 5.1 · 39 → 3.7 · 40 → 3.4 · 41 → 3.10 · 42 → 2.1, 3.3 · 43 → 5.1 (removed with the merge sheet) · 44 → 1.1–1.6 · 45 → 1.7 · 46 → 3.10, 5.3, 5.3b · 47–50 → 4.1a, 6.1, 6.2, 6.3 · 51 → 3.10, 5.3 · 52 → 3.1, 3.10. Seventh-round ruling (no race handling) → no task, by design.
- **Placeholders:** none. Conditional branches are named with a rule (G5 PASS/FAIL; census hits in 3.2c/3.4; the 3.10 split).
- **Type consistency:** `SongSearchHit`, `SongRemoteSearchResult`, `ArtistRemoteSearchResult`, `SongFormSaveRequest`/`SongFormSaveOutcome`/`ArtistDecision`/`SongDecision`/`SaveField`/`ArtistMatch`, `FtsQueryBuilder.BuildTrigramMatch`, `RankScore`, `SearchNameCandidatesAsync`, `FindIdByTitleVersionForArtistAsync`, `ExistsByExternalIdentityAsync`, `SearchConstants.RemoteSearchTimeoutMilliseconds`, `TitleOptionSource`/`TitleSupportingText`/`IsTitleSearchEnabled`, `NameOptionSource`/`NameSupportingText`/`IsNameSearchEnabled` are used identically wherever they appear.
- **Review Focus:** five items, each with a named test in its owning task (3.9/3.9b, 3.10, 3.6b, 5.2, 5.3).

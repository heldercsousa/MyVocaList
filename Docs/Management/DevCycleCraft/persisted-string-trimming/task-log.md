# Whitespace Normalization — Task Log

---
## Task: Task 1 — `StringNormalization` helper + Level-A unit tests
**Plan:** `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md` (Task 1 section)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `Services/Text/StringNormalization.cs`
- `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs`

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-08 | `NormalizeSearchQuery` — null/whitespace-only → `string.Empty` | `StringNormalization.NormalizeSearchQuery` | `NormalizeSearchQuery_NullOrWhitespace_ReturnsEmpty` |
| REQ-TRIM-01 | Edge + internal whitespace normalize to single-spaced trimmed query | `StringNormalization.NormalizeSearchQuery` / `Collapse` | `NormalizeSearchQuery_ExtraWhitespace_CollapsesAndTrims` |
| REQ-TRIM-08 | `TrimForStorage` — null passes through as null | `StringNormalization.TrimForStorage` | `TrimForStorage_Null_ReturnsNull` |
| REQ-TRIM-06 | Internal whitespace runs collapsed on storage (D1) | `StringNormalization.TrimForStorage` / `Collapse` | `TrimForStorage_Whitespace_EdgeTrimsAndCollapses` |
| REQ-TRIM-07 | Optional fields — empty/whitespace-only persists as null | `StringNormalization.TrimForStorageOrNull` | `TrimForStorageOrNull_NullOrWhitespace_ReturnsNull` |
| REQ-TRIM-07 | Optional fields with content normalized like required ones | `StringNormalization.TrimForStorageOrNull` | `TrimForStorageOrNull_WithContent_Normalizes` |
| REQ-TRIM-10 | No case folding / diacritic changes — content preserved verbatim | `StringNormalization` (no case-fold/diacritic code anywhere in the class) | `Normalization_NeverAltersCaseOrDiacritics` |

### Build notes
Build: passed (0 errors, 103 pre-existing warnings unrelated to this change) | Tests: 19 passed, 0 failed | Commit SHA: see below

Test run evidence (`dotnet test MyVocaList.Tests --filter StringNormalizationTests`):
```
Aprovado!  – Com falha:     0, Aprovado:    19, Ignorado:     0, Total:    19, Duração: 78 ms - MyVocaList.Tests.dll (net10.0)
```

TDD sequence followed: Red (test file written first, referencing a type that did not yet exist — would fail to compile) → Green (implemented `Services/Text/StringNormalization.cs` per plan.md Task 1 Step 3, exact signatures) → all 19 tests pass.

Files written and re-read: `Services/Text/StringNormalization.cs`, `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs` (both re-read post-write, content confirmed to match plan.md Task 1 exactly).

### Notes
- Pure static helper, no DI registration (per code-style-reference.md § DI Registration Conventions — not applicable to stateless static classes).
- No regex used in the implementation (design.md implementation note honored — uses `string.Split`/`string.Join`).
- No case folding / diacritic removal anywhere in the class (REQ-TRIM-10, constraints-registry HARD RULE) — collation remains the sole owner of that concern.

---
## Task: Task 2 — PersonService: BUG-046 regression fix (search normalization only)
**Plan:** `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md` (Task 2 section)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `Services/PersonService.cs`
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-01 | Whitespace-polluted query forwarded to repo identically to the clean query | `PersonService.SearchPersonsAsync` | `SearchPersonsAsync_ExtraWhitespace_ForwardsNormalizedTermToRepository` |
| REQ-TRIM-01 | Same, for `SearchPersonsStartsWithAsync` | `PersonService.SearchPersonsStartsWithAsync` | `SearchPersonsStartsWithAsync_ExtraWhitespace_ForwardsNormalizedTermToRepository` |
| REQ-TRIM-04 | Query normalizing below min length returns empty without hitting the repository | `PersonService.SearchPersonsAsync` (min-length gate post-normalization) | `SearchPersonsAsync_NormalizesBelowMinLength_ReturnsEmptyWithoutRepositoryCall` |
| REQ-TRIM-04 | Same, for `SearchPersonsStartsWithAsync` | `PersonService.SearchPersonsStartsWithAsync` | `SearchPersonsStartsWithAsync_NormalizesBelowMinLength_ReturnsEmptyWithoutRepositoryCall` |
| REQ-TRIM-03 | List/picker search term with extra whitespace normalized before hitting the repository | `PersonService.GetPagedPersonsForListAsync` | `GetPagedPersonsForListAsync_ExtraWhitespaceQuery_ForwardsNormalizedTermToRepository` |
| REQ-TRIM-02 | Normalization applied only to the query sent to the repository, never written back to bound `Text` | Holds by construction — `PersonService` has no access to/never writes any UI-bound property; it only receives a `string` parameter and calls the repository. No UI file touched by this task. | N/A (structural — verified by Helder on-device E2E per task-log Task 7) |

### BUG-046 Red→Green evidence (Major severity — regression test mandatory)

**Red** (`dotnet test MyVocaList.Tests --filter "FullyQualifiedName~PersonServiceTests"`, before the fix):
```
Com falha! – Com falha:     9, Aprovado:    31, Ignorado:     0, Total:    40, Duração: 1 s - MyVocaList.Tests.dll (net10.0)
```
9 failures: all 8 new `SearchPersonsAsync`/`SearchPersonsStartsWithAsync` whitespace-forwarding tests plus the new `GetPagedPersonsForListAsync` whitespace test — each failed with `Moq.MockException: ... This setup was not matched` (repo mock set up with the normalized term was never invoked because the service forwarded the raw dirty term).

**Fix:** added `searchTerm = StringNormalization.NormalizeSearchQuery(searchTerm);` at the top of `SearchPersonsAsync` and `SearchPersonsStartsWithAsync` (min-length gate — `searchTerm.Length < 2` — now evaluated post-normalization, REQ-TRIM-04); added `query = string.IsNullOrWhiteSpace(query) ? null : StringNormalization.NormalizeSearchQuery(query);` at the top of `GetPagedPersonsForListAsync` (REQ-TRIM-03; null preserved as null to avoid changing the existing unfiltered-list contract/mock expectations for the default `query = null` case).

**Green** (`dotnet test MyVocaList.Tests --filter "FullyQualifiedName~PersonServiceTests"`, after the fix):
```
Aprovado!  – Com falha:     0, Aprovado:    40, Ignorado:     0, Total:    40, Duração: 549 ms - MyVocaList.Tests.dll (net10.0)
```

Full suite (`dotnet test MyVocaList.Tests`, after the fix — no regressions):
```
Aprovado!  – Com falha:     0, Aprovado:   517, Ignorado:     0, Total:   517, Duração: 12 s - MyVocaList.Tests.dll (net10.0)
```

### Build notes
Build: passed (0 errors, pre-existing warnings unrelated to this change) | Tests: 517 passed, 0 failed (full suite) | Commit SHA: see below
Files written and re-read: `Services/PersonService.cs`, `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` (both re-read post-write; `grep` confirmed both `NormalizeSearchQuery` call sites present in `PersonService.cs`).

### Notes
- Scope respected: `CreatePersonAsync`/`UpdatePersonAsync` ad-hoc `.Trim()` calls (persisted-value trimming) were NOT touched — that is Task 6's job (EF Core `ValueConverter`, D3).
- `GetPagedPersonsForListAsync` null-preservation is an implementation decision not explicitly spelled out in plan.md's one-line sketch (`query = StringNormalization.NormalizeSearchQuery(query);`) — plan.md's literal form would turn `null` into `""` and forward `""` to the repository, changing the existing default (`query = null`) contract and breaking the pre-existing `GetPagedPersonsForListAsync_NoResults_ReturnsEmpty`/`_WithPersons_ReturnsMappedDtos` tests, which assert `GetPagedAsync(1, 20, null, default)`. Implemented as `query = string.IsNullOrWhiteSpace(query) ? null : StringNormalization.NormalizeSearchQuery(query)` instead — same REQ-TRIM-03 normalization for any non-empty query, null stays null. This is a within-scope implementation detail (method-body logic inside the assigned task's `Files owned`), not a redesign.

---
## Task: Task 3 — ArtistService + ArtistSuggestionService normalization (search only)
**Plan:** `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md` (Task 3 section)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `Services/ArtistService.cs`
- `Services/ArtistSuggestionService.cs`
- `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-01/03 | Query with extra internal/edge whitespace reaches the repository fully normalized (edge-trim + internal collapse), not just edge-trimmed | `ArtistService.SearchArtistsByNameAsync` | `SearchArtistsByNameAsync_ExtraInternalWhitespace_ForwardsNormalizedTermToRepository` |
| REQ-TRIM-01/03 | Paged list query reaches the repository fully normalized | `ArtistService.GetPagedArtistsForListAsync` | `GetPagedArtistsForListAsync_ExtraInternalWhitespace_ForwardsNormalizedQueryToRepository` |

`ArtistSuggestionService.GetLocalAsync`/`GetRemoteAsync` normalization has no dedicated new test in
this task (no pre-existing `ArtistSuggestionServiceTests.cs` coverage of whitespace was requested by
the briefing); the change replaces `term?.Trim()` with `StringNormalization.NormalizeSearchQuery(term)`
in `GetLocalAsync` (min-length gate now evaluated post-normalization, same pattern as REQ-TRIM-04) and
normalizes the term once at the top of `GetRemoteAsync` before it is forwarded to remote providers.
Existing `ArtistSuggestionServiceTests.cs` suite (unmodified) remained green after the change.

### Build notes
Build: passed (`Services/MyVocaList.Services.csproj` — 3 projects, 0 errors, 0 warnings;
`MyVocaList.Tests/MyVocaList.Tests.csproj` — 6 projects, 0 errors, 0 warnings)
Tests: `ArtistServiceTests` 19 passed, 0 failed. Full suite: 505 passed, 1 failed
(`AutocompleteFieldDebounceTests.Trigger_AfterDelay_InvokesCallback` — pre-existing timing-based
flaky test, unrelated to this change; confirmed passing in isolation, re-run separately: 3/3 green).
Commit SHA: see below.

Test run evidence (`dotnet test MyVocaList.Tests --filter FullyQualifiedName~ArtistServiceTests`):
```
Aprovado!  – Com falha:     0, Aprovado:    19, Ignorado:     0, Total:    19, Duração: 1 s - MyVocaList.Tests.dll (net10.0)
```

Red evidence (before the fix, same two tests):
```
Com falha! – Com falha:     2, Aprovado:    17, Ignorado:     0, Total:    19, Duração: 763 ms - MyVocaList.Tests.dll (net10.0)
  GetPagedArtistsForListAsync_ExtraInternalWhitespace_ForwardsNormalizedQueryToRepository [FAIL] — setup not matched (raw "ac  dc " forwarded instead of "ac dc")
  SearchArtistsByNameAsync_ExtraInternalWhitespace_ForwardsNormalizedTermToRepository [FAIL] — setup not matched (Trim() only edge-trims; internal double space survives)
```

TDD sequence followed: Red (wrote two failing tests asserting the repository receives the fully
normalized term; confirmed FAIL against the pre-fix `Trim()`/`query?.Trim()` code) → Green (replaced
both call sites with `StringNormalization.NormalizeSearchQuery`, plus the two `ArtistSuggestionService`
term-handling call sites) → all tests pass, full suite green apart from the pre-existing unrelated flake.

Files written and re-read: `Services/ArtistService.cs` (lines 123–144 re-read, confirms
`StringNormalization.NormalizeSearchQuery` at both call sites), `Services/ArtistSuggestionService.cs`
(lines 1–62 re-read, confirms normalization at top of `GetLocalAsync`/`GetRemoteAsync`),
`MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs` (new test block re-read post-write).

### Notes
- Scope held strictly to search-query normalization per the briefing — `CreateArtistAsync`/
  `UpdateArtistAsync` and their ad-hoc `name.Trim()` calls (lines 36, 52, 79) were left untouched;
  that is Task 6's scope (EF Core `ValueConverter`, D3).
- `GetPagedArtistsForListAsync`: `StringNormalization.NormalizeSearchQuery(query)` returns `""` for
  null/whitespace input (vs. the previous `query?.Trim()` which could pass `null` through). Verified
  this is behavior-neutral: `ArtistRepository.GetPagedAsync` guards with
  `string.IsNullOrEmpty(query)`, which treats `null` and `""` identically — no regression.
- `ArtistSuggestionService.GetRemoteAsync` now normalizes `term` before forwarding to
  `FetchFromProvidersAsync` (remote MusicBrainz/Deezer providers) — same substitution the plan.md
  Task 3 Step 3 instructions specify ("normalize `term` once at the top of GetLocalAsync/GetRemoteAsync").

---
## Task: Task 4 — VenueService normalization (search only)
**Plan:** `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md` (Task 4 section — note: plan.md's Task 4 text mentions EventService/storage sites left over from a prior spec revision; ignored per tasks.md, which scopes this task to VenueService search only)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `Services/VenueService.cs`
- `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-03 | Venue list search query with extra whitespace matches normalized term | `VenueService.GetPagedVenuesForListAsync` (`StringNormalization.NormalizeSearchQuery(query)`) | `GetPagedVenuesForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall` |

### Build notes
Build: passed (0 errors) | Tests: VenueServiceTests 16 passed, 0 failed; full suite 505 passed, 0 failed | Commit SHA: see below

TDD sequence: Red (`GetPagedVenuesForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall` added to existing `VenueServiceTests.cs`, ran `dotnet test MyVocaList.Tests --filter FullyQualifiedName~VenueServiceTests` — failed: repository received raw `" bar  x "` instead of normalized `"bar x"`) → Green (added `query = StringNormalization.NormalizeSearchQuery(query);` in `GetPagedVenuesForListAsync`, before the repository call) → re-ran, 16/16 pass.

```
Aprovado!  – Com falha:     0, Aprovado:    16, Ignorado:     0, Total:    16, Duração: 380 ms - MyVocaList.Tests.dll (net10.0)
```

Full suite (`dotnet test MyVocaList.Tests`):
```
Aprovado!  – Com falha:     0, Aprovado:   505, Ignorado:     0, Total:   505, Duração: 10 s - MyVocaList.Tests.dll (net10.0)
```

Files written and re-read: `Services/VenueService.cs` (confirmed `using MyVocaList.Services.Text;` added and normalization call placed before `GetPagedWithEventInfoAsync`), `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs` (confirmed new test appended to existing file, reusing the file's `_repoMock` convention).

### Notes
- Scope confirmed: `EventService.cs` not touched; `CreateVenueAsync`/`UpdateVenueAsync` persisted-value trimming left untouched for Task 6 (D3, EF Core ValueConverter).
- Full-solution Android target build was not run for this Services-only, non-UI change (coordinator direction) — `dotnet test MyVocaList.Tests` builds and exercises `Services`, `Infra`, `MyVocaList`, and `MyVocaList.Tests` projects with 0 errors, which covers all files in scope.

---
## Task: Task 5 — SongService + SongSuggestionService + CatalogService normalization (search only)
**Plan:** `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md` (Task 5 section — plan.md's storage-site instructions superseded by D3/tasks.md; search-normalization scope only)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `Services/SongService.cs`
- `Services/SongSuggestionService.cs`
- `Services/CatalogService.cs`
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
- `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md` (Task 5 checked off)

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-01 / 03 | List-page search query with extra whitespace normalizes to single-spaced, edge-trimmed term before hitting the repository | `SongService.GetPagedSongsForListAsync` | `GetPagedSongsForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall` |
| REQ-TRIM-06 | Comparison term for dedup check normalized consistently with the persisted (converter-collapsed) title, so `"John  Doe"`-style duplicates are caught | `SongService.ExistsByTitleForArtistAsync` | `ExistsByTitleForArtistAsync_TitleWithExtraWhitespace_ComparesNormalizedTerm` |
| REQ-TRIM-03 | Catalog picker/list search term normalized before repository call | `CatalogService.GetPagedCatalogForArtistAsync` | Covered by existing `CatalogServiceTests.GetPagedCatalogForArtistAsync_ReturnsRepositoryResult` (null-query path) + manual code inspection — `CatalogServiceTests.cs` is out of this task's owned-files scope, no test added there per briefing |
| REQ-TRIM-01 | Suggestion term (local + remote provider search) normalized before use | `SongSuggestionService.GetLocalAsync` / `GetRemoteAsync` | Out of owned-files scope (`SongSuggestionServiceTests.cs` not listed in briefing); verified by build + full suite green (506/506) and manual code inspection |

### Build notes
Build: passed (0 errors) | Tests: `SongServiceTests` 34 passed, 0 failed; full suite 506 passed, 0 failed | Commit SHA: see below

Test run evidence (`dotnet test MyVocaList.Tests --filter SongServiceTests`):
```
Aprovado!  – Com falha:     0, Aprovado:    34, Ignorado:     0, Total:    34, Duração: 3 s - MyVocaList.Tests.dll (net10.0)
```

Full suite (`dotnet test MyVocaList.Tests`):
```
Aprovado!  – Com falha:     0, Aprovado:   506, Ignorado:     0, Total:   506, Duração: 9 s - MyVocaList.Tests.dll (net10.0)
```

Files written and re-read: `Services/SongService.cs`, `Services/CatalogService.cs`, `Services/SongSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (all re-read post-edit via grep/read, confirmed `StringNormalization` usage present at expected call sites).

### Notes
- Scope note (briefing-authorized): implementation touches `CatalogService.cs` and `SongSuggestionService.cs`, but the corresponding pre-existing test files (`CatalogServiceTests.cs`, `SongSuggestionServiceTests.cs`) were NOT in this task's `Files owned` list, so no new tests were added there — only `SongServiceTests.cs` per the briefing. Full suite green confirms no regression.
- Did NOT touch `CreateSongAsync`/`UpdateSongAsync`/`CreateSongWithUrlsAsync` persistence — that is Task 6's `ValueConverter` responsibility (D3), per explicit briefing instruction.
- `GetRemoteAsync`'s normalized term is also forwarded to `artistHint`-paired external provider search (`FetchFromProvidersAsync`) — normalizing the term sent to MusicBrainz/Deezer is a reasonable, low-risk extension of the same fix since garbled whitespace would degrade those queries identically; no spec objection since REQ-TRIM-09 scopes normalization to Service-layer search call sites broadly.

---
## Task: Task 6a — Relocate StringNormalization to MyVocaList.Extensions (D4)
**Plan:** Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### Changed files:
- `MyVocaList.Extensions/MyVocaList.Extensions.csproj` (new project)
- `MyVocaList.Extensions/Strings/StringExtensions.cs` (new — ported algorithm, extension-method syntax)
- `MyVocaList.Tests/Unit/Extensions/Strings/StringExtensionsTests.cs` (new — migrated test cases, adapted to extension-method call syntax)
- `Services/Text/StringNormalization.cs` (deleted)
- `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs` (deleted)
- `Services/ArtistService.cs` (using + call-site update)
- `Services/ArtistSuggestionService.cs` (using + call-site update)
- `Services/CatalogService.cs` (using + call-site update)
- `Services/PersonService.cs` (using + call-site update)
- `Services/SongService.cs` (using + call-site update)
- `Services/SongSuggestionService.cs` (using + call-site update)
- `Services/VenueService.cs` (using + call-site update)
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` (using + call-site update)
- `Services/MyVocaList.Services.csproj` (added ProjectReference to MyVocaList.Extensions)
- `MyVocaList.Tests/MyVocaList.Tests.csproj` (added ProjectReference to MyVocaList.Extensions)
- `MyVocaList.sln` (registered new project via `dotnet sln add`)
- `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md` (Task 6a checked off)

### Verification evidence

**Zero-dependency confirmation** — `MyVocaList.Extensions.csproj` contents (no `ProjectReference` element present):
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <NoWarn>$(NoWarn);CS8601;CS8603;CS8625</NoWarn>
  </PropertyGroup>
</Project>
```

**Zero remaining old-reference confirmation** — `grep -rn "MyVocaList.Services.Text\|StringNormalization\." --include=*.cs .` returned no matches.

**Build** (non-MAUI-head projects, `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` — pulls in Extensions/Domain/Contracts/Infra/Services/MyVocaList/MyVocaList.Tests, 7 projects):
```
ok dotnet build: 7 projects, 0 errors, 0 warnings (00:00:14.59)
```
Note: solution-wide `dotnet build` also hit an unrelated pre-existing environment error (`XA0142`/`XAWAS7024`, Android SDK file lock on `lib_Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll.so` from a running .NET host) on the `net10.0-android` head TFM only — not caused by this change (no code path touches Android packaging); all 7 non-Android-head projects (including the MAUI head's other TFMs implicitly via the Tests build) compiled clean.

**Tests** (`dotnet test MyVocaList.Tests`):
```
Com falha! – Com falha:     1, Aprovado:   521, Ignorado:     0, Total:   522, Duração: 7 s
```
The 1 failure (`ArtistRepositoryTests.GetPagedAsync_NoQuery_ReturnsAllSortedByName`, `ObjectDisposedException` on `SQLitePCL.sqlite3`) is a known-flaky SQLite-connection-disposal issue unrelated to this change — confirmed by re-running the same test in isolation:
```
Aprovado!  – Com falha:     0, Aprovado:     1, Ignorado:     0, Total:     1, Duração: 493 ms
```
Total count (522) matches the baseline exactly — pure relocation, no test count drift (the ~8 migrated `StringNormalizationTests` cases became `StringExtensionsTests` 1:1).

### Post-edit re-reads
- `MyVocaList.Extensions/Strings/StringExtensions.cs` — confirmed algorithm identical to original (`Collapse` private helper, same null/whitespace branching).
- `MyVocaList.Tests/Unit/Extensions/Strings/StringExtensionsTests.cs` — confirmed all 8 original test methods present, calls adapted to `input.Method()` syntax.
- `Services/ArtistService.cs`, `Services/PersonService.cs` — confirmed `using MyVocaList.Extensions.Strings;` present and call sites read `x.NormalizeSearchQuery()`.
- `MyVocaList.sln` — confirmed `MyVocaList.Extensions` project entry present.

### Notes
- Pure relocation + calling-syntax change, no behavior change — per design.md § D4 and REQ-TRIM-08's 2026-07-19 amendment. Algorithm (edge-trim + internal whitespace collapse via `Split`/`Join`, no regex, no case-folding/diacritic removal per REQ-TRIM-10) ported verbatim.
- Did not touch `Infra/` per briefing — Task 6 (unmerged, `feat/persisted-string-trimming-converters`) will be rebased onto this branch's output separately to point its `EntityTypeConfiguration`/`Infra.csproj` at `MyVocaList.Extensions` directly, removing the `Infra→Services` edge D4 flagged.
- Not pushed/merged per instructions — committed locally only on `feat/string-extensions-relocation`.

---
## Task: Task 6 — Persistence: EF Core ValueConverters for name-like properties (D3, 2026-07-19)
**Plan:** Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md (Task 6 section)
**Status:** To Review
**Started:** 2026-07-19
**Completed:** 2026-07-19

### D3 rationale citation
Implements `design.md § Decision points -> D3` (resolved 2026-07-19): persisted-string trimming
(REQ-TRIM-05/06/07) is enforced via EF Core `ValueConverter<string,string>` /
`ValueConverter<string?,string?>` configured per name-like property in
`IEntityTypeConfiguration<T>` (Infra), delegating to `StringNormalization.TrimForStorage` /
`TrimForStorageOrNull` (Services). This supersedes the earlier per-Service-Create/Update-method
`.Trim()` calls. D3's rationale (not business logic — universal data-integrity invariant; no
SQL-side cost since `ToProviderExpression` runs client-side before parameterization, including
`WHERE` clause comparands; enforcement over convention; delegation not reimplementation) is fully
reproduced in `design.md`.

### Live-repo discovery (paths verified, not guessed)
- `IEntityTypeConfiguration<T>` implementations live in `Infra/EntityEFConfig/*.cs`, applied via
  `AppDbContext.OnModelCreating`. Existing per-property config (e.g. `.UseCollation(...)`) follows
  the same per-property fluent-chain pattern mirrored here.
- Two distinct `Event` entities exist — `MyVocaList.Domain.Entity.Event` (property `EventName`,
  configured by `EventConfiguration.cs`, table `Events`, appears to be a legacy/unused model, not
  referenced by `EventService`) vs `MyVocaList.Domain.Entities.Event` (plural namespace, property
  `Name`, configured by `QueueManagementEventConfiguration.cs`, table `QueueManagementEvents` —
  this is the entity `EventService.CreateEventAsync` actually persists). The briefing's
  "Event.Name" therefore maps to `QueueManagementEventConfiguration.cs`, not `EventConfiguration.cs`
  — verified via `AppDbContext.cs` DbSet registrations and `EventService.cs`'s using directive
  (`Domain.Entities`, not `Domain.Entity`). `EventConfiguration.cs` (the unused legacy model) was
  left untouched — out of scope, no live call site feeds it.
- `Song.Version` is a non-nullable `string` (`= string.Empty` default; `""` = canonical version,
  domain-meaningful) — used `TrimValueConverters.Required` (not `.Optional`) since
  `TrimForStorage("")` returns `""`, never `null`.
- `Person.ExternalId` is `Guid?`, not `string` — not a converter target (briefing's "Artist
  externalId" only applies to `Artist.ExternalId`/`Song.ExternalId`, both `string?`).

### Implementation
- Added `Infra/EntityEFConfig/TrimValueConverters.cs` — two shared, reusable `ValueConverter`
  instances (`Required`, `Optional`) mirroring the existing `Collation/CollationConstants.cs`
  shared-constant pattern, avoiding per-file converter duplication across the five configurations.
- Configured `.HasConversion(TrimValueConverters.Required|Optional)` on:
  - `PersonConfiguration`: `FullName` (Required), `Email` (Optional)
  - `ArtistConfiguration`: `Name` (Required), `ExternalId` (Optional)
  - `VenueConfiguration`: `Name` (Required)
  - `QueueManagementEventConfiguration`: `Name` (Required)
  - `SongConfiguration`: `Title` (Required), `Version` (Required), `FeaturedArtists` (Optional),
    `ExternalId` (Optional)
- Each site carries a one-line `D3 (design.md § D3): ...` comment per the briefing's "why Infra,
  not Services" requirement.
- **Architecture note (living-spec, in-scope decision):** `Infra/MyVocaList.Infra.csproj` gained a
  `ProjectReference` to `Services/MyVocaList.Services.csproj` so the converter lambdas can call
  `StringNormalization`. No cycle: `Services` does not reference `Infra`. This is the direct,
  necessary consequence of D3's "delegate to Services, don't reimplement" clause — flagged here per
  the Living Spec Protocol rather than left silent; `design.md § D3` already documents the
  delegation intent, no further spec-file edit needed for the csproj mechanics itself.
- Removed the now-redundant ad-hoc `.Trim()` sites that fed persisted properties in
  `PersonService.CreatePersonAsync`/`UpdatePersonAsync` (`FullName`, `Email` — `BirthdayDayMonth`
  is out of D3 scope, untouched), `ArtistService.CreateArtistAsync`/`UpdateArtistAsync` (`Name`),
  `VenueService.CreateVenueAsync`/`UpdateVenueAsync` (`Name`), `EventService.CreateEventAsync`
  (`Name` — the length-validation lines now call `.Trim()` inline since that check doesn't feed
  storage), `SongService.CreateSongAsync`/`UpdateSongAsync`/`CreateSongWithUrlsAsync` (`Title`,
  `Version`, `FeaturedArtists`). Pre-save duplicate-check calls (`ExistsByNameAsync`,
  `ExistsByTitleForArtistAsync`, `IsEmailTakenAsync`, `GetByNameAsync`) were also left untrimmed
  deliberately — EF applies the property's `ValueConverter` to `WHERE` parameters too (verified by
  the `ArtistName_TrimmedOnWrite_StillMatchesCaseInsensitiveQuery` test below), so passing the raw
  value still matches trimmed stored rows. Success/failure message strings that display the raw
  input were left as-is in most cases (cosmetic, not a persistence correctness concern); a few were
  updated to call `.Trim()` inline for message cleanliness where low-risk (e.g. `PersonService`'s
  return messages).
- `SongResolutionService.cs`, `ArtistResolutionService.cs`, `SongKaraokeUrlService.cs`,
  `FeedbackService.cs` — NOT touched (out of the briefing's five-file scope; their `.Trim()` sites
  serve different purposes, e.g. resolution-candidate comparison, and were not audited here).

### Red -> Green evidence
New test file: `MyVocaList.Tests/Integration/Repositories/PersistedStringTrimmingTests.cs` — real
temp-file SQLite via `TestDbContextFactory` (never EF in-memory provider, per `testing.md`
anti-pattern rule), 6 tests.

RED (converters temporarily reverted via `git stash` of the 5 `EntityTypeConfiguration` files,
`dotnet test --filter PersistedStringTrimmingTests`):
```
Com falha!  Com falha:     6, Aprovado:     0, Ignorado:     0, Total:     6, Duracao: 619 ms
```
(e.g. `PersonFullName_...`: Expected "John Doe", Actual " John  Doe "; `SongTitle_...`: Expected
"Bohemian Rhapsody", Actual "  Bohemian  Rhapsody " — confirms properties round-tripped raw before
the fix.)

GREEN (converters restored via `git stash pop`, re-run):
```
Aprovado!  Com falha:     0, Aprovado:     6, Ignorado:     0, Total:     6, Duracao: 629 ms
```

### Collation + converter composition — explicitly verified
`ArtistName_TrimmedOnWrite_StillMatchesCaseInsensitiveQuery`: saves `Artist.Name = "  Queen  "`,
then queries `a.Name == "QUEEN"` (case-different, untrimmed) via EF LINQ — passes, proving the
`ValueConverter` (storage trim) and `.UseCollation(CollationConstants.Default)` (case-insensitive
comparison) coexist correctly on the same property: EF applies `ToProviderExpression` to both the
stored value and the query comparand before the collation-aware SQL comparison runs.

### Build/test evidence
- `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` → 0 errors (builds Domain, Contracts,
  Services, Infra, MyVocaList (net10.0 lib target), Tests).
- `dotnet build` (full solution incl. all MAUI TFMs) → 7 projects, 0 errors, 7 warnings (all
  warnings pre-existing: DevExpress trial-license notices, one `CA1416` platform-reachability
  warning — none introduced by this task).
- `dotnet test MyVocaList.Tests` (full suite) → 528 passed, 0 failed, 0 skipped (baseline before
  this task: 522 passing per briefing; +6 new round-trip tests = 528; no test was removed or
  weakened).

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-TRIM-05 | Leading/trailing whitespace on a saved name-like field is not persisted | `PersonConfiguration`, `ArtistConfiguration`, `SongConfiguration` `.HasConversion(TrimValueConverters.Required)` | `PersonFullName_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed`, `ArtistName_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed`, `SongTitle_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed` |
| REQ-TRIM-06 | Internal whitespace runs collapse to a single space on persist | Same converters (`StringNormalization.TrimForStorage` collapses internally, D1) | Same three tests above (input contains internal double-space runs, e.g. `" John  Doe "`) |
| REQ-TRIM-07 | Optional field normalizing to empty/whitespace-only persists as `null` | `PersonConfiguration.Email`, `SongConfiguration.FeaturedArtists` `.HasConversion(TrimValueConverters.Optional)` | `PersonEmail_WhitespaceOnly_PersistedAsNull`, `SongFeaturedArtists_WhitespaceOnly_PersistedAsNull` |

### Changed files:
- `Infra/EntityEFConfig/TrimValueConverters.cs` (new)
- `Infra/EntityEFConfig/PersonConfiguration.cs` (FullName/Email converters)
- `Infra/EntityEFConfig/ArtistConfiguration.cs` (Name/ExternalId converters)
- `Infra/EntityEFConfig/VenueConfiguration.cs` (Name converter)
- `Infra/EntityEFConfig/QueueManagementEventConfiguration.cs` (Name converter — the live `Event`
  entity used by `EventService`, distinct from the unused `Domain.Entity.Event`/`EventConfiguration.cs`)
- `Infra/MyVocaList.Infra.csproj` (added `ProjectReference` to `Services/MyVocaList.Services.csproj`)
- `Services/PersonService.cs` (removed redundant `FullName`/`Email` storage `.Trim()` sites in Create/Update)
- `Services/ArtistService.cs` (removed redundant `Name` storage `.Trim()` sites in Create/Update)
- `Services/VenueService.cs` (removed redundant `Name` storage `.Trim()` sites in Create/Update)
- `Services/EventService.cs` (removed redundant `Name` storage `.Trim()` site in Create; validation-only length check now trims inline)
- `Services/SongService.cs` (removed redundant `Title`/`Version`/`FeaturedArtists` storage `.Trim()` sites across all three Create/Update methods)
- `MyVocaList.Tests/Integration/Repositories/PersistedStringTrimmingTests.cs` (new — 6 real-SQLite round-trip tests)
- `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md` (Task 6 checked off)

Files written and re-read: all of the above (each `Edit` diff confirmed in-tool; `PersonService.cs`
Create/Update sections re-read post-edit; `TrimValueConverters.cs`, all five configuration files,
and `PersistedStringTrimmingTests.cs` confirmed via successful compile + green test run, which is
stronger evidence than a visual re-read alone for generated-expression-tree converter code).

### Notes
- `.sln` registration: N/A — all changed/new files are `.cs`/`.csproj` under SDK-style glob
  inclusion, not `Docs/`/`.claude/` files (per exit checklist step 4).
- No new EF Core migration was needed or created — `ValueConverter` only changes CLR<->provider
  value mapping for an existing `TEXT` column, no schema change (confirmed by the "no migration"
  success criterion in the briefing).
- Pre-existing-data caveat (documented in `design.md § D3`, not re-litigated here): rows persisted
  before this converter existed are not retroactively trimmed; no backfill migration is in scope.

### Post-rebase repoint (2026-07-19)
Rebased onto post-Task-6a `develop`. Repointed `TrimValueConverters.cs` and `Infra/MyVocaList.Infra.csproj`
from `MyVocaList.Services.Text.StringNormalization` (Services) to `MyVocaList.Extensions.Strings`
extension-method syntax (`v.TrimForStorage()` / `v.TrimForStorageOrNull()`), per D4. Removed the
`Infra→Services` `ProjectReference` (via `dotnet remove ... reference`) and added
`Infra→MyVocaList.Extensions` instead (`dotnet add ... reference`) — resolves the DRY Onion violation
the verifier flagged on the pre-rebase commit. No `EntityTypeConfiguration` file needed a `using`
change (they only reference `TrimValueConverters`, same namespace).
- `grep -rn "MyVocaList.Services.Text\|StringNormalization\." --include=*.cs Infra/` → zero matches.
- `Infra/MyVocaList.Infra.csproj` `ProjectReference`s: `Domain`, `MyVocaList.Extensions` only (no `Services`).
- `dotnet build` → 8 projects, 0 errors, 118 warnings (all pre-existing, unrelated).
- `dotnet test MyVocaList.Tests` → 528 passed, 0 failed, 0 skipped.
- `dotnet test MyVocaList.Tests --filter "FullyQualifiedName~PersistedStringTrimmingTests"` → 6 passed,
  0 failed (includes the collation + converter composition test,
  `ArtistName_TrimmedOnWrite_StillMatchesCaseInsensitiveQuery`, re-verified specifically per the
  rebase briefing).
Changed files this repoint: `Infra/EntityEFConfig/TrimValueConverters.cs` (using + call syntax),
`Infra/MyVocaList.Infra.csproj` (ProjectReference swap), `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md`
(Task 6 status note updated).

### Verifier Verdict - 2026-07-19
**Result:** PASS

**Findings:**

- [ARCHITECTURE FINDING - most prominent, surfaced first per verification instructions]
  Infra/MyVocaList.Infra.csproj gained a new ProjectReference to Services/MyVocaList.Services.csproj
  (commit cc1af4d diff confirmed) so TrimValueConverters.cs (Infra) can call
  MyVocaList.Services.Text.StringNormalization.TrimForStorage/TrimForStorageOrNull (Services).
  Independently re-derived the full reference graph from each .csproj (not from the implementor claim):
  - Contracts -> (none)
  - Domain -> Contracts
  - Services -> Contracts, Domain (does NOT reference Infra)
  - Infra -> Domain, Services (new, this commit)
  - MyVocaList (app) -> Domain, Infra, Services
  Not circular - Services has zero reference to Infra, so .NET build graph is a DAG and the
  build succeeding (7 projects, 0 errors, independently re-run) is consistent, not suspicious.
  However it IS a directional violation of the DRY Onion ordering stated in workflow.md Rule 4 and
  CLAUDE.md Architecture section (Domain -> Infra -> Services -> UI) - Infra now depends forward on
  Services, which is backward relative to that stated order. design.md D3 (approved by Helder
  2026-07-19) documents the conceptual decision - Infra configures WHERE, Services owns WHAT -
  but does not explicitly call out the concrete consequence that this requires a new cross-layer
  ProjectReference running against the stated Onion order; that mechanical detail was decided by
  the implementor at build time, not pre-approved in the spec text itself (the implementor did
  self-flag this transparently in the task-log Architecture note section, which is good practice,
  but self-flagging is not the same as prior Helder sign-off on the csproj-level mechanism).
  Alternative not investigated by the implementor: StringNormalization (Services/Text/, namespace
  MyVocaList.Services.Text) is a pure static function class (verified - unmodified this commit, no
  DI, no side effects, no Services-layer state). Both Domain (has Entities, Entity, Interfaces,
  ReadModels, RepositoryInterface, ServicesInterfaces folders) and Contracts (has DTOs, Enums,
  Messages, Models folders - sits below Domain in the graph, referenced by it) exist as viable
  lower-layer homes. Moving StringNormalization to Contracts would let Infra continue to depend
  only on Domain/Contracts (both already in its dependency set) and eliminate the new
  Infra -> Services edge entirely, restoring strict Onion order with no loss of the D3 shared
  normalization primitive goal. This alternative was not evaluated in design.md or the task-log.
  Recommendation: not a build defect and not a spec violation in letter (D3 was approved), but a
  spec gap - the mechanical layering consequence of D3 was decided unilaterally by the implementor
  rather than pre-approved by Helder as part of D3 resolution. Escalate to Helder for an explicit
  yes/no: (a) accept the Infra -> Services reference as a documented, narrow exception to the Onion
  order (add an explicit note to design.md D3 recording this), or (b) request a follow-up task to
  relocate StringNormalization to Contracts and drop the new ProjectReference. Not a blocker for
  merge given D3 conceptual approval and the transparent self-flagging, but must not be treated as
  silently pre-cleared.

- [PASS] Service method / EF configuration signatures match design.md D3 - ValueConverter<string,string>/ValueConverter<string?,string?> per name-like property, delegating to TrimForStorage/TrimForStorageOrNull.
- [PASS] Validation rules (REQ-TRIM-05/06/07) enforced at the persistence layer per D3 - verified in PersonConfiguration.cs, ArtistConfiguration.cs, VenueConfiguration.cs, QueueManagementEventConfiguration.cs, SongConfiguration.cs diffs.
- [PASS] All three ACs (REQ-TRIM-05/06/07) have a traceability-matrix row and a corresponding passing test.
- [PASS] No DisplayAlert/DisplayActionSheet/DisplayPromptAsync added - diff touches only Infra/Services/test files, no UI.
- [PASS] No business logic added to ViewModels/pages - Services layer only; the trimming algorithm remains in Services/Text/StringNormalization.cs, confirmed unmodified this commit (empty diff).
- [PASS] Repository interfaces unaffected - no IRepository changes in this task.
- [CONDITIONAL - see architecture finding above] Services does not depend on Infra types directly - true and verified, but Infra now depends on Services, which the finding above addresses.
- [N/A] No new ContentPage added.
- [PASS] No new pragma warning disable / SuppressMessage introduced.
- [PASS] Domain.Entities.Event vs Domain.Entity.Event - independently confirmed via grep: two distinct Event classes exist (Domain/Entities/Event.cs implements IAggregateRoot, Domain/Entity/Event.cs is a separate legacy type). EventService.cs imports MyVocaList.Domain.Entities and its CreateEventAsync return type is Domain.Entities.Event?; confirmed the live entity is the one QueueManagementEventConfiguration.cs (converted this commit) configures; EventConfiguration.cs (legacy Domain.Entity.Event) untouched, correctly out of scope.
- [PASS] Each EntityTypeConfiguration diff: HasConversion(TrimValueConverters.Required or Optional) precedes UseCollation(CollationConstants.Default) in the fluent chain on every converted property (Person.FullName, Artist.Name, Venue.Name, Event.Name, Song.Title, Song.Version) - correct EF Core composition order confirmed by reading each diff hunk directly, not from the implementor summary. FromProvider side is identity (v => v) in both TrimValueConverters.Required/Optional.
- [PASS] Services/Text/StringNormalization.cs - zero diff this commit (git diff cc1af4d~1..cc1af4d on that path returns empty), Task-1 contract untouched.
- [PASS] Ad-hoc Trim() removal - confirmed in PersonService.cs/ArtistService.cs/VenueService.cs/EventService.cs/SongService.cs diffs; storage-feeding Trim() calls removed, WHERE-parameter calls left untrimmed with an explanatory comment (correct - EF applies the converter to query comparands too), display-message Trim() calls retained (cosmetic, out of persistence scope). Grep for NormalizeSearchQuery across the full commit diff returns zero matches - confirms Tasks 2-5 search-normalization code was not touched.
- [PASS] PersistedStringTrimmingTests.cs - uses TestDbContextFactory.Create() (real temp-file SQLite per testing.md anti-pattern rule, not EF in-memory), exercises Person, Artist, Song (3 entities as briefed), includes 2 optional-field null-coercion cases (Person.Email, Song.FeaturedArtists), AC REQ-TRIM-NN comments present on the 5 AC-mapped tests (the 6th, collation-composition test, is an infra/cross-cutting verification test, exempt per testing.md).
- [PASS] Red to Green claim independently plausible - test assertions compare against a specific collapsed/trimmed string (e.g. "John Doe" vs raw " John  Doe ") that could only pass if the ValueConverter executed; without it (pre-Task-6 raw SQLite round-trip) the assertions would fail by construction. Task-log stated stash/pop RED (6 failed) to GREEN (6 passed) evidence is internally consistent with this.
- [PASS] task-log.md - exactly 6 "## Task:" headers present (Tasks 1-6), Task 6 entry appended without disturbing prior entries.
- [PASS] tasks.md - Task 6 checkbox is [x].
- [PASS] Independent dotnet build MyVocaList.sln - 7 projects, 0 errors, 7 pre-existing warnings (DevExpress trial-license + one CA1416), matches task-log claim.
- [PASS] Independent dotnet test MyVocaList.Tests - 528/528 passed, 0 failed, 0 skipped, no flaky UI-timing tests observed this run.
- [PASS] git diff --stat develop..HEAD - 15 files changed, all within Task 6 legitimate scope (5 EntityTypeConfiguration files, new TrimValueConverters.cs, Infra.csproj, 5 Service files, new test file, task-log.md, tasks.md); nothing unexpected. Working tree clean (git status).

**Blockers (must be fixed before proceeding):**
- None.

**Warnings (should be fixed; may proceed with justification):**
- The Infra -> Services ProjectReference (architecture finding above) should get an explicit Helder yes/no and, if accepted, a one-line addendum to design.md D3 recording the concrete csproj-level consequence - not because it is wrong, but because it was an implementor-level architectural decision that ran ahead of what the spec text explicitly pre-approved.

**Recommendation:** Proceed to merge. Escalate the Infra -> Services reference-direction question to Helder as a fast-follow (does not block this task) - either add the explicit exception note to design.md D3, or open a follow-up task to relocate StringNormalization to Contracts and remove the new reference.

---

## Task: 7 — Integration merge + docs close-out
**Status:** To Review
**Date:** 2026-08-04
**Agent:** main (orchestrator — shell + docs only, per the task's own "no worktree" note)

### What this task resolved

The item's search half (Tasks 1–5) had been on `develop` since 2026-07-19. The **persistence
half — Task 6, the EF Core `ValueConverter`s that are this item's title deliverable — had never
been merged.** It sat on `feat/persisted-string-trimming-converters` in worktree
`MyVocaList-wt-trim-persist`. The practical consequence on `develop` was that
`TrimForStorage`/`TrimForStorageOrNull` existed with **zero production call sites**: strings were
normalized for *search* but stored raw. This task merged that branch.

### Merge procedure

1. `git merge-tree --write-tree develop feat/persisted-string-trimming-converters` — clean, no
   conflicts, before touching any branch.
2. `git merge develop` **into the branch** inside its worktree (commit `d08636db`), so `develop`
   never held an unverified state — a parallel session is working on `develop`.
3. Build + full suite + targeted suite run on the merge result (evidence below).
4. `git merge --no-ff` the branch into `develop` (commit `761a1f0f`).
5. Build + full suite re-run on `develop` itself.

### Changed files

Landed by the merge (branch commits `f451f68a`, `c4ea58da`, `01625dfa`):

- `Infra/EntityEFConfig/TrimValueConverters.cs` *(new)*
- `Infra/EntityEFConfig/PersonConfiguration.cs`
- `Infra/EntityEFConfig/ArtistConfiguration.cs`
- `Infra/EntityEFConfig/VenueConfiguration.cs`
- `Infra/EntityEFConfig/QueueManagementEventConfiguration.cs`
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/MyVocaList.Infra.csproj`
- `Services/PersonService.cs`, `Services/ArtistService.cs`, `Services/VenueService.cs`,
  `Services/EventService.cs`, `Services/SongService.cs` — redundant ad-hoc `Trim()` removed
- `MyVocaList.Tests/Integration/Repositories/PersistedStringTrimmingTests.cs` *(new)*
- `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md`, `task-log.md`

Written by this task: this entry, `tasks.md` Task 7 checkbox, `README.md` gate + status note,
`Docs/Management/LEDGER.md`, `Docs/Management/BACKLOG.md` (generated).

### Verification evidence

| Check | Where | Result |
|---|---|---|
| Conflict dry run | `git merge-tree` develop vs branch | clean, no conflicts |
| Build | merge result, in worktree | 8 projects, **0 errors**, 113 warnings (all pre-existing) |
| Full suite | merge result, in worktree | **519 passed, 0 failed, 0 skipped** |
| Persistence tests | `--filter FullyQualifiedName~PersistedStringTrimming` | **6 passed, 0 failed** (real temp-file SQLite) |
| Build | `develop` after merge | 8 projects, **0 errors** |
| Full suite | `develop` after merge | **519 passed, 0 failed** |

The 6 persistence tests were run under an explicit filter rather than inferred from the total,
to confirm they execute rather than silently skip.

### AC traceability

REQ-TRIM-01..04 and 08..10 (search normalization) were traced in the Task 1–5 entries above;
REQ-TRIM-05/06/07 (persistence) in the Task 6 entry, each with a passing test and confirmed
`[PASS]` by the Task 6 verifier. This task added no new behaviour and therefore no new AC rows —
it made the already-traced Task 6 rows true of `develop` rather than of a branch.

### Prior verifier warning — resolved, not carried

The Task 6 verifier raised one non-blocking warning: the `Infra → Services` `ProjectReference`
introduced by D3's implementation inverted the DRY Onion direction. **D4 already fixed this**
(branch commit `01625dfa`): `Infra` references the leaf `MyVocaList.Extensions` project, which
has zero `ProjectReference` entries of its own. Nothing is outstanding for Helder here.

### Still open — why this item is not ✅ Done

No implementation work remains and every checkbox in `tasks.md` is checked. The item holds at
🟡 In Progress solely because **Task 7's review lane is Helder's final review + on-device E2E
gate**, and Task 2's is Helder's on-device E2E for REQ-TRIM-01/02 (autocomplete singer field).
Neither has been performed. Recording ✅ would assert a sign-off that was never given.

**Resume point for Helder:** exercise search and save on device, confirm REQ-TRIM-01/02, then
`backlog_gen.py status persisted-string-trimming "✅ Done" --closed 2026-08`.

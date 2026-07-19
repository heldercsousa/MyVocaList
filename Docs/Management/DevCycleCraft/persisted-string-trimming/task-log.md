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

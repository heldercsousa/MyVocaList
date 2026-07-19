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

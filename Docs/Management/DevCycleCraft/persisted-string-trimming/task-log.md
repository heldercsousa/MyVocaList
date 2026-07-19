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

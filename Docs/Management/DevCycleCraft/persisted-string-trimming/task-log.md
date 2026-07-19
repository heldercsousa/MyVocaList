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

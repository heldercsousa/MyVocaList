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

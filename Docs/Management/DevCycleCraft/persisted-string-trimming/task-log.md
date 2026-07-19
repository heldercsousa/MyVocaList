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

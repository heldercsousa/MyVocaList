# Task Log — Song artist field: correctness fixes + inline "create new artist"

**Spec folder:** this folder (`requirements.md` · `design.md` · `tasks.md`).
**Parent:** Artists & Songs Catalog → closes BUG-027; folds in BUG-050 / BUG-051 / BUG-052 (found in DX-AC T7, 2026-07-21).

## Milestones

- **2026-07-21 — Design approved** (Helder, plan mode): inline create-new-artist, affordance = synthetic ➕ dropdown row (Option A) with Option-B fallback; scope minimal.
- **2026-07-21 — DX-AC T7 device run** (Helder): 3 pass / 6 fail. Root-caused BUG-050 (SelectArtist omits `IsArtistLocked=true`), BUG-051 (`ArtistSuggestions` race, no per-request cancellation), BUG-052 (empty artist on edit, compound). Evidence in `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md § T7 outcome`.
- **2026-07-21 — Consolidation decision** (Helder): fold the fixes + inline-create into one sequenced worktree (same handlers, single-writer).
- **2026-07-21 — spec-reviewer PASS** on the consolidated spec (doc-hygiene polish folded in).
- **2026-07-21 — SPEC APPROVED by Helder.** Cleared for the planning phase (`writing-plans` → plan-reviewer → Helder plan approval → implementation in a worktree, T1 first, regression-test-first).

## Status
**Phase:** T3 complete (To Review). Worktree `MyVocaList-inline-ac` on `feat/inline-artist-create`.

---
## Task: T1 (BUG-050, Critical) — selecting a suggestion must lock the field
**Plan:** plan.md § Task 1 (T1)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — added `IsArtistLocked = true;` in `SelectArtist` (BUG-050 fix)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — added regression test `SelectArtist_ExistingSuggestion_LocksField` + `using MyVocaList.Domain.ReadModels;`

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-12 (BUG-050) | Selecting an existing suggestion locks the artist field | `SongFormViewModel.SelectArtist` (`IsArtistLocked = true`) | `SongFormViewModelTests.SelectArtist_ExistingSuggestion_LocksField` |

### Verification evidence
- **Red** (before fix): `dotnet test --filter SelectArtist_ExistingSuggestion_LocksField` →
  `Assert.True() Failure — Expected: True, Actual: False` at SongFormViewModelTests.cs:338 (IsArtistLocked stayed false). Failed: 1, Passed: 0.
- **Green** (after fix): same filter → `Aprovado! Com falha: 0, Aprovado: 1`.
- **Full suite:** `dotnet test` → `Com falha: 0, Aprovado: 512, Ignorado: 0, Total: 512` (baseline 511 + 1 new).
- **Build:** `dotnet build MyVocaList -f net10.0-android` → 6 projects, 0 errors, 22 warnings (DevExpress eval + pre-existing nullable warnings only).

### Build notes
Build: passed (0 errors) | Tests: 512 passed, 0 failed | Files written and re-read: SongFormViewModel.cs, SongFormViewModelTests.cs

### Notes
- Test helper is `CreateSut(...)` (not `CreateSongFormViewModel`); mocks passed as optional params (no `_artistServiceMock` field). `SelectArtistCommand` is `RelayCommand<AutocompleteSuggestion>` (synchronous). Real `AutocompleteSuggestion(Headline, SupportingText, Data)` with `Data` an `ArtistListItem` — plan sketch's `{ Id, Headline }` shape adapted to the actual record signature.

---
## Task: T2 (BUG-051, Major) — stale-search race in `SearchArtistsAsync`
**Plan:** plan.md § Task 2 (T2)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — added `_searchGeneration` field + generation-counter guard in `SearchArtistsAsync` (BUG-051 fix)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — added regression test `SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins`

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-13 (BUG-051) | A slower, earlier-issued search query must not clobber a faster, later-issued query's results | `SongFormViewModel.SearchArtistsAsync` (`_searchGeneration` guard) | `SongFormViewModelTests.SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins` |

### Verification evidence
- **Red** (before fix): `dotnet test --filter SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins` →
  `Assert.Equal() Failure — Expected: "Queen", Actual: "Querido"` (older completion overwrote `ArtistSuggestions`). Failed: 1, Passed: 0.
- **Green** (after fix): same filter → `Aprovado! Com falha: 0, Aprovado: 1`.
- **Full suite:** `dotnet test` → `Com falha: 0, Aprovado: 513, Ignorado: 0, Total: 513` (baseline 512 + 1 new).
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 6 projects, 0 errors, 22 warnings (DevExpress eval + pre-existing NU1903/nullable warnings only).

### Build notes
Build: passed (0 errors) | Tests: 513 passed, 0 failed | Files written and re-read: SongFormViewModel.cs, SongFormViewModelTests.cs

### Notes
- Fix approach: self-contained `int _searchGeneration` counter (Helder-approved 2026-07-21) — captured at entry (`var gen = ++_searchGeneration;`), checked before assigning `ArtistSuggestions` after the `await`. No `CancellationToken` threading (rejected in plan.md in favor of this approach).
- Confirmed `IArtistService.SearchArtistsByNameAsync(string query, int maxResults = 5, CancellationToken ct = default)` returns `IEnumerable<ArtistListItem>` — mocked via `SetupSequence` with two `TaskCompletionSource<IEnumerable<ArtistListItem>>` to control completion order deterministically.

---
## Task: T3 (REQ-ACREATE-03) — retain typed text on blur (was BUG-008 clear-on-blur)
**Plan:** plan.md § Task 3 (T3)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Spec-gap adjudication (orchestrator-authorized, no re-escalation)
A prior agent flagged that the existing test `ArtistBlurredWithoutSelection_NoPriorSelection_ClearsField` (`AC-B8-01`) encoded the superseded BUG-008 clear-on-blur behavior, in direct conflict with the approved REQ-ACREATE-03 (retain-on-no-match). The orchestrator adjudicated **Option A**: repurpose the test in place (not delete) to encode the new approved behavior, since `requirements.md` explicitly documents REQ-ACREATE-03 as superseding BUG-008. Implemented per that decision — see `requirements.md` spec-updated note below.

### Changed files:
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/requirements.md` — added `> **Spec updated [2026-07-21]:**` note under REQ-ACREATE-03 documenting the AC-B8-01 repurpose.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — repurposed `ArtistBlurredWithoutSelection_NoPriorSelection_ClearsField` → `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError` (tag changed `AC-B8-01` → `REQ-ACREATE-03`); asserts `ArtistSearchText` retained, `ArtistHasError == true`, `IsArtistLocked == false`.
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `OnArtistBlurredWithoutSelection`: removed `ArtistSearchText = string.Empty;` in the no-locked-artist branch; added `ArtistHasError = true;` to surface the validation error. Restore-prior-selection branch (`else`) unchanged.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/tasks.md` — ticked T3.

### AC-B8-02 disposition
`ArtistBlurredWithoutSelection_WithPriorSelection_RestoresName` (AC-B8-02) covers the **restore-prior-selection** branch (an artist was already locked/selected, user re-typed then blurred without picking a new suggestion) — a different branch from the one REQ-ACREATE-03 changes. It does not encode the superseded clear-on-no-match behavior, so it was left untouched. It still passes (2/2 green in the filtered run).

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-03 | Blur with unmatched (no local match) typed text retains the text and surfaces a validation error, never clears it | `SongFormViewModel.OnArtistBlurredWithoutSelection` (no-locked-artist branch) | `SongFormViewModelTests.ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError` |

### Verification evidence
- **Red** (before fix, filter `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError`):
  `Assert.Equal() Failure: Strings differ — Expected: "partial text", Actual: ""`. Failed: 1, Passed: 0.
- **Green** (after fix, filter `ArtistBlurredWithoutSelection`, covers both AC-B8-02 and the repurposed test): `Aprovado! Com falha: 0, Aprovado: 2, Total: 2`.
- **Full suite:** `dotnet test MyVocaList.Tests` → `Com falha: 0, Aprovado: 513, Ignorado: 0, Total: 513` (count unchanged — test repurposed, not added).
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 6 projects, 0 errors, 22 warnings (DevExpress eval + pre-existing NU1903/nullable warnings only).

### Build notes
Build: passed (0 errors) | Tests: 513 passed, 0 failed | Files written and re-read: requirements.md, SongFormViewModelTests.cs, SongFormViewModel.cs, tasks.md

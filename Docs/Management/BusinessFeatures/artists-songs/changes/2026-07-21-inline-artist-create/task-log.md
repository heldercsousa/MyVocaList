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
**Phase:** T4 complete (To Review). Worktree `MyVocaList-inline-ac` on `feat/inline-artist-create`.

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

---
## Task: T4 (BUG-052, Major) — edit-mode hydration shows the locked artist and fires no search
**Plan:** plan.md § Task 4 (T4)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Hydration method (confirmed)
The actual edit-mode artist hydration path is `SongFormViewModel.InitializeArtistField()` (not `[QueryProperty]` `OnArtistIdChanged` — that hook explicitly defers to `InitializeArtistField()` because Shell query-property arrival order is not guaranteed). It is called from the page's `OnAppearing` after all `[QueryProperty]` values (`ArtistIdRaw` → `ArtistId`, `ArtistName`) have been applied. Signature: `public void InitializeArtistField()` — reads `ArtistId`/`ArtistName` (already-set query properties), no parameters.

There is no coupling in the ViewModel between `ArtistSearchText` and `SearchArtistsAsync` — the search command (`SearchArtistsCommand`) is invoked only from the page's `TextChanged` event binding in XAML, not from an `OnArtistSearchTextChanged` partial hook. So programmatic hydration (`InitializeArtistField`, and `LoadSongForEditAsync`'s `_isHydrating` window) cannot trigger a search at the VM level by construction — the "no search" half of BUG-052 was already correct in the VM layer; no `_isHydrating` guard was needed for it.

### BUG-052 disposition
**Guard fix applied.** `InitializeArtistField()` set `SelectedArtistId`/`SelectedArtistName`/`ArtistSearchText` but never set `IsArtistLocked = true` — so edit mode showed the artist name but with the field unlocked (editable/searchable), which is the visible half of BUG-052. Added `IsArtistLocked = true;` inside the `if (ArtistId > 0)` branch, reusing the existing pattern from `SelectArtist`/`ResolveAndLockArtistAsync` (no new flag introduced). The "fires no search" assertion in the guard test passes without any code change (see above), confirming that half was never broken at the VM layer.

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `InitializeArtistField()`: added `IsArtistLocked = true;` when `ArtistId > 0` (BUG-052 fix).
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — added guard regression test `InitializeArtistField_EditModeHydration_ShowsLockedArtistWithoutSearch`.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/tasks.md` — ticked T4.

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-14 | Edit-mode hydration shows the stored artist as locked (`IsArtistLocked == true`) and fires no suggestion search | `SongFormViewModel.InitializeArtistField` | `SongFormViewModelTests.InitializeArtistField_EditModeHydration_ShowsLockedArtistWithoutSearch` |

### Verification evidence
- **Red** (before fix, filter `InitializeArtistField_EditModeHydration_ShowsLockedArtistWithoutSearch`): `Assert.True() Failure — Expected: True, Actual: False` (on `IsArtistLocked`). Failed: 1, Passed: 0.
- **Green** (after fix, same filter): `Aprovado! Com falha: 0, Aprovado: 1, Total: 1` (run together with the pre-existing `InitializeArtistField` test: 2/2 green).
- **Full suite:** `dotnet test MyVocaList.Tests` → `Com falha: 0, Aprovado: 514, Ignorado: 0, Total: 514` (513 baseline + 1 new guard test).
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 6 projects, 0 errors, 22 warnings (DevExpress eval + pre-existing NU1903/nullable warnings only). Note: a concurrent duplicate foreground build run raced the NuGet restore lock and reported a spurious NU1903-as-error; the authoritative single run completed with 0 errors.

### Build notes
Build: passed (0 errors) | Tests: 514 passed, 0 failed | Files written and re-read: SongFormViewModel.cs, SongFormViewModelTests.cs, tasks.md

---
## Task: T6 — `AutocompleteSuggestion` create-sentinel discriminator (Level C)
**Plan:** plan.md § Task 6 (T6)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Record shape (confirmed)
`Contracts/Models/AutocompleteSuggestion.cs` is a positional record `(string Headline, string SupportingText, object Data)`. Adding a positional parameter would break every existing call site (all use positional construction). Chose the non-breaking approach: added `public bool IsCreateNew { get; init; }` (default `false`) as a body member on the existing positional record — no change to the positional parameter list or order. The raw typed text for the sentinel row is carried by the existing `Headline` member (no new `RawText` property needed — T7/T8 read `Headline` on the sentinel row).

### Level-C no-test decision
Per `testing.md § TDD Level Guidance` this is Level C (pure DTO addition, no branching logic, no business rule) — no mandatory test. Verification is the build + a full-solution grep of every `new AutocompleteSuggestion(...)` construction site to confirm the addition is non-breaking (positional args and count unchanged everywhere).

### Construction sites grepped (all positional, all unaffected)
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs:288`
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs:288`
- `MyVocaList.Tests/Unit/Components/AutocompleteSuggestionsPropagationTests.cs:76,94`
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelBug044Tests.cs:56`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs:335`

### Changed files:
- `Contracts/Models/AutocompleteSuggestion.cs` — added `IsCreateNew` init-only property (default `false`) with XML doc noting `Headline` carries the raw typed text for the sentinel row.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/tasks.md` — ticked T6.

### Verification evidence
- **Build:** `dotnet build` (full solution, 8 projects incl. `net10.0-android`) → `ok dotnet build: 8 projects, 0 errors, 139 warnings` (all warnings pre-existing NU1903/DX-trial/CA1416, unrelated to this change).
- **Full suite:** `dotnet test` → `Aprovado! Com falha: 0, Aprovado: 514, Ignorado: 0, Total: 514` (unchanged from T4 baseline — no new test, per Level-C decision).

### Build notes
Build: passed (0 errors) | Tests: 514 passed, 0 failed | Files written and re-read: AutocompleteSuggestion.cs, tasks.md

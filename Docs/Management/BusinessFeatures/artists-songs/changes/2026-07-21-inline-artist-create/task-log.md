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

---
## Task: T7 — inline "create new artist" wiring (VM command + page glue)
**Plan:** `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/plan.md` (Task 7)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Confirmed signatures / decisions (on file-open)
- `IArtistService.CreateArtistAsync(string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default) → (bool success, string message, Artist? artist)`. Inline path calls the 1-arg form `CreateArtistAsync(name)`; test mocks match `CreateArtistAsync("...", null, null, It.IsAny<CancellationToken>())`.
- **No pre-existing private lock helper** — `SelectArtist` held the lock logic inline. Extracted a shared `private void LockArtist(int id, string name)` (sets `SelectedArtistId`/`SelectedArtistName`/`ArtistSearchText`, `IsArtistLocked = true`, clears `ArtistSuggestions`/`ArtistHasError`/`ArtistErrorText`); both `SelectArtist` and `CreateArtistInlineAsync` now call it — single lock implementation, no duplication.
- **Raw typed text carried in `Headline`** (T6 decision: `AutocompleteSuggestion.IsCreateNew` doc states "raw typed text is carried in Headline"). Sentinel built as `new AutocompleteSuggestion(text, string.Empty, text) { IsCreateNew = true }`. The "Add «text» as a new artist" display decoration is deferred to the T8 XAML `ItemTemplate` — keeps code-behind glue-only and avoids storing wrapped text. Routing passes `suggestion.Headline` (the raw text) straight to `CreateArtistInlineCommand`.

### Red → Green evidence
- **Red:** `dotnet test --filter CreateArtistInline` → 2× `error CS1061: 'SongFormViewModel' não contém uma definição para "CreateArtistInlineCommand"` (SongFormViewModelTests.cs lines 358, 377). Both tests fail (command undefined).
- **Green:** after implementing `CreateArtistInlineAsync` + command → `Aprovado! Com falha: 0, Aprovado: 2, Total: 2`.

### AC mapping
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ACREATE-04/08 | inline create success locks created artist, clears error | `SongFormViewModel.CreateArtistInlineAsync` → `LockArtist` | `CreateArtistInline_Success_LocksCreatedArtistAndClearsError` |
| REQ-ACREATE-05 | create failure maps error, retains text, no lock | `SongFormViewModel.CreateArtistInlineAsync` else-branch | `CreateArtistInline_Failure_MapsErrorAndRetainsText` |
| REQ-ACREATE-02/10 | sentinel appended (last) for any non-ws text | `SongFormPage.OnArtistItemsRequested` | on-device (T10) |
| REQ-ACREATE-03 | no-match → list holds only the create row | `OnArtistItemsRequested` (append regardless of matches) | on-device (T10) |
| REQ-ACREATE-06/07 | no prompt; validation via ArtistService only | VM delegates to `CreateArtistAsync`; no VM-side validation | (by design) |

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — extracted `LockArtist`; added `CreateArtistInlineAsync` + `CreateArtistInlineCommand` (`AsyncRelayCommand<string>`), wired in ctor.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistItemsRequested` appends the create sentinel (last item, non-ws text); `OnArtistSelectionChanged` routes `IsCreateNew` → `CreateArtistInlineCommand`, else `SelectArtistCommand`.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — 2 Level-A tests (success-locks, failure-maps-retains).
- `Docs/.../tasks.md` — ticked T7.

### Build notes
Build: passed (`dotnet build MyVocaList.csproj -f net10.0-android` → exit 0, warnings only) | Tests: 516 passed, 0 failed (514 baseline + 2 new) | Files written and re-read: SongFormViewModel.cs, SongFormPage.xaml.cs, SongFormViewModelTests.cs

### E2E note
User-facing behavior (autocomplete dropdown row + selection routing). Emulator not launched in this task — T8 (XAML ➕ render) + T10 (on-device manual, Helder) cover the visual/E2E gate per the plan. `E2E: emulator not available — requires manual verification (T10)`.

---
## Task: T8 — `ItemTemplate` distinct ➕ render (`SongFormPage.xaml`)
**Plan:** `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/plan.md` (Task 8)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Approach
- Confirmed via T7 handoff that the sentinel row is built as `new AutocompleteSuggestion(text, string.Empty, text) { IsCreateNew = true }` — `Headline` carries the **raw typed text**, not the wrapped display string. T8 owns composing the `Add "{text}" as a new artist` wrapper.
- Existing `AutoCompleteEdit.ItemTemplate` (`SongFormPage.xaml`) had a single always-visible two-`Label` layout (Headline + SupportingText). Rather than a `DataTrigger` toggling `Setter`s on the same `Label`, used direct `IsVisible` bindings on `IsCreateNew` (and its inverse via the existing `InverseBoolConverter` resource already used elsewhere on the same page) to switch between:
  - **Create row** (`IsCreateNew == true`): a `BoxView` styled `{StaticResource Divider}` (reused — same style key already used for the BottomSheet sections on this page) above the row as a top divider; a leading ➕ `Label` (`TextColor="{StaticResource Primary}"`); and a `FormattedString` (`Span`s) composing `Add "{Headline}" as a new artist` — no hardcoded concatenation, no new converter.
  - **Real-match row** (`IsCreateNew == false`): unchanged — `Headline` + optional `SupportingText`, same style classes as before (`Body.Large`/`Body.Medium`, `OnSurfaceVariant`).
- Reused resources only: `Divider`, `Primary`, `OnSurfaceVariant`, `InverseBoolConverter`, `IsNotNullConverter`, style classes `Body.Large`/`Body.Medium` — no invented MD3 style keys, no hardcoded colors.
- This is an incremental single-file XAML edit (`SongFormPage.xaml` only, one `ItemTemplate` block).

### Level C — no mandatory test
Visual-only change (dropdown row rendering). Per `testing.md` Level C and the plan's own designation ("Level C — no mandatory test; covered by on-device T10"), no unit test was written. Coverage is the on-device E2E gate (T10, manual, Helder) plus the build check below.

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — `AutoCompleteEdit.ItemTemplate`: added divider + ➕ glyph + `FormattedString` wrapper for `IsCreateNew` rows; real-match rows unchanged in content/style.
- `Docs/.../tasks.md` — ticked T8.

### Build notes
Build: passed (`dotnet build MyVocaList.csproj -f net10.0-android` → exit 0, 0 errors, warnings only — pre-existing NU1903/CS8600 etc., none new) | Tests: 516 passed, 0 failed, 0 skipped (unchanged count — visual-only change, no test impact) | Files written and re-read: SongFormPage.xaml

### E2E note
`E2E: emulator not available — requires manual verification (T10, Helder)`. Visual distinctness (➕ glyph, top divider, wrapper text) to be confirmed on-device per the plan's Task 10 checklist.

---
## Task: CODE-REVIEW fold-in — M1/M2/M3/M4/M5 findings
**Plan:** CODE-REVIEW findings on T1–T8 (2026-07-21)
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Findings applied / skipped
- **M1 (applied)** — empty-blur (`OnArtistBlurredWithoutSelection`, no-locked branch) no longer sets `ArtistHasError` when `ArtistSearchText` is empty/whitespace; only flags an error when the user actually typed unmatched text. Aligns to REQ-ACREATE-03 ("unmatched TEXT").
- **M2 (applied)** — `SearchArtistsAsync`'s generation counter now uses `Interlocked.Increment(ref _searchGeneration)` instead of `++_searchGeneration`; hardening only, no behavior change. `gen != _searchGeneration` compare unchanged.
- **M3 (applied)** — `CreateArtistInlineAsync` failure branch now also clears `ArtistSuggestions = []` so the dropdown (incl. the "Add…" row) doesn't linger behind the error. Success path (`LockArtist`) untouched — already cleared suggestions.
- **M4 (applied)** — `SongFormPage.xaml.cs` `OnArtistSelectionChanged`: the create-sentinel branch now guards with `CreateArtistInlineCommand.CanExecute(...)` before `Execute(...)`, preventing a second selection event from re-triggering an in-flight create (BUG-049-style double-tap risk). Trivial, no behavior change to the real-match branch.
- **M5 (skipped)** — whitespace-only confirming test for `OnArtistItemsRequested`'s "no create row" guard. That method lives in `SongFormPage.xaml.cs` (page code-behind, `ItemsRequestEventArgs`/`MAUI` types) with no seam reachable from a ViewModel-level unit test in this test project (no page-level test harness exists for this class). Forcing a test here would require introducing a new test-harness pattern outside this task's scope — logged as skip per the briefing's "SKIP and note why" instruction. The `IsNullOrWhiteSpace` guard itself is unchanged and still present in code.

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — M1 guard on `OnArtistBlurredWithoutSelection`; M2 `Interlocked.Increment`; M3 `ArtistSuggestions = []` on create failure.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — M4 `CanExecute` guard on `OnArtistSelectionChanged`'s create-sentinel branch.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — new test `ArtistBlurredWithoutSelection_EmptyText_NoErrorNoLock` (M1); extended `CreateArtistInline_Failure_MapsErrorAndRetainsText` with `Assert.Empty(sut.ArtistSuggestions)` (M3).

### M1 Red → Green evidence
- **Red** (before guard): `dotnet test --filter FullyQualifiedName~SongFormViewModelTests` →
  `ArtistBlurredWithoutSelection_EmptyText_NoErrorNoLock [FAIL]` — `Assert.False() Failure: Expected: False, Actual: True` (line 308, `ArtistHasError` was `true` on empty-text blur). 1 failed, 41 passed of 42 in that file.
- **Green** (after guard added — `if (!string.IsNullOrWhiteSpace(ArtistSearchText)) ArtistHasError = true;`): full suite green (see below).

### Build notes
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 6 projects, 0 errors, 22 warnings (DevExpress eval + pre-existing NU1903/CS8600 etc., none new).
- `dotnet test` (full suite) → **Com falha: 0, Aprovado: 517, Ignorado: 0, Total: 517** — baseline 516 + 1 net new test (M1) + M3 was an assertion added to an existing test (no count delta). The previously-flaky SQLite test `SongRepositoryTests.GetByTitlesCollatedAsync_NoMatches_ReturnsEmpty` passed in this same full run — no isolation re-run needed (only required if it fails).
- Files written and re-read: `SongFormViewModel.cs`, `SongFormPage.xaml.cs`, `SongFormViewModelTests.cs` — all three re-read post-edit to confirm the changes landed at the correct location.

---

## Bug: BUG-053 (Major, UI-only) — SongFormPage ItemTemplate FormattedString crash on Artist typing

**Symptom:** Typing ≥3 chars in the Artist field on SongFormPage threw a XAML runtime error at Position 57:50 — "Cannot assign property FormattedString: Property does not exist, or is not assignable, or mismatching type between value and property".

**Root cause:** The create-row `ItemTemplate` used the property-element `<Label.FormattedString>`, but Label's property is named `FormattedText` (`FormattedString` is the type of the value, not the property) — the parser could not resolve a `FormattedString` property on Label and crashed at first render of the AutoCompleteEdit dropdown.

**Fix:** Renamed the property-element `<Label.FormattedString>` → `<Label.FormattedText>` (opening and closing tags); the inner `<FormattedString>`/`<Span>` composition (`Add "{Headline}" as a new artist`) is unchanged and correct.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`

### Build notes
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 6 projects, **0 errors**, 32 warnings (DevExpress eval + pre-existing NU1903/CS8600, none new).
- `dotnet test` (full suite) → **Com falha: 0, Aprovado: 517, Ignorado: 0, Total: 517** — no regression.

### Regression test
UI-only rendering crash — no XAML render seam in the unit test project, so an automated regression test is not feasible (per `bug-tracking.md`, Major UI-only → document manual E2E).

Manual E2E: pending on-device (Helder T10) — typing ≥3 chars in the Artist field renders the ➕ create-row without crashing.

---
### Checkpoint (live — overwritten in place)
**Branch/worktree:** feat/inline-artist-create @ MyVocaList-inline-ac (base 8d33547)
**Progress:** T10 defect fixes BUG-054…059. Step 1 of 5 (now attempting BUG-056 Red test).
**Last build/test:** baseline 517/517 green (inherited).
**Next command:** add SearchArtistsCoreAsync Red test → dotnet test --filter SearchArtistsCoreAsync
**Context manifest:**
- MyVocaList/UI/ViewModels/SongFormViewModel.cs — SearchArtistsAsync/LoadSongForEditAsync/LockArtist/CreateArtistInlineAsync
- MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs — OnArtistItemsRequested/OnArtistSelectionChanged
- MyVocaList/UI/Pages/Songs/SongFormPage.xaml — AutoCompleteEdit :28-76
- MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs — CreateSut, MakeSongServiceWithSong, search/hydration tests
- Infra/Repository/SongRepository.cs — GetByIdAsync :53 (needs Include OriginalArtist for name hydration)
- Docs/.../2026-07-21-inline-artist-create/task-log.md (worktree copy) — this log

---
## Bug: BUG-056 (Major) — artist search race + first-search-empty (page-VM coupling)
**Status:** To Review
**Severity:** Major (autocomplete unusable on device; testable VM seam -> regression test)

**Root cause:** `SongFormViewModel.SearchArtistsAsync` assigned `ArtistSuggestions` inside a fire-and-forget `RunOnUiThread`; the page provider (`SongFormPage.OnArtistItemsRequested.RequestAsync`) read `ViewModel.ArtistSuggestions` immediately after `await`, before that UI dispatch landed -> first read saw `[]`, later reads saw the prior query's stale list. The `Interlocked` generation guard was irrelevant to this timing gap.

**Fix:** Introduced `public Task<IReadOnlyList<AutocompleteSuggestion>> SearchArtistsCoreAsync(string)` that maps and RETURNS the current query's results directly; the page provider now consumes that return value instead of reading `ArtistSuggestions`. The shared observable `ArtistSuggestions` is still assigned for the latest query only (BUG-051 generation guard preserved), so latest-query-wins holds for other observers. `SearchArtistsAsync` (command target) now delegates to the core method — the existing BUG-051 test path is unchanged.

**Regression risk:** Low — return-value path is deterministic; prior command/observable behavior preserved for the BUG-051 test.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `SearchArtistsCoreAsync` (returns results) + `SearchArtistsAsync` delegates.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistItemsRequested` consumes returned matches (no `ArtistSuggestions` read).
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — `SearchArtistsCoreAsync_ReturnsCurrentQueryResultsDirectly`.

### AC traceability
| AC ID | Criterion | Implementation | Test |
|-------|-----------|----------------|------|
| REQ-ACREATE-13 (BUG-056) | Search returns current query's results directly; no read-before-dispatch gap | `SongFormViewModel.SearchArtistsCoreAsync` + `OnArtistItemsRequested` | `SearchArtistsCoreAsync_ReturnsCurrentQueryResultsDirectly` (Red->Green) |

### Verification evidence
- **Red:** new test -> `error CS1061: SearchArtistsCoreAsync` undefined (assembly did not compile).
- **Green:** full suite -> `Com falha: 0, Aprovado: 520, Total: 520`.
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` -> exit 0, 0 errors (NU1903/DX-eval warnings only).

---
## Bug: BUG-055 (Major) — edit-mode artist hydration empty; edit-save dropped artist link
**Status:** To Review
**Severity:** Major (core edit flow: saved song opened for edit showed empty Artist -> Save blocked -> catalog cascade BUG-059)

**Root cause:** `LoadSongForEditAsync` hydrated Title/Version/FeaturedArtists/Lyrics but never set `SelectedArtistId`/`SelectedArtistName`/`ArtistSearchText`. `InitializeArtistField` only hydrates from the `artistId` QueryProperty (=0 in normal edit navigation), so the artist field stayed empty. With `SelectedArtistId` null, the Save guard blocked with "Artist is required".

**Fix:** `LoadSongForEditAsync` now hydrates `SelectedArtistId = song.ArtistId`, `SelectedArtistName`/`ArtistSearchText` from `song.OriginalArtist.Name`, and locks the field (`IsArtistLocked = true`, REQ-ACREATE-14) inside the existing `_isHydrating` window so no suggestion search fires. Because the field is locked in edit mode and `ISongService.UpdateSongAsync` preserves the loaded song's `ArtistId`, the artist link is carried through Save purely by hydrating the guard — no service-signature change. To supply the name on device, `SongRepository.GetByIdAsync` now eager-loads `OriginalArtist` (scope note below).

**Regression risk:** Low–Medium. `IsArtistLocked` on edit is now always true (was: only API-imported-without-manual-edits). Intentional per REQ-ACREATE-14 and more correct — `UpdateSongAsync` never accepted a new artistId, so an editable-but-unpersistable artist field was misleading. No existing test asserts unlocked-on-edit for a non-API song.

### Scope note (file outside the briefing's owned set — documented, not silent)
`Infra/Repository/SongRepository.cs` `GetByIdAsync` gained `.Include(s => s.OriginalArtist)`. The briefing assumed the loaded song carried the artist name, but `GetByIdAsync` did not eager-load the navigation and no `GetArtistByIdAsync` exists. One-line eager-load; consumers of `GetByIdAsync`: `SongService.UpdateSongAsync`/`GetSongByIdAsync` (mutate+save — Unchanged artist, safe), `SongResolutionService` (read — harmless), `QueueServiceNew` (read — harmless). No service interface changed; `ArtistService` untouched. Alternative (new `GetArtistByIdAsync` + DI + interface) rejected as larger architectural change for a name lookup.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `LoadSongForEditAsync` artist hydration + always-lock on edit.
- `Infra/Repository/SongRepository.cs` — `GetByIdAsync` eager-loads `OriginalArtist`.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — `LoadSongForEdit_ExistingSong_HydratesArtistAsLocked`, `SaveAsync_EditMode_AfterHydration_PreservesArtistLink`.

### AC traceability
| AC ID | Criterion | Implementation | Test |
|-------|-----------|----------------|------|
| REQ-ACREATE-14 (BUG-055) | Edit-mode hydration shows stored artist as locked, no search | `SongFormViewModel.LoadSongForEditAsync` | `LoadSongForEdit_ExistingSong_HydratesArtistAsLocked` (Red->Green) |
| REQ-ACREATE-08 (BUG-055) | Edit-save preserves the artist link | Save guard hydrated + service preserves `song.ArtistId` | `SaveAsync_EditMode_AfterHydration_PreservesArtistLink` (Red->Green) |

### Verification evidence
- **Red:** both tests failed pre-fix (hydration assertions / guard blocked Save so `UpdateSongAsync` never called).
- **Green:** full suite -> `Com falha: 0, Aprovado: 520, Total: 520`.
- **Build:** android build exit 0, 0 errors.
- **Manual E2E (on device):** deferred to T10 re-run (Helder) — name shown + locked on edit; catalog populates (BUG-059 cascade).

---
## Bug: BUG-054b / BUG-057 / BUG-058 (XAML cluster, UI-only) — Artist AutoCompleteEdit wiring
**Status:** To Review
**Severity:** Major (UI-only; no unit seam — manual E2E deferred to T10 re-run, Helder)

Single incremental XAML edit pass on the one `AutoCompleteEdit` in `SongFormPage.xaml`, then build (0 errors).

- **BUG-054b (lock via IsEnabled disabled the clear icon):** replaced `IsEnabled="{Binding IsArtistLocked, Converter=InverseBoolConverter}"` with `IsReadOnly="{Binding IsArtistLocked}"` and added `ClearIconVisibility="Auto"`, so a locked field is non-editable but the clear (X) icon still works. DX API confirmed via Context7/DX docs (DevExpress MAUI 25.2.4): `IsReadOnly` (editor base), `ClearIconVisibility` (EditBase, type `Visibility`, value `Auto`).
- **BUG-057 (invisible error):** added a dedicated visible `Label` bound to `ArtistErrorText`, `IsVisible="{Binding ArtistHasError}"`, mirroring the existing `PasteUrlError` label on this same page (`StyleClass="Body.Small"` + `TextColor="{StaticResource Error}"`). No invented style keys, no hardcoded colors. The DX editor's own `HasError`/`ErrorText` bindings are retained (red border) but were not surfacing text on-device.
- **BUG-058 (record ToString leaked into Text):** added `DisplayMember="Headline"`. DX doc (25.2.4) confirms `ItemsEditBase.DisplayMember` sets the data-source field whose value is written into the edit box on selection — so `Headline` (artist name / raw typed text) is written instead of `AutocompleteSuggestion.ToString()`.

### DX API names vs briefing
All three (`IsReadOnly`, `ClearIconVisibility="Auto"`, `DisplayMember="Headline"`) matched the briefing exactly — no deviations. Confirmed against DevExpress MAUI 25.2.4 docs (`DevExpress.Maui.Editors.AutoCompleteEdit` / `ItemsEditBase.DisplayMember` / `EditBase.ClearIconVisibility`).

### Observation (not fixed — out of scope)
With `IsReadOnly` locking, tapping the clear (X) icon clears `ArtistSearchText` but `IsArtistLocked` stays true (no unlock-on-clear handler). The briefing scoped BUG-054b to the IsReadOnly/ClearIconVisibility swap only; unlock-on-clear would be a new requirement. Flagged for Helder's T10 observation.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — AutoCompleteEdit attributes (IsReadOnly, ClearIconVisibility, DisplayMember) + adjacent artist error Label.

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` -> exit 0, 0 errors (warnings only). XAML compiled — all three DX properties valid on the control.
- **Manual E2E:** deferred to T10 re-run (Helder): locked field keeps clear icon; error text visible; no `IsCreateNew = True` leak into the box.

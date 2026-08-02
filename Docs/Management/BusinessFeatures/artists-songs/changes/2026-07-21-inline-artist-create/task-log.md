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
**Phase:** Implementation COMPLETE (T1–T9) on `feat/inline-artist-create` (worktree `MyVocaList-inline-ac`, off `develop`). Code review PASS (APPROVE-WITH-MINOR) — findings M1–M4 folded in, M5 skipped (no unit seam). Full suite **517/517 green**. Docs consolidated to develop. **Awaiting T10 (on-device, Helder)** → then close BUG-027/050/051/052 and unblock the Artists & Songs Catalog. Branch not pushed (wincred — Helder pushes).

Baseline was **511/511** (plan.md's 501 was an estimate; per-task counts below offset +10). Commits: T1 `1a7bdaa` · T2 `40fcb2c` · T3 `c96366a` · T4 `08a397a` · T6 `55e7864` · T7 `d386970` · T8 `4d2b78c` · fold-in `50ad9eb`.

## T5 — DX capability spike (Option A CONFIRMED via Context7, DevExpress MAUI 25.2.4)
`AutoCompleteEdit` supports a full custom `ItemTemplate`/`DataTemplate` for distinct row rendering; the app supplies suggestions via a custom async provider (`OnArtistItemsRequested`) whose returned items are the authoritative drop-down content, and `AutoCompleteEditTextChangeReason.ItemSelected` fires on selecting any displayed row. Guardrail: the sentinel row's rendered text (`Add "…" as a new artist`) contains the typed text as a substring, so it survives any residual built-in Contains-filtering. → T7/T8 proceed with the synthetic ➕ row (Option A); Option B not needed. **REQ-ACREATE-11 satisfied via Option A.** No production code (spike).

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
## Task: T9 — full suite + consolidated AC traceability matrix
**Status:** Done (docs on develop). **Full suite: 517/517 green** (`dotnet test`, 0 failed, 0 skipped; the SQLite parallel-flake `GetByTitlesCollatedAsync_NoMatches_ReturnsEmpty` passed in the same run — documented in `constraints-registry.md § EF Core / SQLite`, unrelated to this branch).

### Consolidated AC traceability matrix (REQ-ACREATE-01…14)
| AC ID | Criterion (abbrev.) | Implementation location | Test / verification |
|-------|---------------------|-------------------------|---------------------|
| REQ-ACREATE-01 | Existing local autocomplete unchanged while typing | `SongFormPage.OnArtistItemsRequested` (search path preserved) | on-device T10 + regression suite (517 green) |
| REQ-ACREATE-02 | Distinct "Add «text»" row appended last, visually separated (➕ + divider) | `OnArtistItemsRequested` (append last) + `SongFormPage.xaml` ItemTemplate | on-device T10 (visual) |
| REQ-ACREATE-03 | No-match: typed text retained (never cleared), create row still offered | `SongFormViewModel.OnArtistBlurredWithoutSelection` (no-locked branch) | `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError`; empty-blur guarded by `ArtistBlurredWithoutSelection_EmptyText_NoErrorNoLock` (M1) |
| REQ-ACREATE-04 | Select "Add…" → CreateArtistAsync; success locks created artist, clears error | `SongFormViewModel.CreateArtistInlineAsync` → `LockArtist` | `CreateArtistInline_Success_LocksCreatedArtistAndClearsError` |
| REQ-ACREATE-05 | Create failure surfaces error, retains text, no artist created, no dialog | `CreateArtistInlineAsync` else-branch (clears suggestions, sets error) | `CreateArtistInline_Failure_MapsErrorAndRetainsText` (+ M3 `Assert.Empty(ArtistSuggestions)`) |
| REQ-ACREATE-06 | No confirmation prompt; only name captured inline | `CreateArtistInlineAsync` (direct call, no prompt) | by design (code review PASS) |
| REQ-ACREATE-07 | Validation is ArtistService's single source of truth; not re-implemented in VM | VM delegates to `IArtistService.CreateArtistAsync`/`ValidateNameInput` | by design (code review PASS — no VM-side validation) |
| REQ-ACREATE-08 | After inline create+select, saving persists song with new ArtistId | lock path sets `SelectedArtistId` → existing save guard | on-device T10 (E2E save) |
| REQ-ACREATE-09 | Existing VM + full suites remain green; ArtistService unchanged | no ArtistService change | 517/517 green |
| REQ-ACREATE-10 | Create affordance appears for any non-whitespace typed text | `OnArtistItemsRequested` append (guarded by `IsNullOrWhiteSpace`) | on-device T10; whitespace-guard code present (M5 unit test skipped — no page seam) |
| REQ-ACREATE-11 | Fallback branch (Option B) if DX can't surface synthetic row | N/A — **Option A confirmed** (T5); Option B not needed | T5 spike (Context7) |
| REQ-ACREATE-12 | BUG-050: selecting a suggestion sets IsArtistLocked=true | `SongFormViewModel.SelectArtist` → `LockArtist` | `SelectArtist_ExistingSuggestion_LocksField` (Red→Green) |
| REQ-ACREATE-13 | BUG-051: latest search query wins; earlier slower query never clobbers | `SearchArtistsAsync` (`Interlocked` generation counter) | `SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins` (Red→Green) |
| REQ-ACREATE-14 | BUG-052: edit-mode hydration shows locked artist, fires no search | `SongFormViewModel.InitializeArtistField` (`IsArtistLocked=true`) | `InitializeArtistField_EditModeHydration_ShowsLockedArtistWithoutSearch` (Red→Green) |

**On-device-only rows (T10, Helder):** REQ-ACREATE-01/02/08/10 visual+E2E confirmation, plus REQ-ACREATE-04/05 full E2E (novel artist → ➕ → created+locked+saved; exact-existing name → duplicate error, no orphan).

---
## Bug: BUG-053 (Major, UI-only) — SongFormPage ItemTemplate FormattedString crash on Artist typing
**Status:** Fixed (`8d33547`, worktree `feat/inline-artist-create`). Found on-device by Helder (T10 prep): typing 3 chars threw `Cannot assign property FormattedString… Position 57:50`.
- **Root cause:** the create-row `ItemTemplate` used property-element `<Label.FormattedString>`, but `Label`'s property is `FormattedText` (`FormattedString` is the value *type*, not the property).
- **Fix:** renamed the property-element `<Label.FormattedString>` → `<Label.FormattedText>` (open+close). Inner `FormattedString`/`Span` composition unchanged; real-match rows untouched. XAML-only, no `.cs`.
- **Evidence:** `dotnet build -f net10.0-android` → 0 errors; `dotnet test` → 517/517.
- **Manual E2E:** ✅ confirmed on-device by Helder 2026-07-22 (Part A — typing ≥3 chars renders the ➕ row, no crash).
> Full entry also recorded in the worktree copy of this file; reconcile at merge (develop authoritative).

---
## T10 outcome (2026-07-22) — Helder ran the on-device checklist; root-cause triage
Result: **Part A (BUG-053) fixed; Part B/C mostly FAIL — 6 new defects (BUG-054…059).** The T1–T9 fixes for BUG-050/051/052 passed their VM unit tests but **do not hold on-device**, because the real defects live in the DX `AutoCompleteEdit` wiring/XAML — the seam the unit suite never exercises. Root causes below are from a read-only code trace against the worktree (file:line exact).

| Item | Result | Defect | Root cause (file:line) | Fix layer |
|------|--------|--------|------------------------|-----------|
| A (BUG-053) | ✅ | — | fixed `8d33547` | — |
| a (retain text) | ✅ | — | REQ-ACREATE-03 holds | — |
| b (lock) | ❌ | **BUG-054** | `SongFormPage.xaml:34` locks via `IsEnabled` (disables whole control incl. X; no `ClearIconVisibility` set); `SongFormPage.xaml.cs` `OnArtistItemsRequested` re-appends the ➕ sentinel when `ArtistSearchText` is set to the locked name | XAML (`IsReadOnly` + `ClearIconVisibility="Auto"`) + page code-behind (suppress sentinel when locked / text==SelectedArtistName) |
| c (error text) | ❌ | **BUG-057** | only sink is the DX editor's own `ErrorText` (`xaml:32-33`); no visible `Label` bound to `ArtistErrorText`; on-device the inline error text doesn't surface | XAML (add error Label) |
| e (stale search + first-empty) | ❌ | **BUG-056** | `SearchArtistsAsync` (`SongFormViewModel.cs:289-291`) assigns `ArtistSuggestions` inside a fire-and-forget `RunOnUiThread`; provider `RequestAsync` (`SongFormPage.xaml.cs:57`) reads `ArtistSuggestions` **before** the dispatch lands → first read sees `[]`, stale read sees prior list. Interlocked guard (`:286-288`) is irrelevant to this | page↔VM coupling (return results directly / await UI dispatch) |
| j (edit empty) | ❌ | **BUG-055** | `LoadSongForEditAsync` (`SongFormViewModel.cs:400-419`) never sets `SelectedArtistId`/`SelectedArtistName`/`ArtistSearchText`; `InitializeArtistField` depends on `artistId` QueryProperty (=0 in normal edit). Also `UpdateSongAsync` (`:536-540`) never carries ArtistId. **Hydration bug, not save bug** (new-song save persists ArtistId correctly) | VM load path (+ edit-save ArtistId) |
| i (catalog no-op) | ❌ | **BUG-059** | handler/nav wiring correct (`ViewCatalogCommand`→`NavigateToCatalog`→`Songs?artistId=…`→`GetPagedCatalogForArtistAsync`); catalog renders empty because songs aren't linked to ArtistId — **cascade of BUG-054/055** | none in handler; resolve via 054/055 + DB spot-check |
| C1 (novel create) | ✅ | — | created+locked+saved | — |
| C2 (duplicate) | ❌ (partial) | **BUG-058** | `AutoCompleteEdit` has no `DisplayMember` (`xaml:28`) → DX writes selected object's record `ToString()` into `Text` (two-way→`ArtistSearchText`); success overwrites via `LockArtist`, failure retains → `, IsCreateNew = True }` leaks | XAML (`DisplayMember="Headline"`) + optional VM failure normalize |

### Coupling / single-writer sequencing (same worktree, strictly sequential)
- `SongFormPage.xaml.cs` — BUG-054a & BUG-056 both in `OnArtistItemsRequested`; BUG-054 & BUG-058 both route `OnArtistSelectionChanged`.
- `SongFormPage.xaml` — BUG-054b (`:34`), BUG-057 (`:32-33` + new label), BUG-058 (`DisplayMember` on `:28`) all touch the same `AutoCompleteEdit`.
- `SongFormViewModel.cs` — BUG-055 (`LoadSongForEditAsync`), BUG-056 (`SearchArtistsAsync`), BUG-058 (`CreateArtistInlineAsync`) — separate methods, one file.

**Suggested fix order:** BUG-056 (search race) first (unblocks realistic retest of 054/058) → BUG-055 (hydration + edit-save ArtistId) → XAML cluster BUG-054b/057/058 in one pass → BUG-054a code-behind → BUG-059 verifies as cascade. Each testable defect needs a regression check where a seam exists; the XAML/DX-wiring ones are on-device manual E2E (no unit seam), same as BUG-053. **Consequence:** BUG-027 stays open; the Artists & Songs Catalog stays 🔴 Blocked until T10 re-runs all-green.

---
## T10 re-run #4 (2026-07-30) — Helder, on device — outcome: **FAILED**
**Build under test:** worktree `C:\Users\helde\source\repos\myvocalist-inline-ac`, branch `feat/inline-artist-create`, HEAD `b8f7d2c`. Compiled and run on Helder's Android device.
**Reported by Helder verbatim (2026-07-30); transcribed here without interpretation, with root-cause hypotheses marked as hypotheses.**

### What passed
- **BUG-061 core behavior ✅** — tapping a suggestion loads the tapped artist into the Artist entry, hides the suggestion rows, locks the entry; clearing the entry re-enables redefinition; tapping a new suggestion correctly overrides the previously filled artist.
- **BUG-060 (change-artist / unlock)** remains ✅ as of re-run #3 — not contradicted by this run.
- **BUG-064** as originally scoped (duplicate error label) — not reported as recurring; the messages observed in this run are single, not duplicated. Treat BUG-064 as holding unless the fix wave finds otherwise.

### What failed — three new defects

| # | Mode | Observed |
|---|------|----------|
| 1.1.1 | Add | After selecting a suggestion, a result row reading **"Not found"** appears immediately; it disappears only when tapping outside the Artist entry |
| 1.1.2 | Add | After defining an artist, clearing the entry and typing **1 char** shows **"Not found"** again |
| 1.1.3 | Add | Typing the **2nd char** finally renders the matching options |
| 1.1.4 | Add | Typing an artist that does not exist yet shows the error **"search and select an artist from the list"** — a new artist cannot be created in this context |
| 1.2.1 | Edit | On page load, a **"Not found"** row appears in the autocomplete result list |
| 1.2.2 | Edit | After clearing the persisted artist, typing **1 char** that does match an existing artist shows **"Not found"**; **2 chars** retrieves correctly |
| 1.2.3 | Edit | Clearing the original artist and typing a new artist that does not exist yet shows **"search and select an artist from the list"** — a new artist cannot be created in this context |
| 1.2.4 | Edit | Clearing the artist and selecting a **different existing** artist shows the **"Not found"** row; it disappears on blur |
| 1.2.4.1 | Edit | **Saving after changing the artist to another existing artist does not persist the change — the song keeps the original artist** |

### Defect registration

- **BUG-065 (Major, NEW) — spurious "Not found" row in the Artist autocomplete dropdown.**
  Covers 1.1.1, 1.1.2, 1.2.1, 1.2.2, 1.2.4. Two symptom clusters that may or may not share a root cause:
  (a) *after a programmatic text assignment* (selection, edit-page hydration, re-selection) the dropdown opens showing a "Not found" row instead of staying closed — i.e. the BUG-061 `_suppressNextArtistSearch` guard suppresses the **search** but not the **dropdown opening / empty-result rendering**; the row clears only on blur.
  (b) *at 1 typed character* the list renders "Not found" even when matches exist, and only resolves at 2 characters — **hypothesis (unverified):** a minimum-prefix/minimum-search-length threshold (DX `AutoCompleteEdit` or the VM search guard) returns no results below 2 chars, and the empty result is rendered as a "Not found" row rather than suppressed.
  **Relationship:** this is the same class as BUG-061 (which is otherwise fixed) — do NOT close BUG-061; register BUG-065 as its residual and re-verify both together.
  Regression seam: (b) is testable at the VM/search seam; (a) is DX/XAML wiring → on-device manual E2E, documented here per `bug-tracking.md`.

- **BUG-066 (Major, NEW) — inline "create new artist" is unreachable; a non-existent artist name is rejected.**
  Covers 1.1.4 and 1.2.3 (both add and edit mode). Typing a name with no existing match produces the validation error *"search and select an artist from the list"*, so no new artist can be created from the Song form. **This is the headline capability of this whole change** (REQ-ACREATE-01/02/04 — the ➕ create row) and it is currently not available to the user, in either mode. C1 ("novel create") passed in re-run #2, so this is a **regression introduced by one of the later fix waves** — the prime suspects are the BUG-054a sentinel-suppression work and the `_suppressNextArtistSearch` guard (BUG-061), either of which can prevent `OnArtistItemsRequested` from appending the ➕ sentinel row. Verify against those commits before designing a fix.
  Severity rationale: core feature unusable, no workaround (the user must leave the Song form and create the artist elsewhere) → Major per `bug-tracking.md`.

- **BUG-067 (Critical, NEW) — editing a song's artist does not persist; the original artist is kept.**
  Covers 1.2.4.1. The user clears the artist, selects a different existing artist, saves — and the song still shows/stores the original artist. The edit is silently discarded, so this is **data-correctness with silent loss of a user edit → Critical** (`bug-tracking.md`), and per the same rule a **failing regression test is MANDATORY before the fix** (Red→Green); the seam exists at the ViewModel/service level (`UpdateSongAsync` + the artist-selection state), so there is no "UI-only" exemption here.
  **Likely relation:** BUG-055's second half (`UpdateSongAsync` never carrying `ArtistId`) — that path was supposedly addressed; confirm whether it regressed, was only partially fixed (e.g. handles hydration but not a *changed* selection), or whether the clear→re-select flow leaves `SelectedArtistId` stale. Do not assume a cascade — trace it.

### Consequences
- **T10 re-run #4 = FAILED.** Closeout stays blocked; `feat/inline-artist-create` stays unmerged at `b8f7d2c`; BUG-027 stays open; **Artists & Songs Catalog** stays 🔴 Blocked.
- A fix wave is required in the SAME worktree (`myvocalist-inline-ac`), strictly sequential — the three defects again converge on `SongFormPage.xaml(.cs)` and `SongFormViewModel.cs`.
- **Suggested order:** BUG-067 (Critical, regression test first) → BUG-066 (restores the feature's headline capability; likely a one-condition regression in the sentinel/guard logic) → BUG-065 (b) then (a). BUG-065 and BUG-066 may share the `OnArtistItemsRequested` code path — trace both before editing, and re-verify BUG-061/BUG-064 in the same on-device pass so the guards are not re-broken.
- **Process observation (for the Continuous Enhancement review, not a defect):** this is the fourth on-device re-run in which VM-level unit tests were green while the DX `AutoCompleteEdit` wiring failed, and the second in which a fix wave regressed previously-passing behavior (C1 → BUG-066). The unit suite does not cover this seam at all. Before the next wave, consider whether an instrumented/automated device check for this page is cheaper than a fifth manual round-trip.

### BUG-065 root-cause trace (2026-07-30, read-only, no edits) — one structural defect, not three
Dispatched after Helder chose BUG-065 as the starting point (overriding the recommended Critical-first order). Read-only trace against the worktree at `b8f7d2c`. **No fix was written — the trace ended with two questions that static reading cannot answer.**

**VERIFIED (from the code, not inferred):**
- **The "Not found" text is NOT ours.** It appears nowhere in `MyVocaList/UI/**` (`.cs`/`.xaml`/`.resx`); the only "not found" strings in the solution are unrelated service-layer messages. It is a DevExpress `AutoCompleteEdit` built-in placeholder. **Consequence: four fix waves have been editing the wrong layer for this symptom.**
- **The `1`-char threshold is ours, in XAML:** `CharacterCountThreshold="1"` on the `AsyncItemsSourceProvider` (`SongFormPage.xaml:42`), alongside `RequestDelay="300"`. There is no minimum-length check in `SearchArtistsCoreAsync` — it only guards `IsNullOrWhiteSpace` (`SongFormViewModel.cs:301`), which a 1-char string never trips.
- **What `_suppressNextArtistSearch` actually does.** The guard is consumed in `OnArtistItemsRequested` (`SongFormPage.xaml.cs:45-81`), which then completes the request with an EMPTY array (`:51-55`). It suppresses the *search*; it does **not** stop the control from treating the request as completed. A completed-but-empty request is indistinguishable, on the control's public surface, from "searched and found nothing" — which is what renders the placeholder row. Nothing in our code closes the dropdown; `SelectedItem = null` (`:38`, `:104`) resets selection, not visibility. That is why it clears only on blur.
- **The guard's early `return` (`SongFormPage.xaml.cs:54`) exits BEFORE the ➕-sentinel-append block (`:57-80`).** This is a structural fact, verified by reading, not a timing hypothesis — whenever the guard fires, the create-new-artist row cannot be produced. **This is the most likely root cause of BUG-066**, and it lives in the same method, exactly as suspected.

**The likely single root cause:** `_suppressNextArtistSearch` is keyed to "the next `ItemsRequested` event", not "the event caused by this specific assignment". With `RequestDelay="300"` debouncing, a programmatic set followed by the user typing inside that window can let the *user's* keystroke consume a flag set for the *programmatic* change — returning `[]` and skipping the ➕ append. One defect plausibly explains BUG-065(a), BUG-065(b) and BUG-066.

**UNRESOLVED — must not be guessed at:**
1. `CharacterCountThreshold="1"` — does a request fire at 1 char (`>=`) or only at 2 (`>`)? If it is `>`, then BUG-065(b) is not our bug at all: at 1 char no request ever fires and the placeholder is simply the control's un-populated state.
2. Whether an empty `RequestAsync` result is what opens the popup, and whether any public API exists to close/suppress it or to leave the request un-completed instead.

Both were **blocked on documentation**: Context7 has no `NoResultsFoundText`-equivalent for `DevExpress.Maui.Editors.AutoCompleteEdit` (only for the DataForm/DataGrid sibling classes, which are NOT the control in use), and the DevExpress demo-app MCP returned empty for every query including a bare `AutoCompleteEdit` — treated as **server unavailable**, not an empty result, per `CLAUDE.md § MCP Availability Gate`. **Escalated to decompiling the shipped `DevExpress.Maui.Editors.dll` (ILSpy) to get the comparison operator and the popup behavior from IL.** Guessing an API name here would produce exactly the failure mode this feature has already hit four times.

**Testability — stated honestly.** Unit-testable at the VM: the 7 guard-site assignments, `ConsumeSuppressArtistSearch()` one-shot semantics, `SearchArtistsCoreAsync` mapping, and the `ClearArtist`/`LockArtist`/blur state transitions (all already covered). **NOT unit-testable — on-device only:** whether the control opens the popup on an empty result, the `CharacterCountThreshold` boundary, the `RequestDelay` debounce/text-identity behavior, and the placeholder rendering itself. A green VM suite is NOT evidence this class of bug is fixed — that assumption is what produced re-runs #1–#4.

**Regression exposure for any fix here — all 7 programmatic guard sites must be re-verified, not just the two in the repro steps:** `LockArtist` (`:333`), `InitializeArtistField` (`:422`), `LoadSongForEditAsync` (`:490`), `OnArtistBlurredWithoutSelection` restore (`:385`), `ClearArtist` (`:404`), `ResolveAndLockArtistAsync` ×2 (`:539`, `:551`). This exact class of gap has already regressed twice. BUG-064 exposure: `ArtistHasError`/`ArtistErrorText` must remain the SOLE error surface (`SongFormPage.xaml:33-34`); any new error surface reintroduces the duplicate message.

**Architectural decision surfaced for Helder (not for an agent to take):** the control appears to expose a native `TextChanged` `Reason` (`UserInput`/`ProgrammaticChange`/`ItemSelected`) that is not wired anywhere in this codebase and would replace the hand-rolled `_suppressNextArtistSearch` boolean at all 7 sites. That is a design change to a mechanism BUG-061 depends on — Helder decides, per `CLAUDE.md § Roles`.

### IL evidence (2026-07-30) — the `_suppressNextArtistSearch` guard is guarding an event that never fires
Decompiled `DevExpress.Maui.Editors.dll` + `DevExpress.Maui.Core.dll` v25.2.4 (net10.0-android35.0 — matches the pinned `Directory.Packages.props`, no version mismatch) after both documentation routes failed. Read-only; no repo file touched. **This resolves the two open questions and overturns the working assumption behind the BUG-061 fix.**

**THE FINDING — `ItemsRequested` never fires for a programmatic text change.**
`AsyncItemsSourceProvider.OnEditorTextChanged` opens with:
```csharp
if (e.Reason != AutoCompleteEditTextChangeReason.UserInput) return;
```
An early return, before any threshold or request logic. `AutoCompleteEditTextChangeReason` = `UserInput = 0`, `ProgrammaticChange = 1`, `ItemSelected = 2`. **The control already suppresses programmatic changes natively.**

**Consequence — the guard is not merely redundant, it is the defect.** `_suppressNextArtistSearch` is set at 7 programmatic sites and consumed in `OnArtistItemsRequested`. Since no `ItemsRequested` fires for those assignments, **the flag is never consumed by the event it was set for — it stays set and is eaten by the user's NEXT GENUINE KEYSTROKE**, which is then early-returned with an empty array (`SongFormPage.xaml.cs:51-55`), skipping the ➕-append block (`:57-80`).

That mechanism explains two of the three reported defects exactly, with no timing hypothesis required:
- **BUG-065(b)** — `ClearArtist` (`:404`) sets the flag; the user's **1st** keystroke consumes it → empty result → "Not found". The **2nd** keystroke finds no flag → real matches render. This is precisely the reported 1-char/2-char behavior.
- **BUG-066** — the same stale-flag consumption early-returns before the ➕ row can be appended, so inline create is unreachable and blur then raises "search and select an artist from the list" (`SongFormViewModel.cs:369-389`) because `SelectedArtistId` was never set.

**Question 1 — `CharacterCountThreshold`: RESOLVED, and it is NOT the cause.** `OnEditorTextChanged` compares `Text.Length >= CharacterCountThreshold` (default `1`). With our `1`, a request fires at 1 character as intended. The threshold is correctly configured; BUG-065(b) is the stale-flag bug above.

**Question 2 — the placeholder and the dropdown: RESOLVED.**
- The literal text is DevExpress's: `EditorLocalizer` registers `EditorStringId.ComboBox_NotFound` → `"Not found"`. There is **no bindable property** on `AutoCompleteEdit`/`AsyncItemsSourceProvider` to change or suppress it — only the shared localizer string.
- **`IsDropDownOpen` (on `ItemsEditBase`, `BindableProperty`, two-way) is a public, provider-endorsed way to force the popup shut** — `AsyncItemsSourceProvider` sets it itself in `OnEditorSubmitted` and on empty text. This is the supported lever for BUG-065(a).
- **Do NOT leave `RequestAsync`/`Request` unassigned** — `RaiseCreateAsyncItemsSourceRequest` falls through and invokes the null `Request()` delegate inside a background `Task`, awaited unguarded in an `async void`: it **crashes**, it does not no-op.
- **Do NOT try to self-cancel** — `ItemsRequestEventArgs.CancellationToken` is get-only with no `Cancel()`; the token is owned by the provider.
- Returning `null` instead of an empty array is treated identically by the C# layer (`ItemsSource` is assigned unguarded).

**Still UNRESOLVED (honest limit):** the exact native trigger that opens the popup and renders the no-results row lives in the Android-native widget (`DevExpress.Android.Editors`), not in decompilable IL — the MAUI handler only tints a native `NoResultsFoundTextTint`. So **BUG-065(a)**'s precise mechanism is inferred, not proven; `IsDropDownOpen = false` is the evidenced remedy, but it must be confirmed on-device.

**Implication for BUG-061 — this needs Helder's decision, not an agent's.** BUG-061 was fixed by *adding* this guard. The IL says the guard cannot have been suppressing programmatic searches (they never reached the handler), so whatever BUG-061's real cause was, the flag was not the cure — and it introduced BUG-065(b)/BUG-066. The coherent fix is to **delete the mechanism at all 7 sites and close the dropdown via `IsDropDownOpen = false` instead**, but that removes code BUG-061's regression tests assert on (`SongFormViewModelTests.cs:476-535`) and per `CLAUDE.md § Roles` an agent does not take that call. **Recorded as an open decision for Helder 2026-07-30.**

### ID-allocation note (blocks `backlog_gen.py register` for these three)
`backlog_gen.py register` was attempted for BUG-067 and refused: *"expected id BUG-067 but the tree says BUG-053"*. `next_bug_id()` derives the next id from **item folders + archive files only**, and BUG-053…BUG-064 were never given folders — they exist only in this task-log and in `LEDGER.md`. The generator would therefore hand out already-used ids. These three are consequently tracked here (matching the BUG-053…064 precedent) and **not** registered as folders. Logged as a follow-up in `spec-evolution-versioning/POST-MIGRATION-FOLLOWUPS.md`.

---

## 🔷 DECISION TAKEN — Helder, 2026-08-02 (unblocks the fix wave)

The open architectural decision recorded in `handoff.md § STOP POINT` is now settled:

**D1 — guard disposition: Option 1 (recommended) ACCEPTED.**
Delete `_suppressNextArtistSearch` at **all 7 sites** and dismiss the dropdown via
`IsDropDownOpen = false` instead (the supported `ItemsEditBase` two-way `BindableProperty`;
DevExpress's own provider uses it). Rationale: the IL evidence (§ IL evidence 2026-07-30) proves
`AsyncItemsSourceProvider.OnEditorTextChanged` early-returns unless
`e.Reason == UserInput`, so the guard is never consumed by the programmatic assignment that set
it — it leaks into the user's next keystroke. One coherent change covers BUG-065(a), BUG-065(b)
and BUG-066.
Consequence accepted: **BUG-061's regression tests (`SongFormViewModelTests.cs:476-535`) are
rewritten against the real mechanism** — they currently assert on an inert flag. All 7 sites plus
BUG-064 are re-verified in the same on-device pass.

The 7 sites (handled together — this class of gap has already regressed twice):
`LockArtist` :333 · `InitializeArtistField` :422 · `LoadSongForEditAsync` :490 ·
`OnArtistBlurredWithoutSelection` restore :385 · `ClearArtist` :404 ·
`ResolveAndLockArtistAsync` ×2 :539, :551.

**D2 — wave order: BUG-065+066 (one fix) → BUG-067 → single on-device T10 re-run #5.**
BUG-067 (Critical, still unanalysed) is fixed **regression-test-first (Red→Green)** per
`bug-tracking.md`. Only one device pass is requested of Helder, covering 065/066/067 plus
re-verification of BUG-061 and BUG-064.

Wave runs strictly sequential in the existing worktree
`C:\Users\helde\source\repos\MyVocaList-inline-ac` (clean at `b8f7d2c`) — all three defects
converge on `SongFormPage.xaml(.cs)` + `SongFormViewModel.cs`.

---
## Task: Fix wave step 1 — BUG-065 + BUG-066 (D1, Option 1)
**Plan:** Helder's decision D1 (`§ DECISION TAKEN — Helder, 2026-08-02` above)
**Status:** To Review
**Started:** 2026-08-02
**Completed:** 2026-08-02

### Approach
Implemented D1 exactly as decided (no redesign): deleted `_suppressNextArtistSearch` at all 7
programmatic-assignment sites in `SongFormViewModel.cs` and replaced the mechanism with a new
two-way bound `IsArtistDropDownOpen` `[ObservableProperty]`, bound in `SongFormPage.xaml` to
`dxe:AutoCompleteEdit.IsDropDownOpen` (`ItemsEditBase`, `BindableProperty`, two-way — the same
lever DevExpress's own `AsyncItemsSourceProvider` uses internally, per the IL evidence). Every site
that previously set the flag now sets `IsArtistDropDownOpen = false` immediately alongside the
programmatic `ArtistSearchText` assignment, forcing the popup shut directly instead of relying on a
flag that (per IL) was never consumed by the event it was set for.

`SongFormPage.xaml.cs`'s `OnArtistItemsRequested` no longer has a guard-consumption branch — since
that event never fires for a programmatic text change in the first place (IL-verified), removing the
guard restores normal search behavior for every REAL user keystroke, including the character
immediately following a lock/clear/hydration. This is what unblocks BUG-066 (create-row append is no
longer skipped by a stale-flag early return) and BUG-065(b) (the 1st keystroke after a programmatic
set is no longer misrouted to an empty-result branch).

### 7 guard sites confirmed handled (all in `SongFormViewModel.cs`, current line numbers)
`LockArtist` (`:329-339`) · `OnArtistBlurredWithoutSelection` restore branch (`:369-389`) ·
`ClearArtist` (`:399-410`) · `InitializeArtistField` (`:416-426`) · `LoadSongForEditAsync`
(`:452-501`) · `ResolveAndLockArtistAsync` ×2 (`:523-556`) — verified via a full-file grep for
`IsArtistDropDownOpen` (7 matches, one per site) and for `_suppressNextArtistSearch`/
`ConsumeSuppressArtistSearch` (0 remaining code references — only explanatory comments referencing
the deleted mechanism by name).

### BUG-061 regression tests rewritten (per D1's accepted consequence)
All 7 tests previously asserting `ConsumeSuppressArtistSearch()` were rewritten in place (same test
count, not added/removed) to assert `IsArtistDropDownOpen == false` on the real mechanism instead:
- `SelectArtist_ExistingSuggestion_SuppressesNextArtistSearch` → `SelectArtist_ExistingSuggestion_ClosesDropdown`
- `InitializeArtistField_WithArtistId_SuppressesNextArtistSearch` → `InitializeArtistField_WithArtistId_ClosesDropdown`
- `ConsumeSuppressArtistSearch_NoProgrammaticSet_ReturnsFalse` → `IsArtistDropDownOpen_NoProgrammaticSet_DefaultsFalse`
- `ResolveAndLockArtistAsync_ExactMatch_SuppressesNextArtistSearch` → `ResolveAndLockArtistAsync_ExactMatch_ClosesDropdown`
- `ResolveAndLockArtistAsync_NoMatch_SuppressesNextArtistSearch` → `ResolveAndLockArtistAsync_NoMatch_ClosesDropdown`
- `ArtistBlurredWithoutSelection_RestoresPriorSelection_SuppressesNextArtistSearch` → `ArtistBlurredWithoutSelection_RestoresPriorSelection_ClosesDropdown`
- `ClearArtist_SuppressesNextArtistSearch` → `ClearArtist_ClosesDropdown`

Each rewritten test still encodes BUG-061's original behaviour (a programmatic text assignment must
close/not leave open the suggestion dropdown) via the property that now actually carries it, per the
briefing's instruction — coverage was not weakened, only re-pointed at the real seam. No other test
was touched.

### BUG-066 coverage — spec gap, not a silent gap
The synthetic ➕-row append itself (`OnArtistItemsRequested` in `SongFormPage.xaml.cs`) lives in page
code-behind (`ItemsRequestEventArgs`/MAUI types) with no page-level test harness in this test project
— same conclusion the prior CODE-REVIEW fold-in reached for M5. No new unit-testable seam was created
by this fix; the regression coverage for BUG-066 is indirect (all 7 `IsArtistDropDownOpen` tests above
prove the stale-flag mechanism that caused the early-return is gone) plus the removal itself (verified
by the zero-remaining-reference grep). **The actual "create row reachable again" behaviour is NOT
unit-testable and requires the on-device T10 re-run #5.**

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-12 (BUG-050/065) | Selecting a suggestion closes the dropdown | `SongFormViewModel.LockArtist` | `SelectArtist_ExistingSuggestion_ClosesDropdown` |
| REQ-ACREATE-14 (BUG-052/065) | Edit-mode hydration closes the dropdown | `SongFormViewModel.InitializeArtistField` | `InitializeArtistField_WithArtistId_ClosesDropdown` |
| REQ-ACREATE-01 (BUG-065) | Fresh VM / song-picker no-match prefill: dropdown state correct | `SongFormViewModel` ctor default; `ResolveAndLockArtistAsync` no-match branch | `IsArtistDropDownOpen_NoProgrammaticSet_DefaultsFalse`; `ResolveAndLockArtistAsync_NoMatch_ClosesDropdown` |
| REQ-ACREATE-04 (BUG-065) | Song-picker exact-match auto-lock closes the dropdown | `SongFormViewModel.ResolveAndLockArtistAsync` exact-match branch | `ResolveAndLockArtistAsync_ExactMatch_ClosesDropdown` |
| REQ-ACREATE-03 (BUG-065) | Blur-restore of prior selection closes the dropdown | `SongFormViewModel.OnArtistBlurredWithoutSelection` restore branch | `ArtistBlurredWithoutSelection_RestoresPriorSelection_ClosesDropdown` |
| REQ-ACREATE-15 (BUG-060/065) | Clear (X) icon closes the dropdown | `SongFormViewModel.ClearArtist` | `ClearArtist_ClosesDropdown` |
| REQ-ACREATE-02/10 (BUG-066) | ➕ create-new-artist row reachable again after any keystroke, including the first after a programmatic set | `SongFormPage.OnArtistItemsRequested` (guard removed) | **on-device only — T10 re-run #5** |
| REQ-ACREATE-04/05 (BUG-066) | Inline create end-to-end (novel name → ➕ → created + locked) | `SongFormViewModel.CreateArtistInlineAsync` (unchanged) | pre-existing `CreateArtistInline_*` tests (still green) + **on-device T10 re-run #5** |

### What could NOT be verified without a device (stated per the briefing)
- **BUG-065(a)'s exact remedy** — whether setting `IsDropDownOpen = false` actually closes the native
  Android popup in this app's rendering. IL evidence proves the property is the provider-endorsed
  lever but the popup-open/close native behaviour itself lives outside decompilable IL (Android-native
  widget). Needs on-device confirmation.
- **BUG-065(b)'s 1-char/2-char fix** — that the first real keystroke after a programmatic set now
  searches and renders matches immediately, instead of hitting the (now-removed) empty-result branch.
  Unit tests prove the flag/branch is gone; they cannot observe the DX popup rendering.
- **BUG-066's headline behaviour** — that the ➕ "Add as new artist" row is selectable again for a
  novel name in both Add and Edit mode, end to end (create → lock → save).
- **BUG-061/BUG-064 re-verification** — Helder's instruction was to re-verify both in the same
  on-device pass; neither was touched by this fix, but no regression check beyond the unit suite is
  possible here.
- **BUG-067** — explicitly out of scope for this dispatch (step 2, separate dispatch per the briefing).

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` (worktree `MyVocaList-inline-ac`) → `ok dotnet build: 6 projects, 0 errors, 17 warnings` (all pre-existing NU1903, unrelated to this change).
- **Full suite:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → `Aprovado! – Com falha: 0, Aprovado: 530, Ignorado: 0, Total: 530` (unchanged count from baseline — 7 tests rewritten in place, no net add/remove).
- **Files written and re-read** (post-edit verification, all 4 re-read after edit to confirm correct placement): `SongFormViewModel.cs`, `SongFormPage.xaml.cs`, `SongFormPage.xaml`, `SongFormViewModelTests.cs`.

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (worktree) — removed `_suppressNextArtistSearch` field, doc comment, and `ConsumeSuppressArtistSearch()` method; added `IsArtistDropDownOpen` `[ObservableProperty]`; replaced all 7 `_suppressNextArtistSearch = true;` sites with `IsArtistDropDownOpen = false;` alongside each programmatic `ArtistSearchText` assignment.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (worktree) — removed the `ConsumeSuppressArtistSearch()`-gated early-return branch from `OnArtistItemsRequested`; updated the surrounding comment to explain the IL-proven mechanism.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (worktree) — added `IsDropDownOpen="{Binding IsArtistDropDownOpen, Mode=TwoWay}"` to `dxe:AutoCompleteEdit`.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (worktree) — rewrote the 7 BUG-061 regression tests in place against `IsArtistDropDownOpen` (see list above); no other test touched.
- Commit: `befc2fe` on `feat/inline-artist-create`, pushed to `origin/feat/inline-artist-create`.

### Checkpoint (final — task complete)
Branch/worktree: `MyVocaList-inline-ac`, `feat/inline-artist-create`, HEAD `befc2fe` (pushed). Build:
0 errors. Tests: 530/530 green. Next step: BUG-067 (step 2, separate dispatch per the briefing), then
a single on-device T10 re-run #5 covering 065/066/067 + re-verification of BUG-061/BUG-064.

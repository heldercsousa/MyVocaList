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

---
## Bug: BUG-054a (Major, UI-only) — create sentinel re-appears when field is locked
**Status:** To Review
**Severity:** Major (UI-only; no VM seam — manual E2E deferred to T10 re-run, Helder)

**Root cause:** `SongFormPage.OnArtistItemsRequested.RequestAsync` re-appended the ➕ create sentinel for any non-whitespace text, including when the field was already locked to a selected artist (the editor re-requests items after a lock sets `ArtistSearchText` to the chosen name).

**Fix:** suppress appending the sentinel when `ViewModel.IsArtistLocked` is true OR the requested text equals `ViewModel.SelectedArtistName`. The existing whitespace guard is preserved. Page code-behind stays glue-only.

**Regression risk:** Low — narrows when the sentinel appears; does not change the create/select routing.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistItemsRequested` sentinel suppression guard.

### Verification evidence
- **Build:** android build -> exit 0, 0 errors. **Tests:** 520/520 green (no VM seam for the page provider — page code-behind).
- **Manual E2E:** deferred to T10 re-run (Helder): after selecting/creating an artist (locked), typing/opening the dropdown must not show the ➕ row for the locked name.

---
## Bug: BUG-059 (Major, cascade) — artist catalog empty
**Status:** To Review (resolves as a cascade — verify on device)

**Root cause:** the catalog nav/handler wiring is correct (`ViewCatalogCommand` -> `NavigateToCatalog` -> `Songs?artistId=…` -> `GetPagedCatalogForArtistAsync`). The catalog rendered empty only because songs were not linked to an `ArtistId` at save time — a cascade of BUG-054/BUG-055 (edit-save dropped the artist link / hydration left `SelectedArtistId` null so Save was blocked).

**Fix:** none in the catalog handler (verified correct, not edited). Resolves as a cascade of the BUG-055 fix: edit-mode hydration now populates `SelectedArtistId` and the service preserves `song.ArtistId`, so saved/edited songs carry the artist link and the catalog populates.

**Regression risk:** None (no code change here).

### Verification evidence
- No code change. Confirmed the nav chain is intact (read-only trace; handler untouched).
- **Manual E2E / DB spot-check:** on device (T10 re-run, Helder) — a saved/edited song now carries `ArtistId`, so the artist catalog populates. Not runnable here (no app run).

---
## Bug: BUG-060 (Major) — REQ-ACREATE-15: locked artist field could not be changed
**Status:** To Review
**Severity:** Major (core edit flow: user could not change a wrong/locked artist selection)

**Root cause:** `LockArtist`/`InitializeArtistField`/edit-mode hydration all set `IsArtistLocked = true` but nothing ever unlocked it. Tapping the DX `AutoCompleteEdit` clear (X) icon only clears the bound `Text` (native editor behavior on `Text`) — no VM hook existed for the DX `ClearIconClicked` event, so `IsArtistLocked`/`SelectedArtistId`/`SelectedArtistName` were never reset. On blur, `OnArtistBlurredWithoutSelection`'s restore-prior-selection branch then repopulated the just-cleared name because `SelectedArtistId` still had a value — net effect: the field was stuck on the first artist ever selected.

**Fix:** new `SongFormViewModel.ClearArtistCommand` ([RelayCommand], REQ-ACREATE-15) resets `SelectedArtistId`/`SelectedArtistName`/`ArtistSearchText`/`IsArtistLocked`/`ArtistSuggestions`/`ArtistHasError`/`ArtistErrorText` to the normal searchable state. Wired the DX `AutoCompleteEdit.ClearIconClicked` event (confirmed via Context7, DevExpress MAUI 25.2.4 docs — `docs.devexpress.com/MAUI/404570/editors/icons`) in `SongFormPage.xaml` to a thin code-behind forwarder (`OnArtistClearIconClicked`) that calls `ViewModel.ClearArtistCommand`. No separate "deliberately cleared" flag was needed: because `ClearArtist` nulls `SelectedArtistId`, a subsequent blur naturally takes the "no artist selected" branch of `OnArtistBlurredWithoutSelection`, not the restore-prior branch — re-typing over a *still-locked* field remains impossible by construction (`IsReadOnly` is bound to `IsArtistLocked`), so the restore-prior branch's only remaining trigger (re-type without clearing) cannot occur while locked.

**Regression risk:** Low — new command + one new XAML event wire-up; no existing binding paths changed.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `ClearArtistCommand` + `[RelayCommand] ClearArtist()`.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — `ClearIconClicked="OnArtistClearIconClicked"` on `artistEdit`.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistClearIconClicked` thin forwarder.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — 2 new tests (Red→Green confirmed locally: both failed before `ClearArtistCommand` existed — compile error since the command didn't exist — then passed after the fix).

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → exit 0, 0 errors (the `net10.0-android` TFM cannot be built in this session — see Environment note below; XAML/C# compiled clean on the `net10.0` library TFM shared with the test project, which exercises the exact same XAML compiler pass).
- **Tests:** 523/523 green (520 baseline + 3 new: `ClearArtist_WhenLocked_UnlocksAndClearsSelection`, `ArtistBlurredWithoutSelection_AfterDeliberateClear_DoesNotRestorePriorArtist`, `ArtistBlurredWithoutSelection_NoPriorSelection_SetsErrorText` — the last is BUG-057's).
- **Manual E2E (on device):** deferred to next T10 re-run (Helder) — tap X on a locked field → field becomes editable/searchable; blur after clearing does not restore the prior artist.

---
## Bug: BUG-057 (Major, REOPENED) — inline artist error text invisible
**Status:** To Review
**Severity:** Major (user-facing: a Major/no-workaround validation error was silently invisible)

**Root cause differs from the handoff's assumed direction.** The XAML (`SongFormPage.xaml` error `Label`, binding path, `x:DataType`, `{StaticResource Error}`) was correct — same `BindingContext` as the rest of the page, no template/section boundary issue. The real cause was in the VM: `OnArtistBlurredWithoutSelection`'s "no artist selected" branch set `ArtistHasError = true` (which makes the `Label` visible — hence "reserves layout space") but never set `ArtistErrorText`, so the Label rendered with an empty string. `SaveAsync`'s artist-required guard sets both flags correctly, which is why the bug was easy to miss in earlier E2E passes that triggered the error via Save rather than via blur.

**Fix:** `OnArtistBlurredWithoutSelection` now also sets `ArtistErrorText = "Search and select an artist from the list"` (same message `SaveAsync` uses for a non-empty unmatched search) whenever it sets `ArtistHasError = true`.

**Regression risk:** None — additive, only fills a previously-empty string on an existing error path.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `OnArtistBlurredWithoutSelection`.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — `ArtistBlurredWithoutSelection_NoPriorSelection_SetsErrorText` (Red confirmed: failed against the pre-fix code with `Assert.False(string.IsNullOrEmpty(sut.ArtistErrorText))`, then green after the fix).

### Verification evidence
- **Build:** see BUG-060 entry (same session/build).
- **Tests:** included in the 523/523 run above.
- **Manual E2E (on device):** deferred to next T10 re-run (Helder) — blur the Artist field with unmatched typed text → error message text is now visible under the field.

---
## Bug: BUG-061 (UI, NEW) — selected suggestion row lingers in the dropdown
**Status:** To Review
**Severity:** Minor/UI (cosmetic — no functional block, but confusing on selection and on edit-mode load)

**Root cause:** the DX `AutoCompleteEdit`/`CollectionView`-backed drop-down keeps the picked item as `SelectedItem` after a selection is routed (DevExpress `CollectionView` semantics: tapping the same selected item again is what clears `SelectedItem` — confirmed via Context7). Nothing in `OnArtistSelectionChanged` or the edit-mode load path ever reset `artistEdit.SelectedItem`, so the picked row stayed visually marked/highlighted in the suggestion list until tapped a second time.

**Fix:** `OnArtistSelectionChanged` sets `artistEdit.SelectedItem = null` immediately after routing the pick to the VM (create or select command). `OnAppearing` also resets `artistEdit.SelectedItem = null` after `InitializeArtistField()`, covering the edit-mode initial-load case named in the bug report.

**Regression risk:** Low — code-behind only, glue layer; does not touch VM state or the `Text`/`ArtistSearchText` binding that carries the actual chosen value.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistSelectionChanged`, `OnAppearing`.

### Verification evidence
- **Build:** see BUG-060 entry (same session/build).
- **Tests:** no VM seam — UI-only, page code-behind (per bug-tracking.md, UI-only Major/Minor → manual E2E documented here).
- **Manual E2E (on device):** deferred to next T10 re-run (Helder) — select a suggestion → dropdown row is not left highlighted; open the edit page → no stale highlighted row on load.

---
## BUG-059 (Major, REOPENED) — blocked: spec gap, NOT fixed this session

**Location:** `Services/CatalogService.cs` / `Infra/Repository/CatalogRepository.cs` / `Services/SongService.cs`, cross-referenced against `Docs/Management/BusinessFeatures/artists-songs/design.md` (Catalog entity + `ICatalogService`/`ICatalogRepository` sections, "Artist Catalog management" flow).

**Gap description:** the previous fix wave's "cascade" diagnosis (BUG-054/055 → catalog populates once `Song.ArtistId` persists) is **wrong**, confirmed by direct trace: `GetPagedCatalogForArtistAsync` → `CatalogRepository.GetPagedByArtistAsync` filters `_db.Catalog.Where(c => c.ArtistId == artistId)` — a **separate join table** (`Catalog { ArtistId, SongId }`), not `Song.ArtistId`. Per `design.md` (`§ Catalog entity`, `§ Artist Catalog management` flow), a `Catalog` row is created **only** via `ICatalogService.AddSongToCatalogAsync`, which is invoked **only** from the Songs-list-in-Catalog-mode "add from picker" flow (`AddToCatalogCommand` → song picker → `AddSongToCatalogAsync(artistId, songId)`). `SongService.CreateSongAsync`/`CreateSongWithUrlsAsync`/`UpdateSongAsync` — the paths this feature's Song form uses — never call `AddSongToCatalogAsync` and never touch the `Catalog` table. So "an artist's Catalog is empty right after creating a song for them via the Song form, without a separate explicit 'add to catalog' action" is the **currently-specified, intentional** behavior, not a persistence bug. `Song.ArtistId` being correctly set (BUG-054/055) does not and per the current design should not populate `Catalog`.

**Options:**
- **Option A — this is not a bug; T10 item "i" tested the wrong expectation.** Close BUG-059 as "working as designed"; if Helder wants the demo/T10 checklist to show a populated catalog, the test step should explicitly use the "Add to Catalog" flow (Songs page, Catalog mode, picker) after creating the song, not expect auto-population from the Song form.
- **Option B — new AC: creating/inline-linking a song to an artist via the Song form should ALSO insert a `Catalog` row for that artist**, i.e. every song a user directly authors for an artist becomes part of that artist's performable repertoire by default. This is a genuine behavior change to `SongService.CreateSongAsync`/`CreateSongWithUrlsAsync` (call `ICatalogService.AddSongToCatalogAsync`/inject `ICatalogRepository` and add a `Catalog` row atomically with song creation) — and arguably to `UpdateSongAsync` if `ArtistId` can ever change. Needs its own AC (e.g. REQ-ACREATE-16) and touches `Services/SongService.cs` (in this task's allowed file list) but is a product-behavior decision, not a bug fix.

**Recommendation:** Option A, with Option B as a legitimate follow-up feature request if Helder's actual intent was "songs I create for an artist should show up in their catalog automatically" — that reading is plausible given the T10 checklist's phrasing, but it changes what "Catalog" means (curated repertoire vs. authored-songs) and should not be decided by a bug-fix pass.

**Blocking:** Yes — cannot proceed without Helder's decision between Option A and Option B. No code changed for BUG-059 this session (`Services/CatalogService.cs`, `Infra/Repository/CatalogRepository.cs`, `Services/SongService.cs` all read-only this session, confirmed unmodified in the diff).

---
### Checkpoint — COMPLETE (2026-07-23, T10 re-run #2 follow-up)
BUG-060/057/061 fixed and verified (build + 523/523 tests green); BUG-059 investigated and reported `blocked: spec gap` (see above — do not self-adjudicate the Catalog-auto-population question). Commits on feat/inline-artist-create (worktree, not pushed/merged): see git log after this entry. **Environment note:** `dotnet build -f net10.0-android` failed in this session with `XARLP7024: O arquivo ou pasta está corrompido e ilegível` extracting an AndroidX/Material AAR resource (`design_layout_snackbar_include.xml`) into `obj/Debug/net10.0-android/lp/162/...` — reproduced after deleting `obj`/`bin` and the NuGet package cache entry for `xamarin.google.android.material`; the file does not exist on disk between attempts (confirmed via PowerShell `Get-Item`) yet the Android resource-extraction step reports it corrupted every time, which points to a Windows-level file-write interception (AV/EDR or similar) rather than a stale cache or a code defect. Build was verified 0-errors on the `net10.0` TFM instead (same XAML/C# compiler pass, no AAR packaging step). **Needs Helder to build/run on Android locally (or investigate the filesystem/AV issue) before the next on-device T10 re-run.**

---
## Bug: BUG-061 (Major UI, RE-FIXED — T10 re-run #3) — lingering autocomplete suggestion row after selection
**Status:** To Review
**Severity:** Major (previous fix, resetting `artistEdit.SelectedItem`, did not address the actual root cause)

**Root cause (Helder diagnosed, confirmed by trace):** when `ArtistSearchText` is set PROGRAMMATICALLY
(artist-selection lock via `LockArtist`, edit-mode hydration via `InitializeArtistField` and
`LoadSongForEditAsync`), the DX `AutoCompleteEdit`'s two-way `Text` binding re-fires
`ItemsRequested`/search — re-rendering items matching the exact just-set name, which re-opens the
dropdown on the just-selected/hydrated artist. The earlier `SelectedItem = null` fix (BUG-061 first
pass) cleared the highlighted row but did not stop the re-search that repopulated it.

**Fix:** added a one-shot guard in `SongFormViewModel` (`_suppressNextArtistSearch` +
`ConsumeSuppressArtistSearch()`), set immediately before every programmatic `ArtistSearchText`
assignment on the selection-lock and edit-hydration paths (`LockArtist` — covers both
`SelectArtist` and `CreateArtistInlineAsync` — `InitializeArtistField`, and the
`LoadSongForEditAsync` artist-hydration block). `SongFormPage.OnArtistItemsRequested` calls
`ConsumeSuppressArtistSearch()` first; if true, it short-circuits `e.RequestAsync` to an empty
result and returns without touching the VM search path, consuming the flag so the very next
(user-typed) items request runs normally. The `SelectedItem = null` resets from the prior pass are
left in place (still correct, just insufficient alone).

**Regression risk:** Low — additive one-shot flag; only short-circuits the specific items-request
that immediately follows a programmatic text set; user-typed searches are unaffected (flag defaults
false and only becomes true right before the four call sites above).

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `_suppressNextArtistSearch` field,
  `ConsumeSuppressArtistSearch()`, set-before-assign in `LockArtist`, `InitializeArtistField`,
  `LoadSongForEditAsync` artist hydration block.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnArtistItemsRequested` consumes the guard
  before building the search `RequestAsync` delegate.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — 3 new tests:
  `SelectArtist_ExistingSuggestion_SuppressesNextArtistSearch`,
  `InitializeArtistField_WithArtistId_SuppressesNextArtistSearch`,
  `ConsumeSuppressArtistSearch_NoProgrammaticSet_ReturnsFalse` (Red→Green: all three failed to
  compile before `ConsumeSuppressArtistSearch()` existed, then passed after the fix).

### AC traceability
| AC ID | Criterion | Implementation | Test |
|-------|-----------|-----------------|------|
| BUG-061 | Programmatic artist-text set (selection lock) must not re-open the dropdown | `LockArtist` sets guard; `OnArtistItemsRequested` consumes it | `SelectArtist_ExistingSuggestion_SuppressesNextArtistSearch` |
| BUG-061 | Programmatic artist-text set (edit-mode hydration) must not re-open the dropdown | `InitializeArtistField` sets guard | `InitializeArtistField_WithArtistId_SuppressesNextArtistSearch` |
| BUG-061 | User-typed search (no prior programmatic set) is never suppressed | guard defaults false | `ConsumeSuppressArtistSearch_NoProgrammaticSet_ReturnsFalse` |

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → exit 0, 0 errors (`net10.0-android`
  still blocked locally by the `XARLP7024` AV/EDR AAR-corruption issue documented in the prior
  checkpoint — not a code defect; Helder builds/verifies Android on device).
- **Tests:** 526/526 green (523 baseline + 3 new).
- **Manual E2E (on device, pending Helder):** select a suggestion → dropdown does not re-open on the
  picked artist; open the edit page → no dropdown reopening on initial hydration.

---
## Bug: BUG-064 (Minor) — duplicate artist error message
**Status:** To Review
**Severity:** Minor (cosmetic — same error text rendered twice)

**Root cause:** the Artist field's validation error was rendered by TWO bindings simultaneously: the
DX `AutoCompleteEdit`'s own `HasError`/`ErrorText` (bound at `SongFormPage.xaml` lines 33-34) — which
BUG-057's fix confirmed now surfaces correctly on-device per T10 re-run #3 — AND a separate `Label`
(`Text={Binding ArtistErrorText}`, `IsVisible={Binding ArtistHasError}`) added directly underneath it
in commit `cb78f3e` as a workaround for the DX control's error text not surfacing at the time.

**Fix:** removed the separate `Label` (kept the DX control's native `HasError`/`ErrorText` bindings —
MD3-compliant, matches every other field's error presentation on this page: `TitleErrorText`,
`VersionErrorText`, `PasteUrlError`, all rendered via each editor's own error slot, not a
sibling `Label`). `ArtistErrorText`/`ArtistHasError` remain unchanged in the ViewModel — the DX
control's bindings are the sole remaining consumer, so the surviving message is unaffected.

**Regression risk:** None — single-element XAML removal; the error is still shown via the DX
control's own binding, unchanged.

### Changed files
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — removed the standalone Artist-error `Label`
  (BUG-057 workaround), replaced with an explanatory comment.

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → exit 0, 0 errors — XAML compiled
  clean with the Label removed.
- **Tests:** no VM/testable seam — pure XAML markup removal. Included in the 526/526 run above
  (no regression to any VM test that reads `ArtistErrorText`/`ArtistHasError`).
- **Manual E2E (on device, pending Helder):** blur/trigger the artist error → exactly ONE error
  message renders under the field (the DX control's own inline error), not two.

---
### Checkpoint — COMPLETE (2026-07-25, T10 re-run #3 follow-up)
BUG-061 re-fixed at the correct root cause (programmatic-vs-user-typed text-set guard, VM-testable
seam) and BUG-064 fixed (removed redundant Label). Build 0 errors (net10.0; Android TFM still
blocked locally by the AV/EDR `XARLP7024` issue — unchanged from prior session, Helder builds
Android). Tests 526/526 green. Both fixes committed on `feat/inline-artist-create` in worktree
`MyVocaList-inline-ac`; manual on-device E2E for both is pending Helder's next T10 pass.

---
## BUG-061 re-fix completion — 2 missed paths closed (review finding)
**Status:** To Review
**Severity:** Major (same class as the original BUG-061 re-fix — code review found the guard incomplete)

**Finding:** code review of commit `7c594e2` confirmed the `_suppressNextArtistSearch`/
`ConsumeSuppressArtistSearch()` guard mechanism was correct but missed two more programmatic
`ArtistSearchText` assignment paths that reproduce the same dropdown-reopen symptom:

- **`ResolveAndLockArtistAsync`** (song-picker return flow, `OnSongPicked` → `ResolveAndLockArtistAsync`,
  a real user path): both the exact-match auto-lock branch (`ArtistSearchText = match.Name`) and the
  no-match prefill branch (`ArtistSearchText = artistName`) were unguarded.
- **`OnArtistBlurredWithoutSelection`**: the restore-prior-selection branch
  (`ArtistSearchText = SelectedArtistName ?? string.Empty`) was unguarded.

**Fix:** same one-shot `_suppressNextArtistSearch = true;` pattern already established — set
immediately before each of the three programmatic assignments above. No change to
`ConsumeSuppressArtistSearch()` or `OnArtistItemsRequested` (the consumer side was already correct).

**Regression risk:** Low — additive guard-set only; the assignments' existing behavior is unchanged.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — guard set before both `ResolveAndLockArtistAsync`
  branches and before the `OnArtistBlurredWithoutSelection` restore assignment.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — `CreateSut` gained an optional
  `messenger` parameter (defaults to a mock, unchanged for all existing tests); added `using
  MyVocaList.Contracts.DTOs;` and a `CanonicalSongPickedMessage` alias (mirrors the ViewModel's own
  alias — avoids ambiguity with `MyVocaList.UI.ViewModels.SongPickedMessage`); 3 new tests:
  `ResolveAndLockArtistAsync_ExactMatch_SuppressesNextArtistSearch`,
  `ResolveAndLockArtistAsync_NoMatch_SuppressesNextArtistSearch`,
  `ArtistBlurredWithoutSelection_RestoresPriorSelection_SuppressesNextArtistSearch`. The first two
  route through a real `WeakReferenceMessenger` instance (`OnSongPicked` is only reachable via
  message, not a public command) sending a `CanonicalSongPickedMessage`, exercising the exact
  `OnSongPicked` → `ResolveAndLockArtistAsync` path a real song-picker return uses.

### AC traceability
| AC ID | Criterion | Implementation | Test |
|-------|-----------|-----------------|------|
| BUG-061 | Song-picker exact-match auto-lock must not re-open the dropdown | `ResolveAndLockArtistAsync` match branch sets guard | `ResolveAndLockArtistAsync_ExactMatch_SuppressesNextArtistSearch` |
| BUG-061 | Song-picker no-match prefill must not re-open the dropdown | `ResolveAndLockArtistAsync` no-match branch sets guard | `ResolveAndLockArtistAsync_NoMatch_SuppressesNextArtistSearch` |
| BUG-061 | Blur-restore of prior selection must not re-open the dropdown | `OnArtistBlurredWithoutSelection` restore branch sets guard | `ArtistBlurredWithoutSelection_RestoresPriorSelection_SuppressesNextArtistSearch` |

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → exit 0, 0 errors.
- **Tests:** 529/529 green (526 baseline + 3 new).
- **Manual E2E (on device, pending Helder):** import a song via the song-picker (exact-match and
  no-match cases) → dropdown does not re-open on the auto-locked/pre-filled name; blur the artist
  field after typing over a previously-selected artist without picking a new suggestion → dropdown
  does not re-open on the restored name.

BUG-061 — ClearArtist guard added, final residual closed (re-verify finding). `SongFormViewModel.ClearArtist()` now sets `_suppressNextArtistSearch = true` before `ArtistSearchText = string.Empty`. New test `ClearArtist_SuppressesNextArtistSearch`. Build 0 errors; tests 530/530 green (529 baseline + 1 new).
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

---
## Task: Fix wave step 2 — BUG-067 (Critical) — editing a saved song's artist is not persisted
**Plan:** Helder's decision D2 (`§ DECISION TAKEN — Helder, 2026-08-02`)
**Status:** To Review
**Started:** 2026-08-02
**Completed:** 2026-08-02

### Root cause (Phase 1 — systematic-debugging, evidence before fix)
**The artist FK was a create-path-only assignment. The edit path could not send the artist at all.**

- `ISongService.UpdateSongAsync` had **no `artistId` parameter** in its signature.
- `SongService.UpdateSongAsync` (`Services/SongService.cs:99-144` pre-fix) only ever **read**
  `song.ArtistId` — at line 121, to scope the title-uniqueness check — and **never assigned it**.
  The FK was written exclusively by `CreateSongWithUrlsAsync`.
- `SongFormViewModel.ExecuteEditSaveAsync` (`:598-617` pre-fix) therefore passed
  (id, title, featuredArtists, lyrics, hasManualEdits, externalId, externalProvider, version) —
  structurally unable to communicate the user's new artist selection.

So on edit-save the ViewModel's `SelectedArtistId` was correct in memory (BUG-060/REQ-ACREATE-15's
clear-then-reselect works), the artist-required guard passed against it, and then the value was
simply dropped on the floor at the Service boundary. The stored song kept its original artist —
a silent loss of a user edit (Critical).

Ruled out by reading the actual code, not assumed: it is **not** a stale field captured at load
time (`SelectedArtistId` is live), **not** an EF change-tracking/NoTracking problem (the entity is
tracked and `UpdateAsync`/`SaveChangesAsync` do run — they just had nothing new to write for the
FK), and **not** hydration overwriting the selection (`LoadSongForEditAsync` runs once, before the
user's change).

### Observed Red (before the fix)
The signature was extended first (inert, no assignment) so the defect could be expressed as a test;
the failures below are the defect itself, not setup errors. Command:
`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "<the 5 new tests>"`

```
Com falha SongServiceTests.UpdateSongAsync_WithChangedArtistId_PersistsNewArtist [5 ms]
  Assert.Equal() Failure: Values differ
  Expected: 2
  Actual:   1                       <- song.ArtistId never re-assigned

Com falha SongServiceTests.UpdateSongAsync_UnknownArtistId_ReturnsFalseAndDoesNotWrite [2 ms]
  Assert.False() Failure  Expected: False  Actual: True     <- unknown artist accepted silently

Com falha SongServiceTests.UpdateSongAsync_ChangedArtistWithDuplicateTitle_ReturnsFalse
  Assert.False() Failure  Expected: False  Actual: True     <- uniqueness checked against OLD artist

Com falha SongFormViewModelTests.SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService [168 ms]
  Moq.MockException : Expected invocation on the mock once, but was 0 times:
    s => s.UpdateSongAsync(42, "Stored Title", ..., True, ..., 9, It.IsAny<CancellationToken>())
  Performed invocations:
    ISongService.UpdateSongAsync(42, "Stored Title", "", null, True, null, null, "", null, CancellationToken)
                                                                                     ^^^^ artistId null

Com falha! - Com falha: 4, Aprovado: 1, Ignorado: 0, Total: 5
```

`UpdateSongAsync_NullArtistId_KeepsExistingArtist` passed pre-fix by construction — it pins the
null-keeps-existing default so the fix cannot regress untouched-artist edits.

### Fix
1. **Service (business logic — constitutional layer).** `UpdateSongAsync` gains
   `int? artistId = null` (null = keep existing, the same semantics `version`/`externalId` already
   use). When a different id is supplied it is validated against `IArtistRepository` ("Artist not
   found" on miss, no write); the title-uniqueness check now runs against the **effective** artist;
   `song.ArtistId = effectiveArtistId` is assigned before the write.
2. **ViewModel (plumbing only).** `ExecuteEditSaveAsync` forwards `SelectedArtistId` — completing
   the "send the complete current form data" contract BUG-024 established.

No logic moved into the ViewModel; validation, existence and uniqueness all stay in `SongService`.

### Observed Green (after the fix)
`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`
```
Aprovado! - Com falha: 0, Aprovado: 535, Ignorado: 0, Total: 535, Duracao: 3 s
```
535 = 530 baseline + 5 new. No pre-existing test regressed.

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-16 | A changed artist on a saved song is persisted | `SongService.UpdateSongAsync` (`song.ArtistId = effectiveArtistId`) | `UpdateSongAsync_WithChangedArtistId_PersistsNewArtist` |
| REQ-ACREATE-16 | Edit-mode Save forwards the current selection | `SongFormViewModel.ExecuteEditSaveAsync` | `SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService` |
| REQ-ACREATE-16 | null = keep existing (untouched artist not cleared) | `SongService.UpdateSongAsync` (`artistId ?? song.ArtistId`) | `UpdateSongAsync_NullArtistId_KeepsExistingArtist` |
| REQ-ACREATE-16 | Unknown artist id rejected, no write | `SongService.UpdateSongAsync` artist-existence guard | `UpdateSongAsync_UnknownArtistId_ReturnsFalseAndDoesNotWrite` |
| REQ-ACREATE-16 | Uniqueness evaluated against the NEW artist | `SongService.UpdateSongAsync` (`ExistsByTitleForArtistAsync(effectiveArtistId, ...)`) | `UpdateSongAsync_ChangedArtistWithDuplicateTitle_ReturnsFalse` |
| REQ-ACREATE-16 | Reopening the song shows the new artist (end-to-end) | full edit flow | **on-device only — T10 re-run #5** |

### Existing tests touched — mechanical arity only, FLAGGED for Helder
Adding a parameter to `UpdateSongAsync` breaks every Moq `Setup`/`Verify` lambda that names the
method, because the expression must match the new arity. 10 such expressions were updated by
inserting `It.IsAny<int?>()` in the new position. **No assertion was relaxed, removed, weakened or
re-pointed** — this is signature propagation, not adjudication of a failing test, and it is
categorically different from the (spent) BUG-061 authorisation. Sites:
`SongResolutionServiceTests.cs` (6) and `SongFormViewModelTests.cs` (4). `SongServiceTests.cs`
needed none (it already used named arguments). Production call sites in
`SongResolutionService.cs:208,232` switched their positional `ct` to `ct: ct`.
If Helder prefers zero test churn, the alternative was placing `artistId` after
`CancellationToken ct` — rejected as it violates the ct-last convention in a Domain interface.

### Design concern (implemented as specified, raised for review)
`ExecuteEditSaveAsync` now always sends the current artist id rather than only on change. This is
consistent with BUG-024's "send the complete current form data" and avoids a stale
original-artist baseline field, but it means an edit-save re-validates the artist whenever it
differs from stored (it short-circuits when equal). No behavioural downside found.

### Changed files:
- `Domain/ServicesInterfaces/ISongService.cs` (worktree) — `UpdateSongAsync` gains `int? artistId = null` + XML doc for it.
- `Services/SongService.cs` (worktree) — artist-existence guard, effective-artist uniqueness check, `song.ArtistId` assignment.
- `Services/SongResolutionService.cs` (worktree) — 2 call sites: positional `ct` → named `ct: ct`.
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (worktree) — `ExecuteEditSaveAsync` forwards `SelectedArtistId`.
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (worktree) — 4 new service-seam regression tests.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (worktree) — 1 new VM-seam regression test + 4 arity updates.
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` (worktree) — 6 arity updates.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/requirements.md` (develop) — **REQ-ACREATE-16 added** (SDD invariant: no existing AC covered artist mutation on an existing song; REQ-ACREATE-08 covers only the create path).
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/task-log.md` (develop) — this entry.

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → `ok dotnet build: 6 projects, 0 errors, 10 warnings`. First attempt, no retries.
- **Tests:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → `Com falha: 0, Aprovado: 535, Total: 535`.
- **Red→Green:** both outputs captured above, in this session.
- **Files re-read after edit:** `Services/SongService.cs` (:98-159), `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (:598-621), `Domain/ServicesInterfaces/ISongService.cs`, `requirements.md`.
- **.sln registration:** no file created/moved/deleted under `Docs/` or `.claude/` — nothing to register.
- **BUG-065/066 not re-touched:** this fix is entirely in the save/persistence path; no artist-field
  search, lock, dropdown or ➕-row behaviour was modified. Helder's pending device verification of
  065/066 is unaffected.

---
## Task: Strengthen two weak dropdown-close regression tests (verifier follow-up, 2026-08-02)
**Plan:** this folder
**Status:** To Review
**Started:** 2026-08-02
**Completed:** 2026-08-02

### Context
Verifier flagged `SelectArtist_ExistingSuggestion_ClosesDropdown` and
`InitializeArtistField_WithArtistId_ClosesDropdown` as asserting `Assert.False(sut.IsArtistDropDownOpen)`
without seeding the property `true` first. Since `IsArtistDropDownOpen` defaults to `false`, both tests
passed regardless of whether the production assignment fired — they proved nothing. Production code was
confirmed correct (both `LockArtist` and `InitializeArtistField` do set `IsArtistDropDownOpen = false`);
this was a test-quality gap only, no production defect.

### Changed files:
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — both tests now seed
  `sut.IsArtistDropDownOpen = true;` before the action, mirroring the arrange/act/assert shape already
  used by `ClearArtist_ClosesDropdown` / `ResolveAndLockArtistAsync_ExactMatch_ClosesDropdown` /
  `ResolveAndLockArtistAsync_NoMatch_ClosesDropdown` / `ArtistBlurredWithoutSelection_RestoresPriorSelection_ClosesDropdown`.
  `[AC]` tags preserved unchanged.

### Comment-out / restore evidence (proves the strengthening actually catches the defect)
- `LockArtist` (`SongFormViewModel.cs:333`, `IsArtistDropDownOpen = false`) commented out →
  `SelectArtist_ExistingSuggestion_ClosesDropdown`: **FAIL** (`Assert.False() Failure — Expected: False, Actual: True`).
  Line restored → same test: **PASS** (1/1).
- `InitializeArtistField` (`SongFormViewModel.cs:422`, `IsArtistDropDownOpen = false`) commented out →
  `InitializeArtistField_WithArtistId_ClosesDropdown`: **FAIL** (same assertion failure).
  Line restored → same test: **PASS** (1/1).

### Build notes
Build: passed (0 errors) | Tests: 535 passed, 0 failed (net10.0 only — Android blocked locally by
`XARLP7024`, an AV/EDR artifact, not code) | Commit SHA: see `feat/inline-artist-create` HEAD
`git diff --stat` at commit time showed only `SongFormViewModelTests.cs` — no production file touched.

### BOM observation (report only, not fixed — out of scope)
`SongResolutionServiceTests.cs`, `SongServiceTests.cs`, and `SongFormViewModelTests.cs` all begin with a
UTF-8 BOM (`EF BB BF`). Sampled 10 other files in `MyVocaList.Tests/Unit/ViewModels/` — none have a BOM.
This does not match the project convention; a byte-level sweep to fix it is out of scope for this task.

### Requires Helder's device (T10 re-run #5)
- End-to-end: open a saved song → clear (X) the locked artist → pick or inline-create a different
  artist → Save → reopen → the new artist is shown. Persistence is proven by unit test; the UI path
  that feeds `SelectedArtistId` (the DX `AutoCompleteEdit` clear/select interaction) is not.
- The duplicate-title-on-new-artist rejection message surfacing correctly in the form
  (it is routed to `TitleErrorText`, the existing edit-save failure channel).


---

## T10 re-run #5 — Helder, on device + emulator, 2026-08-02 → FAILED (Critical regression exposed)

Build under test: `e13a495` on `feat/inline-artist-create` (535/535 unit tests green).
Tested on BOTH an emulator (x86_64, debugger attached) and a physical device (arm64) — **identical
behaviour on both**, so nothing here is emulator-specific.

### Per-item results

| Item | Covers | Result | Disposition |
|------|--------|--------|-------------|
| A1 first keystroke shows real matches | BUG-065(b) | ✅ | fixed — guard removal worked |
| A2 second keystroke correct | BUG-065(b) | ✅ | fixed |
| A3 no spurious "Not found" after programmatic fill | BUG-065(a) | ⚠️ **PARTIAL** | "Not found" row is GONE, but the dropdown **re-opens** after selection — see BUG-069 |
| B1 ➕ row appears on a novel name (Add mode) | BUG-066 | ✅ | fixed |
| B2 ➕ completes the creation | BUG-066 | ✅ | fixed |
| B3 ➕ row appears in Edit mode | BUG-066 | ✅ | fixed |
| C1 edit page shows the saved artist | BUG-067 | ✅ | hydration correct |
| C2 clear (X) unlocks the field | BUG-060 | ✅ | still fixed |
| C3 select a different artist | — | ✅ | selection works |
| C4 **Save** | BUG-067 | ❌ **FAILED** | **BUG-068 (Critical)** — see below |
| C5 reopen shows the new artist | BUG-067 | ⛔ **NOT TESTABLE** | blocked by C4 |
| C6 Songs list under new artist | BUG-067 | ⛔ NOT TESTABLE | blocked by C4 |
| D1 dropdown does not re-open | BUG-061 | ❌ **FAILED** | **BUG-069** — same defect as A3 |
| D2 single readable error message | BUG-064/057 | ⚠️ **UX DEFECT** | **BUG-070** — message misleads; see below |
| D3 typed text retained on blur | REQ-ACREATE-03 | ✅ | holds |

### 🔴 BUG-068 (Critical, NEW) — EF Core identity conflict aborts every edit-mode save

**This supersedes BUG-067 as the reason an artist edit does not persist.** BUG-067's fix (the missing
`artistId` parameter) was necessary but NOT sufficient — the write now *reaches* the repository, and
the repository throws.

Two distinct user-visible behaviours, same root area:

1. **Tap a suggestion ONCE, then Save** → the UI reports **success**, but the change is **not
   persisted** (silent data loss — no exception surfaced to the user).
2. **Tap a suggestion, then tap it again when the dropdown re-shows it** (i.e. after BUG-069), then
   Save → **"Failed to save song. Please try again."** plus the logged exception below.

Verbatim exception (identical on emulator `00:19:29` and device `21:32:48`):

```
[ERR] MyVocaList.UI.ViewModels.SongFormViewModel
Save failed in Edit mode
System.InvalidOperationException: The instance of entity type 'Song' cannot be tracked because
another instance with the same key value for {'Id'} is already being tracked. When attaching
existing entities, ensure that only one entity instance with a given key value is attached.
   at ...IdentityMap[System.Int32].ThrowIdentityConflict(InternalEntityEntry entry)
   at ...StateManager.StartTracking(InternalEntityEntry entry)
   at ...EntityGraphAttacher.AttachGraph(...)
   at ...InternalDbSet[MyVocaList.Domain.Entity.Song].Update(Song entity)
   at MyVocaList.Infra.Repository.SongRepository.UpdateAsync(Song song, CancellationToken ct)
        in Infra\Repository\SongRepository.cs:line 135
   at MyVocaList.Services.SongService.UpdateSongAsync(Int32 id, String title, String featuredArtists,
        String lyrics, Boolean hasManualEdits, String externalId, String externalProvider,
        String version, Nullable[Int32] artistId, CancellationToken ct)
        in Services\SongService.cs:line 156
   at MyVocaList.UI.ViewModels.SongFormViewModel.ExecuteEditSaveAsync(String title, String version)
        in MyVocaList\UI\ViewModels\SongFormViewModel.cs:line 605
   at MyVocaList.UI.ViewModels.SongFormViewModel.SaveAsync()
        in MyVocaList\UI\ViewModels\SongFormViewModel.cs:line 579
```

**Reading of the trace (hypothesis — NOT proven; must be verified before any code is written):**
`SongService.UpdateSongAsync` loads the `Song` (tracked) to run its checks, then calls
`SongRepository.UpdateAsync`, which calls `DbSet.Update(song)` on what is by then a SECOND instance
carrying the same key — the first is still tracked in the same scoped `DbContext`. The BUG-067 fix
added a `song.ArtistId = effectiveArtistId` write against the loaded instance, which is plausibly
what pushed a latent tracking conflict onto the failing path.

Directions to EVALUATE (deliberately not pre-committed to one): mutate the already-tracked instance
and `SaveChanges` without calling `DbSet.Update`; or read with `AsNoTracking` and attach exactly
once; or narrow the read/write to a single tracked entity. The read-model / NoTracking rules in
`library/database-indexing.md` and `code-style-reference.md` apply, and whatever is decided here
likely feeds the parked **Read Model + Global NoTracking Pattern — Guidelines Update** activity.

**Why the unit suite missed it completely:** `SongServiceTests` mock `ISongRepository`, so
`DbSet.Update` never executes; and `SongRepository` has no integration test covering
update-after-read within one context. `testing.md § Project anti-patterns` already requires
repository tests to run against **real SQLite** — that unmet requirement is precisely why 535/535
was green while every edit-mode save failed on the device.

**Severity: Critical** (silent data loss on path 1, hard failure on path 2). Regression test is
MANDATORY, Red before Green, at the **repository/integration seam against real SQLite**. A mocked
service test cannot reproduce this and must not be accepted as the regression test.

### 🟠 BUG-069 (Major, NEW — supersedes/reopens BUG-061) — dropdown re-opens after a selection, listing prefix matches

After tapping a suggestion the list correctly hides and then **immediately re-opens**, showing every
artist whose name prefix-matches the picked one. With artists *Helder*, *Helder Sousa* and
*Helder Carvalho de Sousa*, picking **Helder** re-shows all three. It also occurs on **edit-mode page
load**.

The re-shown row is tappable, and tapping it is exactly the sequence that produces BUG-068 path 2 —
so BUG-069 is the **trigger for the Critical failure**, not a cosmetic nuisance. Fixing 068 without
069 leaves the user one tap away from the same crash.

The mechanism has changed since BUG-061: that was a *lingering* row (never dismissed); this is
*dismiss-then-re-open* — the selection commits text into the editor, and the committed text then
appears to be treated as a fresh query. The `IsDropDownOpen = false` write does happen; something
re-opens the popup afterwards. What re-opens it must be established from decompiled DevExpress IL
before editing. Per `§ IL evidence (2026-07-30)`, `IsDropDownOpen` is confirmed as the supported
lever; what is NOT established is the re-open path.

### 🟡 BUG-070 (Minor/UX, NEW) — validation message tells the user to do the one thing it implies they cannot

On a novel artist name the field shows **"Search and select an artist from the list"**, which reads
as "creating a new artist here is impossible" — the opposite of the feature that now works (B1–B3 all
passed). The Artist field is also styled with the error border while the ➕ create path is still
available.

Copy and trigger condition both need Helder's decision, and this likely changes a REQ-ACREATE
acceptance criterion — so it is spec work first, not a code-only fix.

### Evidence artifacts

Full emulator and device logcat captures were supplied by Helder in-session (2026-08-02). The
exception blocks are transcribed verbatim above; the remaining lines are HWUI / Choreographer / IME
noise with no diagnostic value. Screenshots (4-panel) show: the dropdown re-open with three prefix
matches, the misleading validation message, and two "Failed to save song. Please try again." states.

Also present in the logs and deliberately NOT filed as new bugs (pre-existing or out of scope — do
not chase these during the fix waves):

- `SongsPage appearing=47883ms (ctor→OnAppearing)` — Debug-build page-load cost; already covered by
  the CRUD page structural reduction backlog item.
- `Skipped NN frames` / `Davey! duration=…` — Debug-build jank.
- `FORTIFY: pthread_mutex_lock called on a destroyed mutex` + `SIGABRT` at emulator teardown — this
  is the known **BUG-026** HWUI render-teardown crash.

### Status

INLINE-AC closeout is **blocked**, and the branch must **NOT** be merged to develop:
`feat/inline-artist-create` currently persists no artist edit at all in edit mode. The BUG-067 fix
and REQ-ACREATE-16 remain correct and stay — they are simply not sufficient on their own.

---
## Task: Fix BUG-068 (Critical) — EF Core identity conflict aborts every edit-mode save
**Plan:** narrow single-bug dispatch (Helder, 2026-08-03) — BUG-069/BUG-070 explicitly out of scope
**Status:** To Review
**Started:** 2026-08-03
**Completed:** 2026-08-03

### Root cause (proven, systematic-debugging — reproduced before any fix)
**`AppDbContext` is registered `Scoped` (`AddDbContext`), but MAUI's DI container has no
per-page/request child scope — the root `ServiceProvider` resolves one instance that lives for
the app's entire session, behaving as a de facto singleton.**
`ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking` (global default,
`AppDbContext.cs:37`) only suppresses tracking for **query results** — it does NOT detach
entities that were explicitly `Add()`'ed/`Update()`'d and then `SaveChangesAsync()`'d earlier in
that same long-lived context. A Song's own creation (`SongRepository.AddAsync` +
`SaveChangesAsync`, or any earlier edit-save) therefore leaves it tracked (`Unchanged`) in the
context's identity map for the rest of the app session. The next edit-save's
`SongRepository.GetByIdAsync` (untracked, per the global default — it has no `.AsTracking()`
call despite its stale comment) returns a **second, distinct** `Song` instance with the same
`Id`. `SongRepository.UpdateAsync`'s `_db.Songs.Update(song)` then tries to attach that second
instance, and EF Core's `IdentityMap.Add` throws
`InvalidOperationException: The instance of entity type 'Song' cannot be tracked because another
instance with the same key value for {'Id'} is already being tracked` — exactly the verbatim
device/emulator exception in T10 re-run #5.

**Confirmed empirically, not assumed:** a diagnostic test dumped `_db.ChangeTracker.DebugView`
immediately before the failing `Update(song)` call and showed a **second tracked `Song {Id: 1}
Unchanged`** entry that neither `GetByIdAsync` call created directly — it was the leftover from
the test's own `Add` + `SaveChangesAsync` seeding step, on the SAME context, reproducing the
exact production mechanism (song creation, then a later edit-save, sharing one long-lived
`AppDbContext`). A control test proved the conflict fires even with **zero prior reads** — a
single `UpdateSongAsync` call is sufficient, once the row has EVER been written before in that
context's lifetime (which is always true for a "saved song" — it was created at some point).

**Why 535/535 unit tests were green while every device save failed:** `SongServiceTests` mocks
`ISongRepository`, so `DbSet.Update`'s identity-map code never executes — `testing.md § Project
anti-patterns` already flags exactly this gap. No prior test exercised `SongRepository` against a
`AppDbContext` that had EVER written the same Song row before.

### Both user-visible faces, addressed
- **Face 2 (hard failure — the exception above):** directly reproduced and fixed by the change
  below; proven by the Red→Green pair.
- **Face 1 (silent "success", no persisted change, no exception):** the identity conflict is
  deterministic — once a Song row has been touched before in the app session (always true for a
  saved song), `Update()` throws every time, not intermittently. A silent-success-with-no-write
  and no exception is therefore **not explained by BUG-068's mechanism** and is not reproducible
  from this root cause; it is consistent with BUG-069 (the dropdown reopening and the user's
  selection reverting to the original artist before Save reads it) being mistaken for a
  persistence failure — nothing changed because nothing was actually selected differently by the
  time Save ran. This fix does not touch the artist-field search/dropdown/selection path (out of
  this task's scope per the briefing); BUG-069 needs its own on-device re-verification to confirm
  face 1 is closed. Flagging this explicitly rather than claiming face 1 fixed by inference.
- Independent of the above, this fix's regression tests DO prove that once a save reaches the
  repository, it always **persists** the change and never silently no-ops — closing the
  "no exception, but also nothing written" possibility that would exist even for a correctly
  selected artist, for any Song row already tracked in the context.

### Fix
`SongRepository.UpdateAsync` (`Infra/Repository/SongRepository.cs:133-149`): before calling
`_db.Songs.Update(song)`, look up whether a `Song` with the same `Id` is already tracked in the
context (`_db.ChangeTracker.Entries<Song>()`). If a tracked instance exists and it is a different
object reference than the one passed in, copy the new instance's values onto the **tracked**
entry via `tracked.CurrentValues.SetValues(song)` (scalar/FK properties only — it does not touch
navigation properties, so it cannot re-attach `song.OriginalArtist`'s graph either). Otherwise
(first-ever touch of this row in the context, or the exact same tracked instance was passed in),
fall back to the original `_db.Songs.Update(song)`.

This is scoped to `SongRepository` only, as instructed — it does not change `AppDbContext`'s
registration, `QueryTrackingBehavior`, or any other repository. The underlying MAUI-DI
Scoped-behaves-as-Singleton condition is broader than this one repository (every repository over
`AppDbContext` is subject to the same "row already tracked from an earlier write" hazard) — **this
implication is flagged for Helder**, per the briefing's escalation instruction, rather than
generalized here. It likely feeds the parked **Read Model + Global NoTracking Pattern —
Guidelines Update** activity referenced in the T10 re-run #5 entry above.

### Observed Red (before the fix — both new tests)
```
System.InvalidOperationException : The instance of entity type 'Song' cannot be tracked because
another instance with the same key value for {'Id'} is already being tracked. When attaching
existing entities, ensure that only one entity instance with a given key value is attached.
   at ...IdentityMap`1.ThrowIdentityConflict(InternalEntityEntry entry)
   at ...StateManager.StartTracking(InternalEntityEntry entry)
   at ...EntityGraphAttacher.AttachGraph(...)
   at ...InternalDbSet`1.Update(TEntity entity)
   at MyVocaList.Infra.Repository.SongRepository.UpdateAsync(Song song, CancellationToken ct)
        in Infra\Repository\SongRepository.cs:line 135
   at MyVocaList.Services.SongService.UpdateSongAsync(...) in Services\SongService.cs:line 156
   at MyVocaList.Tests.Integration.Services.SongServiceUpdateIntegrationTests.
        UpdateSongAsync_AfterPriorHydrationRead_PersistsChangedArtistWithoutThrowing()
Com falha! - Com falha: 2, Aprovado: 0, Ignorado: 0, Total: 2
```
(Both `UpdateSongAsync_AfterPriorHydrationRead_PersistsChangedArtistWithoutThrowing` and
`UpdateSongAsync_SongAlreadyTrackedInContext_PersistsWithoutThrowing` failed with the identical
exception — captured with the production fix commented out, then the line was restored:
comment-out/restore evidence, not a one-shot observation.)

### Observed Green (after the fix)
```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongServiceUpdateIntegrationTests"
Aprovado! – Com falha: 0, Aprovado: 2, Ignorado: 0, Total: 2, Duração: 739 ms

dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj (full suite)
Aprovado! – Com falha: 0, Aprovado: 537, Ignorado: 0, Total: 537, Duração: 4 s
```
537 = 535 baseline (per T10 re-run #5's recorded count) + 2 new. No pre-existing test regressed.

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|-------------|
| REQ-ACREATE-16 | A changed artist on an already-saved song persists even after the page's edit-mode hydration read has run on the same `AppDbContext` | `SongRepository.UpdateAsync` (tracked-entry `SetValues` path) | `UpdateSongAsync_AfterPriorHydrationRead_PersistsChangedArtistWithoutThrowing` |
| REQ-ACREATE-16 | A changed artist persists on a Song row already tracked in the context from its own earlier creation/save (BUG-068 face 2 — the exact device exception) | `SongRepository.UpdateAsync` (tracked-entry `SetValues` path) | `UpdateSongAsync_SongAlreadyTrackedInContext_PersistsWithoutThrowing` |
| REQ-ACREATE-16 | End-to-end device confirmation that the save no longer throws and the new artist is shown on reopen | full edit flow | **on-device only — requires a fresh T10-style re-run, not covered here (BUG-069 still open on the same page)** |

### What could NOT be verified without a device
- The actual on-device/emulator save flow end-to-end (the original T10 re-run #5 repro steps).
  The repository-seam test reproduces the exact exception and proves the fix removes it and
  persists correctly; it cannot observe the DevExpress UI layer.
- Whether face 1 (silent success, nothing persisted, no exception) is actually closed — per the
  analysis above, that symptom is not explained by BUG-068's mechanism and is suspected to be a
  BUG-069 (dropdown-reopen/selection-reversion) symptom instead. Needs Helder's device
  re-verification with BUG-069 also fixed (or independently, to isolate which bug degrades which
  symptom).

### Changed files:
- `Infra/Repository/SongRepository.cs` (worktree `MyVocaList-inline-ac`) — `UpdateAsync` now
  checks `_db.ChangeTracker.Entries<Song>()` for an already-tracked instance with the same `Id`
  and updates its `CurrentValues` instead of attaching a second instance via `DbSet.Update`.
- `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (worktree,
  **new file**) — 2 new repository/integration-seam regression tests against real SQLite
  (`TestDbContextFactory`), constructing `SongService` with real `SongRepository`/`ArtistRepository`
  (not mocked) so `DbSet.Update`'s identity-map code actually executes.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/requirements.md`
  (develop) — `> Spec updated [2026-08-03]` note appended to REQ-ACREATE-16 documenting the
  repository-seam regression-test requirement this bug exposed.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/task-log.md`
  (develop) — this entry.

### Verification evidence
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0` → `ok dotnet build: 6 projects, 0 errors, 11 warnings` (all pre-existing NU1903, unrelated). First attempt, no retries.
- **Tests:** full suite `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → `Com falha: 0, Aprovado: 537, Total: 537`.
- **Red→Green:** both captured above in this session, plus an explicit comment-out/restore pass on the production fix (not just the initial Red before any code existed).
- **Files written and re-read after edit:** `Infra/Repository/SongRepository.cs` (:132-158, re-read after the comment-out/restore cycle to confirm the fix is the version left in place), `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (full file re-read), `requirements.md` (REQ-ACREATE-16 section re-read).
- **`.sln` registration:** not required — no file created/moved/deleted under `Docs/` or `.claude/`; the new test `.cs` file is covered by the test project's existing glob include, matching the pattern of every other file in `Integration/Repositories/`.
- **BUG-069/BUG-070 not touched:** confirmed no edit to `SongFormPage.xaml(.cs)`, `SongFormViewModel.cs`'s artist search/dropdown/lock members, or any validation-message string. `git diff --stat` on the worktree shows only `Infra/Repository/SongRepository.cs` (production) and one new test file.

### Design concern / escalation (not blocking, flagged per briefing)
The root architectural condition — `AppDbContext` Scoped-registered but MAUI has no per-page DI
scope, so it behaves as a singleton for the app's session — affects every repository built on
`AppDbContext`, not just `SongRepository`. This fix is intentionally narrow (Song only, per the
briefing's explicit instruction not to make a broader read-model/NoTracking policy change). The
same "row already tracked from an earlier write, second read collides on `Update()`" hazard is
latent in any other repository whose `UpdateAsync` calls `DbSet.Update(entity)` after a fresh
(NoTracking) read of a previously-written row. Recommend this feeds the parked **Read Model +
Global NoTracking Pattern — Guidelines Update** activity as a concrete, reproduced case, or that
Helder considers introducing an actual `IServiceScopeFactory`-based per-page scope in
`MauiProgram.cs` (sequential-only file — out of this task's scope) as the systemic fix.

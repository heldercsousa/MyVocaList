# Song Form Validation — Task Log (Task 04)

**Feature:** Apply the Form Validation Standard to `SongFormPage` / `SongFormViewModel`.
**Standard source:** `.claude/library/dialogs-validation.md § Form Validation Standard`
**Requirements source:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/01-ui-form-validation-guide.md`
**Reference implementation:** `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md` (Task 03, Person/Singer —
multi-field reference), `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md` (Task 02, Venue — single-field
baseline).

---

## Task: Song form — title / version fields — blur + keystroke-clear inline validation

**Plan:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` (Task 04)
**Status:** To Review
**Started:** 07/01/2026
**Completed:** 07/01/2026

### Requirement ID scheme

Reused from the Venue/Person reference (`venues/form-validation-task-log.md`, `persons/form-validation-task-log.md`) —
same IDs, same source (`01-ui-form-validation-guide.md` + `dialogs-validation.md § Form Validation Standard`).

| ID | Requirement |
|----|-------------|
| R1 | Validate on **blur** for standard fields (dirty field only). |
| R2 | "Reward early": while a field is in error, re-validate on each keystroke; clear the instant it is valid. |
| R3 | Do **not** validate on keystroke before the field is in error (no "Impatient Teacher"). |
| R4 | **Save** re-runs all validators as the final safety net (+ uniqueness/DB checks). |
| R5 | No **Wall of Red** (submit-only + error summary). |
| R6 | No native dialog / summary / snackbar as the validation channel — inline only. |
| R7 | Error messages are specific and actionable. |
| R8 | No premature error on a **pristine** field the user only tabbed through. |
| R9 | Errors are **field-addressed** and inline via `HasError` + `ErrorText`. |
| R10 | Integer rules — spec-incomplete, N/A to this form. |
| — | **Edit-mode dirty-guard** (Task 02/03 pattern): `[QueryProperty]` pre-population must not mark a field dirty, or a pre-filled invalid value flashes an error on first blur before the user touches anything. |

### Scope decisions / out of scope

- **Fields validated:** Title (`SongTitle`) and Version (`SongVersion`) only — the two free-text `dxe:TextEdit`
  fields on the form that have (or need) a validation rule.
- **Artist field is OUT OF SCOPE.** It is an `autocomplete:AutocompleteField` (picker-driven), not a free-text
  field — per the task brief, picker/autocomplete-driven fields are excluded from the blur-validation pattern.
  Its existing `ArtistHasError`/`ArtistErrorText` + `ArtistBlurredWithoutSelectionCommand` wiring (BUG-008)
  is unrelated prior work and was left untouched.
- **FeaturedArtists field is OUT OF SCOPE.** It is a free-text `dxe:TextEdit` but has no defined validation
  rule anywhere in the spec or `SongConfiguration.cs` (no `HasMaxLength`, `IsRequired(false)` only) — per the
  Brownfield rule (`workflow.md`) and the over-specification guard, no validation rule was invented for it.
- **Lyrics field is OUT OF SCOPE.** Stock MAUI `Editor` (pre-existing choice, not a `dxe:TextEdit`), not part
  of this task's blur-validation scope, and has no defined validation rule either.
- **New service method added: `ISongService.ValidateVersionInput(string? version, bool isRequired = false)`.**
  `ValidateTitleInput` already existed with full branch coverage and was reused unchanged. The Version field
  had **no** existing service-level validator — its "required" rule for the "Save as new version" flow was
  previously hardcoded directly in `SongFormViewModel.ConfirmSaveAsNewVersionAsync` (a constitutional
  violation of "business logic in Services" that pre-dated this task). This task both (a) added the missing
  optional-field max-length validator (`MaxVersionLength = 60`, matching `SongConfiguration.Version`'s
  `HasMaxLength(60)`) for the main-form blur/keystroke path, and (b) moved the pre-existing "required in this
  context" rule into the same service method via the `isRequired` parameter, fixing the constitutional gap
  as a byproduct of touching that exact code path.
- **`SaveAsync` safety net simplified.** The prior code manually cleared `VersionHasError`/`VersionErrorText`
  at the top of `SaveAsync` (to erase a stale error from a previous "Save as new version" attempt) and only
  validated Title. The new safety net calls `ApplyTitleValidation` + `ApplyVersionValidation` unconditionally,
  which naturally overwrites/clears any stale Version error as a side effect (Version is optional in the main
  form, so an empty value always validates as `true` there) — the manual clear was removed as redundant.
- **BUG-020 preserved.** `SongFormViewModel.RefreshApiKeyFlagAsync()`'s existing try-catch around
  `SecureStorage.GetAsync` (added in commit `3b2cb75`, already on `develop` before this task started) was
  **not modified**. `SongFormPage.xaml.cs.OnAppearing` was restructured to call `ViewModel.CompleteHydration()`
  before `await ViewModel.RefreshApiKeyFlagAsync()`, but the method body of `RefreshApiKeyFlagAsync` itself
  (where the try-catch lives) was untouched.

### Changed files

- `Domain/ServicesInterfaces/ISongService.cs` — added `ValidateVersionInput(string? version, bool isRequired = false)`
  to the interface with XML doc comment.
- `Services/SongService.cs` — added `MaxVersionLength => 60` constant; implemented `ValidateVersionInput`
  (empty valid unless `isRequired`; max 60 chars).
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — added `_titleDirty`/`_versionDirty` flags; added
  `_isHydrating` guard + public `CompleteHydration()`; added `[RelayCommand] ValidateTitle()` /
  `ValidateVersion()` (blur, dirty-guarded); rewrote `OnSongTitleChanged` to keystroke-revalidate only when
  already in error and skip dirty-marking during hydration (previously called an unconditional
  `ClearTitleError()` on every keystroke); added new `OnSongVersionChanged` partial method (did not exist
  before — Version had no keystroke handling at all); extracted `ApplyTitleValidation`/`ApplyVersionValidation`;
  rewrote `SaveAsync` to call both validators unconditionally as the safety net instead of only validating
  Title and manually clearing Version's error; rewrote `ConfirmSaveAsNewVersionAsync` to call
  `_songService.ValidateVersionInput(version, isRequired: true)` instead of a hardcoded
  `string.IsNullOrEmpty(version)` check; removed the now-unused `ClearTitleError()` helper.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — added `Unfocused="OnTitleUnfocused"` to the Title
  `dxe:TextEdit`; added `Unfocused="OnVersionUnfocused"` to **both** Version `dxe:TextEdit` instances (main
  form field and the "Save as new version" resolution-sheet field — both bind to the same `SongVersion`
  property).
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — added `private SongFormViewModel ViewModel => (SongFormViewModel)BindingContext;`
  property (replacing the ad hoc `if (BindingContext is SongFormViewModel vm)` pattern); added
  `ViewModel.CompleteHydration()` call in `OnAppearing` (after Shell has applied `[QueryProperty]` values,
  before `RefreshApiKeyFlagAsync`/`InitializeArtistField`); added `OnTitleUnfocused`/`OnVersionUnfocused`
  handlers bridging blur events to the ViewModel commands. The BUG-020 try-catch inside
  `RefreshApiKeyFlagAsync` (ViewModel method, not touched by this file) is unaffected.
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` — added 5 new `[Fact]` tests for
  `ValidateVersionInput` (empty+not-required valid, empty+required invalid, too-long invalid,
  max-length-60 valid, valid value) on top of 21 pre-existing tests (26 total in this file).
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — added 12 new `[Fact]` tests (blur /
  keystroke / Save-safety-net / edit-mode hydration-guard, one group per field: Title and Version) on top of
  18 pre-existing tests (30 total in this file). Also updated 6 pre-existing tests
  (`SaveAsync_ServiceThrows_ShowsErrorSnackbar`, `SaveAsync_NoArtist_SetsArtistError_NoServiceCall`,
  `SaveAsync_NoMatch_CallsCreateSongWithUrls`, `SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible`,
  `ConfirmUpdateExisting_TargetHasManualEdits_PopulatesMergeRows`, `ConfirmSaveAsNewVersion_EmptyVersion_SetsVersionError`)
  to add a `ValidateVersionInput` mock setup — required because `SaveAsync`/`ConfirmSaveAsNewVersionAsync`
  now call the new interface method, and Moq's default unconfigured return for an unmocked tuple method is
  `(false, null)`, which broke these tests' pre-existing assertions about reaching the artist-check /
  resolution-flow code paths. This is a mechanical interface-extension update to test *setup*, not a change
  to any test's assertions or intent (see `testing.md § Builder Must Not Modify Tests` — the assertions
  themselves are unchanged in all 6 tests).
- `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md` — this file (NEW).
- `MyVocaList.sln` — registered the new task-log doc under the existing `artists-songs` solution folder
  (`{C141C5C9-833C-4A26-96BF-3745A2DA1AD4}`).

### Verification evidence

- Build (`dotnet build MyVocaList.sln`, all 7 projects — net10.0, net10.0-android, net10.0-ios where
  applicable): PASS — **0 errors**, 90 warnings (all pre-existing DevExpress trial-license / nullability /
  CA-analyzer warnings unrelated to this change; no new warning categories introduced). An earlier full-solution
  build attempt failed with `XA0142`/`XAWAS7024` (Android APK-linking file lock from a stale concurrent
  `dotnet.exe` process holding an intermediate file) — confirmed via `grep -i "error CS"` that this was **not**
  a C# compile error; a clean re-run after the stale process released the lock produced 0 errors.
- Tests (Song filter): PASS — `SongFormViewModelTests` 30/30 (18 pre-existing + 12 new), `SongServiceTests`
  26/26 (21 pre-existing + 5 new).
- Tests (full suite): PASS — **403/403**, 0 failures (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`).
  Baseline before this task was 386/386 (Person/Task 03) — the +17 delta is exactly the 12 new
  `SongFormViewModelTests` + 5 new `SongServiceTests` methods; no other test count changed.
- Net-new test count (exact, per coordinator request): **17** (12 `SongFormViewModelTests` + 5 `SongServiceTests`).
  Pre-existing test count in the two touched files before this task: 18 + 21 = 39. After: 30 + 26 = 56.
- Post-edit re-read: confirmed for all 7 changed files (`ISongService.cs`, `SongService.cs`,
  `SongFormViewModel.cs`, `SongFormPage.xaml`, `SongFormPage.xaml.cs`,
  `SongServiceTests.cs`, `SongFormViewModelTests.cs`).
- Spec compliance: confirmed against `dialogs-validation.md § Form Validation Standard` (blur/keystroke/Save
  timing, inline `HasError`/`ErrorText`, no native dialog, business logic in Service, edit-mode dirty-guard).
- BUG-020 regression check: confirmed the try-catch inside `SongFormViewModel.RefreshApiKeyFlagAsync()` is
  byte-for-byte unchanged; `RefreshApiKeyFlagAsync_SecureStorageThrows_DoesNotThrowAndSetsFalse` (pre-existing
  regression test) still passes.
- **E2E emulator (Helder gate — NOT run by agent):** REMAINING MANUAL VERIFICATION. On the emulator confirm,
  for **each** of Title/Version: (1) tabbing through an empty/pristine field shows no error (R8); (2) typing
  an invalid value (Title empty or >100 chars; Version >60 chars) then blurring shows the specific error
  inline under the field (R1/R9); (3) with the error showing, editing to a valid value clears it immediately
  without re-blurring (R2); (4) typing an invalid value first (before any error) shows nothing until blur
  (R3); (5) Save on an invalid field surfaces the inline error, never a dialog (R4/R6); (6) edit an existing
  Song and confirm no error flashes on page load; (7) trigger "Save as new version" with an empty Version and
  confirm the "required in this context" error still surfaces correctly with the new service-routed message.

### AC traceability

| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|--------------------------|-------------|
| R1 | Blur validates a dirty invalid title field | `SongFormViewModel.ValidateTitle` → `SongService.ValidateTitleInput` | `ValidateTitleCommand_DirtyInvalidField_SetsError` |
| R1 | Blur passes a dirty valid title field | `SongFormViewModel.ValidateTitle` | `ValidateTitleCommand_DirtyValidField_NoError` |
| R1 | Blur validates a dirty invalid version field | `SongFormViewModel.ValidateVersion` → `SongService.ValidateVersionInput` | `ValidateVersionCommand_DirtyInvalidField_SetsError` |
| R1 | Blur passes a dirty valid version field | `SongFormViewModel.ValidateVersion` | `ValidateVersionCommand_DirtyValidField_NoError` |
| R2 | Title keystroke clears error when valid | `SongFormViewModel.OnSongTitleChanged` | `OnSongTitleChanged_WhileInError_ClearsErrorWhenValid` |
| R2 | Version keystroke clears error when valid | `SongFormViewModel.OnSongVersionChanged` | `OnSongVersionChanged_WhileInError_ClearsErrorWhenValid` |
| R3 | No title keystroke validation before error | `SongFormViewModel.OnSongTitleChanged` | `OnSongTitleChanged_NotInError_DoesNotValidate` |
| R3 | No version keystroke validation before error | `SongFormViewModel.OnSongVersionChanged` | `OnSongVersionChanged_NotInError_DoesNotValidate` |
| R4 | Save re-validates title as safety net even if never dirtied | `SongFormViewModel.SaveAsync` | `SaveAsync_TitleNeverDirty_SafetyNetSetsTitleError` |
| R4 | "Save as new version" required-Version rule routed through the service | `SongFormViewModel.ConfirmSaveAsNewVersionAsync` → `SongService.ValidateVersionInput(isRequired: true)` | `ConfirmSaveAsNewVersion_EmptyVersion_SetsVersionError` |
| R8 | Pristine title field shows no blur error | `SongFormViewModel.ValidateTitle` (dirty guard) | `ValidateTitleCommand_PristineField_DoesNotSetError` |
| R8 | Pristine version field shows no blur error | `SongFormViewModel.ValidateVersion` (dirty guard) | `ValidateVersionCommand_PristineField_DoesNotSetError` |
| Edit-mode dirty-guard | Hydrating title field is not dirtied/validated on blur | `SongFormViewModel.OnSongTitleChanged` (`_isHydrating`) | `OnSongTitleChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur` |
| R6/R9 | Errors surfaced inline via `HasError`/`ErrorText` only | `SongFormPage.xaml` (`dxe:TextEdit` Title, `dxe:TextEdit` Version x2) | (XAML — E2E emulator gate) |
| R7 | Messages specific/actionable | `SongService.ValidateTitleInput` / `ValidateVersionInput` | `SongServiceTests.ValidateTitleInput_*` / `ValidateVersionInput_*` |

---

## Notes / deviations from the Reference Pattern

- **Version field appears twice in the XAML** (main form + inside the "Save as new version" resolution
  BottomSheet), both bound to the same `SongVersion` property. Both `dxe:TextEdit` instances got the same
  `Unfocused="OnVersionUnfocused"` handler — this is a deviation from the Person/Venue reference (single
  instance per field) driven by the Song form's existing two-surface UI, not a pattern change.
- **A pre-existing constitutional gap was closed as a byproduct.** The "Save as new version" required-Version
  check was hardcoded in the ViewModel before this task (business logic outside Services). Since this task
  was already touching that exact validation branch to wire it through the new `ValidateVersionInput` method,
  the gap was fixed in the same change rather than left in place — this was the minimal-diff option, not scope
  creep, since leaving the hardcoded check while adding a parallel service validator would have created two
  sources of truth for the same rule.
- **No component-change-governance trigger.** No shared/governed component (`AutocompleteField`, `ListItem`,
  etc.) was modified — only page-local `dxe:TextEdit` instances and the page's own ViewModel/code-behind.

---

# Artist Form Validation — Task Log (Task 05)

**Feature:** Apply the Form Validation Standard to `ArtistFormPage` / `ArtistFormViewModel`.
**Standard source:** `.claude/library/dialogs-validation.md § Form Validation Standard`
**Requirements source:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/01-ui-form-validation-guide.md`
**Reference implementation:** Person form (Task 03, multi-field reference), Venue form (Task 02, single-field baseline).
**Requirement ID scheme:** R1–R10 reused unchanged from Task 02/03/04 (see the table at the top of this file).

---

## Task: Artist form — Name field — blur + keystroke-clear inline validation

**Plan:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` (Task 05)
**Status:** To Review
**Started:** 07/02/2026
**Completed:** 07/02/2026

### Scope decisions / out of scope

- **Fields validated: Artist Name only** — the form's single free-text `dxe:TextEdit`. Confirmed by reading
  `ArtistFormPage.xaml`: the only other interactive elements are the "Search music database" picker `ListItem`
  (navigation, not an input), the duplicate-suggestion buttons (navigation), and Cancel/Save.
- **Validator already existed and was correctly named** (`IArtistService.ValidateNameInput`) with R7-compliant
  messages ("Artist name is required" / "Name is too long. Maximum 60 characters.") — no service validator
  changes were needed.
- **Async failure mapping — documented single-field mapping, not substring routing.** `ApplyAsyncFailure`
  routes every Create/Update failure message to the Name field inline. Rationale (documented in the method's
  XML doc): the form has exactly one editable field, and the only field-attributable failure the service can
  return after the local validator passes is the duplicate-name uniqueness check. This is the explicitly
  sanctioned single-field mapping from the Task 05 brief; no `message.Contains(...)` routing exists.
- **Picker (`ArtistPickedMessage`) sets `ArtistName` after hydration completes** — this intentionally marks
  the field dirty (it is a user-initiated selection, not programmatic pre-population), so a subsequent blur
  validates the picked value, and an in-error field is cleared immediately by the keystroke-path re-validation.
- **Counter threshold drift fixed (Task 04 review lesson).** `ArtistService.GetCharacterCounterInfo` flagged
  `isError` at `>= MaxInputLength` (60), but `ValidateNameInput` accepts exactly 60 — the counter showed an
  error state for a value the validator accepts. Changed to `> MaxInputLength` (TDD: seen Red at 60, Green
  after fix).
- **Counter counts the trimmed string (Task 04 review lesson).** `OnArtistNameChanged` now passes
  `value?.Trim().Length` to the counter helpers — the same string `ValidateNameInput` checks — so trailing
  whitespace cannot inflate the count into a false warning/error.
- **`MaxCharacterCount` drift fixed.** `ArtistFormPage.xaml` had `MaxCharacterCount="250"`; the service's
  `MaxInputLength` is 60. Set to 60.
- **Null-safe Shell navigation** (`Shell.Current?.GoToAsync(..) ?? Task.CompletedTask`) adopted in
  `SaveAsync`/`CancelAsync` to match the Person reference and allow the Save success-path unit test
  (`Shell.Current` is null under test). `NavigateToArtistPickerAsync`/`SelectDuplicateAsync` untouched
  (navigation-only commands, excluded from unit tests per `testing.md`).
- **No component-change-governance trigger.** No shared/governed component was modified.

### Changed files:

- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` — added `_nameDirty` dirty-guard, `_isHydrating` +
  `CompleteHydration()`, `ValidateNameCommand` (blur), keystroke re-validate-only-while-in-error in
  `OnArtistNameChanged`, `ApplyNameValidation`, Save safety net, `ApplyAsyncFailure` (documented single-field
  mapping), trimmed-length counter, null-safe Shell navigation; removed the old unconditional
  `ClearError()`-on-keystroke behavior (which wrongly cleared errors even while still invalid).
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` — `Unfocused="OnNameUnfocused"` blur hook on `nameEdit`;
  `MaxCharacterCount` 250 → 60 (service constant alignment).
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml.cs` — `CompleteHydration()` in `OnAppearing`,
  `OnNameUnfocused` bridge to `ValidateNameCommand`, focus name field only in create mode (Person-reference
  pattern; previously focused in edit mode too, which would have blurred/validated prematurely on back-nav).
- `Services/ArtistService.cs` — `GetCharacterCounterInfo` `isError` threshold `>=` → `>` (counter/validator
  alignment) with rationale comment.
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs` — NEW file, 11 `[Fact]` tests (blur x3,
  keystroke x3, hydration guard x1, Save safety net / create / duplicate x3, trimmed counter x1).
- `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs` — added 2 `GetCharacterCounterInfo` threshold tests.
- `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md` — this entry (existing file,
  already registered in `MyVocaList.sln`; no `.sln` change required).

### Verification evidence

- Build: PASS — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors. Test project builds
  as part of `dotnet test`.
- Tests (Artist filter): PASS — `ArtistFormViewModelTests` 11/11 (all new), `ArtistServiceTests` 15/15
  (13 pre-existing + 2 new).
- Tests (full suite): PASS — **416/416**, 0 failures (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`).
  Baseline before this task was 403/403 (Task 04) — the +13 delta is exactly the 11 new
  `ArtistFormViewModelTests` + 2 new `ArtistServiceTests` methods.
- Net-new test count (exact): **13** (11 ViewModel + 2 Service).
- TDD evidence: ViewModel tests seen Red first (CS1061 — `CompleteHydration`/`ValidateNameCommand` did not
  exist), then Green after implementation. Counter threshold test seen Red (1 failed at length 60), Green
  after the one-line service fix. Deviation note: the 4 Save/counter ViewModel tests were written after the
  ViewModel pattern was already implemented in the first Green increment, so they were first seen passing —
  the whole blur/dirty/hydration/Save pattern was implemented as one unit against the first Red batch
  (single-agent batching, transparent per `testing.md § One test at a time`). Two test arranges were corrected
  during Red→Green (setting `ArtistName = ""` when the default is already `""` never fires the CommunityToolkit
  property-changed, so the field never became dirty) — arrange-only fixes; no assertion was weakened.
- Post-edit re-read: confirmed for all 6 changed code files.
- Spec compliance: confirmed against `dialogs-validation.md § Form Validation Standard` (blur/keystroke/Save
  timing, inline `HasError`/`ErrorText`, no native dialog, business logic in Services, edit-mode dirty-guard,
  no substring routing, counter/validator alignment). `SafeAreaEdges="All"` retained on the page (form-page
  keyboard-avoidance pattern per `dialogs-validation.md`, same as Venue/Person/Song form pages).
- **E2E emulator (Helder gate — NOT run by agent):** REMAINING MANUAL VERIFICATION. On the emulator confirm:
  (1) tabbing through the pristine Name field shows no error (R8); (2) clearing the name then blurring shows
  "Artist name is required" inline (R1/R9); (3) with the error showing, typing a valid name clears it
  immediately without re-blurring (R2); (4) typing an invalid value before any error shows nothing until blur
  (R3); (5) Save with an empty name surfaces the inline error, never a dialog (R4/R6); (6) Save with a
  duplicate name shows "An artist with this name already exists" inline under the Name field; (7) edit an
  existing artist and confirm no error flashes on page load (hydration guard); (8) pick an artist from the
  music-database picker and confirm no spurious error appears; (9) type 60 chars — counter shows 60/60 without
  error color; input is capped at 60 by `MaxCharacterCount`.

### AC traceability

| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|--------------------------|-------------|
| R1 | Blur validates a dirty invalid name field | `ArtistFormViewModel.ValidateName` → `ArtistService.ValidateNameInput` | `ValidateNameCommand_DirtyInvalidField_SetsError` |
| R1 | Blur passes a dirty valid name field | `ArtistFormViewModel.ValidateName` | `ValidateNameCommand_DirtyValidField_NoError` |
| R2 | Keystroke clears error when valid | `ArtistFormViewModel.OnArtistNameChanged` | `OnArtistNameChanged_WhileInError_ClearsErrorWhenValid` |
| R2 | Keystroke keeps error while still invalid | `ArtistFormViewModel.OnArtistNameChanged` | `OnArtistNameChanged_WhileInError_StillInvalid_KeepsError` |
| R3 | No keystroke validation before error | `ArtistFormViewModel.OnArtistNameChanged` | `OnArtistNameChanged_NotInError_DoesNotValidate` |
| R4 | Save re-validates even a never-dirtied field and aborts (create never called) | `ArtistFormViewModel.SaveAsync` | `SaveCommand_UntouchedEmptyName_SetsErrorAndDoesNotCreate` |
| R4 | Save with a valid name calls create | `ArtistFormViewModel.SaveAsync` | `SaveCommand_ValidName_CallsCreate` |
| R4/R9 | Duplicate-name create failure surfaces inline on the Name field | `ArtistFormViewModel.ApplyAsyncFailure` | `SaveCommand_DuplicateName_SetsNameHasErrorInline` |
| R8 | Pristine field shows no blur error | `ArtistFormViewModel.ValidateName` (dirty guard) | `ValidateNameCommand_PristineField_DoesNotSetError` |
| Edit-mode dirty-guard | Hydration does not dirty/validate | `ArtistFormViewModel.OnArtistNameChanged` (`_isHydrating`) + `ArtistFormPage.OnAppearing` | `OnArtistNameChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur` |
| Counter alignment | Counter error only when validator rejects | `ArtistService.GetCharacterCounterInfo` | `GetCharacterCounterInfo_AtMaxLength60_IsNotError` / `GetCharacterCounterInfo_OverMaxLength_IsError` |
| Counter alignment | Counter counts the trimmed string the validator checks | `ArtistFormViewModel.OnArtistNameChanged` | `OnArtistNameChanged_TrailingWhitespace_CounterUsesTrimmedLength` |
| R6/R9 | Errors inline via `HasError`/`ErrorText`, never a dialog | `ArtistFormPage.xaml` (`dxe:TextEdit nameEdit`) | (XAML — E2E emulator gate) |
| R7 | Messages specific/actionable | `ArtistService.ValidateNameInput` (pre-existing) | `ArtistServiceTests.ValidateNameInput_*` (pre-existing) |

---
## Task: BUG-024 — SongForm edit-mode Save wipes FeaturedArtists + Lyrics and discards Version (Critical)
**Plan:** commit message as spec (Bug Fix Pattern) + `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-024-songform-edit-data-loss/BUG-024-songform-edit-data-loss.md`
**Status:** To Review
**Started:** 07/02/2026
**Completed:** 07/02/2026

Approved approach (Helder, 2026-07-02): add `GetSongByIdAsync` to `ISongService`/`SongService`,
fully hydrate `LoadSongForEditAsync` (FeaturedArtists, Lyrics, Version + external identity stash),
make `ExecuteEditSaveAsync` send the complete current form data including Version.
Repository check result: `ISongRepository.GetByIdAsync` already returns the full entity
(FeaturedArtists/Lyrics/Version are scalar columns) — no repository extension needed.

### Changed files:
- `Domain/ServicesInterfaces/ISongService.cs` — added `GetSongByIdAsync(int, CancellationToken)` with XML docs; added `string? version = null` param (null = keep existing) to `UpdateSongAsync` with full per-param docs
- `Services/SongService.cs` — implemented `GetSongByIdAsync` (`<inheritdoc />`, delegates to repository); `UpdateSongAsync` validates provided version (`ValidateVersionInput`) and persists `song.Version = version.Trim()` when non-null
- `Services/SongResolutionService.cs` — both `UpdateSongAsync` call sites pass `song.Version` (preserves prior keep-existing behavior under the new signature)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `LoadSongForEditAsync` hydrates Title/Version/FeaturedArtists/Lyrics + `SelectedExternalId`/`SelectedProvider` from `GetSongByIdAsync` inside a re-opened `_isHydrating` window (dirty-guard preserved even when the async load lands after `CompleteHydration()`); `IsArtistLocked` now uses the full BUG-008 rule (`ExternalId` set AND `!HasManualEdits`); `ExecuteEditSaveAsync` passes `version`
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` — 4 new tests (GetSongByIdAsync returns full entity; version persisted; null version keeps existing; version too long rejected)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — 3 new tests (hydration regression — the data-loss proof; edit-save sends hydrated fields + edited version; API-imported song locks artist field) + shared `MakeSongServiceWithSong` helper
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` — mechanical mock-plumbing update only: 6 `UpdateSongAsync` Setup/Verify arg lists gained an `It.IsAny<string>()` for the new `version` param (no assertion intent changed — `testing.md § Builder Must Not Modify Tests` respected)
- `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-024-songform-edit-data-loss/BUG-024-songform-edit-data-loss.md` — bug details file (NEW)
- `MyVocaList.sln` — registered the new bug folder/file (`{FA1234BC-0001-4000-8000-000000000035}` nested under artists-songs `bugs` `{7A021F6B-F297-41EA-A028-C4F881146791}`)
- `Docs/Management/BACKLOG.md` — BUG-024 row status 🟢 Ready → ✅ Fixed
- `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md` — this entry

### Verification evidence
- Build: PASS — `dotnet build MyVocaList.Tests` 0 errors; `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors
- Tests: PASS — full suite 435/435 (baseline 428/428 confirmed green before any change; +7 new). One intermittent failure observed in parallel full-suite runs (`SongRepositoryTests.InitializeAsync`, temp-DB `EnsureCreatedAsync` collision) both at baseline and mid-task — passes in isolation and on re-run; pre-existing flake, unrelated to this change.
- Post-edit re-read: confirmed (all changed sections re-read after final Green)
- Spec compliance: confirmed — approved BUG-024 approach followed exactly; no spec-described behavior changed outside the bug scope

### Red → Green evidence (regression tests, Critical class — mandatory)
| Test | Red (exact failure seen before fix) | Green |
|------|--------------------------------------|-------|
| `GetSongByIdAsync_ExistingId_ReturnsSongWithAllFields` | CS1061 compile-Red, then `Assert.NotNull() Failure: Value is null` against TDD stub | Pass |
| `LoadSongForEdit_ExistingSong_HydratesFeaturedArtistsLyricsAndVersion` | `Assert.Equal() Failure: Strings differ` (FeaturedArtists empty — entity never loaded) | Pass |
| `UpdateSongAsync_WithVersion_PersistsVersion` | CS1503 compile-Red, then `Expected: "Acoustic" / Actual: "Live"` against skeleton param | Pass |
| `SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion` | Moq: `Performed invocations: UpdateSongAsync(42, "Stored Title", "Feat A", "Stored lyrics", True, null, null, null, ct)` — version still null | Pass |
| `UpdateSongAsync_NullVersion_KeepsExistingVersion` / `UpdateSongAsync_VersionTooLong_ReturnsFalse` / `LoadSongForEdit_ApiImportedWithoutManualEdits_LocksArtistField` | Guard-rail/coverage tests for branches written within the preceding Green steps — not individually seen Red (documented deviation from one-test-at-a-time) | Pass |

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|--------------------------|-------------|
| BUG-024-1 | Edit hydration loads FeaturedArtists/Lyrics/Version | `SongFormViewModel.LoadSongForEditAsync` + `SongService.GetSongByIdAsync` | `LoadSongForEdit_ExistingSong_HydratesFeaturedArtistsLyricsAndVersion` |
| BUG-024-2 | Edit Save sends complete form data incl. Version | `SongFormViewModel.ExecuteEditSaveAsync` | `SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion` |
| BUG-024-3 | `UpdateSongAsync` persists provided Version | `SongService.UpdateSongAsync` | `UpdateSongAsync_WithVersion_PersistsVersion` |
| BUG-024-4 | Null Version keeps stored value (resolution paths unaffected) | `SongService.UpdateSongAsync` + `SongResolutionService` call sites | `UpdateSongAsync_NullVersion_KeepsExistingVersion` |
| BUG-024-5 | Version length rule enforced in Services layer | `SongService.UpdateSongAsync` → `ValidateVersionInput` | `UpdateSongAsync_VersionTooLong_ReturnsFalse` |

### Decisions taken (ambiguities)
- **Uniqueness check left title-only in `UpdateSongAsync`** (not switched to the 3-column
  `ExistsByTitleVersionForArtistAsync(..., excludeId)` overload): changing it would alter behavior
  encoded in the pre-existing test `UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse`,
  which is outside the approved scope and forbidden by Builder-must-not-modify-tests. Pre-existing
  consequence documented in the bug details file (§ Out of scope) — recommend a follow-up bug:
  a song sharing its title with a sibling version cannot currently be edit-saved.
- **`version` param semantics: null = keep existing** — mirrors the established
  `externalId`/`externalProvider` pattern in the same method, so `SongResolutionService` callers
  keep exact prior behavior.
- **Hydration guard**: `_isHydrating` is saved/re-opened/restored inside the `RunOnUiThread`
  block because the entity load is async and can complete after `CompleteHydration()` — without
  this, hydrated values would mark fields dirty and defeat the edit-mode dirty-guard.

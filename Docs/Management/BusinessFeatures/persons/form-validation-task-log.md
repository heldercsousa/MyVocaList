# Person (Singer) Form Validation — Task Log (Task 03)

**Feature:** Apply the Form Validation Standard to the Person/Singer form (multi-field reference).
**Standard source:** `.claude/library/dialogs-validation.md § Form Validation Standard`
**Requirements source:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/01-ui-form-validation-guide.md`
**Reference implementation:** `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md` (Task 02, Venue — merged).

---

## Task: Person form — name / birthday / email fields — blur + keystroke-clear inline validation

**Plan:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` (Task 03)
**Status:** To Review
**Started:** 07/01/2026
**Completed:** 07/01/2026

### Requirement ID scheme

Reused from the Venue reference (`venues/form-validation-task-log.md`) — same IDs, same source
(`01-ui-form-validation-guide.md` + `dialogs-validation.md § Form Validation Standard`).

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
| — | **Edit-mode dirty-guard** (Opus review note, Task 02): `[QueryProperty]` pre-population must not mark a field dirty, or a pre-filled invalid value flashes an error on first blur before the user touches anything. New for multi-field forms (03–05). |

### Scope decisions / out of scope

- **Birthday no-year entry mechanism is OUT OF SCOPE** (Helder gate, `dialogs-validation.md` "OPEN" note).
  The birthday field keeps its existing `DD/MM` masked `dxe:TextEdit` free-text entry; only the
  validation wiring (blur + keystroke-clear + dirty-guard + Save safety-net) was added on top of the
  existing `ValidateBirthday` service method, which was already correct and unchanged.
- **No validator added to `Services/PersonService.cs`.** `ValidateNameInput`, `ValidateBirthday`, and
  `ValidateEmail` already existed with full branch coverage (`PersonServiceTests`) and already return the
  standard `(bool isValid, string message)` tuple — nothing to add. Business logic remained entirely in
  the Service; the ViewModel only invokes it and maps the result to error state.
- **Async CRUD-failure routing (post-validation):** after all three field validators pass, the only
  field-attributable failure the service can still return is the email-uniqueness check
  (`"Email already registered to another singer."`). This is routed to `EmailHasError`/`EmailErrorText`
  via a single, narrow, well-known substring check — this is not the "guess which field" pattern being
  removed (that pattern routed *every* validation message across three fields by keyword sniffing). Any
  other async failure (e.g. `"Singer not found."` if the record was deleted concurrently — not
  attributable to any field) is surfaced via `ISnackbarComponent.ShowErrorAsync` as a non-blocking notice,
  since no field owns it.

### Changed files

- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` — added `_nameDirty`/`_birthdayDirty`/`_emailDirty`
  flags; added `_isHydrating` guard + public `CompleteHydration()`; added
  `[RelayCommand] ValidateName()` / `ValidateBirthday()` / `ValidateEmail()` (blur, dirty-guarded);
  rewrote `OnPersonNameChanged`/`OnPersonBirthdayChanged`/`OnPersonEmailChanged` to keystroke-revalidate
  only when already in error, and to skip dirty-marking during hydration; extracted
  `ApplyNameValidation`/`ApplyBirthdayValidation`/`ApplyEmailValidation`; rewrote `SaveAsync` to re-run
  ALL three validators (safety net) instead of returning at the first invalid field; removed the fragile
  `SetInlineError` substring router, replaced with `ApplyAsyncFailureAsync` (narrow, single-check email
  routing + snackbar fallback for non-field-attributable failures); removed the old unconditional
  `ClearNameError`/`ClearBirthdayError`/`ClearEmailError` helpers.
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml` — added `BlurredWithoutSelectionCommand="{Binding ValidateNameCommand}"`
  to the `AutocompleteField` name field (reused its existing blur-hook bindable property — the
  `AutocompleteField` component itself was NOT modified, so `component-change-governance.md` does not
  apply); added `x:Name`/`Unfocused="OnBirthdayUnfocused"` to the birthday `dxe:TextEdit`; added
  `x:Name`/`Unfocused="OnEmailUnfocused"` to the email `dxe:TextEdit`.
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` — added `_viewModel.CompleteHydration()` call in
  `OnAppearing` (after Shell has applied `[QueryProperty]` values, before the page is shown); added
  `OnBirthdayUnfocused`/`OnEmailUnfocused` handlers bridging blur events to the ViewModel commands.
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` — added 18 new Level-A tests (blur /
  keystroke / Save-safety-net / edit-mode hydration-guard, one group per field) on top of the 12
  pre-existing tests (30 total in this file).
- `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md` — this file (NEW).
- `MyVocaList.sln` — registered the new task-log doc under the existing `persons` solution folder
  (`{D01D4F5A-EA21-4BEA-9808-B8FD795E79C7}`).

`Services/PersonService.cs` was **unchanged** — see "Scope decisions" above.

### Verification evidence

- Build (`MyVocaList.Tests`, net10.0): PASS — 0 errors, 0 test-project warnings (pre-existing DevExpress
  trial-license / nullability warnings unrelated to this change persist, unchanged in count/kind).
- Build (`MyVocaList` MAUI head, net10.0-android): PASS — 0 errors (XAML + code-behind compile).
- Tests (Person filter): PASS — 30/30 `PersonFormViewModelTests` (12 pre-existing + 18 new), 0 failures.
- Tests (full suite): PASS — **386/386**, 0 failures (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`).
  Baseline before this task was 368/368 (Venue merge) — the +18 delta is exactly the new
  `PersonFormViewModelTests` methods; no other test count changed.
- Post-edit re-read: confirmed (VM, XAML, code-behind).
- Spec compliance: confirmed against `dialogs-validation.md § Form Validation Standard` (blur/keystroke/
  Save timing, inline `HasError`/`ErrorText`, no native dialog, business logic in Service, edit-mode
  dirty-guard).
- **E2E emulator (Helder gate — NOT run by agent):** REMAINING MANUAL VERIFICATION. On the emulator confirm,
  for **each** of name/birthday/email: (1) tabbing through an empty/pristine field shows no error (R8);
  (2) typing an invalid value then blurring shows the specific error inline under the field (R1/R9);
  (3) with the error showing, editing to a valid value clears it immediately without re-blurring (R2);
  (4) typing an invalid value first (before any error) shows nothing until blur (R3); (5) Save on an
  invalid field surfaces the inline error, never a dialog (R4/R6); (6) **edit an existing Singer whose
  birthday/email happens to be in a format no longer considered valid (if any legacy data exists) and
  confirm no error flashes on page load** — only after the user actually edits that field.

### AC traceability

| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|--------------------------|-------------|
| R1 | Blur validates a dirty invalid name field | `PersonFormViewModel.ValidateName` → `PersonService.ValidateNameInput` | `ValidateNameCommand_DirtyInvalidField_SetsError` |
| R1 | Blur passes a dirty valid name field | `PersonFormViewModel.ValidateName` | `ValidateNameCommand_DirtyValidField_NoError` |
| R1 | Blur validates a dirty invalid birthday field | `PersonFormViewModel.ValidateBirthday` → `PersonService.ValidateBirthday` | `ValidateBirthdayCommand_DirtyInvalidField_SetsError` |
| R1 | Blur validates a dirty invalid email field | `PersonFormViewModel.ValidateEmail` → `PersonService.ValidateEmail` | `ValidateEmailCommand_DirtyInvalidField_SetsError` |
| R2 | Name keystroke clears error when valid | `PersonFormViewModel.OnPersonNameChanged` | `OnPersonNameChanged_WhileInError_ClearsErrorWhenValid` |
| R2 | Name keystroke keeps/updates error while still invalid | `PersonFormViewModel.OnPersonNameChanged` | `OnPersonNameChanged_WhileInError_StillInvalid_KeepsError` |
| R2 | Birthday keystroke clears error when valid | `PersonFormViewModel.OnPersonBirthdayChanged` | `OnPersonBirthdayChanged_WhileInError_ClearsErrorWhenValid` |
| R2 | Email keystroke clears error when valid | `PersonFormViewModel.OnPersonEmailChanged` | `OnPersonEmailChanged_WhileInError_ClearsErrorWhenValid` |
| R3 | No name keystroke validation before error | `PersonFormViewModel.OnPersonNameChanged` | `OnPersonNameChanged_NotInError_DoesNotValidate` |
| R3 | No birthday keystroke validation before error | `PersonFormViewModel.OnPersonBirthdayChanged` | `OnPersonBirthdayChanged_NotInError_DoesNotValidate` |
| R3 | No email keystroke validation before error | `PersonFormViewModel.OnPersonEmailChanged` | `OnPersonEmailChanged_NotInError_DoesNotValidate` |
| R4 | Save re-runs all 3 validators as safety net | `PersonFormViewModel.SaveAsync` | `SaveCommand_MultipleInvalidFields_SetsHasErrorOnAllOfThem` |
| R8 | Pristine name field shows no blur error | `PersonFormViewModel.ValidateName` (dirty guard) | `ValidateNameCommand_PristineField_DoesNotSetError` |
| R8 | Pristine birthday field shows no blur error | `PersonFormViewModel.ValidateBirthday` (dirty guard) | `ValidateBirthdayCommand_PristineField_DoesNotSetError` |
| R8 | Pristine email field shows no blur error | `PersonFormViewModel.ValidateEmail` (dirty guard) | `ValidateEmailCommand_PristineField_DoesNotSetError` |
| Edit-mode dirty-guard | Hydrating name field is not dirtied/validated on blur | `PersonFormViewModel.OnPersonNameChanged` (`_isHydrating`) | `OnPersonNameChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur` |
| Edit-mode dirty-guard | Hydrating birthday field is not dirtied/validated on blur | `PersonFormViewModel.OnPersonBirthdayChanged` (`_isHydrating`) | `OnPersonBirthdayChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur` |
| Edit-mode dirty-guard | Hydrating email field is not dirtied/validated on blur | `PersonFormViewModel.OnPersonEmailChanged` (`_isHydrating`) | `OnPersonEmailChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur` |
| R6/R9 | Errors surfaced inline via `HasError`/`ErrorText` only | `PersonFormPage.xaml` (`AutocompleteField`, `dxe:TextEdit` x2) | (XAML — E2E emulator gate) |
| R7 | Messages specific/actionable | `PersonService.ValidateNameInput` / `ValidateBirthday` / `ValidateEmail` | `PersonServiceTests.Validate*` |

### E2E emulator gate — RESULT (Done 2026-07-01; RETESTED 2026-07-03)
TEST-005 (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md`) confirms the Name field behavior (R1–R4/R8) matches spec. Note: the TEST-005 8-step checklist (copied verbatim from the Venue single-field pattern) only fully applies to the Name field — Birthday/Email needed a subset. Four new findings from the 2026-07-03 retest, none yet fixed:
1. **UI glitch (Minor):** Edit-Singer load page shows a rendering issue in the full-name entry. Screenshot: `Docs/Management/BusinessFeatures/persons/bugs/edit-singer-load-page-issue.jpg`.
2. **Bug (Minor):** the birthday validation error message appears to expect the `/` separator inside the validated string, but the mask should only ever deliver the 4 digits (DDMM) to validation, not the separator. Likely a regex/mask mismatch in `PersonService.ValidateBirthday` vs the `Mask="00/00"` added by BUG-022. Screenshot: `Docs/Management/BusinessFeatures/persons/bugs/edit-singer-load-page-issue.jpg/singer-bithday-validation-error.jpg`.
3. **Bug (Major — UX consistency):** after Save in edit mode, the app does not navigate back to the prior page (Venues does navigate back after edit-save) — success snackbar shows correctly but the user is left on the form. Confirm whether back-navigation-after-save is the intended cross-CRUD pattern before fixing.
4. **Bug (Major — matches BUG-025 pattern):** the email-uniqueness inline error does not appear on blur as designed — it only appears after Save is tapped. This is distinct from already-registered BUG-025 (uniqueness error being cleared by weaker keystroke re-validation); this is the error never showing on blur in the first place.

These four are registered in `BACKLOG.md` (Person CRUD nested rows, BUG-035–BUG-038) pending dedicated fix sessions.

---

## Notes for Songs / Artists (Tasks 04–05)

- Same Reference Pattern as Venue (§ `venues/form-validation-task-log.md`), replicated per field.
- If a form's async CRUD failure can be attributed to more than one field via a single distinguishing
  substring (as here with "Email"), that is acceptable — it is not the forbidden pattern. The forbidden
  pattern is guessing *any* validation message onto *any* field by generic keyword sniffing across all
  fields (the old `SetInlineError`). If no substring reliably identifies the field, route the failure to
  the non-blocking snackbar channel instead of guessing.

---

## Task: BUG-036 — PersonFormPage birthday validation blocks Save (masked value)

**Plan:** commit-message-as-spec (Bug Fix Pattern) — no three-file spec.
**Status:** To Review
**Started:** 2026-07-12
**Completed:** 2026-07-12
**Severity:** Major (reclassified from Minor 2026-07-12 — Save is blocked entirely, no Person persists). Confirmed by Helder on-device (release build).

### Root cause
`PersonService.ValidateBirthday` validated against `^(\d{1,2})/(\d{1,2})$`, requiring a literal
"/". After BUG-022 added `Mask="00/00"` to the birthday `dxe:TextEdit`, the "/" is a display-only
mask literal and the delivered `Text` is 4 digits ("1503"). Every non-empty birthday therefore
failed validation and Save never persisted.

### Fix
Strip any "/" separator and validate the 4-digit "DDMM" form (`^(\d{2})(\d{2})$`); legacy "DD/MM"
records still validate (backward compatible). The mask and the storage path
(`BirthdayDayMonth = birthday.Trim()`) are left unchanged, per Helder's direction.

### Changed files
- `Services/PersonService.cs` — `ValidateBirthday` regex/normalization (strip "/" + 4-digit match).
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` — +2 tests (`ValidateBirthday_MaskedFourDigits_ReturnsValid`, `ValidateBirthday_MaskedFourDigitsInvalidMonth_ReturnsInvalid`).

### Verification evidence
- Build: PASS — Android head 0 errors; test project 0 errors.
- Tests: PASS — 10/10 `ValidateBirthday` tests (8 existing + 2 new); full suite 476/476. Both new tests seen Red before the fix (masked value returned invalid).
- Post-edit re-read: confirmed.

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| BUG-036 | Masked 4-digit birthday accepted | `PersonService.ValidateBirthday` | `ValidateBirthday_MaskedFourDigits_ReturnsValid` |
| BUG-036 | Range checks still apply to masked form | `PersonService.ValidateBirthday` | `ValidateBirthday_MaskedFourDigitsInvalidMonth_ReturnsInvalid` |

Commit: `1788320`. ⏳ Helder: on-device re-verify Save persists a birthday.

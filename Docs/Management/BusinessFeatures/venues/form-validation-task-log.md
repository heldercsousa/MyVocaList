# Venue Form Validation — Task Log (Reference Implementation)

**Feature:** Apply the Form Validation Standard to the Venue form (single-field reference).
**Standard source:** `.claude/library/dialogs-validation.md § Form Validation Standard`
**Requirements source:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/01-ui-form-validation-guide.md`

> This is the **reference implementation**. The Person/Singer, Songs, and Artists forms must
> replicate the pattern documented in the **Reference Pattern** section below.

---

## Task: Venue name field — blur + keystroke-clear inline validation

**Plan:** (bug-exempt / standard ceremony — single form, service already owns validation)
**Status:** To Review
**Started:** 07/01/2026
**Completed:** 07/01/2026

### Requirement ID scheme

The requirements doc (`01-ui-form-validation-guide.md`) is prose without explicit IDs. IDs R1–R10
below are **derived from its sections + the Form Validation Standard** and used for AC traceability.
(R5 and R10 already appear by these numbers inside `dialogs-validation.md`.)

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
| R10 | Integer rules — **spec-incomplete**, out of scope (guide ends with `<TODO>`). |

### Changed files

- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs` — added `_nameDirty` flag; added `[RelayCommand] ValidateName()`
  (blur, dirty-guarded); rewrote `OnVenueNameChanged` to keystroke-revalidate only when already in error;
  extracted `ApplyNameValidation`; removed the old unconditional `ClearError()`.
- `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml` — added `Unfocused="OnNameUnfocused"` to `nameEdit`.
- `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml.cs` — added typed `ViewModel` accessor and `OnNameUnfocused`
  handler bridging the blur event to `ValidateNameCommand`.
- `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelTests.cs` — NEW: 7 Level-A tests (blur/keystroke/save).
- `MyVocaList.sln` — registered the new task-log doc under the `venues` solution folder.

Service (`Services/VenueService.cs`) was **unchanged**: `ValidateNameInput` already returns the standard
`(bool isValid, string message)` tuple and already carried full branch coverage in `VenueServiceTests`.
Business logic stayed in the Service — the ViewModel only invokes it and maps the result to error state.

### Verification evidence

- Build (Services): PASS — 0 errors, 0 warnings.
- Build (MyVocaList.Tests, net10.0): PASS — 0 errors, 0 warnings.
- Build (MyVocaList MAUI head, net10.0-android): PASS — XAML + code-behind compile (see build log).
- Tests (Venue filter): PASS — 19/19 (12 `VenueServiceTests` + 7 `VenueFormViewModelTests`), 0 failures.
- Tests (full suite): PASS — 368/368, 0 failures (orchestrator re-ran `dotnet test` in the worktree on merge — 2026-07-01).
- Post-edit re-read: confirmed (VM, XAML, code-behind, test file).
- Spec compliance: confirmed against `dialogs-validation.md § Form Validation Standard` (blur/keystroke/Save timing,
  inline `HasError`/`ErrorText`, no native dialog, business logic in Service).
- **E2E emulator (Helder gate — NOT run by agent):** REMAINING MANUAL VERIFICATION. On the emulator confirm:
  (1) tabbing through an empty name field shows no error (R8); (2) typing 1 char then blurring shows the
  "too short" error inline under the field (R1/R9); (3) with the error showing, typing a 2nd valid char clears
  it immediately without re-blurring (R2); (4) typing an invalid char first (before any error) shows nothing
  until blur (R3); (5) Save on an empty field surfaces the inline error, never a dialog (R4/R6).

### AC traceability

| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|-------------------------|-------------|
| R1 | Blur validates a dirty invalid field | `VenueFormViewModel.ValidateName` → `VenueService.ValidateNameInput` | `ValidateNameCommand_DirtyInvalidField_SetsError` |
| R1 | Blur passes a dirty valid field | `VenueFormViewModel.ValidateName` | `ValidateNameCommand_DirtyValidField_NoError` |
| R2 | Keystroke clears error when value becomes valid | `VenueFormViewModel.OnVenueNameChanged` | `OnVenueNameChanged_WhileInError_ClearsErrorWhenValid` |
| R2 | Keystroke keeps/updates error while still invalid | `VenueFormViewModel.OnVenueNameChanged` | `OnVenueNameChanged_WhileInError_StillInvalid_KeepsError` |
| R3 | No keystroke validation before field is in error | `VenueFormViewModel.OnVenueNameChanged` | `OnVenueNameChanged_NotInError_DoesNotValidate` |
| R4 | Save re-runs validator as safety net | `VenueFormViewModel.SaveAsync` | `SaveCommand_InvalidName_SetsNameHasError` |
| R8 | Pristine tabbed-through field shows no error | `VenueFormViewModel.ValidateName` (dirty guard) | `ValidateNameCommand_PristineField_DoesNotSetError` |
| R6/R9 | Error surfaced inline via `HasError`/`ErrorText` only | `VenueFormPage.xaml` `dxe:TextEdit` | (XAML — E2E emulator gate) |
| R7 | Messages specific/actionable | `VenueService.ValidateNameInput` | `VenueServiceTests.ValidateNameInput_*` |

### E2E emulator gate — RESULT 2026-07-03
Helder ran TEST-004 (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md`). R1–R4/R6/R8/R9 all confirmed working as designed (blur-first, keystroke-clear, Save safety-net, inline-only errors). Two findings:
1. **Bug (Minor, unregistered as dedicated BUG-NNN yet — see BACKLOG.md Artists & Songs Catalog row "BUG-034"):** once the Name field reaches ~26 typed characters, the character counter renders **duplicated** (two overlapping counter labels). Screenshots: `Docs/Management/BusinessFeatures/venues/bugs/validation-error-26chars.jpg`, `Docs/Management/BusinessFeatures/venues/bugs/validation-error-31morechars.jpg`. Same symptom reproduced on ArtistFormPage (see `artists-songs/form-validation-task-log.md`) — likely a shared `dxe:TextEdit` counter configuration issue, not Venue-specific.
2. Step 8 (edit-mode dirty-guard on legacy over-30-char data) could not be exercised — no such record exists in the current DB. Abandoned for this pass; re-test if/when a pre-existing long-name Venue record is available.

---

## Reference Pattern — what Singer/Songs/Artists forms MUST replicate

**1. Service owns validation (business logic in Services).** One validator per field returning the
standard tuple; the ViewModel never contains a validation rule:

```csharp
(bool isValid, string message) ValidateNameInput(string name);   // e.g. ValidateEmail, ValidateBirthday
```

**2. ViewModel wiring — dirty flag + blur command + keystroke-clear.** Per field:

```csharp
private bool _nameDirty;   // set true once the user edits the field

// Blur (invoked from the page Unfocused event): validate only a dirty field
[RelayCommand]
private void ValidateName()
{
    if (!_nameDirty) return;                 // R8 — pristine field: no premature error
    ApplyNameValidation(Name);
}

// Keystroke: mark dirty; re-validate ONLY if already in error (R2 reward early / R3 no impatient teacher)
partial void OnNameChanged(string value)
{
    _nameDirty = true;
    if (!NameHasError) return;               // R3 — not in error yet: do nothing
    ApplyNameValidation(value);
}

private void ApplyNameValidation(string value)
{
    var (isValid, message) = _service.ValidateNameInput(value ?? string.Empty);
    NameHasError = !isValid;                 // R9 — field-addressed HasError
    NameErrorText = isValid ? string.Empty : message;
}
```

Save re-runs every field's validator before persisting (R4). Multi-field forms (Person/Songs/Artists)
repeat this block **once per field** — each field has its own `_<field>Dirty`, `Validate<Field>Command`,
`<Field>HasError`, `<Field>ErrorText`. Do **not** route one service message to a field by substring
matching (remove the Person form's `SetInlineError` substring approach).

**3. XAML — inline error binding + blur hook (no dialog, no summary):**

```xml
<dxe:TextEdit x:Name="nameEdit"
              Text="{Binding Name, Mode=TwoWay}"
              HasError="{Binding NameHasError}"
              ErrorText="{Binding NameErrorText}"
              Unfocused="OnNameUnfocused" />
```

**4. Page code-behind — bridge MAUI blur to the VM command:**

```csharp
private VenueFormViewModel ViewModel => (VenueFormViewModel)BindingContext;
private void OnNameUnfocused(object sender, FocusEventArgs e) => ViewModel.ValidateNameCommand.Execute(null);
```

**Notes for the multi-field forms:**
- Dates use `dxe:DateEdit` (picker validity) or masked `dxe:TextEdit` (`Mask="00/00/0000"`); masks are UI-only,
  never persisted. Person's **day/month-only birthday** is an OPEN emulator decision (see the standard) — do
  not implement it until Helder confirms.
- Integer validation (R10) is spec-incomplete — do not invent rules.

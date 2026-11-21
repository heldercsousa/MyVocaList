# Task log

---
## Task: T1 — Domain + Infra + Services (person-form-birthday-as-day-and-month-dropdowns)
**Plan:** Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/tasks.md
**Status:** Check build
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `Domain/Entity/Person.cs`
- `Domain/Entity/BirthdayRules.cs` (new)
- `Domain/ServicesInterfaces/IPersonService.cs`
- `Infra/EntityEFConfig/PersonConfiguration.cs`
- `Infra/Migrations/20261120232838_PersonBirthDayMonth.cs` (new)
- `Infra/Migrations/20261120232838_PersonBirthDayMonth.Designer.cs` (new, EF scaffold)
- `Infra/Migrations/AppDbContextModelSnapshot.cs`
- `Contracts/Models/PersonListItemDto.cs`
- `Services/PersonService.cs`
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (T1 bridge only, see deviations)
- `MyVocaList/UI/ViewModels/PersonsViewModel.cs` (T1 bridge only)
- `Tests/Unit/Domain/PersonBirthdayTests.cs` (new)
- `Tests/Unit/Services/PersonServiceTests.cs`
- `Tests/Unit/ViewModels/PersonFormViewModelTests.cs` (signature adaptation only)
- `Tests/Unit/ViewModels/PersonFormViewModelBug044Tests.cs` (fixture adaptation only)
- `Tests/Integration/Repositories/PersonRepositoryTests.cs`
- `Tests/Integration/Migrations/PersonBirthDayMonthMigrationTests.cs` (new)
- `Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` (signature adaptation only)
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/design.md` (spec update note)
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/tasks.md` (T1 ticked)
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/task-log.md`

### Build notes
Build: `dotnet build Tests/MyVocaList.Tests.csproj` 0 errors (142 warnings); `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors (3 warnings) | Tests: 1958 passed, 0 failed, 0 skipped (`dotnet test Tests/MyVocaList.Tests.csproj`) | Commit SHA: see git log (commit follows this entry)
Files written and re-read: Person.cs, PersonConfiguration.cs, IPersonService.cs, PersonService.cs, PersonFormViewModel.cs (bridge), PersonsViewModel.cs (bridge), migration Up(), design.md note.

### Verification evidence
- Migration test suite (5 tests) passes against real SQLite: DD/MM and DDMM convert, malformed and invalid pairs (ab/cd, 45/13, 30/02, 31/04) become NULL, 29/02 kept, old column and IX_Persons_Name_Birthday dropped, CHECK rejects day without month.
- Repository CHECK tests (day-only, month-only) fail before the expression fix and pass after it (red then green).
- Full suite green after the final code change.

### AC traceability
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-BDAY-05 | Both null valid | Services/PersonService.cs ValidateBirthday | PersonServiceTests.ValidateBirthday_BothNull_ReturnsValid |
| REQ-BDAY-05 | Exactly one null → "Select both day and month" | PersonService.cs ValidateBirthday | ValidateBirthday_OnlyDay_ReturnsSelectBothMessage, ValidateBirthday_OnlyMonth_ReturnsSelectBothMessage |
| REQ-BDAY-05 | Month outside 1–12 → "Month must be between 1 and 12" | PersonService.cs | ValidateBirthday_MonthOutOfRange_ReturnsMonthMessage (0, 13) |
| REQ-BDAY-05 | Day outside 1–31 / invalid for month → "Invalid date for this month" | PersonService.cs + BirthdayRules.DaysInMonth | ValidateBirthday_DayOutOfRange_ReturnsInvalidDateMessage, ValidateBirthday_31Apr_…, ValidateBirthday_30Feb_… |
| REQ-BDAY-05 | 29 Feb valid | PersonService.cs | ValidateBirthday_29Feb_ReturnsValid |
| REQ-BDAY-05 | Create/Update reject one-sided birthday, nothing saved | PersonService.cs | CreatePersonAsync_MonthWithoutDay_…, UpdatePersonAsync_DayWithoutMonth_… |
| REQ-BDAY-07 | Entity stores BirthDay/BirthMonth (int?) | Domain/Entity/Person.cs; Infra PersonConfiguration.cs | CreatePersonAsync_ValidBirthday_StoresDayAndMonth, UpdatePersonAsync_ValidBirthday_AssignsDayAndMonth, SaveChanges_ValidBirthday_RoundTripsDayAndMonth |
| REQ-BDAY-07 | CHECK rejects one-sided and out-of-range | PersonConfiguration.cs, migration Up step 5 | SaveChanges_DayWithoutMonth_ThrowsDbUpdateException, SaveChanges_MonthWithoutDay_ThrowsDbUpdateException, Migration_CheckConstraint_RejectsDayWithoutMonth |
| REQ-BDAY-07 | Old column and index removed | migration Up step 4 | Migration_DropsOldColumnAndIndex |
| REQ-BDAY-08 | DD/MM and DDMM converted | migration Up step 2 | Migration_SlashAndCompactFormats_ConvertToDayAndMonth |
| REQ-BDAY-08 | Malformed / invalid pair → NULL | migration Up step 3 | Migration_MalformedAndInvalidValues_BecomeNull |
| REQ-BDAY-08 | 29/02 survives conversion | migration Up step 3 | Migration_Feb29_IsKept |
| REQ-BDAY-10 | "(15 Mar)" display, InvariantCulture, priority unchanged | Domain/Entity/Person.cs GetDisplayIdentifier | PersonBirthdayTests.GetDisplayIdentifier_BirthdayWithoutEmail_RendersDayAndAbbreviatedMonth, …_SingleDigitDay_IsNotPadded, …_EmailAndBirthday_EmailWins, …_NoEmailNoBirthday_FallsBackToId |
| REQ-BDAY-02 (domain half) | Days per month, Feb = 29 | Domain/Entity/BirthdayRules.cs | PersonBirthdayTests.DaysInMonth_EachMonth_ReturnsCalendarMaximum (Theory, 12 cases) |

REQ-BDAY-01/02/03/04/06/09 are UI and belong to T2/T3. Not covered in T1.

### Deviations and spec gaps
- **Spec updated (design.md, Infra):** the CHECK expression in design.md was `(BirthDay IS NULL AND BirthMonth IS NULL) OR (BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12)`. SQLite treats a NULL CHECK result as pass, so a day without a month was accepted, and the repository tests caught it. The expression now adds `BirthDay IS NOT NULL AND BirthMonth IS NOT NULL` to the second branch. Applied to PersonConfiguration, the migration Up, the migration Designer and the model snapshot. Blocking: No.
- **Path mismatch in design.md "All consumers":** the repository is `Infra/Repository/PersonRepository.cs`, not `Infra/Repositories/`. It has no birthday references, so no change was needed. The "about 12 hits" were in `PersonRepositoryTests.cs`. Blocking: No.
- **Design "All consumers" list:** `PersistedStringTrimmingTests` has no birthday references, so it was not changed.
- **Update signature:** design.md shows `UpdatePersonAsync(int id, string fullName, int? birthDay, int? birthMonth, string email, ct)` with no defaults. Implemented as written, so existing callers pass nulls explicitly.
- **T1 bridge in ViewModels (temporary, removed by T2):** PersonFormViewModel keeps the `PersonBirthday` "DDMM" string. `ParseBirthdayBridge` converts it to day and month before the service call. The navigation URLs still use `personBirthday` with a "DDMM" value, built from the int pair. The inline message "Use DD/MM format (e.g.: 15/03)" is kept for malformed text only, since T2 replaces the field. XAML was not touched.
- **Migration Down:** restores the old column and index shape but cannot restore the converted text data. Data-loss reversal is accepted for a dev-stage migration; flagged for Helder.
- **Migration tool:** `dotnet ef` is the global 9.0.6 tool against the EF Core 10.0.0 runtime. The tool warned about the version gap. The scaffold was hand-reordered as the design requires.
- **E2E emulator gate:** not run in T1. The change alters ViewModel save and navigation paths, so the on-device check is due in T2/T3 (REQ-BDAY-01/04 gate). Status is set to `Check build` per the protocol, pending that verification.

### Design concern
- The design's Create signature puts `email` after two optional ints. Positional calls that passed email as the third argument now fail to compile, so every call site was checked. No concern beyond that.

---
## Task: T2 — UI (person-form-birthday-as-day-and-month-dropdowns)
**Plan:** Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/tasks.md
**Status:** Check build
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `MyVocaList/UI/Models/MonthOption.cs` (new)
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- `MyVocaList/UI/ViewModels/PersonsViewModel.cs`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`
- `Tests/Unit/ViewModels/PersonFormViewModelTests.cs`
- `Tests/Unit/ViewModels/PersonFormViewModelBug044Tests.cs`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/design.md` (spec update note)
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/tasks.md` (T2 ticked)
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/task-log.md`

### Build notes
Build: `dotnet build Tests/MyVocaList.Tests.csproj` 0 errors (146 warnings); `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors (3 warnings) | Tests: 1971 passed, 0 failed, 0 skipped (`dotnet test Tests/MyVocaList.Tests.csproj`; focused PersonForm/PersonsViewModel filter 59 passed) | Commit SHA: see git log (commit follows this entry)
Files written and re-read: PersonFormViewModel.cs (birthday section, query setters, Save, suggestion URL), PersonsViewModel.cs (NavigateToEditAsync, using), PersonFormPage.xaml (Grid of two ComboBoxEdit), PersonFormPage.xaml.cs (OnBirthdayUnfocused removed), MonthOption.cs, design.md (spec note), tasks.md (T2 ticked).

### E2E
E2E: emulator not available — requires manual verification. `adb devices` lists no device and no AVD is installed (`/c/Android` holds platform-tools only). No clean Android build or deploy was run, and no screenshot was taken. Manual checks for the on-device gate (T3): New Singer shows two dropdowns; February offers 29 days; choosing April after 31 clears the day; each clear icon empties its dropdown; Save with one side only shows the inline error on the empty dropdown; edit form pre-selects the stored day and month.

### Verification evidence
- Full suite green: 1971 passed, 0 failed.
- Focused run (PersonForm and PersonsViewModel filter): 59 passed.
- The XAML compiles at DevExpress.Maui.Editors 25.2.4, so `ClearIconVisibility`, `DisplayMember`, `SelectedItem` and `HasError`/`ErrorText` on `dxe:ComboBoxEdit` resolved at build time. Context7 (current docs) confirms `ClearIconVisibility` is an `IconVisibility` on `EditBase` (Never/Always/Auto).
- TDD order: the new tests were written in one pass with the ViewModel change, not run against the old ViewModel first, so a separate red run was not captured. Flagged below.

### AC traceability
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-BDAY-01 | Two dropdowns, Birth day then Birth month, English names, no free-text field | PersonFormPage.xaml (Grid, two ComboBoxEdit); PersonFormViewModel.Months | Months_HoldsEnglishNamesJanuaryToDecember (list half); on-device gate T3 (render) |
| REQ-BDAY-02 | Day list per month, 1..31 when no month, Feb 29 | PersonFormViewModel.RebuildDays; BirthdayRules.DaysInMonth | SelectedBirthMonth_Change_RebuildsDaysForThatMonth (Theory 1/2/4/9/12), Days_NoMonthSelected_Holds1To31 |
| REQ-BDAY-03 | Day cleared when invalid for new month, kept when valid | PersonFormViewModel.RebuildDays (clamp), OnSelectedBirthMonthChanged | SelectedBirthMonth_DayInvalidForNewMonth_ClearsDay (31 then April), SelectedBirthMonth_DayValidForNewMonth_KeepsDay |
| REQ-BDAY-04 | Each dropdown clearable | PersonFormPage.xaml ClearIconVisibility="Auto" | UI-only: on-device gate T3 |
| REQ-BDAY-06 | Save with one side only: inline error on empty dropdown, no save | PersonFormViewModel.ApplyBirthdayValidation (ErrorOnDay/ErrorOnMonth), SaveAsync | SaveCommand_DayWithoutMonth_ShowsErrorOnMonthAndDoesNotSave, SaveCommand_MonthWithoutDay_ShowsErrorOnDayAndDoesNotSave |
| REQ-BDAY-06 | Error re-validates on selection while in error; not otherwise | OnSelectedBirthDayChanged / OnSelectedBirthMonthChanged | BirthdayDropdown_WhileInError_ClearsErrorWhenValid, BirthdayDropdown_NotInError_DoesNotValidate |
| REQ-BDAY-09 | Edit pre-selects stored day and month (query params) | PersonFormViewModel.PersonBirthDayRaw / PersonBirthMonthRaw | QueryParams_Hydration_PreSelectsDayAndMonth, QueryParams_EmptyValues_LeaveDropdownsEmpty, QueryParams_Hydration_DoesNotValidate |
| REQ-BDAY-09 | Save passes day and month as ints to the service | PersonFormViewModel.SaveAsync | SaveCommand_CreateMode_PassesDayAndMonthToService |
| REQ-BDAY-09 | Suggestion pick and list edit navigate with personBirthDay / personBirthMonth | PersonFormViewModel.SuggestionSelectedAsync; PersonsViewModel.NavigateToEditAsync | SuggestionSelected_CarriesBirthDayAndMonthAsQueryParams (suggestion path). NavigateToEditAsync: no test, see gaps |

### Deviations and spec gaps
- **Spec updated (design.md, UI):** the birthday field has no blur step, so `ValidateBirthdayCommand`, the birthday dirty flag and their blur-first tests were removed. Validation runs on Save and on selection while in error. The error placement rule is recorded there. Blocking: No.
- **Removed tests (superseded):** the birthday blur tests `ValidateBirthdayCommand_PristineField_DoesNotSetError`, `ValidateBirthdayCommand_DirtyInvalidField_SetsError`, `OnPersonBirthdayChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur`, `OnPersonBirthdayChanged_NotInError_DoesNotValidate` and `OnPersonBirthdayChanged_WhileInError_ClearsErrorWhenValid` encoded the text-field blur behaviour. They were replaced by the dropdown tests above. The email blur tests are unchanged.
- **Hydration guard for birthday:** the `_isHydrating` check is not used for the birthday. Query setters never validate, so there is no dirty state to guard. Name and email keep the pattern.
- **`_isRebuildingDays` guard (implementation decision):** the Birth day ComboBoxEdit can clear its selection when `ItemsSource` is cleared. The guard keeps that clear from being read as a user selection, and the kept day is restored afterwards.
- **TDD red not captured:** tests were written and the ViewModel changed before the first test run. The first run was green. No separate red run exists for these tests. Flagged for Helder.
- **Gap, untested:** `PersonsViewModel.NavigateToEditAsync` uses `Shell.Current` directly, which is null in tests (testing.md anti-pattern), so its URL is not unit tested. It was reviewed by reading. The edit path is covered on device in T3.
- **E2E:** emulator not available. Status is Check build (see E2E above).
- **Bridge removed:** the T1 DDMM bridge (`ParseBirthdayBridge`, `personBirthday`, the "Use DD/MM format" message) is gone from the ViewModels and the page.

### Design concern
- None.

### Bug fix — birthday dropdowns do not open (bday-dropdown-open)
**Plan:** fix commit on task/bday-dropdown-open (Bug Fix Pattern, `workflow.md § Bug Fix Pattern`); Item: person-form-birthday-as-day-and-month-dropdowns
**Status:** To Review
**Started:** 2026-11-20
**Completed:** 2026-11-20

**Symptom:** On the New Singer form, tapping Birth day or Birth month focused the field but the drop-down never opened (also on the arrow icon). No exception in logcat.

**Root cause:** Both birthday `dxe:ComboBoxEdit` controls in `PersonFormPage.xaml` carried `IsReadOnly="True"`. DevExpress `EditBase.IsReadOnly` means "users can change a value in the editor" (Context7 /websites/devexpress_maui); the read-only state suppresses the drop-down. Text entry is already blocked by default on ComboBoxEdit because `IsFilterEnabled` defaults to false, so the attribute was redundant as well as harmful.

**Fix:** Removed `IsReadOnly="True"` from both birthday ComboBoxEdits. No other changes.

**Regression risk:** Low — the change only restores the default ComboBoxEdit behaviour; the keyboard stayed hidden in E2E, so no text entry path was opened.

### Changed files:
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/task-log.md`

### Verification evidence
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run` after clean of `obj/bin/Debug/net10.0-android` in the worktree: 0 errors, 10 pre-existing warnings.
- Emulator (emulator-5554, app not uninstalled, no singer saved, form left via Cancel):
  - Birth month opens with January..December: `scratchpad/e2e-fix-03-month-open.png`
  - Keyboard does not appear on dropdown taps: `e2e-fix-03-month-open.png`, `e2e-fix-04-april-day-open.png`
  - April selected, day list opens 1..30 (scrolled to end): `e2e-fix-04-april-day-open.png`, `e2e-fix-05-april-day-scrolled.png`
  - Day 30 kept with April: `e2e-fix-06-day30-selected.png`
  - Switch month to February clears day: `e2e-fix-07-february-selected.png`
  - February day list ends at 29: `e2e-fix-08-february-days.png`
  - Day 29 selected: `e2e-fix-09-day29-selected.png`
  - Month clear icon empties month: `e2e-fix-10-month-cleared.png`
  - Day clear icon empties day: `e2e-fix-11-day-cleared.png`
  - Screenshots live in `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\`
- Tests: `dotnet test Tests/MyVocaList.Tests.csproj` — see build notes below.

### Deviations and spec gaps
- Arrow-icon tap was not separately exercised; the field tap was verified (the arrow is part of the same control).
- FeedbackPage.xaml (Category ComboBoxEdit, line ~32) also has `IsReadOnly="True"`. Same mechanism is expected to block its drop-down, but it was NOT verified on the emulator and NOT changed (separate item).

---
## Task: Fix A — birthday ComboBox selected value unreadable (E2E finding, 2026-11-21)
**Plan:** orchestrator emulator E2E finding on PersonFormPage (no plan file)
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Changed files:
- `MyVocaList/Resources/Styles/MaterialStyles.xaml`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/task-log.md`

### Root cause
`MaterialStyles.xaml` has implicit styles for `dx:TextEdit` and `dx:AutoCompleteEdit` only. The two birthday `dxe:ComboBoxEdit` controls had no style, so they used DevExpress defaults: the selected value rendered in near-invisible dark text on the dark page background, and the box looked different from the other form fields.

### Fix
Added an implicit `dx:ComboBoxEdit` style directly after the `dx:AutoCompleteEdit` style, with the same five setters: `BoxMode` Outlined, `FocusedBorderColor` Primary, `BorderColor` Outline, `BackgroundColor` SurfaceContainerHighest, `TextColor` OnSurface. The `dx:` xmlns already covers `ComboBoxEdit` (the `dxe` clr-namespace is `DevExpress.Maui.Editors`, the same namespace as TextEdit). The drop-down list colors are unchanged. The style is not needed for readability of the list itself (the list is light with dark text on device).

### API check
`ComboBoxEdit` lives in `DevExpress.Maui.Editors` and inherits `EditBase.BoxMode`, `BorderColor`, `FocusedBorderColor`, `BackgroundColor` and `TextColor` (Context7 `/websites/devexpress_maui`). The Context7 pages show NuGet 26.1.5 while the project pins 25.2.4; the properties used are the same ones the existing TextEdit and AutoCompleteEdit styles already set, so no version-specific API is involved.

### Effect on FeedbackPage (only other consumer)
`FeedbackPage.xaml` Category `dxe:ComboBoxEdit` sets no colors of its own, so it now uses the same Outlined box as the form fields. `IsReadOnly="True"` was not touched (separate item).

### Verification
- Build: `dotnet build -f net10.0-android` after deleting `obj/Debug/net10.0-android` and `bin/Debug/net10.0-android`: 0 errors, 10 pre-existing warnings.
- Emulator (emulator-5554, clean build deployed): New Singer, Birth day set to 1 and Birth month to January. Both selected values render in light readable text on the SurfaceContainerHighest box with the Primary focus border, matching the Full Name field. Screenshot: `C:/Users/helde/AppData/Local/Temp/claude/78c429b059350e8f/15c50708-c550-4362-8273-50a7a757e7cf/scratchpad/e2e-fixAB-07-day-month-set.png.png`.
- The form was cancelled without saving. Singers still lists only Helder Sousa.
- Unit tests (`dotnet test Tests/MyVocaList.Tests.csproj`): 2006 passed, 1 failed, 2007 total. The failure is `SongFormViewModelFrozenArtistTests.EditMode_LocalOnlySongAfterLoad_ArtistBecomesEditableAndNotifies` (SongFormViewModel, line 348). This branch changes no .cs or test file (XAML and docs only). Not verified on develop, flagged for Helder.

---
## Task: Order swap — Birth month before Birth day (Helder, 2026-11-21)
**Plan:** Helder direct request (no plan file)
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Changed files:
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/requirements.md`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/design.md`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/task-log.md`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`

### Spec changes
- REQ-BDAY-01: order is now `Birth month` then `Birth day`, with the dated note "Order changed 2026-11-21 (Helder)".
- Non-goals: added the February-29 line (no birth year stored, so no leap-year computation; a year field would be a separate item).
- design.md UI bullet: column order recorded (month Grid.Column 0, day Grid.Column 1), with a dated spec note.

### Implementation
- `PersonFormPage.xaml`: only the two `Grid.Column` values were swapped (Birth day 0 to 1, Birth month 1 to 0). The element order in the XAML is unchanged (day element is still declared first), so the keyboard/screen-reader focus order remains day then month. Flagged for Helder: if focus order should follow the visual order, the two blocks need reordering in a follow-up.
- VM error placement checked (`PersonFormViewModel.cs` lines 279-281): error on Birth month when only the day is set, otherwise on Birth day. No change needed; no test change.
- Day list rebuild on month change and the clear-invalid-day rule are unchanged.

### Verification
- Clean `obj/Debug/net10.0-android` and `bin/Debug/net10.0-android` deleted before the build.
- Build `MyVocaList -f net10.0-android`: 0 errors, 10 warnings (pre-existing).
- Tests `dotnet test Tests/MyVocaList.Tests.csproj`: 2007 passed, 0 failed, 0 skipped. (The `SongFormViewModelFrozenArtistTests.EditMode_LocalOnlySongAfterLoad_ArtistBecomesEditableAndNotifies` failure noted in the previous entry did not recur in this run.)
- Emulator (emulator-5554), deployed with `dotnet build -t:Run -f net10.0-android -p:AndroidDeployTarget=Device` (the extra property was added by mistake; it only selects the attached emulator). Build and deploy: 0 errors. App not uninstalled.
  - New Singer form: Birth month on the left, Birth day on the right. Screenshot: `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\e2e-order-state.png`
  - Month list opened; April selected. Screenshot: `...\scratchpad\e2e-order-month-open.png`
  - Day list after April, scrolled to its end: last item is 30. Screenshot: `...\scratchpad\e2e-order-day-end.png`
  - Cancelled; nothing saved. Singers list still shows only Helder Sousa.
- Side effect during navigation: two early coordinate taps (the drawer tap missed) opened the Edit Venue form for an existing venue and focused its Address line 2 field. I left it with Navigate up, without Save. The Venues list is unchanged (same entries, no new venue).

Item: person-form-birthday-as-day-and-month-dropdowns
- 2026-11-20 fix (focus order): Birth day was declared before Birth month, so keyboard and screen-reader order was day then month; reordered the two ComboBoxEdit blocks so Birth month comes first. Grid.Column values unchanged, no other change.
- Verification: build MyVocaList net10.0-android 0 errors; dotnet test Tests/MyVocaList.Tests.csproj 2007 passed, 0 failed. No emulator run (declaration order only; visuals unchanged).
- Closes the focus-order follow-up flagged in the Implementation section above (focus order now matches visual order).

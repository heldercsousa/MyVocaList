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

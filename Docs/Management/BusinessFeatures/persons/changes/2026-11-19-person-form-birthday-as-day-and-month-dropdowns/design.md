# Person birthday as day and month dropdowns: Design

Requirements: `requirements.md` (REQ-BDAY-01..10). Layers follow the DRY Onion: Domain → Infra → Services → UI.

## Domain: `Domain/Entity/Person.cs`
- Remove `string? BirthdayDayMonth`. Add `int? BirthDay` and `int? BirthMonth`.
- `GetDisplayIdentifier()`: the birthday branch renders `$"({BirthDay} {abbr})"`, where `abbr` is the English three-letter month (`Jan`..`Dec`). Use a fixed array or `CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames` (InvariantCulture gives English). Never use the current culture. REQ-BDAY-10.

## Infra: `PersonConfiguration.cs` + migration
- `BirthDay` and `BirthMonth`: INTEGER, `IsRequired(false)`.
- `builder.ToTable(t => t.HasCheckConstraint("CK_Persons_Birthday", "(BirthDay IS NULL AND BirthMonth IS NULL) OR (BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12)"))`. The table is **`People`** (`AppDbContext.People`, `InitialCreate`). REQ-BDAY-07.
> **Spec updated [2026-11-20]:** the CHECK expression as first written, `(BirthDay IS NULL AND BirthMonth IS NULL) OR (BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12)`, passes when only one column is NULL. SQLite treats a NULL CHECK result as a pass, so a day without a month was accepted. The expression now adds `BirthDay IS NOT NULL AND BirthMonth IS NOT NULL` to the second branch. REQ-BDAY-07 (both null, or month 1 to 12 and day 1 to 31) is the authority. <!-- impl decision: SQLite NULL-in-CHECK pass; caught by REQ-BDAY-07 repository test -->
- The old `IX_Persons_Name_Birthday` index (FullName + BirthdayDayMonth) has nothing left to point at, so it is dropped. BUG-088 adds the identity index that replaces it.
- Migration `PersonBirthDayMonth` (`dotnet ef migrations add PersonBirthDayMonth -p Infra -s Infra`). If EF scaffolds the steps in the wrong order, edit the scaffold so they run in this order:
  1. Add the two INTEGER columns.
  2. Convert the data with `migrationBuilder.Sql`: `UPDATE People SET BirthDay = CAST(substr(replace(BirthdayDayMonth,'/',''),1,2) AS INTEGER), BirthMonth = CAST(substr(replace(BirthdayDayMonth,'/',''),3,2) AS INTEGER) WHERE length(replace(BirthdayDayMonth,'/','')) = 4 AND replace(BirthdayDayMonth,'/','') GLOB '[0-9][0-9][0-9][0-9]'`.
  3. Null out both values of any converted pair that is invalid: month not 1 to 12, day not 1 to 31, `BirthMonth = 2 AND BirthDay > 29`, or `BirthMonth IN (4,6,9,11) AND BirthDay > 30`. This matches `ValidateBirthday`, so an edit never dead-ends.
  4. Drop the index, then the old column.
  5. Add the CHECK. On SQLite this rebuilds the table.

  REQ-BDAY-08.

## Services: `IPersonService` / `PersonService`
- `ValidateBirthday(int? day, int? month)` replaces `ValidateBirthday(string)`. It applies the rules of REQ-BDAY-05, and the messages are exactly as written there. February allows 29.
- The day-count rule lives in Domain: a static `BirthdayRules.DaysInMonth(int month)` (Feb = 29) in `Domain/` beside `Person`. `ValidateBirthday` uses it, and the ViewModel calls it to build the day list. It is a pure function, so it needs no mock.
- `CreatePersonAsync(string fullName, int? birthDay = null, int? birthMonth = null, string email = null, ct)` and `UpdatePersonAsync(int id, string fullName, int? birthDay, int? birthMonth, string email, ct)` assign the two ints. The DTO maps `BirthDay` and `BirthMonth` (`PersonListItemDto`: replace `BirthdayDayMonth` with the two ints).

## UI: `PersonFormViewModel` + `PersonFormPage.xaml`
- QueryProperties: `personBirthDay` and `personBirthMonth` replace `personBirthday`. Update every navigation that builds the URL: `PersonsViewModel.NavigateToEditAsync` and `PersonFormViewModel.SuggestionSelectedAsync`. REQ-BDAY-09.
- `Months`: a fixed list of `MonthOption(int Number, string Name)` with English full names (January..December).
- `Days`: an `ObservableCollection<int>` (or `ObservableRangeCollection`), rebuilt from `BirthdayRules.DaysInMonth` whenever `SelectedBirthMonth` changes. It holds 1 to 31 when the month is null. After a rebuild, if `SelectedBirthDay` is greater than the new maximum, set it to null. REQ-BDAY-02/03.
- `SelectedBirthDay` (`int?`) and `SelectedBirthMonth` (`MonthOption?`). Hydration from the query params resolves the `MonthOption` by number.
- Validation reuses the existing dirty/hydrating pattern: `BirthdayHasError` and `BirthdayErrorText` are shown on the empty dropdown, or on the Day dropdown when both have values. Validation runs on Save and on each selection change while the field is in error. REQ-BDAY-06.
- XAML: replace the `dxe:TextEdit birthdayEdit` (and its `Unfocused` handler in code-behind) with a two-column `Grid` containing two `dxe:ComboBoxEdit` controls. Both have `IsReadOnly="True"` (pick only, no typing), as in `FeedbackPage.xaml`, and `ClearIconVisibility="Auto"`. `ClearIconVisibility` is an `IconVisibility` property of the editor base class (Never/Always/Auto). Context7 documents it at 26.1.5, but the project uses 25.2.4; a green build confirms it exists at 25.2.4. If it is missing at 25.2.4, stop and report. Month binds with `DisplayMember="Name"`. Order, left to right: `Birth month` (Grid.Column 0), then `Birth day` (Grid.Column 1). The month comes first so the day list is already scoped to the month. REQ-BDAY-01/04.
> **Spec updated [2026-11-21]:** Birth month is left, Birth day is right (Helder). Column order is swapped; no other XAML change.
- Edit ONE XAML file, build, then move to the next file (incremental-edit rule).

> **Spec updated [2026-11-20]:** the birthday has no blur command. A ComboBoxEdit has no blur step, so `ValidateBirthdayCommand`, its blur-first tests and the birthday dirty flag are removed. Validation runs on Save and on each dropdown selection while the field is in error (the same reward-early rule as the text fields). The error marks the empty dropdown: Birth month when only the day is set, otherwise Birth day. <!-- impl decision: ComboBoxEdit has no blur; dirty flag would be unread -->

## All consumers to update (T1 owns every compile fix; the branch must build and test green after T1)
`BirthdayDayMonth` / `personBirthday` / `ValidateBirthday(string)` are used in:
- `Domain/Entity/Person.cs`
- `Infra/EntityEFConfig/PersonConfiguration.cs`
- `Infra/Repositories/PersonRepository.cs` (about 12 hits)
- `Contracts/Models/PersonListItemDto.cs`
- `Domain/ServicesInterfaces/IPersonService.cs`
- `Services/PersonService.cs`
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- `PersonsViewModel.cs`
- `PersonFormPage.xaml(.cs)`
- Tests:
  - `PersonServiceTests`
  - `PersonRepositoryTests`
  - `PersistedStringTrimmingTests`
  - `PersonFormViewModelTests`
  - `PersonFormViewModelBug044Tests`
  - `PersonsViewModelTests`, if it is hit

Grep again at the start: this list is a floor, not a ceiling. Tests that assert the old `(15/03)` display format move to `(15 Mar)` (REQ-BDAY-10).

**Superseded repository tests (handled in T1, because this change drops `IX_Persons_Name_Birthday`):**
- `PersonRepositoryTests.AddAsync_SameNameSameBirthday_ThrowsDbUpdateException`: delete it. BUG-088's identity index test replaces it.
- `AddAsync_SameNameNullBirthday_AllowsDuplicate`: rewrite it with `BirthDay`/`BirthMonth`. It stays valid until BUG-088 T2 updates it.

## Tests
| AC | Test |
|----|------|
| REQ-BDAY-05 | `PersonServiceTests`: replace the 10 string `ValidateBirthday` tests with int-based ones (both null, only day, only month, month 0/13, day 0/32, 31 Apr, 29 Feb ok, 30 Feb rejected). These tests encode the superseded AC-1.8, so replacing them is the spec change. |
| REQ-BDAY-02/03/06/09 | `PersonFormViewModelTests`: day list per month, clamp clears the day, one-of-two error on Save, hydration pre-selects. |
| REQ-BDAY-07 | `PersonRepositoryTests` (real SQLite): saving day without month throws `DbUpdateException` (CHECK). |
| REQ-BDAY-08 | `Tests/Integration/Migrations/PersonBirthDayMonthMigrationTests.cs`: migrate to the previous migration, insert rows `'15/03'`, `'1503'`, `'ab/cd'`, `'45/13'` and `'30/02'`, then migrate to the latest and assert the converted values. |
| REQ-BDAY-10 | Domain unit test on `GetDisplayIdentifier`. |
| REQ-BDAY-01/04 | UI-only: manual on-device check (gate). |

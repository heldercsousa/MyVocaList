# Persons — Implementation Tasks

> **Status:** Pending
> **Last updated:** 2026-03-31

---

## Phase 0: Pre-Implementation Fixes (Infra + Service)

These are bugs in existing code — fix before writing any UI.

- [ ] T-00a: Fix `PersonConfiguration` — `Email` and `BirthdayDayMonth` must be `IsRequired(false)`; `FullNameNormalized` must be `IsRequired()`; add `ExternalId` (`Guid?`, nullable); make Email index unique; add unique `ExternalId` index; add filtered composite unique index on `(FullNameNormalized, BirthdayDayMonth) WHERE BirthdayDayMonth IS NOT NULL`
- [ ] T-00b: Add `ExternalId` (`Guid?`) property to `Person` entity (`Domain/Entity/Person.cs`)
- [ ] T-00c: Add EF Core migration for all `PersonConfiguration` changes
- [ ] T-00d: Rewrite `PersonRepository` — replace `FullName.StartsWith()` with `EF.Functions.Like` + `EF.Functions.Collate` on `FullNameNormalized`; fix `GetByFullNameAsync` (collation-aware); remove `Console.WriteLine`; consolidate `SearchByNameAsync` and `SearchByNameStartsWithAsync` into one method (`SearchByNameStartsWithAsync`); add `SearchByNameOrEmailAsync`; add `GetPagedAsync(pageNumber, pageSize, query)`
- [ ] T-00e: Update `IPersonRepository` — remove `SearchByNameAsync` (redundant); add `SearchByNameOrEmailAsync`; add `GetPagedAsync`
- [ ] T-00f: Fix `PersonService.CreatePersonAsync` — remove bare `catch (Exception ex)` message pass-through; let unexpected exceptions bubble to `GlobalExceptionHandler`
- [ ] T-00g: Build verification after Phase 0 — 0 errors
- [ ] T-00h: Remove `ITextNormalizationService` — delete `Domain/Services/ITextNormalizationService.cs`; remove `_textNormalizer` field and constructor parameter from `PersonService`; replace any `_textNormalizer.Normalize(x)` calls with `x` directly (DB `NOCASE_NOACCENT` collation handles all case/accent normalization at query time)
- [ ] T-00i: Fix `PersonService.ValidateBirthday` — return `(true, "")` when birthday is null or whitespace (birthday is optional; the current code incorrectly returns `(false, "Birthday is required")`)

## Phase 1: Service Additions

- [ ] T-01: Add `GetPagedPersonsForListAsync(pageNumber, pageSize, query)` to `IPersonService` + implement in `PersonService` — normalizes query, delegates to `IPersonRepository.GetPagedAsync`, projects to `PersonListItemDto`; sorted by `FullName` ascending
- [ ] T-02: Add `UpdatePersonAsync(id, fullName, birthday, email)` to `IPersonService` + implement — validates all fields, checks email uniqueness (excluding self), normalizes name, saves; returns `(bool success, string message)`
- [ ] T-03: Add `DeletePersonsAsync(IEnumerable<int> ids)` to `IPersonService` + implement; returns `(bool success, string message)`
- [ ] T-04: Add `IsEmailTakenAsync(email, excludePersonId?)` internally (or inline in service) for email uniqueness check

## Phase 2: Contracts + DI

- [ ] T-05: Add `BirthdayDayMonth` and `Email` fields to `PersonListItemDto` (`Contracts/Models/PersonListItemDto.cs`)
- [ ] T-06: Register `IPersonService → PersonService` (`AddScoped`) in `MauiProgram.cs`
- [ ] T-07: Register `IPersonRepository → PersonRepository` (`AddScoped`) in `MauiProgram.cs`
- [ ] T-08: Add `Routes.PersonForm = "personform"` to `MyVocaList/Navigation/Routes.cs`
- [ ] T-09: Register route `Routes.PersonForm → PersonFormPage` in `AppShell.xaml.cs`

## Phase 2b: AutocompleteField Component (prerequisite for form page)

- [ ] T-09a: Add `AutocompleteSuggestion` record to `Contracts/Models/AutocompleteSuggestion.cs`
- [ ] T-09b: Implement `AutocompleteField` ContentView — see `Docs/superpowers/specs/2026-03-30-autocomplete-field-design.md` for full spec; build + verify in isolation before wiring into form page

## Phase 3: UI — Form Page

- [ ] T-10: Add `PersonFormViewModel` — `[QueryProperty]` for `personId`/`personName`/`personBirthday`/`personEmail`; `IsEditMode`; `PageTitle`; validation state for all three fields; `Suggestions` (`IEnumerable<AutocompleteSuggestion>`); `SearchPersonsCommand(string term)` (receives already-debounced text from `AutocompleteField`); `SuggestionSelectedCommand(AutocompleteSuggestion)` (navigates to edit for selected person); `SaveCommand`; `CancelCommand`; character counter helpers — no debounce logic (owned by `AutocompleteField`)
- [ ] T-11: Add `PersonFormPage.xaml` — `SafeAreaEdges="All"`; `ScrollView`; `AutocompleteField` in Full Name slot (bind `Suggestions`, `SearchRequestedCommand`, `SuggestionSelectedCommand`); birthday `TextEdit` (placeholder "DD/MM"); email `TextEdit`; character counter label with DataTriggers; `Cancel` + `Save` buttons `HorizontalOptions=End`
- [ ] T-12: Add `PersonFormPage.xaml.cs` — `OnAppearing` focuses name field (create mode only); typed `ViewModel` property for compiled bindings
- [ ] T-13: Register `PersonFormPage` + `PersonFormViewModel` as `AddTransient` in `MauiProgram.cs`
- [ ] T-14: Build + smoke test form: create singer → success snackbar → appears in list

## Phase 4: UI — List Page

- [ ] T-15: Add `PersonsViewModel` — always-on selection; `IsSearchMode`/`IsScrolled`/`AppBarTitle`; paging + 400ms search debounce; `FloatingToolbar` commands (Select All / Edit / Delete); confirm-delete BottomSheet state; `OnSelectionChanged(count)`
- [ ] T-16: Add `PersonsPage.xaml` — `SmallAppBar`+`SearchAppBar` in `Shell.TitleView`; `ShimmerView`+`DXCollectionView`; `ListItem` rows with `ListItemLeadingMonogram` + `CheckEdit` trailing; two `EmptyState` components; `FloatingToolbar` (3 slots); FAB (`Style="{StaticResource Fab}"`); `ConfirmSheet` component
- [ ] T-17: Add `PersonsPage.xaml.cs` — `OnCollectionViewScrolled`; `OnSelectionChanged`; `OnConfirmSheetStateChanged`; `OnViewModelPropertyChanged` (opens/closes sheet); `OnBackButtonPressed` (sheet → search → default); `SelectedItems` assigned in `OnAppearing`; typed `ViewModel` property
- [ ] T-18: Register `PersonsPage` + `PersonsViewModel` as `AddTransient` in `MauiProgram.cs`
- [ ] T-19: Wire `PersonsPage` in `AppShell.xaml` as a navigation target or tab

## Phase 4b: Tests (TDD — write tests before each phase's implementation)

> Tests are written **before** the implementation they cover, following the Red-Green-Refactor cycle defined in `.claude/rules/testing.md`.
> The `MyVocaList.Tests` project is set up during Styles & Structure (Step 1) — already available here.

- [ ] T-19a: `PersonServiceTests` — name validation rules (all AC-1.3–1.7 cases + birthday + email format); `CreatePersonAsync` success path; `UpdatePersonAsync` email-excludes-self; `DeletePersonsAsync` returns correct message
- [ ] T-19b: `PersonFormViewModelTests` — `SaveCommand` triggers correct service call; validation errors set `HasError`/`ErrorText` properties; `IsEditMode` derived property; `PageTitle` derived property; `SearchPersonsCommand` maps service results to `AutocompleteSuggestion` collection
- [ ] T-19c: `PersonsViewModelTests` — `AppBarTitle` derived property ("Singers" / "N selected"); `CanEditSelected` (== 1); `CanDeleteSelected` (> 0); `IsEmptyNoPersons` / `IsEmptyNoResults` state transitions; `SelectAllCommand` stays in selection mode when deselecting all
- [ ] T-19d: `PersonRepositoryIntegrationTests` — filtered composite unique index allows same name + null birthday; blocks same name + same birthday; nullable email unique index allows multiple NULLs; `SearchByNameOrEmailAsync` returns correct results with NOCASE collation

---

## Phase 5: Verification

- [ ] T-20: Build verification — 0 errors
- [ ] T-21: Smoke test: create singer → list → edit name/birthday/email → updated → delete → removed
- [ ] T-22: Smoke test: type duplicate name → suggestion appears → tap → edit form opens pre-populated
- [ ] T-23: Smoke test: search by email → matching singer appears in list
- [ ] T-24: Push to remote

---

## Notes

- **Do not rewrite existing domain/service logic** that is already correct — only fix bugs documented in Phase 0.
- `PersonRepository.SearchByAnyWordAsync` stays as `NotImplementedException` — not needed here.
- `PersonListItemDto` is a class (not record) with `INotifyPropertyChanged` — participations/absences update in-place.
- Suggestion debounce is 300ms (form) vs 400ms (list) — form suggestions must feel faster.
- The suggestion overlay must float above the form — use a `Grid` overlay so birthday/email fields don't shift.
- Email uniqueness check: nullable unique DB index prevents duplicates at DB level; service check provides the user-facing error message before the DB is hit.
- Filtered unique index on `(FullNameNormalized, BirthdayDayMonth)`: EF Core generates SQLite `WHERE [BirthdayDayMonth] IS NOT NULL` — this is idiomatic SQLite and requires no manual migration editing.

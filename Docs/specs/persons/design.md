# Persons — Technical Design

> **Status:** Spec approved — pending implementation
> **Last updated:** 2026-03-31

---

## Architecture

Domain + Contracts + Infra + Services layers partially exist. Most MAUI UI artefacts are new.

| Layer | Artefacts status |
|-------|-----------------|
| Domain | `Person` entity ✅ · `IPersonRepository` ✅ · `IPersonService` ✅ — all need additions |
| Contracts | `PersonListItemDto` ✅ — needs `BirthdayDayMonth`, `Email` fields added |
| Infra | `PersonRepository` ⚠️ needs rewrite · `PersonConfiguration` ⚠️ needs fixes |
| Services | `PersonService` ⚠️ needs `GetPagedPersonsForListAsync`, `UpdatePersonAsync`, `DeletePersonsAsync`, email uniqueness check; error handling fix |
| MAUI | `PersonsPage`, `PersonFormPage`, `PersonsViewModel`, `PersonFormViewModel` — **all new** |

---

## Pre-Implementation: Infra + Service Fixes

These issues must be fixed before any UI work begins.

### PersonConfiguration fixes

Current bugs:
- `Email` and `BirthdayDayMonth` have `IsRequired()` (no argument) — defaults to `true`. Must be `IsRequired(false)` — they are optional fields.
- `FullNameNormalized` has `IsRequired(false)` — inverted. Must be `IsRequired()`.
- Email index is not unique.
- No `ExternalId` column.
- No composite unique index on name + birthday.

Required state after fix:
```csharp
builder.Property(p => p.ExternalId)
       .IsRequired(false);

builder.Property(p => p.FullName)
       .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

builder.Property(p => p.FullNameNormalized)
       .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

builder.Property(p => p.BirthdayDayMonth)
       .HasColumnType("TEXT").IsRequired(false).HasMaxLength(5);   // "DD/MM"

builder.Property(p => p.Email)
       .HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);

// Indexes
builder.HasIndex(p => p.FullNameNormalized)
       .HasDatabaseName("IX_Persons_FullNameNormalized");

builder.HasIndex(p => p.Email)
       .IsUnique()
       .HasDatabaseName("IX_Persons_Email");   // NULLs not equal in SQLite — multiple NULLs allowed

builder.HasIndex(p => p.ExternalId)
       .IsUnique()
       .HasDatabaseName("IX_Persons_ExternalId");

// Filtered composite unique index: same name + same birthday = duplicate
// WHERE BirthdayDayMonth IS NOT NULL — allows same name with no birthday
builder.HasIndex(p => new { p.FullNameNormalized, p.BirthdayDayMonth })
       .IsUnique()
       .HasFilter("[BirthdayDayMonth] IS NOT NULL")
       .HasDatabaseName("IX_Persons_Name_Birthday");
```

### Person entity additions

Add `ExternalId` to `Person.cs`:
```csharp
public Guid? ExternalId { get; set; }   // Reserved for future device/account identity
```

### PersonRepository rewrite

Current issues:
- `SearchByNameAsync` and `SearchByNameStartsWithAsync` are identical — both use `FullName.StartsWith()` without collation
- No use of `FullNameNormalized` index
- `GetByFullNameAsync` uses `==` (case-sensitive) instead of collation-aware comparison
- `Console.WriteLine` left in production code
- No email search

Required repository methods after fix:

```csharp
// Exact match by normalized name (for dedup check)
public async Task<Person> GetByFullNameAsync(string fullName)
{
    var normalized = /* normalize via injected service or inline */;
    return await _dbSet.FirstOrDefaultAsync(p =>
        EF.Functions.Like(
            EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
            EF.Functions.Collate(normalized, "NOCASE")));
}

// Suggestions: prefix match on normalized name (used by form suggestion list)
public async Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10)
{
    var normalized = /* normalize term */;
    return await _dbSet
        .Where(p => EF.Functions.Like(
            EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
            EF.Functions.Collate(normalized + "%", "NOCASE")))
        .OrderBy(p => p.FullNameNormalized)
        .Take(maxResults)
        .ToListAsync();
}

// Full search: name OR email (used by list page search)
public async Task<List<Person>> SearchByNameOrEmailAsync(string searchTerm, int maxResults = 10)
{
    var normalized = /* normalize term */;
    var pattern = normalized + "%";
    return await _dbSet
        .Where(p =>
            EF.Functions.Like(
                EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                EF.Functions.Collate(pattern, "NOCASE"))
            ||
            EF.Functions.Like(
                EF.Functions.Collate(p.Email, "NOCASE"),
                EF.Functions.Collate("%" + searchTerm.Trim() + "%", "NOCASE")))
        .OrderBy(p => p.FullNameNormalized)
        .Take(maxResults)
        .ToListAsync();
}

// Paged query for list page (name OR email search, sorted by name)
public async Task<(IEnumerable<Person> items, int totalCount)> GetPagedAsync(
    int pageNumber, int pageSize, string query = null)
{ ... }
```

**Note on `SearchByNameAsync`:** The old method is now redundant (identical to `SearchByNameStartsWithAsync`). Remove it from `IPersonRepository` and `PersonRepository`, and remove the call from `PersonService`. The interface is already in Domain — update it.

**Note on normalization:** `ITextNormalizationService` is removed — the DB `NOCASE_NOACCENT` collation (applied globally via `CollationInterceptor`) handles all case and accent normalization at query time. `FullNameNormalized` is set to the raw `fullName` value before save; the DB handles the rest. The service passes the raw search term to the repository; the repository applies `EF.Functions.Collate` in the `WHERE` clause.

### PersonService additions

Add these methods to `IPersonService` and implement in `PersonService`:

```csharp
// List page data
Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
    int pageNumber, int pageSize, string query = null);

// Edit
Task<(bool success, string message)> UpdatePersonAsync(
    int id, string fullName, string birthday = null, string email = null);

// Delete
Task<(bool success, string message)> DeletePersonsAsync(IEnumerable<int> ids);

// Email uniqueness check (used internally)
Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null);
```

Fix `CreatePersonAsync`: remove bare `catch (Exception ex)` that swallows message as a user-facing string — let unexpected exceptions bubble to `GlobalExceptionHandler`.

---

## Contracts DTO

Add two fields to `PersonListItemDto`:

```csharp
public string BirthdayDayMonth { get; set; }
public string Email { get; set; }
```

These are needed for pre-populating the edit form via query string.

---

## Page Structure

### PersonsPage (list)

| Slot | Component | Notes |
|------|-----------|-------|
| `Shell.TitleView` | `SmallAppBar` + `SearchAppBar` | Swapped via `InverseBoolConverter` on `IsSearchMode` |
| Content root | Single-cell `Grid` | Overlay pattern |
| Loading | `ShimmerView` wrapping `DXCollectionView` | `IsInitialLoading` drives shimmer |
| List | `DXCollectionView` | `SelectionMode="Multiple"` hardcoded; `Margin="0,0,0,80"` |
| Item row | `ListItem` | `ListItemLeadingMonogram` + `CheckEdit` trailing; Headline=`FullName`; SupportingText=`ParticipationsAbsencesNumber` |
| Empty states | Two `EmptyState` components | `IsEmptyNoPersons` / `IsEmptyNoResults`; uses `EmptyState` from `UI/Components/States/` |
| FAB | `DXButton` | `Style="{StaticResource Fab}"` + `Margin="0,0,16,88"` + `Icon` + `Command` inline |
| Actions | `FloatingToolbar` | Slots: Select All / Edit / Delete |
| Confirm delete | `ConfirmSheet` | Component from `UI/Components/Sheets/`; `SheetState` TwoWay bound to `ConfirmSheetState` |

**FloatingToolbar slots:**
| Slot | Icon | Action | CanExecute |
|------|------|--------|-----------|
| Action1 | `checklist_outlined` | Select All toggle | Always |
| Action2 | `edit_outlined` | Edit selected | `SelectedCount == 1` |
| Action3 | `delete_outlined` | Delete selected | `SelectedCount > 0` |

### PersonFormPage (add/edit)

| Slot | Component | Notes |
|------|-----------|-------|
| `Shell.Title` | `PageTitle` binding | "New Singer" / "Edit Singer" |
| Full Name field | `AutocompleteField` component | Encapsulates TextEdit + debounce + suggestion overlay — see `Docs/superpowers/specs/2026-03-30-autocomplete-field-design.md` |
| Birthday field | `TextEdit` | Optional; placeholder "DD/MM" |
| Email field | `TextEdit` | Optional |
| Action buttons | `OutlinedButton("Cancel")` + `FilledButton("Save")` | `HorizontalOptions=End` |

**Name field uses `AutocompleteField` component** (`MyVocaList/UI/Components/AutocompleteField/`) which handles the `TextEdit`, debounce, and overlay internally.

`PersonFormViewModel` exposes:
- `Suggestions` — `IEnumerable<AutocompleteSuggestion>` — set by `SearchPersonsCommand` result
- `SearchPersonsCommand(string term)` — bound to `AutocompleteField.SearchRequestedCommand`; receives already-debounced text
- `SuggestionSelectedCommand(AutocompleteSuggestion s)` — bound to `AutocompleteField.SuggestionSelectedCommand`; navigates to edit form for the selected person

The ViewModel does NOT contain debounce logic or overlay visibility state — that is owned by the component.

`SafeAreaEdges="All"` + `ScrollView` handles keyboard avoidance.

---

## ViewModel Design

### PersonsViewModel

```csharp
[ObservableProperty] bool _isRefreshing;
[ObservableProperty] string _searchText;
[ObservableProperty] bool _isSearchMode;
[ObservableProperty] bool _isScrolled;
[ObservableProperty] int _selectedCount;
[ObservableProperty] BottomSheetState _confirmSheetState;
[ObservableProperty] bool _hasMoreItems;
[ObservableProperty] bool _isInitialLoading;
[ObservableProperty] string _confirmMessage;
[ObservableProperty] string _confirmActionText;

// Derived
string AppBarTitle           // "Singers" | "N selected"
bool CanEditSelected         // SelectedCount == 1
bool CanDeleteSelected       // SelectedCount > 0
bool IsAllSelected
bool IsEmptyNoPersons        // no items, no active search
bool IsEmptyNoResults        // no items, active search

// Commands
RefreshCommand, LoadMoreCommand, AddPersonCommand
EditSelectedCommand, DeleteSelectedCommand
SelectAllCommand, ConfirmActionCommand, DismissConfirmCommand
OpenSearchCommand, CloseSearchCommand
```

### PersonFormViewModel

```csharp
[QueryProperty(nameof(PersonIdRaw), "personId")]
[QueryProperty(nameof(PersonName), "personName")]
[QueryProperty(nameof(PersonBirthday), "personBirthday")]
[QueryProperty(nameof(PersonEmail), "personEmail")]

[ObservableProperty] int? _personId;
[ObservableProperty] string _personName;
[ObservableProperty] string _personBirthday;
[ObservableProperty] string _personEmail;

// Validation
[ObservableProperty] bool _nameHasError;
[ObservableProperty] string _nameErrorText;
[ObservableProperty] bool _birthdayHasError;
[ObservableProperty] string _birthdayErrorText;
[ObservableProperty] bool _emailHasError;
[ObservableProperty] string _emailErrorText;

[ObservableProperty] bool _isBusy;
[ObservableProperty] IEnumerable<AutocompleteSuggestion> _suggestions;  // set by SearchPersonsCommand; forwarded to AutocompleteField

// Character counter (name)
bool ShowCharacterCounter
string CharacterCounterText
bool IsCharacterCounterWarning
bool IsCharacterCounterError

bool IsEditMode => PersonId.HasValue;
string PageTitle => IsEditMode ? "Edit Singer" : "New Singer";
```

**SearchPersonsCommand(string term):** Fired by `AutocompleteField` after its internal debounce (300ms default). Receives already-debounced text from the component. Calls `SearchPersonsStartsWithAsync(term, 5)`. Projects results to `IEnumerable<AutocompleteSuggestion>` and sets `Suggestions` binding. No timer or debounce logic in the ViewModel — that is owned by `AutocompleteField`.

**SuggestionSelectedCommand(AutocompleteSuggestion s):** Fired by `AutocompleteField` on suggestion row tap. Casts `s.Data` to `Person`. Navigates to `PersonForm?personId=X&personName=Y&personBirthday=Z&personEmail=W`.

---

## Code-Behind Responsibilities (PersonsPage.xaml.cs)

| Event | Handler | Purpose |
|-------|---------|---------|
| `OnAppearing` | Assigns `SelectedItems`; calls `InitializeAsync` | DXCollectionView `IList` requirement |
| `SelectionChanged` | `OnSelectionChanged` → `_viewModel.OnSelectionChanged(count)` | `SelectedCount` sync |
| `Scrolled` | `OnCollectionViewScrolled` → `_viewModel.IsScrolled = e.Offset > 0` | App bar elevation |
| `StateChanged` (BottomSheet) | `OnConfirmSheetStateChanged` | User-dismiss → VM sync |
| `PropertyChanged` (VM) | `OnViewModelPropertyChanged` | Opens/closes confirm sheet |
| `OnBackButtonPressed` | Sheet → search → default | Android back priority |

---

## Interaction Flows

### Name suggestion (form)
1. User types ≥ 2 chars → 300ms debounce
2. `SearchPersonsStartsWithAsync(normalizedTerm, 5)` → results
3. `Suggestions` populated → overlay card appears
4. Tap suggestion → `SuggestionSelectedCommand` → navigate to edit that person
5. User ignores → saves new person normally
6. Field cleared < 2 chars → overlay hides

### Select → Edit
1. Row tap → DXCollectionView toggles selection
2. `SelectedCount == 1` → Edit slot activates
3. Tap Edit → clear selection → `GoToAsync(PersonForm?personId=X&...)`
4. Form saves → snackbar → `GoToAsync("..")` → list reloads on `OnAppearing`

### Select → Delete
1. `SelectedCount > 0` → Delete slot activates
2. Tap Delete → `ConfirmSheetState = HalfExpanded`
3. Confirm → `DeletePersonsAsync(ids)` → snackbar → reload

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Load failure | Logged; `IsRefreshing = false`; list stays as-is |
| Save validation failure | Inline field errors; no navigation |
| Email duplicate | Inline `EmailHasError` + `EmailErrorText`; no navigation |
| Singer not found on edit | Inline name error |
| Unexpected exceptions | Bubble to `GlobalExceptionHandler` |

---

## Navigation & DI Registration

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));

// MauiProgram.cs
builder.Services.AddTransient<PersonsPage>();
builder.Services.AddTransient<PersonsViewModel>();
builder.Services.AddTransient<PersonFormPage>();
builder.Services.AddTransient<PersonFormViewModel>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
```

Route: `Routes.PersonForm = "personform"` (add to `Navigation/Routes.cs`).

---

## Key Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Dedup strategy | Option A — non-blocking suggestions | Admin context; blocking adds friction at live events |
| Suggestion trigger | 300ms debounce | Faster than list search (400ms) — suggestions must feel instant |
| Search scope | Name OR Email | Admins may remember an email but not the exact name spelling |
| Normalization in search | Service normalizes term; repository receives normalized string | Single normalization location |
| Email uniqueness | Nullable unique index at DB + service check | DB enforces integrity; service provides user-friendly error |
| Name+Birthday index | Filtered unique (WHERE birthday IS NOT NULL) | DB-level dedup foundation for future Option C; no v1 behavior change |
| ExternalId | `Guid?`, nullable unique index | Low-cost future-proofing for self-registration; painful to add later |
| `BirthdayDayMonth` storage | DD/MM text only | Disambiguation only; year not needed |
| SelectionMode | Always `Multiple` hardcoded | Follows Venues pattern |
| Form pattern | Shell navigation page | Keyboard safety on Android |

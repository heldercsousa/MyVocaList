# Venues — Technical Design

> **Status:** Implemented (reference spec — canonical pattern for all future CRUD features)
> **Last updated:** 2026-03-29

---

## Architecture

All five layers are touched:

| Layer | Artefacts added/modified |
|-------|--------------------------|
| Domain | `Venue` entity, `IVenueRepository`, `IVenueService` |
| Contracts | `VenueListItemDto` |
| Infra | `VenueRepository`, EF Core migration |
| Services | `VenueService` |
| MAUI | `VenuesPage`, `VenueFormPage`, `VenuesViewModel`, `VenueFormViewModel`, DI registration |

---

## Domain Entity

```csharp
// Domain/Entity/Venue.cs
public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Event> Events { get; set; }  // navigation — no delete if count > 0
}
```

---

## Contracts DTO

```csharp
// Contracts/DTOs/List/VenueListItemDto.cs
public class VenueListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int EventCount { get; set; }
    public bool HasEvents => EventCount > 0;  // derived — no DB column
}
```

---

## Repository Interface

```csharp
// Domain/RepositoryInterface/IVenueRepository.cs
public interface IVenueRepository : IBaseRepository<Venue>
{
    Task<Venue?> GetByNameAsync(string name);
    Task<(IEnumerable<(Venue venue, int eventCount)> items, int totalCount)>
        GetPagedWithEventInfoAsync(int pageNumber, int pageSize, string? query = null);
    Task<IEnumerable<(Venue venue, int eventCount)>>
        GetByIdsWithHasEventsAsync(IEnumerable<int> ids);
}
```

Note: `GetByNameAsync` is used for duplicate-name checks (case-insensitive via `CollationInterceptor`).
`GetPagedWithEventInfoAsync` returns `eventCount` per venue — the EF projection computes this via SQL COUNT, not in-memory.

---

## Service Interface

```csharp
// Domain/ServicesInterfaces/IVenueService.cs
public interface IVenueService
{
    (bool isValid, string message) ValidateNameInput(string name);
    Task<(bool success, string message)> CreateVenueAsync(string name);
    Task<(bool success, string message)> UpdateVenueAsync(int id, string newName);
    Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);
    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
    Task<(IEnumerable<VenueListItemDto> items, int totalCount)>
        GetPagedVenuesForListAsync(int pageNumber, int pageSize, string query = null);
}
```

Service validation constants: `MaxInputLength = 30`, `ShowCounterAt = 25`.

---

## Page Structure

### VenuesPage (list)

| Slot | Component | Notes |
|------|-----------|-------|
| `Shell.TitleView` | `SmallAppBar` (default) + `SearchAppBar` (search mode) | Swapped via `InverseBoolConverter` on `IsSearchMode` |
| Content root | Single-cell `Grid` | Overlay pattern — all children stack in the same cell |
| Loading state | `ShimmerView` wrapping `DXCollectionView` | `IsInitialLoading` drives the shimmer |
| List | `DXCollectionView` | `SelectionMode="Multiple"` hardcoded; `Margin="0,0,0,80"` clears toolbar |
| Item row | `ListItem` with `ListItemLeadingIcon` + `CheckEdit` trailing | `ItemTemplate` (IsSelected=False) + `SelectedItemTemplate` (IsSelected=True) |
| Empty states | Two `VerticalStackLayout` overlays | `IsEmptyNoVenues` / `IsEmptyNoResults` |
| FAB | `DXButton` | `VerticalOptions=End`, `HorizontalOptions=End`, `Margin="0,0,16,88"` |
| Actions | `FloatingToolbar` | `VerticalOptions=End`, `HorizontalOptions=Center`, `Margin="0,0,0,16"` |
| Confirm delete | `BottomSheet` | `HalfExpandedRatio=0.28`, driven by `ConfirmSheetState` VM property |

**FAB bottom margin formula:** `toolbar height(48) + toolbar margin(16) + gap(24) = 88dp`
**List bottom margin:** `toolbar height(48) + toolbar margin(16) + breathing room(16) = 80dp`

### VenueFormPage (add/edit)

| Slot | Component |
|------|-----------|
| `Shell.Title` | Bound to `PageTitle` ("New Venue" / "Edit Venue") |
| Content | `ScrollView` → `VerticalStackLayout` with `TextEdit` + character counter + action buttons |
| Action buttons | `HorizontalStackLayout(End)` — `OutlinedButton("Cancel")` + `FilledButton("Save")` |

`SafeAreaEdges="All"` handles keyboard avoidance automatically. No `FloatingToolbar` — keyboard would overlap it.

---

## ViewModel Design

### VenuesViewModel (key properties and commands)

```csharp
// Observable properties
[ObservableProperty] bool _isRefreshing;
[ObservableProperty] string _searchText;
[ObservableProperty] bool _isSearchMode;      // drives SmallAppBar ↔ SearchAppBar swap
[ObservableProperty] bool _isScrolled;        // drives IsElevated on both app bars
[ObservableProperty] int _selectedCount;
[ObservableProperty] BottomSheetState _confirmSheetState;
[ObservableProperty] bool _hasMoreItems;
[ObservableProperty] bool _isInitialLoading;
[ObservableProperty] string _confirmMessage;
[ObservableProperty] string _confirmActionText;

// Derived (notified via OnSelectedCountChanged / NotifyEmptyStates)
string AppBarTitle           // "Venues" | "N selected"
bool CanEditSelected         // SelectedCount == 1
bool CanDeleteSelected       // SelectedCount > 0
bool IsAllSelected           // Venues.Count > 0 && SelectedCount == Venues.Count
bool IsEmpty                 // !IsInitialLoading && Venues.Count == 0
bool IsEmptyNoVenues         // IsEmpty && SearchText is empty
bool IsEmptyNoResults        // IsEmpty && SearchText has value

// Commands
RefreshCommand, LoadMoreCommand, AddVenueCommand
DeleteSelectedCommand (CanExecute: CanDeleteSelected)
EditSelectedCommand   (CanExecute: CanEditSelected)
SelectAllCommand, ConfirmActionCommand, DismissConfirmCommand
OpenSearchCommand, CloseSearchCommand
```

**Threading:** All UI mutations via `RunOnUiThread()`. Load-more guard uses `volatile bool _isLoading`. First-page load uses `SemaphoreSlim(1,1)`. Search debounce uses `CancellationTokenSource` + `Task.Run` (400ms delay).

**Selection restore:** After list reload, selection is restored by matching `Id` from `SelectedVenues` snapshot taken before the reload.

### VenueFormViewModel (key properties)

```csharp
[QueryProperty(nameof(VenueIdRaw), "venueId")]   // string → int? parse
[QueryProperty(nameof(VenueName), "venueName")]

[ObservableProperty] int? _venueId;              // null = create mode
[ObservableProperty] string _venueName;
[ObservableProperty] bool _nameHasError;
[ObservableProperty] string _nameErrorText;
[ObservableProperty] bool _isBusy;
// + character counter properties

bool IsEditMode => VenueId.HasValue;
string PageTitle => IsEditMode ? "Edit Venue" : "New Venue";
```

---

## Code-Behind Responsibilities (VenuesPage.xaml.cs)

| Event | Handler | Purpose |
|-------|---------|---------|
| `OnAppearing` | Assigns `SelectedItems` to VM collection; calls `InitializeAsync` | DXCollectionView requires `IList`; assigned here not in binding |
| `SelectionChanged` | `OnSelectionChanged` → `_viewModel.OnSelectionChanged(count)` | Keeps `SelectedCount` in sync |
| `Scrolled` | `OnCollectionViewScrolled` → `_viewModel.IsScrolled = e.Offset > 0` | Drives app bar elevation |
| `StateChanged` (BottomSheet) | `OnConfirmSheetStateChanged` | Syncs user-dismiss back to VM |
| `PropertyChanged` (VM) | `OnViewModelPropertyChanged` | Opens/closes confirm sheet from VM state |
| `OnBackButtonPressed` | Closes confirm sheet → closes search → default | Android hardware back priority chain |

---

## Interaction Flows

### Page load
1. `OnAppearing` → `InitializeAsync()`
2. `IsInitialLoading = true` → `Task.Yield()` (shimmer renders)
3. `GetPagedVenuesForListAsync(page=1)` → `Venues.ReplaceRange(list)` → `IsInitialLoading = false`

### Search
1. User taps search icon → `OpenSearchCommand` → `IsSearchMode = true`
2. `SearchAppBar` becomes visible; `SmallAppBar` hides
3. User types → `OnSearchTextChanged` → `TriggerSearchDebounce` (400ms)
4. `LoadFirstPageAsync(token)` with `_currentSearchQuery`
5. User taps back arrow → `CloseSearchCommand` → `IsSearchMode = false`, `SearchText = ""`

### Select → Edit
1. User taps row → DXCollectionView toggles selection natively
2. `SelectionChanged` → `SelectedCount` updated → `AppBarTitle` / `CanEditSelected` notified
3. With exactly 1 selected: Edit button activates (`Action2IsSelected=true`)
4. User taps Edit → `NavigateToEditAsync` → clears selection → `GoToAsync(VenueForm?venueId=X&venueName=Y)`
5. Form saves → snackbar → `GoToAsync("..")` → list reloads on `OnAppearing`

### Select → Delete
1. With ≥1 selected: Delete button activates (`Action3IsSelected=true`)
2. User taps Delete → `RequestBatchDelete` → sets `ConfirmMessage` → `ConfirmSheetState = HalfExpanded`
3. Code-behind `OnViewModelPropertyChanged` → `confirmSheet.Show(...)`
4. User taps "Delete" → `ExecuteConfirmActionAsync` → `DeleteVenuesAsync(ids)` → snackbar → list reload
5. User taps "Cancel" or swipes down → `OnConfirmSheetStateChanged` → `ConfirmSheetState = Hidden`

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Load / load-more failure | Logged as Error; `IsRefreshing = false`; list stays as-is |
| Create duplicate name | Inline `NameHasError` + `NameErrorText`; no navigation |
| Update name conflict | Inline `NameHasError` + `NameErrorText`; no navigation |
| Delete with events | Partial delete; result snackbar explains blocked items |
| Venue not found on edit | Service returns `(false, "Venue not found")` → inline error |
| Unexpected exceptions | Bubble up to `GlobalExceptionHandler` — no catch in ViewModel |

---

## Navigation & DI Registration

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(Routes.VenueForm, typeof(VenueFormPage));

// MauiProgram.cs
builder.Services.AddTransient<VenuesPage>();
builder.Services.AddTransient<VenuesViewModel>();
builder.Services.AddTransient<VenueFormPage>();
builder.Services.AddTransient<VenueFormViewModel>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
```

Route constant: `Routes.VenueForm = "venueform"` (defined in `MyVocaList/Navigation/Routes.cs`).

---

## Key Implementation Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| SelectionMode | Always `Multiple` (hardcoded in XAML) | Eliminates mode-toggle complexity; tap = toggle natively |
| Multi-select mode | Removed entirely | Always-on selection is simpler and equally usable |
| Swipe-delete | Removed | FloatingToolbar Delete replaces it; avoids swipe/multi-select conflict |
| Form pattern | Shell navigation page | BottomSheet + keyboard conflict on Android |
| Form buttons | Inline labeled (`Cancel` + `Save`) | Keyboard clarity; FloatingToolbar would be hidden behind keyboard |
| FloatingToolbar | Always visible; slots enable/disable via `CanExecute` | Persistent action bar per MD3 spec |
| SearchAppBar vs inline | SearchAppBar in Shell.TitleView | MD3 Search App Bar pattern; frees content area |

# Queue Feature: QueueListItem Migration + Queue List Entry Point

## Context

Two structural problems in the Queue feature:

1. **`QueueListItem.xaml` bypasses the `ListItem` pattern** — domain-specific, zero-BindableProperty component that hard-binds to `QueueEntryViewModel` fields. Every other list page uses `ListItem` (10 consumers). Was scaffolded as a placeholder during Wave 4 with a `<!-- Status: will be filled in Wave 4B -->` comment that never shipped.

2. **`QueuePage.xaml` is a placeholder** — shows "under construction." The correct design is a **CRUD list page** (same pattern as Venues/People/Artists/Songs): a `CrudListView` showing all queues (events), a FAB to create a new queue, and tap-to-open `QueueManagementPage`. `Event` is an implementation detail of queue management — there is no separate EventsPage.

All changes on a git worktree off `develop`. Not directly on `develop`.

---

## Exploration Findings

### QueueListItem → ListItem Mapping

| QueueListItem (current) | ListItem slot |
|-------------------------|---------------|
| `Position` — 18pt bold number | `LeadingContent` → styled `Label` |
| `PersonName` | `Headline` |
| `SongTitle` | `SupportingText` |
| `Status` | `TrailingContent` — empty (Wave 4B placeholder, nothing to migrate) |

**Namespace bug in consumer:** `QueueManagementPage.xaml` declares `xmlns:queue="clr-namespace:MyVocaList.UI.Components.Queue"` — sub-namespace `.Queue` does not exist. `QueueListItem` and `CurrentSingerCard` are both in `MyVocaList.UI.Components`. Alias must be corrected (not removed — `CurrentSingerCard` uses it too).

### Event Entity & Service

`Event` required fields for creation: `venueId (int)`, `name (string, 1–100)`, `scheduledStart (DateTime)`, `scheduledEnd (DateTime)`, `mode (string)`.

```
EventStatus: Created=0, Started=1, Paused=2, Finished=3
Mode default: "VideoKaraoke"  (other value: "Bandoke")
```

`IEventService.CreateEventAsync` already exists and validates all fields. No `GetPagedEventsForListAsync` exists yet — must be added.

`EventListItemDto` already exists in `Contracts/DTOs/Event/`:
```csharp
record EventListItemDto(int Id, string Name, string? VenueName,
    DateTime? ScheduledStartTime, EventStatus Status, string Mode)
```

**`GetActiveEventAsync` bug:** `EventService` calls `GetPagedAsync(1, 1, null)` — fetches only 1 event (most recently scheduled) and checks its status. Misses active events that aren't the most recently scheduled. Fix: increase pageSize.

### Existing CRUD Infrastructure (reused)

| Class | Reused as-is |
|-------|-------------|
| `CrudListPageBase` | Base class for `QueuePage.xaml.cs` |
| `CrudListViewModelBase<TItem>` | Base class for `QueueViewModel` |
| `CrudListView` | XAML shell in `QueuePage.xaml` |
| `ListItem` | List item DataTemplate |
| `AutocompleteField` | Venue picker in `QueueFormPage` |
| `EmptyState` | Empty states in `QueuePage` |
| `ListItemLeadingIcon` | Leading `queue_music_outlined` icon in list |

Queue's FAB → `QueueFormPage` follows the same tap pattern as every other CRUD page (VenueFormPage, PersonFormPage, etc.).

---

## Plan

### Worktree Setup (main agent, shell only)

```bash
git worktree add .worktrees/fix/queue-entry-and-listitem origin/develop -b fix/queue-entry-and-listitem
```

---

### Task 1 — Replace QueueListItem with ListItem

**Subagent works in:** `.worktrees/fix/queue-entry-and-listitem`

**Files owned:**
- `MyVocaList/UI/Pages/Queue/QueueManagementPage.xaml` (modify)
- `MyVocaList/UI/Components/QueueListItem.xaml` (delete)
- `MyVocaList/UI/Components/QueueListItem.xaml.cs` (delete)
- `MyVocaList.sln` (remove 2 entries)

**Steps:**
1. In `QueueManagementPage.xaml`:
   - Fix `xmlns:queue` alias: `clr-namespace:MyVocaList.UI.Components` (remove spurious `.Queue`)
   - Add `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists;assembly=MyVocaList"`
   - Replace both `<queue:QueueListItem />` DataTemplate usages (Next Up + History) with:
     ```xaml
     <lists:ListItem
         Headline="{Binding PersonName}"
         SupportingText="{Binding SongTitle}">
         <lists:ListItem.LeadingContent>
             <Label Text="{Binding Position}"
                    FontSize="18"
                    FontAttributes="Bold"
                    VerticalOptions="Center"
                    HorizontalOptions="Center" />
         </lists:ListItem.LeadingContent>
     </lists:ListItem>
     ```
2. Delete `QueueListItem.xaml` + `QueueListItem.xaml.cs`
3. Remove both from `MyVocaList.sln` SolutionItems
4. Build → 0 errors

**No governance gate** — `ListItem` is not modified, only a new consumer added. `QueueListItem` had 1 consumer.

---

### Task 2 — Fix `GetActiveEventAsync` in EventService

**Subagent works in:** `.worktrees/fix/queue-entry-and-listitem`

**Files owned:**
- `Services/EventService.cs`

**Fix:**
```csharp
public async Task<Event?> GetActiveEventAsync(CancellationToken ct)
{
    var (events, _) = await _eventRepository.GetPagedAsync(1, 50, null, ct);
    return events.FirstOrDefault(e =>
        e.Status == EventStatus.Started || e.Status == EventStatus.Paused);
}
```

`pageSize: 50` is safe — a typical user won't have 50+ scheduled events without one being active.

Tasks 1 and 2 can run in parallel (no file overlap).

---

### Task 3 — Service Layer: Add `GetPagedEventsForListAsync`

**Sequential after Tasks 1+2.**

**Files owned:**
- `Domain/ServicesInterfaces/IEventService.cs`
- `Services/EventService.cs`
- `MyVocaList.Tests/Unit/Services/EventServiceTests.cs` (add tests)

**New interface method:**
```csharp
Task<(IEnumerable<EventListItemDto> items, int totalCount)> GetPagedEventsForListAsync(
    int pageNumber, int pageSize, string? query, CancellationToken ct);
```

**Implementation** maps `Event` → `EventListItemDto` (field mapping is direct — all fields exist on entity + navigation property `Venue.Name`). Uses `_eventRepository.GetPagedAsync(pageNumber, pageSize, query, ct)`.

**TDD**: Write failing test first (Red), then implement (Green).
- `GetPagedEventsForListAsync_NoQuery_ReturnsAllPagedItems`
- `GetPagedEventsForListAsync_WithQuery_FiltersResults`
- `GetPagedEventsForListAsync_EmptyDb_ReturnsEmpty`

---

### Task 4 — QueueViewModel (CrudListViewModelBase)

**Sequential after Task 3.**

**Files owned:**
- `MyVocaList/UI/ViewModels/Queue/QueueViewModel.cs` (new)
- `MyVocaList.Tests/Unit/ViewModels/QueueViewModelTests.cs` (new)

**Design:**
```csharp
public partial class QueueViewModel : CrudListViewModelBase<EventListItemDto>
{
    // FabCommand → NavigateToCreateQueueAsync (GoToAsync Routes.QueueForm)
    // ItemTappedCommand → GoToAsync($"queue-management/{item.Id}")
    // DeleteItemsCommand → IEventService.DeleteAsync (batch delete Created events only)
    // InitializeAsync → GetPagedEventsForListAsync
    // SearchAsync → GetPagedEventsForListAsync with query
}
```

**TDD** (at minimum):
- `InitializeAsync_EmptyDb_SetsIsEmptyNoItems`
- `InitializeAsync_WithEvents_PopulatesCollection`
- `FabCommand_Executes_NavigatesToQueueForm`
- `ItemTapped_NavigatesToQueueManagement_WithEventId`

---

### Task 5 — QueueFormViewModel + QueueFormPage

**Sequential after Task 4.**

**Files owned:**
- `MyVocaList/UI/ViewModels/Queue/QueueFormViewModel.cs` (new)
- `MyVocaList/UI/Pages/Queue/QueueFormPage.xaml` (new)
- `MyVocaList/UI/Pages/Queue/QueueFormPage.xaml.cs` (new)
- `MyVocaList.Tests/Unit/ViewModels/QueueFormViewModelTests.cs` (new)

**QueueFormPage fields:**
| Field | Control | Default | Constraint |
|-------|---------|---------|-----------|
| Queue Name | `DXTextEdit` | empty | required, 1–100 chars |
| Venue | `AutocompleteField` (existing component) | empty | required |
| Date | `DXDateEdit` | today | required |
| Start Time | `DXTimeEdit` | current time | required |
| End Time | `DXTimeEdit` | current time + 4h | required, must be after start |
| Mode | `DXComboBoxEdit` | Video Karaoke | required; items: "Video Karaoke" / "Bandokê" → values: `"VideoKaraoke"` / `"Bandoke"` |

**On Save:** `IEventService.CreateEventAsync(...)` → on success → `GoToAsync($"queue-management/{newEvent.Id}")` (takes user directly into the new queue).

**TDD:**
- `SaveCommand_EmptyName_ShowsValidationError`
- `SaveCommand_NoVenue_ShowsValidationError`
- `SaveCommand_EndBeforeStart_ShowsValidationError`
- `SaveCommand_ValidInputs_NavigatesToQueueManagement`

---

### Task 6 — Redesign QueuePage.xaml as CRUD List

**Sequential after Task 5.**

**Files owned:**
- `MyVocaList/UI/Pages/Queue/QueuePage.xaml` (full redesign)
- `MyVocaList/UI/Pages/Queue/QueuePage.xaml.cs` (change base class, inject ViewModel)

**QueuePage.xaml structure:**
```xaml
<!-- Extends CrudListPageBase -->
<crud:CrudListView
    FabIcon="add"
    SearchPlaceholder="Search queues"
    EmptyNoItemsTitle="No queues yet"
    EmptyNoItemsSubtitle="Tap + to create your first queue"
    EmptyNoResultsTitle="No queues found"
    ItemsSource="{Binding Items}"
    ...>
    <crud:CrudListView.ItemTemplate>
        <DataTemplate x:DataType="dto:EventListItemDto">
            <lists:ListItem
                Headline="{Binding Name}"
                SupportingText="{Binding VenueName}">
                <lists:ListItem.LeadingContent>
                    <local:ListItemLeadingIcon IconSource="queue_music_outlined" />
                </lists:ListItem.LeadingContent>
                <lists:ListItem.TrailingContent>
                    <!-- Status label: Created/Started/Paused/Finished -->
                    <Label Text="{Binding Status, Converter={StaticResource EventStatusConverter}}" />
                </lists:ListItem.TrailingContent>
            </lists:ListItem>
        </DataTemplate>
    </crud:CrudListView.ItemTemplate>
</crud:CrudListView>

<!-- Preserve exit-app sheet (QueuePage is Shell root) -->
<dx:BottomSheet x:Name="exitConfirmSheet" ...>...</dx:BottomSheet>
```

`QueuePage.xaml.cs` extends `CrudListPageBase`, overrides `OnBackButtonPressed` to show `exitConfirmSheet` (preserves existing exit-app behavior — QueuePage is the Shell root).

**Status display** requires a new `EventStatusConverter` (IValueConverter, string → display label).

---

### Task 7 — DI / Routing / Cleanup

**Sequential after Task 6.**

**Files owned:**
- `MyVocaList/MauiProgram.cs`
- `MyVocaList/AppShell.xaml.cs`
- `MyVocaList/Navigation/Routes.cs` (or wherever `Routes` is defined)
- `MyVocaList/Navigation/NavigationConfig.cs`
- `MyVocaList/AppShell.xaml`
- `MyVocaList.sln`

**Changes:**
1. `MauiProgram.cs`: register `QueueViewModel`, `QueueFormPage`, `QueueFormViewModel` as `AddTransient`
2. `Routes.cs`: add `public const string QueueForm = "queue-form";`
3. `AppShell.xaml.cs`: `Routing.RegisterRoute(Routes.QueueForm, typeof(QueueFormPage));`
4. `AppShell.xaml`: remove `EventsPage` FlyoutItem (Events page no longer exists)
5. `NavigationConfig.cs`: remove `events` route entry
6. `MyVocaList.sln`: add `QueueFormPage.xaml`, `QueueFormPage.xaml.cs`, `QueueFormViewModel.cs` entries; remove `EventsPage` entries

**EventsPage deletion:** Delete placeholder files:
- `MyVocaList/UI/Pages/Events/EventsPage.xaml`
- `MyVocaList/UI/Pages/Events/EventsPage.xaml.cs`
- Any `EventsViewModel.cs` if it exists

---

## Wave Structure (for subagent dispatch)

```
Wave A (parallel): Task 1 + Task 2      → QueueListItem migration + GetActiveEventAsync fix
Wave B:            Task 3               → Service layer GetPagedEventsForListAsync + tests
Wave C:            Task 4               → QueueViewModel + tests
Wave D:            Task 5               → QueueFormViewModel + QueueFormPage + tests
Wave E:            Task 6               → QueuePage.xaml redesign
Wave F:            Task 7               → DI/routing/cleanup
```

---

## Verification

1. `dotnet build` → 0 errors
2. `dotnet test` → 0 failures
3. Emulator smoke test:
   - Tap "Queue" in flyout → `CrudListView` list page appears with FAB
   - Tap FAB → `QueueFormPage` opens; fill in name + venue + dates + mode → Save → navigates to `QueueManagementPage`
   - Back to queue list → new queue appears with correct name, venue, status "Created"
   - Tap queue in list → opens `QueueManagementPage` for that queue
   - Tap QueueManagementPage back → returns to queue list (NOT exit-app)
   - On queue list, press Android back → exit-app ConfirmSheet appears
   - Queue list items show position badge, singer name, song title correctly in `QueueManagementPage`

## Assumptions / Open Items

- `Mode` string values: `"VideoKaraoke"` and `"Bandoke"` — display as "Video Karaoke" / "Bandokê". Confirm if a second mode string exists in the codebase (agent found only `"VideoKaraoke"` as default; no other value was observed).
- `EventStatusConverter`: new `IValueConverter` mapping `EventStatus` enum → display string (e.g., `Started` → "Active"). Lives in `MyVocaList/UI/Converters/`.
- Swipe-delete on queue list: only for `EventStatus.Created` queues (not started/paused/finished). Implementation decision for Task 4.

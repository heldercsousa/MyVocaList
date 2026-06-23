# Queue Feature: QueueListItem Migration + Entry Point Fix

## Context

Two structural problems in the Queue feature identified by Helder:

1. **`QueueListItem.xaml` bypasses the `ListItem` pattern** — it is a domain-specific, zero-BindableProperty component that hard-binds directly to `QueueEntryViewModel` fields. Every other list page in the app uses `ListItem` (10 consumers). `QueueListItem` was scaffolded during Wave 4 as a placeholder with a `<!-- Status: will be filled in Wave 4B -->` comment. Wave 4B shipped via Song Import but the component was never migrated. It sits at the root of `UI/Components/` instead of the `Lists/` subfolder and has a namespace mismatch in its only consumer.

2. **`QueuePage.xaml` is a placeholder with no navigation** — the "Queue" flyout item leads to a card reading "This page is under construction." The fully-implemented `QueueManagementPage` exists at route `"queue-management"` but is unreachable because `EventsPage` (the intended entry via event selection) is also a placeholder.

All changes must be done in a git worktree, not directly on `develop`.

---

## Exploration Findings

### QueueListItem (Problem 1)

| Aspect | Detail |
|--------|--------|
| Location | `MyVocaList/UI/Components/QueueListItem.xaml` (root, not `Lists/`) |
| BindableProperties | **Zero** — binds directly to `QueueEntryViewModel.{Position, PersonName, SongTitle, Status}` |
| Consumers | `QueueManagementPage.xaml` only — 2 DataTemplates (Next Up list + History list) |
| Namespace bug | Consumer declares `xmlns:queue="clr-namespace:MyVocaList.UI.Components.Queue"` — the `.Queue` sub-namespace does not exist; `QueueListItem` is in `MyVocaList.UI.Components` |
| Status column | Has a `<!-- will be filled during Wave 4B -->` comment — no actual content to migrate |
| `CurrentSingerCard` | Also in `MyVocaList.UI.Components` (same namespace), uses the same `queue:` alias — the alias must be updated, not removed |

**ListItem mapping:**

| QueueListItem field | ListItem slot |
|---------------------|---------------|
| `Position` (18pt bold number) | `LeadingContent` — a styled `Label` |
| `PersonName` | `Headline` |
| `SongTitle` | `SupportingText` |
| `Status` | `TrailingContent` — deferred (no content to migrate yet) |

Not a governed-component change: `ListItem` is not being modified — only a new consumer is added. `QueueListItem` has one consumer, so removing it does not require the 4-gate process.

### QueuePage Navigation (Problem 2)

| Aspect | Detail |
|--------|--------|
| Route `"queue"` | Shell root FlyoutItem; `AppShellViewModel` handles it via `PopToRootAsync` |
| `QueuePage.xaml.cs` | Has `OnBackButtonPressed` → shows exit-app `ConfirmSheet` → `Application.Current.Quit()`. No ViewModel. |
| `QueueManagementPage` route | `"queue-management"` — registered as FlyoutItem in AppShell (hidden), also navigated as `"queue-management/{eventId}"` |
| `QueueManagementPage.xaml.cs` | `OnAppearing` parses last URL segment as `int eventId`, calls `_viewModel.InitializeCommand.ExecuteAsync(eventId)` |
| `QueueManagementViewModel.InitializeAsync` | Calls `GetActiveEventAsync()`, validates that `activeEvent.Id == eventId`. Shows "Event not found" snackbar if mismatch or null. |
| **`GetActiveEventAsync` bug** | `EventService.GetActiveEventAsync()` calls `GetPagedAsync(1, 1, null)` — only fetches **1 event** (most recently scheduled) then checks its status in memory. If the active event is not the most recently scheduled, returns `null` even though an active event exists. |
| `PersonPickerPage`/`QueueSongPickerPage` | Sub-pages of `QueueManagementPage`; reached via `GoToAsync("person-picker?eventId=X")` and `GoToAsync("song-picker?entryId=X")`. Not entry-point pages. |

---

## Plan

### Worktree Setup

Create a new worktree off `develop`:
```
git worktree add .worktrees/fix/queue-entry-and-listitem origin/develop -b fix/queue-entry-and-listitem
```

---

### Task 1 — Replace QueueListItem with ListItem

**Files owned:** `QueueManagementPage.xaml`, `QueueListItem.xaml`, `QueueListItem.xaml.cs`, `MyVocaList.sln`

**Steps:**
1. In `QueueManagementPage.xaml`:
   - Add `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists;assembly=MyVocaList"`
   - Fix the `xmlns:queue` alias: change from `clr-namespace:MyVocaList.UI.Components.Queue` to `clr-namespace:MyVocaList.UI.Components` (needed for `CurrentSingerCard` which uses the same alias)
   - Replace both `<queue:QueueListItem />` DataTemplate usages with `<lists:ListItem>`:
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
2. Delete `QueueListItem.xaml` and `QueueListItem.xaml.cs`
3. Remove both files from `MyVocaList.sln` (SolutionItems section)
4. Build → confirm 0 errors

---

### Task 2 — Fix `GetActiveEventAsync` in EventService

**Files owned:** `MyVocaList.Services/EventService.cs`

**Problem:** `GetPagedAsync(1, 1, null)` fetches only the single most-recently-scheduled event and checks its status. Any active event that isn't the most recently scheduled is missed.

**Fix:** Change the call to fetch enough events to find an active one:
```csharp
public async Task<Event?> GetActiveEventAsync(CancellationToken ct)
{
    var (events, _) = await _eventRepository.GetPagedAsync(1, 50, null, ct);
    return events.FirstOrDefault(e =>
        e.Status == EventStatus.Started || e.Status == EventStatus.Paused);
}
```
`pageSize: 50` is safe — a user is unlikely to have 50+ events scheduled without any being the active one. A proper DB-side status filter (`IEventRepository.GetActiveEventAsync`) would be cleaner long-term but is out of scope here.

No test change needed — this is a bug fix to an existing behaviour, and the Queue unit tests already cover `GetActiveEventAsync` usage via mocks.

---

### Task 3 — Make QueuePage navigate to QueueManagementPage

**Files owned:** `QueuePage.xaml`, `QueuePage.xaml.cs`

**Design:** `QueuePage` becomes a routing page. On `OnNavigatedTo` it calls `IQueueServiceNew.GetActiveEventAsync()` (via `IEventService`) and navigates to `QueueManagementPage`. It keeps the exit-app `ConfirmSheet` for the "no active event" state (when the user lands and stays on `QueuePage`).

**`QueuePage.xaml.cs` changes:**
- Inject `IEventService` via constructor
- Override `OnNavigatedTo` (not `OnAppearing` — avoids double-fire on sub-page pop):
  ```csharp
  protected override async void OnNavigatedTo(NavigatedToEventArgs args)
  {
      base.OnNavigatedTo(args);
      var activeEvent = await _eventService.GetActiveEventAsync(CancellationToken.None);
      if (activeEvent != null)
          await Shell.Current.GoToAsync($"queue-management/{activeEvent.Id}");
  }
  ```
- Register `IEventService` injection in constructor (DI already registers it as `AddScoped`)

**`QueuePage.xaml` changes:**
- Replace the "under construction" placeholder content with a proper "No active session" empty state using `<states:EmptyState>` (the established pattern)
- Keep the `exitConfirmSheet` BottomSheet as-is (handles Android back → exit app)
- Add DI-friendly constructor parameter for `IEventService`

**`MauiProgram.cs`:** No change needed — `QueuePage` is already `AddTransient`. `IEventService` is already `AddScoped`.

> **Open question for Helder (before implementation):** When no active event exists and the user lands on QueuePage, should the empty state include a **"Start New Queue" action button** (requires exploring EventService.CreateEventAsync to understand what parameters are needed), or just show a static "No active session — go to Events to start one" message with no action?

---

## Verification

1. `dotnet build` → 0 errors
2. `dotnet test` → 0 failures (queue unit tests cover QueueManagementViewModel + QueueServiceNew)
3. Emulator smoke test:
   - Tap "Queue" in flyout → if no active event: `EmptyState` shown, back button shows exit confirmation
   - Tap "Queue" in flyout → if active event: auto-redirects to `QueueManagementPage` with singer list
   - Drag to reorder in Next Up list → ListItem renders correctly with position badge, name, song

## Files Changed Summary

| File | Change |
|------|--------|
| `MyVocaList/UI/Pages/Queue/QueueManagementPage.xaml` | Replace 2× `QueueListItem` with `ListItem`; fix `xmlns:queue` alias |
| `MyVocaList/UI/Components/QueueListItem.xaml` | **Deleted** |
| `MyVocaList/UI/Components/QueueListItem.xaml.cs` | **Deleted** |
| `MyVocaList.sln` | Remove QueueListItem entries |
| `MyVocaList.Services/EventService.cs` | Fix `GetActiveEventAsync` page size bug |
| `MyVocaList/UI/Pages/Queue/QueuePage.xaml` | Replace placeholder with `EmptyState`; keep exit `ConfirmSheet` |
| `MyVocaList/UI/Pages/Queue/QueuePage.xaml.cs` | Inject `IEventService`; add `OnNavigatedTo` auto-redirect logic |

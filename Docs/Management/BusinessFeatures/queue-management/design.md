# Queue Management — Design

## Architecture Overview

### IA Option Selected: Queue-First (Primary View)
**Rationale:** MVP prioritizes fast queue operations (register, drag, enqueue) over multi-event management. Single active event per session. Minimal navigation (0 taps to reach queue actions). Scales to multi-event management in v1.1 via event switcher.

---

## Layers & Dependencies

```
Domain (Contracts)
  ├─ Event (entity, aggregate root)
  ├─ QueueEntry (value object, part of Event aggregate)
  ├─ EventStatus enum
  ├─ QueueEntryStatus enum
  └─ EventDto, QueueEntryDto (contracts)

Infra (EF Core)
  ├─ Event entity configuration
  ├─ QueueEntry entity configuration
  ├─ EventRepository : IEventRepository
  └─ EF Core migrations (new schema)

Services
  ├─ IEventService (CRUD, state transitions)
  ├─ IQueueService (enqueue, reorder, register)
  └─ EventService, QueueService (implementations)

UI (MAUI)
  ├─ EventsPage (entry point; TBD: new Events list or integrate into Venues)
  ├─ QueueManagementPage (primary queue UI)
  ├─ QueueManagementViewModel
  ├─ Custom Components:
  │  ├─ CurrentSingerCard (displaying current + song + timer)
  │  └─ QueueListItem (queue entry with drag handle)
  └─ Dialogs/Modals:
     ├─ Person Picker (reuse search-picker)
     ├─ Song Picker (reuse search-picker)
     └─ Confirmation dialogs (finish event, mark absent)
```

---

## Data Model (EF Core)

### Entity: Event
```csharp
public class Event : IAggregateRoot
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public required string Name { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public EventStatus Status { get; set; } // CREATED, STARTED, PAUSED, FINISHED
    public string Mode { get; set; } = "VideoKaraoke"; // "VideoKaraoke" | "ArtistInstruments"
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    
    // Navigation
    public Venue Venue { get; set; } = null!;
    public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
}

public enum EventStatus
{
    Created = 0,
    Started = 1,
    Paused = 2,
    Finished = 3,
}
```

### Entity: QueueEntry
```csharp
public class QueueEntry
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int PersonId { get; set; }
    public int? SongId { get; set; }
    public int Position { get; set; } // Order in queue (0-indexed)
    public QueueEntryStatus Status { get; set; } // PENDING, PERFORMING, COMPLETED, ABSENT, CANCELLED
    public DateTime? PerformanceStartTime { get; set; }
    public DateTime? PerformanceEndTime { get; set; }
    public double? PerformanceDurationMinutes { get; set; } // Calculated: (end - start).TotalMinutes
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    
    // Navigation
    public Event Event { get; set; } = null!;
    public Person Person { get; set; } = null!;
    public Song? Song { get; set; }
}

public enum QueueEntryStatus
{
    Pending = 0,
    Performing = 1,
    Completed = 2,
    Absent = 3,
    Cancelled = 4,
}
```

### EF Configuration
```csharp
// Event
modelBuilder.Entity<Event>(eb =>
{
    eb.HasKey(e => e.Id);
    eb.Property(e => e.Name).IsRequired().HasMaxLength(100);
    eb.Property(e => e.Status).HasDefaultValue(EventStatus.Created);
    eb.Property(e => e.Mode).HasDefaultValue("VideoKaraoke");
    eb.HasIndex(e => new { e.VenueId, e.Name, e.ScheduledStartTime })
        .HasName("IX_Event_VenueNameDate")
        .IsUnique()
        .HasFilter("[Status] <> 3"); // Unique per venue/name/date, excluding FINISHED events
    eb.HasMany(e => e.QueueEntries)
        .WithOne(qe => qe.Event)
        .HasForeignKey(qe => qe.EventId)
        .OnDelete(DeleteBehavior.Cascade);
});

// QueueEntry
modelBuilder.Entity<QueueEntry>(eb =>
{
    eb.HasKey(qe => qe.Id);
    eb.HasOne(qe => qe.Person).WithMany().HasForeignKey(qe => qe.PersonId);
    eb.HasOne(qe => qe.Song).WithMany().HasForeignKey(qe => qe.SongId).IsRequired(false);
    eb.Property(qe => qe.Status).HasDefaultValue(QueueEntryStatus.Pending);
    eb.HasIndex(qe => new { qe.EventId, qe.PersonId })
        .HasName("IX_QueueEntry_EventPerson")
        .IsUnique()
        .HasFilter("[Status] <> 4"); // Unique event+person, excluding CANCELLED
    eb.HasIndex(qe => qe.EventId).IncludeProperties(qe => qe.Position, qe => qe.Status);
});
```

---

## Page Architecture

### QueueManagementPage (Primary)
**Purpose:** Host manages active event queue — register performers, enqueue singers, drag-reorder, mark absent.

**Layout (Wireframe):**
```
┌─────────────────────────────────────────┐
│ Event Ctrl Bar [TBD: event name, status]│ (collapse when scrolling)
│ Status: STARTED | 14:32 elapsed         │
├─────────────────────────────────────────┤
│                                         │
│ 👤 CURRENT SINGER CARD                  │
│ ┌─────────────────────────────────────┐ │
│ │ João Silva                          │ │
│ │ Song: "Imagine" (if selected)       │ │
│ │ ⏱️  2:15 elapsed                     │ │
│ │                                     │ │
│ │ [Register Participation] OR         │ │
│ │ [Stop Performance] (if timing)      │ │
│ │ [Select Song] [Mark Absent]         │ │
│ └─────────────────────────────────────┘ │
│                                         │
├─────────────────────────────────────────┤
│                                         │
│ 📋 NEXT UP (Drag-reorderable)           │
│ ┌─────────────────────────────────────┐ │
│ │ ≡ 1. Maria (song not selected)      │ │
│ │ ≡ 2. Carlos (song not selected)     │ │
│ │ ≡ 3. Ana (song not selected)        │ │
│ │ ... (scroll for more)               │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ [+ Add Singer]                          │
│                                         │
├─────────────────────────────────────────┤
│ Quick Actions:                          │
│ [Pause] [End Event] [⋮ More]            │
└─────────────────────────────────────────┘
```

**Components:**

#### 1. **Event Control Bar** (collapsible, sticky on scroll)
- Displays: Event name, venue, status (STARTED/PAUSED), elapsed time
- Actions: [Pause] / [Resume] button (toggle), [Finish Event], [⋮ More]
- Visual hierarchy: high priority, always visible when event active
- Collapse behavior: collapse header on scroll down; expand on scroll up (iOS Maps pattern)

#### 2. **CurrentSingerCard** (custom component)
```xaml
<!-- Pseudocode XAML -->
<Grid Padding="16" BackgroundColor="Surface" CornerRadius="8">
    <VerticalStackLayout>
        <!-- Avatar + Name -->
        <HorizontalStackLayout>
            <Ellipse Width="48" Height="48">
                <!-- Monogram from person.Initials -->
            </Ellipse>
            <VerticalStackLayout>
                <Label Text="{Binding CurrentSinger.Person.FullName}" FontSize="18" FontAttributes="Bold" />
                <Label Text="PERFORMING" FontSize="12" TextColor="Secondary" />
            </VerticalStackLayout>
        </HorizontalStackLayout>
        
        <!-- Song (if selected) -->
        <Label Text="{Binding CurrentSinger.Song.Title, StringFormat='🎵 {0}'}" 
               IsVisible="{Binding CurrentSinger.SongId, Converter={StaticResource IsNotNullConverter}}" />
        
        <!-- Timer -->
        <Label Text="{Binding PerformanceElapsedTime, StringFormat='⏱️  {0:mm\\:ss}'}" 
               FontSize="16" />
        
        <!-- Action Buttons -->
        <HorizontalStackLayout Spacing="8">
            <Button Text="{Binding PerformanceButtonText}" 
                    Command="{Binding TogglePerformanceCommand}" 
                    BackgroundColor="Primary" />
            <Button Text="Select Song" Command="{Binding SelectSongCommand}" 
                    BackgroundColor="Secondary" />
            <Button Text="Mark Absent" Command="{Binding MarkAbsentCommand}" 
                    BackgroundColor="Error" />
        </HorizontalStackLayout>
    </VerticalStackLayout>
</Grid>
```

- Shows: person name, song (if selected), performance timer (MM:SS format)
- Buttons: [Register Participation] (PENDING) → [Stop Performance] (PERFORMING)
- Secondary: [Select Song], [Mark Absent]
- Visual: Card style (DevExpress, MD3), prominent display

#### 3. **QueueListView** (DXCollectionView, drag-reorderable)
```xaml
<!-- Pseudocode -->
<dx:DXCollectionView ItemsSource="{Binding NextUpQueue}" 
                      AllowDragDrop="True"
                      DragDropStartingCommand="{Binding DragStartCommand}"
                      DragDropCompletedCommand="{Binding DragCompletedCommand}">
    <dx:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="vm:QueueEntryViewModel">
            <Grid Padding="8" BackgroundColor="Surface">
                <HorizontalStackLayout Spacing="12">
                    <Label Text="≡" FontSize="20" VerticalOptions="Center" />
                    <VerticalStackLayout>
                        <Label Text="{Binding Position, StringFormat='{0}.'}" FontSize="14" />
                        <Label Text="{Binding PersonName}" FontSize="16" FontAttributes="Bold" />
                        <Label Text="{Binding SongDisplayText}" FontSize="12" TextColor="Secondary" />
                    </VerticalStackLayout>
                </HorizontalStackLayout>
            </Grid>
        </DataTemplate>
    </dx:DXCollectionView.ItemTemplate>
</dx:DXCollectionView>
```

- Shows: position, name, song (if selected), status badge (PENDING | PERFORMING | COMPLETED | ABSENT)
- Drag handle (≡ icon) on left
- Drag-reorder enabled for PENDING singers only
- CURRENT (PERFORMING) not draggable
- COMPLETED/ABSENT in separate History section (not draggable with active queue)
- Visual: list item style (MD3), consistent with other list pages

#### 4. **+ Add Singer Button** (FloatingActionButton or action row)
- Location: Below queue list, sticky
- Action: Opens Person Picker modal
- Visual: FAB (floating action button) or button in row below queue

---

### Person Picker Modal (Reuse Search-Picker Component)
**Purpose:** Search and select a person to enqueue.

**Behavior:**
- Shows search field (placeholder: "Search singers...")
- Shows matching people from DB (case-insensitive name search)
- Tap person → enqueue, close modal, queue updates
- If no match + "Add Person" button visible → tap to create new person inline (or navigate to PersonFormPage)
- After create, person is enqueued; modal closes

**Implementation:** Reuse existing `ArtistPickerPage` pattern (from search-picker feature):
- `PersonPickerViewModel`: search logic, person selection command
- `PersonPickerPage.xaml`: modal UI
- Route: `/person-picker?mode=enqueue&eventId=42`
- Navigation result via `WeakReferenceMessenger<PersonPickerResult>`

---

### Song Picker Modal (Reuse Search-Picker Component)
**Purpose:** Search and select a song to assign to current singer.

**Behavior:**
- Shows search field (placeholder: "Search songs...")
- Shows matching songs from Artists/Songs catalog (case-insensitive on title + artist)
- Tap song → assign to queue entry, close modal, queue updates
- If no songs in DB → show "No songs available" + navigation hint

**Implementation:** Reuse `SongPickerPage` pattern:
- `SongPickerViewModel`: search logic
- Route: `/song-picker?mode=queue&queueEntryId=42`
- Result via `WeakReferenceMessenger<SongPickerResult>`

---

## ViewModel Contract

### QueueManagementViewModel : CrudListViewModelBase<QueueEntryViewModel>

**Observable Properties:**
```csharp
[ObservableProperty] private Event? currentEvent;
[ObservableProperty] private QueueEntryViewModel? currentSinger;
[ObservableProperty] private ObservableRangeCollection<QueueEntryViewModel> nextUpQueue;
[ObservableProperty] private ObservableRangeCollection<QueueEntryViewModel> history; // Completed + Absent

[ObservableProperty] private TimeSpan performanceElapsedTime; // Timer for current performance
[ObservableProperty] private string performanceButtonText; // "Register Participation" | "Stop Performance"
[ObservableProperty] private TimeSpan? estimatedCompletionTime;

[ObservableProperty] private bool isEventStarted;
[ObservableProperty] private bool isPaused;
[ObservableProperty] private string eventStatusDisplay; // "STARTED · 14:32 elapsed"

// From CrudListViewModelBase (inherited)
[ObservableProperty] private bool isLoading;
[ObservableProperty] private bool isRefreshing;
[ObservableProperty] private string searchText;
[ObservableProperty] private bool isSearchMode;
```

**Commands:**
```csharp
// Event lifecycle
[RelayCommand] public Task StartEventAsync();
[RelayCommand] public Task PauseEventAsync();
[RelayCommand] public Task ResumeEventAsync();
[RelayCommand] public Task FinishEventAsync();

// Queue operations
[RelayCommand] public Task RegisterParticipationAsync(); // PENDING → PERFORMING
[RelayCommand] public Task StopPerformanceAsync(); // PERFORMING → COMPLETED, advance queue
[RelayCommand] public Task MarkAbsentAsync(); // Current → ABSENT
[RelayCommand] public Task SelectSongAsync(); // Open song picker

// Enqueue
[RelayCommand] public Task OpenPersonPickerAsync(); // Open person picker

// Drag-reorder
[RelayCommand] public Task DragStartAsync(QueueEntryViewModel item);
[RelayCommand] public Task DragCompletedAsync(QueueEntryViewModel item, int newPosition);

// UI lifecycle
[RelayCommand] public Task InitializeAsync(int eventId);
```

**Methods (private, derived properties):**
```csharp
private void CalculateEstimatedCompletionTime()
{
    // If 5+ completed singers:
    // avg = sum(performance_durations) / count_completed
    // remaining = count_pending * avg
    // eta = now + remaining
    // Else: null
}

private void UpdatePerformanceTimer()
{
    // Runs every 1 second; updates PerformanceElapsedTime
    // Timer.Elapsed += UpdatePerformanceTimer
}

private async Task AdvanceQueue()
{
    // Move next PENDING to CURRENT (PERFORMING)
    // Update UI
}

partial void OnCurrentSingerChanged(QueueEntryViewModel? value)
{
    // When current singer changes, reset performance timer
    // Reset PerformanceElapsedTime = 0:00
    // Update PerformanceButtonText
}
```

---

## Navigation & Routing

### Entry Points

1. **From Venues Page (proposed for MVP)**
   - Each venue card shows "1 active event" badge if event.Status = STARTED
   - Tap badge → navigate to QueueManagementPage
   - Route: `/queue/{eventId}`

2. **From new Events List Page (post-MVP)**
   - Lists all events per venue
   - Filters: CREATED (draft), STARTED (active), FINISHED (archived)
   - Tap STARTED event → navigate to QueueManagementPage

3. **Direct Navigation (deep link)**
   - `/queue/{eventId}` — requires eventId parameter

### Route Registration (AppShell.xaml)
```xml
<!-- pseudo-code -->
<ShellContent Title="Queue" Route="queue" ContentTemplate="{DataTemplate local:QueueManagementPage}" />

<!-- Routes for modals -->
<ShellContent Title="PersonPicker" Route="person-picker" ... />
<ShellContent Title="SongPicker" Route="song-picker" ... />
```

### Modal Navigation Pattern
```csharp
// From QueueManagementPage, opening person picker:
await Shell.Current.GoToAsync($"person-picker?mode=enqueue&eventId={currentEvent.Id}");

// PersonPickerViewModel receives result, publishes via messenger:
WeakReferenceMessenger.Default.Send(new PersonPickerResult { SelectedPerson = person });

// QueueManagementViewModel subscribes:
WeakReferenceMessenger.Default.Register<PersonPickerResult>(this, OnPersonSelected);
```

---

## Interaction Flows

### Flow 1: Create Event → Start → Queue (Minimal)
```
[User] → "Create Event" (modal/form)
  → Enter: venue, name, date/time, mode
  → Save → Event created (status: CREATED)
[System] → Navigate to QueueManagementPage
[User] → "+ Add Singer" → Person Picker → Select "João"
  → João enqueued (position 1, status: PENDING)
[User] → [Start Event] → Event status: STARTED
[User] → [Register Participation] on João → João: PENDING → PERFORMING, timer starts
[User] → Wait 2:30 (simulated performance)
[User] → [Stop Performance] → João: PERFORMING → COMPLETED, timer stops, duration logged
         → Maria (next) becomes CURRENT
```

### Flow 2: Drag-Reorder Mid-Event
```
[User] → Queue shows: Maria (1), Carlos (2), Ana (3)
[User] → Long-press on Carlos (drag handle)
[System] → Visual feedback: drag cursor, highlight
[User] → Drag Carlos above Ana
[System] → Optimistic UI update: queue reorders to Maria (1), Ana (2), Carlos (3)
         → DB persists (immediate, no undo)
```

### Flow 3: Mark Absent
```
[User] → Current: João (PENDING, 0:00 elapsed, no timer running)
[User] → [Mark Absent]
[System] → Confirmation snackbar: "Mark João as Absent?" [Undo]
[User] → Tap to confirm (or wait 3s auto-dismiss)
[System] → João: PENDING → ABSENT
         → João moved to History section (read-only)
         → Maria (next) becomes CURRENT
         → UI updates
[User] → Tap [Undo] within 3s → João: ABSENT → PENDING, restored to queue at original position
```

### Flow 4: Finish Event
```
[User] → Event status: STARTED, queue has 12 singers (8 COMPLETED, 4 PENDING)
[User] → [Finish Event] in control bar
[System] → Confirmation dialog: "End event? Queue will be archived."
[User] → Confirm
[System] → Event: STARTED → FINISHED
         → ActualEndTime = now
         → Calculate completion stats: 8 completed, 0 absent, avg duration, total time
         → Queue locked to read-only (no enqueue, drag, register buttons)
         → Event removed from active view; appears in history/analytics (future feature)
```

---

## Timing & Performance

### Performance Requirements
- **Register Participation action:** ≤ 200ms perceived latency (UI updates immediately, DB save async)
- **Stop Performance & advance queue:** ≤ 300ms (timer stops, next singer loaded, UI updates)
- **Drag-reorder:** optimistic UI update < 50ms, DB persist async
- **Performance timer:** accurate to ±1 second over 5+ min duration

### Timer Implementation
- `DispatcherTimer` or `Task.Run` with `Task.Delay(1000)` loop
- Updates `PerformanceElapsedTime` every 1s
- Stop timer when `PerformanceEndTime` is set
- Resume timer when `PerformanceStartTime` changes

---

## State Management & Persistence

### Local State (ViewModel)
- `CurrentEvent`, `CurrentSinger`, `NextUpQueue`, `History` — in-memory after Load
- `PerformanceElapsedTime` — local timer, not persisted (calculated on page load)

### Persistent State (Database)
- Event status, ActualStartTime, ActualEndTime
- QueueEntry status, position, PerformanceStartTime, PerformanceEndTime, SongId
- All persisted immediately on action (no transaction required; optimistic UI)

### Session Resumption
- On app resume, load current active event by `eventId` parameter (or from session memory)
- Reload queue entries from DB
- Recalculate `PerformanceElapsedTime` if PERFORMING (start_time to now)
- Resume timer

---

## Error Handling

### Expected Failures & Recovery

| Error | Scenario | Recovery |
|-------|----------|----------|
| Event not found | eventId invalid or event deleted | Show error snackbar, navigate back to Venues |
| Person not in DB | Enqueue fails (race condition) | Retry enqueue; show "Singer no longer available" |
| Unique constraint violation | Same person enqueued twice | Show snackbar "Singer already in queue" |
| DB save timeout | Slow network, operation > 5s | Retry with exponential backoff; show "Saving..." toast |
| Drag-reorder conflict | Concurrent drag from another device | Reload queue from DB; show "Queue updated by another user" |

### User-Facing Messages
- ✅ Success: "João enqueued" (snackbar, 2s auto-dismiss)
- ⚠️ Warning: "Queue updated by another user" (toast, stay visible)
- ❌ Error: "Failed to mark absent. Please try again." (snackbar with [Retry] button)

---

## Accessibility Requirements (WCAG 2.4)

- [ ] All buttons have descriptive labels (not icon-only)
- [ ] Performance timer is screen-reader announced every 5s (or on pause/resume)
- [ ] Drag-reorder can be done via keyboard (Tab to item, Enter to drag, Arrow keys to move, Enter to drop)
- [ ] Modal focus trap: person picker modal keeps focus inside until dismissed
- [ ] Color not sole indicator: "PERFORMING" status uses both color + badge label
- [ ] Touch target size: all buttons ≥ 48dp (MD3 standard)

---

## Decisions & Trade-Offs

### Decision 1: Queue-First IA (vs. Event-Centric)
- **Chosen:** Queue-First (Option 1)
- **Rationale:** MVP prioritizes speed; single active event per session; minimal navigation
- **Future:** Evolve to dual-pane dashboard (Option 3) post-MVP when multi-event management required

### Decision 2: Real-Time Queue Sync (vs. Optimistic + Reload)
- **Chosen:** Optimistic UI + reload on resume (MVP)
- **Rationale:** No WebSocket/SignalR infrastructure required; acceptable for single-host scenarios
- **Future:** Implement SignalR/WebSocket for multi-host live sync in v1.1

### Decision 3: Drag-Reorder Scope (PENDING only, not COMPLETED)
- **Chosen:** PENDING singers only; COMPLETED/ABSENT in separate read-only History
- **Rationale:** Prevents confusion; COMPLETED singers shouldn't move in active queue; cleaner UI
- **Alternative:** Allow drag to reorder COMPLETED (rejected: creates position ambiguity)

### Decision 4: Song Selection Timing
- **Chosen:** Optional before or during performance; can change mid-performance
- **Rationale:** Accommodates both modes (Video Karaoke = pre-selected, Artist Instruments = on-the-fly)
- **Alternative:** Mandate pre-selection before register (rejected: slower UX)

---

## Future Enhancements (Post-MVP)

1. **v1.1: Multi-Event Dashboard**
   - Dual-pane (Option 3): event sidebar + queue canvas
   - Event switcher for multiple concurrent events

2. **v1.2: Real-Time Sync**
   - SignalR/WebSocket for multi-device queue updates
   - Live queue broadcast to spectators (future: public display mode)

3. **v2.0: Analytics Dashboard**
   - Completion time estimates per singer history
   - Event retrospectives (who performed, how long, songs played)
   - Performance trends

4. **v2.1: Karaoke Software Integration**
   - Webhook to trigger video playback on song selection
   - Audio stream management (volume, backing track selection)

5. **v3.0: Singer Self-Registration**
   - QR code / public link for singers to join queue
   - SMS/WhatsApp integration for queue notifications

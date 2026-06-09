# Queue Management — Implementation Plan

**Feature:** Queue Management (MVP)  
**Status:** 🗺️ Plan (Ready for Implementation)  
**Estimated Duration:** 3–4 weeks (parallel waves, 2–4 subagents per wave)  
**Approval:** Spec approved 2026-06-09

---

## Overview

This plan breaks the Queue Management spec (requirements.md + design.md) into implementation waves following DRY Onion architecture: Domain → Infra → Services → UI → Tests.

Each wave produces a complete, testable layer. Waves are executed sequentially; phases within a wave run in parallel where dependencies allow.

---

## Wave 1: Domain Contracts (1.5 days)

**Goal:** Define entities, enums, repository interfaces, and DTOs.

**Dispatch:** 1 subagent (all 4 phases parallel where possible)

### Phase 1.1: Event Entity + EventStatus Enum
- File: `MyVocaList.Domain/Entities/Event.cs`
- File: `MyVocaList.Domain/Enums/EventStatus.cs`
- Enum values: `Created=0, Started=1, Paused=2, Finished=3`
- Event properties: Id, VenueId (FK), Name (required, ≤100), ScheduledStartTime, ScheduledEndTime, ActualStartTime?, ActualEndTime?, Status, Mode (default "VideoKaraoke"), CreatedAt, ModifiedAt, Venue nav, QueueEntries nav
- Implement `IAggregateRoot` marker interface

### Phase 1.2: QueueEntry Entity + QueueEntryStatus Enum
- File: `MyVocaList.Domain/Entities/QueueEntry.cs`
- File: `MyVocaList.Domain/Enums/QueueEntryStatus.cs`
- Enum values: `Pending=0, Performing=1, Completed=2, Absent=3, Cancelled=4`
- QueueEntry properties: Id, EventId (FK), PersonId (FK), SongId? (FK), Position (int), Status, PerformanceStartTime?, PerformanceEndTime?, PerformanceDurationMinutes?, CreatedAt, ModifiedAt, Event nav, Person nav, Song nav

### Phase 1.3: Repository Interfaces
- File: `MyVocaList.Domain/Interfaces/IEventRepository.cs`
  - Methods: `GetByIdAsync(int id, CancellationToken ct)`, `GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct)`, `AddAsync(Event entity, CancellationToken ct)`, `UpdateAsync(Event entity, CancellationToken ct)`, `DeleteAsync(int id, CancellationToken ct)`, `ExistsByNameAsync(int venueId, string name, DateTime date, CancellationToken ct)`
- File: `MyVocaList.Domain/Interfaces/IQueueRepository.cs`
  - Methods: `GetByIdAsync(int id, CancellationToken ct)`, `GetByEventIdAsync(int eventId, CancellationToken ct)`, `AddAsync(QueueEntry entry, CancellationToken ct)`, `UpdateAsync(QueueEntry entry, CancellationToken ct)`, `DeleteAsync(int id, CancellationToken ct)`, `ReorderAsync(int eventId, IEnumerable<(int entryId, int position)> newPositions, CancellationToken ct)`

### Phase 1.4: DTOs (Contracts.DTOs)
- File: `MyVocaList.Contracts/DTOs/Event/EventDto.cs` (all Event properties)
- File: `MyVocaList.Contracts/DTOs/Event/EventListItemDto.cs` (display: Id, Name, VenueName, ScheduledStartTime, Status, Mode)
- File: `MyVocaList.Contracts/DTOs/Queue/QueueEntryDto.cs` (all QueueEntry properties + nested PersonDto, SongDto)
- File: `MyVocaList.Contracts/DTOs/Queue/QueueEntryListItemDto.cs` (display: Id, Position, PersonName, SongTitle, Status, PerformanceDurationMinutes)

**Verification:** All files compile; no build errors.

---

## Wave 2: Infrastructure & EF Core (2 days)

**Goal:** Create database schema, implement repositories.

**Dispatch:** 2 subagents in parallel (Phase 2.2 & 2.3 after 2.1 completes)

### Phase 2.1: EF Core Configuration + Migration
- File: Update `MyVocaList.Infra/AppDbContext.cs`
  - Add: `public DbSet<Event> Events { get; set; }`
  - Add: `public DbSet<QueueEntry> QueueEntries { get; set; }`
  - In `OnModelCreating()`:
    - Event: HasKey(e => e.Id), Property(e => e.Name).IsRequired().HasMaxLength(100), HasIndex(e => new { e.VenueId, e.Name, e.ScheduledStartTime }).IsUnique().HasFilter("[Status] <> 3"), HasMany(e => e.QueueEntries).WithOne(qe => qe.Event).HasForeignKey(qe => qe.EventId).OnDelete(DeleteBehavior.Cascade)
    - QueueEntry: HasKey(qe => qe.Id), HasOne(qe => qe.Person).WithMany().HasForeignKey(qe => qe.PersonId), HasOne(qe => qe.Song).WithMany().HasForeignKey(qe => qe.SongId).IsRequired(false), Property(qe => qe.Status).HasDefaultValue(QueueEntryStatus.Pending), HasIndex(qe => new { qe.EventId, qe.PersonId }).IsUnique().HasFilter("[Status] <> 4"), HasIndex(qe => qe.EventId).IncludeProperties(qe => qe.Position, qe => qe.Status)
- Run: `dotnet ef migrations add AddEventAndQueueEntities`
- Run: Verify migration file is created at `MyVocaList.Infra/Migrations/*_AddEventAndQueueEntities.cs`

**Verification:** Migration compiles; `dotnet ef database update` succeeds without errors.

### Phase 2.2: EventRepository Implementation
- File: `MyVocaList.Infra/Repositories/EventRepository.cs`
  - Implement `IEventRepository`
  - `GetByIdAsync`: Include(e => e.QueueEntries).Include(e => e.Venue)
  - `GetPagedAsync`: Pagination using `.Skip().Take()`, optional filter by status
  - `AddAsync`: Validate name unique per venue per date using `ExistsByNameAsync`, then persist
  - `UpdateAsync`: Update ModifiedAt timestamp
  - `DeleteAsync`: EF cascade handles QueueEntry cleanup
  - `ExistsByNameAsync`: Query with case-insensitive collation

**Verification:** CRUD operations work; queries return correct results; uniqueness enforced.

### Phase 2.3: QueueRepository Implementation
- File: `MyVocaList.Infra/Repositories/QueueRepository.cs`
  - Implement `IQueueRepository`
  - `GetByIdAsync`: Include(qe => qe.Person).Include(qe => qe.Song).Include(qe => qe.Event)
  - `GetByEventIdAsync`: OrderBy(qe => qe.Position), filter by status if needed
  - `AddAsync`: Auto-assign position = max(existing positions) + 1
  - `UpdateAsync`: Update ModifiedAt
  - `DeleteAsync`: Delete single entry (event cascade not involved)
  - `ReorderAsync`: Transaction block; update all affected Position values in single query

**Verification:** Position ordering maintained; reorder operation atomic; queries return correct sort order.

---

## Wave 3: Services & Business Logic (2.5 days)

**Goal:** Implement state machines, validation, calculations.

**Dispatch:** 2 subagents in parallel (EventService + QueueService)

### Phase 3.1: EventService
- File: `MyVocaList.Services/Interfaces/IEventService.cs`
- File: `MyVocaList.Services/EventService.cs`
  - Inject: `IEventRepository`, `ILogger`
  - Methods:
    - `CreateEventAsync(venueId, name, scheduledStart, scheduledEnd, mode, ct)` → validate, check duplicate name, persist → `(bool success, string message, Event? event)`
    - `StartEventAsync(eventId, ct)` → validate state is CREATED, transition to STARTED, set ActualStartTime, persist → `(bool success, string message)`
    - `PauseEventAsync(eventId, ct)` → validate state is STARTED, transition to PAUSED, persist → `(bool success, string message)`
    - `ResumeEventAsync(eventId, ct)` → validate state is PAUSED, transition to STARTED, persist → `(bool success, string message)`
    - `FinishEventAsync(eventId, ct)` → validate state is STARTED or PAUSED, transition to FINISHED, set ActualEndTime, calculate completion stats → `(bool success, string message)`
    - `GetActiveEventAsync(ct)` → return first event with status STARTED or PAUSED (single-session assumption)
    - `ValidateEventNameAsync(venueId, name, scheduledDate, ct)` → check name length, uniqueness → `(bool isValid, string message)`
  - Validation rules per spec (name length, date ranges, state transitions)

**Verification:** All state transitions work; validation messages are clear; tuple returns have correct format.

### Phase 3.2: QueueService
- File: `MyVocaList.Services/Interfaces/IQueueService.cs`
- File: `MyVocaList.Services/QueueService.cs`
  - Inject: `IQueueRepository`, `IPersonRepository`, `ISongRepository`, `IEventRepository`, `ILogger`
  - Methods:
    - `EnqueueSingerAsync(eventId, personId, songId?, ct)` → check person/song exist, check not already enqueued, add to end of queue → `(bool success, string message)`
    - `RegisterParticipationAsync(queueEntryId, ct)` → validate status PENDING, transition to PERFORMING, set PerformanceStartTime, update current singer → `(bool success, string message)`
    - `StopPerformanceAsync(queueEntryId, ct)` → validate status PERFORMING, transition to COMPLETED, set PerformanceEndTime, calculate PerformanceDurationMinutes, advance to next PENDING singer → `(bool success, string message)`
    - `MarkAbsentAsync(queueEntryId, ct)` → validate status PENDING, transition to ABSENT, move to history, advance to next → `(bool success, string message)`
    - `UpdateSongSelectionAsync(queueEntryId, songId?, ct)` → update SongId, persist → `(bool success, string message)`
    - `ReorderQueueAsync(eventId, newOrder, ct)` → validate all entries are PENDING, update positions atomically → `(bool success, string message)`
    - `GetCurrentSingerAsync(eventId, ct)` → return entry with status PERFORMING for event, or first PENDING if none performing
    - `CalculateCompletionEstimateAsync(eventId, ct)` → if 5+ COMPLETED: avg_duration = sum(durations) / count; eta = now + (remaining_pending * avg); else null → `TimeSpan?`
  - Business rules per spec

**Verification:** State transitions correct; calculations accurate; uniqueness constraint enforced; ETA formula works.

### Phase 3.3: DI Registration
- File: Update `MyVocaList/MauiProgram.cs`
  - Add: `builder.Services.AddScoped<IEventRepository, EventRepository>();`
  - Add: `builder.Services.AddScoped<IQueueRepository, QueueRepository>();`
  - Add: `builder.Services.AddScoped<IEventService, EventService>();`
  - Add: `builder.Services.AddScoped<IQueueService, QueueService>();`

**Verification:** Services are resolvable from DI container at runtime.

---

## Wave 4: UI – ViewModels, Pages, Components (5 days)

**Goal:** Functional queue management UI.

**Dispatch:** 3–4 subagents per phase (details below)

### Phase 4.1: QueueManagementViewModel
- File: `MyVocaList.UI/ViewModels/QueueManagementViewModel.cs` (inherit `CrudListViewModelBase<QueueEntryViewModel>`)
- File: `MyVocaList.UI/Models/QueueEntryViewModel.cs` (display-friendly, observable)
  - Properties: Id, Position, PersonName, PersonInitials, SongTitle, ArtistName, Status, PerformanceDurationMinutes, IsCurrentSinger, CanDrag
  - Commands on QueueManagementViewModel: `InitializeAsync(eventId)`, `RegisterParticipationCommand`, `StopPerformanceCommand`, `MarkAbsentCommand`, `SelectSongCommand`, `OpenPersonPickerCommand`, `DragStartCommand`, `DragCompletedCommand`, `StartEventCommand`, `PauseEventCommand`, `ResumeEventCommand`, `FinishEventCommand`
  - Observable props: `CurrentEvent`, `CurrentSinger`, `NextUpQueue`, `History`, `PerformanceElapsedTime`, `PerformanceButtonText`, `EstimatedCompletionTime`, `IsEventStarted`, `IsPaused`, `EventStatusDisplay`
  - Timer logic: Dispatch timer every 1s; update `PerformanceElapsedTime` while PERFORMING
  - On navigation: Resume timer on `OnNavigatedTo`, pause on `OnNavigatedFrom`

**Verification:** ViewModel initializes correctly; commands execute without errors; timer counts; ETA appears after 5+ singers.

### Phase 4.2: Custom UI Components
- File: `MyVocaList.UI/Components/Queue/CurrentSingerCard.xaml` + `.xaml.cs`
  - Display: Monogram avatar, singer name, "PERFORMING" badge, song title (if selected), elapsed timer (MM:SS), [Register Participation]/[Stop Performance] toggle button, [Select Song] and [Mark Absent] secondary buttons
  - Binding: `{Binding CurrentSinger}`, `{Binding PerformanceElapsedTime}`, `{Binding PerformanceButtonText}`
  - Button styling: DevExpress DXButton with MD3 theme
  - Layout: Card-style with padding/corner-radius, grid for 2-col button layout
- File: `MyVocaList.UI/Components/Queue/QueueListItem.xaml` + `.xaml.cs`
  - Display: Drag handle (≡), position number, singer name, song title, status badge
  - Binding: `{Binding Position}`, `{Binding PersonName}`, `{Binding SongTitle}`, `{Binding Status}`
  - Touch target size: ≥48dp per MD3
  - Drag handle appearance: accent color, feedback on tap

**Verification:** Components render correctly; data bindings work; no layout crashes.

### Phase 4.3: QueueManagementPage XAML
- File: `MyVocaList.UI/Pages/Queue/QueueManagementPage.xaml` + `.xaml.cs`
  - Root: `ContentPage` with `SafeAreaEdges="Container"`, `BackgroundColor="{StaticResource Surface}"`
  - AppBar: SmallAppBar with event name, status (STARTED · 14:32 elapsed), trailing buttons [Pause]/[Resume] toggle, [Finish Event]
  - Body: `ScrollView` (allows collapse/expand header)
    - CurrentSingerCard (sticky at top, custom component)
    - DXCollectionView for `NextUpQueue` (drag-drop enabled, position ordered)
      - ItemTemplate: QueueListItem component
    - [+ Add Singer] button (sticky below list)
  - Snackbar region for feedback
- Code-behind: `OnAppearing()` calls `InitializeAsync(eventId)`; `OnDisappearing()` pauses timer
- Tap handlers: Register commands to buttons per design.md

**Verification:** Page loads without crashes; list displays correctly; drag-drop works on both Android/iOS.

### Phase 4.4: Modal Pages (Person Picker, Song Picker)
- File: `MyVocaList.UI/Pages/Queue/PersonPickerPage.xaml` + ViewModel
  - Reuse SearchAppBar component; use search-picker pattern (proven in Artists/Songs)
  - ViewModel: `PersonPickerViewModel` with SearchCommand, SelectResultCommand, BackCommand
  - On selection: Send `PersonPickedMessage` via messenger; close modal
- File: `MyVocaList.UI/Pages/Queue/SongPickerPage.xaml` + ViewModel
  - Same pattern; search songs by title + artist
  - On selection: Send `SongPickedMessage`; close modal

**Verification:** Modal opens/closes correctly; search works; selection returns to queue and updates.

### Phase 4.5: Dialogs & Confirmations
- File: `QueueManagementPage.xaml` (dialogs embedded or separate sheets)
  - [Finish Event] → BottomSheet: "End event and archive queue?" [Cancel] [Confirm]
  - [Mark Absent] → Snackbar: "Mark {name} as Absent?" [Undo] (3s auto-dismiss)

**Verification:** Dialogs appear/dismiss correctly; confirmations flow to service calls.

### Phase 4.6: Navigation & Routing
- File: Update `AppShell.xaml`
  - Register routes: `/queue/{eventId}`, `/person-picker`, `/song-picker`
  - Shell content: Add QueueManagementPage as tab or flyout item
- File: Update `NavigationConfig.cs` or routing registration in MauiProgram
  - Map routes to pages/viewmodels

**Verification:** Routes resolve; back button behavior correct; modals open/close without hang.

### Phase 4.7: E2E Smoke Test
- Execute full demo workflow from spec (create event, enqueue 5 singers, register, drag-reorder, finish)
- Verify: no crashes, timer counts correctly, UI updates in real-time, ETA appears after 5 singers, event finishes and locks

**Verification:** Demo completes in < 2 minutes without user-visible errors.

---

## Wave 5: Testing & Final Review (1.5 days)

**Goal:** Full test coverage, code review, ship-ready.

**Dispatch:** 2 subagents (tests in parallel), then 1 reviewer

### Phase 5.1: Service Unit Tests
- File: `MyVocaList.Tests/Unit/Services/EventServiceTests.cs`
  - Test: CreateEventAsync validates name/dates, rejects duplicates, returns success tuple
  - Test: State transitions (CREATED→STARTED→PAUSED↔RESUMED→FINISHED)
  - Test: Invalid transitions rejected
  - Target coverage: ≥80%
- File: `MyVocaList.Tests/Unit/Services/QueueServiceTests.cs`
  - Test: Enqueue validates person exists, rejects duplicates, adds at end
  - Test: RegisterParticipation/StopPerformance state transitions, timer recorded
  - Test: MarkAbsent advances queue
  - Test: Reorder maintains position integrity
  - Test: ETA calculates correctly after 5+ completed
  - Target coverage: ≥85% (complex state machine)

**Verification:** All tests pass; coverage meets thresholds.

### Phase 5.2: Integration Tests
- File: `MyVocaList.Tests/Integration/Repositories/EventRepositoryTests.cs`
  - Real SQLite temp DB
  - Test: CRUD operations, uniqueness constraint, cascade deletes
- File: `MyVocaList.Tests/Integration/Repositories/QueueRepositoryTests.cs`
  - Test: Position ordering, reorder operation, GetByEventIdAsync order

**Verification:** All tests pass; queries work correctly on real DB.

### Phase 5.3: Code Review (Fresh Subagent)
- Review all Wave 1–4 code against:
  - Constitutional constraints (DevExpress-first, MD3, no DisplayAlert, SafeAreaEdges, English-only)
  - Spec compliance (all ACs addressed)
  - MyVocaList patterns (ViewModel, commands, async, DI)
  - Test coverage (AC traceability matrix)
- Fix any findings
- Sign-off: No blockers; feature ready to ship

**Verification:** Review passes; no Critical findings.

---

## Acceptance Gates (Pre-Ship)

- [ ] All 12 user stories have passing acceptance criteria
- [ ] E2E smoke test completes on emulator without crashes
- [ ] Unit test coverage ≥80% (Services)
- [ ] Integration test coverage ≥70% (Repositories)
- [ ] Code review passes (no Critical or Elevated findings)
- [ ] All files committed to git with clear commit messages
- [ ] BACKLOG.md status updated to ✅ Done

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Timer drift (GC pauses) | Calibrate on real device; document acceptable margin (±1s) |
| Drag-reorder platform differences | Test on both Android emulator + iOS simulator |
| Modal navigation stuck | Test close-and-return flow manually; verify messenger cleanup |
| Concurrent event state (MVP assumption) | Document single-session guarantee; note post-MVP multi-session complexity |
| ETA miscalculation (edge case: 0 completed) | Guard: if count < 5, return null; if count_remaining = 0, return now |

---

## Next Steps

1. **User approval:** Confirm plan is acceptable
2. **Dispatch Wave 1:** 1 subagent, entities + DTOs (1.5 days)
3. **Dispatch Wave 2:** 2 subagents in parallel, migrations + repositories (2 days)
4. **Continue waves:** Sequential gate before each wave (verify prior wave is green)
5. **Final review:** Fresh reviewer agent before shipping

**Start date:** 2026-06-09  
**Target completion:** 2026-06-30 (3 weeks, with parallel execution)

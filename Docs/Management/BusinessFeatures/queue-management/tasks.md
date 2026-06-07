# Queue Management — Implementation Tasks

**Feature:** Queue Management (MVP)  
**Timeframe:** 3–4 weeks (parallel waves)  
**Status:** 🟢 Ready for Planning  

---

## DRY Onion Architecture (Wave Ordering)

Tasks are ordered from inside out: **Domain → Infra → Services → UI**.

- **Wave 1 (Domain):** Entities, enums, interfaces (no DB access, no service logic)
- **Wave 2 (Infra):** EF Core migrations, repository implementations
- **Wave 3 (Services):** Business logic, state transitions, validation
- **Wave 4 (UI):** ViewModels, pages, components, navigation

Sequential constraints are noted per task. Parallel tasks within a wave are marked `[P]`.

---

## Wave 1: Domain Contracts

> **Output:** Domain layer ready for Infra layer to consume

### Phase 1.1: Event Entity & Enums

- [ ] **Define Event entity + EventStatus enum** [SEQUENTIAL — Wave 1, Phase 1]
  - **Produces:** `MyVocaList.Domain/Entities/Event.cs`, `MyVocaList.Domain/Enums/EventStatus.cs`
  - **Consumes:** nothing (standalone)
  - **Risk:** Low
  - **Files owned:** 
    - `MyVocaList.Domain/Entities/Event.cs`
    - `MyVocaList.Domain/Enums/EventStatus.cs`
  - **Demo:** Event class compiles, properties are readable/writable
  - **Review lane:** Standard

### Phase 1.2: QueueEntry Entity & QueueEntryStatus Enum

- [ ] **Define QueueEntry entity + QueueEntryStatus enum** [P]
  - **Produces:** `MyVocaList.Domain/Entities/QueueEntry.cs`, `MyVocaList.Domain/Enums/QueueEntryStatus.cs`
  - **Consumes:** Person, Song entities (already exist); Event.cs (from Phase 1.1)
  - **Risk:** Low
  - **Files owned:**
    - `MyVocaList.Domain/Entities/QueueEntry.cs`
    - `MyVocaList.Domain/Enums/QueueEntryStatus.cs`
  - **Demo:** QueueEntry class compiles, relationships to Event/Person/Song are defined
  - **Review lane:** Standard

### Phase 1.3: Repository Interfaces

- [ ] **Define IEventRepository & IQueueRepository interfaces** [P]
  - **Produces:** `MyVocaList.Domain/Interfaces/IEventRepository.cs`, `IQueueRepository.cs`
  - **Consumes:** Event.cs, QueueEntry.cs (from Phases 1.1–1.2)
  - **Risk:** Low
  - **Files owned:**
    - `MyVocaList.Domain/Interfaces/IEventRepository.cs`
    - `MyVocaList.Domain/Interfaces/IQueueRepository.cs`
  - **Demo:** Interfaces compile, method signatures match design.md contract
  - **Review lane:** Standard
  - **Interface signatures (summary):**
    ```csharp
    IEventRepository:
      - Task<Event?> GetByIdAsync(int id, CancellationToken ct)
      - Task<(IEnumerable<Event> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct)
      - Task AddAsync(Event entity, CancellationToken ct)
      - Task UpdateAsync(Event entity, CancellationToken ct)
      - Task DeleteAsync(int id, CancellationToken ct)
    
    IQueueRepository:
      - Task<QueueEntry?> GetByIdAsync(int id, CancellationToken ct)
      - Task<IEnumerable<QueueEntry>> GetByEventIdAsync(int eventId, CancellationToken ct)
      - Task AddAsync(QueueEntry entry, CancellationToken ct)
      - Task UpdateAsync(QueueEntry entry, CancellationToken ct)
      - Task DeleteAsync(int id, CancellationToken ct)
      - Task ReorderAsync(int eventId, IEnumerable<(int entryId, int position)> newPositions, CancellationToken ct)
    ```

### Phase 1.4: DTOs (Contracts.DTOs)

- [ ] **Define EventDto, QueueEntryDto, and list DTOs** [P]
  - **Produces:**
    - `MyVocaList.Contracts/DTOs/Event/EventDto.cs`
    - `MyVocaList.Contracts/DTOs/Queue/QueueEntryDto.cs`
    - `MyVocaList.Contracts/DTOs/List/EventListItemDto.cs`
    - `MyVocaList.Contracts/DTOs/List/QueueEntryListItemDto.cs`
  - **Consumes:** EventStatus, QueueEntryStatus enums (from Phases 1.1–1.2)
  - **Risk:** Low
  - **Files owned:** DTOs folder structure
  - **Demo:** DTOs compile, contain flattened/display-friendly versions of entities
  - **Review lane:** Standard

---

## Wave 2: Infrastructure (EF Core)

> **Output:** Database schema ready; repository implementations callable

### Phase 2.1: EF Core Configuration & Migration

- [ ] **Configure Event & QueueEntry in AppDbContext; create migration** [SEQUENTIAL — waits for Wave 1]
  - **Produces:** EF Core entity configurations, migration file `*_AddEventAndQueue.cs`
  - **Consumes:** Event.cs, QueueEntry.cs (from Wave 1)
  - **Risk:** Medium (schema, collation, indexes)
  - **Files owned:**
    - `MyVocaList.Infra/AppDbContext.cs` (add modelBuilder.Entity<Event>, <QueueEntry> config)
    - `MyVocaList.Infra/Migrations/*_AddEventAndQueue.cs` (generated)
  - **Demo:** Migration creates Event and QueueEntry tables with correct schema, indexes, constraints
  - **Review lane:** Elevated (schema review)
  - **Notes:**
    - Event name must be unique per venue per date (composite index with filter)
    - QueueEntry position must be ordered (index)
    - Foreign keys: Event.VenueId → Venue.Id, QueueEntry.EventId → Event.Id, etc.
    - Collation on Event.Name (case-insensitive for uniqueness check)

### Phase 2.2: EventRepository Implementation

- [ ] **Implement EventRepository : IEventRepository** [SEQUENTIAL — waits for Phase 2.1]
  - **Produces:** `MyVocaList.Infra/Repositories/EventRepository.cs`
  - **Consumes:** IEventRepository.cs, AppDbContext
  - **Risk:** Medium (queries, pagination)
  - **Files owned:** `EventRepository.cs`
  - **Demo:** CRUD methods work; GetPaged returns correct page; uniqueness check via query
  - **Review lane:** Standard
  - **Methods:**
    - GetByIdAsync (include QueueEntries, Person, Song nav properties)
    - GetPagedAsync (with optional filter)
    - AddAsync (validate, persist)
    - UpdateAsync (timestamp)
    - DeleteAsync (cascade deletes QueueEntries)

### Phase 2.3: QueueRepository Implementation

- [ ] **Implement QueueRepository : IQueueRepository** [P — parallel with Phase 2.2]
  - **Produces:** `MyVocaList.Infra/Repositories/QueueRepository.cs`
  - **Consumes:** IQueueRepository.cs, AppDbContext
  - **Risk:** Medium (reordering logic, position calculation)
  - **Files owned:** `QueueRepository.cs`
  - **Demo:** Add, update, delete, reorder operations work correctly
  - **Review lane:** Standard
  - **Methods:**
    - GetByIdAsync (include nav properties)
    - GetByEventIdAsync (order by position)
    - AddAsync (auto-assign position at end of queue)
    - UpdateAsync (status, timestamps, song selection)
    - DeleteAsync
    - ReorderAsync (update all affected positions in transaction)

---

## Wave 3: Services (Business Logic)

> **Output:** Service methods callable; state transitions validated

### Phase 3.1: IEventService & EventService

- [ ] **Define IEventService interface + EventService implementation** [SEQUENTIAL — waits for Wave 2]
  - **Produces:**
    - `MyVocaList.Services/Interfaces/IEventService.cs`
    - `MyVocaList.Services/EventService.cs`
  - **Consumes:** IEventRepository, IQueueRepository, Event/QueueEntry entities
  - **Risk:** Medium (state machine logic, validation)
  - **Files owned:** both service files
  - **Demo:** Service methods return correct (success, message, entity?) tuples
  - **Review lane:** Elevated (business logic)
  - **Methods:**
    ```csharp
    IEventService:
      Task<(bool success, string message, Event? event)> CreateEventAsync(
        int venueId, string name, DateTime scheduledStart, DateTime scheduledEnd, 
        string mode, CancellationToken ct)
      Task<(bool success, string message)> StartEventAsync(int eventId, CancellationToken ct)
      Task<(bool success, string message)> PauseEventAsync(int eventId, CancellationToken ct)
      Task<(bool success, string message)> ResumeEventAsync(int eventId, CancellationToken ct)
      Task<(bool success, string message)> FinishEventAsync(int eventId, CancellationToken ct)
      Task<Event?> GetActiveEventAsync(CancellationToken ct) // single active event
      Task<(bool success, string message)> ValidateEventNameAsync(
        int venueId, string name, DateTime scheduledDate, CancellationToken ct)
    ```
  - **Validation:**
    - Event name: 1–100 chars, unique per venue per date
    - Dates: start < end, end ≤ start + 24h
    - State transitions: CREATED→STARTED, STARTED↔PAUSED, STARTED/PAUSED→FINISHED (others blocked)

### Phase 3.2: IQueueService & QueueService

- [ ] **Define IQueueService interface + QueueService implementation** [P — parallel with Phase 3.1]
  - **Produces:**
    - `MyVocaList.Services/Interfaces/IQueueService.cs`
    - `MyVocaList.Services/QueueService.cs`
  - **Consumes:** IQueueRepository, IPersonRepository, ISongRepository, Event/QueueEntry entities
  - **Risk:** High (timing logic, position reordering, state transitions)
  - **Files owned:** both service files
  - **Demo:** Enqueue, register, advance queue, reorder all work correctly; ETA calculates
  - **Review lane:** Elevated (complex state machine)
  - **Methods:**
    ```csharp
    IQueueService:
      Task<(bool success, string message)> EnqueueSingerAsync(
        int eventId, int personId, int? songId, CancellationToken ct)
      Task<(bool success, string message)> RegisterParticipationAsync(
        int queueEntryId, CancellationToken ct)
      Task<(bool success, string message)> StopPerformanceAsync(
        int queueEntryId, CancellationToken ct)
      Task<(bool success, string message)> MarkAbsentAsync(
        int queueEntryId, CancellationToken ct)
      Task<(bool success, string message)> UpdateSongSelectionAsync(
        int queueEntryId, int? songId, CancellationToken ct)
      Task<(bool success, string message)> ReorderQueueAsync(
        int eventId, IEnumerable<(int entryId, int position)> newOrder, CancellationToken ct)
      Task<QueueEntry?> GetCurrentSingerAsync(int eventId, CancellationToken ct)
      Task<TimeSpan?> CalculateCompletionEstimateAsync(int eventId, CancellationToken ct)
    ```
  - **Business rules:**
    - Enqueue: check person exists, event exists, not already enqueued
    - RegisterParticipation: only PENDING → PERFORMING, record timestamp
    - StopPerformance: only PERFORMING → COMPLETED, calculate duration, advance to next
    - MarkAbsent: PENDING → ABSENT, advance to next
    - Reorder: only PENDING singers, maintain position integrity

### Phase 3.3: DI Registration (MauiProgram.cs)

- [ ] **Register IEventService, IQueueService, repositories in DI** [P]
  - **Produces:** Updated `MyVocaList/MauiProgram.cs`
  - **Consumes:** EventService, QueueService interfaces/implementations
  - **Risk:** Low (DI configuration)
  - **Files owned:** `MauiProgram.cs`
  - **Demo:** Services are resolvable from DI container; no runtime resolution errors
  - **Review lane:** Standard
  - **Registrations:**
    ```csharp
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<IQueueRepository, QueueRepository>();
    builder.Services.AddScoped<IEventService, EventService>();
    builder.Services.AddScoped<IQueueService, QueueService>();
    ```

---

## Wave 4: UI (MAUI Pages & ViewModels)

> **Output:** Fully functional queue management UI

### Phase 4.1: QueueManagementViewModel

- [ ] **Implement QueueManagementViewModel : CrudListViewModelBase<QueueEntryViewModel>** [SEQUENTIAL — waits for Wave 3]
  - **Produces:**
    - `MyVocaList.UI/ViewModels/QueueManagementViewModel.cs`
    - `MyVocaList.UI/Models/QueueEntryViewModel.cs` (display model)
  - **Consumes:** IEventService, IQueueService
  - **Risk:** High (timer logic, state management, real-time UI updates)
  - **Files owned:** both ViewModel files
  - **Demo:** 
    - ViewModel initializes with eventId parameter
    - Queue loads from service
    - Commands execute without errors
    - Performance timer counts correctly
    - ETA calculates after 5+ completed singers
  - **Review lane:** Elevated (complex state)
  - **Key methods:**
    - InitializeAsync(eventId) — load event, queue, start timer if PERFORMING
    - RegisterParticipationAsync() — call service, update UI
    - StopPerformanceAsync() — call service, advance queue, reset timer
    - MarkAbsentAsync() — call service with confirmation
    - UpdatePerformanceTimer() — runs every 1s, updates elapsed time
    - CalculateCompletionEstimate() — derive from completed performers
    - OnNavigatedTo() — resume timer if event is STARTED
    - OnNavigatedFrom() — pause timer

### Phase 4.2: Custom UI Components

- [ ] **Implement CurrentSingerCard component** [P]
  - **Produces:** `MyVocaList.UI/Components/Queue/CurrentSingerCard.xaml[.cs]`
  - **Consumes:** QueueManagementViewModel (data binding)
  - **Risk:** Low (component composition)
  - **Files owned:** `.xaml` and `.xaml.cs` files
  - **Demo:** Component displays current singer, song, timer, and buttons correctly
  - **Review lane:** Standard
  - **Includes:**
    - Monogram avatar (from Person.Initials)
    - Singer name + status badge
    - Song display (if selected)
    - Performance timer (MM:SS)
    - [Register Participation] / [Stop Performance] toggle button
    - [Select Song] and [Mark Absent] secondary buttons

- [ ] **Implement QueueListItem component (for drag-reorderable list)** [P]
  - **Produces:** `MyVocaList.UI/Components/Queue/QueueListItem.xaml[.cs]`
  - **Consumes:** QueueEntryViewModel
  - **Risk:** Medium (drag handle, position display)
  - **Files owned:** `.xaml` and `.xaml.cs` files
  - **Demo:** List items display position, name, song, status badge; drag handle is visible
  - **Review lane:** Standard
  - **Includes:**
    - Drag handle (≡ icon)
    - Position number (1, 2, 3, ...)
    - Singer name + song (if selected)
    - Status badge (PENDING | PERFORMING | COMPLETED | ABSENT)

### Phase 4.3: QueueManagementPage (XAML + Code-Behind)

- [ ] **Implement QueueManagementPage.xaml[.cs]** [SEQUENTIAL — waits for Phase 4.2]
  - **Produces:** `MyVocaList.UI/Pages/Queue/QueueManagementPage.xaml[.cs]`
  - **Consumes:** QueueManagementViewModel, CurrentSingerCard, QueueListItem
  - **Risk:** High (layout, drag-drop, state-dependent UI)
  - **Files owned:** both XAML and code-behind files
  - **Demo:**
    - Page loads event and queue
    - CurrentSingerCard displays correctly
    - QueueListView shows all singers in order
    - Drag-reorder works (PENDING singers only)
    - [+ Add Singer] button opens PersonPicker
    - All buttons (Register, Stop, MarkAbsent, SelectSong, Pause, End) respond to taps
    - Timer counts correctly
  - **Review lane:** Elevated (complex layout + interactions)
  - **Layout structure:**
    - AppBar (event name, status, [Pause]/[Resume] toggle, [End Event])
    - ScrollView (allows collapse/expand header)
      - CurrentSingerCard (Sticky at top)
      - QueueListView (DXCollectionView, drag-drop enabled)
      - [+ Add Singer] button (sticky below)
    - Snackbar container (for feedback messages)

### Phase 4.4: Modal Pages (Person Picker, Song Picker)

- [ ] **Implement PersonPickerPage for queue enqueue** [P]
  - **Produces:** `MyVocaList.UI/Pages/Queue/PersonPickerPage.xaml[.cs]`, ViewModel
  - **Consumes:** Reuse existing SearchAppBar, PersonSearchViewModel (search-picker pattern)
  - **Risk:** Low (reuse existing pattern; already proven in Artists/Songs)
  - **Files owned:** `.xaml`, `.xaml.cs`, ViewModel
  - **Demo:**
    - Page opens as modal
    - Search field filters people by name
    - Tap person → enqueues them, closes modal
    - If not found + "Add Person" button → can create new person
    - After enqueue, queue updates in background
  - **Review lane:** Standard

- [ ] **Implement SongPickerPage for queue song selection** [P]
  - **Produces:** `MyVocaList.UI/Pages/Queue/SongPickerPage.xaml[.cs]`, ViewModel
  - **Consumes:** Reuse SearchAppBar, SongSearchViewModel (search-picker pattern)
  - **Risk:** Low (reuse existing pattern)
  - **Files owned:** `.xaml`, `.xaml.cs`, ViewModel
  - **Demo:**
    - Page opens as modal
    - Search field filters songs by title + artist
    - Tap song → assigns to queue entry, closes modal
    - Queue updates with selected song
    - If no songs → show "No songs available" message
  - **Review lane:** Standard

### Phase 4.5: Dialogs & Confirmations

- [ ] **Implement confirmation dialogs (Finish Event, Mark Absent)** [P]
  - **Produces:** Confirmation dialog layouts (BottomSheet, not DisplayAlert per CLAUDE.md)
  - **Consumes:** QueueManagementViewModel commands
  - **Risk:** Low (dialog scaffolding)
  - **Files owned:** `.xaml` for dialogs (embedded in QueueManagementPage or separate)
  - **Demo:**
    - [Finish Event] shows confirmation sheet: "End event and archive queue?" [Cancel] [Confirm]
    - [Mark Absent] shows snackbar: "Mark João as Absent?" [Undo] (auto-dismiss 3s)
  - **Review lane:** Standard

### Phase 4.6: Navigation & Routing

- [ ] **Register routes in AppShell; wire up navigation** [SEQUENTIAL — waits for Phase 4.4]
  - **Produces:** Updated `AppShell.xaml`, route registrations
  - **Consumes:** QueueManagementPage, PersonPickerPage, SongPickerPage
  - **Risk:** Low (routing config)
  - **Files owned:** `AppShell.xaml`, any route registration code
  - **Demo:**
    - `/queue/{eventId}` navigates to QueueManagementPage
    - `/person-picker` navigates to PersonPickerPage
    - `/song-picker` navigates to SongPickerPage
    - Back button behavior is correct on each page
  - **Review lane:** Standard

### Phase 4.7: Testing E2E (Manual Smoke Test on Emulator)

- [ ] **E2E smoke test: full queue workflow** [SEQUENTIAL — final gate before "To Review"]
  - **Produces:** Task-log evidence (screenshots or narrative of steps executed)
  - **Consumes:** All implemented code (Waves 1–4)
  - **Risk:** Low (manual testing, not automated)
  - **Files owned:** None (test only)
  - **Demo (execute on emulator):**
    1. Navigate to Venues page
    2. Create a new event (venue: Jazz Club, name: "Test Gala", date/time, mode: Video Karaoke)
    3. Verify event appears in Events list / Venues view
    4. Tap event → navigate to QueueManagementPage
    5. Verify page shows empty queue + "+ Add Singer" button
    6. Tap "+ Add Singer" → search for "João" → enqueue
    7. Verify João appears in "Next Up" section
    8. Add 3 more singers (Maria, Carlos, Ana)
    9. Verify all 4 appear in queue in order
    10. Tap [Start Event] → event status changes to STARTED
    11. Verify João becomes CURRENT (top card)
    12. Tap [Select Song] on João → search "Imagine" → select
    13. Verify song appears in CurrentSingerCard
    14. Tap [Register Participation] → timer starts, button changes to [Stop Performance]
    15. Wait 30 seconds (simulated performance)
    16. Tap [Stop Performance] → João moves to COMPLETED, Maria becomes CURRENT, timer resets
    17. Drag Carlos above Ana → reorder succeeds
    18. Continue registering 2 more singers
    19. Verify ETA appears ("~X mins remaining")
    20. Tap [Finish Event] → confirmation dialog
    21. Confirm → event transitions to FINISHED, queue locks to read-only
    22. Verify buttons are disabled
  - **Review lane:** Standard

---

## Wave 5: Testing & Refinement

> **Output:** Tests passing; code review clean; ready to ship

### Phase 5.1: Unit Tests (Services)

- [ ] **Write unit tests for EventService methods** [P]
  - **Produces:** `MyVocaList.Tests/Unit/Services/EventServiceTests.cs`
  - **Consumes:** EventService implementation
  - **Risk:** Medium (TDD coverage)
  - **Files owned:** Test file
  - **AC traceability:**
    - AC-1.x: CreateEventAsync validates name, dates, venue
    - AC-2.x: StartEventAsync transitions state correctly
    - AC-3-5.x: Pause/Resume/Finish state transitions
  - **Demo:** All tests pass; coverage ≥ 80%
  - **Review lane:** Standard

- [ ] **Write unit tests for QueueService methods** [P]
  - **Produces:** `MyVocaList.Tests/Unit/Services/QueueServiceTests.cs`
  - **Consumes:** QueueService implementation
  - **Risk:** High (complex state machine)
  - **Files owned:** Test file
  - **AC traceability:**
    - AC-8.x: EnqueueSingerAsync validates uniqueness, adds to end
    - AC-9.x: RegisterParticipationAsync / StopPerformanceAsync state transitions
    - AC-10.x: MarkAbsentAsync advances queue
    - AC-11.x: Song selection persists
    - AC-12.x: ETA calculation correct after 5+ singers
  - **Demo:** All tests pass; coverage ≥ 85%
  - **Review lane:** Elevated (complex logic)

### Phase 5.2: Integration Tests (Repositories)

- [ ] **Write integration tests for EventRepository** [P]
  - **Produces:** `MyVocaList.Tests/Integration/Repositories/EventRepositoryTests.cs`
  - **Consumes:** EventRepository, real SQLite temp DB
  - **Risk:** Low (repository tests established pattern)
  - **Files owned:** Test file
  - **Demo:** Queries, uniqueness, cascade deletes all work
  - **Review lane:** Standard

- [ ] **Write integration tests for QueueRepository (reordering, position integrity)** [P]
  - **Produces:** `MyVocaList.Tests/Integration/Repositories/QueueRepositoryTests.cs`
  - **Consumes:** QueueRepository, real SQLite temp DB
  - **Risk:** Medium (position ordering complexity)
  - **Files owned:** Test file
  - **Demo:** Reorder operation maintains position integrity; queries return correct order
  - **Review lane:** Standard

### Phase 5.3: Code Review & Fixes

- [ ] **Full code review (subagent or Helder)** [SEQUENTIAL — final gate]
  - **Produces:** Reviewed code, checklist sign-off
  - **Consumes:** All implemented code
  - **Risk:** Low (review only)
  - **Files owned:** None
  - **Demo:** All findings addressed; no blockers
  - **Review lane:** Elevated (full feature review)

---

## Dependency & Timing Summary

```
Wave 1 (Domain)
  ├─ Phase 1.1: Event entity [1d]
  ├─ Phase 1.2: QueueEntry entity [1d, P with 1.1]
  ├─ Phase 1.3: Interfaces [0.5d, P with 1.1–1.2]
  └─ Phase 1.4: DTOs [0.5d, P with all above]
  Total Wave 1: ~1.5 days

Wave 2 (Infra) [waits for Wave 1]
  ├─ Phase 2.1: EF config + migration [1d]
  ├─ Phase 2.2: EventRepository [1d, waits for 2.1]
  └─ Phase 2.3: QueueRepository [1d, P with 2.2]
  Total Wave 2: ~2 days

Wave 3 (Services) [waits for Wave 2]
  ├─ Phase 3.1: EventService [1.5d]
  ├─ Phase 3.2: QueueService [2d, P with 3.1]
  └─ Phase 3.3: DI registration [0.5d, P with 3.1–3.2]
  Total Wave 3: ~2.5 days

Wave 4 (UI) [waits for Wave 3]
  ├─ Phase 4.1: ViewModel [1.5d]
  ├─ Phase 4.2: Components [1d, P with 4.1]
  ├─ Phase 4.3: Page XAML [1.5d, waits for 4.2]
  ├─ Phase 4.4: Modal pages [1d, P with 4.3]
  ├─ Phase 4.5: Dialogs [0.5d, P with all]
  ├─ Phase 4.6: Routing [0.5d, waits for 4.4]
  └─ Phase 4.7: E2E smoke test [0.5d, final gate]
  Total Wave 4: ~5 days

Wave 5 (Tests & Review)
  ├─ Phase 5.1: Service unit tests [1d, P]
  ├─ Phase 5.2: Integration tests [1d, P]
  └─ Phase 5.3: Code review [0.5d, final]
  Total Wave 5: ~1.5 days

**Grand total: ~12 days elapsed time** (with 2–4 subagents in parallel)
```

---

## Execution Strategy

### Recommended Wave Dispatch Order

1. **Wave 1:** Dispatch 1 agent; output: 4 files (entities, enums, interfaces, DTOs) ✅
2. **Wave 2:** Dispatch 2 agents in parallel (Phase 2.1 sequential, then 2.2 & 2.3 parallel) ✅
3. **Wave 3:** Dispatch 2 agents in parallel (EventService + QueueService) ✅
4. **Wave 4:** Dispatch 3–4 agents (ViewModel → Components in parallel, then Page → Modals/Dialogs, then Routing, then E2E) ✅
5. **Wave 5:** Dispatch 2 agents in parallel (unit tests + integration tests), then final review

### Coordination Checkpoints

- **After Wave 2:** Confirm schema migration works (`dotnet ef database update`)
- **After Wave 3:** Confirm services are DI-registered and callable
- **After Wave 4:** Confirm page loads without crashes; E2E smoke test passes
- **After Wave 5:** Confirm all tests pass; code review clean

### Risk Mitigation

- **Timer accuracy:** Test timer manually on emulator (drift may appear due to GC); calibrate if needed
- **Drag-reorder:** Test on both Android and iOS; DXCollectionView drag API varies
- **Modal navigation:** Test person/song picker modals close and return to queue correctly
- **Concurrent state changes:** MVP assumes single-session usage; document race condition risk in post-MVP

---

## Acceptance Gates (Pre-Ship)

- [ ] All user stories have passing acceptance criteria
- [ ] E2E smoke test completes without errors
- [ ] Unit test coverage ≥ 80% (Services)
- [ ] Integration test coverage ≥ 70% (Repositories)
- [ ] Code review passes (no Critical or Elevated findings)
- [ ] BACKLOG.md status updated to ✅ Done
- [ ] Demo: Host can create event, enqueue 5 singers, register performances, reorder, finish event in < 2 min

# Queue Management — Requirements

## Overview
Core product feature: manage an active karaoke queue for a live event. Hosts create events, enqueue singers, track performance timing, and manage queue progression through multiple rounds. Supports two modes: Video Karaoke (prerecorded playback) and Artist Instruments (live band accompaniment).

## User Stories

### Story 1: Create Event
**As a** host manager  
**I want to** create a new karaoke event  
**So that** I can start managing singers for a specific venue and time  

**Acceptance Criteria:**
- AC-1.1: User navigates to an event creation flow (entry point TBD — may be Venues page or dedicated Events page)
- AC-1.2: User selects or searches for a venue from existing venue list
- AC-1.3: User enters event name (required, ≤ 100 chars, unique per venue per date)
- AC-1.4: User selects scheduled start date and time (required, future date only)
- AC-1.5: User selects scheduled end date and time (required, ≥ start time)
- AC-1.6: User selects event mode: "Video Karaoke" or "Artist Instruments" (required; may be changed later, pre-MVP)
- AC-1.7: On save, event is created with status CREATED and empty queue
- AC-1.8: User is navigated to the Queue Management page for that event

---

### Story 2: Start Event
**As a** host  
**I want to** start an event  
**So that** singers can begin performing and I can track actual timing  

**Acceptance Criteria:**
- AC-2.1: User navigates to a CREATED event in the queue view
- AC-2.2: User taps a "Start Event" button/action (action palette, floating button, or AppBar)
- AC-2.3: Event status transitions from CREATED → STARTED
- AC-2.4: Actual start timestamp is recorded (server time at moment of transition)
- AC-2.5: Queue view becomes fully operational: enqueue, drag-reorder, register performance all enabled
- AC-2.6: User sees confirmation feedback (snackbar or subtle UI update showing STARTED status + elapsed time)

---

### Story 3: Pause Event
**As a** host  
**I want to** pause an event  
**So that** singers don't advance to their turn while the event is suspended (e.g., technical break, venue issue)  

**Acceptance Criteria:**
- AC-3.1: User is viewing a STARTED event
- AC-3.2: User taps "Pause Event" action
- AC-3.3: Event status transitions STARTED → PAUSED
- AC-3.4: Current singer's performance timer is paused (if mid-performance, timer frozen)
- AC-3.5: No queue advancement happens while paused (can still enqueue, drag, mark absent, but progression halted)
- AC-3.6: UI shows PAUSED status with remaining time until resumption or end
- AC-3.7: User can "Resume" without losing queue state

---

### Story 4: Resume Event
**As a** host  
**I want to** resume a paused event  
**So that** singers continue performing where they left off  

**Acceptance Criteria:**
- AC-4.1: User is viewing a PAUSED event
- AC-4.2: User taps "Resume Event" action
- AC-4.3: Event status transitions PAUSED → STARTED
- AC-4.4: Timing resumes; current singer's performance timer (if paused mid-performance) resumes from where it was frozen
- AC-4.5: UI updates to show STARTED status and elapsed time
- AC-4.6: Queue advancement is re-enabled

---

### Story 5: Finish Event
**As a** host  
**I want to** mark an event as finished  
**So that** the event is archived and no further changes are possible  

**Acceptance Criteria:**
- AC-5.1: User is viewing a STARTED or PAUSED event
- AC-5.2: User taps "Finish Event" action
- AC-5.3: User sees a confirmation dialog (e.g., "End event and archive queue?")
- AC-5.4: On confirmation, event status → FINISHED, actual end timestamp recorded
- AC-5.5: Queue is locked (no further enqueue, drag, or register actions allowed; read-only view)
- AC-5.6: Event appears in history/analytics section (future feature; pre-MVP shows "Event Finished")
- AC-5.7: Completion stats are calculated and persisted (total singers, absent count, total elapsed time, completion time estimate)

---

### Story 6: Visualize Queue
**As a** host  
**I want to** see the current singer, next singers in queue, and understand queue status at a glance  
**So that** I can manage the event flow and communicate with singers  

**Acceptance Criteria:**
- AC-6.1: Queue page has a "Current" section showing the singer currently performing
- AC-6.2: Current section displays: singer name, selected song (if any), time remaining (if registered), and quick action buttons
- AC-6.3: Queue page shows "Next Up" section listing singers 1–N in order
- AC-6.4: Each queue entry displays: position, singer name, song (if selected), status (PENDING | PERFORMING | COMPLETED | ABSENT)
- AC-6.5: Scrolling reveals all singers in queue (no artificial limit)
- AC-6.6: Visual distinction between CURRENT, NEXT (top 3–5 in list), and future queue items (lower visual priority)
- AC-6.7: Queue updates in real-time as singers are added, reordered, or marked complete/absent (no page refresh required)
- AC-6.8: On event FINISHED, queue transitions to read-only; buttons disabled

---

### Story 7: Drag-Reorder Queue
**As a** host  
**I want to** reorder singers in the queue by dragging  
**So that** I can accommodate late arrivals or requests to move earlier/later  

**Acceptance Criteria:**
- AC-7.1: Queue is visible on screen with PENDING and COMPLETED singers
- AC-7.2: Host long-presses (Android) or drags (iOS) a singer entry in the queue
- AC-7.3: A drag handle appears (visual feedback, cursor changes to grab)
- AC-7.4: Host drags the singer up/down in the queue
- AC-7.5: Queue reorders visually and persistently on drop
- AC-7.6: CURRENT singer (performing) cannot be dragged
- AC-7.7: COMPLETED and ABSENT singers appear in a separate "History" section (not draggable with active queue)
- AC-7.8: Reorder is persisted to DB immediately (optimistic UI update, no confirmation needed)
- AC-7.9: Reorder is persisted to other connected devices in real-time (future: WebSocket or SignalR broadcast; MVP: optimistic + reload on app resume)

---

### Story 8: Enqueue Singer
**As a** host  
**I want to** add a singer to the queue  
**So that** new singers can join or late arrivals can be added  

**Acceptance Criteria:**
- AC-8.1: Queue page shows "+ Add Singer" button (always visible, even when queue is full)
- AC-8.2: Tapping "+ Add Singer" opens a **Person Picker modal** (search existing singers from DB)
- AC-8.3: Picker shows search field with placeholder "Search singers..." (case-insensitive, filters on name)
- AC-8.4: Picker shows list of matching people, displaying: name, initials avatar, and any existing participation count
- AC-8.5: User taps a person → person is enqueued to the event; modal closes; queue updates with new entry at end
- AC-8.6: If user searches and **person not found**, picker shows "No match" and an **"Add Person" button** (entry point to Create Person flow)
- AC-8.7: User taps "Add Person" → inline person creation form appears (or navigates to PersonFormPage)
- AC-8.8: After creating person, they are enqueued to the event; picker closes; queue updates
- AC-8.9: Same person cannot be enqueued twice in the same event (constraint: unique event_id + person_id)
- AC-8.10: Error handling: if enqueue fails (DB error, constraint violation), show snackbar with reason and allow retry

---

### Story 9: Register Participation
**As a** host  
**I want to** mark the current singer as performing  
**So that** I can track actual performance time and move them to completed  

**Acceptance Criteria:**
- AC-9.1: Current singer section shows a "Register Participation" button (prominent, high affordance)
- AC-9.2: Button is enabled only when queue has at least one PENDING singer
- AC-9.3: Tapping "Register Participation" transitions the current singer from PENDING → PERFORMING
- AC-9.4: Performance start timestamp is recorded (server time at transition)
- AC-9.5: A performance timer appears in the Current section, counting up from 0:00
- AC-9.6: User interface shows current singer as actively performing (e.g., visual highlight, status badge "PERFORMING")
- AC-9.7: While timer is running, a "Stop Performance" button appears (replaces "Register Participation")
- AC-9.8: Tapping "Stop Performance" does the following:
  - Records performance end timestamp
  - Calculates performance_duration = end - start
  - Transitions singer status to COMPLETED
  - Moves singer to the end of the queue (or to "History" section if all singers have performed once)
  - Advances queue: next PENDING singer becomes CURRENT
  - Timer resets to 0:00
  - UI updates to show new CURRENT singer
- AC-9.9: If the current singer is marked ABSENT (via separate action), skip registration and move directly to next
- AC-9.10: Performance duration data is persisted to DB and used for analytics (future feature: completion time estimate)

---

### Story 10: Mark Singer Absent
**As a** host  
**I want to** mark the current singer as absent  
**So that** they are skipped and queue advances  

**Acceptance Criteria:**
- AC-10.1: Current singer section shows "Mark Absent" button (secondary action)
- AC-10.2: Tapping "Mark Absent" shows a confirmation (e.g., snackbar "Mark João as Absent?")
- AC-10.3: On confirmation:
  - Current singer status → ABSENT
  - Singer is removed from active queue and moved to History section
  - Next PENDING singer becomes CURRENT
  - UI updates immediately
  - DB is persisted
- AC-10.4: Absent singers appear in History section with ABSENT badge/tag
- AC-10.5: Undo is provided via snackbar ("Undo") for 3 seconds; tapping Undo restores singer to queue at original position

---

### Story 11: Link Song to Singer (Pre-Performance)
**As a** host  
**I want to** assign a song to a singer before they perform  
**So that** I (or the video system) knows which track to play  

**Acceptance Criteria:**
- AC-11.1: Current singer section shows "Select Song" button or click target
- AC-11.2: Tapping opens a **Song Picker modal** (search existing songs from Artists/Songs catalog)
- AC-11.3: Picker shows search field, filtering songs by title or artist name (case-insensitive)
- AC-11.4: User taps a song → song is linked to the queue entry; modal closes
- AC-11.5: Current singer section displays selected song name and artist (e.g., "Imagine" by John Lennon)
- AC-11.6: Song selection is optional; user can register performance without selecting a song (for Artist Instruments mode where band chooses)
- AC-11.7: Song selection is persisted to the queue_entry record in DB
- AC-11.8: If user wants to change the song after selecting, they tap "Select Song" again and can pick a different song
- AC-11.9: Error handling: if no songs exist in DB, show "No songs available. Add songs first." with navigation hint

---

### Story 12: Track Performance Time & Calculate Completion Estimate
**As a** host  
**I want to** see how long singers are performing  
**So that** I can estimate when the event will finish  

**Acceptance Criteria:**
- AC-12.1: Timer in Current section shows elapsed performance time (MM:SS format)
- AC-12.2: After 5+ singers have completed, app calculates average_performance_duration = sum of all completed durations / count
- AC-12.3: App shows estimated completion time: "ETA: ~16:45" in event header or a separate widget
- AC-12.4: Estimate updates dynamically as more singers complete (moving average)
- AC-12.5: Estimate is visible on the Queue page
- AC-12.6: (Future feature: Send estimate to host via notification or display on admin panel)

---

## Out of Scope (MVP)

- ❌ Real-time multi-device queue sync (WebSocket, SignalR). MVP: optimistic UI, reload on resume.
- ❌ Singer self-registration or public join link. MVP: Host enqueues only.
- ❌ Audio/lyrics streaming or in-app playback. MVP: Video playback supported via external YouTube launch (search and open URL buttons in SongFormPage, pickers, and queue).
- ❌ Multiple concurrent events per host. MVP: Assume single active event per session.
- ❌ Event history analytics dashboard. MVP: Basic stats on finish (count, absent, avg time).
- ❌ Undo for all operations (only for Mark Absent). Full undo available post-MVP.
- ❌ Song suggestion or song history per singer. MVP: Manual selection each time.
- ❌ Round-based progression UI (song selection per round). MVP: Linear queue progression.
- ❌ Integration with Bandokê or external instruments. MVP: Timing only.

---

## Validation Rules

### Event Creation
- Event name: required, 1–100 chars, unique per venue per date (case-insensitive)
- Scheduled start: required, future date/time, per venue's timezone
- Scheduled end: required, ≥ scheduled start + 15 min, ≤ scheduled start + 24 hours
- Venue: required, must exist in DB

### Queue Entry
- Singer (person_id): required, must exist in DB
- Event (event_id): required, must exist and be STARTED or PAUSED
- Unique constraint: (event_id, person_id) — same person cannot be enqueued twice in one event
- Song (song_id): optional, must exist in DB if selected

### Performance Registration
- Only PENDING singers can transition to PERFORMING
- Only PERFORMING singers can transition to COMPLETED
- Start timestamp must be ≤ end timestamp

---

## Business Rules

### Rule: Queue Progression
1. Event starts → queue transitions to STARTED
2. Host taps "Register Participation" on current singer → PENDING → PERFORMING + timer starts
3. Host taps "Stop Performance" → PERFORMING → COMPLETED + timer stops + next singer becomes CURRENT
4. If current singer marked ABSENT → skip, move to History, next singer becomes CURRENT

### Rule: Drag-Reorder Scope
- Only PENDING singers can be reordered
- CURRENT (PERFORMING) singer cannot be dragged
- COMPLETED and ABSENT singers are in read-only History section
- Reorder persists immediately (no confirmation)

### Rule: Event Lifecycle
- CREATED → (Start) → STARTED → (Pause) → PAUSED ↔ (Resume) → STARTED → (Finish) → FINISHED
- FINISHED events are read-only; no further changes allowed

### Rule: Performance Timing
- Timer starts when "Register Participation" is tapped (performance_start_time = now)
- Timer stops when "Stop Performance" is tapped (performance_end_time = now)
- Duration = end - start (excludes pause duration; paused timer is frozen)
- Duration is persisted in queue_entry.performance_duration (minutes, float)

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Event** | A karaoke session at a venue with scheduled and actual timing |
| **Queue** | Ordered list of singers in an event |
| **Queue Entry** | One singer in one event; has status (PENDING, PERFORMING, COMPLETED, ABSENT, CANCELLED) |
| **Current Singer** | The queue entry with PERFORMING status |
| **Register Participation** | Action: transition PENDING → PERFORMING, start timer |
| **Stop Performance** | Action: transition PERFORMING → COMPLETED, stop timer, advance queue |
| **Drag-Reorder** | Touch gesture: long-press + drag to change position in queue |
| **Performance Duration** | Time elapsed between performance_start_time and performance_end_time |
| **Completion Estimate** | ETA calculated from average_performance_duration and remaining singers |

---

## Demo Statement

**MVP Completion Demo:**
1. Host creates a new event ("Open Mic Night" @ Jazz Club, scheduled 14:00–22:00)
2. Host taps "Start Event" → event transitions to STARTED
3. Host taps "+ Add Singer" → searches for "João" → enqueues him
4. Host adds 4 more singers via picker (Maria, Carlos, Ana, Bob)
5. Queue shows: João (CURRENT), Maria (Next #1), Carlos (#2), Ana (#3), Bob (#4)
6. Host taps "Select Song" on João → picks "Imagine" from DB
7. Host taps "Register Participation" → João transitions to PERFORMING, timer starts (0:00)
8. Manually wait ~45 seconds (simulating performance)
9. Host taps "Stop Performance" → João moves to COMPLETED, Maria becomes CURRENT, timer resets
10. Host drags Carlos above Ana (reorder) → queue updates visually
11. Host marks Bob as ABSENT via "Mark Absent" → Bob moves to History
12. Host continues registering participation for remaining singers
13. After 5+ singers complete, ETA appears in header ("~15 mins remaining")
14. Host finishes the event → event transitions to FINISHED, queue locks to read-only

---

## Success Criteria

- ✅ All user stories have at least one passing acceptance criterion
- ✅ Queue operations (register, drag, mark absent, enqueue) each work with no lag (< 200ms perceived latency)
- ✅ Performance timer is accurate to ±1 second over 5+ min duration
- ✅ No data loss on app suspend/resume (persistent queue state)
- ✅ ETA calculation triggers after 5 singers and is visually clear
- ✅ Integration with existing Person picker (reuse search-picker component)
- ✅ Integration with existing Song picker (reuse search-picker component)

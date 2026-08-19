# Handoff — UOW Phase 3.5 (repository families / Queue + Event migration)

> **Written 2026-08-19.** Read this first when resuming. It is the Rule 7 §1 active handoff file
> for this item. Everything a fresh session needs is here or linked from here — do **not** glob `Docs/**`.

---

## 1. Where the work stands

Phase 3.5 was registered on 2026-08-04 as *"merge the duplicate repository families"*. **That framing
was wrong and must not be executed as written** — see `README.md § SCOPE CORRECTION (2026-08-18)`.

The two families are not two implementations of one domain model. They serve **two different `Event`
entities against two different tables**:

| | `Domain/Entity/Event` (OLD) | `Domain/Entities/Event` (NEW) |
|---|---|---|
| Table | `Events` | `QueueManagementEvents` (explicit `ToTable`) |
| Migration | `20260107193224_InitialCreate` | `20260610224249_AddQueueManagementEventAndQueueEntry` |
| Name field | `EventName` | `Name` (required) |
| Time | `EventDate` | 4 nullable timestamps |
| State | `QueueActive` (bool) | `Status` (`EventStatus` enum) |
| Extra | — | `Mode`, `CreatedAt`, `ModifiedAt` |
| Children | `Participations` | `QueueEntries` |
| Interface | none | `IAggregateRoot` |

`Venue.Events` and `EventParticipation.Event` still navigate to the **OLD** Event.

Two questions were conflated in the original framing:

1. **Which save semantics are correct** — SETTLED: stage-only, repositories do not call `SaveChangesAsync`.
2. **Which domain model survives** — STILL OPEN, and there is a schema + possible data behind it.

(1) is fixable in place without deleting either folder. Do not let (2) block (1).

---

## 2. What has been done (all merged to `develop`)

| Date | Commit | What |
|------|--------|------|
| 2026-08-18 | `d73db7ab` | Deleted the unregistered `QueueService`/`IQueueService` pair + `VenueService`'s dead `IEventRepository` field (deadcode Item 1 steps 1–2) |
| 2026-08-18 | `bbea3b6a` | Recorded Item 1 completion + the zero-consumer finding |
| 2026-08-19 | `5751c27f` | Demoted REQ-UOW-10 to a guideline in `design.md` per gate 3.4 / F1 |
| 2026-08-19 | *(see LEDGER)* | Deleted the dead OLD-family `IEventRepository`/`EventRepository` pair + its DI line; recorded D13 ratification on `IUnitOfWork.cs` |

**Key consequence:** the OLD family's `Event` half is now *unreferenced code*, not a competing
implementation. Phase 3.5's surface is materially smaller than the 2026-08-04 note assumes.

### Ratified API surface (D13 — final, not provisional)

```csharp
Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
Task          ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);
Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
Task          FlushAsync(CancellationToken ct = default);
```

**REQ-UOW-28 is load-bearing:** resolve every collaborator from the lambda's own `IServiceProvider`
(`sp`) — never from `_`-prefixed constructor fields. This is the single rule most likely to be
violated by an agent writing a boundary from memory.

**REQ-UOW-10 is a guideline, not a gate** (gate 3.4 / F1): one logical line is *preferred*; more is
allowed. Do not report a multi-line boundary as a violation.

---

## 3. Remaining work, in recommended order

### 3a. Convert the NEW (plural) family to stage-only + add UOW boundaries to Queue/Event
The actual BUG-071 fix for this area. **Needs no decision about which `Event` model wins** — it is
purely the save-semantics half, which was never in dispute. Touches `Infra/Repositories/*` and
`Services/QueueServiceNew.cs` + `Services/EventService.cs`.

> **This is very likely the point at which Phase 3.5's BUG-071 exposure closes.** If it does, item
> 3b stops being a blocker and becomes ordinary cleanup. Re-assess before committing to 3b.

### 3b. Reconcile the two `Event` domain models + schema
The real design question, and the **only** part that touches data. **Gated on §4 below.**

### 3c. Deadcode cleanup leftovers (independent, low risk)
Tracked in `Docs/Management/BusinessFeatures/queue-management/queue-deadcode-cleanup.md`:
- **Item 1 step 3** — rename `QueueServiceNew`/`IQueueServiceNew` → `QueueService`/`IQueueService`
  (the "New" suffix is now a smell). Touches `MauiProgram.cs`, `QueueManagementViewModel.cs`, the
  two service/interface files. `MauiProgram.cs` is in the **sequential-only file registry** — never
  run this concurrently with another task touching it.
- **Item 2** — delete the `QueuePage.xaml(.cs)` placeholder (712 bytes, dead). `.xaml` ⇒ **not**
  ITF-eligible; dispatch an implementor.

---

## 4. OPEN QUESTION — blocks 3b only, needs Helder (manual step)

**Is the OLD `Events` table empty in production?**

Working assumption recorded in the spec: *never populated*, because nothing writes to it except the
now-deleted `QueueService`. **This is an assumption, not a verified fact.** It must be confirmed on a
real device before any migration touches the `Events` table.

`.claude/MyVocaList.db` **cannot answer this** — it is a stale February snapshot, 8 migrations
behind, containing only `InitialCreate` + `venuesSeedForTest`, with no `QueueManagementEvents` or
`QueueEntries` tables at all. At that snapshot: `Events`=0, `EventParticipations`=0, `Venues`=498.

### Manual procedure (Helder) — run from the main repo, branch `develop`, no worktree needed

Device must be connected with USB debugging on, and the app must be a debuggable build.

```bash
# 1. Confirm the device is visible
/c/Android/platform-tools/adb devices

# 2. Pull the live database out of the app sandbox
/c/Android/platform-tools/adb exec-out run-as com.companyname.myvocalist \
  cat databases/MyVocaList.db > /c/Users/helde/Desktop/live-MyVocaList.db

# 3. Answer the question
/c/Android/platform-tools/sqlite3 /c/Users/helde/Desktop/live-MyVocaList.db \
  "SELECT 'Events', COUNT(*) FROM Events
   UNION ALL SELECT 'EventParticipations', COUNT(*) FROM EventParticipations
   UNION ALL SELECT 'QueueManagementEvents', COUNT(*) FROM QueueManagementEvents
   UNION ALL SELECT 'QueueEntries', COUNT(*) FROM QueueEntries;"
```

- If the package name in step 2 is wrong, get it with
  `/c/Android/platform-tools/adb shell pm list packages | grep -i voca`.
- If `run-as` is refused, the installed build is not debuggable — rebuild and deploy a Debug build.

**Record the four counts in this file under §5 before starting 3b.**

**Decision rule:** `Events`=0 **and** `EventParticipations`=0 ⇒ the OLD model can be dropped with a
plain destructive migration, no data move. Any non-zero count ⇒ 3b needs a data-migration design and
must go back through the brainstorming architectural path.

---

## 5. Verified data counts (fill in after running §4)

| Table | Count | Source | Date |
|-------|-------|--------|------|
| `Events` | *(pending)* | | |
| `EventParticipations` | *(pending)* | | |
| `QueueManagementEvents` | *(pending)* | | |
| `QueueEntries` | *(pending)* | | |

---

## 6. Process state

- **Brainstorming path:** architectural. Currently at step 1 (*explore project context*) **complete**;
  step 4 (*propose 2–3 approaches*) **not started**. The HARD GATE applies — no implementation for
  3a/3b until Helder approves a presented design.
- **Test baseline on `develop`:** 590 passed / 0 failed / 0 skipped.
- **Branch discipline:** all code work in a worktree on a task branch based on `develop`
  (`git merge-base --is-ancestor develop HEAD` must pass). Docs land on `develop` directly.
- **`rtk` hazard (recurring, bit this session 5+ times):** the `rtk` proxy rewrites `git` and `grep`
  and **silently drops lines**. `git log --oneline` has shown a stale HEAD after a real merge, and a
  consumer census came back with 2 of 4 entries. Verify every census and every SHA with two
  independent methods (`git rev-parse`, the `Grep` tool, direct `sed`/`cat`). Also note
  `.claudeignore` blocks `Glob` on `Docs/**` — use `git ls-files` instead.

## 7. Known pre-existing issues (not part of this item)

BUG-069 / BUG-070 (unknown whether they still reproduce) · BUG-075 (pre-commit hook needs
`--no-restore`; confirmed worktree-specific — a `develop` commit runs the hook normally) ·
BUG-076 (flaky parallel-SQLite `ObjectDisposedException` in `QueueRepositoryTests`; run in
isolation) · the BUG-067 defect-class audit · ~10 dead constructor-only fields across the four
pilot services.

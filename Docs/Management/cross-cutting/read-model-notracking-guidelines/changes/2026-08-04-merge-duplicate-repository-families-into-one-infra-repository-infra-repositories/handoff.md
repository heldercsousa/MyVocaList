# Handoff — UOW Phase 3.5 (repository families / Queue + Event migration)

> **Written 2026-08-19; §4/§5 revised 2026-08-20.** Read this first when resuming. It is the Rule 7 §1 active handoff file
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
| 2026-08-19 | `45871f70` | This handoff added (+ `.sln` registration) |
| 2026-08-19 | `ee6cebcb` | LEDGER next-action corrected — it still told a resuming session to execute the INVERTED direction |
| 2026-08-19 | `e5ec44b1` (merge of `d9fb79c9`) | Deleted the dead OLD-family `IEventRepository`/`EventRepository` pair (14 + 53 lines) + its DI line at `ServiceCollectionExtensions.cs:28`; recorded D13 ratification on `IUnitOfWork.cs`. 4 files, +2/−71. Build 0 errors, **590/590 green on develop** after merge. Worktree and branch removed; zero unmerged commits verified. |

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

## 2b. FROZEN — the whole Event/Queue area (Helder, 2026-08-20)

> **Read this before doing anything in §3. Items 3a and 3b are frozen and must not be executed.**

Helder ruled on 2026-08-20 that the Event and Queue feature area is **frozen pending a re-plan**.
It is not to be refactored, converted, renamed, or cleaned up in its current shape, because the
shape itself is going to change.

**What is frozen (do not touch):**

- `Services/EventService.cs`, `Services/QueueServiceNew.cs`
- `Infra/Repositories/EventRepository.cs`, `Infra/Repositories/QueueRepository.cs`
- `Domain/Interfaces/IEventRepository.cs`, `Domain/Interfaces/IQueueRepository.cs`,
  `Domain/ServicesInterfaces/IEventService.cs`, `Domain/ServicesInterfaces/IQueueServiceNew.cs`
- `MyVocaList/UI/ViewModels/QueueManagementViewModel.cs` and the Queue/Event pages
- The 21 tests covering them (13 `EventRepositoryTests`, 5 `QueueRepositoryTests`,
  3 `QueueManagementViewModelTests`) — **frozen, not deleted**, so the coverage survives the re-plan
- The `TODO [BUG-071 / UOW]` markers — left deliberately in place as the signal to the re-planner

**Explicitly unchanged as well** (Helder, same ruling): entity/EF/DbContext **definitions**
(`AppDbContext` DbSets, all four `IEntityTypeConfiguration` classes, `Venue.Events`,
`EventParticipation.Event`), the **UI entry points**, and the **DI registrations**. No migration is
triggered by any of this.

### Why no code change was needed to satisfy "executions must disappear"

The ruling asked that Event/Queue code stop *executing* while its definitions stay intact. A
reachability trace on 2026-08-20 established that **it already does not execute** — verified by two
independent methods:

- `QueuePage` (Shell's initial content) and `EventsPage` are static "under construction"
  placeholders — no `BindingContext`, no DI, no service calls.
- The route `queue-management` occurs **exactly once** in the entire solution: its own
  `FlyoutItem` declaration in `AppShell.xaml:108`. Nothing navigates to it; there is no
  `Routes.QueueManagement` constant and it is absent from `NavigationConfig.BuildMenuGroups`.
- App startup (`App.xaml.cs`, `AppShell.xaml.cs`, `MauiProgram.cs`) touches no Event/Queue
  service, repository, or DbSet; there is no seeding of `Events`/`QueueManagementEvents`/`QueueEntries`.
- Independently corroborated by the pre-existing backlog note *"QueueManagementPage is unreachable
  in the app; Helder to re-prioritize."*

So `QueueManagementViewModel`'s calls into `IEventService`/`IQueueServiceNew` are real code that no
user can reach. **Zero files were changed.** Neutralising method bodies was considered and rejected:
it would have meant editing the very code being frozen, and would be harder to unwind at re-plan
time than leaving it inert.

**Consequence — BUG-071 is CLOSED** (✅ Fixed, closed 2026-08). It had been held open only by the
"Queue and Event still carry the defect" gate; with that code unreachable and frozen, no live path
carries the defect. The closure note in the bug's `README.md` states plainly that this is a *scope*
closure, not a claim the Queue/Event code was fixed.

**If Queue/Event is ever made reachable again** — e.g. the pending *"Queue Entry Point Redesign —
QueuePage as CRUD event list"* backlog item — the unit-of-work conversion of those four files is a
**prerequisite of that work**, not a bug fix. Anything in §3 below is superseded by the re-plan.

---

## 3. Remaining work, in recommended order

### 3a. ~~Convert the NEW (plural) family to stage-only~~ — **FROZEN 2026-08-20, do not execute (§2b)**
The actual BUG-071 fix for this area. **Needs no decision about which `Event` model wins** — it is
purely the save-semantics half, which was never in dispute. Touches `Infra/Repositories/*` and
`Services/QueueServiceNew.cs` + `Services/EventService.cs`.

> **This is very likely the point at which Phase 3.5's BUG-071 exposure closes.** If it does, item
> 3b stops being a blocker and becomes ordinary cleanup. Re-assess before committing to 3b.

### 3b. ~~Reconcile the two `Event` domain models + schema~~ — **FROZEN 2026-08-20 (§2b)**
The real design question, and the only part that touches schema. **No longer gated on data** — §4
resolved 2026-08-20: there is no production deployment, so the OLD model can be dropped with a plain
destructive migration. What remains is the code-shape decision (which `Event` model survives, and how
`Venue.Events` / `EventParticipation.Event` are re-pointed), which still needs a design + Helder's
approval before implementation.

### 3c. Deadcode cleanup leftovers (independent, low risk)
Tracked in `Docs/Management/BusinessFeatures/queue-management/queue-deadcode-cleanup.md`:
- **Item 1 step 3** — rename `QueueServiceNew`/`IQueueServiceNew` → `QueueService`/`IQueueService`
  (the "New" suffix is now a smell). Touches `MauiProgram.cs`, `QueueManagementViewModel.cs`, the
  two service/interface files. `MauiProgram.cs` is in the **sequential-only file registry** — never
  run this concurrently with another task touching it.
- **Item 2** — delete the `QueuePage.xaml(.cs)` placeholder (712 bytes, dead). `.xaml` ⇒ **not**
  ITF-eligible; dispatch an implementor.

---

## 4. RESOLVED — no production deployment exists (Helder, 2026-08-20)

**Original question:** is the OLD `Events` table empty in production?

**Answer: the question is moot.** Helder confirmed on 2026-08-20 that **there is no production
version of the app** — no deployed installs, no user data at risk. Event data may be discarded
freely if that is the chosen approach.

**Consequence for 3b:** the decision rule in the original §4 resolves to its permissive branch.
The OLD `Domain/Entity/Event` model + its `Events` table can be dropped with a **plain destructive
migration** — no data-migration design, no return trip through the architectural brainstorming path
for data-preservation reasons. (A design gate still applies to the *model reconciliation* itself,
which is a code-shape decision, not a data one.)

The `adb pull` + `sqlite3` count procedure previously recorded here is **obsolete and was never
run**; it is deliberately not preserved, because executing it would imply a production concern that
does not exist. Should a deployment ever precede this work, restore the procedure from
`git show 45871f70 -- <this file>`.

---

## 5. Verified data counts — N/A

Not applicable: no production deployment exists (§4). No counts were taken and none are required.

---

## 6. Process state

- **Area status: FROZEN** (§2b) — 3a and 3b are not executable; only §3c is arguably live, and
  it too touches frozen files, so treat the whole item as parked pending the Queue/Event re-plan.

- **Brainstorming path:** architectural. Step 1 (*explore project context*) **complete**; step 4
  (*propose 2–3 approaches*) **in progress 2026-08-20 for 3a**. The HARD GATE applies — no
  implementation for 3a/3b until Helder approves a presented design.
- **Open data question: CLOSED** (§4) — no production version exists.
- **Test baseline on `develop`:** 590 passed / 0 failed / 0 skipped.
- **Branch discipline:** all code work in a worktree on a task branch based on `develop`
  (`git merge-base --is-ancestor develop HEAD` must pass). Docs land on `develop` directly.
- **`rtk` hazard (recurring, bit this session 5+ times):** the `rtk` proxy rewrites `git` and `grep`
  and **silently drops lines**. `git log --oneline` has shown a stale HEAD after a real merge, and a
  consumer census came back with 2 of 4 entries. Verify every census and every SHA with two
  independent methods (`git rev-parse`, the `Grep` tool, direct `sed`/`cat`). Also note
  `.claudeignore` blocks `Glob` on `Docs/**` — use `git ls-files` instead.

## 7. Known pre-existing issues (not part of this item)

BUG-069 / BUG-070 (unknown whether they still reproduce) · **BUG-075 — likely stale, re-verify before working it:** on 2026-08-19 the pre-commit hook ran
*normally inside a worktree* (`MyVocaList-wt-eventrepo-dead`), executing its own full gate and
reporting `pre-commit: build + tests green` with 590/590. That contradicts the recorded
"inoperative in worktrees" symptom, so the bug is either fixed or conditional on something not yet
identified. Reproduce before spending time on a fix. ·
BUG-076 (flaky parallel-SQLite `ObjectDisposedException` in `QueueRepositoryTests`; run in
isolation) · the BUG-067 defect-class audit · ~10 dead constructor-only fields across the four
pilot services.

---
id: BUG-071
title: "BUG-071 (alias BUG-068): Edit-mode song save fails — EF Core identity conflict (Critical)"
status: ✅ Fixed
severity: Critical
target: 2026-08-03
section: BusinessFeatures
parent: artists-songs
goal: editing a saved song must persist; today one tap reports success but writes nothing (silent data loss) and a second tap throws an EF tracking conflict.
gate: "Closed 2026-08-20. Fixed for Songs/Artists by the UOW pilot; the Queue/Event remainder was verified unreachable and frozen, so no live path carries it."
closed: 2026-08
kind: bug
---

# BUG-071 (alias BUG-068): Edit-mode song save fails — EF Core identity conflict (Critical)

> **Two ids, one defect.** This bug is recorded as **BUG-068** throughout
> `changes/2026-07-21-inline-artist-create/task-log.md`, `LEDGER.md` and Helder's device
> reports. `backlog_gen.py` could not mint `BUG-068` — its high-water mark had already
> advanced to `BUG-071` because the allocator scans `task-log.md` prose (REQ-SEV-11a), and
> it can only ever mint the *next* id, never backfill one already recorded. Registered as
> `BUG-071` on Helder's instruction (2026-08-03). **Both numbers refer to this folder.**

## Symptom

Found by Helder on device + emulator during INLINE-AC **T10 re-run #5** (2026-08-02),
identical behaviour on both. Two faces:

1. Tap an artist suggestion **once**, then Save → the UI reports **success**, but nothing is
   persisted. **Silent data loss**, no exception surfaced.
2. Tap a suggestion, then tap the re-shown row (BUG-069), then Save → *"Failed to save song.
   Please try again."* plus:

```
System.InvalidOperationException: The instance of entity type 'Song' cannot be tracked
because another instance with the same key value for {'Id'} is already being tracked.
   at MyVocaList.Infra.Repository.SongRepository.UpdateAsync(...) SongRepository.cs:line 135
   at MyVocaList.Services.SongService.UpdateSongAsync(...)          SongService.cs:line 156
```

## Root cause (proven, not inferred)

`AddDbContext<AppDbContext>` (`MauiProgram.cs:61`) registers **Scoped**, but **MAUI creates no
per-page DI scope** — the root `ServiceProvider` yields one `AppDbContext` for the app's entire
session. `QueryTrackingBehavior.NoTracking` (global, `MauiProgram.cs:67`) suppresses tracking of
*query results* only; it does **not** detach entities left tracked by an earlier `Add`/`Update` +
`SaveChangesAsync` on the same row. `GetByIdAsync`'s fresh untracked read therefore collides with
the stale tracked instance when `DbSet.Update(song)` attaches.

Proven with a `ChangeTracker.DebugView` dump; a control test showed the conflict fires with **zero
prior reads in the edit session**, so a single `UpdateSongAsync` is enough once the row was ever
written before.

BUG-067's fix (the missing `artistId` parameter) and REQ-ACREATE-16 are **correct and stay** — they
were necessary but not sufficient. The write now *reaches* the repository, and the repository throws.

## Current fix is a STOPGAP — scheduled for deletion

Commit `1a114c1` on `feat/inline-artist-create` makes `SongRepository.UpdateAsync` scan
`ChangeTracker.Entries<Song>()` and merge onto the tracked entry via `CurrentValues.SetValues`.
It is verified (PASS, no blockers) and 537/537 tests pass, **but it is not the intended design**:

- it is a **per-repository** workaround for an application-wide DI lifetime defect;
- the same unguarded shape remains in `ArtistRepository`, `BaseRepository<T>` (→ `PersonRepository`,
  `VenueRepository`, `EventParticipationRepository`, `EventRepository`), `Infra/Repositories/EventRepository`
  and `QueueRepository`;
- it hand-rolls what EF Core already ships as `UpdatingIdentityResolutionInterceptor`.

It remains until the unit-of-work waves delete it. It is **deleted** by
`cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/`,
which establishes the correct unit-of-work pattern (Helder's decision, 2026-08-03: fix it properly
rather than mask it — the app is pre-production). *(The prior justification — that it stays only so
Helder's on-device T10 re-run #6 is not blocked — is withdrawn 2026-08-04: there is no reason to
device-test a stopgap that is about to be deleted and replaced.)*

## Face 1 is NOT closed

Only face 2 (the exception) is fixed. Face 1 (silent success, nothing persisted) is **not** explained
by this mechanism — the identity conflict is deterministic, not intermittent, so it either throws or
persists correctly. Face 1 was never observed to be fixed, and it is now closed **by inference, not
observation**: the on-device T10 re-run #6 that would have re-confirmed it on device is not
happening (Helder cancelled the gate 2026-08-04 — see the unit-of-work spec's § 8 decision). The
inferred explanation is a **BUG-069** symptom (the selection reverting before Save reads it), tracked
separately. **This is inference, not verified evidence — do not cite face 1 as confirmed fixed.**

## Why the unit suite missed it

`SongServiceTests` mock `ISongRepository`, so `DbSet.Update` never executes, and `SongRepository` had
no real-SQLite update-after-read test — 535/535 was green while every edit-mode save failed on device.
Closed by `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (real SQLite via
`TestDbContextFactory`, real repositories, Red observed before Green).

## Regression risk of the stopgap

Low, and confined to `SongRepository`. `Song` has no concurrency token and its navigations
(`OriginalArtist`, `CatalogEntries`) are not written by the update path, so scalar/FK-only copying
loses nothing.


---

## Closure (2026-08-20)

**Closed by Helder's ruling that the Event/Queue area is frozen pending a re-plan.**

The bug was held open solely on the gate *"Queue and Event still carry the defect until
Phase 3.5"*. That gate no longer describes a live risk:

- The four pilot services (`SongService`, `ArtistService`, `ArtistResolutionService`,
  `SongResolutionService`) were fixed and verified on device — that half is genuinely done.
- The Queue/Event half was **verified unreachable on 2026-08-20**: no user-reachable code path
  executes `EventService`, `QueueServiceNew`, `EventRepository` or `QueueRepository`. `QueuePage`
  and `EventsPage` are static placeholders; the `queue-management` route appears exactly once in
  the solution — its own `FlyoutItem` declaration in `AppShell.xaml` — and nothing navigates to
  it. App startup touches no Event/Queue service, repository or DbSet. (Independently corroborated
  by the pre-existing backlog note *"QueueManagementPage is unreachable in the app"*.)

So the residual defect sits in code that cannot run. It is not a latent production risk; it is a
property of code that will be re-planned before it is ever reachable again.

**This is a scope closure, not a claim that the Queue/Event code was fixed.** The
`TODO [BUG-071 / UOW]` markers in `Infra/Repositories/EventRepository.cs`,
`Infra/Repositories/QueueRepository.cs`, `Services/EventService.cs` and
`Services/QueueServiceNew.cs` are deliberately **left in place** — they are the correct signal to
whoever re-plans the feature. Do not read their presence as an open bug.

**If Queue/Event is ever made reachable again** (e.g. the pending *"Queue Entry Point Redesign —
QueuePage as CRUD event list"* backlog item), the unit-of-work conversion of those four files is a
prerequisite of that work, not a bug fix. Re-register it as a task of the re-plan.

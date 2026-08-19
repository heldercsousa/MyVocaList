---
id: merge-duplicate-repository-families-into-one-infra-repository-infra-repositories
title: Merge duplicate repository families into one (Infra/Repository + Infra/Repositories)
status: 💡 Pending
target: 2026-08-04
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Two repository folders exist by accident from prior sessions; they must become one family so later refactors touch a single code path.
gate: Runs AFTER the pilot proves the pattern (Helder 2026-08-04, option a); merged with the deferred Queue/Event unit-of-work item.
kind: change
---

# Merge duplicate repository families into one (Infra/Repository + Infra/Repositories)

Two repository folders exist by accident from prior sessions; they must become one family so later refactors touch a single code path.

## Direction (Helder, 2026-08-04)

> "We must get rid of 1 repository family, the one out of pattern. And migrate callers to refer to
> the pattern compliant repository."

**The family to delete is `Infra/Repositories/` — despite being the newer one.** The survivor is
`Infra/Repository/`. Verified 2026-08-04:

| | `Infra/Repository/` (KEEP) | `Infra/Repositories/` (DELETE) |
|---|---|---|
| Repositories | 10 (Artist, Backup, Base, Catalog, EventParticipation, Event, Person, SongKaraokeUrl, Song, Venue) | 2 (Event, Queue) |
| Entity namespace | `Domain/Entity/` — 13 entities | `Domain/Entities/` — 2 (`Event`, `QueueEntry`) |
| Save semantics | **stage-only** — `AddAsync`/`UpdateAsync`/`DeleteAsync` never call `SaveChangesAsync` | **embedded** — `AddAsync`/`UpdateAsync` call `SaveChangesAsync` internally |
| Consumers | every service except the two below | `QueueServiceNew`, `EventService` only |

Stage-only is the pattern-compliant shape: a repository must not decide the commit boundary — the
unit of work does. This is the same rule REQ-UOW-11 encodes (remove embedded saves), and all six
known embedded saves live in the family being deleted.

## Why this is not a mechanical move

1. **`Event` is duplicated across both entity namespaces with different shapes** —
   `Domain/Entity/Event.cs` (441 B) and `Domain/Entities/Event.cs` (897 B). The merge must reconcile
   two divergent domain models of one concept, not just move files. `QueueEntry` has no counterpart
   and simply relocates.

2. **`QueueServiceNew` and `EventService` contain ZERO `SaveChangesAsync` calls** (verified by grep,
   2026-08-04) — they depend entirely on the embedded saves. Converting their repositories to
   stage-only without adding a commit boundary in the same change makes both services **silently stop
   persisting**. This is the exact defect class the parent unit-of-work item exists to eliminate, and
   the codebase has a prior recorded incident of it (`Docs/Changelog/Archive/changelog-jan2026-to-jun2026.md:50`).

## Sequencing — DECIDED by Helder 2026-08-04: **option (a), but NOT YET**

> "a, but not yet!"

This item is merged with
`changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/` and both run
**after the pilot** (Phase 3) has proven the unit-of-work pattern — so the merge targets a known shape
rather than a provisional one (the API shape is itself deferred to the pilot by D13).

**This inverts the ordering the parent spec currently states.** The parent's `gate:` and § 10 say this
merge is a prerequisite that must land *before* Phase 0. Under Helder's decision the real order is:

```
Phase 0 → 1 → 2 (pilot) → 3 (VERIFY, hard gate)
   → THEN: family merge + Queue/Event unit-of-work migration, together
   → THEN: Phase 4+ spread
```

The parent spec's prerequisite wording must be updated to match before implementation planning treats
it as blocking. Validity of the inversion rests on the pilot being independent of the deleted family —
the pilot's four services (`SongService`, `ArtistService`, `ArtistResolutionService`,
`SongResolutionService`) all sit in `Infra/Repository/` + `Domain/Entity/`. Verified 2026-08-04; see
the parent spec's § 10 note.

## Original analysis (options as presented)

Because of point 2, this merge **cannot safely be completed independently** of giving Queue/Event a
commit boundary, and that boundary is the unit of work. So this item and
`changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/` are
arguably one piece of work. Doing them separately means either an intermediate broken state, or
adding explicit `SaveChangesAsync` calls that are deleted again days later.

Options:

- **(a) Merge the two items** — do the family merge and the Queue/Event unit-of-work migration
  together, after the pilot proves the pattern (so the target shape is known).
- **(b) Keep them separate** — this item adds explicit `SaveChangesAsync` calls to `QueueServiceNew`/
  `EventService` as a temporary commit boundary, which the deferred item later removes. Safe but
  throwaway work.
- **(c) Narrow this item** — merge only what does not touch Queue/Event (in practice: almost nothing,
  since the deleted family serves only those two services), and fold the rest into the deferred item.

## Scope exception required

The parent spec's D12 puts all Queue/Event code out of scope. This item necessarily touches it (that
is where the deleted family's only consumers are). D12's exclusion governs the **rollout phases**; this
prerequisite item is explicitly exempt. Record that exemption before work starts.

## Related

The in-source `TODO [BUG-071 / UOW]` markers (`git grep -n "TODO \[BUG-071 / UOW\]"`) cover the same
Queue/Event files this item edits — reconcile them rather than leaving both.

---

## SCOPE CORRECTION (2026-08-18) — this is not a folder merge

A fresh source census against current HEAD contradicts the premise of the 2026-08-04 direction above.
**The 2026-08-04 "delete `Infra/Repositories/`" decision should not be executed as written** until the
question below is settled.

### The two `Event` entities are two live tables, not two copies of one model

Both are mapped, on separate `DbSet`s, to separate tables, with no collision:

| | `Domain/Entity/Event` (OLD) | `Domain/Entities/Event` (NEW) |
|---|---|---|
| Table | `Events` | `QueueManagementEvents` (explicit `ToTable`) |
| Created by | `20260107193224_InitialCreate` | `20260610224249_AddQueueManagementEventAndQueueEntry` |
| Name field | `EventName` | `Name` (required) |
| Time | `EventDate` (one) | `ScheduledStartTime`/`ScheduledEndTime`/`ActualStartTime`/`ActualEndTime` |
| State | `QueueActive` (bool) | `Status` (`EventStatus` enum) |
| Also | — | `Mode`, `CreatedAt`, `ModifiedAt` |
| Children | `Participations` → `EventParticipation` | `QueueEntries` → `QueueEntry` |
| Marker iface | none | `IAggregateRoot` |

**No migration ever dropped the old `Events` table.** `AppDbContextModelSnapshot.cs` carries both
(`ToTable("Events")` and `ToTable("QueueManagementEvents")`).

### Why that invalidates the recorded direction

The 2026-08-04 decision picked the surviving family on **save semantics** (stage-only is
pattern-compliant; embedded saves are not). That reasoning is correct *about save semantics* — and is
not in dispute. But it assumed the two families implement one domain model. They do not: the family
marked for deletion serves the **newer, richer** model that the live queue-management feature actually
uses, while the surviving family's `EventRepository` serves the **older** model whose only consumers
are `QueueService` (dead, unregistered) and `VenueService`'s dead field.

Two independent questions were conflated:

1. **Which save semantics are correct** — settled: stage-only, the unit of work owns the boundary.
2. **Which domain model survives** — undecided, and it has a schema (and possibly data) behind it.

(1) is fixable *in place*: convert the plural family to stage-only and give its consumers a unit-of-work
boundary. It does not require deleting either folder.

### Deleting the old `Event` is a domain-model change

`Venue.Events` and `EventParticipation.Event` both navigate to the OLD `Event`. Removing it re-points
those navigations plus `IEventRepository`, `IEventParticipationRepository`, and needs a migration
against the `Events` table.

### Sequencing decided by Helder 2026-08-18: **dead-code first, then re-decide**

Land the already-sanctioned dead-code deletions before designing anything
(`BusinessFeatures/queue-management/queue-deadcode-cleanup.md` Item 1 + `VenueService`'s dead field),
then re-census. Expected effect: `Infra/Repository/EventRepository` + `Domain.RepositoryInterface.IEventRepository`
lose their last consumers, and the "which family survives" question largely dissolves instead of
needing a design decision. The Phase 3.5 spec is written against the reduced surface, not this one.

### Open — data safety

`.claude/MyVocaList.db` cannot answer whether the old `Events` table holds real data: it is a stale
February snapshot (only `InitialCreate` + `venuesSeedForTest` applied, 8 migrations behind, no
`QueueManagementEvents`/`QueueEntries` tables at all). At that state `Events` = 0 rows,
`EventParticipations` = 0 rows.

**Working assumption, to be stated explicitly in the spec and confirmed before any migration:** the old
`Events` table has never been populated in production — nothing writes to it except the dead,
unregistered `QueueService`, so no code path could have filled it since June 2026. Confirming this
needs the live device DB, which is Helder's to pull.

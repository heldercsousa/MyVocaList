---
id: apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred
title: Apply the unit-of-work pattern to Queue and Event entities (deferred)
status: 💡 Pending
target: 2026-08-04
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Queue and Event code is excluded from the unit-of-work rollout pending their own full refactor, so they keep using the session-lifetime context and stay exposed to the tracking-conflict defect.
gate: Starts only once the pattern is established in the guides; the six embedded repository saves live here.
kind: change
---

# Apply the unit-of-work pattern to Queue and Event entities (deferred)

Queue and Event code is excluded from the unit-of-work rollout pending their own full refactor, so they keep using the session-lifetime context and stay exposed to the tracking-conflict defect.

## In-code markers (added 2026-08-04, Helder's instruction)

Every Queue/Event site that still carries the defect is commented in source with a
`TODO [BUG-071 / UOW]` marker, so the exposure is visible where the code is read rather than
only here. Find them all with:

```
git grep -n "TODO \[BUG-071 / UOW\]"
```

Whoever picks up this item should **remove every one of those markers as part of the work** — a
surviving marker after this item closes means a site was missed. The markers deliberately do
**not** name a concrete API (`ExecuteAsync` etc.): the shape is provisional until the pilot's
Phase 3 decision (D13), so they point at the spec folder as the authority instead.

## Why this is a live risk, not merely deferred work

`AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` also registers `AppDbContext` as
an ordinary scoped service — that property is what lets migrated and unmigrated code coexist, and
it is why the rollout needs no repository signature changes. It cuts both ways: every
constructor-injected repository in Queue/Event code still resolves the **same single window-scope
`AppDbContext`**. Because that code keeps the BUG-071 defect by design, a throw on a Queue or
Event screen can leave the shared context unusable for the rest of the session — including for
the Venue/Artist/Person/Song features that this rollout fixes.

Two consequences to carry forward:

1. **A green Phase 3 pilot proves the pattern, not the absence of BUG-071.** The defect class
   remains reachable through Queue/Event screens until this item lands.
2. **This item has no target date.** The risk persists for as long as that is true. Helder's
   call — flagged here so the open-endedness is a decision rather than an oversight.

## Known boundary crossings (verified against source, 2026-08-04)

| Crossing | Site | Consequence for this item |
|---|---|---|
| Excluded → in-scope (repository layer) | `Services/QueueService.cs:134` calls `SaveChangesAsync()` on the **in-scope** `_venueRepository` | Blocks removing `IBaseRepository.SaveChangesAsync` in the main rollout's Phase 1; that removal is deferred to **this** item |
| Excluded → in-scope (service layer) | `QueueService.AddPersonToQueueAsync` → `PersonService.CreatePersonAsync` / `GetPersonByNameAsync` | Benign once `PersonService` is wrapped — the wrapped side opens and disposes its own context |
| In-scope → excluded (dead) | `Services/VenueService.cs:16` injects an **unused** `IEventRepository` | Delete the field; do not migrate it |
| UI layer | `QueueManagementViewModel` injects `IEventService` + `IQueueServiceNew` **and** `IPersonRepository`/`ISongRepository` | Only the repository usage migrates in the main rollout; the two service fields wait for this item |

## Embedded repository saves (all six live here)

`Infra/Repository/EventRepository.cs:37`, `Infra/Repositories/QueueRepository.cs:56,67,93`,
`Infra/Repositories/EventRepository.cs:66,77`. Zero in-scope repositories embed a save, which is
why the main rollout could drop this from its scope — but the defect they represent is not fixed,
only relocated to this item.

> Note: `EventRepository` exists in **two** repository families (`Infra/Repository/` and
> `Infra/Repositories/`). Merging those families is a separate prerequisite item; check whether it
> has landed before starting here.

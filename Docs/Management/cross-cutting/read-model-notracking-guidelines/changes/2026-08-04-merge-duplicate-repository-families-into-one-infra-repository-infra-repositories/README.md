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

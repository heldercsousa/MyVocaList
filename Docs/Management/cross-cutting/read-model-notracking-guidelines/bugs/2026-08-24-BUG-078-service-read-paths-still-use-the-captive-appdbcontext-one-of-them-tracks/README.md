---
id: BUG-078
title: Service read paths still use the captive AppDbContext; one of them tracks
status: 💡 Pending
severity: Major
target: 2026-08-24
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Service read methods call the constructor-injected repository, which holds the app-lifetime AppDbContext that has no scope. ArtistRepository.GetByIdAsync is the only .AsTracking() read in the repository layer, and ArtistService.GetDeleteConfirmationAsync reaches it outside any unit of work.
kind: bug
---

# Service read paths still use the captive AppDbContext; one of them tracks

Service read methods call the constructor-injected repository, which holds the app-lifetime AppDbContext that has no scope. ArtistRepository.GetByIdAsync is the only .AsTracking() read in the repository layer, and ArtistService.GetDeleteConfirmationAsync reaches it outside any unit of work.

## How this was found

During UoW Phase 4.6b's captive-dependency audit. The first audit pass concluded every UI hit was safe
because the services it calls "wrap in `IUnitOfWork`". A second, independent read-only pass rejected
that reasoning as **too coarse**: a service can wrap its *writes* and still read through the captive
field. The orchestrator then verified the second pass's claim directly, and it holds.

This is the correction to an over-claim: the rollout made every **write** go through `IUnitOfWork`, and
`IBaseRepository<T>.SaveChangesAsync()` is gone so no write can escape. **Reads were never in the
rollout's scope** and still run on the constructor-injected repository.

## The defect

`AppDbContext` is registered scoped and MAUI creates no scope, so the constructor-injected repository in
each service holds a context that lives for the whole app session. Every service read goes through it:

`ArtistService.cs:156,168,177` · `PersonService.cs:178,182,190,199,212` · `SongService.cs:269,277-278,312`
· `VenueService.cs:199` · `CatalogService.cs:32` · `ArtistSuggestionService.cs:42,43,79` ·
`SongSuggestionService.cs:46,133,165`

**Most of these are benign for tracking purposes.** `AppDbContext` sets
`QueryTrackingBehavior.NoTracking` globally (`AppDbContext.cs:37`, `:54`) and the list/search methods add
an explicit `.AsNoTracking()` as defence-in-depth (BUG-018). A no-tracking read leaves nothing in the
change tracker.

**There is exactly one exception, and it is the bug.** A tree-wide census of `Infra/Repository/` found a
single `.AsTracking()`:

```
Infra/Repository/ArtistRepository.cs:80
    => await _db.Artists.AsTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
```

and it is reached outside any unit of work by:

```
Services/ArtistService.cs:177   (GetDeleteConfirmationAsync)
    var artist = await _artistRepository.GetByIdAsync(idList[0], ct);
```

So **every artist delete-confirmation attaches a tracked `Artist` to a context that is never disposed
for the life of the app.**

## Consequences

1. **Unbounded change-tracker growth.** Each delete-confirmation adds an entity that is never released.
   A long session accumulates them.
2. **Stale reads — the user-visible one.** EF returns the already-tracked instance for a subsequent
   `GetByIdAsync` of the same id, ignoring the database. Writes now commit through a *different*,
   fresh context, so a rename committed via `IUnitOfWork` is **not** reflected in the captive context's
   cached copy. The delete-confirmation dialog can therefore show the artist's **old** name after a
   successful rename.

Note this is *not* the BUG-068 tracking-conflict shape: the write context and the read context are now
separate, so nothing throws. It fails silently instead, which is why no test caught it.

## Why this blocks Phase 4.7 (`DbLoadGate` removal)

`CrudListViewModelBase.cs:16`'s static `SemaphoreSlim` exists because "all CRUD list ViewModels share one
effectively-singleton `AppDbContext` (MAUI has no per-page scope), so at most one DB load may run at a
time app-wide" — `DbContext` is not thread-safe for concurrent operations. **Reads still share that one
context**, so the gate's justification is fully intact. Phase 4.7 must NOT proceed until reads are
scoped. Removing the gate now would permit concurrent operations on a shared `DbContext`.

Separately, the `Task.Run(...)` offloads in `LoadFirstPageAsync` / `LoadMoreAsync` are a **different**
mitigation — for `Microsoft.Data.Sqlite` completing async methods synchronously on the calling thread —
whose revert trigger is `INFRA_MSSQL`, not this work. They must survive any removal of the gate. The two
rationales sit in one comment block but address different mechanisms; conflating them would retire the
wrong one.

## Suggested direction (not yet decided — needs Helder)

The narrow fix is to stop `GetDeleteConfirmationAsync` reading through the captive field — route it
through `IUnitOfWork`'s read path, resolving the repository from the lambda's own `sp` (REQ-UOW-28), or
have `GetByIdAsync` not track when the caller only needs to display a name.

The broad fix is to extend the pattern to **all** reads, which would also retire `DbLoadGate` and close
Phase 4.7 properly. That is a larger piece of work than the narrow fix and deserves its own spec, since
`ExecuteReadAsync` already exists on `IUnitOfWork` and is currently reserved for multi-repository read
joins.

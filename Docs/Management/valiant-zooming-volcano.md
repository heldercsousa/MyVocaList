# BUG-018 — Final Implementation Plan
# ArtistFormPage Edit Save crash: EF Core duplicate entity tracking

**Severity:** Critical  
**Feature:** Artists & Songs Catalog  
**Plan written:** 2026-06-27

---

## Context

The Singleton `AppDbContext` + `Task.Run`-offloaded queries produce concurrent, non-thread-safe
DbContext access. EF Core's ChangeTracker becomes inconsistent under concurrent reads. The
existing ChangeTracker guard in `UpdateAsync` (lines 122–130, `ArtistRepository.cs`) runs
`ChangeTracker.Entries<Artist>().FirstOrDefault(...)` on a corrupted collection, finds nothing,
falls through to `_db.Artists.Update(artist)`, and EF throws `InvalidOperationException`
because a stale tracked instance IS present but the guard missed it.

---

## Architectural decisions (settled in design session)

| Decision | Rationale |
|---|---|
| Global `NoTracking` on `AppDbContext` constructor | Eliminates tracker pollution at the root. Aligns with `efcore-patterns` skill Pattern 1. TestDbContextFactory already mirrors this. |
| Explicit `.AsNoTracking()` kept on list methods | Defence-in-depth + documents intent. Lets the regression test verify the explicit layer independently of the global setting. |
| `.AsTracking()` added to `GetByIdAsync` | Edit path must track so `SaveChangesAsync` picks up mutations without an explicit `Update()` call on the entity the service already holds. |
| Remove ChangeTracker guard from `UpdateAsync` | Guard was unreliable under concurrent access. After the fix, the guard is dead code. |
| `ArtistListItem` record in `Domain/ReadModels/` | Single type end-to-end. No Infra → Contracts dependency. Drops the `Dto` suffix (it lives in Domain, not a transfer layer). Naming: `ArtistListItem` — describes its role in the list, avoids misleading suffixes. |
| SQL column projection in `GetPagedAsync` | Avoid fetching unused columns (`ExternalId`, `CreatedAt`, `UpdatedAt`, etc.) for a page of 20 list rows. EF translates `.Select(a => new ArtistListItem(...))` to a narrow `SELECT`. |
| `SearchByNameAsync` reuses `ArtistListItem` | Shared projection type; `CatalogCount = 0` (search doesn't need it, same as today). Eliminates separate Artist entity mapping in the service. |
| `ArtistListItemDto` deleted from Contracts | Replaced by `ArtistListItem` in Domain. No intermediary mapping needed. |
| `GetByExternalIdAsync` gets `AsNoTracking()` | Import callers create via `AddAsync`, never mutate the returned reference through `UpdateAsync`. |

---

## Files to change

| File | Change |
|---|---|
| `Infra/AppDbContext.cs` | Add `ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking` to constructor |
| `Domain/ReadModels/ArtistListItem.cs` | **New file** — `public record ArtistListItem(int Id, string Name, string ExternalProvider, bool HasManualEdits, int CatalogCount)` |
| `Domain/RepositoryInterface/IArtistRepository.cs` | `GetPagedAsync` return type → `Task<(IEnumerable<ArtistListItem> items, int totalCount)>`. `SearchByNameAsync` return type → `Task<IEnumerable<ArtistListItem>>` |
| `Infra/Repository/ArtistRepository.cs` | See implementation detail below |
| `Services/ArtistService.cs` | Return `ArtistListItem` directly; remove DTO mapping. `IArtistService` return types updated to match. |
| `Domain/ServicesInterfaces/IArtistService.cs` | Update `GetPagedArtistsForListAsync` and `SearchArtistsByNameAsync` return types to `ArtistListItem` |
| `Contracts/DTOs/List/ArtistListItemDto.cs` | **Deleted** |
| All consumers of `ArtistListItemDto` | Update to `ArtistListItem` (primarily `ArtistsViewModel`, `ArtistPickerViewModel`) |
| `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs` | Add regression test |
| `MyVocaList.Tests/Infrastructure/TestDbContextFactory.cs` | No change required |

---

## ArtistRepository implementation detail

```csharp
// Constructor / class top — no change

// GetPagedAsync
var q = _db.Artists.AsNoTracking().AsQueryable(); // explicit + global default = doubly safe

// ... existing filters unchanged ...

var rawItems = await q
    .OrderBy(a => a.Name)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(a => new ArtistListItem(           // ← SQL-level column projection
        a.Id,
        a.Name,
        a.ExternalProvider,
        a.HasManualEdits,
        a.CatalogEntries.Count()))             // ← scalar subquery in one SQL statement
    .ToListAsync(ct);

return (rawItems, totalCount);                 // no intermediate tuple unwrap needed

// SearchByNameAsync
var q = _db.Artists.AsNoTracking().AsQueryable();
// ... existing filter unchanged ...
return await q
    .OrderBy(a => a.Name)
    .Take(maxResults)
    .Select(a => new ArtistListItem(a.Id, a.Name, a.ExternalProvider, a.HasManualEdits, 0))
    .ToListAsync(ct);

// GetByIdAsync
return await _db.Artists.AsTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

// GetByExternalIdAsync
return await _db.Artists.AsNoTracking().FirstOrDefaultAsync(
    a => a.ExternalId == externalId && a.ExternalProvider == provider, ct);

// UpdateAsync — guard removed
public Task UpdateAsync(Artist artist, CancellationToken ct)
{
    _db.Artists.Update(artist);
    return Task.CompletedTask;
}
```

---

## Regression test

The test overrides the global NoTracking on the specific instance to verify that the
**explicit** `.AsNoTracking()` on list methods is the defence-in-depth layer that prevents
tracking even when the global setting is absent. This gives a genuine Red/Green cycle.

```csharp
[Fact]
// [AC] BUG-018: GetPagedAsync must not add entities to the ChangeTracker
public async Task GetPagedAsync_ExplicitNoTracking_DoesNotPollutTracker_AndUpdateSucceeds()
{
    // Override global NoTracking — simulates a context where the global setting is absent
    _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

    // Arrange — seed
    var artist = new Artist { Name = "Test Artist" };
    _db.Artists.Add(artist);
    await _db.SaveChangesAsync();
    _db.ChangeTracker.Clear();

    // Act — list query (must not track despite TrackAll context setting)
    await _repo.GetPagedAsync(1, 20, string.Empty);

    // Assert 1 — explicit AsNoTracking on GetPagedAsync overrides context default
    Assert.Empty(_db.ChangeTracker.Entries<Artist>());

    // Assert 2 — update on the same entity Id succeeds with no tracking conflict
    artist.Name = "Updated Name";
    await _repo.UpdateAsync(artist, CancellationToken.None);
    await _db.SaveChangesAsync();

    var saved = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == artist.Id);
    Assert.Equal("Updated Name", saved.Name);
}
```

**Red:** Remove `.AsNoTracking()` from `GetPagedAsync` — `ChangeTracker.Entries<Artist>()`
returns 1 entry — first Assert fails.  
**Green:** `.AsNoTracking()` present — ChangeTracker empty — both Asserts pass.

---

## Interface changes summary (breaking — internal only)

`IArtistRepository.GetPagedAsync` and `SearchByNameAsync` change return types. All consumers
(`ArtistService`, test classes) are updated in the same commit. No public NuGet API is affected.

---

## Verification checklist

- [ ] `dotnet build` — 0 errors
- [ ] `dotnet test` — 0 failures (regression test Red before fix, Green after)
- [ ] Emulator: open ArtistsPage (list loads), tap Edit on any artist, change name, Save — no crash, name updated in list
- [ ] Emulator: open ArtistsPage, scroll quickly while editing — no crash
- [ ] BUG-018 BACKLOG row → ✅ Fixed

---

## Out of scope (tracked in BACKLOG)

- `GetByExternalIdAsync` import-path tracking behaviour: verified no mutation via returned ref; `AsNoTracking()` added
- `Infra/Repository` + `Infra/Repositories` folder consolidation → separate BACKLOG entry
- Guidelines update for global NoTracking pattern → separate BACKLOG entry (after smoke test)
- Persons / Songs / Venues CRUD read model refactoring → separate BACKLOG entry
- Specification Pattern for cross-provider business logic → future architectural initiative
- `CatalogEntries.Count()` correlated subquery optimisation → existing BACKLOG entry (evidence-driven)

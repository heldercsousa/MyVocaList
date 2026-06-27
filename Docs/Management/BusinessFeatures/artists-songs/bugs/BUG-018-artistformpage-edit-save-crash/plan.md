# Plan: BUG-018 — ArtistFormPage Edit Save fatal crash (EF Core duplicate entity tracking)

**Severity:** Critical
**Feature:** Artists & Songs Catalog
**Registered:** 2026-06-27
**Source:** `BUG-017-artistscrud-emulator-debug-often-stops/artistis-crud-manual-tests-log.txt`

---

## Root Cause

Fatal crash stack trace (18:01:54 in the debug log):

```
FATAL EXCEPTION: main
System.InvalidOperationException: The instance of entity type 'Artist' cannot be tracked
because another instance with the same key value for {'Id'} is already being tracked.
    at ArtistRepository.UpdateAsync(ArtistRepository.cs:124)
    at ArtistService.UpdateArtistAsync(ArtistService.cs:90)
    at ArtistFormViewModel.SaveAsync(ArtistFormViewModel.cs:86)
```

### Why it happens

The `AppDbContext` is registered as a **Singleton** (long-lived, shared across all operations — part of the page-load-frozen workaround documented in `constraints-registry.md § EF Core / SQLite`). Because the context lives for the entire app session, entities tracked from earlier operations accumulate in its change tracker.

`ArtistRepository.GetPagedAsync` does **not** call `AsNoTracking()`:

```csharp
var q = _db.Artists.AsQueryable(); // tracks every returned Artist entity
```

When a list load materialises `Artist {Id=5}` as instance A, and a subsequent (concurrent `Task.Run`-offloaded) form load later materialises the same record as instance B, the change tracker holds both. Calling `_db.Artists.Update(instanceB)` while instance A is still in the tracker throws `InvalidOperationException` — EF Core's identity map enforces one tracked instance per key.

### Why `GetPagedAsync` is the root entry point

All list/search queries that return full `Artist` entities without `AsNoTracking()` contribute to tracker bloat. Affected methods in `ArtistRepository`:

- `GetPagedAsync` — main list, no `AsNoTracking()`
- `SearchByNameAsync` — search results, no `AsNoTracking()`
- `GetByExternalIdAsync` — lookup for import, no `AsNoTracking()`
- `GetByIdAsync` — single-entity form load (may stay tracked for edit; see Fix below)

---

## Architectural Fix (Helder's Direction)

**Do NOT use the ChangeTracker guard approach.** The correct fix is architectural:

### 1. Add `AsNoTracking()` + DTO projection to all list/search queries

List queries exist to populate read-only CRUD list rows. They should **never** track full entities.

```csharp
// GetPagedAsync — project to a lightweight DTO, not the full Artist entity
var q = _db.Artists
           .AsNoTracking()       // ← no tracking
           .AsQueryable();

// In the Select projection — emit only what the list row needs:
.Select(a => new ArtistListItemDto
{
    Id = a.Id,
    Name = a.Name,
    // ... only the fields actually displayed in the list
    CatalogCount = a.CatalogEntries.Count()
})
```

The same pattern applies to `SearchByNameAsync` and `GetByExternalIdAsync`.

### 2. `GetByIdAsync` for form load (Add / Edit)

For the **Edit** form, `GetByIdAsync` loads a single entity that the user then modifies. After `SaveChangesAsync()` completes, EF Core stops tracking the saved state. The next list reload will re-query fresh data via `AsNoTracking()`. This means the duplicate-tracking window is eliminated: the tracker only ever holds the single in-flight edit entity, not a stale copy from a prior list load.

Option A — keep `GetByIdAsync` tracked (simplest, correct after list queries are fixed):
```csharp
// Default EF Core tracking — safe once list queries no longer pollute the tracker
return await _db.Artists.FindAsync(new object[] { id }, ct);
```

Option B — use `AsNoTracking()` and re-attach for update:
```csharp
var entity = await _db.Artists.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
```
(Requires `UpdateAsync` to `_db.Entry(artist).State = EntityState.Modified` or `_db.Artists.Update(artist)` — works cleanly with no tracked duplicates.)

**Helder decides which option.** Option A is simpler. Option B is consistent (all queries AsNoTracking) but requires explicit attach logic.

### 3. Update `IArtistRepository` if list return type changes

If `GetPagedAsync` is changed to return `ArtistListItemDto` instead of `(Artist artist, int catalogCount)`, update the interface signature in `MyVocaList.Domain/Interfaces/IArtistRepository.cs` and the consumer in `ArtistService`.

---

## Implementation Steps

1. **Write failing regression test first (Critical — mandatory):**
   - Test in `ArtistRepositoryTests`: load a paged list (which normally tracks), then call `UpdateAsync` on the same entity — confirm no `InvalidOperationException` is thrown after fix.
   - Confirm test FAILS on the current code (Red).

2. **Add `AsNoTracking()` + DTO projection to `GetPagedAsync` and `SearchByNameAsync`:**
   - Update `IArtistRepository` return type if needed.
   - Update `ArtistService` consumer.

3. **Run regression test — confirm it passes (Green).**

4. **Build + all tests green.**

5. **Commit:** `fix: add AsNoTracking + DTO projection to list queries, eliminate EF tracker bloat [BUG-018]`

6. **Emulator smoke test:** Edit an artist and save — confirm no crash.

---

## Files to Change

| File | Change |
|------|--------|
| `Infra/Repository/ArtistRepository.cs` | Add `AsNoTracking()` + DTO projection to `GetPagedAsync`, `SearchByNameAsync`, `GetByExternalIdAsync` |
| `MyVocaList.Domain/Interfaces/IArtistRepository.cs` | Update `GetPagedAsync` return type if DTO projection changes the signature |
| `MyVocaList.Services/ArtistService.cs` | Adapt consumer if interface signature changes |
| `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs` | Add regression test |

---

## Regression Test (Critical — Mandatory)

Per `bug-tracking.md § Critical`: write the failing test first (Red), then fix (Green). No exceptions.

```csharp
[Fact]
// [AC] BUG-018: UpdateAsync must not throw when entity was previously loaded by a list query
public async Task UpdateAsync_EntityPreviouslyLoadedByListQuery_DoesNotThrow()
{
    // Arrange — seed an artist
    var artist = new Artist { Name = "Test Artist" };
    _db.Artists.Add(artist);
    await _db.SaveChangesAsync();

    // Act — simulate a list query that would previously track the entity
    await _repo.GetPagedAsync(1, 20, string.Empty);

    // Then update the same entity (this threw before the fix)
    artist.Name = "Updated Name";
    await _repo.UpdateAsync(artist, CancellationToken.None);
    await _db.SaveChangesAsync();

    // Assert — no exception, change persisted
    var saved = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == artist.Id);
    Assert.Equal("Updated Name", saved.Name);
}
```

---

## Verification

- Regression test: FAIL before fix, PASS after fix
- Build: 0 errors
- All tests: 0 failures
- Emulator: edit an artist → save → no crash, artist name updated in list

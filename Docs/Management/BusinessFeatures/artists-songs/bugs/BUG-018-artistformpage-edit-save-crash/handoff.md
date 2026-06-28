# BUG-018 Handoff — Session blocked on regression test failure

**Date:** 2026-06-27  
**Status:** 🔴 BLOCKED — Uncommitted changes; tests failing (1 fail, 359 pass)  
**Blocker:** `UpdateAsync_EntityAlreadyTracked_DoesNotThrow` test failing  

---

## What was attempted

Subagent `a0e750f301fe575e8` executed the BUG-018 implementation plan:

1. ✅ Created `ArtistListItem` record in Domain/ReadModels/
2. ✅ Updated IArtistRepository interfaces
3. ✅ Updated ArtistRepository implementation (with `.AsNoTracking()`)
4. ✅ Updated ArtistService
5. ✅ Updated ArtistsViewModel, ArtistPickerViewModel, ArtistFormViewModel
6. ✅ Deleted ArtistListItemDto
7. ✅ Added global `ChangeTracker.QueryTrackingBehavior = NoTracking` to AppDbContext
8. ✅ Added regression test

**Build result:** ✅ 0 errors  
**Test result:** ❌ 359 pass, **1 fails**: `UpdateAsync_EntityAlreadyTracked_DoesNotThrow`

---

## The failing test

```csharp
[Fact]
public async Task UpdateAsync_EntityAlreadyTracked_DoesNotThrow()
{
    // Override global NoTracking to TrackAll
    _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

    // Seed and clear
    var artist = new Artist { Name = "Test Artist" };
    _db.Artists.Add(artist);
    await _db.SaveChangesAsync();
    _db.ChangeTracker.Clear();

    // Act: GetPagedAsync should NOT track despite TrackAll setting
    await _repo.GetPagedAsync(1, 20, string.Empty);

    // Assert 1: ChangeTracker must be empty (explicit .AsNoTracking() overrides)
    Assert.Empty(_db.ChangeTracker.Entries<Artist>());

    // Assert 2: UpdateAsync should succeed with no tracking conflict
    artist.Name = "Updated Name";
    await _repo.UpdateAsync(artist, CancellationToken.None);
    await _db.SaveChangesAsync();

    var saved = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == artist.Id);
    Assert.Equal("Updated Name", saved.Name);
}
```

---

## What we know

- **359 tests pass** — most of the implementation is correct
- **The regression test is failing** — suggests the explicit `.AsNoTracking()` on GetPagedAsync may not be overriding the `TrackAll` context setting
- **Subagent could not self-fix** and did not respond to diagnostic requests
- **Changes are uncommitted** and blocking session-end

---

## Diagnosis needed

To fix this in the next session, investigate:

1. **Exact error message:** Run:
   ```bash
   dotnet test --filter "UpdateAsync_EntityAlreadyTracked_DoesNotThrow" --verbosity detailed
   ```
   Which assertion is failing?
   - Assert 1 (ChangeTracker not empty after GetPagedAsync)?
   - Assert 2 (UpdateAsync throws)?

2. **Verify GetPagedAsync implementation:**
   ```csharp
   var q = _db.Artists.AsNoTracking().AsQueryable();
   // ... rest of query
   ```
   Is the explicit `.AsNoTracking()` actually in the code?

3. **Verify AppDbContext constructor:**
   ```csharp
   ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
   ```
   Is this set globally?

4. **Verify UpdateAsync:**
   ```csharp
   public Task UpdateAsync(Artist artist, CancellationToken ct)
   {
       _db.Artists.Update(artist);
       return Task.CompletedTask;
   }
   ```
   Is the ChangeTracker guard removed?

---

## Files modified (uncommitted)

- `Infra/AppDbContext.cs`
- `Domain/ReadModels/ArtistListItem.cs` (new)
- `Domain/RepositoryInterface/IArtistRepository.cs`
- `Infra/Repository/ArtistRepository.cs`
- `Services/ArtistService.cs`
- `Domain/ServicesInterfaces/IArtistService.cs`
- `Contracts/DTOs/List/ArtistListItemDto.cs` (deleted)
- `ArtistsViewModel.cs`
- `ArtistPickerViewModel.cs`
- `ArtistFormViewModel.cs`
- `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs`

---

## Next steps

### Option 1: Manual debug in VS
1. Open the solution
2. Run the failing test in debug mode
3. Step through GetPagedAsync to see if entities are being tracked
4. Identify the exact issue and fix
5. Commit when tests pass

### Option 2: Fresh subagent with explicit diagnostic task
1. Dispatch a subagent with explicit instructions to:
   - Run the test with `--verbosity detailed`
   - Copy the exact error output
   - Inspect ArtistRepository.GetPagedAsync line-by-line
   - Report findings before attempting fixes

### Option 3: Kill the work and restart in worktree
1. `git checkout -- .` (discard all changes)
2. Create a new worktree for BUG-018
3. Start fresh with the same plan
4. This ensures isolation and clean state

---

## Recommendation

**Option 2** (fresh diagnostic subagent) is safest — it will give you the exact error, which will point to the fix immediately. The 359 passing tests suggest the architecture is sound; it's likely a small implementation detail (e.g., `.AsNoTracking()` in the wrong place, or missing from one code path).

**Go with Option 2 → fix → commit → resume next session.**

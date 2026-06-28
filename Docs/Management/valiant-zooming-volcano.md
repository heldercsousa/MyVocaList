# BUG-018 Architecture Review — Critical Analysis

> This is NOT an implementation plan. It is a critique of the existing plan and of the two
> architectural opinions raised before implementation begins.

---

## 1. Actual Bug Mechanism (full picture)

The Singleton `AppDbContext` + `Task.Run`-offloaded queries = concurrent, non-thread-safe
DbContext access. EF Core's ChangeTracker internal state becomes inconsistent under concurrent
reads. Symptom: the existing ChangeTracker guard in `UpdateAsync` (lines 122–130 of
`ArtistRepository.cs`) runs `ChangeTracker.Entries<Artist>().FirstOrDefault(...)` on a
corrupted collection → finds nothing → falls through to `_db.Artists.Update(artist)` → EF
throws because something IS tracked but the guard missed it.

Root fix: list queries must never add to the ChangeTracker. `AsNoTracking()` on `GetPagedAsync`
and `SearchByNameAsync` eliminates the window where a second tracked instance can exist
alongside the edit-path instance loaded by `GetByIdAsync`.

---

## 2. Failures in the Existing Plan

### 2.1 — Regression test will PASS on current code (Red phase never happens)

`TestDbContextFactory.Create()` (from `testing.md § Infrastructure`) sets:
```csharp
.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
```
With global NoTracking, `GetPagedAsync` in the test context tracks nothing even without
`AsNoTracking()`. The test as written in plan.md will be Green immediately on the current
code. That is **not a regression test** — it is a vacuous test.

**Fix:** The regression test must explicitly create a tracking DbContext (no
`UseQueryTrackingBehavior` override, so EF Core defaults to `TrackAll`). That reproduces
the production context and ensures the test is genuinely Red before the fix.

### 2.2 — DTO projection in the repository violates the dependency graph

The plan proposes returning `ArtistListItemDto` from `GetPagedAsync` inside `ArtistRepository`
(Infra layer). `ArtistListItemDto` lives in `MyVocaList.Contracts`. Currently Infra only
references `MyVocaList.Domain`. The plan introduces a new **Infra → Contracts** dependency
with no architectural justification.

Worse: **the service already does this projection** (ArtistService.cs lines 130–135):
```csharp
var dtos = items.Select(x => new ArtistListItemDto(
    x.artist.Id, x.artist.Name, x.artist.ExternalProvider,
    x.artist.HasManualEdits, x.catalogCount));
```
If the repo were to also project to `ArtistListItemDto`, the result is a DTO→DTO pass-through
in the service: dead code at best, double-mapping at worst. The plan's DTO projection is
gold-plating and architecturally inconsistent with what already exists.

### 2.3 — The ChangeTracker guard is unaddressed

The plan says "Do NOT use the ChangeTracker guard approach." But `UpdateAsync` **already has
one**. The plan never says to remove it. After the `AsNoTracking()` fix, the guard becomes
dead weight — the crash path it tried to handle can no longer occur, and the guard has
already proven unreliable under concurrent access. It should be removed and replaced with
the direct `_db.Artists.Update(artist)` call, which is safe once only one instance per key
can ever be tracked.

---

## 3. Critique of User Opinion 1 — "Repos should return IQueryable"

### Claim
Repositories deliver lazy `IQueryable<Artist>`. The Service layer builds projections and
executes them. When a second Infra layer (e.g., MSSQL) is added, the repo is swapped without
touching the Service.

### Why this fails in this codebase

**IQueryable leaks the query provider into Services.**

`GetPagedAsync` computes `a.CatalogEntries.Count()` inside a LINQ `Select`. This is
translated to a SQL COUNT subquery by EF Core's query provider. If the Service layer
builds this expression on an `IQueryable<Artist>`, the Services project must know what
`a.CatalogEntries` is in SQL terms — which means it depends on EF Core.

Similarly, `EF.Functions.Like` and `EF.Functions.Collate` (used in name search) are
`Microsoft.EntityFrameworkCore`-namespaced. The Services project has no current reference to
EF Core and must not acquire one. Calling `ToListAsync(ct)` on an `IQueryable` is also an
EF Core extension method. Without it, the only materializtion option is `.ToList()` (sync),
which blocks the thread.

**The "multiple Infra" argument is self-defeating.**

`constraints-registry.md` confirms that a MSSQL Infra layer is planned. The SQLite repo
uses `EF.Functions.Collate(a.Name, CollationConstants.Default)` — a SQLite-specific
collation. If the Service holds these expressions on an `IQueryable`, the MSSQL Infra's
query provider will either fail to translate them or produce wrong SQL. The incompatibility
moves from Infra (isolated, containable) to Services (cross-cutting, hard to fix).

The correct abstraction for cross-provider query composability is the **Specification
Pattern** (`ISpecification<T>`), not raw `IQueryable`. But that is a significant architectural
change unrelated to this bug fix.

**Verdict:** The IQueryable approach does not achieve isolation from the provider; it moves
provider coupling to a layer that should not have it.

---

## 4. Critique of User Opinion 2 — "GetPagedAsync belongs in the Service"

### Claim
Pagination is business logic. Repositories should expose plain entities. `GetPagedAsync` is
a service-layer concern.

### Why this fails

`GetPagedAsync` performs: `WHERE` filter, role filter (`WHERE EXISTS` subquery), `COUNT(*)`,
`ORDER BY Name`, `SKIP/TAKE`, and `SELECT a, COUNT(CatalogEntries)`. Every one of these is
a SQL operation. Moving them to the Service layer requires one of:

- **IQueryable from repo** → provider leak to Services (see §3 above).
- **Full entity set from repo** (`IEnumerable<Artist>`) → full table scan every page load.
  O(N) memory, O(N) compute. Catastrophic at scale.

**The current split is already correct.** The service already has its pagination concern in
`GetPagedArtistsForListAsync` (ArtistService.cs line 121): it validates `pageNumber` and
`pageSize`, calls the repo, and maps to `ArtistListItemDto`. That IS the service-layer
pagination responsibility. The repo's `GetPagedAsync` is pure SQL mechanics — precisely
what the repository pattern is for.

Note also that `ArtistRoleFilter` is defined in `IArtistRepository.cs` (Domain layer), not
in the Infra project. It is already a domain concept, not an infra leak.

**Verdict:** `GetPagedAsync` belongs in the repository. The service wrapper already exists
and already provides the business-logic boundary.

---

## 5. The Correct Minimal Fix for BUG-018

### What to change (3 files, 6 lines)

**`Infra/Repository/ArtistRepository.cs`**

1. `GetPagedAsync`: change `_db.Artists.AsQueryable()` →
   `_db.Artists.AsNoTracking().AsQueryable()`
2. `SearchByNameAsync`: change `_db.Artists.AsQueryable()` →
   `_db.Artists.AsNoTracking().AsQueryable()`
3. `GetByExternalIdAsync`: verify callers — used in import flow for existence check.
   If no caller mutates the returned entity through the same context (import creates via
   `AddAsync`, not `UpdateAsync` on the returned instance), add `AsNoTracking()` here too.
   If callers do mutate, leave tracked and document in code.
4. `UpdateAsync`: **remove the ChangeTracker guard**. Replace the entire method body with:
   ```csharp
   public Task UpdateAsync(Artist artist, CancellationToken ct)
   {
       _db.Artists.Update(artist);
       return Task.CompletedTask;
   }
   ```
   After the `AsNoTracking()` fix, the only tracked Artist for any given Id is the one
   `GetByIdAsync` returned. `ArtistService.UpdateArtistAsync` mutates that same reference
   in place and passes it back. Calling `Update()` on an already-tracked entity with state
   `Unchanged` or `Modified` does not throw — EF Core sets it to `Modified`.

**No interface change. No DTO projection in repo. No new Contracts dependency.**

### Regression test fix

The test must use a `TrackAll` DbContext. Add a second factory method to
`TestDbContextFactory`:

```csharp
public static AppDbContext CreateTracking()
{
    var dbPath = Path.Combine(Path.GetTempPath(), $"myvocalist_test_{Guid.NewGuid():N}.db");
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        // No UseQueryTrackingBehavior — defaults to TrackAll
        .Options;
    return new AppDbContext(options);
}
```

Use `CreateTracking()` in `ArtistRepositoryTests.InitializeAsync()` for this specific test.
Confirm: on current code the test throws `InvalidOperationException` (Red). After adding
`AsNoTracking()`, the test passes (Green).

---

## 6. Decision Requested Before Implementation

| Question | Options |
|----------|---------|
| `GetByExternalIdAsync` — track or not? | A) Add AsNoTracking (import callers never update via returned ref) · B) Leave tracked (import may update the returned ref) |
| Remove ChangeTracker guard from `UpdateAsync`? | Yes (safe after the list-query fix) · No (keep as defence-in-depth, document that it's unreachable) |

Recommendation: Option A for `GetByExternalIdAsync` (import resolution creates new entities
via AddAsync, not UpdateAsync on a loaded ref). Remove the guard — keeping dead code that
was demonstrably unreliable under the real failure mode adds confusion, not safety.

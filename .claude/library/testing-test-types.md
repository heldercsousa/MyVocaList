# Testing — Reference — Test types — Service, ViewModel, Repository + TestDbContextFactory

> Section file split from `testing-reference.md` on 2026-07-14 (token-scoped reads). Index + provenance: `testing-reference.md`. Never-miss rules: `.claude/rules/testing.md`.

## Test Types

### 1. Unit Tests — Services

**What:** Business logic in `MyVocaList.Services`. Mock all repository interfaces.
**Scope:** Validation, tuple return values (success/message), entity mapping.
**Do NOT:** Spin up EF Core, connect to SQLite, or test the repository implementation here.

```csharp
public class VenueServiceTests
{
    private readonly Mock<IVenueRepository> _repoMock = new();
    private readonly Mock<ILogger<VenueService>> _loggerMock = new();

    private VenueService CreateSut() => new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task CreateVenueAsync_NameTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var name = new string('x', 31); // exceeds 30-char limit

        var (success, message, venue) = await sut.CreateVenueAsync(name);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(venue);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateVenueAsync_DuplicateName_ReturnsFalse()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, _) = await sut.CreateVenueAsync("Existing Venue");

        Assert.False(success);
        Assert.Contains("already", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateVenueAsync_ValidName_ReturnsSuccessAndEntity()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Venue>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message, venue) = await sut.CreateVenueAsync("New Venue");

        Assert.True(success);
        Assert.NotNull(venue);
        Assert.Equal("New Venue", venue.Name);
    }
}
```

### 2. Unit Tests — ViewModels

**What:** ViewModel commands, state transitions, notification of derived properties.
**Scope:** Commands (`ExecuteAsync`, `CanExecute`), ObservableProperty chains, empty-state flags.
**Do NOT:** Test XAML bindings, Shell navigation directly (mock via interface), or DX control behavior.

Generic scaffolding (mock fields + `CreateSut()` factory, `[Fact]`/`[Theory]`, command-execution pattern, mocking MAUI services table) → **`maui-unit-testing` skill § ViewModel Testing Pattern**. Project-specific test targets for MyVocaList list ViewModels:

- `InitializeAsync` with an empty service result must set `IsEmptyNoVenues` true, `IsEmptyNoResults` false, `IsInitialLoading` false (empty-state flag family — every CRUD list VM has the equivalent trio).
- `OnSelectionChanged(n)` drives the derived-state family: `CanEditSelected`/`CanDeleteSelected` gates and `AppBarTitle` ("1 selected" at n=1; the page title, e.g. "Venues", at n=0).
- Service mocks return the project's paged-tuple shape, e.g. `.ReturnsAsync((Enumerable.Empty<VenueListItemDto>(), 0))`.

**ViewModel test constraints:**
- `Shell.Current` must NOT be called from tested methods. Commands that navigate must be excluded from unit tests unless `Shell.Current` is mocked (impractical). Test state transitions only; navigation commands are integration-tested via emulator.
- `Task.Yield()` inside `InitializeAsync` is fine — `await task` in tests resolves it correctly.

### 3. Integration Tests — Repositories

**What:** Real EF Core + SQLite temp DB. Verifies queries, indexes, collation, and entity configurations.
**Scope:** CRUD, search queries, pagination, unique constraint violations, cascade behavior.
**Do NOT:** Mock the DbContext or repository. The whole point is to test the real implementation.

```csharp
public class VenueRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private VenueRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new VenueRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ValidVenue_PersistedAndReturnedById()
    {
        var venue = new Venue { Name = "Jazz Club" };
        await _repo.AddAsync(venue, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(venue.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Jazz Club", found.Name);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_FindsMatch()
    {
        _db.Venues.AddRange(
            new Venue { Name = "Rock Arena" },
            new Venue { Name = "Jazz Club" });
        await _db.SaveChangesAsync();

        var (results, count) = await _repo.GetPagedAsync(1, 20, "rock");

        Assert.Equal(1, count);
        Assert.Equal("Rock Arena", results.First().Name);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsDbUpdateException()
    {
        _db.Venues.Add(new Venue { Name = "Unique" });
        await _db.SaveChangesAsync();

        _db.Venues.Add(new Venue { Name = "unique" }); // case variant

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _db.SaveChangesAsync());
    }
}
```

### Infrastructure: `TestDbContextFactory`

```csharp
public static class TestDbContextFactory
{
    /// <summary>Creates a fresh in-memory SQLite AppDbContext for each test.</summary>
    public static AppDbContext Create()
    {
        // Use a unique file name per test run to ensure isolation
        var dbPath = Path.Combine(Path.GetTempPath(), $"myvocalist_test_{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new AppDbContext(options);
    }
}
```

> **Why real SQLite and not in-memory provider?** SQLite has LIKE collation quirks (`EF.Functions.Collate`) that in-memory EF doesn't replicate. Repository tests must use real SQLite to catch collation bugs.

---

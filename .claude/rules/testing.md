# Testing Rules — MyVocaList

> **Status:** Active from Step 3 (Venue CRUD Tests) onward.
> TDD applies to all new Services, ViewModels, and Repositories from AutocompleteField + Person CRUD forward.
> Venue CRUD tests (Step 3) establish the test infrastructure baseline; TDD workflow kicks in from Step 4+.
## TDD within SDD

TDD and SDD are complementary, not competing disciplines. Each operates at a different level:

| Level | Discipline | Output |
|-------|-----------|--------|
| Feature intent | SDD (Spec-Driven Development) | `requirements.md`, `design.md`, `tasks.md` |
| Behavior contract | TDD (Test-Driven Development) | Failing test that encodes a single acceptance criterion |
| Implementation | Code | Minimal code to make the test pass |

**How they connect:**
- Every acceptance criterion in `requirements.md` must map to at least one test (see Acceptance Criteria Traceability below).
- The spec defines *what* must be true; the failing test is the machine-checkable encoding of that truth.
- Do not write a test that has no corresponding acceptance criterion — if the behavior matters, add the AC to the spec first.
- Do not write implementation code that has no corresponding test — if the code matters, write the test first.

> Skipping either discipline degrades both. SDD without TDD produces specs that drift from the implementation. TDD without SDD produces tests that encode assumptions never reviewed by the architect.

---

## Acceptance Criteria Traceability

Every test that covers a user-facing behavior must be traceable to an acceptance criterion (AC) in `requirements.md`.

### Format

Add an `[AC]` tag as the first line of each test method's doc comment (or inline comment) citing the AC ID:

```csharp
[Fact]
// [AC] REQ-VENUE-03: Name must be unique across all venues (case-insensitive)
public async Task CreateVenueAsync_DuplicateName_ReturnsFalse()
{
    ...
}
`````r

### Rules

1. **Every AC → at least one test.** If an AC has no corresponding test, it is unverified. Treat missing coverage as a spec gap.
2. **One test → one AC.** A test that covers multiple ACs is testing too much. Split it.
3. **AC IDs come from the spec.** Do not invent IDs. If the spec has no ID scheme, add one before writing tests.
4. **Infrastructure tests are exempt.** Tests for `TestDbContextFactory`, builder helpers, or test-only utilities do not require an AC tag.

### Traceability matrix (per feature)

When a feature reaches code review, produce a traceability table in the task-log:

```r
| AC ID | Description | Test method |
|-------|-------------|-------------|
| REQ-VENUE-01 | Venue name ≤ 30 chars | CreateVenueAsync_NameTooLong_ReturnsFalse |
| REQ-VENUE-02 | Name required | CreateVenueAsync_EmptyName_ReturnsFalse |
| REQ-VENUE-03 | Name unique (case-insensitive) | CreateVenueAsync_DuplicateName_ReturnsFalse |
`````r

Missing rows = missing tests = incomplete feature.

---

## Test Project Structure

### Project: `MyVocaList.Tests`

```
MyVocaList.Tests/
├── MyVocaList.Tests.csproj
├── GlobalUsings.cs
├── Unit/
│   ├── Services/              ← pure business logic, Moq dependencies
│   │   └── VenueServiceTests.cs
│   └── ViewModels/            ← ViewModel commands + state, Moq services
│       └── VenuesViewModelTests.cs
├── Integration/
│   └── Repositories/          ← real SQLite temp DB, no containers
│       └── VenueRepositoryTests.cs
└── Infrastructure/
    └── TestDbContextFactory.cs ← shared DB setup/teardown helper
```

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>   <!-- NOT net10.0-android -->
    <IsPackable>false</IsPackable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="coverlet.collector" Version="6.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <!-- Domain, Contracts, Infra, Services — the non-MAUI projects -->
    <ProjectReference Include="..\MyVocaList.Domain\MyVocaList.Domain.csproj" />
    <ProjectReference Include="..\MyVocaList.Contracts\MyVocaList.Contracts.csproj" />
    <ProjectReference Include="..\MyVocaList.Infra\MyVocaList.Infra.csproj" />
    <ProjectReference Include="..\MyVocaList.Services\MyVocaList.Services.csproj" />
    <!-- MAUI head project: only if ViewModel tests are needed -->
    <!-- <ProjectReference Include="..\MyVocaList\MyVocaList.csproj" /> -->
  </ItemGroup>
</Project>
```

> **Important:** Do NOT reference `MyVocaList.csproj` (MAUI head) unless ViewModel tests are needed.
> ViewModels use `CommunityToolkit.Mvvm` which is a plain .NET library — it can be referenced through the MAUI project.
> If the MAUI project is referenced, add `<OutputType>Library</OutputType>` to its `net10.0` TFM:
>
> ```xml
> <!-- In MyVocaList/MyVocaList.csproj -->
> <PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
>   <OutputType>Library</OutputType>
> </PropertyGroup>
> ```

### GlobalUsings.cs

```csharp
global using Xunit;
global using Moq;
global using MyVocaList.Domain.Entities;
global using MyVocaList.Domain.Interfaces;
global using MyVocaList.Contracts.DTOs.List;
global using MyVocaList.Services;
```

---

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

```csharp
public class VenuesViewModelTests
{
    private readonly Mock<IVenueService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<VenuesViewModel>> _loggerMock = new();

    private VenuesViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object);

    [Fact]
    public async Task InitializeAsync_EmptyDb_SetsIsEmptyNoVenues()
    {
        _serviceMock.Setup(s => s.GetPagedVenuesForListAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((Enumerable.Empty<VenueListItemDto>(), 0));
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.True(sut.IsEmptyNoVenues);
        Assert.False(sut.IsEmptyNoResults);
        Assert.False(sut.IsInitialLoading);
    }

    [Fact]
    public void OnSelectionChanged_OneSelected_CanEditSelected()
    {
        var sut = CreateSut();

        sut.OnSelectionChanged(1);

        Assert.True(sut.CanEditSelected);
        Assert.True(sut.CanDeleteSelected);
        Assert.Equal("1 selected", sut.AppBarTitle);
    }

    [Fact]
    public void OnSelectionChanged_Zero_AppBarShowsTitle()
    {
        var sut = CreateSut();

        sut.OnSelectionChanged(0);

        Assert.Equal("Venues", sut.AppBarTitle);
        Assert.False(sut.CanEditSelected);
        Assert.False(sut.CanDeleteSelected);
    }
}
```

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

## Naming Conventions

### Test Class
`{Subject}Tests` where subject is the class under test:
- `VenueServiceTests`
- `VenuesViewModelTests`
- `VenueRepositoryTests`

### Test Method
`{Method}_{Context}_{Expected}`:
- `CreateVenueAsync_NameTooLong_ReturnsFalse`
- `InitializeAsync_EmptyDb_SetsIsEmptyNoVenues`
- `SearchAsync_CaseInsensitive_FindsMatch`

All three parts are required. No generic names like `Test1`, `CreateVenue_Works`, or `ItWorks`.

---

## What to Test

| Layer | Test | Skip |
|---|---|---|
| **Service** | Validation logic, business rules, tuple return values, error messages | Framework plumbing, DI wiring |
| **ViewModel** | Command execution, state after commands, derived properties (AppBarTitle, IsEmpty*), CanExecute gates | XAML bindings, Shell.Current calls, DX control state |
| **Repository** | CRUD operations, search/filter queries, unique constraint enforcement, EF configuration | EF migration internals, DTO mapping done by services |

### Service — what defines a test boundary
Every `if` branch in a service method that changes the return value is a test case:
```csharp
// This generates 3 test cases:
if (name.Length > MaxLength) return (false, "Name too long", null);       // test 1
if (await _repo.ExistsByNameAsync(name)) return (false, "Duplicate", null); // test 2
// ... success path                                                          // test 3
```

### ViewModel — focus on observable state
Test what the ViewModel exposes to the view, not how it calls services internally:
- `[ObservableProperty]` values after commands run
- Derived `bool` properties (`CanEditSelected`, `IsEmptyNoVenues`, `IsAllSelected`)
- `AppBarTitle` string derived from `SelectedCount`

### Repository — always test the query, not just CRUD
For every repository method that takes a filter/sort/page parameter, write one test that verifies the filtering is actually applied:
```csharp
// Not enough:
[Fact] async Task AddAsync_PersistsVenue() { ... }

// Required:
[Fact] async Task GetPagedAsync_SearchQuery_ReturnsOnlyMatching() { ... }
[Fact] async Task GetPagedAsync_Page2_SkipsFirstPage() { ... }
```

---

## TDD Workflow (Red → Green → Refactor)

Starting from AutocompleteField + Person CRUD (Step 4+):

1. **Write the test first** — it fails (Red).
2. **Run `dotnet test`** — confirm failure message matches expected behavior.
3. **Write only enough implementation to make it pass** (Green).
4. **Run `dotnet test`** — confirm all pass.
5. **Refactor if needed** — no new tests fail.

**Never write implementation before the test.** If you write implementation first, you are not doing TDD.

### Regression tests
When fixing a bug, write the failing test FIRST, confirm it fails, then fix, then confirm it passes. The regression test proves the bug existed and the fix works.

---

## Running Tests

```bash
# Run all tests
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj

# Verbose output (shows test names)
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal

# Filter to a specific class
dotnet test --filter "FullyQualifiedName~VenueServiceTests"

# Filter to a specific method
dotnet test --filter "FullyQualifiedName~CreateVenueAsync_NameTooLong"

# Coverage (outputs cobertura XML to TestResults/)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Prerequisites Before Step 3 (Test Project Setup)

These must be fixed when the test project is created. Do NOT create tests until they are resolved:

1. **`AppDbContext` missing `QueryTrackingBehavior.NoTracking`** — add to the `OnConfiguring` or options builder. Without it, repository integration tests may have unexpected tracking side effects.

2. **`AppDbContext` has `Console.WriteLine` calls** — remove. Violates code-principles.md (no debug output in production code). Test runs will pollute output.

3. **Serilog version drift** — `MyVocaList.Services` uses Serilog 4.2.0, `MyVocaList.Infra` uses 4.3.1. Normalize via `Directory.Packages.props` (CPM) when setting up the test project. CPM is a prerequisite to adding a 5th project cleanly.

---

## Anti-Patterns — Never Do These

| Anti-pattern | Why |
|---|---|
| Mock the DbContext in repository tests | Defeats the purpose — EF query translation only runs against a real provider |
| Assert on private state (`_field`) | Test the public interface only |
| Test XAML binding correctness | That's the MAUI runtime's job |
| Call `Shell.Current` in ViewModel tests | `Shell.Current` is null in test context — wrap navigation behind a service interface |
| Write multiple `Assert.*` for unrelated behaviors | One test, one behavioral assertion (related asserts for a single behavior are fine) |
| Use `Thread.Sleep` for async timing | Use `await Task.Delay` or `TaskCompletionSource` |
| Skip writing the failing test first (Step 4+) | This is TDD — the failing test is not optional |

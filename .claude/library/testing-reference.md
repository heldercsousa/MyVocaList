# Testing — Reference

> Extracted from `.claude/rules/testing.md` (2026-07-05, rules-file-refactoring Task 09–10). The rule file is now a routing table; this file holds the full detail (project structure, test-type patterns with code, naming, what-to-test, Tester/Builder split, TDD workflow, running tests, quality audit, anti-patterns). Mutation testing (Stryker) and property-based testing (FsCheck) live in their own on-demand files. Discovered via the `myvocalist-coding` skill map or the rule's routing table.
> Content moved verbatim; only corrupted markdown code-fences (`` `ash ``, `` `json ``, ```` ```r ````) were normalized to proper fences — no wording changed.
> **Trimmed 2026-07-07 (Task 18, audit F9/R8):** generic xUnit/Moq scaffolding (csproj skeleton, OutputType trick, generic ViewModel test pattern, generic run commands) now lives in the enabled `maui-unit-testing` skill — this file keeps only what is project-specific and points there for the rest.

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

### .csproj — project-specific deltas only

Generic test-project setup (csproj skeleton, xUnit/Moq/coverlet packages, conditional `OutputType` trick for referencing the app head) → **`maui-unit-testing` skill § Test Project Setup**. MyVocaList-specific facts:

- **TFM is `net10.0` only** (NOT `net10.0-android`) so tests run on the desktop host.
- Add `Microsoft.EntityFrameworkCore.Sqlite` `10.*` (repository integration tests use real SQLite).
- Reference the four non-MAUI projects: `MyVocaList.Domain`, `MyVocaList.Contracts`, `MyVocaList.Infra`, `MyVocaList.Services`.
- Do NOT reference `MyVocaList.csproj` (MAUI head) unless ViewModel tests are needed; if referenced, apply the skill's conditional `<OutputType>Library</OutputType>` on the `net10.0` TFM.

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

## Tester/Builder Role Separation

In a TDD cycle, the agent that writes tests (Tester) and the agent that writes implementation (Builder) must be kept conceptually separate. In practice with subagents, enforce this by dispatching test-writing and implementation-writing as distinct tasks.

### Why it matters
When a single agent writes both tests and implementation simultaneously, it naturally writes tests that match the implementation rather than tests that verify the spec. The result is tests that pass but prove nothing.

### Rules

1. **Tester writes tests first, then stops.** The Tester subagent writes all tests for a task, confirms they compile and fail (Red), commits, and exits. It does NOT write any implementation. (Note: in a single-agent session, apply one-at-a-time discipline per "One test at a time — Exception.")
2. **Builder receives failing tests, makes them pass.** The Builder subagent reads the committed failing tests, writes only enough implementation to make them pass (Green), and exits. It does NOT modify tests.
3. **Refactor is a third, optional pass.** After Green, a separate refactor pass may clean up implementation without changing test or behavior.
4. **In a single-agent session:** apply the same discipline mentally — write all tests, run them to confirm failure, then switch to implementation mode.

### Dispatch pattern (from workflow.md)

```
Wave A: Tester subagent
  Input: spec (requirements.md, design.md), task description
  Output: committed failing tests, task-log status = "Red — tests written"

Wave B: Builder subagent
  Input: failing tests from Wave A, spec files
  Output: committed passing implementation, task-log status = "To Review"
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

### One test at a time

Write and run **one test** before proceeding to the next. Do not write all tests for a service method in one batch, then run them together.

**Rationale:** Batching test writes delays the Red confirmation. A test that was never seen failing may have been written incorrectly (wrong assertion, wrong setup). Each test must be seen to fail before the implementation that makes it pass is written.

**Incremental TDD cycle per test:**
1. Write one test → run → confirm Red
2. Write minimal implementation → run → confirm Green
3. Write next test → run → confirm Red (existing tests still Green)
4. Extend implementation → run → confirm all Green
5. Repeat

**Exception:** When the Tester/Builder split is used (separate subagents), the Tester writes all tests for a task together and confirms all fail, because the Builder has not yet run. The one-at-a-time discipline applies within a single-agent session.

---

## Running Tests

Test project path: `MyVocaList.Tests/MyVocaList.Tests.csproj` — e.g. `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`. Filter/verbosity/coverage command variants → **`maui-unit-testing` skill § Running Tests**.

---

## Test Quality Audit Checklist

Run this checklist during code review for any test file. A test that fails one or more items must be fixed before the feature is marked `To Review`.

### For each test method

- [ ] **Name follows convention** — `{Method}_{Context}_{Expected}` with all three parts present
- [ ] **Has a Red phase** — the test was seen failing before the implementation that makes it pass was written (or Tester/Builder split was used)
- [ ] **Single behavioral assertion** — the test asserts one outcome; related asserts for the same outcome are permitted, but unrelated behaviors must be in separate tests
- [ ] **AC tag present** — user-facing behavior tests carry an `// [AC] REQ-XXX-YY` comment
- [ ] **AC exists in spec** — the referenced AC ID is present in `requirements.md`
- [ ] **No `Thread.Sleep`** — async timing uses `await Task.Delay` or `TaskCompletionSource`
- [ ] **No private-state assertions** — only public interface is tested
- [ ] **Arrange/Act/Assert** structure is visible — blank lines separate the three phases

### For each test class

- [ ] **No shared mutable state** between tests — each `[Fact]` is independent
- [ ] **Repository tests use real SQLite** — no in-memory EF provider
- [ ] **Service tests use Moq** — no real repositories, no real DB
- [ ] **Traceability matrix exists** in task-log for user-facing feature tests

### Audit frequency

- Before setting a task to `To Review` in the task-log
- During `/sln-review` (run after every task)

---

## Builder Must Not Modify Tests

During the Green phase, the Builder's only permitted action is writing or modifying **production code** in `MyVocaList.Domain`, `MyVocaList.Services`, `MyVocaList.Infra`, or `MyVocaList` (MAUI).

**The Builder must never:**
- Edit a test file to make a test pass
- Comment out an assertion
- Change a test's setup to avoid triggering a failure
- Delete a test that cannot be made to pass

**If a test appears wrong:**
The Builder must stop, document the suspected spec gap in the task-log (`blocked: spec gap`), and wait for the architect (Helder) to resolve it. The Builder does not unilaterally decide a test is wrong.

**Rationale:** A test represents an encoded acceptance criterion. Changing the test without changing the spec is silent spec deletion — the behavior remains unverified but appears tested.

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
| Modify a test to make it pass during Green phase | Tests define the contract. Changing a test to pass is not Green — it is spec deletion. If a test is wrong, escalate to the architect; do not silently fix it. See "Builder Must Not Modify Tests" for full escalation protocol. |
| Delete a failing test instead of implementing the behavior | Same as above — spec deletion. Failing tests are blockers, not noise. |

---

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).

# Testing Rules — MyVocaList

> TDD applies to all new and modified Services, ViewModels, and Repositories.
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
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| REQ-VENUE-01 | Name ≤ 30 chars | VenueService.ValidateNameInput | CreateVenueAsync_NameTooLong_ReturnsFalse |
| REQ-VENUE-02 | Name required | VenueService.ValidateNameInput | CreateVenueAsync_EmptyName_ReturnsFalse |
| REQ-VENUE-03 | Name unique (case-insensitive) | VenueService.CreateVenueAsync | CreateVenueAsync_DuplicateName_ReturnsFalse |
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
## TDD Level Guidance by Risk

Not all code warrants the same test investment. Use risk classification to calibrate test coverage without over-testing low-risk code.

### Risk levels

| Level | Label | Definition | Test requirement |
|-------|-------|-----------|-----------------|
| A | **High risk** | Business logic with validation, state mutation, or user-facing failure modes | Full TDD: Red → Green → Refactor. Unit + property-based tests. All branches covered. |
| B | **Medium risk** | Query logic, mapping, pagination, EF configuration | Example-based tests for happy path + key edge cases. Integration tests for query behavior. |
| C | **Low risk** | Pure plumbing, DI registration, DTO mapping with no logic, trivial getters | No mandatory test. Optional smoke test if needed for confidence. |

### Classification guide

| Code | Risk level |
|------|-----------|
| Service validation methods (`ValidateNameInput`, `CreateVenueAsync` guards) | A |
| Service methods that mutate state (create, update, delete) | A |
| ViewModel command state transitions, `CanExecute` logic | A |
| Repository query methods (search, filter, sort, paginate) | B |
| EF entity configurations (index, collation, cascade) | B |
| DTO mapping in services | B |
| Repository CRUD without custom logic (`AddAsync`, `GetByIdAsync`) | B |
| DI registration in `MauiProgram.cs` | C |
| DTO record definitions | C |
| `ObservableProperty` with no derived logic | C |

### Applying the classification

When the Tester subagent receives a task, it must classify each method:
- Level A → write all tests before Builder starts
- Level B → write tests for non-trivial paths; mark trivial CRUD as C
- Level C → document as "no test required" in the task-log; do not write empty test stubs. If a Level C task has ACs, document the no-test decision in the task-log — it will be scrutinized at review.

### Escalation

If a method is initially classified C but a bug is found in production, reclassify to A or B and add regression tests before fixing.

---

## Mutation Testing with Stryker.NET

Use mutation testing to detect tests that pass even when the production code is subtly wrong. Stryker.NET introduces small code mutations and verifies that at least one test fails per mutation.

### When to run

- After completing a Level A feature (see TDD Level Guidance above)
- When a bug is found in production in an area believed to be well-tested
- As part of a quality audit requested by Helder

> Do NOT run Stryker on every commit — it is slow (minutes to hours). Run it as a periodic quality gate, not a CI gate.

### Setup (one-time, global .NET tool)

`ash
dotnet tool install -g dotnet-stryker
`

### Running

`ash
# From solution root — targets Services project, reports to TestResults/
dotnet stryker --project MyVocaList.Services/MyVocaList.Services.csproj \
               --test-project MyVocaList.Tests/MyVocaList.Tests.csproj \
               --reporter html \
               --output TestResults/Stryker
`

Open TestResults/Stryker/reports/mutation-report.html to review surviving mutants.

### Interpreting results

| Outcome | Meaning | Action |
|---------|---------|--------|
| **Killed** | A test caught this mutation | Good |
| **Survived** | No test failed for this mutation | Test gap — write a test that kills it |
| **No coverage** | No test exercises this code at all | Test gap or Level C code — classify and decide |
| **Timeout** | Mutation caused an infinite loop | May indicate a logic bug in the code |

### Target mutation score

| Layer | Minimum score |
|-------|--------------|
| Services (Level A methods) | 80% |
| Repositories (Level B methods) | 60% |
| ViewModels (Level A state transitions) | 70% |

> These are minimums, not targets. Aim higher when the effort is justified by risk.

### Surviving mutant triage

For each surviving mutant, decide:
1. **Write a killing test** — the mutant exposes a real gap; write a test that fails for this mutation
2. **Exclude the mutant** — the mutation is semantically equivalent (e.g., `i++` vs `i += 1`); add to .stryker-config.json excludes with a comment explaining why
3. **Reclassify code as Level C** — if the surviving mutant is in trivial plumbing, record the decision in the task-log

### Configuration file

Create .stryker-config.json at solution root when exclusions are needed:

`json
{
  "stryker-config": {
    "mutate": [
      "MyVocaList.Services/**/*.cs",
      "!MyVocaList.Services/GlobalUsings.cs"
    ],
    "excluded-mutations": ["StringLiteral"]
  }
}
`

---
## Property-Based Testing with FsCheck

Use property-based testing (PBT) for service methods whose correctness must hold across a wide range of inputs — not just the specific examples in example-based tests.

### When to use PBT

| Use PBT | Use example-based tests |
|---------|------------------------|
| Validation rules (length, format, range) | Specific error messages |
| Round-trip invariants (create → read → same) | Exact entity mapping |
| Pagination arithmetic (skip/take consistency) | Integration flows |
| Commutative or associative operations | Error path specifics |

### Setup

Add to MyVocaList.Tests.csproj:

`xml
<PackageReference Include="FsCheck.Xunit" Version="2.*" />
`

Add to GlobalUsings.cs:

`csharp
global using FsCheck;
global using FsCheck.Xunit;
`

### Usage pattern

`csharp
[Property]
public Property CreateVenueAsync_NameWithinLimit_AlwaysSucceeds(string name)
{
    // Generate names that are 1–30 chars (within valid range)
    return Prop.ForAll(
        Arb.Default.NonEmptyString().Filter(s => s.Value.Length <= 30),
        async name =>
        {
            _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
            var sut = CreateSut();
            var (success, _, _) = await sut.CreateVenueAsync(name.Value);
            return success;
        });
}

[Property]
public Property ValidateNameInput_NameExceedsLimit_AlwaysInvalid()
{
    return Prop.ForAll(
        Arb.Default.NonEmptyString().Filter(s => s.Value.Length > 30),
        name =>
        {
            var sut = CreateSut();
            var (isValid, _) = sut.ValidateNameInput(name.Value);
            return !isValid;
        });
}
`

### Rules

1. **PBT supplements, does not replace, example-based tests.** Keep the example tests — they document specific behaviors. PBT adds confidence across the input space.
2. **Always filter generated inputs to valid domains.** Unconstrained string generation produces null, empty, and control characters that test error-handling rather than the property.
3. **Properties must be deterministic.** A property that depends on external I/O (DB, clock) is not a property — it is a flaky test. Mock all dependencies.
4. **Label failures.** Use `|@` in FsCheck to attach labels when a property has multiple conditions, so shrunk counterexamples are readable.

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
- During `/project:review` (run after every task)

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

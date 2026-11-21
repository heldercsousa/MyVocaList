using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class PersonRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private PersonRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new PersonRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── CRUD ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidPerson_PersistedAndReturnedById()
    {
        var person = new Person("John Doe") { Email = "john@example.com" };

        await _repo.AddAsync(person);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(person.Id);

        Assert.NotNull(found);
        Assert.Equal("John Doe", found.FullName);
    }

    // ── Collation-aware search ────────────────────────────────────────────

    [Fact]
    public async Task SearchByNameStartsWithAsync_CaseInsensitive_FindsMatch()
    {
        var person = new Person("João Silva") { Email = "joao.silva@example.com" };
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameStartsWithAsync("joão");

        Assert.Single(results);
        Assert.Equal("João Silva", results[0].FullName);
    }

    [Fact]
    public async Task SearchByNameOrEmailAsync_SearchByEmail_FindsMatch()
    {
        var person = new Person("Jane Smith");
        person.Email = "jane@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameOrEmailAsync("jane@");

        Assert.Single(results);
        Assert.Equal("Jane Smith", results[0].FullName);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsAllSortedByName()
    {
        _db.Set<Person>().AddRange(
            Create("Zara Adams"),
            Create("Alice Brown"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20);

        Assert.Equal(2, totalCount);
        var list = items.ToList();
        Assert.Equal("Alice Brown", list[0].FullName);
        Assert.Equal("Zara Adams", list[1].FullName);
    }

    [Fact]
    public async Task GetPagedAsync_WithQuery_FiltersResults()
    {
        _db.Set<Person>().AddRange(
            Create("Alice Brown"),
            Create("Bob Smith"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "alice");

        Assert.Equal(1, totalCount);
        Assert.Equal("Alice Brown", items.Single().FullName);
    }

    // ── Index constraints ─────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsDbUpdateException()
    {
        var p1 = Create("John Doe");
        p1.Email = "dup@example.com";
        var p2 = Create("Jane Smith");
        p2.Email = "dup@example.com";

        _db.Set<Person>().AddRange(p1, p2);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_SameNameDifferentEmail_AllowsDuplicate()
    {
        // BUG-088 (superseded AddAsync_SameNameNullBirthday_AllowsDuplicate): same name is allowed when the
        // identity differs. Name alone is not unique; the identity index covers name + email + birthday.
        var p1 = Create("John Doe");
        var p2 = Create("John Doe");   // different generated email, so a different identity

        _db.Set<Person>().AddRange(p1, p2);

        // Should not throw
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Set<Person>().CountAsync());
    }

    // [AC] REQ-BDAY-07: a day without a month violates the CHECK constraint
    [Fact]
    public async Task SaveChanges_DayWithoutMonth_ThrowsDbUpdateException()
    {
        var person = Create("John Doe");
        person.BirthDay = 15;
        person.BirthMonth = null;

        _db.Set<Person>().Add(person);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // [AC] REQ-BDAY-07: a month without a day violates the CHECK constraint
    [Fact]
    public async Task SaveChanges_MonthWithoutDay_ThrowsDbUpdateException()
    {
        var person = Create("John Doe");
        person.BirthDay = null;
        person.BirthMonth = 3;

        _db.Set<Person>().Add(person);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // [AC] REQ-BDAY-07: day 1-31 and month 1-12 are stored and read back as integers
    [Fact]
    public async Task SaveChanges_ValidBirthday_RoundTripsDayAndMonth()
    {
        var person = Create("John Doe");
        person.BirthDay = 15;
        person.BirthMonth = 3;
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(person.Id);

        Assert.NotNull(found);
        Assert.Equal(15, found.BirthDay);
        Assert.Equal(3, found.BirthMonth);
    }

    [Fact]
    public async Task AddAsync_NullEmailMultipleTimes_AllowsMultipleNulls()
    {
        // Nullable unique index: NULL != NULL in SQLite — multiple NULL emails are allowed.
        // Both have null email, so each carries a birthday (BUG-088 CHECK); the birthdays differ.
        var alice = Create("Alice Brown");
        alice.Email = null;
        alice.BirthDay = 1;
        alice.BirthMonth = 1;
        var bob = Create("Bob Smith");
        bob.Email = null;
        bob.BirthDay = 2;
        bob.BirthMonth = 2;
        _db.Set<Person>().AddRange(alice, bob);

        // Should not throw
        await _db.SaveChangesAsync();
    }

    // ── IsEmailTakenAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsEmailTakenAsync_NewEmail_ReturnsFalse()
    {
        var result = await _repo.IsEmailTakenAsync("new@example.com");
        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailTakenAsync_ExistingEmail_ReturnsTrue()
    {
        var person = Create("John Doe");
        person.Email = "taken@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var result = await _repo.IsEmailTakenAsync("taken@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task IsEmailTakenAsync_ExcludeSelf_ReturnsFalse()
    {
        var person = Create("John Doe");
        person.Email = "self@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        // Excluding own ID — should not be considered "taken"
        var result = await _repo.IsEmailTakenAsync("self@example.com", person.Id);

        Assert.False(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    // BUG-088 CHECK: every singer needs an email or a birthday. A unique generated email keeps the default valid.
    private static Person Create(string name) => new(name) { Email = $"{Guid.NewGuid():N}@example.com" };

    // ── Accent-insensitive search ─────────────────────────────────────────

    [Fact]
    public async Task SearchByNameStartsWithAsync_QueryWithoutAccents_FindsAccentedPerson()
    {
        _db.Set<Person>().Add(new Person("João Silva") { Email = "joao.silva@example.com" });
        _db.Set<Person>().Add(new Person("Anna Smith") { Email = "anna.smith@example.com" }); // should not match "joao"
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameStartsWithAsync("joao", 10, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("João Silva", results.First().FullName);
    }

    // ── BUG-088 identity queries (default collation, no C# normalization) ──

    [Fact]
    public async Task IdentityExistsAsync_SameIdentityDifferentCase_ReturnsTrue()
    {
        _db.Set<Person>().Add(new Person("João Silva") { BirthDay = 15, BirthMonth = 3 });
        await _db.SaveChangesAsync();

        Assert.True(await _repo.IdentityExistsAsync("JOAO SILVA", null, 15, 3));
    }

    [Fact]
    public async Task IdentityExistsAsync_ExcludeSelf_ReturnsFalse()
    {
        var person = new Person("Ana Lima") { Email = "ana@example.com" };
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        Assert.False(await _repo.IdentityExistsAsync("Ana Lima", "ana@example.com", null, null, person.Id));
    }

    // [AC] REQ-DUP-09: the list excludes the exact identity match when includeIdentityMatch is false
    [Fact]
    public async Task GetSameNameAsync_ExcludesExactMatchWhenNotIncluded()
    {
        var exact = new Person("Lia Costa") { Email = "lia@example.com", BirthDay = 1, BirthMonth = 2 };
        var namesake = new Person("Lia Costa") { Email = "other@example.com" };
        _db.Set<Person>().AddRange(exact, namesake);
        await _db.SaveChangesAsync();

        var results = await _repo.GetSameNameAsync("lia costa", "LIA@example.com", 1, 2, null, false, 3);

        var only = Assert.Single(results);
        Assert.Equal(namesake.Id, only.Id);
    }

    // [AC] REQ-DUP-13a: with includeIdentityMatch true, a name-only form also lists the exact name-only match
    [Fact]
    public async Task GetSameNameAsync_NameOnlyIncludeIdentityMatch_ReturnsExactNameOnlyMatch()
    {
        var nameOnly = new Person("Lia Costa");
        var namesake = new Person("Lia Costa") { Email = "other@example.com" };
        _db.Set<Person>().AddRange(nameOnly, namesake);
        await _db.SaveChangesAsync();

        var results = await _repo.GetSameNameAsync("lia costa", null, null, null, null, true, 3);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, p => p.Id == nameOnly.Id);
    }

    // [AC] REQ-DUP-13a: the count follows the same includeIdentityMatch rule as the list
    [Fact]
    public async Task CountSameNameAsync_NameOnlyWithAndWithoutIdentityMatch_CountsAccordingly()
    {
        _db.Set<Person>().AddRange(new Person("Lia Costa"), new Person("Lia Costa") { Email = "other@example.com" });
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _repo.CountSameNameAsync("lia costa", null, null, null, null, true));
        Assert.Equal(1, await _repo.CountSameNameAsync("lia costa", null, null, null, null, false));
    }

    // [AC] REQ-DUP-09: results are ordered by Id (oldest first), at most take rows, and the count is the full total
    [Fact]
    public async Task GetSameNameAsync_OrderedByIdAndTakeLimitsRows_CountIsFullTotal()
    {
        var people = Enumerable.Range(1, 4).Select(i => new Person("Lia Costa") { Email = $"n{i}@example.com" }).ToList();
        _db.Set<Person>().AddRange(people);
        await _db.SaveChangesAsync();

        var results = await _repo.GetSameNameAsync("Lia Costa", "new@example.com", null, null, null, false, 3);

        Assert.Equal(people.Take(3).Select(p => p.Id), results.Select(p => p.Id));
        Assert.Equal(4, await _repo.CountSameNameAsync("Lia Costa", "new@example.com", null, null, null, false));
    }

    // [AC] REQ-DUP-13: SameNameExistsAsync matches the name with the default collation and honours excludePersonId
    [Fact]
    public async Task SameNameExistsAsync_DifferentCaseAndAccent_ReturnsTrueUnlessExcluded()
    {
        var person = new Person("João Silva") { Email = "joao@example.com" };
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        Assert.True(await _repo.SameNameExistsAsync("JOAO SILVA"));
        Assert.False(await _repo.SameNameExistsAsync("JOAO SILVA", person.Id));
    }
}

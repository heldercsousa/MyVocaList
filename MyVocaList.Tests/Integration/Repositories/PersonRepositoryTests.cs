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
        person.SetNormalizedName("John Doe");

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
        var person = new Person("João Silva");
        person.SetNormalizedName("João Silva");
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
        person.SetNormalizedName("Jane Smith");
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
    public async Task AddAsync_SameNameNullBirthday_AllowsDuplicate()
    {
        // Same name + null birthday = allowed (filtered unique index only applies when birthday IS NOT NULL)
        var p1 = Create("John Doe");
        var p2 = Create("John Doe");   // no birthday — should be allowed

        _db.Set<Person>().AddRange(p1, p2);

        // Should not throw
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Set<Person>().CountAsync());
    }

    [Fact]
    public async Task AddAsync_SameNameSameBirthday_ThrowsDbUpdateException()
    {
        var p1 = Create("John Doe");
        p1.BirthdayDayMonth = "15/03";
        var p2 = Create("John Doe");
        p2.BirthdayDayMonth = "15/03";

        _db.Set<Person>().AddRange(p1, p2);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_NullEmailMultipleTimes_AllowsMultipleNulls()
    {
        // Nullable unique index: NULL != NULL in SQLite — multiple NULL emails are allowed
        _db.Set<Person>().AddRange(
            Create("Alice Brown"),
            Create("Bob Smith"));  // both have null email

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

    private static Person Create(string name)
    {
        var p = new Person(name);
        p.SetNormalizedName(name);
        return p;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// BUG-088 database backstop. Real SQLite temp file via <see cref="TestDbContextFactory"/>, migrated to the
/// latest migration with <c>MigrateAsync</c>, because the identity index is an expression index that exists
/// only as migration SQL. Rows are inserted with raw SQL so the database itself, not the service, is under test.
/// </summary>
public class PersonIdentityIndexTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.GetService<IMigrator>().MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // Literal SQL (test-controlled values): EF cannot map DBNull as a raw parameter.
    private Task InsertAsync(int id, string fullName, string? email, int? birthDay, int? birthMonth)
    {
        var mail = email is null ? "NULL" : $"'{email}'";
        var day = birthDay?.ToString() ?? "NULL";
        var month = birthMonth?.ToString() ?? "NULL";
        return _db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO People (Id, FullName, Email, BirthDay, BirthMonth, Participations, Absences) VALUES ({id}, '{fullName}', {mail}, {day}, {month}, 0, 0)");
    }

    // [AC] REQ-DUP-04: same name (case and accent insensitive), null email and same birthday is a duplicate identity
    [Fact]
    public async Task IdentityIndex_SameNameCaseAndAccentDifferent_NullEmailSameBirthday_IsRejected()
    {
        await InsertAsync(1, "João Silva", null, 15, 3);

        await Assert.ThrowsAnyAsync<Exception>(() => InsertAsync(2, "JOAO SILVA", null, 15, 3));
    }

    // [AC] REQ-DUP-04: same name with a different email is a different identity and is allowed
    [Fact]
    public async Task IdentityIndex_SameNameDifferentEmail_IsAllowed()
    {
        await InsertAsync(1, "John Doe", "a@example.com", 15, 3);

        await InsertAsync(2, "John Doe", "b@example.com", 15, 3);

        Assert.Equal(2, await _db.Set<Person>().CountAsync());
    }

    // [AC] REQ-DUP-04: the email part of the identity uses the default collation, so a case-only email difference is a duplicate
    [Fact]
    public async Task IdentityIndex_SameNameSameEmailDifferentCase_IsRejected()
    {
        await InsertAsync(1, "Ana Lima", "ANA@example.com", 1, 2);

        await Assert.ThrowsAnyAsync<Exception>(() => InsertAsync(2, "Ana Lima", "ana@example.com", 1, 2));
    }

    // [AC] REQ-DUP-04: the expression index exists after MigrateAsync (guards against a later table rebuild dropping it)
    [Fact]
    public async Task IdentityIndex_ExistsInSqliteMasterAfterMigrate()
    {
        var names = await _db.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'index' AND name = 'IX_Persons_Identity'")
            .ToListAsync();

        Assert.Equal(["IX_Persons_Identity"], names);
    }

    // [AC] REQ-DUP-14: the migrations drop CK_Persons_EmailOrBirthday, so the People table SQL no longer contains it
    [Fact]
    public async Task EmailOrBirthdayCheck_DroppedAfterMigrate_TableSqlHasNoCheck()
    {
        var tableSql = await _db.Database.SqlQueryRaw<string>(
            "SELECT sql AS Value FROM sqlite_master WHERE type = 'table' AND name = 'People'")
            .SingleAsync();

        Assert.DoesNotContain("CK_Persons_EmailOrBirthday", tableSql);
    }

    // [AC] REQ-DUP-12: a name-only row (no email, no birthday) is accepted by the database when no namesake exists
    [Fact]
    public async Task IdentityIndex_NameOnlyRow_IsAccepted()
    {
        await InsertAsync(1, "Lia Costa", null, null, null);

        Assert.Equal(1, await _db.Set<Person>().CountAsync());
    }

    // [AC] REQ-DUP-04: two name-only rows with the same name (case-insensitive) are the same identity, so the second is rejected
    [Fact]
    public async Task IdentityIndex_TwoNameOnlyRowsSameName_IsRejected()
    {
        await InsertAsync(1, "Lia Costa", null, null, null);

        await Assert.ThrowsAnyAsync<Exception>(() => InsertAsync(2, "LIA COSTA", null, null, null));
    }
}

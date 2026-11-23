using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Tests the <c>PersonBirthDayMonth</c> migration (REQ-BDAY-08). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>: migrate to the previous migration, seed raw <c>BirthdayDayMonth</c> values,
/// migrate to latest, assert the converted <c>BirthDay</c>/<c>BirthMonth</c> columns.
/// </summary>
public class PersonBirthDayMonthMigrationTests : IAsyncDisposable
{
    private const string PreviousMigration = "20261120052950_BackfillDerivedCatalogRows";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedToPreviousAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        return db;
    }

    private static Task SeedPersonAsync(AppDbContext db, int id, string? birthdayDayMonth)
    {
        var value = birthdayDayMonth is null ? "NULL" : $"'{birthdayDayMonth}'";
        // Email is set on every seeded row, including rows whose malformed birthday converts to NULL.
        return db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO People (Id, FullName, Email, BirthdayDayMonth, Participations, Absences) VALUES ({id}, 'Singer {id}', 'singer{id}@example.com', {value}, 0, 0)");
    }

    private static async Task<List<string>> ConvertedRowsAsync(AppDbContext db)
        => await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || COALESCE(CAST(BirthDay AS TEXT), '-') || '|' || COALESCE(CAST(BirthMonth AS TEXT), '-') AS Value FROM People ORDER BY Id")
            .ToListAsync();

    [Fact]
    public async Task Migration_SlashAndCompactFormats_ConvertToDayAndMonth()
    {
        // [AC] REQ-BDAY-08: DD/MM and DDMM values are converted into BirthDay and BirthMonth
        var db = await MigratedToPreviousAsync();
        await SeedPersonAsync(db, 1, "15/03");
        await SeedPersonAsync(db, 2, "1503");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|15|3", "2|15|3"], await ConvertedRowsAsync(db));
    }

    [Fact]
    public async Task Migration_MalformedAndInvalidValues_BecomeNull()
    {
        // [AC] REQ-BDAY-08: values that are not DD/MM or DDMM, or not a valid date for their month, become null in both columns
        var db = await MigratedToPreviousAsync();
        await SeedPersonAsync(db, 1, "ab/cd");
        await SeedPersonAsync(db, 2, "45/13");
        await SeedPersonAsync(db, 3, "30/02");
        await SeedPersonAsync(db, 4, "31/04");
        await SeedPersonAsync(db, 5, null);

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|-|-", "2|-|-", "3|-|-", "4|-|-", "5|-|-"], await ConvertedRowsAsync(db));
    }

    [Fact]
    public async Task Migration_Feb29_IsKept()
    {
        // [AC] REQ-BDAY-08: 29/02 is a valid birthday (no year is stored), so it survives the conversion
        var db = await MigratedToPreviousAsync();
        await SeedPersonAsync(db, 1, "29/02");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|29|2"], await ConvertedRowsAsync(db));
    }

    [Fact]
    public async Task Migration_DropsOldColumnAndIndex()
    {
        // [AC] REQ-BDAY-07: the BirthdayDayMonth column and its IX_Persons_Name_Birthday index are removed
        var db = await MigratedToPreviousAsync();

        await db.GetService<IMigrator>().MigrateAsync();

        var columns = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('People')").ToListAsync();
        Assert.DoesNotContain("BirthdayDayMonth", columns);
        Assert.Contains("BirthDay", columns);
        Assert.Contains("BirthMonth", columns);
        var indexes = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'index' AND tbl_name = 'People'").ToListAsync();
        Assert.DoesNotContain("IX_Persons_Name_Birthday", indexes);
    }

    [Fact]
    public async Task Migration_CheckConstraint_RejectsDayWithoutMonth()
    {
        // [AC] REQ-BDAY-07: the CHECK constraint is in place after the migration
        var db = await MigratedToPreviousAsync();
        await db.GetService<IMigrator>().MigrateAsync();

        var act = () => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO People (FullName, BirthDay, BirthMonth, Participations, Absences) VALUES ('X', 15, NULL, 0, 0)");

        await Assert.ThrowsAsync<SqliteException>(act);
    }

    public async ValueTask DisposeAsync()
    {
        var paths = _contexts.Select(c => c.Database.GetDbConnection().DataSource).ToList();
        foreach (var c in _contexts) await c.DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var p in paths)
            if (File.Exists(p)) File.Delete(p);
        GC.SuppressFinalize(this);
    }
}

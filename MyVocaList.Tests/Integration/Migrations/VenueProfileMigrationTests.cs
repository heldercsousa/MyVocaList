using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for <c>AddVenueProfile</c> (REQ-VEN-05). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>, never the in-memory provider. Temp databases are deleted on dispose.
/// </summary>
public class VenueProfileMigrationTests : IAsyncDisposable
{
    private const string MigrationSuffix = "_AddVenueProfile";

    private static readonly string[] NewColumns =
        ["AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", "ImageUrl", "Latitude", "Longitude"];

    private static readonly string[] NocaseTextColumns =
        ["AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", "ImageUrl"];

    private static readonly string[] SeededNames = ["Migration Venue A", "Migration Venue B"];

    private readonly List<AppDbContext> _contexts = [];

    private AppDbContext NewDb()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        return db;
    }

    private static string VenueProfileMigration(AppDbContext db)
        => db.Database.GetMigrations().Single(m => m.EndsWith(MigrationSuffix, StringComparison.Ordinal));

    private static string PreviousOf(AppDbContext db, string migration)
    {
        var all = db.Database.GetMigrations().ToList();
        var index = all.IndexOf(migration);
        Assert.True(index > 0, $"Migration '{migration}' has no predecessor.");
        return all[index - 1];
    }

    private static string NameList() => string.Join(", ", SeededNames.Select(n => $"'{n}'"));

    [Fact]
    public async Task AddVenueProfile_ExistingVenues_KeptWithNullNewColumns()
    {
        // [AC] REQ-VEN-05: the migration adds the nine columns as null for every existing venue and keeps the rows
        var db = NewDb();
        var migration = VenueProfileMigration(db);
        await db.Database.MigrateAsync(PreviousOf(db, migration));

        foreach (var name in SeededNames)
            await db.Database.ExecuteSqlRawAsync($"INSERT INTO Venues (Name) VALUES ('{name}')");

        await db.Database.MigrateAsync(migration);

        var rowCount = await db.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS Value FROM Venues WHERE Name IN ({NameList()})").SingleAsync();
        Assert.Equal(SeededNames.Length, rowCount);

        var nonNullValues = await db.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS Value FROM Venues WHERE Name IN ({NameList()}) AND ("
            + string.Join(" OR ", NewColumns.Select(c => $"\"{c}\" IS NOT NULL"))
            + ")").SingleAsync();
        Assert.Equal(0, nonNullValues);
    }

    [Fact]
    public async Task AddVenueProfile_Up_AddsNineNullableColumns_TextColumnsKeepNocaseNoaccent()
    {
        // [AC] REQ-VEN-05: the nine columns exist and are nullable; the text columns keep the NOCASE_NOACCENT collation
        var db = NewDb();
        await db.Database.MigrateAsync();

        var columns = await db.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_table_info('Venues')").ToListAsync();
        foreach (var column in NewColumns)
            Assert.Contains(column, columns);

        var notNullCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM pragma_table_info('Venues') WHERE \"notnull\" <> 0 AND name IN ("
            + string.Join(", ", NewColumns.Select(c => $"'{c}'")) + ")").SingleAsync();
        Assert.Equal(0, notNullCount);

        var tableSql = await db.Database.SqlQueryRaw<string>(
            "SELECT sql AS Value FROM sqlite_master WHERE type = 'table' AND name = 'Venues'").SingleAsync();
        foreach (var column in NocaseTextColumns)
            Assert.Matches($"\"{column}\"\\s+TEXT\\s+COLLATE\\s+NOCASE_NOACCENT", tableSql);
    }

    [Fact]
    public async Task AddVenueProfile_Down_DropsNineColumnsAndKeepsVenueIdAndName()
    {
        // [AC] REQ-VEN-05: rolling back drops the nine columns and keeps the existing venue rows (Id, Name) intact
        var db = NewDb();
        var migration = VenueProfileMigration(db);
        await db.Database.MigrateAsync(migration);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Venues (Id, Name, City, AddressLine1, Latitude, Longitude) VALUES (9001, 'Migration Venue A', 'Recife', 'Rua X', -8.05, -34.9)");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Venues (Id, Name) VALUES (9002, 'Migration Venue B')");

        await db.Database.MigrateAsync(PreviousOf(db, migration));

        var columns = await db.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_table_info('Venues')").ToListAsync();
        foreach (var column in NewColumns)
            Assert.DoesNotContain(column, columns);

        var ids = await db.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Venues WHERE Id IN (9001, 9002) ORDER BY Id").ToListAsync();
        Assert.Equal([9001, 9002], ids);

        var names = await db.Database.SqlQueryRaw<string>(
            "SELECT Name AS Value FROM Venues WHERE Id IN (9001, 9002) ORDER BY Id").ToListAsync();
        Assert.Equal(SeededNames, names);
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

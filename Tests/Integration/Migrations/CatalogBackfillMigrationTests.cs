using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Infra.Migrations;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Tests the data-only <c>BackfillDerivedCatalogRows</c> migration (REQ-CATPROP-09). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>: migrate to the previous migration, seed with raw SQL, migrate to latest, assert.
/// </summary>
public class CatalogBackfillMigrationTests : IAsyncDisposable
{
    private const string PreviousMigration = "20261120031836_RemoveSongVersionAndFeaturedArtists";
    private const string BackfillMigration = "20261120052950_BackfillDerivedCatalogRows";
    private const string Stamp = "'2026-01-01 00:00:00'";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedToPreviousAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        return db;
    }

    private static Task ExecAsync(AppDbContext db, string sql) => db.Database.ExecuteSqlRawAsync(sql);

    private static Task SeedArtistAsync(AppDbContext db, int id, string name)
        => ExecAsync(db, $"INSERT INTO Artists (Id, Name, CreatedAt, UpdatedAt) VALUES ({id}, '{name}', {Stamp}, {Stamp})");

    private static Task SeedSongAsync(AppDbContext db, int id, int artistId, string title)
        => ExecAsync(db, $"INSERT INTO Songs (Id, ArtistId, Title, CreatedAt, UpdatedAt) VALUES ({id}, {artistId}, '{title}', {Stamp}, {Stamp})");

    private static async Task<List<string>> CatalogRowsAsync(AppDbContext db)
        => await db.Database.SqlQueryRaw<string>("SELECT ArtistId || '|' || SongId AS Value FROM Catalog ORDER BY ArtistId, SongId").ToListAsync();

    private static Task MigrateToBackfillAsync(AppDbContext db) => db.GetService<IMigrator>().MigrateAsync(BackfillMigration);

    [Fact]
    public async Task Backfill_SongWithoutDerivedRow_GetsOne()
    {
        // [AC] REQ-CATPROP-09: every song has its derived row after migration
        var db = await MigratedToPreviousAsync();
        await SeedArtistAsync(db, 1, "Queen");
        await SeedArtistAsync(db, 2, "Muse");
        await SeedSongAsync(db, 1, 1, "One");
        await SeedSongAsync(db, 2, 2, "Two");

        await MigrateToBackfillAsync(db);

        Assert.Equal(["1|1", "2|2"], await CatalogRowsAsync(db));
    }

    [Fact]
    public async Task Backfill_ExistingManualAuthorRow_NotDuplicated()
    {
        // [AC] REQ-CATPROP-09: a song whose author already had a manual row still has exactly one row for the pair
        var db = await MigratedToPreviousAsync();
        await SeedArtistAsync(db, 1, "Queen");
        await SeedSongAsync(db, 1, 1, "One");
        await ExecAsync(db, "INSERT INTO Catalog (ArtistId, SongId) VALUES (1, 1)");

        await MigrateToBackfillAsync(db);

        var count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Catalog WHERE ArtistId = 1 AND SongId = 1").SingleAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Backfill_CoverRowsUntouched()
    {
        // [AC] REQ-CATPROP-09: no other row is added, changed or removed; cover rows survive as they were
        var db = await MigratedToPreviousAsync();
        await SeedArtistAsync(db, 1, "Queen");
        await SeedArtistAsync(db, 2, "Muse");
        await SeedSongAsync(db, 1, 1, "One");
        await ExecAsync(db, "INSERT INTO Catalog (ArtistId, SongId) VALUES (2, 1)");

        await MigrateToBackfillAsync(db);

        Assert.Equal(["1|1", "2|1"], await CatalogRowsAsync(db));
    }

    [Fact]
    public async Task Backfill_SecondApply_InsertsNothing()
    {
        // [AC] REQ-CATPROP-09: applying the backfill to a database that already satisfies the invariant inserts nothing
        var db = await MigratedToPreviousAsync();
        await SeedArtistAsync(db, 1, "Queen");
        await SeedSongAsync(db, 1, 1, "One");
        await SeedSongAsync(db, 2, 1, "Two");
        await MigrateToBackfillAsync(db);
        var before = await CatalogRowsAsync(db);
        Assert.Equal(["1|1", "1|2"], before); // guards against a vacuous pass

        await db.Database.ExecuteSqlRawAsync(BackfillDerivedCatalogRows.BackfillSql);

        Assert.Equal(before, await CatalogRowsAsync(db));
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

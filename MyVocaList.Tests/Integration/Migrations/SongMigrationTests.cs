using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for the Song/Artist schema changes of the song-form title-first change (SFT).
/// M1 drops the HasManualEdits columns (REQ-SFT-37). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>, never the in-memory provider. Temp databases are deleted on dispose.
/// </summary>
public class SongMigrationTests : IAsyncDisposable
{
    private const string DropHasManualEditsName = "DropHasManualEdits";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedToAsync(string? target = null)
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(target);
        return db;
    }

    private static async Task<IReadOnlyList<string>> ColumnsAsync(AppDbContext db, string table)
        => await db.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}')").ToListAsync();

    private static async Task<bool> IsUniqueIndexAsync(AppDbContext db, string table, string indexName)
        => await db.Database.SqlQueryRaw<int>(
            $"SELECT \"unique\" AS Value FROM pragma_index_list('{table}') WHERE name = '{indexName}'")
            .SingleOrDefaultAsync() == 1;

    [Fact]
    public async Task M1_DropsHasManualEditsFromSongsAndArtists()
    {
        // [AC] REQ-SFT-37: HasManualEdits column no longer exists on Songs or Artists after migration
        var db = await MigratedToAsync();

        Assert.DoesNotContain("HasManualEdits", await ColumnsAsync(db, "Songs"));
        Assert.DoesNotContain("HasManualEdits", await ColumnsAsync(db, "Artists"));
    }

    [Fact]
    public async Task M1_ExistingRowsSurvive()
    {
        // [AC] REQ-SFT-37: dropping the columns keeps every existing artist and song row intact
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrations = db.Database.GetMigrations().ToList();
        var index = migrations.FindIndex(m => m.EndsWith("_" + DropHasManualEditsName, StringComparison.Ordinal));
        if (index < 1)
            throw new InvalidOperationException($"Migration '{DropHasManualEditsName}' not found (or has no predecessor).");
        var before = migrations[index - 1];

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(before);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Artists (Id, Name, HasManualEdits, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', 1, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Songs (Id, ArtistId, Title, Version, HasManualEdits, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '', 1, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Songs (Id, ArtistId, Title, Version, HasManualEdits, CreatedAt, UpdatedAt) VALUES (2, 1, 'One', 'Live', 0, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");

        await migrator.MigrateAsync();

        var artists = await db.Database.SqlQueryRaw<string>("SELECT Id || '|' || Name AS Value FROM Artists").ToListAsync();
        var songs = await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || ArtistId || '|' || Title || '|' || Version AS Value FROM Songs ORDER BY Id").ToListAsync();
        Assert.Equal(["1|Metallica"], artists);
        Assert.Equal(["1|1|One|", "2|1|One|Live"], songs);
    }

    [Fact]
    public async Task M1_TableRebuild_KeepsUniqueCollatedIndexes()
    {
        // [AC] REQ-SFT-52: after the M1 table rebuild, Artists.Name stays unique under NOCASE_NOACCENT and the song identity index stays unique
        var db = await MigratedToAsync();

        Assert.True(await IsUniqueIndexAsync(db, "Artists", "IX_Artists_Name"));
        Assert.True(await IsUniqueIndexAsync(db, "Songs", "IX_Songs_ArtistId_Title_Version"));

        // Tolerates both schemas: the NOT NULL HasManualEdits column exists only until M1 is applied.
        var artistFlag = (await ColumnsAsync(db, "Artists")).Contains("HasManualEdits");
        var songFlag = (await ColumnsAsync(db, "Songs")).Contains("HasManualEdits");
        Task InsertArtist(int id, string name) => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Artists (Id, Name, {(artistFlag ? "HasManualEdits, " : "")}CreatedAt, UpdatedAt) VALUES ({id}, '{name}', {(artistFlag ? "0, " : "")}'2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        Task InsertSong(int id, string title) => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Songs (Id, ArtistId, Title, Version, {(songFlag ? "HasManualEdits, " : "")}CreatedAt, UpdatedAt) VALUES ({id}, 1, '{title}', '', {(songFlag ? "0, " : "")}'2026-01-01 00:00:00', '2026-01-01 00:00:00')");

        await InsertArtist(1, "Metallica");
        await Assert.ThrowsAnyAsync<Exception>(() => InsertArtist(2, "metallica"));

        await InsertSong(1, "One");
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertSong(2, "one"));
        Assert.IsType<SqliteException>(ex is DbUpdateException ? ex.InnerException : ex);
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

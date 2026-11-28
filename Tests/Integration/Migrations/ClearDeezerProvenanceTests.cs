using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for <c>ClearDeezerProvenance</c> (music-sourcing change, migration section). Rows are edited in place:
/// ids, titles, lyrics and karaoke links survive. Real SQLite temp file, never the in-memory provider.
/// </summary>
public class ClearDeezerProvenanceTests : IAsyncDisposable
{
    private const string MigrationName = "ClearDeezerProvenance";
    private const string ts = "'2026-01-01 00:00:00'";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<(AppDbContext Db, IMigrator Migrator, string Target)> MigratedToPreviousAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrations = db.Database.GetMigrations().ToList();
        var index = migrations.FindIndex(m => m.EndsWith("_" + MigrationName, StringComparison.Ordinal));
        if (index < 1)
            throw new InvalidOperationException($"Migration '{MigrationName}' not found (or has no predecessor).");
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[index - 1]);
        return (db, migrator, migrations[index]);
    }

    private static async Task SeedAsync(AppDbContext db, string songProvider, string artistProvider)
    {
        await db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Artists (Id, Name, ExternalProvider, ExternalId, ImageUrl, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', '{artistProvider}', 'a1', 'https://img/a', {ts}, {ts})");
        await db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Songs (Id, ArtistId, Title, ExternalProvider, ExternalId, ImageUrl, Lyrics, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '{songProvider}', 's1', 'https://img/s', 'My lyrics', {ts}, {ts})");
        await db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO SongKaraokeUrls (SongId, VideoId, PlayCount, AddedAt) VALUES (1, 'abcdefghijk', 3, {ts})");
    }

    [Fact]
    public async Task Migrate_DeezerSong_KeepsIdTitleLyricsAndClearsProvenance()
    {
        // [AC] REQ-MSRC-01: Deezer provenance is cleared in place; the row keeps id, title and lyrics
        var (db, migrator, target) = await MigratedToPreviousAsync();
        await SeedAsync(db, "Deezer", "deezer");

        await migrator.MigrateAsync(target);

        var song = await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || Title || '|' || Lyrics || '|' || IFNULL(ExternalProvider,'null') || '|' || IFNULL(ExternalId,'null') || '|' || IFNULL(ImageUrl,'null') AS Value FROM Songs").SingleAsync();
        var artist = await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || Name || '|' || IFNULL(ExternalProvider,'null') || '|' || IFNULL(ExternalId,'null') || '|' || IFNULL(ImageUrl,'null') AS Value FROM Artists").SingleAsync();
        Assert.Equal("1|One|My lyrics|null|null|null", song);
        Assert.Equal("1|Metallica|null|null|null", artist);
    }

    [Fact]
    public async Task Migrate_KeepsKaraokeUrlsAndHistoryRows()
    {
        // [AC] REQ-MSRC-01: rows are never recreated, so dependent rows (karaoke links, play counts) survive
        var (db, migrator, target) = await MigratedToPreviousAsync();
        await SeedAsync(db, "Deezer", "Deezer");

        await migrator.MigrateAsync(target);

        var link = await db.Database.SqlQueryRaw<string>(
            "SELECT SongId || '|' || VideoId || '|' || PlayCount AS Value FROM SongKaraokeUrls").SingleAsync();
        Assert.Equal("1|abcdefghijk|3", link);
    }

    [Fact]
    public async Task Migrate_DbWithoutDeezerRows_NoChange()
    {
        // [AC] REQ-MSRC-01: non-Deezer provenance (MusicBrainz) is left untouched
        var (db, migrator, target) = await MigratedToPreviousAsync();
        await SeedAsync(db, "MusicBrainz", "MusicBrainz");

        await migrator.MigrateAsync(target);

        var song = await db.Database.SqlQueryRaw<string>(
            "SELECT ExternalProvider || '|' || ExternalId || '|' || ImageUrl AS Value FROM Songs").SingleAsync();
        Assert.Equal("MusicBrainz|s1|https://img/s", song);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var c in _contexts)
        {
            var path = c.Database.GetDbConnection().DataSource;
            await c.DisposeAsync();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(path); } catch { }
        }
    }
}

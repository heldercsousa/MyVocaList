using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for <c>RemoveSongVersionAndFeaturedArtists</c> (REQ-RMV-03, REQ-RMV-04, REQ-RMV-05;
/// INV-RMV-1, INV-RMV-2). Real SQLite temp file via <see cref="TestDbContextFactory"/> (so the
/// NOCASE_NOACCENT collation of <c>CollationInterceptor</c> is registered), never the in-memory provider.
/// Temp databases are deleted on dispose.
/// </summary>
public class RemoveSongVersionMigrationTests : IAsyncDisposable
{
    private const string PreviousMigration = "20261116152258_AddSongLyricsOrigin";
    private const string NewMigrationName = "RemoveSongVersionAndFeaturedArtists";
    private const string Now = "2026-01-01 00:00:00";
    private const string SongsSelectTitles = "SELECT Id || '|' || Title AS Value FROM Songs ORDER BY Id";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> AtPreviousMigrationAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        await db.Database.ExecuteSqlAsync($"INSERT INTO Artists (Id, Name, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', {Now}, {Now})");
        return db;
    }

    private async Task<AppDbContext> MigratedToLatestAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync();
        return db;
    }

    private static string NewMigrationId(AppDbContext db) =>
        db.Database.GetMigrations().SingleOrDefault(m => m.EndsWith("_" + NewMigrationName, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Migration '{NewMigrationName}' not found.");

    private static Task InsertSongAsync(AppDbContext db, int id, string title, string version, string? featured = null)
        => db.Database.ExecuteSqlAsync(
            $"INSERT INTO Songs (Id, ArtistId, Title, Version, FeaturedArtists, CreatedAt, UpdatedAt) VALUES ({id}, 1, {title}, {version}, {featured}, {Now}, {Now})");

    private static async Task<List<string>> RowsAsync(AppDbContext db, string sql)
        => await db.Database.SqlQueryRaw<string>(sql).ToListAsync();

    private static async Task<List<string>> ColumnsAsync(AppDbContext db, string table)
        => await RowsAsync(db, $"SELECT name AS Value FROM pragma_table_info('{table}')");

    // ---- REQ-RMV-03 / REQ-RMV-02: schema after the migration ----

    [Fact]
    public async Task Up_DropsVersionAndFeaturedArtistsColumns()
    {
        // [AC] REQ-RMV-03: both Songs columns are dropped by the migration
        var db = await MigratedToLatestAsync();

        var columns = await ColumnsAsync(db, "Songs");
        Assert.DoesNotContain("Version", columns);
        Assert.DoesNotContain("FeaturedArtists", columns);
    }

    [Fact]
    public async Task Up_UniqueTwoColumnIdentityIndex_UsesCollatedTitle()
    {
        // [AC] REQ-RMV-02, INV-RMV-1: IX_Songs_ArtistId_Title is unique on (ArtistId, Title) with the NOCASE_NOACCENT collation
        var db = await MigratedToLatestAsync();

        var indexList = await RowsAsync(db, "SELECT name || '|' || \"unique\" AS Value FROM pragma_index_list('Songs') WHERE name = 'IX_Songs_ArtistId_Title'");
        Assert.Equal(["IX_Songs_ArtistId_Title|1"], indexList);

        var keyColumns = await RowsAsync(db, "SELECT name || '|' || coll AS Value FROM pragma_index_xinfo('IX_Songs_ArtistId_Title') WHERE key = 1 ORDER BY seqno");
        Assert.Equal(["ArtistId|BINARY", "Title|NOCASE_NOACCENT"], keyColumns);
    }

    [Fact]
    public async Task Up_ThreeColumnIndexIsGone()
    {
        // [AC] REQ-RMV-03: IX_Songs_ArtistId_Title_Version no longer exists
        var db = await MigratedToLatestAsync();

        var names = await RowsAsync(db, "SELECT name AS Value FROM pragma_index_list('Songs')");
        Assert.DoesNotContain("IX_Songs_ArtistId_Title_Version", names);
    }

    [Fact]
    public async Task Up_KeepsExternalIdentityIndexAndSearchTriggers()
    {
        // [AC] REQ-RMV-03: the ExternalProvider+ExternalId unique index, the SongSearch table and all seven FTS triggers survive the migration
        var db = await MigratedToLatestAsync();

        var unique = await RowsAsync(db, "SELECT \"unique\" AS Value FROM pragma_index_list('Songs') WHERE name = 'IX_Songs_ExternalProvider_ExternalId'");
        Assert.Equal(["1"], unique);

        var tables = await RowsAsync(db, "SELECT name AS Value FROM sqlite_master WHERE type = 'table'");
        Assert.Contains("SongSearch", tables);
        Assert.Contains("ArtistSearch", tables);

        var triggers = await RowsAsync(db, "SELECT name AS Value FROM sqlite_master WHERE type = 'trigger'");
        string[] expected =
        [
            "SongSearch_ai", "SongSearch_au", "SongSearch_ad", "SongSearch_artist_au",
            "ArtistSearch_ai", "ArtistSearch_au", "ArtistSearch_ad",
        ];
        foreach (var name in expected) Assert.Contains(name, triggers);
    }

    // ---- REQ-RMV-04: rename rules on colliding rows ----

    [Fact]
    public async Task Rename_EmptyVersionLowestIdKeepsTitle_WhitespaceVersionCountsAsEmpty()
    {
        // [AC] REQ-RMV-04 (a): the empty-Version row with the lowest Id keeps its title; a whitespace-only Version counts as empty
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X", "Live");
        await InsertSongAsync(db, 2, "X", "");
        await InsertSongAsync(db, 3, "X", "   ");

        await db.GetService<IMigrator>().MigrateAsync();

        // Id 1 gets rule (b); Id 3 has no suffix from rule (b), so rule (c) applies
        Assert.Equal(["1|X (Live)", "2|X", "3|X #3"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_NoEmptyVersion_LowestIdKeepsTitle()
    {
        // [AC] REQ-RMV-04 (a): without any empty Version, the lowest-Id row keeps its title
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X", "Live");
        await InsertSongAsync(db, 2, "X", "Demo");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|X", "2|X (Demo)"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_SuffixUsesTrimmedVersion()
    {
        // [AC] REQ-RMV-04 (b): the suffix is ' (' + trimmed Version + ')'
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X", "");
        await InsertSongAsync(db, 2, "X", "  Live  ");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|X", "2|X (Live)"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_RowCollidingWithPreExistingRow_GetsIdSuffix()
    {
        // [AC] REQ-RMV-04 (c): a row still colliding after rule (b), here with a pre-existing row, gets ' #<Id>'
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X (Live)", "");
        await InsertSongAsync(db, 2, "X", "");
        await InsertSongAsync(db, 3, "X", "Live");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|X (Live)", "2|X", "3|X (Live) #3"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_TruncatesTitleSoSuffixStaysIntactAtMost250()
    {
        // [AC] REQ-RMV-04 (d): title plus suffix is at most 250 characters; the suffix is never cut
        var longTitle = new string('a', 250);
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, longTitle, "");
        await InsertSongAsync(db, 2, longTitle, "Live");
        await InsertSongAsync(db, 3, longTitle, "   "); // whitespace-only: empty after trim, distinct under the old 3-column index

        await db.GetService<IMigrator>().MigrateAsync();

        var expected = new[]
        {
            "1|" + longTitle,
            "2|" + new string('a', 250 - " (Live)".Length) + " (Live)",
            "3|" + new string('a', 250 - " #3".Length) + " #3",
        };
        Assert.Equal(expected, await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_NeverFails_WhenRenamedTitleWouldCollideUnderTheOldThreeColumnIndex()
    {
        // [AC] REQ-RMV-04 (e): the migration never fails because of duplicates, even when a rename would collide under the old 3-column index
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X", "Live");
        await InsertSongAsync(db, 2, "X (Live)", "Live");
        await InsertSongAsync(db, 3, "X", "");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|X (Live)", "2|X (Live) #2", "3|X"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Rename_NoCollisionDatabase_TitlesUnchanged()
    {
        // [AC] REQ-RMV-04: without collisions only the columns are dropped and no title changes
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "One", "");
        await InsertSongAsync(db, 2, "Two", "Live", "Featuring Someone");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(["1|One", "2|Two"], await RowsAsync(db, SongsSelectTitles));
    }

    // ---- REQ-RMV-03 / INV-RMV-2: row and foreign-key integrity ----

    [Fact]
    public async Task Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty()
    {
        // [AC] REQ-RMV-03, INV-RMV-2: the row count is unchanged, foreign-key rows still point at the kept songs, PRAGMA foreign_key_check is empty
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "One", "");
        await InsertSongAsync(db, 2, "Two", "Live");
        await InsertSongAsync(db, 3, "Two", "Demo");
        await db.Database.ExecuteSqlAsync($"INSERT INTO Catalog (ArtistId, SongId) VALUES (1, 2)");
        await db.Database.ExecuteSqlAsync($"INSERT INTO SongKaraokeUrls (SongId, VideoId, PlayCount) VALUES (3, 'vid-3', 0)");

        // Pinned to this migration: later data migrations (BackfillDerivedCatalogRows) legitimately add Catalog rows.
        await db.GetService<IMigrator>().MigrateAsync(NewMigrationId(db));

        Assert.Equal(["1", "2", "3"], await RowsAsync(db, "SELECT Id AS Value FROM Songs ORDER BY Id"));
        Assert.Equal(["1|2"], await RowsAsync(db, "SELECT ArtistId || '|' || SongId AS Value FROM Catalog"));
        Assert.Equal(["3|vid-3"], await RowsAsync(db, "SELECT SongId || '|' || VideoId AS Value FROM SongKaraokeUrls"));
        Assert.Empty(await RowsAsync(db, "SELECT \"table\" AS Value FROM pragma_foreign_key_check"));
    }

    // ---- REQ-RMV-05: Down ----

    [Fact]
    public async Task Down_RestoresEmptyColumnsAndThreeColumnIndex_DoesNotUndoRenames()
    {
        // [AC] REQ-RMV-05: Down re-adds Version (NOT NULL, default '') and FeaturedArtists (nullable), recreates the 3-column index, and keeps the renamed titles
        var db = await AtPreviousMigrationAsync();
        await InsertSongAsync(db, 1, "X", "");
        await InsertSongAsync(db, 2, "X", "Live");

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync();
        await migrator.MigrateAsync(PreviousMigration);

        var columns = await RowsAsync(db,
            "SELECT name || '|' || \"notnull\" || '|' || IFNULL(dflt_value, 'null') AS Value FROM pragma_table_info('Songs') WHERE name IN ('Version', 'FeaturedArtists') ORDER BY name");
        Assert.Equal(["FeaturedArtists|0|null", "Version|1|''"], columns);

        var versionDefinition = await RowsAsync(db, "SELECT sql AS Value FROM sqlite_master WHERE type = 'table' AND name = 'Songs'");
        Assert.Matches("\"Version\"[^,]*NOCASE_NOACCENT", versionDefinition.Single());

        var indexes = await RowsAsync(db, "SELECT name || '|' || \"unique\" AS Value FROM pragma_index_list('Songs') WHERE name LIKE 'IX_Songs_ArtistId_Title%' ORDER BY name");
        Assert.Equal(["IX_Songs_ArtistId_Title_Version|1"], indexes);

        var versions = await RowsAsync(db, "SELECT Version AS Value FROM Songs ORDER BY Id");
        Assert.Equal(["", ""], versions);
        Assert.Equal(["1|X", "2|X (Live)"], await RowsAsync(db, SongsSelectTitles));
    }

    [Fact]
    public async Task Down_IsRegisteredAsTheNewMigration()
    {
        // [AC] REQ-RMV-05: the migration exists after the previous one (the Down target is the previous migration)
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrations = db.Database.GetMigrations().ToList();

        Assert.Equal(PreviousMigration, migrations[migrations.IndexOf(NewMigrationId(db)) - 1]);
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

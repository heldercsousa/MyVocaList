using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// BUG-080 (Major): the venuesSeedForTest migration's Down() used to run "DELETE FROM Venues",
/// wiping every venue (including user-created ones) on rollback. Real SQLite (temp file) via
/// TestDbContextFactory, per testing.md anti-patterns. Approach: migrate a temp-file DB up to the
/// seed migration, insert a user venue, migrate down to the previous migration (InitialCreate)
/// through IMigrator, and assert the venue is still there.
/// </summary>
public class VenuesSeedMigrationDownTests
{
    private const string SeedMigration = "20260211055225_venuesSeedForTest";
    private const string PreviousMigration = "20260107193224_InitialCreate";
    private const string UserVenueName = "BUG-080 User Venue";

    [Fact]
    public async Task Down_DoesNotDeleteUserVenues()
    {
        // [AC] BUG-080: Down() never deletes user venues
        await using var db = TestDbContextFactory.Create();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(SeedMigration);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Venues (Name) VALUES ({0})", UserVenueName);

        await migrator.MigrateAsync(PreviousMigration);

        var count = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS \"Value\" FROM Venues WHERE Name = {0}", UserVenueName).SingleAsync();
        Assert.Equal(1, count);
    }
}

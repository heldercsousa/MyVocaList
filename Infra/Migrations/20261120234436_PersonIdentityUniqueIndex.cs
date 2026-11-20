using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PersonIdentityUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG-088 REQ-DUP-04, REQ-DUP-11. Kept in its own migration: EF's SQLite generator defers the People
            // table rebuild of PersonEmailOrBirthdayCheck to the end of THAT migration, so a raw index in the same
            // migration would be dropped by the rebuild. Here the table is already rebuilt, so the index survives.
            // Expression index: NULL email and NULL day/month are coalesced so they count as empty. Collation is the
            // literal of CollationConstants.Default (migrations stay frozen). No dedupe: a remaining duplicate aborts.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_Persons_Identity ON People (
                    FullName COLLATE NOCASE_NOACCENT,
                    COALESCE(Email, '') COLLATE NOCASE_NOACCENT,
                    COALESCE(BirthDay, 0),
                    COALESCE(BirthMonth, 0));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Persons_Identity;");
        }
    }
}

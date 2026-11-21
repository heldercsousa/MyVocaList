using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PersonIdentityUniqueIndexRestore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG-088 REQ-DUP-14, REQ-DUP-04. The index is restored here, after the previous migration (the CHECK drop) has finished the People
            // rebuild, because a rebuild drops raw-SQL indexes. DROP IF EXISTS keeps this idempotent. The collation is the
            // literal of CollationConstants.Default (migrations stay frozen). Same SQL as PersonIdentityUniqueIndex.
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Persons_Identity;");

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

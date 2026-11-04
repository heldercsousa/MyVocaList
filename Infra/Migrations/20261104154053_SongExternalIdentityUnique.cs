using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class SongExternalIdentityUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ExternalId",
                table: "Songs");

            // REQ-SFT-39: before the unique index is built, the lowest Id keeps a duplicated
            // (ExternalProvider, ExternalId) identity; the other rows keep all their data and lose
            // only the identity. No row is deleted.
            migrationBuilder.Sql(
                "UPDATE Songs SET ExternalId = NULL, ExternalProvider = NULL " +
                "WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
                "AND Id NOT IN (SELECT MIN(Id) FROM Songs WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
                "GROUP BY ExternalProvider, ExternalId);");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ExternalProvider_ExternalId",
                table: "Songs",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Identities cleared by Up's dedupe are not restored.
            migrationBuilder.DropIndex(
                name: "IX_Songs_ExternalProvider_ExternalId",
                table: "Songs");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ExternalId",
                table: "Songs",
                column: "ExternalId");
        }
    }
}

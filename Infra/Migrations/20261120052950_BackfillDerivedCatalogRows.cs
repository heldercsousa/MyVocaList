using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDerivedCatalogRows : Migration
    {
        /// <summary>Inserts each song's derived catalog row (author, song); idempotent and keeps manual rows.</summary>
        public const string BackfillSql = "INSERT OR IGNORE INTO Catalog (ArtistId, SongId) SELECT ArtistId, Id FROM Songs;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(BackfillSql);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberate no-op: derived rows cannot be told apart from rows a user added by hand (D4).
        }
    }
}

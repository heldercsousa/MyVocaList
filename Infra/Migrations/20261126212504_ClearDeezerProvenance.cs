using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ClearDeezerProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Music-sourcing change: Deezer Content may not be kept locally (Deezer terms III.4). Rows are edited in place, never
            // recreated, so ids, lyrics, karaoke links and performance history survive. Idempotent: a second run matches nothing.
            // The UPDATE on Songs/Artists keeps the FTS5 triggers intact (no table rebuild).
            migrationBuilder.Sql(
                "UPDATE Songs SET ExternalProvider = NULL, ExternalId = NULL, ImageUrl = NULL WHERE lower(ExternalProvider) = 'deezer';");
            migrationBuilder.Sql(
                "UPDATE Artists SET ExternalProvider = NULL, ExternalId = NULL, ImageUrl = NULL WHERE lower(ExternalProvider) = 'deezer';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op by design: restoring Deezer values would mean keeping a copy of Deezer Content (Deezer terms III.4).
        }
    }
}

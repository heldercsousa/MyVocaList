using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToSongsAndArtists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Songs",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Artists",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                collation: "NOCASE_NOACCENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // EF's DropColumn rebuilds the table on SQLite, which fails while the search triggers of
            // AddSearchIndexes reference Songs/Artists. The native statement drops the column in place
            // (no index, trigger or view references ImageUrl).
            migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"ImageUrl\";");
            migrationBuilder.Sql("ALTER TABLE \"Artists\" DROP COLUMN \"ImageUrl\";");
        }
    }
}

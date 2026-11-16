using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSongLyricsOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyricsOrigin",
                table: "Songs",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // EF's DropColumn rebuilds the table on SQLite, which fails while the search triggers of
            // AddSearchIndexes reference Songs. The native statement drops the column in place
            // (no index, trigger or view references LyricsOrigin).
            migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"LyricsOrigin\";");
        }
    }
}

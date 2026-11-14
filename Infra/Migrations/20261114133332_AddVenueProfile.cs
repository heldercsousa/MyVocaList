using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Venues",
                type: "TEXT",
                maxLength: 100,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Venues",
                type: "TEXT",
                maxLength: 100,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Venues",
                type: "TEXT",
                maxLength: 80,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Venues",
                type: "TEXT",
                maxLength: 60,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Venues",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Venues",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Venues",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Venues",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Venues",
                type: "TEXT",
                maxLength: 80,
                nullable: true,
                collation: "NOCASE_NOACCENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // EF's DropColumn rebuilds the table on SQLite; Venues is the parent of the events tables' foreign keys,
            // so the native statement drops each column in place. No index, trigger or view references these columns.
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"AddressLine1\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"AddressLine2\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"City\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"Country\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"ImageUrl\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"Latitude\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"Longitude\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"PostalCode\";");
            migrationBuilder.Sql("ALTER TABLE \"Venues\" DROP COLUMN \"Region\";");
        }
    }
}

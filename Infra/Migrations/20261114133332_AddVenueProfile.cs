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
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Venues");
        }
    }
}

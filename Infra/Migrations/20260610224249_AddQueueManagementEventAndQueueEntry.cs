using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueManagementEventAndQueueEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueueManagementEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VenueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE_NOACCENT"),
                    ScheduledStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScheduledEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "VideoKaraoke", collation: "NOCASE_NOACCENT"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueManagementEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueManagementEvents_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    SongId = table.Column<int>(type: "INTEGER", nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PerformanceStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PerformanceEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PerformanceDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueEntries_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueueEntries_QueueManagementEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "QueueManagementEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_EventPosition",
                table: "QueueEntries",
                columns: new[] { "EventId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_EventStatus",
                table: "QueueEntries",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PersonId",
                table: "QueueEntries",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_SongId",
                table: "QueueEntries",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_UniquePersonPerEvent",
                table: "QueueEntries",
                columns: new[] { "EventId", "PersonId" },
                unique: true,
                filter: "[Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_QueueManagementEvents_UniqueName",
                table: "QueueManagementEvents",
                columns: new[] { "VenueId", "Name", "ScheduledStartTime" },
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueEntries");

            migrationBuilder.DropTable(
                name: "QueueManagementEvents");
        }
    }
}

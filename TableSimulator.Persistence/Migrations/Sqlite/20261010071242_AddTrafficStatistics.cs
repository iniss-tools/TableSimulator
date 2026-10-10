using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddTrafficStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardTraffic",
                columns: table => new
                {
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<int>(type: "INTEGER", nullable: false),
                    Minute = table.Column<long>(type: "INTEGER", nullable: false),
                    Frames = table.Column<int>(type: "INTEGER", nullable: false),
                    Acks = table.Column<int>(type: "INTEGER", nullable: false),
                    Naks = table.Column<int>(type: "INTEGER", nullable: false),
                    Unanswered = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxGapMs = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTraffic", x => new { x.Line, x.Address, x.Minute });
                });

            migrationBuilder.CreateTable(
                name: "LineStatusChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineStatusChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineTraffic",
                columns: table => new
                {
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Minute = table.Column<long>(type: "INTEGER", nullable: false),
                    Ok = table.Column<int>(type: "INTEGER", nullable: false),
                    BadChecksum = table.Column<int>(type: "INTEGER", nullable: false),
                    NoBoard = table.Column<int>(type: "INTEGER", nullable: false),
                    Noise = table.Column<int>(type: "INTEGER", nullable: false),
                    Other = table.Column<int>(type: "INTEGER", nullable: false),
                    Replies = table.Column<int>(type: "INTEGER", nullable: false),
                    Faults = table.Column<int>(type: "INTEGER", nullable: false),
                    BytesIn = table.Column<long>(type: "INTEGER", nullable: false),
                    BytesOut = table.Column<long>(type: "INTEGER", nullable: false),
                    Connections = table.Column<int>(type: "INTEGER", nullable: false),
                    Disconnections = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineTraffic", x => new { x.Line, x.Minute });
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardTraffic_Minute",
                table: "BoardTraffic",
                column: "Minute");

            migrationBuilder.CreateIndex(
                name: "IX_LineStatusChanges_TimeMs",
                table: "LineStatusChanges",
                column: "TimeMs");

            migrationBuilder.CreateIndex(
                name: "IX_LineTraffic_Minute",
                table: "LineTraffic",
                column: "Minute");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardTraffic");

            migrationBuilder.DropTable(
                name: "LineStatusChanges");

            migrationBuilder.DropTable(
                name: "LineTraffic");
        }
    }
}

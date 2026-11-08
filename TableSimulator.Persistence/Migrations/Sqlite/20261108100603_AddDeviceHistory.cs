using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddDeviceHistory : Migration
    {
        private static readonly string[] LightStateColumns = ["Line", "Address", "TimeMs"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceAnnouncements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<int>(type: "INTEGER", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    StartMs = table.Column<long>(type: "INTEGER", nullable: false),
                    EndMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Sounds = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLightStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Flags = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLightStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAnnouncements_EndMs",
                table: "DeviceAnnouncements",
                column: "EndMs");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAnnouncements_StartMs",
                table: "DeviceAnnouncements",
                column: "StartMs");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLightStates_Line_Address_TimeMs",
                table: "DeviceLightStates",
                columns: LightStateColumns);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLightStates_TimeMs",
                table: "DeviceLightStates",
                column: "TimeMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAnnouncements");

            migrationBuilder.DropTable(
                name: "DeviceLightStates");
        }
    }
}

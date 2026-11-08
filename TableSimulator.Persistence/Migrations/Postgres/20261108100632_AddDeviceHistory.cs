using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Postgres
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Line = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    StartMs = table.Column<long>(type: "bigint", nullable: false),
                    EndMs = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sounds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLightStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Line = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<int>(type: "integer", nullable: false),
                    TimeMs = table.Column<long>(type: "bigint", nullable: false),
                    Flags = table.Column<int>(type: "integer", nullable: false)
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

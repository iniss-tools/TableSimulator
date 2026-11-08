using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAnnouncementQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Circuit",
                table: "DeviceAnnouncements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Queue",
                table: "DeviceAnnouncements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Circuit",
                table: "DeviceAnnouncements");

            migrationBuilder.DropColumn(
                name: "Queue",
                table: "DeviceAnnouncements");
        }
    }
}

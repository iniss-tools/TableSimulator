using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCheckUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckUpdates",
                table: "ServerSettings",
                type: "boolean",
                nullable: false,
                // existujuce nastavenie: kontrola aktualizacii zapnuta ako pri novom
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckUpdates",
                table: "ServerSettings");
        }
    }
}

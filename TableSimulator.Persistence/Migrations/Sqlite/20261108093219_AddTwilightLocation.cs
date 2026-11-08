using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddTwilightLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ServerSettings",
                type: "REAL",
                nullable: false,
                // existujuce nastavenie: stred Slovenska ako pri novom
                defaultValue: 48.67);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ServerSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 19.70);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ServerSettings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ServerSettings");
        }
    }
}

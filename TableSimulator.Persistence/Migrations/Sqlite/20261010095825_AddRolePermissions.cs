using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetRoles",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "AspNetRoles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "AspNetRoles");
        }
    }
}

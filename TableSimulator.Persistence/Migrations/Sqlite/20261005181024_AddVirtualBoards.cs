using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddVirtualBoards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualBoards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RowCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RowStart = table.Column<int>(type: "INTEGER", nullable: false),
                    Separator = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Template = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Parameter = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualBoards", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualBoards");
        }
    }
}

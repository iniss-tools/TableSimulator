using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddAccessKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessKeys_Hash",
                table: "AccessKeys",
                column: "Hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessKeys");
        }
    }
}

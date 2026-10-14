using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class OAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthClients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RedirectUris = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuthGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ClientName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AccessExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefreshHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviousRefreshHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthGrants_OAuthClients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "OAuthClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthGrants_AccessHash",
                table: "OAuthGrants",
                column: "AccessHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthGrants_ClientId",
                table: "OAuthGrants",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthGrants_PreviousRefreshHash",
                table: "OAuthGrants",
                column: "PreviousRefreshHash");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthGrants_RefreshHash",
                table: "OAuthGrants",
                column: "RefreshHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthGrants");

            migrationBuilder.DropTable(
                name: "OAuthClients");
        }
    }
}

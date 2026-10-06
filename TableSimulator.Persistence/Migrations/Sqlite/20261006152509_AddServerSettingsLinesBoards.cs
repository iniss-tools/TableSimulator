using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableSimulator.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddServerSettingsLinesBoards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lines",
                columns: table => new
                {
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    TableClass = table.Column<int>(type: "INTEGER", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AutoLearn = table.Column<bool>(type: "INTEGER", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Mirror = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lines", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowNetwork = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    FontsFolder = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DesignsFolder = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ConnectHost = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MonitorFolder = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VirtualFolders = table.Column<string>(type: "TEXT", nullable: false),
                    HttpsPort = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpsRedirect = table.Column<bool>(type: "INTEGER", nullable: false),
                    HttpsCertificate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HttpsKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HttpsPassword = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Address = table.Column<int>(type: "INTEGER", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Profile = table.Column<string>(type: "TEXT", nullable: false),
                    Design = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FaultKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FaultDelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    FaultStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    SecondAddress = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => new { x.Line, x.Address });
                    table.ForeignKey(
                        name: "FK_Boards_Lines_Line",
                        column: x => x.Line,
                        principalTable: "Lines",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "Lines");
        }
    }
}

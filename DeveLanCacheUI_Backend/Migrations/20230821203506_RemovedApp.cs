using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemovedApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamDepots_SteamApps_SteamAppId",
                table: "SteamDepots");

            migrationBuilder.DropTable(
                name: "SteamApps");

            migrationBuilder.DropIndex(
                name: "IX_SteamDepots_SteamAppId",
                table: "SteamDepots");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SteamDepots",
                newName: "SteamDepotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SteamDepotId",
                table: "SteamDepots",
                newName: "Id");

            migrationBuilder.CreateTable(
                name: "SteamApps",
                columns: table => new
                {
                    AppId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamApps", x => x.AppId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamDepots_SteamAppId",
                table: "SteamDepots",
                column: "SteamAppId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamDepots_SteamApps_SteamAppId",
                table: "SteamDepots",
                column: "SteamAppId",
                principalTable: "SteamApps",
                principalColumn: "AppId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApps : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamApps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AppName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamApps", x => x.Id);
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
                principalColumn: "Id");
        }
    }
}

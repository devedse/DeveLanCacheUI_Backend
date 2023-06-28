using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "SteamAppDownloadEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SteamAppId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientIp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CacheHitBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CacheMissBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAppDownloadEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamAppDownloadEvents_SteamApps_SteamAppId",
                        column: x => x.SteamAppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamAppDownloadEvents_SteamAppId",
                table: "SteamAppDownloadEvents",
                column: "SteamAppId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamAppDownloadEvents");

            migrationBuilder.DropTable(
                name: "SteamApps");
        }
    }
}

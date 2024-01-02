using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDbSteamManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamManifests",
                columns: table => new
                {
                    DepotId = table.Column<uint>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalCompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TotalUncompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CalculatedCompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CalculatedUncompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamManifests", x => new { x.DepotId, x.CreationTime });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamManifests");
        }
    }
}

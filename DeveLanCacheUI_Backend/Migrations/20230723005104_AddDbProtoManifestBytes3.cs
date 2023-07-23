using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDbProtoManifestBytes3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculatedCompressedSize",
                table: "SteamManifests");

            migrationBuilder.DropColumn(
                name: "CalculatedUncompressedSize",
                table: "SteamManifests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "CalculatedCompressedSize",
                table: "SteamManifests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "CalculatedUncompressedSize",
                table: "SteamManifests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }
    }
}

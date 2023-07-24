using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReworkedManifestStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalProtobufData",
                table: "SteamManifests");

            migrationBuilder.AddColumn<string>(
                name: "UniqueManifestIdentifier",
                table: "SteamManifests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SteamManifests_UniqueManifestIdentifier",
                table: "SteamManifests",
                column: "UniqueManifestIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamManifests_UniqueManifestIdentifier",
                table: "SteamManifests");

            migrationBuilder.DropColumn(
                name: "UniqueManifestIdentifier",
                table: "SteamManifests");

            migrationBuilder.AddColumn<byte[]>(
                name: "OriginalProtobufData",
                table: "SteamManifests",
                type: "BLOB",
                nullable: true);
        }
    }
}

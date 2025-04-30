using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DownloadEvents_CacheIdentifier",
                table: "DownloadEvents",
                column: "CacheIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadEvents_ClientIp",
                table: "DownloadEvents",
                column: "ClientIp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DownloadEvents_CacheIdentifier",
                table: "DownloadEvents");

            migrationBuilder.DropIndex(
                name: "IX_DownloadEvents_ClientIp",
                table: "DownloadEvents");
        }
    }
}

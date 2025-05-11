using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class EpicManifestsAndRewrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpicManifests",
                columns: table => new
                {
                    DownloadIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalCompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TotalUncompressedSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ManifestBytesSize = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpicManifests", x => new { x.DownloadIdentifier, x.CreationTime });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpicManifests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AsyncLogEntryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsyncLogEntryProcessingQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LanCacheLogEntryRaw_CacheIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_RemoteAddress = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_ForwardedFor = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_RemoteUser = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_TimeLocal = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_Request = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_Status = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_BodyBytesSent = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_Referer = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_UserAgent = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_UpstreamCacheStatus = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_Host = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_HttpRange = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_OriginalLogLine = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_ParseException = table.Column<string>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LanCacheLogEntryRaw_BodyBytesSentLong = table.Column<long>(type: "INTEGER", nullable: false),
                    LanCacheLogEntryRaw_DownloadIdentifier = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsyncLogEntryProcessingQueueItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsyncLogEntryProcessingQueueItems");
        }
    }
}

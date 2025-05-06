using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AsyncLogEntryTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_UserAgent",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_UpstreamCacheStatus",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_TimeLocal",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Status",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Request",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_RemoteUser",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_RemoteAddress",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Referer",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_ParseException",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_HttpRange",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Host",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_ForwardedFor",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_BodyBytesSent",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_UserAgent",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_UpstreamCacheStatus",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_TimeLocal",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Status",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Request",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_RemoteUser",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_RemoteAddress",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Referer",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_ParseException",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_HttpRange",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_Host",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_ForwardedFor",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanCacheLogEntryRaw_BodyBytesSent",
                table: "AsyncLogEntryProcessingQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

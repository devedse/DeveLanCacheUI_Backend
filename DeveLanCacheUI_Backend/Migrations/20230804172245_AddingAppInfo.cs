using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddingAppInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots");

            migrationBuilder.AlterColumn<uint>(
                name: "SteamAppId",
                table: "SteamDepots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "Id",
                table: "SteamDepots",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots",
                columns: new[] { "Id", "SteamAppId" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamDepots_SteamApps_SteamAppId",
                table: "SteamDepots");

            migrationBuilder.DropTable(
                name: "SteamApps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots");

            migrationBuilder.DropIndex(
                name: "IX_SteamDepots_SteamAppId",
                table: "SteamDepots");

            migrationBuilder.AlterColumn<int>(
                name: "SteamAppId",
                table: "SteamDepots",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SteamDepots",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots",
                column: "Id");
        }
    }
}

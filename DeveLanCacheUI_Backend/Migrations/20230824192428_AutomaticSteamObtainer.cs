using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveLanCacheUI_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AutomaticSteamObtainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SteamDepots",
                newName: "SteamDepotId");

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
                name: "SteamDepotId",
                table: "SteamDepots",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots",
                columns: new[] { "SteamDepotId", "SteamAppId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamDepots",
                table: "SteamDepots");

            migrationBuilder.RenameColumn(
                name: "SteamDepotId",
                table: "SteamDepots",
                newName: "Id");

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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreLogoInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoImageContentType",
                table: "EventInfos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoImageData",
                table: "EventInfos",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoImageFileName",
                table: "EventInfos",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoImageContentType",
                table: "EventInfos");

            migrationBuilder.DropColumn(
                name: "LogoImageData",
                table: "EventInfos");

            migrationBuilder.DropColumn(
                name: "LogoImageFileName",
                table: "EventInfos");
        }
    }
}

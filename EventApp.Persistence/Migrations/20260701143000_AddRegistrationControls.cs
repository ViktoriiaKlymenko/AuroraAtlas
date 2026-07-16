using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActivityRegistrationClosed",
                table: "EventInfos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRegistrationClosed",
                table: "EventInfos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActivityRegistrationClosed",
                table: "EventInfos");

            migrationBuilder.DropColumn(
                name: "IsSystemRegistrationClosed",
                table: "EventInfos");
        }
    }
}

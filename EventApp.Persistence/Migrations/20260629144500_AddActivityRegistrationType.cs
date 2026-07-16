using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EventAppDbContext))]
    [Migration("20260629144500_AddActivityRegistrationType")]
    public partial class AddActivityRegistrationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresRegistration",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresRegistration",
                table: "Activities");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EventAppDbContext))]
    [Migration("20260629182000_AddActivityDetails")]
    public partial class AddActivityDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "Activities",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Activities"
                SET "Details" = "Description"
                WHERE "Details" = ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                table: "Activities");
        }
    }
}

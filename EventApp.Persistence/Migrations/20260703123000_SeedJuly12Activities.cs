using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJuly12Activities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    admin_id uuid;
                    now_utc timestamptz := now();
                BEGIN
                    SELECT "Id"
                    INTO admin_id
                    FROM "Users"
                    WHERE "Role" = 'Admin'
                    ORDER BY "CreatedAt"
                    LIMIT 1;

                    IF admin_id IS NULL THEN
                        RAISE EXCEPTION 'Cannot seed 2026-07-12 activities because no admin user exists.';
                    END IF;

                    DELETE FROM "Activities"
                    WHERE "Date" = DATE '2026-07-12';

                    INSERT INTO "Activities"
                        ("Id", "Title", "Description", "Details", "Date", "StartTime", "EndTime", "Location", "RequiresRegistration", "MaxParticipants", "IsPainted", "CreatedByAdminId", "CreatedAt", "UpdatedAt")
                    VALUES
                        ('e7ce8db1-4bd4-41b1-947c-72fd41a6d60d', $title$8:00 - 11:00 - Breakfast$title$, '', '', DATE '2026-07-12', TIME '08:00', TIME '11:00', $location$The Quarter Restaurant
                Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('841548c4-6a80-483b-9722-d3fe8c6af7f5', $title$Checkout (until 12:00). 
                Transfer back to Bratislava$title$, '', $details$We are all gathering in the hotel lobby at 12:00. Our transfer will take us directly to the Event Office located at Legionarska 10, Bratislava. Please be on time, ready for departure, and double-check that you haven't left anything behind in your rooms.$details$, DATE '2026-07-12', TIME '12:00', TIME '13:00', $location$Lobby$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc);
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Activities"
                WHERE "Id" IN (
                    'e7ce8db1-4bd4-41b1-947c-72fd41a6d60d',
                    '841548c4-6a80-483b-9722-d3fe8c6af7f5'
                );
                """);
        }
    }
}

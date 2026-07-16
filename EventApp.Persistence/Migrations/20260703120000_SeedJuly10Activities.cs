using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJuly10Activities : Migration
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
                        RAISE EXCEPTION 'Cannot seed 2026-07-10 activities because no admin user exists.';
                    END IF;

                    DELETE FROM "Activities"
                    WHERE "Date" = DATE '2026-07-10';

                    INSERT INTO "Activities"
                        ("Id", "Title", "Description", "Details", "Date", "StartTime", "EndTime", "Location", "RequiresRegistration", "MaxParticipants", "IsPainted", "CreatedByAdminId", "CreatedAt", "UpdatedAt")
                    VALUES
                        ('d52e9036-258f-41db-bcad-bde1e1e636d3', $title$15:00 - 17:30 - Lunch$title$, '', $details$-$details$, DATE '2026-07-10', TIME '15:00', TIME '17:30', $location$The Quarter Restaurant, Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('0c977c8e-e0b1-4cc5-a9b4-8a7e43950f0a', $title$15:00 - 20:00 - SPA $title$, '', $details$SPA, Pools, Water polo, Sauna, Gym$details$, DATE '2026-07-10', TIME '15:00', TIME '20:00', $location$Level 7-8$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('6825bde3-c3af-4de3-82be-8a5886d32cf1', $title$16:30 - 18:00 - Dj Set in SPA$title$, '', '', DATE '2026-07-10', TIME '16:30', TIME '18:00', $location$Swiming pool, 
                Level 8$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('56ad1308-5ba6-4fd2-a849-28239b793970', $title$15:00 - 20:00 - Billiards
                Chess$title$, '', $details$Game on! Billiards and chess are available for anyone.
                Find partners and drop by anytime for a quick match.$details$, DATE '2026-07-10', TIME '15:00', TIME '20:00', $location$Game Lounge, 
                Level 1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('9e05a94f-2980-4f8a-af93-5d27edeb0898', $title$15:00 - 20:00 - Table football, 
                Bowling, 
                Darts, 
                Shuffleboard$title$, '', $details$Game on! Activities are available for anyone.
                Find partners and drop by anytime for a quick match.$details$, DATE '2026-07-10', TIME '15:00', TIME '20:00', $location$Snowbeer Restaurant,
                 Level -1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('edce13a6-242a-4ad6-bc6f-7a8f91723962', $title$15:00 - 20:00 - Sport props for outdoor games$title$, '', $details$Table tennis 
                Badminton 
                Frisbee 
                Beach tennis$details$, DATE '2026-07-10', TIME '15:00', TIME '20:00', $location$Pond lounge zone$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('cbab8d3e-5236-4a37-9c60-9d68741b0d81', $title$20:00 - 23:00 - Dinner$title$, '', $details$-$details$, DATE '2026-07-10', TIME '20:00', TIME '23:00', $location$The Quarter Restaurant & Terrace, Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('b9eb5074-cc76-411a-92db-fd4f9fc43d61', $title$23:00 - 02:00 - Afterparty$title$, '', $details$-$details$, DATE '2026-07-10', TIME '23:00', TIME '02:00', $location$Lobby$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('b8fa70c6-574d-47bc-97bc-8489119564cc', $title$23:00 - 02:00 - Cigar tasting$title$, '', $details$Take a break and step into our interactive relaxation zone. Explore a curated selection of premium cigars from around the world, and chat with our expert moderator to discover unique flavor profiles tailored to your taste.$details$, DATE '2026-07-10', TIME '23:00', TIME '02:00', $location$Cigar Lounge, 
                 Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('7ff68891-bcc4-4753-b61c-75ae0771a940', $title$23:00 - 02:00 - Texas Hold'em,
                Roulette$title$, '', $details$The Fun Casino Tournament
                Play: Texas Holdem & Roulette with professional equipment and dealers. Collect your chips, play freely, and build your fortune.
                Win: The top 3 players with the highest chip count at the end win prizes!$details$, DATE '2026-07-10', TIME '23:00', TIME '02:00', $location$Game Lounge, 
                Level 1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('fb840c65-0179-4563-81c4-e87c85544634', $title$23:00 - 02:00 - Karaoke$title$, '', $details$Lose yourself in the music! Grab your friends, pick your favorite tracks, and own the stage. It's time to sing your heart out and make some unforgettable memories tonight.$details$, DATE '2026-07-10', TIME '23:00', TIME '02:00', $location$Cinema room,  
                 Level -1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc);
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Activities"
                WHERE "Id" IN (
                    'd52e9036-258f-41db-bcad-bde1e1e636d3',
                    '0c977c8e-e0b1-4cc5-a9b4-8a7e43950f0a',
                    '6825bde3-c3af-4de3-82be-8a5886d32cf1',
                    '56ad1308-5ba6-4fd2-a849-28239b793970',
                    '9e05a94f-2980-4f8a-af93-5d27edeb0898',
                    'edce13a6-242a-4ad6-bc6f-7a8f91723962',
                    'cbab8d3e-5236-4a37-9c60-9d68741b0d81',
                    'b9eb5074-cc76-411a-92db-fd4f9fc43d61',
                    'b8fa70c6-574d-47bc-97bc-8489119564cc',
                    '7ff68891-bcc4-4753-b61c-75ae0771a940',
                    'fb840c65-0179-4563-81c4-e87c85544634'
                );
                """);
        }
    }
}

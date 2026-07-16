using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJuly11Activities : Migration
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
                        RAISE EXCEPTION 'Cannot seed 2026-07-11 activities because no admin user exists.';
                    END IF;

                    DELETE FROM "Activities"
                    WHERE "Date" = DATE '2026-07-11';

                    INSERT INTO "Activities"
                        ("Id", "Title", "Description", "Details", "Date", "StartTime", "EndTime", "Location", "RequiresRegistration", "MaxParticipants", "IsPainted", "CreatedByAdminId", "CreatedAt", "UpdatedAt")
                    VALUES
                        ('8c2e6d78-b695-44bb-982c-8f2137cbd289', $title$08:00 - 11:00 - Breakfast$title$, '', '', DATE '2026-07-11', TIME '08:00', TIME '11:00', $location$The Quarter Restaurant,
                Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('7e6e3520-6850-4e9f-a95b-f82aa744e599', $title$09:00 - 10:00 - Yoga$title$, '', $details$Start your day on the terrace with a breathtaking mountain view! Perfect for all levels. We will meet early in the hotel lobby to head up together. Please wear comfortable clothing; yoga mats will be provided on-site.$details$, DATE '2026-07-11', TIME '09:00', TIME '10:00', $location$Executive lounge (Level 2) / Outdoor (Gathering in the lobby)$location$, TRUE, 10000, FALSE, admin_id, now_utc, now_utc),
                        ('f5e64582-3f11-4f4a-9f76-c9e671c31db8', $title$09:00 - 10:00 - Running$title$, '', $details$Kickstart your day with an energizing run through the woods while the camp is still asleep! This isn't a grueling marathon, but a fun, high-vibe session with beautiful nature trails for all levels. We will meet in the lobby to head out together, so grab your friends, register now, and fuel up with fresh air before the festival day kicks off!$details$, DATE '2026-07-11', TIME '09:00', TIME '10:00', $location$Outdoor Forestside (Gathering in the lobby)$location$, TRUE, 10000, FALSE, admin_id, now_utc, now_utc),
                        ('3a074c01-32dc-49d7-91bb-a9ab54a9100c', $title$10:00 - 10:30 - Morning warm-up with DJ$title$, '', $details$Join us on the terrace near the restaurant to warm up to some high-energy dance beats! Let's kick off this festival day actively and break into the fun together. See you there!$details$, DATE '2026-07-11', TIME '10:00', TIME '10:30', $location$The Quarter Restaurant
                 Terrace$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('828bc151-98e9-4cdf-b65c-a15e302f8d37', $title$10:30 - 14:30 - Hiking$title$, '', $details$Join us for an incredible guided hike! Here is the route plan:
                1. Meet at the lobby information desk for a 15-minute walk to the Chopok cable car.
                2. A scenic 20-minute ride up to Chopok peak.
                3. A spectacular 1-hour hike along the mountain ridge toward Polana.
                4. A rewarding 2-hour trek back down through the fresh mountain air.
                Quick Tips:
                - Wear sturdy hiking shoes and bring a light windbreaker (weather on the ridge changes fast).
                - The summer sun is strong, so please pack a hat, sunglasses, and apply SPF.
                - Bring enough water to stay hydrated, some snacks, and a fully charged phone.$details$, DATE '2026-07-11', TIME '10:30', TIME '14:30', $location$Outdoor - Mountain Chopok$location$, TRUE, 40, FALSE, admin_id, now_utc, now_utc),
                        ('5d97563d-d79d-4f57-935b-25f69b1521c8', $title$10:30 - 14:30 - Paddleboarding$title$, '', $details$Paddle out onto the stunning Liptovska Mara lake! Perfect for both beginners and pros, this session offers incredible views right from the water.
                Let's meet at the Lobby Bar for the 25-minute transfer to the lake.
                Quick Tips:
                - Wear a swimsuit or comfy clothes that can get wet, plus flip-flops or water shoes.
                - Bring a towel or a small blanket to chill on the shore, and don't forget your SPF and a hat.
                - Pack a bottle of water to stay hydrated under the sun.$details$, DATE '2026-07-11', TIME '10:30', TIME '14:30', $location$Outdoor - Liptovska Mara Lake$location$, TRUE, 40, FALSE, admin_id, now_utc, now_utc),
                        ('85316a17-369f-4a56-8d55-cef0884501d3', $title$10:30 - 17:30 - Electric bicycles$title$, '', $details$Electric bikes are available right outside the lobby, perfect for an effortless day of sightseeing and fresh air! Just head to the parking area where we have 20 e-bikes ready for you. Sign up, log your rental duration, and feel free to ask our team for the best scenic routes and local recommendations. Then smoothly cruise around for a fantastic day of exploring!$details$, DATE '2026-07-11', TIME '10:30', TIME '17:30', $location$Parking
                Near the main entrance$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('707d7acb-667e-446e-9554-02f05aac5313', $title$10:30 - 19:30 - Sport props for outdoor games$title$, '', $details$Table tennis 
                Badminton 
                Frisbee 
                Beach tennis$details$, DATE '2026-07-11', TIME '10:30', TIME '19:30', $location$Pond lounge zone$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('7208c439-1bb4-40d0-8078-dda861ec85c0', $title$10:30 - 19:30 - Rewards point / info desk$title$, '', $details$Welcome to our Information Desk & Rewards Point! This is your gateway to the grand raffle. Just show our hostess photo proof of any completed zone to get your colorful wristband and a raffle ticket dropped into the drum. Remember, each new zone equals an extra ticket, boosting your chances to win! Keep exploring, snap those photos, and make sure you are physically present at the main stage for the Grand Finale draw to claim your prize!$details$, DATE '2026-07-11', TIME '10:30', TIME '19:30', $location$Lobby$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('6d87c7cf-b4a5-4f56-8804-c020bdc2bf5b', $title$10:30 - 19:30 - Billiards
                Chess$title$, '', $details$The tables are reset on the 1st floor! Whether you want to sharpen your chess strategy or call a rematch at billiards, the game zone is open all day. Drop by whenever you're ready for another round.$details$, DATE '2026-07-11', TIME '10:30', TIME '19:30', $location$Game Lounge 
                Level 1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('ddfe78bb-56dd-4f52-9f91-bc4454443a99', $title$10:30 - 19:30 - Table football, 
                Bowling, 
                Darts, 
                Shuffleboard$title$, '', $details$Everything is set up and ready for you! Grab a drink, bring your friends, and jump into bowling, table football, darts, or shuffleboard. The zone is fully open - just come by and play whenever you like!$details$, DATE '2026-07-11', TIME '10:30', TIME '19:30', $location$Snowbeer Restaurant
                 Level -1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('4d27caa2-e25f-4ce9-9208-3402d1dcdb7c', $title$10:30 - 19:30 - SPA, Pools, Water polo, Sauna, Gym$title$, '', $details$Keep the good vibes going on Day 2! The pools, saunas, and gym are fully open for your relaxation and recovery. Ready for some team action today? Grab the water polo equipment at the pool and start a match with friends!$details$, DATE '2026-07-11', TIME '10:30', TIME '19:30', $location$SPA 
                Level 7-8$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('a05cdf5b-d06a-44b8-8e75-62c255de6f70', $title$10:30 - 14:30 - Festival Glam Lab$title$, '', $details$Ready to level up your festival outfit? Stop by our creative station and design accessories that reflect your personality.
                Create friendship bracelets, flower crowns, decorate your sunglasses, or mix colorful beads and charms into your own unique masterpiece. No experience needed - just imagination and good vibes!$details$, DATE '2026-07-11', TIME '10:30', TIME '14:30', $location$Lobby / Game lounge level 1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('e464902a-0d27-48d2-9fc9-0cb7d04cc03e', $title$10:30 - 11:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '10:30', TIME '11:30', $location$Lobby / Game lounge level 1$location$, TRUE, 14, FALSE, admin_id, now_utc, now_utc),
                        ('fcdaea14-e84a-45ec-a828-51fbf84c13e0', $title$11:30 - 12:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '11:30', TIME '12:30', $location$Lobby / Game lounge level 1$location$, TRUE, 14, FALSE, admin_id, now_utc, now_utc),
                        ('4d2d445b-6cfa-49b4-bc23-d04625a358a6', $title$12:00 - 13:30 - DJ Set in SPA$title$, '', '', DATE '2026-07-11', TIME '12:00', TIME '13:30', $location$Swimming pool, 
                Level 8$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('bd67b5c4-9887-48dd-b467-56285dc54f38', $title$12:30 - 13:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '12:30', TIME '13:30', $location$Lobby / Game lounge level 1$location$, TRUE, 14, FALSE, admin_id, now_utc, now_utc),
                        ('93318588-9eb3-450c-99d4-41b71e27f0cb', $title$13:00 - 16:00 - Lunch$title$, '', '', DATE '2026-07-11', TIME '13:00', TIME '16:00', $location$The Quarter Restaurant
                Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('51af9252-cce6-49a8-9b28-09276b54b8cc', $title$13:30 - 14:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '13:30', TIME '14:30', $location$Lobby / Game lounge level 1$location$, TRUE, 14, FALSE, admin_id, now_utc, now_utc),
                        ('1581ec04-81c3-4163-ac40-813677e1ab01', $title$15:30 - 16:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '15:30', TIME '16:30', $location$Lobby / Game lounge level 1$location$, TRUE, 14, FALSE, admin_id, now_utc, now_utc),
                        ('02ce9645-6c46-45fd-b0ef-44abb3f4ee70', $title$15:30 - 17:30 - Festival Glam Lab$title$, '', $details$Our creative zone keeps rolling! Drop by to customize your festival look - make flower crowns, friendship bracelets, or decorate your sunglasses. Everything is ready, just show up and create!$details$, DATE '2026-07-11', TIME '15:30', TIME '17:30', $location$Lobby / Game lounge level 1$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('9f746ee1-22b7-418d-8d49-80471d52e0c4', $title$16:30 - 17:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '16:30', TIME '17:30', $location$Lobby / Game lounge level 1$location$, TRUE, 15, FALSE, admin_id, now_utc, now_utc),
                        ('ae2c7ba1-8587-47d8-9cd8-1813d7013326', $title$17:30 - 18:30 - Tote Bag Painting Workshop$title$, '', $details$Join our creative workshop and transform a simple eco-friendly tote bag into a unique accessory. Choose from a variety of vibrant designs, transfer them onto fabric, and bring your creation to life with colorful textile paints - no artistic experience required!$details$, DATE '2026-07-11', TIME '17:30', TIME '18:30', $location$Lobby / Game lounge level 1$location$, TRUE, 15, FALSE, admin_id, now_utc, now_utc),
                        ('65069e6e-4166-4cb3-9ecd-68d750d7e11c', $title$17:00 - 20:00 - Beauty Bar$title$, '', $details$Let's add a little sparkle before the party begins!
                Stop by our Beauty Bar for a quick festival makeover and let our professional makeup artist give your look an extra touch of magic. From shimmering glitter to colorful accents and sparkling rhinestones, we'll help you create the perfect gala-dinner style.$details$, DATE '2026-07-11', TIME '17:00', TIME '20:00', $location$Barber room,
                 Ground floor$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('f9906957-2357-4119-91b9-af3a8c8424db', $title$20:00 - 03:00 - Gala Party & Dinner$title$, '', $details$Get ready for an unforgettable Gala Dinner packed with incredible energy and non-stop fun! A truly amazing evening awaits you, featuring a massive dance vibe powered by DJ Prohor and DJ Johnny de City. Dance the night away, enjoy live performances by the REPLAY band, and get a chance to win fantastic prizes in our exciting raffle. Bring your best mood and let's make this a night to remember!$details$, DATE '2026-07-11', TIME '20:00', TIME '03:00', $location$Ballroom$location$, FALSE, 0, FALSE, admin_id, now_utc, now_utc),
                        ('38f3f2c1-cfed-4355-af7a-17d0c72e6be0', $title$00:00 - 03:00 - Karaoke$title$, '', $details$The mic is still hot! Pick your favorite tracks, step into the spotlight, and own the stage once again. Let's keep the energy high, sing your heart out, and make some more unforgettable memories tonight!$details$, DATE '2026-07-11', TIME '00:00', TIME '03:00', $location$Cinema, 
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
                    '8c2e6d78-b695-44bb-982c-8f2137cbd289',
                    '7e6e3520-6850-4e9f-a95b-f82aa744e599',
                    'f5e64582-3f11-4f4a-9f76-c9e671c31db8',
                    '3a074c01-32dc-49d7-91bb-a9ab54a9100c',
                    '828bc151-98e9-4cdf-b65c-a15e302f8d37',
                    '5d97563d-d79d-4f57-935b-25f69b1521c8',
                    '85316a17-369f-4a56-8d55-cef0884501d3',
                    '707d7acb-667e-446e-9554-02f05aac5313',
                    '7208c439-1bb4-40d0-8078-dda861ec85c0',
                    '6d87c7cf-b4a5-4f56-8804-c020bdc2bf5b',
                    'ddfe78bb-56dd-4f52-9f91-bc4454443a99',
                    '4d27caa2-e25f-4ce9-9208-3402d1dcdb7c',
                    'a05cdf5b-d06a-44b8-8e75-62c255de6f70',
                    'e464902a-0d27-48d2-9fc9-0cb7d04cc03e',
                    'fcdaea14-e84a-45ec-a828-51fbf84c13e0',
                    '4d2d445b-6cfa-49b4-bc23-d04625a358a6',
                    'bd67b5c4-9887-48dd-b467-56285dc54f38',
                    '93318588-9eb3-450c-99d4-41b71e27f0cb',
                    '51af9252-cce6-49a8-9b28-09276b54b8cc',
                    '1581ec04-81c3-4163-ac40-813677e1ab01',
                    '02ce9645-6c46-45fd-b0ef-44abb3f4ee70',
                    '9f746ee1-22b7-418d-8d49-80471d52e0c4',
                    'ae2c7ba1-8587-47d8-9cd8-1813d7013326',
                    '65069e6e-4166-4cb3-9ecd-68d750d7e11c',
                    'f9906957-2357-4119-91b9-af3a8c8424db',
                    '38f3f2c1-cfed-4355-af7a-17d0c72e6be0'
                );
                """);
        }
    }
}

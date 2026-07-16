using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetAndSeedFictionalEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- This is intentionally a destructive reset migration. EF's
                -- __EFMigrationsHistory table is not included and is preserved.
                TRUNCATE TABLE
                    "ActivityReminderDeliveries",
                    "WebPushSubscriptions",
                    "ActivityRegistrations",
                    "Activities",
                    "EventInfos",
                    "Users"
                CASCADE;

                INSERT INTO "Users"
                    ("Id", "FullName", "Email", "PasswordHash", "Role", "DirectoryType",
                     "Company", "Position", "Bio", "IsActive", "CreatedAt", "UpdatedAt")
                VALUES
                    ('10000000-0000-4000-8000-000000000001', 'Morgan Vale', 'admin@aurora-atlas.example', NULL, 'Admin', 'Attendee', 'Aurora Atlas Lab', 'Event Administrator', 'Administrator for this fictional test event.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000002', 'Avery Stone', 'avery.stone@example.com', NULL, 'User', 'Speaker', 'Northwind Ideas', 'Futures Researcher', 'Explores imaginary cities and speculative public spaces.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000003', 'Jordan Reed', 'jordan.reed@example.com', NULL, 'User', 'Speaker', 'Paper Kite Studio', 'Creative Director', 'Designs playful experiences for fictional communities.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000004', 'Taylor Brooks', 'taylor.brooks@example.com', NULL, 'User', 'Sponsor', 'Moonbeam Works', 'Partnership Lead', 'Supports experimental gatherings and creative technology.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000005', 'Casey Morgan', 'casey.morgan@example.com', NULL, 'User', 'Sponsor', 'Cloudberry Labs', 'Community Manager', 'Builds welcoming programs for distributed teams.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000006', 'Riley Quinn', 'riley.quinn@example.com', NULL, 'User', 'Exhibitor', 'Lantern Robotics', 'Demo Engineer', 'Creates friendly robots for places that do not exist.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000007', 'Jamie Parker', 'jamie.parker@example.com', NULL, 'User', 'Exhibitor', 'Mosslight Design', 'Product Designer', 'Makes tactile maps and fictional wayfinding systems.', TRUE, now(), now()),
                    ('10000000-0000-4000-8000-000000000008', 'Cameron Blake', 'cameron.blake@example.com', NULL, 'User', 'Attendee', 'Independent', 'Explorer', 'Curious attendee testing the event application.', TRUE, now(), now());

                INSERT INTO "EventInfos"
                    ("Id", "Title", "Description", "StartDate", "EndDate", "Location", "Address",
                     "Contacts", "AdditionalInfo", "BannerImageUrl", "LogoImageUrl",
                     "IsSystemRegistrationClosed", "IsActivityRegistrationClosed",
                     "CreatedAt", "UpdatedAt")
                VALUES
                    ('20000000-0000-4000-8000-000000000001',
                     'Aurora Atlas: Festival of Impossible Places',
                     'A completely fictional three-day test event about mapping cities, landscapes, and meeting places that do not exist.',
                     DATE '2026-10-22', DATE '2026-10-24',
                     'Lumen Harbor Convention Island', '1 Imaginary Quay, Lumen Harbor, ZZ 00000',
                     'hello@aurora-atlas.example | +0 000 000 0000',
                     'TEST DATA ONLY. This event, venue, address, contacts, organizations, and schedule are fictional. Doors open at 08:30 each day.',
                     NULL, NULL, FALSE, FALSE, now(), now());

                INSERT INTO "Activities"
                    ("Id", "Title", "Description", "Details", "Date", "StartTime", "EndTime", "Location",
                     "RequiresRegistration", "MaxParticipants", "IsPainted", "CreatedByAdminId", "CreatedAt", "UpdatedAt")
                VALUES
                    ('30000000-0000-4000-8000-000000000001', 'Opening the Impossible Atlas', 'Welcome and orientation for the fictional festival.', 'Meet the test team and learn how to navigate the imaginary venue.', DATE '2026-10-22', TIME '09:00', TIME '10:00', 'Prism Atrium', FALSE, 0, TRUE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000002', 'Cities Above the Clouds', 'A speculative talk about airborne neighborhoods.', 'Avery presents transit, gardens, and civic spaces designed for a city suspended in the sky.', DATE '2026-10-22', TIME '10:30', TIME '11:30', 'Nimbus Hall', TRUE, 80, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000003', 'Build a Pocket Landscape', 'Hands-on workshop for creating miniature fictional terrain.', 'Materials are supplied; participants create a portable landscape and a short story for it.', DATE '2026-10-22', TIME '13:00', TIME '14:30', 'Mosslight Workshop', TRUE, 24, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000004', 'Lantern Robot Parade', 'Demonstration of friendly wayfinding robots.', 'Robots guide guests through a made-up night market and demonstrate accessible navigation.', DATE '2026-10-22', TIME '16:00', TIME '17:00', 'Comet Courtyard', FALSE, 0, TRUE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000005', 'Breakfast at the Moon Garden', 'Informal networking breakfast.', 'A fictional menu accompanies facilitated introductions for speakers, sponsors, and guests.', DATE '2026-10-23', TIME '08:30', TIME '09:30', 'Moon Garden Terrace', TRUE, 50, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000006', 'Designing Streets for Stories', 'Workshop on narrative-driven public spaces.', 'Teams turn story prompts into maps, signs, landmarks, and small public rituals.', DATE '2026-10-23', TIME '10:00', TIME '12:00', 'Paper Kite Room', TRUE, 32, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000007', 'The Cloudberry Exchange', 'Sponsor showcase and product test session.', 'Explore fictional collaboration tools and leave structured feedback for UI testing.', DATE '2026-10-23', TIME '13:30', TIME '14:30', 'Cloudberry Pavilion', FALSE, 0, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000008', 'Maps Without North', 'Panel discussion on alternative navigation.', 'Speakers compare emotional, social, and sensory maps of invented places.', DATE '2026-10-23', TIME '15:00', TIME '16:15', 'Echo Observatory', TRUE, 90, TRUE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000009', 'Sunrise Ferry to Nowhere', 'A calm closing-day gathering beside the water.', 'No real ferry is involved; this is a fictional test activity at an imaginary pier.', DATE '2026-10-24', TIME '09:00', TIME '10:00', 'Silverwake Pier', TRUE, 40, FALSE, '10000000-0000-4000-8000-000000000001', now(), now()),
                    ('30000000-0000-4000-8000-000000000010', 'Closing: Fold the Map', 'Festival recap and farewell.', 'Share discoveries, review the imaginary atlas, and close the fictional event.', DATE '2026-10-24', TIME '14:00', TIME '15:00', 'Prism Atrium', FALSE, 0, TRUE, '10000000-0000-4000-8000-000000000001', now(), now());

                INSERT INTO "ActivityRegistrations"
                    ("Id", "UserId", "ActivityId", "RegisteredAt")
                VALUES
                    ('40000000-0000-4000-8000-000000000001', '10000000-0000-4000-8000-000000000008', '30000000-0000-4000-8000-000000000002', now()),
                    ('40000000-0000-4000-8000-000000000002', '10000000-0000-4000-8000-000000000008', '30000000-0000-4000-8000-000000000005', now()),
                    ('40000000-0000-4000-8000-000000000003', '10000000-0000-4000-8000-000000000003', '30000000-0000-4000-8000-000000000006', now()),
                    ('40000000-0000-4000-8000-000000000004', '10000000-0000-4000-8000-000000000007', '30000000-0000-4000-8000-000000000008', now());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The data that Up() removes cannot be reconstructed. Down() only
            // removes the fictional dataset created by this migration.
            migrationBuilder.Sql("""
                TRUNCATE TABLE
                    "ActivityReminderDeliveries",
                    "WebPushSubscriptions",
                    "ActivityRegistrations",
                    "Activities",
                    "EventInfos",
                    "Users"
                CASCADE;
                """);
        }
    }
}

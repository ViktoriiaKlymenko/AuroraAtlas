using EventApp.Application;
using EventApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventApp.Persistence;

public static class DatabaseSeeder
{
    private static readonly DateOnly EventStart = new(2026, 9, 14);
    private static readonly DateOnly EventEnd = new(2026, 9, 16);

    public static async Task SeedAsync(EventAppDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var adminEmail = NormalizeEmail(configuration["SeedData:AdminEmail"] ?? "valeriia.shocotco@example.com");
        var adminPassword = configuration["SeedData:AdminPassword"] ?? "Valeriia2026!";
        var attendeeEmail = NormalizeEmail(configuration["SeedData:DefaultUserEmail"] ?? "attendee@example.com");
        var attendeePassword = configuration["SeedData:DefaultUserPassword"] ?? "Attendee2026!";

        var admin = await UpsertUserAsync(
            db,
            adminEmail,
            "Valeriia Shocotco",
            UserRole.Admin,
            DirectoryType.Attendee,
            "Event Operations",
            "Administrator",
            "Event administrator responsible for schedule, participant directory, and event information.",
            adminPassword,
            now,
            cancellationToken);

        var attendee = await UpsertUserAsync(
            db,
            attendeeEmail,
            "Default Attendee",
            null,
            DirectoryType.Attendee,
            "Independent",
            "Participant",
            "Default attendee account for local testing.",
            attendeePassword,
            now,
            cancellationToken);

        if (!await db.EventInfos.AnyAsync(cancellationToken))
        {
            db.EventInfos.Add(new EventInfo
            {
                Title = "Summer Fest",
                Description = "A three-day forum for organizers, speakers, sponsors, exhibitors, and attendees focused on practical event operations and attendee experience.",
                StartDate = EventStart,
                EndDate = EventEnd,
                Location = "Grand Conference Center",
                Address = "14 Central Avenue, Kyiv",
                Contacts = "info@international-event-forum.example | +380 44 000 00 00",
                AdditionalInfo = "Registration desk opens at 08:00. Please bring an ID and arrive 10 minutes before each registered activity.",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!await db.Activities.AnyAsync(cancellationToken))
        {
            db.Activities.AddRange(
                new Activity
                {
                    Title = "Opening Keynote: Designing Better Event Experiences",
                    Description = "A practical keynote on building clear attendee journeys, reducing friction, and keeping event information current.",
                    Details = "Join the opening keynote for a deeper look at practical event experience design, from first-touch attendee communication through live operations and post-event follow-up. The session focuses on clear information architecture, schedule readiness, and small operational choices that make a large event easier to navigate.",
                    Date = EventStart,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                    Location = "Main Hall",
                    MaxParticipants = 120,
                    CreatedByAdminId = admin.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Activity
                {
                    Title = "Speaker Roundtable",
                    Description = "Speakers discuss preparation, audience engagement, and post-session follow-up.",
                    Details = "A moderated roundtable for speakers and content teams covering session preparation, live audience engagement, Q&A handling, and meaningful follow-up after the event. Attendees can expect practical examples and time for questions.",
                    Date = EventStart,
                    StartTime = new TimeOnly(10, 30),
                    EndTime = new TimeOnly(11, 45),
                    Location = "Room A",
                    MaxParticipants = 40,
                    CreatedByAdminId = admin.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Activity
                {
                    Title = "Sponsor Showcase",
                    Description = "Sponsors present services and partnership opportunities for attendees and organizers.",
                    Details = "Sponsors will introduce their services, partnership opportunities, and event technology offerings in a structured showcase. This session is designed for attendees, organizers, and partner teams looking for relevant contacts and practical vendor insight.",
                    Date = EventStart.AddDays(1),
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(14, 30),
                    Location = "Expo Zone",
                    MaxParticipants = 75,
                    CreatedByAdminId = admin.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Activity
                {
                    Title = "Exhibitor Product Walkthroughs",
                    Description = "Guided sessions with exhibitors demonstrating tools and services in small groups.",
                    Details = "Small-group walkthroughs give exhibitors time to demonstrate products, answer detailed questions, and connect attendees with the right follow-up contacts. The format is guided but informal, with room to move between demonstrations.",
                    Date = EventStart.AddDays(1),
                    StartTime = new TimeOnly(15, 0),
                    EndTime = new TimeOnly(16, 30),
                    Location = "Demo Theater",
                    MaxParticipants = 50,
                    CreatedByAdminId = admin.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Activity
                {
                    Title = "Closing Networking Session",
                    Description = "A final moderated networking session for speakers, sponsors, exhibitors, and attendees.",
                    Details = "Close the forum with a moderated networking session built around introductions, key takeaways, and follow-up opportunities. The session is open to speakers, sponsors, exhibitors, and attendees who want to make final connections before departure.",
                    Date = EventEnd,
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(12, 30),
                    Location = "Lounge",
                    MaxParticipants = 100,
                    CreatedByAdminId = admin.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                });
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!await db.ActivityRegistrations.AnyAsync(r => r.UserId == attendee.Id, cancellationToken))
        {
            var firstActivity = await db.Activities.OrderBy(a => a.Date).ThenBy(a => a.StartTime).FirstAsync(cancellationToken);
            db.ActivityRegistrations.Add(new ActivityRegistration
            {
                UserId = attendee.Id,
                ActivityId = firstActivity.Id,
                RegisteredAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> UpsertUserAsync(
        EventAppDbContext db,
        string email,
        string fullName,
        UserRole? role,
        DirectoryType directoryType,
        string company,
        string position,
        string bio,
        string? password,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        email = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                CreatedAt = now
            };
            db.Users.Add(user);
        }

        var changed = user.FullName != fullName ||
            user.DirectoryType != directoryType ||
            user.Company != company ||
            user.Position != position ||
            user.Bio != bio ||
            !user.IsActive;

        if (changed)
        {
            user.FullName = fullName;
            user.DirectoryType = directoryType;
            user.Company = company;
            user.Position = position;
            user.Bio = bio;
            user.IsActive = true;
        }

        if (role.HasValue && user.Role != role.Value)
        {
            user.Role = role.Value;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (user.PasswordHash is null || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.Hash(password);
                changed = true;
            }
        }

        if (changed || user.CreatedAt == now)
        {
            user.UpdatedAt = now;
        }

        return user;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static Task<User> UpsertDirectoryUserAsync(
        EventAppDbContext db,
        string email,
        string fullName,
        DirectoryType directoryType,
        string company,
        string position,
        string bio,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        UpsertUserAsync(db, email, fullName, null, directoryType, company, position, bio, string.Empty, now, cancellationToken);
}

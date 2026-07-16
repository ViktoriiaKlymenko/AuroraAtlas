using System.Text.Json;
using EventApp.Application;
using EventApp.Domain;
using EventApp.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPush;

namespace EventApp.Api;

public sealed class PushNotificationOptions
{
    public string? Subject { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }
    public string EventTimeZoneId { get; set; } = "Europe/Kyiv";
    public int ReminderOffsetMinutes { get; set; } = 30;
    public int PollIntervalSeconds { get; set; } = 60;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Subject) &&
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !string.IsNullOrWhiteSpace(PrivateKey);
}

public sealed class ActivityReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PushNotificationOptions> options,
    ILogger<ActivityReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Event reminder worker failed.");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(15, options.Value.PollIntervalSeconds));
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        var pushOptions = options.Value;
        if (!pushOptions.IsConfigured)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventAppDbContext>();
        var eventTimeZone = EventTimeZone.Resolve(pushOptions.EventTimeZoneId);
        var now = DateTimeOffset.UtcNow;
        var reminderOffset = TimeSpan.FromMinutes(Math.Max(1, pushOptions.ReminderOffsetMinutes));
        var lookAheadDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now.Add(reminderOffset).AddMinutes(5), eventTimeZone).DateTime);
        var lookBackDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now.AddDays(-1), eventTimeZone).DateTime);

        var registrations = await db.ActivityRegistrations
            .Include(r => r.Activity)
            .Include(r => r.User)
                .ThenInclude(u => u!.WebPushSubscriptions)
            .Include(r => r.ReminderDeliveries)
            .Where(r => r.Activity != null &&
                r.User != null &&
                r.User.IsActive &&
                r.Activity.Date >= lookBackDate &&
                r.Activity.Date <= lookAheadDate)
            .ToListAsync(cancellationToken);

        var webPushClient = new WebPushClient();
        var vapidDetails = new VapidDetails(pushOptions.Subject, pushOptions.PublicKey, pushOptions.PrivateKey);

        foreach (var registration in registrations)
        {
            var activity = registration.Activity!;
            var startsAt = EventTimeZone.ToUtc(activity.Date, activity.StartTime, eventTimeZone);
            var reminderAt = startsAt - reminderOffset;
            if (now < reminderAt || now >= startsAt)
            {
                continue;
            }

            foreach (var savedSubscription in registration.User!.WebPushSubscriptions.ToList())
            {
                if (registration.ReminderDeliveries.Any(d => d.WebPushSubscriptionId == savedSubscription.Id))
                {
                    continue;
                }

                var payload = JsonSerializer.Serialize(new
                {
                    title = $"{activity.Title} starts in {Math.Max(1, (int)Math.Ceiling((startsAt - now).TotalMinutes))} minutes",
                    body = $"{activity.Date:MMM d, yyyy} {activity.StartTime:HH\\:mm} at {activity.Location}",
                    url = "/#schedule",
                    tag = $"eventapp-reminder-{activity.Id}"
                });

                try
                {
                    var subscription = new PushSubscription(savedSubscription.Endpoint, savedSubscription.P256dh, savedSubscription.Auth);
                    await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken);
                    db.ActivityReminderDeliveries.Add(new ActivityReminderDelivery
                    {
                        ActivityRegistrationId = registration.Id,
                        WebPushSubscriptionId = savedSubscription.Id,
                        ReminderAt = reminderAt
                    });
                }
                catch (WebPushException exception) when ((int?)exception.StatusCode is 404 or 410)
                {
                    db.WebPushSubscriptions.Remove(savedSubscription);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to send event reminder push notification.");
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public static class EventTimeZone
{
    public static TimeZoneInfo Resolve(string configuredId)
    {
        foreach (var id in new[] { configuredId, "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time", "UTC" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var localTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = timeZone.GetUtcOffset(localTime);
        return new DateTimeOffset(localTime, offset).ToUniversalTime();
    }
}

namespace EventApp.Domain;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public enum DirectoryType
{
    Speaker = 0,
    Sponsor = 1,
    Exhibitor = 2,
    Attendee = 3
}

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class User : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTimeOffset? PasswordResetCodeExpiresAt { get; set; }
    public string? AvatarUrl { get; set; }
    public byte[]? AvatarImageData { get; set; }
    public string? AvatarImageContentType { get; set; }
    public string? AvatarImageFileName { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DirectoryType DirectoryType { get; set; } = DirectoryType.Attendee;
    public string? Company { get; set; }
    public string? Position { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ActivityRegistration> Registrations { get; set; } = [];
    public ICollection<WebPushSubscription> WebPushSubscriptions { get; set; } = [];
}

public sealed class Activity : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool RequiresRegistration { get; set; } = true;
    public int MaxParticipants { get; set; }
    public bool IsPainted { get; set; }
    public Guid CreatedByAdminId { get; set; }
    public User? CreatedByAdmin { get; set; }
    public ICollection<ActivityRegistration> Registrations { get; set; } = [];

    public DateTimeOffset StartsAt => new(Date.ToDateTime(StartTime), TimeSpan.Zero);
    public DateTimeOffset EndsAt => new(Date.ToDateTime(EndTime), TimeSpan.Zero);
}

public sealed class ActivityRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ActivityReminderDelivery> ReminderDeliveries { get; set; } = [];
}

public sealed class WebPushSubscription : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ActivityReminderDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityRegistrationId { get; set; }
    public ActivityRegistration? ActivityRegistration { get; set; }
    public Guid WebPushSubscriptionId { get; set; }
    public WebPushSubscription? WebPushSubscription { get; set; }
    public DateTimeOffset ReminderAt { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EventInfo : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Contacts { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public string? LogoImageUrl { get; set; }
    public bool IsSystemRegistrationClosed { get; set; }
    public bool IsActivityRegistrationClosed { get; set; }
    public byte[]? LogoImageData { get; set; }
    public string? LogoImageContentType { get; set; }
    public string? LogoImageFileName { get; set; }
}

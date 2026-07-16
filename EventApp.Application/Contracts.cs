using System.Text.Json.Serialization;
using EventApp.Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventApp.Application;

public sealed record ActivityDto(
    Guid Id,
    string Title,
    string Description,
    string Details,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Location,
    bool RequiresRegistration,
    int MaxParticipants,
    int RegisteredParticipants,
    bool IsRegistered,
    bool IsFull,
    bool IsPainted,
    DateTimeOffset UpdatedAt);

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    UserRole Role,
    DirectoryType DirectoryType,
    string? Company,
    string? Position,
    string? Bio,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public sealed record EventInfoDto(
    Guid Id,
    string Title,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Location,
    string Address,
    string Contacts,
    string AdditionalInfo,
    string? BannerImageUrl,
    string? LogoImageUrl,
    bool IsSystemRegistrationClosed,
    bool IsActivityRegistrationClosed,
    DateTimeOffset UpdatedAt);

public sealed record EventLogoDto(
    byte[] Data,
    string ContentType,
    string FileName,
    DateTimeOffset UpdatedAt);

public sealed record UserAvatarDto(
    byte[] Data,
    string ContentType,
    string FileName,
    DateTimeOffset UpdatedAt);

public sealed record UserDetailsDto(
    UserDto User,
    IReadOnlyList<ActivityDto> RegisteredActivities);

public sealed record UpsertActivityRequest(
    string Title,
    string Description,
    string Details,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Location,
    int MaxParticipants,
    bool? RequiresRegistration);

public sealed record UpdateProfileRequest(string FullName, string? AvatarUrl, string? Company, string? Position, string? Bio);
public sealed record UpdateUserAdminRequest(UserRole? Role, DirectoryType? DirectoryType, bool? IsActive);
public sealed record SignInRequest(string Provider, string IdToken, string? Password, string? FullName);
public sealed record SignUpRequest(string FullName, string Email, string Password);
public sealed record PasswordResetRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
public sealed record PushSubscriptionRequest(string Endpoint, PushSubscriptionKeys Keys);
public sealed record PushSubscriptionKeys([property: JsonPropertyName("p256dh")] string P256dh, string Auth);

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string FullName { get; }
    bool IsAdmin { get; }
}

public interface ITokenValidator
{
    Task<ExternalUser> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken);
}

public sealed record ExternalUser(string Email, string FullName, string? AvatarUrl);

public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken);
}

public interface IApplicationDbContext
{
    IQueryable<Activity> Activities { get; }
    IQueryable<ActivityRegistration> ActivityRegistrations { get; }
    IQueryable<User> Users { get; }
    IQueryable<EventInfo> EventInfos { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<IDbContextTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

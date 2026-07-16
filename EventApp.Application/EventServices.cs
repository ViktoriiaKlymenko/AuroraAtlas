using EventApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventApp.Application;

public sealed class EventService(IApplicationDbContext db)
{
    private const int RegistrationRetryLimit = 3;

    public async Task<IReadOnlyList<ActivityDto>> GetScheduleAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        return await db.Activities
            .AsNoTracking()
            .Include(a => a.Registrations)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.EndTime)
            .Select(a => ToDto(a, currentUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityDto?> GetActivityAsync(Guid activityId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await db.Activities
            .AsNoTracking()
            .Include(a => a.Registrations)
            .Where(a => a.Id == activityId)
            .Select(a => ToDto(a, currentUserId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityDto>> GetRegisteredActivitiesAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await db.Activities
            .AsNoTracking()
            .Include(a => a.Registrations)
            .Where(a => a.RequiresRegistration && a.Registrations.Any(r => r.UserId == userId))
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.EndTime)
            .Select(a => ToDto(a, currentUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityDto> CreateActivityAsync(Guid adminId, UpsertActivityRequest request, CancellationToken cancellationToken)
    {
        ValidateActivity(request);

        var activity = new Activity
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Details = request.Details?.Trim() ?? string.Empty,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location.Trim(),
            RequiresRegistration = request.RequiresRegistration ?? true,
            MaxParticipants = request.MaxParticipants,
            CreatedByAdminId = adminId
        };

        db.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(activity, adminId);
    }

    public async Task<ActivityDto?> UpdateActivityAsync(Guid activityId, UpsertActivityRequest request, CancellationToken cancellationToken)
    {
        ValidateActivity(request);

        var activity = await db.Activities
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return null;
        }

        activity.Title = request.Title.Trim();
        activity.Description = request.Description.Trim();
        activity.Details = request.Details?.Trim() ?? string.Empty;
        activity.Date = request.Date;
        activity.StartTime = request.StartTime;
        activity.EndTime = request.EndTime;
        activity.Location = request.Location.Trim();
        activity.RequiresRegistration = request.RequiresRegistration ?? true;
        activity.MaxParticipants = request.MaxParticipants;
        if (!activity.RequiresRegistration)
        {
            foreach (var registration in activity.Registrations.ToList())
            {
                db.Remove(registration);
            }
        }

        activity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(activity, Guid.Empty);
    }

    public async Task<bool> DeleteActivityAsync(Guid activityId, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null)
        {
            return false;
        }

        db.Remove(activity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ActivityDto?> PaintActivityAsync(Guid activityId, CancellationToken cancellationToken)
    {
        var activity = await db.Activities
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return null;
        }

        activity.IsPainted = true;
        activity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(activity, Guid.Empty);
    }

    public async Task<RegistrationResult> RegisterAsync(Guid userId, Guid activityId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= RegistrationRetryLimit; attempt++)
        {
            try
            {
                return await TryRegisterAsync(userId, activityId, cancellationToken);
            }
            catch (Exception exception) when (IsRetryableRegistrationConflict(exception) && attempt < RegistrationRetryLimit)
            {
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
            catch (Exception exception) when (IsRetryableRegistrationConflict(exception))
            {
                return RegistrationResult.RetryLater;
            }
        }

        return RegistrationResult.RetryLater;
    }

    private async Task<RegistrationResult> TryRegisterAsync(Guid userId, Guid activityId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginSerializableTransactionAsync(cancellationToken);
        var activity = await db.Activities
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return RegistrationResult.NotFound;
        }

        if (await db.EventInfos.AsNoTracking().AnyAsync(i => i.IsActivityRegistrationClosed, cancellationToken))
        {
            return RegistrationResult.Closed;
        }

        if (!activity.RequiresRegistration)
        {
            return RegistrationResult.NotRegistrable;
        }

        if (activity.Registrations.Any(r => r.UserId == userId))
        {
            return RegistrationResult.AlreadyRegistered;
        }

        if (activity.Registrations.Count >= activity.MaxParticipants)
        {
            return RegistrationResult.Full;
        }

        var overlapDateStart = activity.Date.AddDays(-1);
        var overlapDateEnd = activity.Date.AddDays(1);
        var registeredActivities = await db.ActivityRegistrations
            .Include(r => r.Activity)
            .Where(r =>
                r.UserId == userId &&
                r.Activity != null &&
                r.Activity.Date >= overlapDateStart &&
                r.Activity.Date <= overlapDateEnd)
            .Select(r => r.Activity!)
            .ToListAsync(cancellationToken);

        var hasOverlap = registeredActivities.Any(registeredActivity => ActivitiesOverlap(activity, registeredActivity));

        if (hasOverlap)
        {
            return RegistrationResult.Overlaps;
        }

        db.Add(new ActivityRegistration { UserId = userId, ActivityId = activityId });
        activity.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return RegistrationResult.Registered;
    }

    private static bool ActivitiesOverlap(Activity first, Activity second)
    {
        var firstInterval = GetActivityInterval(first.Date, first.StartTime, first.EndTime);
        var secondInterval = GetActivityInterval(second.Date, second.StartTime, second.EndTime);
        return firstInterval.Start < secondInterval.End && secondInterval.Start < firstInterval.End;
    }

    private static (DateTime Start, DateTime End) GetActivityInterval(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var start = date.ToDateTime(startTime, DateTimeKind.Unspecified);
        var endDate = endTime > startTime ? date : date.AddDays(1);
        var end = endDate.ToDateTime(endTime, DateTimeKind.Unspecified);
        return (start, end);
    }

    private static TimeSpan GetRetryDelay(int attempt) =>
        TimeSpan.FromMilliseconds((attempt * 40) + Random.Shared.Next(10, 35));

    private static bool IsRetryableRegistrationConflict(Exception exception)
    {
        var sqlState = GetSqlState(exception);
        return sqlState is "40001" or "40P01" or "23505";
    }

    private static string? GetSqlState(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (!string.IsNullOrWhiteSpace(sqlState))
            {
                return sqlState;
            }
        }

        return null;
    }

    public async Task<bool> CancelRegistrationAsync(Guid userId, Guid activityId, CancellationToken cancellationToken)
    {
        var registration = await db.ActivityRegistrations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ActivityId == activityId, cancellationToken);

        if (registration is null)
        {
            return false;
        }

        db.Remove(registration);
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is not null)
        {
            activity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<UserDto>> GetParticipantsAsync(Guid activityId, CancellationToken cancellationToken)
    {
        return await db.ActivityRegistrations
            .AsNoTracking()
            .Where(r => r.ActivityId == activityId && r.User != null)
            .Select(r => r.User!)
            .OrderBy(u => u.FullName)
            .Select(u => ToDto(u))
            .ToListAsync(cancellationToken);
    }

    private static ActivityDto ToDto(Activity activity, Guid currentUserId)
    {
        var registered = activity.RequiresRegistration ? activity.Registrations.Count : 0;
        return new ActivityDto(
            activity.Id,
            activity.Title,
            activity.Description,
            activity.Details,
            activity.Date,
            activity.StartTime,
            activity.EndTime,
            activity.Location,
            activity.RequiresRegistration,
            activity.MaxParticipants,
            registered,
            activity.RequiresRegistration && activity.Registrations.Any(r => r.UserId == currentUserId),
            activity.RequiresRegistration && registered >= activity.MaxParticipants,
            activity.IsPainted,
            activity.UpdatedAt);
    }

    private static void ValidateActivity(UpsertActivityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Activity title is required.");
        }

        if (request.EndTime == request.StartTime)
        {
            throw new ArgumentException("Activity end time must be different from start time.");
        }

        if ((request.RequiresRegistration ?? true) && request.MaxParticipants < 1)
        {
            throw new ArgumentException("Participant limit must be at least 1.");
        }
    }
    private static UserDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.AvatarUrl,
        user.Role,
        user.DirectoryType,
        user.Company,
        user.Position,
        user.Bio,
        user.IsActive,
        user.UpdatedAt);
}

public enum RegistrationResult
{
    Registered,
    AlreadyRegistered,
    Full,
    Overlaps,
    NotRegistrable,
    RetryLater,
    Closed,
    NotFound
}

public sealed class DirectoryService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(DirectoryType? directoryType, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        if (directoryType is not null)
        {
            query = query.Where(u => u.DirectoryType == directoryType);
        }

        return await query.OrderBy(u => u.FullName).Select(u => ToDto(u)).ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => ToDto(u)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserAvatarDto?> GetAvatarAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.AvatarImageData is null ||
            string.IsNullOrWhiteSpace(user.AvatarImageContentType) ||
            string.IsNullOrWhiteSpace(user.AvatarImageFileName))
        {
            return null;
        }

        return new UserAvatarDto(
            user.AvatarImageData,
            user.AvatarImageContentType,
            user.AvatarImageFileName,
            user.UpdatedAt);
    }

    public async Task<UserDetailsDto?> GetUserDetailsAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => ToDto(u)).FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var activities = await db.ActivityRegistrations
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Activity != null)
            .Select(r => r.Activity!)
            .Where(a => a.RequiresRegistration)
            .Include(a => a.Registrations)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.EndTime)
            .Select(a => new ActivityDto(
                a.Id,
                a.Title,
                a.Description,
                a.Details,
                a.Date,
                a.StartTime,
                a.EndTime,
                a.Location,
                a.RequiresRegistration,
                a.MaxParticipants,
                a.Registrations.Count,
                a.Registrations.Any(r => r.UserId == currentUserId),
                a.RequiresRegistration && a.Registrations.Count >= a.MaxParticipants,
                a.IsPainted,
                a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new UserDetailsDto(user, activities);
    }

    public async Task<UserDto?> UpdateCurrentUserAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.Company = request.Company;
        user.Position = request.Position;
        user.Bio = request.Bio;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateCurrentUserAvatarAsync(
        Guid userId,
        string avatarUrl,
        byte[] avatarImageData,
        string avatarImageContentType,
        string avatarImageFileName,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.AvatarUrl = avatarUrl;
        user.AvatarImageData = avatarImageData;
        user.AvatarImageContentType = avatarImageContentType;
        user.AvatarImageFileName = avatarImageFileName;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserAdminRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        if (request.DirectoryType.HasValue)
        {
            user.DirectoryType = request.DirectoryType.Value;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (userId == currentUserId)
        {
            throw new ArgumentException("You cannot delete your own account while signed in.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var createdActivities = await db.Activities
            .Where(a => a.CreatedByAdminId == userId)
            .ToListAsync(cancellationToken);
        foreach (var activity in createdActivities)
        {
            activity.CreatedByAdminId = currentUserId;
            activity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        db.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.AvatarUrl,
        user.Role,
        user.DirectoryType,
        user.Company,
        user.Position,
        user.Bio,
        user.IsActive,
        user.UpdatedAt);
}

public sealed class InfoService(IApplicationDbContext db)
{
    public async Task<EventInfoDto> GetAsync(CancellationToken cancellationToken)
    {
        var info = await GetOrCreateAsync(cancellationToken);
        return ToDto(info);
    }

    public async Task<EventLogoDto?> GetLogoAsync(CancellationToken cancellationToken)
    {
        var info = await db.EventInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (info?.LogoImageData is null ||
            string.IsNullOrWhiteSpace(info.LogoImageContentType) ||
            string.IsNullOrWhiteSpace(info.LogoImageFileName))
        {
            return null;
        }

        return new EventLogoDto(
            info.LogoImageData,
            info.LogoImageContentType,
            info.LogoImageFileName,
            info.UpdatedAt);
    }

    public async Task<EventInfoDto> UpdateAsync(EventInfoDto request, CancellationToken cancellationToken)
    {
        var info = await GetOrCreateAsync(cancellationToken);
        info.Title = request.Title.Trim();
        info.Description = request.Description.Trim();
        info.StartDate = request.StartDate;
        info.EndDate = request.EndDate;
        info.Location = request.Location.Trim();
        info.Address = request.Address.Trim();
        info.Contacts = request.Contacts.Trim();
        info.AdditionalInfo = request.AdditionalInfo.Trim();
        info.IsSystemRegistrationClosed = request.IsSystemRegistrationClosed;
        info.IsActivityRegistrationClosed = request.IsActivityRegistrationClosed;
        info.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(info);
    }

    public async Task<EventInfoDto> UpdateLogoAsync(
        string? logoImageUrl,
        byte[]? logoImageData,
        string? logoImageContentType,
        string? logoImageFileName,
        CancellationToken cancellationToken)
    {
        var info = await GetOrCreateAsync(cancellationToken);
        info.LogoImageUrl = logoImageUrl;
        info.LogoImageData = logoImageData;
        info.LogoImageContentType = logoImageContentType;
        info.LogoImageFileName = logoImageFileName;
        info.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(info);
    }

    private async Task<EventInfo> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var info = await db.EventInfos.FirstOrDefaultAsync(cancellationToken);
        if (info is not null)
        {
            return info;
        }

        info = new EventInfo
        {
            Title = "Summer Fest",
            Description = "Welcome to Summer Fest.",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Location = "Venue",
            Address = "Address",
            Contacts = "Contact details",
            AdditionalInfo = "Additional instructions"
        };
        db.Add(info);
        await db.SaveChangesAsync(cancellationToken);
        return info;
    }

    private static EventInfoDto ToDto(EventInfo info) => new(
        info.Id,
        info.Title,
        info.Description,
        info.StartDate,
        info.EndDate,
        info.Location,
        info.Address,
        info.Contacts,
        info.AdditionalInfo,
        info.BannerImageUrl,
        info.LogoImageUrl,
        info.IsSystemRegistrationClosed,
        info.IsActivityRegistrationClosed,
        info.UpdatedAt);
}

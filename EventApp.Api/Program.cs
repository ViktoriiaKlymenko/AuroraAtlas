using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventApp.Api;
using EventApp.Application;
using EventApp.Domain;
using EventApp.Infrastructure;
using EventApp.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

ValidateProductionConfiguration(builder.Configuration, builder.Environment);

builder.Services.AddOpenApi();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<DirectoryService>();
builder.Services.AddScoped<InfoService>();
builder.Services.AddSingleton<TokenIssuer>();
builder.Services.Configure<PushNotificationOptions>(builder.Configuration.GetSection("PushNotifications"));
builder.Services.AddHostedService<ActivityReminderWorker>();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? "local-development-signing-key-change-before-production";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventApp.Api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EventApp.Web",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
});

var app = builder.Build();
const string DuplicateUserMessage = "This user already exists";

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventAppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (statusCode, title) = exception switch
        {
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message),
            UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, unauthorizedAccessException.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { title });
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var fileName = context.File.Name;
        if (string.Equals(fileName, "index.html", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "service-worker.js", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/", (HttpContext httpContext, IWebHostEnvironment environment) =>
{
    var indexPath = Path.Combine(environment.WebRootPath ?? "wwwroot", "index.html");
    if (!File.Exists(indexPath))
    {
        return Results.Ok(new { status = "ok", service = "EventApp.Api" });
    }

    httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    httpContext.Response.Headers.Expires = "0";
    return Results.File(indexPath, "text/html");
});

var auth = app.MapGroup("/api/auth");
auth.MapPost("/signin", async (
    SignInRequest request,
    TokenIssuer tokenIssuer,
    IConfiguration configuration,
    EventAppDbContext db,
    CancellationToken cancellationToken) =>
{
    var email = (request.IdToken ?? string.Empty).Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest(new { message = "Email is required." });
    }

    if (!IsApplicationPasswordValid(configuration, request.Password))
    {
        return Results.BadRequest(new { message = "Application password is incorrect." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
    if (user is null)
    {
        return Results.NotFound(new { message = "Registered user with this email was not found." });
    }

    return Results.Ok(new { accessToken = tokenIssuer.Create(user), user = ToUserDto(user) });
});

auth.MapPost("/signup", async (
    SignUpRequest request,
    TokenIssuer tokenIssuer,
    IConfiguration configuration,
    EventAppDbContext db,
    CancellationToken cancellationToken) =>
{
    var fullName = (request.FullName ?? string.Empty).Trim();
    var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

    if (await db.EventInfos.AsNoTracking().AnyAsync(i => i.IsSystemRegistrationClosed, cancellationToken))
    {
        return Results.Conflict(new { message = "Registration to the system is closed." });
    }

    if (string.IsNullOrWhiteSpace(fullName))
    {
        return Results.BadRequest(new { message = "Full name is required." });
    }

    if (!IsFullNameValid(fullName))
    {
        return Results.BadRequest(new { message = "Full name must contain two words, each starting with a capital letter and at least 2 letters long." });
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest(new { message = "Email is required." });
    }

    if (!IsEmailValid(email))
    {
        return Results.BadRequest(new { message = "Enter a valid email address." });
    }

    if (!IsApplicationPasswordValid(configuration, request.Password))
    {
        return Results.BadRequest(new { message = "Application password is incorrect." });
    }

    if (await UserExistsAsync(db, email, cancellationToken))
    {
        return Results.Conflict(new { message = DuplicateUserMessage });
    }

    var user = new User
    {
        FullName = fullName,
        Email = email,
        Role = UserRole.User
    };

    db.Users.Add(user);
    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
    {
        return Results.Conflict(new { message = DuplicateUserMessage });
    }

    return Results.Ok(new { accessToken = tokenIssuer.Create(user), user = ToUserDto(user) });
});

auth.MapPost("/password-reset/request", async (
    PasswordResetRequest request,
    EventAppDbContext db,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest(new { message = "Email is required." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash))
    {
        return Results.NotFound(new { message = "Registered user with this email was not found." });
    }

    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    user.PasswordResetCodeHash = PasswordHasher.Hash(code);
    user.PasswordResetCodeExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
    user.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);

    await emailSender.SendPasswordResetCodeAsync(user.Email, code, cancellationToken);
    return Results.Ok(new { message = "Password reset code sent to your email." });
});

auth.MapPost("/password-reset/confirm", async (
    ResetPasswordRequest request,
    EventAppDbContext db,
    CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(request.Code) ||
        string.IsNullOrWhiteSpace(request.NewPassword))
    {
        return Results.BadRequest(new { message = "Email, reset code, and new password are required." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash))
    {
        return Results.NotFound(new { message = "Registered user with this email was not found." });
    }

    if (user.PasswordResetCodeExpiresAt is null ||
        user.PasswordResetCodeExpiresAt <= DateTimeOffset.UtcNow ||
        !PasswordHasher.Verify(request.Code.Trim(), user.PasswordResetCodeHash))
    {
        return Results.BadRequest(new { message = "Password reset code is invalid or expired." });
    }

    user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
    user.PasswordResetCodeHash = null;
    user.PasswordResetCodeExpiresAt = null;
    user.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { message = "Password updated. You can sign in with your new password." });
});

auth.MapGet("/me", async (HttpContext httpContext, ICurrentUser currentUser, EventAppDbContext db, CancellationToken cancellationToken) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
    return user is null ? Results.NotFound() : Fresh(httpContext, ToUserDto(user), user.UpdatedAt);
})
    .RequireAuthorization();
auth.MapGet("/me/activities", async (HttpContext httpContext, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
{
    var activities = await service.GetRegisteredActivitiesAsync(currentUser.UserId, currentUser.UserId, cancellationToken);
    return Fresh(httpContext, activities, GetActivitiesLastModified(activities));
})
    .RequireAuthorization();
auth.MapPost("/signout", () => Results.Ok(new { message = "Signed out." }))
    .RequireAuthorization();

var notifications = app.MapGroup("/api/notifications").RequireAuthorization();
notifications.MapGet("/config", (IOptions<PushNotificationOptions> options) =>
{
    var pushOptions = options.Value;
    return Results.Ok(new { isEnabled = pushOptions.IsConfigured, publicKey = pushOptions.PublicKey });
});
notifications.MapPost("/subscriptions", async (
    PushSubscriptionRequest request,
    ICurrentUser currentUser,
    EventAppDbContext db,
    HttpContext httpContext,
    IOptions<PushNotificationOptions> options,
    CancellationToken cancellationToken) =>
{
    if (!options.Value.IsConfigured)
    {
        return Results.BadRequest(new { message = "Push notifications are not configured on the server." });
    }

    if (string.IsNullOrWhiteSpace(request.Endpoint) ||
        string.IsNullOrWhiteSpace(request.Keys.P256dh) ||
        string.IsNullOrWhiteSpace(request.Keys.Auth))
    {
        return Results.BadRequest(new { message = "Push subscription is incomplete." });
    }

    var subscription = await db.WebPushSubscriptions
        .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, cancellationToken);
    if (subscription is null)
    {
        subscription = new WebPushSubscription { Endpoint = request.Endpoint };
        db.WebPushSubscriptions.Add(subscription);
    }

    subscription.UserId = currentUser.UserId;
    subscription.P256dh = request.Keys.P256dh;
    subscription.Auth = request.Keys.Auth;
    subscription.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
    subscription.LastSeenAt = DateTimeOffset.UtcNow;
    subscription.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { message = "Push subscription saved." });
});
notifications.MapPost("/subscriptions/delete", async (
    PushSubscriptionRequest request,
    ICurrentUser currentUser,
    EventAppDbContext db,
    CancellationToken cancellationToken) =>
{
    var subscriptions = await db.WebPushSubscriptions
        .Where(s => s.UserId == currentUser.UserId && s.Endpoint == request.Endpoint)
        .ToListAsync(cancellationToken);
    db.WebPushSubscriptions.RemoveRange(subscriptions);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { message = "Push subscription removed." });
});

var activities = app.MapGroup("/api/activities").RequireAuthorization();
activities.MapGet("/", async (HttpContext httpContext, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
{
    var schedule = await service.GetScheduleAsync(currentUser.UserId, cancellationToken);
    return Fresh(httpContext, schedule, GetActivitiesLastModified(schedule));
});
activities.MapGet("/{id:guid}", async (Guid id, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
    await service.GetActivityAsync(id, currentUser.UserId, cancellationToken) is { } activity ? Results.Ok(activity) : Results.NotFound());
activities.MapGet("/{id:guid}/participants", async (Guid id, HttpContext httpContext, EventService service, CancellationToken cancellationToken) =>
{
    var participants = await service.GetParticipantsAsync(id, cancellationToken);
    return Fresh(httpContext, participants, GetUsersLastModified(participants));
});
activities.MapPost("/", (UpsertActivityRequest request, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
    service.CreateActivityAsync(currentUser.UserId, request, cancellationToken))
    .RequireAuthorization("AdminOnly");
activities.MapPut("/{id:guid}", async (Guid id, UpsertActivityRequest request, EventService service, CancellationToken cancellationToken) =>
    await service.UpdateActivityAsync(id, request, cancellationToken) is { } activity ? Results.Ok(activity) : Results.NotFound())
    .RequireAuthorization("AdminOnly");
activities.MapPost("/{id:guid}/paint", async (Guid id, EventService service, CancellationToken cancellationToken) =>
    await service.PaintActivityAsync(id, cancellationToken) is { } activity ? Results.Ok(activity) : Results.NotFound())
    .RequireAuthorization("AdminOnly");
activities.MapDelete("/{id:guid}", async (Guid id, EventService service, CancellationToken cancellationToken) =>
    await service.DeleteActivityAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization("AdminOnly");
activities.MapPost("/{id:guid}/registrations", async (Guid id, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
{
    var result = await service.RegisterAsync(currentUser.UserId, id, cancellationToken);
    return result switch
    {
        RegistrationResult.Registered => Results.Ok(new { message = "Registered successfully." }),
        RegistrationResult.AlreadyRegistered => Results.Conflict(new { message = "You are already registered for this activity." }),
        RegistrationResult.Full => Results.Conflict(new { message = "Activity is full." }),
        RegistrationResult.Overlaps => Results.Conflict(new { message = "Activity overlaps with another registration." }),
        RegistrationResult.NotRegistrable => Results.Conflict(new { message = "This activity does not require registration." }),
        RegistrationResult.RetryLater => Results.Conflict(new { message = "Registration is busy right now. Please try again in a moment." }),
        RegistrationResult.Closed => Results.Conflict(new { message = "Registration to activities is closed." }),
        _ => Results.NotFound()
    };
});
activities.MapDelete("/{id:guid}/registrations", async (Guid id, ICurrentUser currentUser, EventService service, CancellationToken cancellationToken) =>
    await service.CancelRegistrationAsync(currentUser.UserId, id, cancellationToken)
        ? Results.Ok(new { message = "Registration cancelled." })
        : Results.NotFound());

var directory = app.MapGroup("/api/directory").RequireAuthorization();
directory.MapGet("/", async (DirectoryType? type, HttpContext httpContext, DirectoryService service, CancellationToken cancellationToken) =>
{
    var users = await service.GetUsersAsync(type, includeInactive: false, cancellationToken);
    return Fresh(httpContext, users, GetUsersLastModified(users));
});
directory.MapGet("/{id:guid}", async (Guid id, DirectoryService service, CancellationToken cancellationToken) =>
    await service.GetUserAsync(id, cancellationToken) is { } user ? Results.Ok(user) : Results.NotFound());
directory.MapGet("/{id:guid}/details", async (Guid id, HttpContext httpContext, ICurrentUser currentUser, DirectoryService service, CancellationToken cancellationToken) =>
{
    var user = await service.GetUserDetailsAsync(id, currentUser.UserId, cancellationToken);
    return user is null ? Results.NotFound() : Fresh(httpContext, user, GetUserDetailsLastModified(user));
});

var users = app.MapGroup("/api/users").RequireAuthorization();
users.MapPut("/me", async (UpdateProfileRequest request, ICurrentUser currentUser, DirectoryService service, CancellationToken cancellationToken) =>
    await service.UpdateCurrentUserAsync(currentUser.UserId, request, cancellationToken) is { } user ? Results.Ok(user) : Results.NotFound());
users.MapPost("/me/avatar", async (HttpRequest request, ICurrentUser currentUser, DirectoryService service, CancellationToken cancellationToken) =>
{
    var image = await ReadUploadedImageAsync(request, cancellationToken);
    var imageUrl = $"{request.Scheme}://{request.Host}/api/users/{currentUser.UserId}/avatar/image?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    var updated = await service.UpdateCurrentUserAvatarAsync(
        currentUser.UserId,
        imageUrl,
        image.Data,
        image.ContentType,
        image.FileName,
        cancellationToken);
    if (updated is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { imageUrl, user = updated });
});
users.MapGet("/{id:guid}/avatar/image", async (Guid id, DirectoryService service, CancellationToken cancellationToken) =>
{
    var avatar = await service.GetAvatarAsync(id, cancellationToken);
    return avatar is null
        ? Results.NotFound()
        : Results.File(avatar.Data, avatar.ContentType, avatar.FileName, lastModified: avatar.UpdatedAt);
})
    .AllowAnonymous();
users.MapPut("/{id:guid}/admin", async (Guid id, UpdateUserAdminRequest request, DirectoryService service, CancellationToken cancellationToken) =>
    await service.UpdateUserAsync(id, request, cancellationToken) is { } user ? Results.Ok(user) : Results.NotFound())
    .RequireAuthorization("AdminOnly");
users.MapGet("/", async (HttpContext httpContext, DirectoryService service, CancellationToken cancellationToken) =>
{
    var allUsers = await service.GetUsersAsync(null, includeInactive: true, cancellationToken);
    return Fresh(httpContext, allUsers, GetUsersLastModified(allUsers));
})
    .RequireAuthorization("AdminOnly");
users.MapDelete("/{id:guid}", async (Guid id, ICurrentUser currentUser, DirectoryService service, CancellationToken cancellationToken) =>
    await service.DeleteUserAsync(id, currentUser.UserId, cancellationToken) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization("AdminOnly");

var info = app.MapGroup("/api/info").RequireAuthorization();
info.MapGet("/branding", async (HttpContext httpContext, InfoService service, CancellationToken cancellationToken) =>
{
    var infoDto = await service.GetAsync(cancellationToken);
    return Fresh(httpContext, new
    {
        infoDto.Title,
        infoDto.LogoImageUrl,
        infoDto.IsSystemRegistrationClosed,
        infoDto.IsActivityRegistrationClosed,
        infoDto.UpdatedAt
    }, infoDto.UpdatedAt);
})
    .AllowAnonymous();
info.MapGet("/", async (HttpContext httpContext, InfoService service, CancellationToken cancellationToken) =>
{
    var infoDto = await service.GetAsync(cancellationToken);
    return Fresh(httpContext, infoDto, infoDto.UpdatedAt);
});
info.MapGet("/logo/image", async (InfoService service, CancellationToken cancellationToken) =>
{
    var logo = await service.GetLogoAsync(cancellationToken);
    return logo is null
        ? Results.NotFound()
        : Results.File(logo.Data, logo.ContentType, logo.FileName, lastModified: logo.UpdatedAt);
})
    .AllowAnonymous();
info.MapPut("/", (EventInfoDto request, InfoService service, CancellationToken cancellationToken) =>
    service.UpdateAsync(request, cancellationToken))
    .RequireAuthorization("AdminOnly");
info.MapPost("/logo/upload", async (HttpRequest request, InfoService service, CancellationToken cancellationToken) =>
{
    var image = await ReadUploadedImageAsync(request, cancellationToken);
    var imageUrl = $"{request.Scheme}://{request.Host}/api/info/logo/image?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    var infoDto = await service.UpdateLogoAsync(
        imageUrl,
        image.Data,
        image.ContentType,
        image.FileName,
        cancellationToken);
    return Results.Ok(new { imageUrl, info = infoDto });
})
    .RequireAuthorization("AdminOnly");
info.MapDelete("/logo", (InfoService service, CancellationToken cancellationToken) =>
    service.UpdateLogoAsync(null, null, null, null, cancellationToken))
    .RequireAuthorization("AdminOnly");

app.MapFallbackToFile("index.html");

app.Run();

static UserDto ToUserDto(User user) => new(
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

static IResult Fresh<T>(HttpContext httpContext, T value, DateTimeOffset? lastModified)
{
    var json = JsonSerializer.Serialize(value);
    var etag = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}\"";
    httpContext.Response.Headers.CacheControl = "no-store";
    httpContext.Response.Headers.ETag = etag;
    if (lastModified.HasValue)
    {
        httpContext.Response.Headers.LastModified = lastModified.Value.UtcDateTime.ToString("R");
    }

    return Results.Json(value);
}

static DateTimeOffset? GetActivitiesLastModified(IEnumerable<ActivityDto> activities) =>
    activities.Any() ? activities.Max(a => a.UpdatedAt) : null;

static DateTimeOffset? GetUsersLastModified(IEnumerable<UserDto> users) =>
    users.Any() ? users.Max(u => u.UpdatedAt) : null;

static DateTimeOffset? GetUserDetailsLastModified(UserDetailsDto details)
{
    var values = new[] { details.User.UpdatedAt }
        .Concat(details.RegisteredActivities.Select(a => a.UpdatedAt));
    return values.Max();
}

static async Task<UploadedImage> ReadUploadedImageAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (!request.HasFormContentType)
    {
        throw new ArgumentException("Expected multipart form data.");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
    {
        throw new ArgumentException("Image file is required.");
    }

    if (file.Length > 1_000_000)
    {
        throw new ArgumentException("Image file must be 1 MB or smaller.");
    }

    var extension = GetImageExtension(file.ContentType);
    var fileName = Path.GetFileName(file.FileName);
    if (string.IsNullOrWhiteSpace(fileName))
    {
        fileName = $"logo{extension}";
    }

    await using var stream = file.OpenReadStream();
    using var memory = new MemoryStream();
    await stream.CopyToAsync(memory, cancellationToken);
    return new UploadedImage(memory.ToArray(), file.ContentType, fileName);
}

static string GetImageExtension(string contentType)
{
    var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/svg+xml"] = ".svg",
        ["image/webp"] = ".webp"
    };

    if (!allowedTypes.TryGetValue(contentType, out var extension))
    {
        throw new ArgumentException("Only PNG, JPG, SVG, or WebP images are allowed.");
    }

    return extension;
}

static void ValidateProductionConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    if (environment.IsDevelopment())
    {
        return;
    }

    var missing = new List<string>();
    var databaseUrl = configuration["DATABASE_URL"];
    var connectionString = configuration.GetConnectionString("EventApp");
    if (string.IsNullOrWhiteSpace(databaseUrl) &&
        (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Host=127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    {
        missing.Add("DATABASE_URL or production ConnectionStrings__EventApp");
    }

    var signingKey = configuration["Jwt:SigningKey"];
    if (string.IsNullOrWhiteSpace(signingKey) ||
        signingKey.Contains("local-development", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Contains("replace", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Length < 32)
    {
        missing.Add("Jwt__SigningKey (32+ characters)");
    }

    var applicationPassword = configuration["Authentication:ApplicationPassword"];
    if (string.IsNullOrWhiteSpace(applicationPassword) ||
        applicationPassword.Contains("change", StringComparison.OrdinalIgnoreCase) ||
        applicationPassword.Length < 8)
    {
        missing.Add("Authentication__ApplicationPassword (8+ characters)");
    }

    if (missing.Count > 0)
    {
        throw new InvalidOperationException(
            "Production configuration is incomplete. Set: " + string.Join(", ", missing));
    }
}

static bool IsApplicationPasswordValid(IConfiguration configuration, string? password)
{
    if (string.IsNullOrWhiteSpace(password))
    {
        return false;
    }

    var applicationPassword = configuration["Authentication:ApplicationPassword"];
    if (string.IsNullOrWhiteSpace(applicationPassword))
    {
        throw new InvalidOperationException("Application password is not configured.");
    }

    var submittedHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(applicationPassword));
    return CryptographicOperations.FixedTimeEquals(submittedHash, configuredHash);
}

static bool IsUniqueConstraintViolation(Exception exception) =>
    GetSqlState(exception) == "23505";

static async Task<bool> UserExistsAsync(EventAppDbContext db, string email, CancellationToken cancellationToken)
{
    var emailAccount = GetEmailAccount(email);
    var emailDomain = GetEmailDomain(email);
    var emailPrefix = emailAccount + "@";
    var candidateEmails = await db.Users
        .AsNoTracking()
        .Where(user => user.Email == email || user.Email.StartsWith(emailPrefix))
        .Select(user => user.Email)
        .ToListAsync(cancellationToken);

    return candidateEmails.Any(existingEmail =>
    {
        var existingDomain = GetEmailDomain(existingEmail);
        return string.Equals(existingEmail, email, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(existingDomain, emailDomain, StringComparison.OrdinalIgnoreCase) ||
            existingDomain.StartsWith(emailDomain + ".", StringComparison.OrdinalIgnoreCase) ||
            emailDomain.StartsWith(existingDomain + ".", StringComparison.OrdinalIgnoreCase);
    });
}

static string? GetSqlState(Exception exception)
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

static bool IsFullNameValid(string fullName) =>
    Regex.IsMatch(fullName, "^[A-Z][A-Za-z]{1,}\\s+[A-Z][A-Za-z]{1,}$");

static bool IsEmailValid(string email) =>
    Regex.IsMatch(email, "^[^@\\s]+@[^@\\s.]+(?:\\.[^@\\s.]+)+$");

static string GetEmailAccount(string email) =>
    email.Split('@', 2)[0];

static string GetEmailDomain(string email) =>
    email.Split('@', 2).ElementAtOrDefault(1) ?? string.Empty;

internal sealed class TokenIssuer(IConfiguration configuration)
{
    public string Create(User user)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? "local-development-signing-key-change-before-production";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "EventApp.Api",
            audience: configuration["Jwt:Audience"] ?? "EventApp.Web",
            claims: claims,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal sealed record UploadedImage(byte[] Data, string ContentType, string FileName);

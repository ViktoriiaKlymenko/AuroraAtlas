using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using EventApp.Application;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace EventApp.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string FullName => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}

public sealed class ExternalTokenValidator(IConfiguration configuration) : ITokenValidator
{
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> AppleConfigurationManager = new(
        "https://appleid.apple.com/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever());

    public async Task<ExternalUser> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedAccessException("Identity token is required.");
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "google" => await ValidateGoogleAsync(idToken, cancellationToken),
            "apple" => await ValidateAppleAsync(idToken, cancellationToken),
            _ => throw new UnauthorizedAccessException("Unsupported authentication provider.")
        };
    }

    private async Task<ExternalUser> ValidateGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var audiences = configuration.GetSection("Authentication:Google:ClientIds").Get<string[]>()
            ?? [];
        if (audiences.Length == 0)
        {
            throw new UnauthorizedAccessException("Google authentication is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = audiences });
        }
        catch (InvalidJwtException exception)
        {
            throw new UnauthorizedAccessException("Google token validation failed.", exception);
        }

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email address is not verified.");
        }

        return new ExternalUser(payload.Email, payload.Name ?? string.Empty, payload.Picture);
    }

    private async Task<ExternalUser> ValidateAppleAsync(string idToken, CancellationToken cancellationToken)
    {
        var audiences = configuration.GetSection("Authentication:Apple:ClientIds").Get<string[]>()
            ?? [];
        if (audiences.Length == 0)
        {
            throw new UnauthorizedAccessException("Apple authentication is not configured.");
        }

        var appleConfiguration = await AppleConfigurationManager.GetConfigurationAsync(cancellationToken);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(idToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudiences = audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = appleConfiguration.SigningKeys
            }, out _);

            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? throw new UnauthorizedAccessException("Apple token did not include an email address.");
            var fullName = principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue("name")
                ?? string.Empty;

            return new ExternalUser(email, fullName, null);
        }
        catch (SecurityTokenException exception)
        {
            throw new UnauthorizedAccessException("Apple token validation failed.", exception);
        }
    }
}

public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken)
    {
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP is not configured. Password reset code for {Email}: {Code}", email, code);
            return;
        }

        var fromEmail = configuration["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP sender email is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, configuration["Smtp:FromName"] ?? "Event App"),
            Subject = "Password reset code",
            Body = $"Your password reset code is: {code}\n\nThis code expires in 30 minutes.",
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var client = new SmtpClient(host, configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = configuration.GetValue("Smtp:UseSsl", true)
        };

        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<ITokenValidator, ExternalTokenValidator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        return services;
    }
}

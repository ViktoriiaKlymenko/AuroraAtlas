using EventApp.Application;
using EventApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace EventApp.Persistence;

public sealed class EventAppDbContext(DbContextOptions<EventAppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityRegistration> ActivityRegistrations => Set<ActivityRegistration>();
    public DbSet<EventInfo> EventInfos => Set<EventInfo>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<ActivityReminderDelivery> ActivityReminderDeliveries => Set<ActivityReminderDelivery>();

    IQueryable<User> IApplicationDbContext.Users => Users;
    IQueryable<Activity> IApplicationDbContext.Activities => Activities;
    IQueryable<ActivityRegistration> IApplicationDbContext.ActivityRegistrations => ActivityRegistrations;
    IQueryable<EventInfo> IApplicationDbContext.EventInfos => EventInfos;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Add(entity);
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Remove(entity);
    Task<IDbContextTransaction> IApplicationDbContext.BeginSerializableTransactionAsync(CancellationToken cancellationToken) =>
        Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(160).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
            entity.Property(u => u.PasswordResetCodeHash).HasMaxLength(512);
            entity.Property(u => u.AvatarUrl).HasMaxLength(2048);
            entity.Property(u => u.AvatarImageContentType).HasMaxLength(128);
            entity.Property(u => u.AvatarImageFileName).HasMaxLength(260);
            entity.Property(u => u.Company).HasMaxLength(160);
            entity.Property(u => u.Position).HasMaxLength(160);
            entity.Property(u => u.Bio).HasMaxLength(2000);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(u => u.DirectoryType).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.Property(a => a.Title).HasMaxLength(180).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(4000).IsRequired();
            entity.Property(a => a.Details).HasMaxLength(8000).IsRequired();
            entity.Property(a => a.Location).HasMaxLength(240).IsRequired();
            entity.HasIndex(a => new { a.Date, a.StartTime });
            entity.HasOne(a => a.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(a => a.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActivityRegistration>(entity =>
        {
            entity.HasIndex(r => new { r.UserId, r.ActivityId }).IsUnique();
            entity.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Activity)
                .WithMany(a => a.Registrations)
                .HasForeignKey(r => r.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebPushSubscription>(entity =>
        {
            entity.Property(s => s.Endpoint).HasMaxLength(2048).IsRequired();
            entity.Property(s => s.P256dh).HasMaxLength(512).IsRequired();
            entity.Property(s => s.Auth).HasMaxLength(256).IsRequired();
            entity.Property(s => s.UserAgent).HasMaxLength(512);
            entity.HasIndex(s => s.Endpoint).IsUnique();
            entity.HasOne(s => s.User)
                .WithMany(u => u.WebPushSubscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityReminderDelivery>(entity =>
        {
            entity.HasIndex(d => new { d.ActivityRegistrationId, d.WebPushSubscriptionId }).IsUnique();
            entity.HasOne(d => d.ActivityRegistration)
                .WithMany(r => r.ReminderDeliveries)
                .HasForeignKey(d => d.ActivityRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.WebPushSubscription)
                .WithMany()
                .HasForeignKey(d => d.WebPushSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventInfo>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(180).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(8000).IsRequired();
            entity.Property(i => i.Location).HasMaxLength(240).IsRequired();
            entity.Property(i => i.Address).HasMaxLength(400).IsRequired();
            entity.Property(i => i.Contacts).HasMaxLength(1000).IsRequired();
            entity.Property(i => i.AdditionalInfo).HasMaxLength(8000).IsRequired();
            entity.Property(i => i.BannerImageUrl).HasMaxLength(2048);
            entity.Property(i => i.LogoImageUrl).HasMaxLength(2048);
            entity.Property(i => i.LogoImageContentType).HasMaxLength(128);
            entity.Property(i => i.LogoImageFileName).HasMaxLength(260);
        });
    }
}

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ToNpgsqlConnectionString(configuration["DATABASE_URL"])
            ?? configuration.GetConnectionString("EventApp")
            ?? "Host=localhost;Database=eventapp;Username=postgres;Password=postgres";

        services.AddDbContext<EventAppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<EventAppDbContext>());
        return services;
    }

    private static string? ToNpgsqlConnectionString(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            return databaseUrl;
        }

        if (uri.Scheme is not ("postgres" or "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = Npgsql.SslMode.Require
        };

        return builder.ConnectionString;
    }
}

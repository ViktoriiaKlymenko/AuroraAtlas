# Render Environment Variables

Set these on the Render backend service before publishing.

## Required

| Variable | Example value | Purpose |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runs the API with production settings. |
| `Jwt__SigningKey` | `generate-a-long-random-secret-at-least-32-chars` | Signs and validates app JWT tokens. Use a strong random value. |
| `Jwt__Issuer` | `EventApp.Api` | JWT issuer. Keep this stable after users sign in. |
| `Jwt__Audience` | `EventApp.Web` | JWT audience expected by the API. |
| `Authentication__ApplicationPassword` | `choose-one-shared-entry-password` | Shared application password required for every web sign-up and sign-in. |

## Database

Use one of these:

| Variable | Example value | Purpose |
| --- | --- | --- |
| `DATABASE_URL` | Render PostgreSQL internal/external database URL | Preferred on Render. The app converts Render's `postgres://...` URL automatically. |
| `ConnectionStrings__EventApp` | `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` | Standard .NET connection string alternative. |

## Seed users

These are used when the API starts and seeds the database.

| Variable | Example value | Purpose |
| --- | --- | --- |
| `SeedData__AdminEmail` | `admin@example.com` | Initial admin account email. |
| `SeedData__AdminPassword` | `change-this-admin-password` | Legacy per-user password for seeded admin accounts. Web sign-in uses `Authentication__ApplicationPassword`. |
| `SeedData__DefaultUserEmail` | `attendee@example.com` | Optional default attendee account email. |
| `SeedData__DefaultUserPassword` | `change-this-attendee-password` | Legacy per-user password for the default attendee. Web sign-in uses `Authentication__ApplicationPassword`. |

## Web push reminders

Set these for closed-app event reminders. Generate the VAPID key pair once, keep it stable, and never expose the private key outside server environment variables.

| Variable | Example value | Purpose |
| --- | --- | --- |
| `PushNotifications__Subject` | `mailto:admin@example.com` | VAPID contact subject for push providers. |
| `PushNotifications__PublicKey` | `B...` | Public VAPID key sent to browsers during subscription. |
| `PushNotifications__PrivateKey` | `...` | Private VAPID key used by the backend reminder worker. |
| `PushNotifications__EventTimeZoneId` | `Europe/Kyiv` | Timezone used to interpret activity start times. |
| `PushNotifications__ReminderOffsetMinutes` | `30` | Minutes before an activity starts to send the reminder. |

On iPhone, users must install the web app to the Home Screen and allow notifications. Android browsers can subscribe from the PWA/browser notification prompt.

## Optional social sign-in

Set these only if Google or Apple sign-in is enabled in the web app.

| Variable | Example value | Purpose |
| --- | --- | --- |
| `Authentication__Google__ClientIds__0` | `your-google-client-id.apps.googleusercontent.com` | First allowed Google OAuth client ID. |
| `Authentication__Google__ClientIds__1` | `another-google-client-id.apps.googleusercontent.com` | Additional Google client ID, if needed. |
| `Authentication__Apple__ClientIds__0` | `com.companyname.eventapp.web` | First allowed Apple services/client ID. |

Render also provides `PORT`; the API now reads it automatically and binds to `http://0.0.0.0:{PORT}`.

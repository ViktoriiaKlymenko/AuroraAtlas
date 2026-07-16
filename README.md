# Summer Fest

Summer Fest is a full-stack event management application that I designed, developed, and deployed from start to finish. I created the product experience, responsive interface, backend API, database model, authentication flow, administration tools, Progressive Web App features, and production hosting.

The application gives attendees one place to explore the event schedule, register for activities, view participant information, manage their profile, and receive timely reminders. Administrators can manage the event program, users, registrations, and event information through a dedicated interface.

## User guide and demo access

- **User guide:** [eventuserguide.onrender.com](https://eventuserguide.onrender.com/)
- **Application password:** `summer2026`

> The password above is a shared application access password intended for this portfolio/demo project. Do not reuse it for production secrets or personal accounts.

## What I built

- Responsive, mobile-first event experience
- Multi-day schedule with detailed activity pages
- Activity registration and cancellation
- Capacity limits and schedule-overlap validation
- Personal agenda and attendee profiles
- Directory for speakers, sponsors, exhibitors, and attendees
- Event information, venue details, contacts, branding, and announcements
- Role-based user and administrator access
- Admin tools for activities, users, registration controls, and event content
- Installable PWA with offline-capable assets and service-worker support
- Browser push notifications for upcoming activity reminders
- Secure JWT-based API authentication
- PostgreSQL persistence with Entity Framework Core migrations and seed data
- Dockerized production build and Render-ready configuration

## My role

I owned the complete application lifecycle:

1. Defined the product requirements and attendee/admin workflows.
2. Designed the visual language, navigation, responsive layouts, and interaction states.
3. Built the frontend as a lightweight JavaScript PWA.
4. Designed and implemented the ASP.NET Core API and application services.
5. Modeled the PostgreSQL database and created EF Core migrations and seed data.
6. Implemented authentication, authorization, validation, registration rules, and notifications.
7. Containerized the application and configured it for hosting on Render.
8. Created the supporting end-user documentation.

## Technology stack

| Area | Technology |
| --- | --- |
| Frontend | HTML5, CSS3, vanilla JavaScript |
| PWA | Web App Manifest, Service Worker, Web Push |
| Backend | ASP.NET Core Minimal APIs, .NET 10 |
| Authentication | JWT Bearer authentication, role-based authorization |
| Data | PostgreSQL, Entity Framework Core |
| Architecture | Domain, Application, Infrastructure, Persistence, and API layers |
| Deployment | Docker, Render |

## Architecture

The solution separates business rules from delivery and infrastructure concerns:

```text
EventApp.Api             HTTP endpoints, authentication, static PWA, background reminders
EventApp.Application     Use cases, services, contracts, DTOs, and validation
EventApp.Domain          Core entities and enums
EventApp.Infrastructure  Authentication and external-service implementations
EventApp.Persistence     EF Core context, migrations, and database seeding
```

## Run locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL
- Docker (optional)

### With the .NET CLI

1. Create a PostgreSQL database.
2. Update `ConnectionStrings:EventApp` in `EventApp.Api/appsettings.Development.json` or provide it as an environment variable.
3. Set local values for the JWT signing key and application password.
4. Restore and run the API:

```bash
dotnet restore EventApp.slnx
dotnet run --project EventApp.Api/EventApp.Api.csproj
```

The application applies pending database migrations and seeds its initial data when it starts.

### With Docker

```bash
docker build -t summer-fest .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="your-postgresql-url" \
  -e Jwt__SigningKey="your-random-signing-key-at-least-32-characters" \
  -e Authentication__ApplicationPassword="summer2026" \
  summer-fest
```

Open `http://localhost:8080` in your browser.

## Production configuration

The deployed application expects configuration through environment variables. The required values are:

- `ASPNETCORE_ENVIRONMENT=Production`
- `DATABASE_URL` or `ConnectionStrings__EventApp`
- `Jwt__SigningKey`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Authentication__ApplicationPassword`

Web push reminders additionally require VAPID public/private keys and a contact subject. See [RENDER_ENVIRONMENT.md](./RENDER_ENVIRONMENT.md) for the complete deployment reference.

## Project status

The project is complete and deployed as a portfolio demonstration of an end-to-end event platform, covering product design, engineering, database development, production configuration, hosting, and user documentation.

## Author

Designed, developed, and deployed by [Viktoriia Klymenko](https://github.com/ViktoriiaKlymenko).

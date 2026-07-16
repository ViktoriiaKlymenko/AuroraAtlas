FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventApp.Api/EventApp.Api.csproj EventApp.Api/
COPY EventApp.Application/EventApp.Application.csproj EventApp.Application/
COPY EventApp.Domain/EventApp.Domain.csproj EventApp.Domain/
COPY EventApp.Infrastructure/EventApp.Infrastructure.csproj EventApp.Infrastructure/
COPY EventApp.Persistence/EventApp.Persistence.csproj EventApp.Persistence/

RUN dotnet restore EventApp.Api/EventApp.Api.csproj

COPY EventApp.Api/ EventApp.Api/
COPY EventApp.Application/ EventApp.Application/
COPY EventApp.Domain/ EventApp.Domain/
COPY EventApp.Infrastructure/ EventApp.Infrastructure/
COPY EventApp.Persistence/ EventApp.Persistence/

RUN dotnet publish EventApp.Api/EventApp.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "EventApp.Api.dll"]

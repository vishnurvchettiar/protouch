# Build the ASP.NET Core / Blazor Server application.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ClinicApp/ClinicApp.csproj ClinicApp/
RUN dotnet restore ClinicApp/ClinicApp.csproj

COPY ClinicApp/ ClinicApp/
RUN dotnet publish ClinicApp/ClinicApp.csproj -c Release -o /app/publish --no-restore

# Render exposes the web service on PORT (10000 by default).
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
# Render containers can have a low inotify watcher limit. Polling avoids a
# startup crash when ASP.NET Core monitors configuration files.
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
# Used only when no DATABASE_URL is set (SQLite fallback). With a Render PostgreSQL database,
# set DATABASE_URL on the web service and everything is stored there instead.
ENV ConnectionStrings__DefaultConnection="Data Source=/var/data/app.db"
EXPOSE 10000

COPY --from=build /app/publish .
RUN mkdir -p /var/data
ENTRYPOINT ["dotnet", "ClinicApp.dll"]

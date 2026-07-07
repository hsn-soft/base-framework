FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Metadata labels
LABEL org.opencontainers.image.title="TechSummus Hhs Scheduler Api Trigger" \
      org.opencontainers.image.description="TechSummus Hhs Scheduler Api Trigger" \
      org.opencontainers.image.authors="TechSummus <admin@techsummus.com>" \
      org.opencontainers.image.source="https://github.com/tech-summus/hhs-app-scheduler"

USER root

WORKDIR /app
EXPOSE 8080

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_ENVIRONMENT='docker'

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Health check endpoint (varsayılan: /health)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-stage
WORKDIR /build-source

# Non-root user setup
RUN groupadd -r -g 1001 servicegroup && \
    useradd -r -u 1001 serviceuser -g servicegroup

# Configuration files
COPY ["./nuget.config", "./"]
COPY ["./common.version.props", "./"]

COPY ["./src/Hhs.Scheduler.ApiTrigger.Host/Hhs.Scheduler.ApiTrigger.Host.csproj", "./src/Hhs.Scheduler.ApiTrigger.Host/"]

ENV NUGET_CERT_REVOCATION_MODE=offline
RUN dotnet restore "./src/Hhs.Scheduler.ApiTrigger.Host/Hhs.Scheduler.ApiTrigger.Host.csproj" --force --verbosity minimal --configfile ./nuget.config

# Copy source code
COPY --chown=1001:1001 ["./src/Hhs.Scheduler.ApiTrigger.Host/.", "./src/Hhs.Scheduler.ApiTrigger.Host/"]

# Build
RUN dotnet build "./src/Hhs.Scheduler.ApiTrigger.Host/Hhs.Scheduler.ApiTrigger.Host.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

# (Opsiyonel) Test
RUN dotnet test "./src/Hhs.Scheduler.ApiTrigger.Host/Hhs.Scheduler.ApiTrigger.Host.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Publish
RUN dotnet publish "./src/Hhs.Scheduler.ApiTrigger.Host/Hhs.Scheduler.ApiTrigger.Host.csproj" \
    --no-restore \
    --no-build \
    --configuration Release \
    --output /build-source/publish \
    /p:UseAppHost=false

# Final Runtime Image
FROM base AS final
WORKDIR /app
COPY --chown=1001:1001 --from=build-stage /build-source/publish .

# Runtime User
USER 1001

ENTRYPOINT ["dotnet", "Hhs.Scheduler.ApiTrigger.Host.dll"]
CMD []
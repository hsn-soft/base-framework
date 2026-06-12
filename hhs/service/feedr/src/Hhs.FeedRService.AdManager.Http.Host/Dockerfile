FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Metadata labels
LABEL org.opencontainers.image.title="TechSummus Hhs FeedR Service" \
      org.opencontainers.image.description="TechSummus Hhs FeedR Service" \
      org.opencontainers.image.authors="TechSummus <admin@techsummus.com>" \
      org.opencontainers.image.source="https://github.com/tech-summus/hhs-service-feedr"

USER root

WORKDIR /app
EXPOSE 8080

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_URLS='http://+:8080;' \
    ASPNETCORE_ENVIRONMENT='docker'

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Health check endpoint (varsayılan: /health)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/LivenessCheck || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-stage
WORKDIR /build-source

# Non-root user setup
RUN groupadd -r -g 1001 servicegroup && \
    useradd -r -u 1001 serviceuser -g servicegroup

# Configuration files
COPY ["./nuget.config", "./"]
COPY ["./common.props", "./"]
COPY ["./common.version.props", "./"]
COPY ["./common.test.props", "./"]
COPY ["./public_key.xml", "./"]

COPY ["./src/Hhs.FeedRService.Application/Hhs.FeedRService.Application.csproj", "./src/Hhs.FeedRService.Application/"]
COPY ["./src/Hhs.FeedRService.Domain/Hhs.FeedRService.Domain.csproj", "./src/Hhs.FeedRService.Domain/"]
COPY ["./src/Hhs.FeedRService.EntityFrameworkCore/Hhs.FeedRService.EntityFrameworkCore.csproj", "./src/Hhs.FeedRService.EntityFrameworkCore/"]
COPY ["./src/Hhs.FeedRService.MongoDb/Hhs.FeedRService.MongoDb.csproj", "./src/Hhs.FeedRService.MongoDb/"]
COPY ["./src/Hhs.FeedRService.AdManager.Http.Host/Hhs.FeedRService.AdManager.Http.Host.csproj", "./src/Hhs.FeedRService.AdManager.Http.Host/"]

ENV NUGET_CERT_REVOCATION_MODE=offline

# --- 🔑 BUILD ARG Credentials ---
ARG NUGET_USER
ARG NUGET_TOKEN

RUN dotnet nuget update source github \
        --username $NUGET_USER \
        --password $NUGET_TOKEN \
        --store-password-in-clear-text || true

RUN dotnet restore "./src/Hhs.FeedRService.AdManager.Http.Host/Hhs.FeedRService.AdManager.Http.Host.csproj" --force --verbosity minimal

# Copy source code
COPY --chown=1001:1001 ["./src/Hhs.FeedRService.Application/.", "./src/Hhs.FeedRService.Application/"]
COPY --chown=1001:1001 ["./src/Hhs.FeedRService.Domain/.", "./src/Hhs.FeedRService.Domain/"]
COPY --chown=1001:1001 ["./src/Hhs.FeedRService.EntityFrameworkCore/.", "./src/Hhs.FeedRService.EntityFrameworkCore/"]
COPY --chown=1001:1001 ["./src/Hhs.FeedRService.MongoDb/.", "./src/Hhs.FeedRService.MongoDb/"]
COPY --chown=1001:1001 ["./src/Hhs.FeedRService.AdManager.Http.Host/.", "./src/Hhs.FeedRService.AdManager.Http.Host/"]

# Build
RUN dotnet build "./src/Hhs.FeedRService.AdManager.Http.Host/Hhs.FeedRService.AdManager.Http.Host.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

# (Opsiyonel) Test
RUN dotnet test "./src/Hhs.FeedRService.AdManager.Http.Host/Hhs.FeedRService.AdManager.Http.Host.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Publish
RUN dotnet publish "./src/Hhs.FeedRService.AdManager.Http.Host/Hhs.FeedRService.AdManager.Http.Host.csproj" \
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

ENTRYPOINT ["dotnet", "Hhs.FeedRService.AdManager.Http.Host.dll"]
CMD []
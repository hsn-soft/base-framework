FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Metadata labels
LABEL org.opencontainers.image.title="TechSummus Hhs Identity Service" \
      org.opencontainers.image.description="TechSummus Hhs Identity Service" \
      org.opencontainers.image.authors="TechSummus <admin@techsummus.com>" \
      org.opencontainers.image.source="https://github.com/tech-summus/hhs-service-identity"

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

COPY ["./src/Hhs.IdentityService.Application/Hhs.IdentityService.Application.csproj", "./src/Hhs.IdentityService.Application/"]
COPY ["./src/Hhs.IdentityService.Domain/Hhs.IdentityService.Domain.csproj", "./src/Hhs.IdentityService.Domain/"]
COPY ["./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj", "./src/Hhs.IdentityService.EntityFrameworkCore/"]
COPY ["./src/Hhs.IdentityService.Http.Host/Hhs.IdentityService.Http.Host.csproj", "./src/Hhs.IdentityService.Http.Host/"]

ENV NUGET_CERT_REVOCATION_MODE=offline

# --- 🔑 BUILD ARG Credentials ---
ARG NUGET_USER
ARG NUGET_TOKEN

RUN dotnet nuget update source github \
        --username $NUGET_USER \
        --password $NUGET_TOKEN \
        --store-password-in-clear-text || true

RUN dotnet restore "./src/Hhs.IdentityService.Http.Host/Hhs.IdentityService.Http.Host.csproj" --force --verbosity minimal

# Copy source code
COPY --chown=1001:1001 ["./src/Hhs.IdentityService.Application/.", "./src/Hhs.IdentityService.Application/"]
COPY --chown=1001:1001 ["./src/Hhs.IdentityService.Domain/.", "./src/Hhs.IdentityService.Domain/"]
COPY --chown=1001:1001 ["./src/Hhs.IdentityService.EntityFrameworkCore/.", "./src/Hhs.IdentityService.EntityFrameworkCore/"]
COPY --chown=1001:1001 ["./src/Hhs.IdentityService.Http.Host/.", "./src/Hhs.IdentityService.Http.Host/"]

# Build
RUN dotnet build "./src/Hhs.IdentityService.Http.Host/Hhs.IdentityService.Http.Host.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

# (Opsiyonel) Test
RUN dotnet test "./src/Hhs.IdentityService.Http.Host/Hhs.IdentityService.Http.Host.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Publish
RUN dotnet publish "./src/Hhs.IdentityService.Http.Host/Hhs.IdentityService.Http.Host.csproj" \
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

ENTRYPOINT ["dotnet", "Hhs.IdentityService.Http.Host.dll"]
CMD []
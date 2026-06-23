FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Metadata labels
LABEL org.opencontainers.image.title="TechSummus Hhs Commercial Web Gateway" \
      org.opencontainers.image.description="TechSummus Hhs Commercial Web Gateway" \
      org.opencontainers.image.authors="TechSummus <admin@techsummus.com>" \
      org.opencontainers.image.source="https://github.com/tech-summus/hhs-gateway-commercial"

USER root

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_URLS='http://+:8080;https://+:8081;' \
    ASPNETCORE_HTTPS_PORT=8081 \
    ASPNETCORE_Kestrel__Certificates__Default__Path='/https/localhost.pfx' \
    ASPNETCORE_Kestrel__Certificates__Default__Password='e8202f07-66e5-4619-be07-72ba76fde97f' \
    ASPNETCORE_ENVIRONMENT='docker'

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy certificate file to ca-certificates
COPY ["./etc/dev-cert/localhost.pfx", "/https/localhost.pfx"]

# Add the certificate as a "trusted root"
RUN apt-get install -y ca-certificates \
    && cp /https/localhost.pfx /usr/local/share/ca-certificates/localhost.crt \
    && update-ca-certificates

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
COPY ["./public_key.xml", "./"]

COPY ["./src/Hhs.Gateway.Commercial.Http.Host/Hhs.Gateway.Commercial.Http.Host.csproj", "./src/Hhs.Gateway.Commercial.Http.Host/"]

ENV NUGET_CERT_REVOCATION_MODE=offline

# --- 🔑 BUILD ARG Credentials ---
ARG NUGET_USER
ARG NUGET_TOKEN

RUN dotnet nuget update source github \
        --username $NUGET_USER \
        --password $NUGET_TOKEN \
        --store-password-in-clear-text || true

RUN dotnet restore "./src/Hhs.Gateway.Commercial.Http.Host/Hhs.Gateway.Commercial.Http.Host.csproj" --force --verbosity minimal


# Copy source code
COPY --chown=1001:1001 ["./src/Hhs.Gateway.Commercial.Http.Host/.", "./src/Hhs.Gateway.Commercial.Http.Host/"]

# Build
RUN dotnet build "./src/Hhs.Gateway.Commercial.Http.Host/Hhs.Gateway.Commercial.Http.Host.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

# (Opsiyonel) Test
RUN dotnet test "./src/Hhs.Gateway.Commercial.Http.Host/Hhs.Gateway.Commercial.Http.Host.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Publish
RUN dotnet publish "./src/Hhs.Gateway.Commercial.Http.Host/Hhs.Gateway.Commercial.Http.Host.csproj" \
    --no-restore \
    --no-build \
    --configuration Release \
    --output /build-source/publish \
    /p:UseAppHost=false

# Final Runtime Image
FROM base AS final
WORKDIR /app
COPY --chown=1001:1001 --from=build-stage /build-source/publish .

# Certificate folder access permission
RUN chown -R 1001:1001 /https

# Runtime User
USER 1001

ENTRYPOINT ["dotnet", "Hhs.Gateway.Commercial.Http.Host.dll"]
CMD []
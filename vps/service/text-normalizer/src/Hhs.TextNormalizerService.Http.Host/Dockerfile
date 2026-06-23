FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Metadata labels
LABEL org.opencontainers.image.title="TechSummus Hhs TextNormalizer Service" \
      org.opencontainers.image.description="TechSummus Hhs TextNormalizer Service" \
      org.opencontainers.image.authors="TechSummus <admin@techsummus.com>" \
      org.opencontainers.image.source="https://github.com/tech-summus/hhs-service-text-normalizer"

USER root

WORKDIR /app
EXPOSE 8080

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_SHUTDOWNTIMEOUTSECONDS=60 \
    ASPNETCORE_URLS='http://+:8080;' \
    ASPNETCORE_ENVIRONMENT='docker'

# 1) Dependencies + fonts
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates curl gnupg \
      fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
      libnss3 libatk-bridge2.0-0 libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libgbm1 \
      libasound2t64 libpangocairo-1.0-0 libatk1.0-0 libcups2 libdrm2 libxfixes3 libxext6 \
      libx11-xcb1 libxcb1 libx11-6 \
    && rm -rf /var/lib/apt/lists/*

# 2) Install Google Chrome (NOT snap)
RUN install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://dl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /etc/apt/keyrings/google.gpg && \
    echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/google.gpg] http://dl.google.com/linux/chrome/deb/ stable main" \
      > /etc/apt/sources.list.d/google-chrome.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends google-chrome-stable && \
    rm -rf /var/lib/apt/lists/*

# 3) Puppeteer settings
ENV PUPPETEER_EXECUTABLE_PATH="/usr/bin/google-chrome" \
    XDG_CONFIG_HOME=/tmp/.chrome \
    XDG_CACHE_HOME=/tmp/.chrome

# sanity check (fail build if chrome missing)
RUN /usr/bin/google-chrome --version

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

COPY ["./src/Hhs.TextNormalizerService.Application/Hhs.TextNormalizerService.Application.csproj", "./src/Hhs.TextNormalizerService.Application/"]
COPY ["./src/Hhs.TextNormalizerService.Domain/Hhs.TextNormalizerService.Domain.csproj", "./src/Hhs.TextNormalizerService.Domain/"]
COPY ["./src/Hhs.TextNormalizerService.MongoDb/Hhs.TextNormalizerService.MongoDb.csproj", "./src/Hhs.TextNormalizerService.MongoDb/"]
COPY ["./src/Hhs.TextNormalizerService.Http.Host/Hhs.TextNormalizerService.Http.Host.csproj", "./src/Hhs.TextNormalizerService.Http.Host/"]

ENV NUGET_CERT_REVOCATION_MODE=offline

# --- 🔑 BUILD ARG Credentials ---
ARG NUGET_USER
ARG NUGET_TOKEN

RUN dotnet nuget update source github \
        --username $NUGET_USER \
        --password $NUGET_TOKEN \
        --store-password-in-clear-text || true

RUN dotnet restore "./src/Hhs.TextNormalizerService.Http.Host/Hhs.TextNormalizerService.Http.Host.csproj" --force --verbosity minimal

# Copy source code
COPY --chown=1001:1001 ["./src/Hhs.TextNormalizerService.Application/.", "./src/Hhs.TextNormalizerService.Application/"]
COPY --chown=1001:1001 ["./src/Hhs.TextNormalizerService.Domain/.", "./src/Hhs.TextNormalizerService.Domain/"]
COPY --chown=1001:1001 ["./src/Hhs.TextNormalizerService.MongoDb/.", "./src/Hhs.TextNormalizerService.MongoDb/"]
COPY --chown=1001:1001 ["./src/Hhs.TextNormalizerService.Http.Host/.", "./src/Hhs.TextNormalizerService.Http.Host/"]

# Build
RUN dotnet build "./src/Hhs.TextNormalizerService.Http.Host/Hhs.TextNormalizerService.Http.Host.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

# (Opsiyonel) Test
RUN dotnet test "./src/Hhs.TextNormalizerService.Http.Host/Hhs.TextNormalizerService.Http.Host.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Publish
RUN dotnet publish "./src/Hhs.TextNormalizerService.Http.Host/Hhs.TextNormalizerService.Http.Host.csproj" \
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

ENTRYPOINT ["dotnet", "Hhs.TextNormalizerService.Http.Host.dll"]
CMD []
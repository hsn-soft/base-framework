FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /packages
USER root

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-stage
WORKDIR /build-source

COPY ["./nuget.config", "./"]
COPY ["./common.props", "./"]
COPY ["./common.version.props", "./"]
COPY ["./common.test.props", "./"]

COPY ["./src/Hhs.IdentityService.Domain/Hhs.IdentityService.Domain.csproj", "./src/Hhs.IdentityService.Domain/"]
COPY ["./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj", "./src/Hhs.IdentityService.EntityFrameworkCore/"]

# Update NuGet source with secret credentials
RUN --mount=type=secret,id=NUGET_SECRET \
    export NUGET_SECRET=$(cat /run/secrets/NUGET_SECRET) && \
    dotnet nuget update source github \
        --username hsnsh \
        --password $NUGET_SECRET \
        --store-password-in-clear-text

RUN dotnet restore "./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj" --force --verbosity minimal

COPY ["./src/Hhs.IdentityService.Domain/.", "./src/Hhs.IdentityService.Domain/"]
COPY ["./src/Hhs.IdentityService.EntityFrameworkCore/.", "./src/Hhs.IdentityService.EntityFrameworkCore/"]

RUN dotnet build "./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj" --no-restore --no-incremental --verbosity minimal --configuration Release

RUN dotnet test "./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

# Pack with version
RUN --mount=type=secret,id=VERSION_NUMBER \
    --mount=type=secret,id=ACTION_NUMBER \
    dotnet pack "./src/Hhs.IdentityService.Domain/Hhs.IdentityService.Domain.csproj" \
        --no-restore --no-build --configuration Release \
        --output ./packages \
        -p:PackageVersion=$(cat /run/secrets/VERSION_NUMBER).$(cat /run/secrets/ACTION_NUMBER)

RUN --mount=type=secret,id=VERSION_NUMBER \
    --mount=type=secret,id=ACTION_NUMBER \
    dotnet pack "./src/Hhs.IdentityService.EntityFrameworkCore/Hhs.IdentityService.EntityFrameworkCore.csproj" \
        --no-restore --no-build --configuration Release \
        --output ./packages \
        -p:PackageVersion=$(cat /run/secrets/VERSION_NUMBER).$(cat /run/secrets/ACTION_NUMBER)

# Final stage
FROM base AS final
WORKDIR /packages
COPY --from=build-stage /build-source/packages .

# Push to private NuGet source using secrets securely
RUN --mount=type=secret,id=NUGET_SOURCE \
    --mount=type=secret,id=NUGET_SECRET \
    dotnet nuget push *.nupkg \
        --source $(cat /run/secrets/NUGET_SOURCE) \
        --api-key $(cat /run/secrets/NUGET_SECRET) \
        --skip-duplicate

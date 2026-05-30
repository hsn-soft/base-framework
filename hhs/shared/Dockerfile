FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /packages
USER root

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-stage
WORKDIR /build-source

COPY ["./nuget.config", "./"]
COPY ["./common.props", "./"]
COPY ["./common.version.props", "./"]
COPY ["./Hhs.Shared.sln", "./"]

COPY ["./src/Hhs.Shared.Contracts/Hhs.Shared.Contracts.csproj", "./src/Hhs.Shared.Contracts/"]
COPY ["./src/Hhs.Shared.Helper/Hhs.Shared.Helper.csproj", "./src/Hhs.Shared.Helper/"]
COPY ["./src/Hhs.Shared.Localization/Hhs.Shared.Localization.csproj", "./src/Hhs.Shared.Localization/"]
COPY ["./src/Hhs.Shared.Hosting/Hhs.Shared.Hosting.csproj", "./src/Hhs.Shared.Hosting/"]
COPY ["./src/Hhs.Shared.Hosting.Gateways/Hhs.Shared.Hosting.Gateways.csproj", "./src/Hhs.Shared.Hosting.Gateways/"]
COPY ["./src/Hhs.Shared.Hosting.Microservices/Hhs.Shared.Hosting.Microservices.csproj", "./src/Hhs.Shared.Hosting.Microservices/"]

RUN dotnet restore "./Hhs.Shared.sln" --force --verbosity minimal --configfile nuget.config

COPY ["./src/Hhs.Shared.Contracts/.", "./src/Hhs.Shared.Contracts/"]
COPY ["./src/Hhs.Shared.Helper/.", "./src/Hhs.Shared.Helper/"]
COPY ["./src/Hhs.Shared.Localization/.", "./src/Hhs.Shared.Localization/"]
COPY ["./src/Hhs.Shared.Hosting/.", "./src/Hhs.Shared.Hosting/"]
COPY ["./src/Hhs.Shared.Hosting.Gateways/.", "./src/Hhs.Shared.Hosting.Gateways/"]
COPY ["./src/Hhs.Shared.Hosting.Microservices/.", "./src/Hhs.Shared.Hosting.Microservices/"]

RUN dotnet build "./Hhs.Shared.sln" --no-restore --no-incremental --verbosity minimal --configuration Release

RUN dotnet test "./Hhs.Shared.sln" --no-restore --no-build --verbosity minimal --configuration Release --filter "category!=integration"

RUN --mount=type=secret,id=VERSION_NUMBER \
    export VERSION_NUMBER=$(cat /run/secrets/VERSION_NUMBER) && \
    echo ${VERSION_NUMBER} > ./version_number 

RUN --mount=type=secret,id=ACTION_NUMBER \
    export ACTION_NUMBER=$(cat /run/secrets/ACTION_NUMBER) && \
    echo ${ACTION_NUMBER} > ./action_number

RUN dotnet pack "./Hhs.Shared.sln" --no-restore --no-build --configuration Release --output ./packages -p:PackageVersion=$(cat ./version_number).$(cat ./action_number)

FROM base AS final
WORKDIR /packages
COPY --from=build-stage /build-source/packages .

RUN --mount=type=secret,id=NUGET_SOURCE \
    export NUGET_SOURCE=$(cat /run/secrets/NUGET_SOURCE) && \
    echo ${NUGET_SOURCE} > ./nuget_source

RUN --mount=type=secret,id=NUGET_SECRET \
    export NUGET_SECRET=$(cat /run/secrets/NUGET_SECRET) && \
    echo ${NUGET_SECRET} > ./nuget_secret

RUN dotnet nuget push *.nupkg --source $(cat ./nuget_source) --api-key $(cat ./nuget_secret) --skip-duplicate

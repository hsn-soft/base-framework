FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /packages
ENV NUGET_SOURCE='https://nuget.pkg.github.com/hsn-soft/index.json'
USER root

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-stage
WORKDIR /build-source

COPY ["./nuget.config", "./"]
COPY ["./common.props", "./"]
COPY ["./FeedR.Shared.sln", "./"]

COPY ["./src/FeedR.Shared.Hosting/FeedR.Shared.Hosting.csproj", "./src/FeedR.Shared.Hosting/"]
COPY ["./src/FeedR.Shared.Hosting.Microservices/FeedR.Shared.Hosting.Microservices.csproj", "./src/FeedR.Shared.Hosting.Microservices/"]

RUN dotnet restore "./FeedR.Shared.sln" --force --no-cache --verbosity normal

COPY ["./src/FeedR.Shared.Hosting/.", "./src/FeedR.Shared.Hosting/"]
COPY ["./src/FeedR.Shared.Hosting.Microservices/.", "./src/FeedR.Shared.Hosting.Microservices/"]

RUN dotnet build "./FeedR.Shared.sln" --no-restore --configuration Release --verbosity normal

RUN dotnet test "./FeedR.Shared.sln" --no-restore --no-build --configuration Release --verbosity normal

RUN --mount=type=secret,id=VERSION_NUMBER \
    export VERSION_NUMBER=$(cat /run/secrets/VERSION_NUMBER) && \
    echo ${VERSION_NUMBER} > ./version_number 

RUN --mount=type=secret,id=ACTION_NUMBER \
    export ACTION_NUMBER=$(cat /run/secrets/ACTION_NUMBER) && \
    echo ${ACTION_NUMBER} > ./action_number

RUN dotnet pack "./FeedR.Shared.sln" --no-restore --no-build --configuration Release --output ./packages -p:PackageVersion=$(cat ./version_number)-dev.$(cat ./action_number)

FROM base AS final
WORKDIR /packages
COPY --from=build-stage /build-source/packages .

RUN --mount=type=secret,id=NUGET_SECRET \
    export NUGET_SECRET=$(cat /run/secrets/NUGET_SECRET) && \
    echo ${NUGET_SECRET} > ./nuget_secret

RUN dotnet nuget push *.nupkg --source ${NUGET_SOURCE} --api-key $(cat ./nuget_secret) --skip-duplicate

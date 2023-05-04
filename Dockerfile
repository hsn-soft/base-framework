FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /packages
ENV NUGET_SOURCE='https://nuget.pkg.github.com/hsn-soft/index.json'
USER root

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-stage
WORKDIR /build-source

COPY ["./assets/hsn-base.png", "./assets/"]
COPY ["./README.md", "./"]
COPY ["./NuGet.Config", "./"]
COPY ["./common.props", "./"]
COPY ["./configureawait.props", "./"]
COPY ["./HsnSoft.Base.sln", "./"]

COPY ["./src/.", "./src/"]

RUN dotnet restore "./HsnSoft.Base.sln"

RUN dotnet build "./HsnSoft.Base.sln" --no-restore --configuration Release

#COPY ["./test/.", "./test/"]
#RUN dotnet test "./HsnSoft.Base.sln" --no-restore --no-build --configuration Release

RUN --mount=type=secret,id=VERSION_NUMBER \
    export VERSION_NUMBER=$(cat /run/secrets/VERSION_NUMBER) && \
    echo ${VERSION_NUMBER} > ./version_number 

RUN --mount=type=secret,id=ACTION_NUMBER \
    export ACTION_NUMBER=$(cat /run/secrets/ACTION_NUMBER) && \
    echo ${ACTION_NUMBER} > ./action_number

RUN dotnet pack "./HsnSoft.Base.sln" --no-restore --no-build --configuration Release --output ./packages \
    -p:PackageVersion=$(cat ./version_number)-dev.$(cat ./action_number) \
    --verbosity normal

FROM base AS final
WORKDIR /packages
COPY --from=build-stage /build-source/packages .

RUN --mount=type=secret,id=NUGET_SECRET \
    export NUGET_SECRET=$(cat /run/secrets/NUGET_SECRET) && \
    echo ${NUGET_SECRET} > ./nuget_secret

RUN dotnet nuget push *.nupkg --source ${NUGET_SOURCE} --api-key $(cat ./nuget_secret) --skip-duplicate

# One Dockerfile for every Edx host: the host is chosen at build time (see build-images.sh).
#   docker build -f samples/edx/deploy/Dockerfile --build-arg HOST=service/invoice/Edx.InvoiceService.Http.Host --build-arg ASSEMBLY=Edx.InvoiceService.Http.Host -t edx/invoice-service:dev .
# The context is the root of the repository (the hosts reference src/). Dockerfile.dockerignore keeps out everything the build does not need, the private key of the auth server among it:
# a key is never in an image, the auth server gets it as a secret mounted at /app/private_key.xml.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG HOST
ARG ASSEMBLY
WORKDIR /repo
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "samples/edx/${HOST}/${ASSEMBLY}.csproj" --configuration Release --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ASSEMBLY
WORKDIR /app
COPY --from=build /out .
# the environment of the sample that has the names of a compose network (appsettings.docker.json); Kubernetes and compose override the passwords
ENV ASPNETCORE_ENVIRONMENT=docker
EXPOSE 8080
# The host is started directly (its apphost, under one name for every host), not through `sh -c`: a shell drops the environment variables whose names have a hyphen, and the names of the connection strings
# (ConnectionStrings__EfCore-OrderService) have one, so the settings that Kubernetes and compose give would never reach the host.
RUN ln -s "/app/${ASSEMBLY}" /app/host
USER $APP_UID
ENTRYPOINT ["/app/host"]

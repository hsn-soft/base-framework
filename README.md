# HsnSoft Base Framework

![build and test](https://img.shields.io/github/actions/workflow/status/hsn-soft/base-framework/net-core-build-and-test.yml?branch=dev&style=flat-square&label=dev%20build%20and%20test)
[![NuGet](https://img.shields.io/nuget/v/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet (with prereleases)](https://img.shields.io/nuget/vpre/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet Download](https://img.shields.io/nuget/dt/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)

HsnSoft Base Framework is the infrastructure code a company's services repeat, written once and independent of any product.
A service picks the packages for the technologies it uses; nothing forces a broker, a database or a cache on it.

## Packages

An application takes only the capabilities it uses. Each package is one capability; an agent that reads MongoDB never loads
JWT, Swagger, a broker or a cache.

| Package | Capability | Depends on |
|---|---|---|
| `HsnSoft.Base.Core` | Bare essentials: shared exceptions, health-check tag, startup retry | - |
| `HsnSoft.Base.Logging` | Logger interfaces and models, masking of sensitive data, the `FrameworkLogger` settings; independent of any logging library ([docs](docs/Logging.md)) | Core |
| `HsnSoft.Base.Logging.Serilog` | Serilog provider: console, Graylog or file, Debug/Verbose stay in the console | Logging, Hosting |
| `HsnSoft.Base.Hosting` | Generic Host workers: `PeriodicBackgroundService` | Core |
| `HsnSoft.Base.Authentication` | JWT bearer authentication, claim names and `ClaimsPrincipal` helpers | Core |
| `HsnSoft.Base.AspNetCore` | Health endpoints (`/health/live`, `/health/ready`) | Core |
| `HsnSoft.Base.AspNetCore.WebApi` | Controllers, every answer in one `BaseResponse` type (results, validation, exceptions), global exception handler, request/response logging ([docs](docs/WebApi.md)) | Core |
| `HsnSoft.Base.AspNetCore.Swashbuckle` | Swagger document and UI (Swashbuckle), Bearer or OAuth2 button, off in production-like environments ([docs](docs/Swagger.md)) | Core |
| `HsnSoft.Base.AspNetCore.SwaggerGateway` | One Swagger UI for a gateway and the services behind it: their documents, with the gateway's address and paths ([docs](docs/Swagger.md)) | Swashbuckle |
| `HsnSoft.Base.EventBus` | Event bus core: message envelope, handlers, dispatcher, naming, publish retry ([docs](docs/EventBus.md)) | Core |
| `HsnSoft.Base.EventBus.RabbitMQ` | RabbitMQ broker, wire and topology compatible with the production services | EventBus |
| `HsnSoft.Base.EventBus.Kafka` | Kafka broker: producer, consumer group, dead-letter topic | EventBus |
| `HsnSoft.Base.Caching.StackExchangeRedis` | `IDistributedCache`, connection, health check | Core |
| `HsnSoft.Base.EntityFrameworkCore` | What the EF Core providers share: `DbContext` health check, startup schema creation | Core |
| `HsnSoft.Base.EntityFrameworkCore.PostgreSql` | PostgreSQL `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.EntityFrameworkCore.SqlServer` | SQL Server `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.BlobStoring` | `IBlobStore`: save, read and delete files in containers | Core |
| `HsnSoft.Base.BlobStoring.Minio` | MinIO (S3) implementation of `IBlobStore`; Azure would be a sibling package | BlobStoring |
| `HsnSoft.Base.MongoDB` | `IMongoDatabase` registration and health check | Core |
| `HsnSoft.Base` | Name holder that references `HsnSoft.Base.Core` | Core |

Abstractions and their implementations are separate packages (`EventBus` / `EventBus.RabbitMQ` / `EventBus.Kafka`, `BlobStoring` /
`BlobStoring.Minio`), so a product picks the broker or the storage per service and can change it with one registration. A package never depends on a sibling it does not need. Anything only one product needs (the Xyz gateway's rate
limit, for example) is that product's shared code, not a framework package.

## Tests

Every package has a unit test project (`test/<Package>.Tests`; `scripts/ci-check-test-projects.sh` fails the build when one is missing).
The code that only runs against a real service (brokers, databases, MinIO) is tested by `test/HsnSoft.Base.IntegrationTests`, which
starts the services in Docker with Testcontainers.

```bash
./scripts/run-tests.sh unit          # fast, no Docker
./scripts/run-tests.sh integration   # needs a running Docker
```

CI runs both and merges their coverage: a line counts as covered when either kind of test ran it, and the build fails below
`COVERAGE_THRESHOLD` (99 %) of the lines of `src`. Add a test project for every new package, and an integration test for every
new provider.

## The product decides the composition

The framework offers building blocks; the product's shared library chooses which ones its services use and in which order they
are registered and run. `samples/Xyz.Shared` does it for a JWT-secured web api; `samples/Agt.Scheduler.Host` is an agent that
takes only three packages.

```csharp
// product shared library: one composition for every service of the product
public static IServiceCollection AddXyzApi(this IServiceCollection services, IConfiguration configuration)
    => services.AddBaseWebApi().AddBaseSwaggerWithBearer("Orders API", "Enter a valid token", configuration).AddBaseHealthChecks()
               .AddBaseJwtAuthentication(configuration).AddBaseRedis(configuration);

public static WebApplication UseXyzApi(this WebApplication app, Action<WebApplication>? beforeAuthorization = null)
{
    app.UseBaseExceptionHandling(); app.UseBaseSwagger();
    app.UseBaseAuthentication(); beforeAuthorization?.Invoke(app); app.UseBaseAuthorization();
    app.MapBaseHealth();
    return app;
}
```

A service then adds its own store and broker:

```csharp
builder.Services
    .AddXyzApi(builder.Configuration)
    .AddBasePostgres<OrderDbContext>(builder.Configuration)
    .AddRabbitMqEventBus(builder.Configuration)
    .AddEventHandler<StockReserved, StockReservedHandler>();
```

## Configuration

| Section | Used by |
|---|---|
| `ConnectionStrings:Redis` / `Postgres` / `Mongo` | the matching `AddBase...` registration (the name is a parameter) |
| `Redis:InstanceName` | key prefix, e.g. `myproduct:` |
| `RabbitMq:Connection` (`HostName`, `Port`, `UserName`, `Password`, `VirtualHost`) and `RabbitMq:EventBus` (`ExchangeName`, `ConsumerClientName`, `ErrorClientName`, ...) | `AddRabbitMqEventBus` |
| `Jwt` (`Issuer`, `Audience`, `Key`) | `AddBaseJwtAuthentication` |
| `EventBusOptions` (retries, naming, timeouts), see [docs/EventBus.md](docs/EventBus.md) | `AddRabbitMqEventBus` |

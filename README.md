# HsnSoft Base Framework

**Development** (`dev`, checked every Monday and by hand; [what the badges say](docs/adr/0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md))

[![build and test](https://img.shields.io/github/actions/workflow/status/hsn-soft/base-framework/ci-control-workflow.yml?branch=dev&style=flat-square&label=build%20and%20test)](https://github.com/hsn-soft/base-framework/actions/workflows/ci-control-workflow.yml)
[![coverage](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fhsn-soft%2Fbase-framework%2Fbadges%2Fcoverage.json&style=flat-square)](https://github.com/hsn-soft/base-framework/actions/workflows/ci-control-workflow.yml)
[![unit tests](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fhsn-soft%2Fbase-framework%2Fbadges%2Funit-tests.json&style=flat-square)](https://github.com/hsn-soft/base-framework/actions/workflows/ci-control-workflow.yml)
[![integration tests](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fhsn-soft%2Fbase-framework%2Fbadges%2Fintegration-tests.json&style=flat-square)](https://github.com/hsn-soft/base-framework/actions/workflows/ci-control-workflow.yml)
[![checked](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fhsn-soft%2Fbase-framework%2Fbadges%2Fchecked.json&style=flat-square)](https://github.com/hsn-soft/base-framework/actions/workflows/ci-control-workflow.yml)

**NuGet**

[![NuGet](https://img.shields.io/nuget/v/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet (with prereleases)](https://img.shields.io/nuget/vpre/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet Download](https://img.shields.io/nuget/dt/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)

HsnSoft Base Framework is the infrastructure code a company's services repeat, written once and independent of any product.
A service picks the packages for the technologies it uses; nothing forces a broker, a database or a cache on it.

## Documentation

[`docs/`](docs/README.md) has the reference documents and, in [`docs/adr`](docs/adr/README.md), the architecture decision records: why the repository is the way it is.

## Packages

An application takes only the capabilities it uses. Each package is one capability; an agent that reads MongoDB never loads
JWT, Swagger, a broker or a cache.

| Package | Capability | Depends on |
|---|---|---|
| `HsnSoft.Base.Core` | Bare essentials: shared exceptions, health-check tag, startup retry, `ICurrentUser`, `ApplicationService` (logger and current user for the application layer), `Guard` (checks of the domain), the contracts of soft delete and localization (`ISoftDelete`, `IMessageLocalizer`, ...) with their defaults | - |
| `HsnSoft.Base.Logging` | Logger interfaces and models, masking of sensitive data, the settings every provider reads (`Logging:LogLevel` for the console, `PersistentLogger` for what is kept); independent of any logging library ([docs](docs/Logging.md)) | Core |
| `HsnSoft.Base.Logging.Serilog` | Serilog provider: console, Graylog or file, Debug/Verbose stay in the console | Logging, Hosting |
| `HsnSoft.Base.Logging.NLog` | NLog provider with the same settings, routing, fields and masking as the Serilog one: console, Graylog (GELF over HTTP) or file ([ADR-0076](docs/adr/0076-more-providers-and-the-rules-every-provider-shares.md)) | Logging |
| `HsnSoft.Base.Hosting` | Generic Host workers: `PeriodicBackgroundService` | Core |
| `HsnSoft.Base.Auditing` | Who made a record and when, who changed it last, who deleted it: the audit contracts an entity and its DTO share (`IAuditedObject`, `IFullAuditedObject`, ...), the marks an entity takes (`IMarkCreated`, ...) and the setter that fills them with the clock and the current user ([docs](docs/EntityConcepts.md)) | Core |
| `HsnSoft.Base.Data` | What every data store shares whatever the database: the switch of the data filters (soft delete, tenants, ...) that follows the flow of the code (`IDataFilter`) | Core |
| `HsnSoft.Base.Ddd.Domain` | The domain layer's building blocks: entity bases with audit and soft delete, the repository contract with paging, filtering and safe sorting; registers the concepts of an entity (`AddBaseEntityConcepts`) | Auditing, Data |
| `HsnSoft.Base.Ddd.Application` | The application layer's building blocks: the DTOs of an entity, of a list request and of its result; the paging model | Ddd.Domain |
| `HsnSoft.Base.MultiTenancy` | Tenants, for the service that wants them: the entity of a tenant (`IMultiTenant`), the tenant of the request (`ICurrentTenant`) and the sources it comes from (`ITenantSource`); the rest of the capability is in the four packages below ([docs](docs/EntityConcepts.md#tenants-a-package-of-their-own)) | Core |
| `HsnSoft.Base.MultiTenancy.Authentication` | The tenant of a request from the claims of its token (`tenant_id`, `is_system_tenant`, `allowed_tenant_id`) | MultiTenancy |
| `HsnSoft.Base.MultiTenancy.EventBus` | The tenant travels with the messages (headers) and a handler runs in the tenant of its message | EventBus, MultiTenancy |
| `HsnSoft.Base.MultiTenancy.EntityFrameworkCore` | `MultiTenantDbContext` (the tenant filter) and the interceptor that gives a new entity the tenant | EntityFrameworkCore, MultiTenancy |
| `HsnSoft.Base.MultiTenancy.MongoDB` | The tenant filter and the tenant of a new document for the MongoDB repositories | MongoDB, MultiTenancy |
| `HsnSoft.Base.Authentication` | JWT bearer authentication (a shared key, or the RSA public key of the auth server), claim names and `ClaimsPrincipal` helpers | Core |
| `HsnSoft.Base.Authorization` | Permissions: `[PermissionAuthorize]` on an endpoint, the checker for an operation or a limit on a value, the grants read from the distributed cache under a scope (the tenant) ([docs](docs/Authorization.md)) | Core |
| `HsnSoft.Base.AspNetCore` | Health endpoints (`/health/live`, `/health/ready`) | Core |
| `HsnSoft.Base.AspNetCore.WebApi` | Controllers, every answer in one `BaseResponse` type (results, validation, exceptions), global exception handler, `ApiControllerBase` (logger and current user), request/response logging ([docs](docs/WebApi.md)) | AspNetCore |
| `HsnSoft.Base.AspNetCore.Swashbuckle` | Swagger document and UI (Swashbuckle), Bearer or OAuth2 button, off in production-like environments ([docs](docs/Swagger.md)) | AspNetCore |
| `HsnSoft.Base.AspNetCore.SwaggerGateway` | One Swagger UI for a gateway and the services behind it: their documents, with the gateway's address and paths ([docs](docs/Swagger.md)) | Swashbuckle |
| `HsnSoft.Base.EventBus` | Event bus core: message envelope, handlers, dispatcher, naming, publish retry ([docs](docs/EventBus.md)) | Core |
| `HsnSoft.Base.EventBus.RabbitMQ` | RabbitMQ broker, wire and topology compatible with the production services | EventBus |
| `HsnSoft.Base.EventBus.Kafka` | Kafka broker: producer, consumer group, dead-letter topic | EventBus |
| `HsnSoft.Base.EventBus.EntityFrameworkCore` | The inbox of a service on EF Core, opt in per service: a message that comes twice is handled once ([docs](docs/EventBus.md#the-inbox-a-message-that-comes-twice-is-handled-once)) | EventBus, EntityFrameworkCore |
| `HsnSoft.Base.EventBus.MongoDB` | The inbox of a service on MongoDB, opt in per service | EventBus, MongoDB |
| `HsnSoft.Base.Caching.StackExchangeRedis` | `IDistributedCache`, connection, health check | Core |
| `HsnSoft.Base.EntityFrameworkCore` | What the EF Core providers share: `DbContext` health check, startup schema creation, `BaseDbContext` and the interceptor for audit and soft delete, the generic repository (`EfCoreRepository`) | Ddd.Domain |
| `HsnSoft.Base.EntityFrameworkCore.PostgreSql` | PostgreSQL `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.EntityFrameworkCore.SqlServer` | SQL Server `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.EntityFrameworkCore.MySql` | MySQL `DbContext` with retries (Oracle's `MySql.EntityFrameworkCore`) | EntityFrameworkCore |
| `HsnSoft.Base.BlobStoring` | `IBlobStore`: save, read and delete files in containers | Core |
| `HsnSoft.Base.BlobStoring.Minio` | MinIO (S3) implementation of `IBlobStore` | BlobStoring |
| `HsnSoft.Base.BlobStoring.Azure` | Azure Blob Storage implementation of `IBlobStore` (a connection string, or the identity of the host) | BlobStoring |
| `HsnSoft.Base.BlobStoring.Aws` | Amazon S3 implementation of `IBlobStore` (the role of the host; a `ServiceUrl` for any store with the S3 API) | BlobStoring |
| `HsnSoft.Base.Localization` | JSON texts embedded in the assemblies, merged from the innermost layer out, found by key in the language of the request | Core |
| `HsnSoft.Base.MongoDB` | `IMongoDatabase` registration and health check, `MongoEntityConcepts` (audit and soft delete for repositories), the generic repository (`MongoRepository`) | Ddd.Domain |
| `HsnSoft.Base` | Name holder that references `HsnSoft.Base.Core` | Core |

Abstractions and their implementations are separate packages (`EventBus` / `EventBus.RabbitMQ` / `EventBus.Kafka`, `BlobStoring` /
`BlobStoring.Minio` / `.Azure` / `.Aws`, `Logging.Serilog` / `.NLog`), so a product picks the broker or the storage per service and can change it with one registration. A package never depends on a sibling it does not need. Anything only one product needs (a product's gateway rate
limit, for example) is that product's shared code, not a framework package.

## Tests

Every package has a unit test project (`test/<Package>.Tests`; `scripts/ci-detector/ci-check-test-project-exist.sh` fails the build when one is missing).
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
are registered and run. A product's shared hosting project does it for a JWT-secured web api, and a host that needs less takes fewer packages
(the gateways, for one, have no database or broker).

```csharp
// product shared library: one composition for every service of the product
public static IServiceCollection AddShopApi(this IServiceCollection services, IConfiguration configuration)
    => services.AddBaseWebApi().AddBaseSwaggerWithBearer("Orders API", "Enter a valid token", configuration).AddBaseHealthChecks()
               .AddBaseJwtAuthentication(configuration).AddBaseRedis(configuration);

public static WebApplication UseShopApi(this WebApplication app, Action<WebApplication>? beforeAuthorization = null)
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
    .AddShopApi(builder.Configuration)
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


# HsnSoft Base Framework

![build and test](https://img.shields.io/github/actions/workflow/status/hsn-soft/base-framework/net-core-build-and-test.yml?branch=dev&style=flat-square&label=dev%20build%20and%20test)
[![NuGet](https://img.shields.io/nuget/v/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet (with prereleases)](https://img.shields.io/nuget/vpre/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)
[![NuGet Download](https://img.shields.io/nuget/dt/HsnSoft.Base.Core.svg?style=flat-square)](https://www.nuget.org/packages/HsnSoft.Base.Core)

HsnSoft Base Framework is the infrastructure code a company's services repeat, written once and independent of any product.
A service picks the packages for the technologies it uses; nothing forces a broker, a database or a cache on it.

## Packages

An application takes only the capabilities it uses. Each package is one capability; an agent that reads MongoDB never loads
JWT, OpenAPI, a broker or a cache.

| Package | Capability | Depends on |
|---|---|---|
| `HsnSoft.Base.Core` | Bare essentials: shared exceptions, health-check tag, startup retry | - |
| `HsnSoft.Base.Hosting` | Generic Host workers: `PeriodicBackgroundService` | Core |
| `HsnSoft.Base.Authentication` | JWT bearer authentication, claim names and `ClaimsPrincipal` helpers | Core |
| `HsnSoft.Base.AspNetCore` | Health endpoints (`/health/live`, `/health/ready`) | Core |
| `HsnSoft.Base.AspNetCore.WebApi` | Problem-details errors, global exception handler, the OpenAPI document | Core |
| `HsnSoft.Base.EventBus` | Event bus contracts, dispatcher with retry, wire format | Core |
| `HsnSoft.Base.EventBus.RabbitMQ` | RabbitMQ broker | EventBus |
| `HsnSoft.Base.EventBus.Kafka` | Kafka broker: producer, consumer group, dead-letter topic | EventBus |
| `HsnSoft.Base.Caching.StackExchangeRedis` | `IDistributedCache`, connection, health check | Core |
| `HsnSoft.Base.EntityFrameworkCore` | What the EF Core providers share: `DbContext` health check, startup schema creation | Core |
| `HsnSoft.Base.EntityFrameworkCore.PostgreSql` | PostgreSQL `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.EntityFrameworkCore.SqlServer` | SQL Server `DbContext` with retries | EntityFrameworkCore |
| `HsnSoft.Base.BlobStoring` | `IBlobStore`: save, read and delete files in containers | - |
| `HsnSoft.Base.BlobStoring.Minio` | MinIO (S3) implementation of `IBlobStore`; Azure would be a sibling package | BlobStoring, Core |
| `HsnSoft.Base.MongoDB` | `IMongoDatabase` registration and health check | Core |
| `HsnSoft.Base` | Name holder that references `HsnSoft.Base.Core` | Core |

Abstractions and their implementations are separate packages (`EventBus` / `EventBus.RabbitMQ` / `EventBus.Kafka`, `BlobStoring` /
`BlobStoring.Minio`), so a product picks the broker or the storage per service and can change it with one registration. A package never depends on a sibling it does not need. Anything only one product needs (the Xyz gateway's rate
limit, for example) is that product's shared code, not a framework package.

## The product decides the composition

The framework offers building blocks; the product's shared library chooses which ones its services use and in which order they
are registered and run. `samples/Xyz.Shared` does it for a JWT-secured web api; `samples/Agt.Scheduler.Host` is an agent that
takes only three packages.

```csharp
// product shared library: one composition for every service of the product
public static IServiceCollection AddXyzApi(this IServiceCollection services, IConfiguration configuration)
    => services.AddBaseWebApi().AddBaseOpenApi().AddBaseHealthChecks()
               .AddBaseJwtAuthentication(configuration).AddBaseRedis(configuration);

public static WebApplication UseXyzApi(this WebApplication app, Action<WebApplication>? beforeAuthorization = null)
{
    app.UseBaseExceptionHandling(); app.MapBaseOpenApi();
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
    .AddRabbitMqEventBus(builder.Configuration, "order-service")
    .AddEventHandler<StockReserved, StockReservedHandler>();
```

## Configuration

| Section | Used by |
|---|---|
| `ConnectionStrings:Redis` / `Postgres` / `Mongo` | the matching `AddBase...` registration (the name is a parameter) |
| `Redis:InstanceName` | key prefix, e.g. `myproduct:` |
| `RabbitMq` (`Host`, `Port`, `UserName`, `Password`, `VirtualHost`, `ExchangeName`) | `AddRabbitMqEventBus` |
| `Jwt` (`Issuer`, `Audience`, `Key`) | `AddBaseJwtAuthentication` |
| `EventBus` options in code (`ServiceName`, retries) | `AddRabbitMqEventBus` |

# Abc: the references of the projects

What references what in the Abc sample, read from the project files (a line `HsnSoft.Base.X (framework)` is a package of the framework, where the tree stops, its own graph is in [ADR-0004](../../docs/adr/0004-core-once-and-the-dependency-graph.md); `[Name]` is an
external NuGet package). The rules are [ADR-0028](../../docs/adr/0028-shared-hosting-projects.md), [ADR-0068](../../docs/adr/0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md), [ADR-0069](../../docs/adr/0069-who-references-which-shared-project-in-abc.md) and
[ADR-0070](../../docs/adr/0070-a-framework-package-brings-what-its-api-exposes.md).

## The rules

- A **shared project** has one concern and builds on the framework only (`Helper` on `Core`, `Contracts` on `EventBus`, `Localization` on `Localization`, `Hosting` on the framework projects it composes and the logger the Abc hosts use (`Logging.Serilog`), `Hosting.Microservices` and
  `Hosting.Gateways` on `Hosting`). `Hosting` knows nothing of a service's layers (no `Ddd.Application`, no paging) and chooses neither the languages nor the cache of a host (no `Localization`, no `Redis`): a host chooses them itself ([ADR-0072](../../docs/adr/0072-a-host-chooses-its-localization-and-its-cache.md)).
- A **microservice** is a Domain, an Application and one or more persistence (infrastructure) class libraries, and a Host. **Required** means every service has it, **optional** that a service takes it when it needs it:

| Project | Required | Optional |
|---|---|---|
| **Domain** | `HsnSoft.Base.Ddd.Domain` | `Abc.Shared.Helper`, `Abc.Shared.Localization`, `HsnSoft.Base.MultiTenancy` (entities with tenants) |
| **Application** | `HsnSoft.Base.Ddd.Application`, `Abc.Shared.Contracts`, its Domain | - |
| **Infrastructure, EF Core** | its Domain, a provider of `HsnSoft.Base.EntityFrameworkCore` (it brings the base package) | `EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer` (at least one), `EventBus.EntityFrameworkCore` (the inbox), `MultiTenancy.EntityFrameworkCore` (tenants) |
| **Infrastructure, MongoDB** | its Domain, `HsnSoft.Base.MongoDB` | `EventBus.MongoDB` (the inbox), `MultiTenancy.MongoDB` (tenants) |
| **Host** | `Abc.Shared.Hosting.Microservices`, its Application, its infrastructure projects | what it chooses for itself: `HsnSoft.Base.Caching.StackExchangeRedis`, and for tenants `MultiTenancy.Authentication` and `MultiTenancy.EventBus` (the texts in several languages come with the Domain's shared texts) |

  An Application project has no external package of its own: the dependency-injection and localization abstractions come with the framework's projects, and `Microsoft.EntityFrameworkCore.Relational` comes with `HsnSoft.Base.EntityFrameworkCore`
  ([ADR-0070](../../docs/adr/0070-a-framework-package-brings-what-its-api-exposes.md)). A Host references only what it chooses for itself (the cache): the logger comes through the hosting, the texts in several languages through its Domain.
- **Tenants are a choice of the service** ([ADR-0074](../../docs/adr/0074-multitenancy-is-a-package-of-its-own.md)): the identity, order and product services have them (their Domain, their infrastructure and their Host reference the `MultiTenancy` packages); **the shipment service has none** and references none of them, and the shared hosting projects know nothing of tenants.
- The **auth server** is a host that works on the users of the identity service: `Hosting.Microservices`, `Helper`, `Localization`, `Caching.StackExchangeRedis` (its choice) and the identity infrastructure project.
- A **gateway** references `Hosting.Gateways` only (the commercial one also chooses Redis); neither has texts of its own, so neither chooses the localization.

## The shared projects

### Abc.Shared.Helper

```
Abc.Shared.Helper
└── HsnSoft.Base.Core   (framework)
```

### Abc.Shared.Contracts

```
Abc.Shared.Contracts
└── HsnSoft.Base.EventBus   (framework)
```

### Abc.Shared.Localization

```
Abc.Shared.Localization
└── HsnSoft.Base.Localization   (framework)
```

### Abc.Shared.Hosting

```
Abc.Shared.Hosting
├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
├── HsnSoft.Base.AspNetCore.WebApi   (framework)
├── HsnSoft.Base.Authentication   (framework)
└── HsnSoft.Base.Logging.Serilog   (framework)
```

### Abc.Shared.Hosting.Microservices

```
Abc.Shared.Hosting.Microservices
├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
└── Abc.Shared.Hosting
    ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
    ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
    ├── HsnSoft.Base.Authentication   (framework)
    └── HsnSoft.Base.Logging.Serilog   (framework)
```

### Abc.Shared.Hosting.Gateways

```
Abc.Shared.Hosting.Gateways
├── HsnSoft.Base.AspNetCore.SwaggerGateway   (framework)
├── Abc.Shared.Hosting
│   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│   ├── HsnSoft.Base.Authentication   (framework)
│   └── HsnSoft.Base.Logging.Serilog   (framework)
└── [Yarp.ReverseProxy]
```

## A microservice

The order service is the example; the others have the same shape (what differs is said under their tree).

### Abc.OrderService.Http.Host (the Host, with everything below it)

```
Abc.OrderService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Abc.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
│   └── Abc.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Abc.OrderService.Application
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Abc.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Abc.OrderService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Abc.Shared.Helper
│       │   └── HsnSoft.Base.Core   (framework)
│       └── Abc.Shared.Localization
│           └── HsnSoft.Base.Localization   (framework)
└── Abc.OrderService.EntityFrameworkCore
    ├── HsnSoft.Base.EntityFrameworkCore.PostgreSql   (framework)
    ├── HsnSoft.Base.EventBus.EntityFrameworkCore   (framework)
    ├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
    └── Abc.OrderService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Abc.Shared.Helper
        │   └── HsnSoft.Base.Core   (framework)
        └── Abc.Shared.Localization
            └── HsnSoft.Base.Localization   (framework)
```

The layers on their own:

```
Abc.OrderService.Domain              -> Ddd.Domain, Shared.Helper, Shared.Localization
Abc.OrderService.Application         -> OrderService.Domain, Shared.Contracts, Ddd.Application
Abc.OrderService.EntityFrameworkCore -> OrderService.Domain, EntityFrameworkCore.PostgreSql, EventBus.EntityFrameworkCore
Abc.OrderService.Http.Host           -> OrderService.Application, OrderService.EntityFrameworkCore, Shared.Hosting.Microservices, Caching.StackExchangeRedis (its choice)
```

### The others

#### Abc.IdentityService.Http.Host

SQL Server for the users and PostgreSQL for the history (two contexts, one project), the inbox on SQL Server.

```
Abc.IdentityService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Abc.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
│   └── Abc.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Abc.IdentityService.Application
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Abc.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Abc.IdentityService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Abc.Shared.Helper
│       │   └── HsnSoft.Base.Core   (framework)
│       └── Abc.Shared.Localization
│           └── HsnSoft.Base.Localization   (framework)
└── Abc.IdentityService.EntityFrameworkCore
    ├── HsnSoft.Base.EntityFrameworkCore.PostgreSql   (framework)
    ├── HsnSoft.Base.EntityFrameworkCore.SqlServer   (framework)
    ├── HsnSoft.Base.EventBus.EntityFrameworkCore   (framework)
    ├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
    └── Abc.IdentityService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Abc.Shared.Helper
        │   └── HsnSoft.Base.Core   (framework)
        └── Abc.Shared.Localization
            └── HsnSoft.Base.Localization   (framework)
```

#### Abc.ProductService.Http.Host

MongoDB for the products and for the stock history (its own database), the inbox on MongoDB.

```
Abc.ProductService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Abc.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
│   └── Abc.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Abc.ProductService.Application
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Abc.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Abc.ProductService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Abc.Shared.Helper
│       │   └── HsnSoft.Base.Core   (framework)
│       └── Abc.Shared.Localization
│           └── HsnSoft.Base.Localization   (framework)
└── Abc.ProductService.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Abc.ProductService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Abc.Shared.Helper
        │   └── HsnSoft.Base.Core   (framework)
        └── Abc.Shared.Localization
            └── HsnSoft.Base.Localization   (framework)
```

#### Abc.ShipmentService.Http.Host

two infrastructure projects: EF Core (PostgreSQL) for the history of the shipments and MongoDB for the shipments, the customers and the payments; no inbox.

```
Abc.ShipmentService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── Abc.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
│   └── Abc.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Abc.ShipmentService.Application
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Abc.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Abc.ShipmentService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── Abc.Shared.Helper
│       │   └── HsnSoft.Base.Core   (framework)
│       └── Abc.Shared.Localization
│           └── HsnSoft.Base.Localization   (framework)
├── Abc.ShipmentService.EntityFrameworkCore
│   ├── HsnSoft.Base.EntityFrameworkCore.PostgreSql   (framework)
│   └── Abc.ShipmentService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── Abc.Shared.Helper
│       │   └── HsnSoft.Base.Core   (framework)
│       └── Abc.Shared.Localization
│           └── HsnSoft.Base.Localization   (framework)
└── Abc.ShipmentService.MongoDB
    ├── HsnSoft.Base.MongoDB   (framework)
    └── Abc.ShipmentService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── Abc.Shared.Helper
        │   └── HsnSoft.Base.Core   (framework)
        └── Abc.Shared.Localization
            └── HsnSoft.Base.Localization   (framework)
```

## Gateways

### Abc.Gateway.Commercial.Host

```
Abc.Gateway.Commercial.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
└── Abc.Shared.Hosting.Gateways
    ├── HsnSoft.Base.AspNetCore.SwaggerGateway   (framework)
    ├── Abc.Shared.Hosting
    │   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
    │   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
    │   ├── HsnSoft.Base.Authentication   (framework)
    │   └── HsnSoft.Base.Logging.Serilog   (framework)
    └── [Yarp.ReverseProxy]
```

### Abc.Gateway.Public.Host

```
Abc.Gateway.Public.Host
└── Abc.Shared.Hosting.Gateways
    ├── HsnSoft.Base.AspNetCore.SwaggerGateway   (framework)
    ├── Abc.Shared.Hosting
    │   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
    │   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
    │   ├── HsnSoft.Base.Authentication   (framework)
    │   └── HsnSoft.Base.Logging.Serilog   (framework)
    └── [Yarp.ReverseProxy]
```

## Apps

### Abc.AuthServer.Host

```
Abc.AuthServer.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── Abc.Shared.Helper
│   └── HsnSoft.Base.Core   (framework)
├── Abc.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.EventBus.RabbitMQ   (framework)
│   └── Abc.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Abc.Shared.Localization
│   └── HsnSoft.Base.Localization   (framework)
└── Abc.IdentityService.EntityFrameworkCore
    ├── HsnSoft.Base.EntityFrameworkCore.PostgreSql   (framework)
    ├── HsnSoft.Base.EntityFrameworkCore.SqlServer   (framework)
    ├── HsnSoft.Base.EventBus.EntityFrameworkCore   (framework)
    ├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
    └── Abc.IdentityService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Abc.Shared.Helper
        │   └── HsnSoft.Base.Core   (framework)
        └── Abc.Shared.Localization
            └── HsnSoft.Base.Localization   (framework)
```

### Abc.CommercialWeb.Host and Abc.PublicWeb.Host

Pages that call the gateways over HTTP: they use no shared Abc project, only the framework (and their own logger).

```
Abc.CommercialWeb.Host
├── HsnSoft.Base.AspNetCore.WebApi   (framework)
└── HsnSoft.Base.Logging.Serilog   (framework)
```

```
Abc.PublicWeb.Host
├── HsnSoft.Base.AspNetCore.WebApi   (framework)
└── HsnSoft.Base.Logging.Serilog   (framework)
```

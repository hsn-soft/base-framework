# Edx: the references of the projects

What references what in the Edx sample, read from the project files (a line `HsnSoft.Base.X (framework)` is a package of the framework, where the tree stops, its own graph is in [ADR-0004](../../docs/adr/0004-core-once-and-the-dependency-graph.md); `[Name]` is an
external NuGet package). The rules are those of Abc ([abc/REFERENCES.md](../abc/REFERENCES.md): [ADR-0028](../../docs/adr/0028-shared-hosting-projects.md), [ADR-0069](../../docs/adr/0069-who-references-which-shared-project-in-abc.md),
[ADR-0070](../../docs/adr/0070-a-framework-package-brings-what-its-api-exposes.md)), and the decisions of this system are [ADR-0078](../../docs/adr/0078-the-edx-sample-a-file-intake-system.md), [ADR-0079](../../docs/adr/0079-grpc-and-wcf-are-entry-points-of-a-host.md) and
[ADR-0080](../../docs/adr/0080-edx-identity-tenants-roles-and-signing-in-by-tenant.md).

## What is different from Abc

- **Only the microservices reach Kafka.** `Edx.Shared.Hosting.Microservices` references `HsnSoft.Base.EventBus.Kafka`; the gateway and the auth server are built on `Edx.Shared.Hosting` and `Edx.Shared.Hosting.Gateways`, which reference no broker. The identity service is a microservice that publishes and handles
  nothing, so its host does not call `AddEdxEventBus`.
- **Redis** is chosen by the host (`HsnSoft.Base.Caching.StackExchangeRedis`) in the gateway, the auth server and every microservice. **No host has localization.**
- **The intake service speaks REST, gRPC and WCF**: its Host references `Grpc.AspNetCore`, `CoreWCF.Http` and `CoreWCF.Primitives`, and the blob store it chose (`HsnSoft.Base.BlobStoring.Minio`); its Application references `HsnSoft.Base.BlobStoring` (the abstraction) only.
- **The auth server reads the identity database** (it references `Edx.IdentityService.EntityFrameworkCore`, as the Abc auth server references the identity one) and issues the token with the tenant in it.
- **Permissions** are checked by the microservices (`Edx.Shared.Hosting.Microservices` references `HsnSoft.Base.Authorization` and `MultiTenancy`: the scope of a grant is the tenant) and written by the identity service, which references `Authorization` too ([ADR-0081](../../docs/adr/0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md)). The auth server and the gateway have none.
- **Tenants are in every service** (Domain: `MultiTenancy`; MongoDB: `MultiTenancy.MongoDB`; EF Core: `MultiTenancy.EntityFrameworkCore`; Host: `MultiTenancy.Authentication`, and `MultiTenancy.EventBus` where events travel).

## The shared projects

### Edx.Shared.Contracts

```
Edx.Shared.Contracts
└── HsnSoft.Base.EventBus   (framework)
```

### Edx.Shared.Helper

```
Edx.Shared.Helper
└── HsnSoft.Base.Core   (framework)
```

### Edx.Shared.Hosting.Gateways

```
Edx.Shared.Hosting.Gateways
├── [Yarp.ReverseProxy]
├── HsnSoft.Base.AspNetCore.SwaggerGateway   (framework)
└── Edx.Shared.Hosting
    ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
    ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
    ├── HsnSoft.Base.Authentication   (framework)
    └── HsnSoft.Base.Logging.Serilog   (framework)
```

### Edx.Shared.Hosting.Microservices

```
Edx.Shared.Hosting.Microservices
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.EventBus.Kafka   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
├── Edx.Shared.Helper
│   └── HsnSoft.Base.Core   (framework)
└── Edx.Shared.Hosting
    ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
    ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
    ├── HsnSoft.Base.Authentication   (framework)
    └── HsnSoft.Base.Logging.Serilog   (framework)
```

### Edx.Shared.Hosting

```
Edx.Shared.Hosting
├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
├── HsnSoft.Base.AspNetCore.WebApi   (framework)
├── HsnSoft.Base.Authentication   (framework)
└── HsnSoft.Base.Logging.Serilog   (framework)
```

## The services

### Edx.IdentityService.Application

```
Edx.IdentityService.Application
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.Ddd.Application   (framework)
└── Edx.IdentityService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.IdentityService.Domain

```
Edx.IdentityService.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
└── Edx.Shared.Helper
    └── HsnSoft.Base.Core   (framework)
```

### Edx.IdentityService.EntityFrameworkCore

```
Edx.IdentityService.EntityFrameworkCore
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.EntityFrameworkCore.SqlServer   (framework)
├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
└── Edx.IdentityService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.IdentityService.Http.Host

```
Edx.IdentityService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── Edx.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.EventBus.Kafka   (framework)
│   ├── HsnSoft.Base.MultiTenancy   (framework)
│   ├── Edx.Shared.Helper
│   │   └── HsnSoft.Base.Core   (framework)
│   └── Edx.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Edx.IdentityService.Application
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   └── Edx.IdentityService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.IdentityService.EntityFrameworkCore
    ├── HsnSoft.Base.Authorization   (framework)
    ├── HsnSoft.Base.EntityFrameworkCore.SqlServer   (framework)
    ├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
    └── Edx.IdentityService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.IntakeService.Application

```
Edx.IntakeService.Application
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.BlobStoring   (framework)
├── HsnSoft.Base.Ddd.Application   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
└── Edx.IntakeService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.IntakeService.Domain

```
Edx.IntakeService.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
└── Edx.Shared.Helper
    └── HsnSoft.Base.Core   (framework)
```

### Edx.IntakeService.Http.Host

```
Edx.IntakeService.Http.Host
├── [CoreWCF.Http]
├── [CoreWCF.Primitives]
├── [Grpc.AspNetCore]
├── HsnSoft.Base.BlobStoring.Minio   (framework)
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Edx.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.EventBus.Kafka   (framework)
│   ├── HsnSoft.Base.MultiTenancy   (framework)
│   ├── Edx.Shared.Helper
│   │   └── HsnSoft.Base.Core   (framework)
│   └── Edx.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Edx.IntakeService.Application
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Edx.IntakeService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.IntakeService.MongoDB
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.IntakeService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.IntakeService.MongoDB

```
Edx.IntakeService.MongoDB
├── HsnSoft.Base.MongoDB   (framework)
├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
└── Edx.IntakeService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.Application

```
Edx.InvoiceService.Application
├── HsnSoft.Base.Ddd.Application   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
└── Edx.InvoiceService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.Domain

```
Edx.InvoiceService.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
└── Edx.Shared.Helper
    └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.Http.Host

```
Edx.InvoiceService.Http.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Edx.Shared.Hosting.Microservices
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.EventBus.Kafka   (framework)
│   ├── HsnSoft.Base.MultiTenancy   (framework)
│   ├── Edx.Shared.Helper
│   │   └── HsnSoft.Base.Core   (framework)
│   └── Edx.Shared.Hosting
│       ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│       ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│       ├── HsnSoft.Base.Authentication   (framework)
│       └── HsnSoft.Base.Logging.Serilog   (framework)
├── Edx.InvoiceService.Application
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Edx.InvoiceService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.InvoiceService.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.InvoiceService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.MongoDB

```
Edx.InvoiceService.MongoDB
├── HsnSoft.Base.EventBus.MongoDB   (framework)
├── HsnSoft.Base.MongoDB   (framework)
├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
└── Edx.InvoiceService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

## The hosts

### Edx.AuthServer.Host

```
Edx.AuthServer.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
├── HsnSoft.Base.MultiTenancy.Authentication   (framework)
├── Edx.Shared.Helper
│   └── HsnSoft.Base.Core   (framework)
├── Edx.Shared.Hosting
│   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│   ├── HsnSoft.Base.Authentication   (framework)
│   └── HsnSoft.Base.Logging.Serilog   (framework)
└── Edx.IdentityService.EntityFrameworkCore
    ├── HsnSoft.Base.Authorization   (framework)
    ├── HsnSoft.Base.EntityFrameworkCore.SqlServer   (framework)
    ├── HsnSoft.Base.MultiTenancy.EntityFrameworkCore   (framework)
    └── Edx.IdentityService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.Gateway.Host

```
Edx.Gateway.Host
├── HsnSoft.Base.Caching.StackExchangeRedis   (framework)
└── Edx.Shared.Hosting.Gateways
    ├── [Yarp.ReverseProxy]
    ├── HsnSoft.Base.AspNetCore.SwaggerGateway   (framework)
    └── Edx.Shared.Hosting
        ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
        ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
        ├── HsnSoft.Base.Authentication   (framework)
        └── HsnSoft.Base.Logging.Serilog   (framework)
```

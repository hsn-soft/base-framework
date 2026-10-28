# Edx: the references of the projects

What references what in the Edx sample, read from the project files (a line `HsnSoft.Base.X (framework)` is a package of the framework, where the tree stops, its own graph is in [ADR-0004](../../docs/adr/0004-core-once-and-the-dependency-graph.md); `[Name]` is an
external NuGet package). The rules are those of Abc ([abc/REFERENCES.md](../abc/REFERENCES.md): [ADR-0028](../../docs/adr/0028-shared-hosting-projects.md), [ADR-0069](../../docs/adr/0069-who-references-which-shared-project-in-abc.md),
[ADR-0070](../../docs/adr/0070-a-framework-package-brings-what-its-api-exposes.md)), and the decisions of this system are [ADR-0078](../../docs/adr/0078-the-edx-sample-a-file-intake-system.md), [ADR-0079](../../docs/adr/0079-grpc-and-wcf-are-entry-points-of-a-host.md) and
[ADR-0080](../../docs/adr/0080-edx-identity-tenants-roles-and-signing-in-by-tenant.md).

## The back office

`app/back-office` is an Angular 19 application and has no project file: it is built with `npm run build` and talks only to the gateway ([ADR-0084](../../docs/adr/0084-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md)).

## What is different from Abc

- **Only the microservices reach Kafka.** `Edx.Shared.Hosting.Microservices` references `HsnSoft.Base.EventBus.Kafka`; the gateway and the auth server are built on `Edx.Shared.Hosting` and `Edx.Shared.Hosting.Gateways`, which reference no broker. The identity service is a microservice that publishes and handles
  nothing, so its host does not call `AddEdxEventBus`.
- **Redis** is chosen by the host (`HsnSoft.Base.Caching.StackExchangeRedis`) in the gateway, the auth server and every microservice. **No host has localization.**
- **The firm invoice integration is three hosts** ([ADR-0086](../../docs/adr/0086-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md)): `Http.Host` (REST, the back office's routes, the event handlers), `Grpc.Host` (`Grpc.AspNetCore`) and `Wcf.Host` (`CoreWCF.Http`, `CoreWCF.Primitives`), each with the blob store it chose (`HsnSoft.Base.BlobStoring.Minio`) over the same Application, Domain and MongoDB projects; the Application references `HsnSoft.Base.BlobStoring` (the abstraction) only. The invoice service reads the files of the firm invoice integration and keeps its own (`BlobStoring`, `ExcelDataReader` for the sheets of the customers). The customer examples (`client/Edx.Client.*`) reference no Edx project: they are what a customer writes.
- **The mock GİB center** (`mock/`) is not a microservice: it is built on `Edx.Shared.Hosting` without a token, keeps its data in MongoDB and its envelopes in the blob store (`HsnSoft.Base.BlobStoring.Minio`), and shares `Edx.Shared.Efatura` (the WCF contract for CoreWCF and for clients, the envelope, the system response, the UBL-TR schema check, the status codes) with the integrator side ([ADR-0087](../../docs/adr/0087-the-edx-mock-gib-center.md)).
- **The auth server reads the identity database** (it references `Edx.IdentityService.EntityFrameworkCore`, as the Abc auth server references the identity one) and issues the token with the tenant in it.
- **Permissions** are checked by the microservices (`Edx.Shared.Hosting.Microservices` references `HsnSoft.Base.Authorization` and `MultiTenancy`: the scope of a grant is the tenant) and written by the identity service, which references `Authorization` too ([ADR-0081](../../docs/adr/0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md)). The auth server and the gateway have none.
- **Tenants are in every service** (Domain: `MultiTenancy`; MongoDB: `MultiTenancy.MongoDB`; EF Core: `MultiTenancy.EntityFrameworkCore`; Host: `MultiTenancy.Authentication`, and `MultiTenancy.EventBus` where events travel).

## The shared projects

### Edx.Shared.Contracts

```
Edx.Shared.Contracts
└── HsnSoft.Base.EventBus   (framework)
```

### Edx.Shared.Efatura

```
Edx.Shared.Efatura
├── [CoreWCF.Http]
├── [CoreWCF.Primitives]
├── [System.ServiceModel.Http]
└── [System.ServiceModel.Primitives]
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

### Edx.InvoiceService.Application

```
Edx.InvoiceService.Application
├── [ExcelDataReader]
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.BlobStoring   (framework)
├── HsnSoft.Base.Ddd.Application   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
├── Edx.Shared.Efatura
│   ├── [CoreWCF.Http]
│   ├── [CoreWCF.Primitives]
│   ├── [System.ServiceModel.Http]
│   └── [System.ServiceModel.Primitives]
└── Edx.InvoiceService.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    ├── Edx.Shared.Contracts
    │   └── HsnSoft.Base.EventBus   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.Domain

```
Edx.InvoiceService.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
└── Edx.Shared.Helper
    └── HsnSoft.Base.Core   (framework)
```

### Edx.InvoiceService.Http.Host

```
Edx.InvoiceService.Http.Host
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
├── Edx.InvoiceService.Application
│   ├── [ExcelDataReader]
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   ├── Edx.Shared.Efatura
│   │   ├── [CoreWCF.Http]
│   │   ├── [CoreWCF.Primitives]
│   │   ├── [System.ServiceModel.Http]
│   │   └── [System.ServiceModel.Primitives]
│   └── Edx.InvoiceService.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Edx.Shared.Contracts
│       │   └── HsnSoft.Base.EventBus   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.InvoiceService.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.InvoiceService.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Edx.Shared.Contracts
        │   └── HsnSoft.Base.EventBus   (framework)
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
    ├── Edx.Shared.Contracts
    │   └── HsnSoft.Base.EventBus   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.SignerService.Http.Host

```
Edx.SignerService.Http.Host
├── HsnSoft.Base.BlobStoring.Minio   (framework)
├── HsnSoft.Base.MultiTenancy.EventBus   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
├── Edx.Shared.Efatura
│   ├── [CoreWCF.Http]
│   ├── [CoreWCF.Primitives]
│   ├── [System.ServiceModel.Http]
│   └── [System.ServiceModel.Primitives]
└── Edx.Shared.Hosting.Microservices
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

### Edx.Client.Grpc

```
Edx.Client.Grpc
├── [Google.Protobuf]
├── [Grpc.Net.Client]
└── [Grpc.Tools]
```

### Edx.Client.Rest

```
Edx.Client.Rest
```

### Edx.Client.Wcf

```
Edx.Client.Wcf
├── [System.ServiceModel.Http]
└── [System.ServiceModel.Primitives]
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

### Edx.Integration.FirmInvoice.Application

```
Edx.Integration.FirmInvoice.Application
├── HsnSoft.Base.Authorization   (framework)
├── HsnSoft.Base.BlobStoring   (framework)
├── HsnSoft.Base.Ddd.Application   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
└── Edx.Integration.FirmInvoice.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    ├── Edx.Shared.Contracts
    │   └── HsnSoft.Base.EventBus   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.FirmInvoice.Domain

```
Edx.Integration.FirmInvoice.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
└── Edx.Shared.Helper
    └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.FirmInvoice.Grpc.Host

```
Edx.Integration.FirmInvoice.Grpc.Host
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
├── Edx.Integration.FirmInvoice.Application
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Edx.Integration.FirmInvoice.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Edx.Shared.Contracts
│       │   └── HsnSoft.Base.EventBus   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.Integration.FirmInvoice.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.Integration.FirmInvoice.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Edx.Shared.Contracts
        │   └── HsnSoft.Base.EventBus   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.FirmInvoice.Http.Host

```
Edx.Integration.FirmInvoice.Http.Host
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
├── Edx.Integration.FirmInvoice.Application
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Edx.Integration.FirmInvoice.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Edx.Shared.Contracts
│       │   └── HsnSoft.Base.EventBus   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.Integration.FirmInvoice.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.Integration.FirmInvoice.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Edx.Shared.Contracts
        │   └── HsnSoft.Base.EventBus   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.FirmInvoice.MongoDB

```
Edx.Integration.FirmInvoice.MongoDB
├── HsnSoft.Base.EventBus.MongoDB   (framework)
├── HsnSoft.Base.MongoDB   (framework)
├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
└── Edx.Integration.FirmInvoice.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    ├── Edx.Shared.Contracts
    │   └── HsnSoft.Base.EventBus   (framework)
    └── Edx.Shared.Helper
        └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.FirmInvoice.Wcf.Host

```
Edx.Integration.FirmInvoice.Wcf.Host
├── [CoreWCF.Http]
├── [CoreWCF.Primitives]
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
├── Edx.Integration.FirmInvoice.Application
│   ├── HsnSoft.Base.Authorization   (framework)
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   └── Edx.Integration.FirmInvoice.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       ├── Edx.Shared.Contracts
│       │   └── HsnSoft.Base.EventBus   (framework)
│       └── Edx.Shared.Helper
│           └── HsnSoft.Base.Core   (framework)
└── Edx.Integration.FirmInvoice.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.Integration.FirmInvoice.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        ├── Edx.Shared.Contracts
        │   └── HsnSoft.Base.EventBus   (framework)
        └── Edx.Shared.Helper
            └── HsnSoft.Base.Core   (framework)
```

### Edx.Integration.GibInvoice.Application

```
Edx.Integration.GibInvoice.Application
├── HsnSoft.Base.BlobStoring   (framework)
├── HsnSoft.Base.Ddd.Application   (framework)
├── Edx.Shared.Contracts
│   └── HsnSoft.Base.EventBus   (framework)
├── Edx.Shared.Efatura
│   ├── [CoreWCF.Http]
│   ├── [CoreWCF.Primitives]
│   ├── [System.ServiceModel.Http]
│   └── [System.ServiceModel.Primitives]
├── Edx.Shared.Helper
│   └── HsnSoft.Base.Core   (framework)
└── Edx.Integration.GibInvoice.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Efatura
        ├── [CoreWCF.Http]
        ├── [CoreWCF.Primitives]
        ├── [System.ServiceModel.Http]
        └── [System.ServiceModel.Primitives]
```

### Edx.Integration.GibInvoice.Domain

```
Edx.Integration.GibInvoice.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
├── HsnSoft.Base.MultiTenancy   (framework)
└── Edx.Shared.Efatura
    ├── [CoreWCF.Http]
    ├── [CoreWCF.Primitives]
    ├── [System.ServiceModel.Http]
    └── [System.ServiceModel.Primitives]
```

### Edx.Integration.GibInvoice.Http.Host

```
Edx.Integration.GibInvoice.Http.Host
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
├── Edx.Integration.GibInvoice.Application
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   ├── Edx.Shared.Efatura
│   │   ├── [CoreWCF.Http]
│   │   ├── [CoreWCF.Primitives]
│   │   ├── [System.ServiceModel.Http]
│   │   └── [System.ServiceModel.Primitives]
│   ├── Edx.Shared.Helper
│   │   └── HsnSoft.Base.Core   (framework)
│   └── Edx.Integration.GibInvoice.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       └── Edx.Shared.Efatura
│           ├── [CoreWCF.Http]
│           ├── [CoreWCF.Primitives]
│           ├── [System.ServiceModel.Http]
│           └── [System.ServiceModel.Primitives]
└── Edx.Integration.GibInvoice.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.Integration.GibInvoice.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Efatura
            ├── [CoreWCF.Http]
            ├── [CoreWCF.Primitives]
            ├── [System.ServiceModel.Http]
            └── [System.ServiceModel.Primitives]
```

### Edx.Integration.GibInvoice.MongoDB

```
Edx.Integration.GibInvoice.MongoDB
├── HsnSoft.Base.EventBus.MongoDB   (framework)
├── HsnSoft.Base.MongoDB   (framework)
├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
└── Edx.Integration.GibInvoice.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    ├── HsnSoft.Base.MultiTenancy   (framework)
    └── Edx.Shared.Efatura
        ├── [CoreWCF.Http]
        ├── [CoreWCF.Primitives]
        ├── [System.ServiceModel.Http]
        └── [System.ServiceModel.Primitives]
```

### Edx.Integration.GibInvoice.Wcf.Host

```
Edx.Integration.GibInvoice.Wcf.Host
├── [CoreWCF.Http]
├── [CoreWCF.Primitives]
├── HsnSoft.Base.BlobStoring.Minio   (framework)
├── Edx.Shared.Efatura
│   ├── [CoreWCF.Http]
│   ├── [CoreWCF.Primitives]
│   ├── [System.ServiceModel.Http]
│   └── [System.ServiceModel.Primitives]
├── Edx.Shared.Hosting
│   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│   ├── HsnSoft.Base.Authentication   (framework)
│   └── HsnSoft.Base.Logging.Serilog   (framework)
├── Edx.Integration.GibInvoice.Application
│   ├── HsnSoft.Base.BlobStoring   (framework)
│   ├── HsnSoft.Base.Ddd.Application   (framework)
│   ├── Edx.Shared.Contracts
│   │   └── HsnSoft.Base.EventBus   (framework)
│   ├── Edx.Shared.Efatura
│   │   ├── [CoreWCF.Http]
│   │   ├── [CoreWCF.Primitives]
│   │   ├── [System.ServiceModel.Http]
│   │   └── [System.ServiceModel.Primitives]
│   ├── Edx.Shared.Helper
│   │   └── HsnSoft.Base.Core   (framework)
│   └── Edx.Integration.GibInvoice.Domain
│       ├── HsnSoft.Base.Ddd.Domain   (framework)
│       ├── HsnSoft.Base.MultiTenancy   (framework)
│       └── Edx.Shared.Efatura
│           ├── [CoreWCF.Http]
│           ├── [CoreWCF.Primitives]
│           ├── [System.ServiceModel.Http]
│           └── [System.ServiceModel.Primitives]
└── Edx.Integration.GibInvoice.MongoDB
    ├── HsnSoft.Base.EventBus.MongoDB   (framework)
    ├── HsnSoft.Base.MongoDB   (framework)
    ├── HsnSoft.Base.MultiTenancy.MongoDB   (framework)
    └── Edx.Integration.GibInvoice.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        ├── HsnSoft.Base.MultiTenancy   (framework)
        └── Edx.Shared.Efatura
            ├── [CoreWCF.Http]
            ├── [CoreWCF.Primitives]
            ├── [System.ServiceModel.Http]
            └── [System.ServiceModel.Primitives]
```

### Edx.Mock.Center.Domain

```
Edx.Mock.Center.Domain
├── HsnSoft.Base.Ddd.Domain   (framework)
└── Edx.Shared.Efatura
    ├── [CoreWCF.Http]
    ├── [CoreWCF.Primitives]
    ├── [System.ServiceModel.Http]
    └── [System.ServiceModel.Primitives]
```

### Edx.Mock.Center.MongoDB

```
Edx.Mock.Center.MongoDB
├── HsnSoft.Base.MongoDB   (framework)
└── Edx.Mock.Center.Domain
    ├── HsnSoft.Base.Ddd.Domain   (framework)
    └── Edx.Shared.Efatura
        ├── [CoreWCF.Http]
        ├── [CoreWCF.Primitives]
        ├── [System.ServiceModel.Http]
        └── [System.ServiceModel.Primitives]
```

### Edx.Mock.Center.Wcf.Host

```
Edx.Mock.Center.Wcf.Host
├── [CoreWCF.Http]
├── [CoreWCF.Primitives]
├── HsnSoft.Base.BlobStoring.Minio   (framework)
├── Edx.Shared.Efatura
│   ├── [CoreWCF.Http]
│   ├── [CoreWCF.Primitives]
│   ├── [System.ServiceModel.Http]
│   └── [System.ServiceModel.Primitives]
├── Edx.Shared.Hosting
│   ├── HsnSoft.Base.AspNetCore.Swashbuckle   (framework)
│   ├── HsnSoft.Base.AspNetCore.WebApi   (framework)
│   ├── HsnSoft.Base.Authentication   (framework)
│   └── HsnSoft.Base.Logging.Serilog   (framework)
└── Edx.Mock.Center.MongoDB
    ├── HsnSoft.Base.MongoDB   (framework)
    └── Edx.Mock.Center.Domain
        ├── HsnSoft.Base.Ddd.Domain   (framework)
        └── Edx.Shared.Efatura
            ├── [CoreWCF.Http]
            ├── [CoreWCF.Primitives]
            ├── [System.ServiceModel.Http]
            └── [System.ServiceModel.Primitives]
```

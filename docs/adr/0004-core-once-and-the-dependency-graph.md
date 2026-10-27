# ADR-0004: Packages reference `HsnSoft.Base.Core` once, and form this dependency graph

- **Status:** Accepted
- **Date:** 2026-10-22
- **Note:** [ADR-0076](0076-more-providers-and-the-rules-every-provider-shares.md): `Logging.NLog` (on `Logging`), `BlobStoring.Azure` and `BlobStoring.Aws` (on `BlobStoring`), `EntityFrameworkCore.MySql` (on `EntityFrameworkCore`).
- **Note:** [ADR-0075](0075-rules-of-the-framework-project-files.md): the rule that every package reaches `Core` and does not list it twice is a check (`scripts/ci-check-framework-references.py`) and is kept there.
- **Note:** [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md): five `MultiTenancy` packages (the tenants and where they come from, the token, the event bus, EF Core, MongoDB); `Core` has no tenant.
- **Note:** [ADR-0073](0073-auditing-and-data-are-packages-of-their-own.md): `Auditing` and `Data` are packages of their own on `Core`; `Ddd.Domain` is on them.

## Context

The framework needs a small shared base (exceptions, `BaseResponse`, log field names, the `[SensitiveData]` attribute, the masking contract). Without a rule, every package
referenced it in its own way, and sibling references multiplied: restores slowed, version conflicts appeared, and it was unclear who depended on whom.

## Decision

- Every package depends on **`HsnSoft.Base.Core`, directly or through another framework package**. If a package already references a sibling that references Core, it does not
  add Core again.
- Core is small and has no framework-specific dependencies: shared exceptions, health-check tag, startup retry, `BaseResponse`, log field names and types (`LogFields`, `LogTypes`), the
  masking contract, and the like. A type goes to Core only when two or more packages need it.
- The graph (a package depends only on the ones in its row; the root README holds the same table):

| Package | Depends on |
|---|---|
| `Core` | - |
| `Logging` | Core |
| `Logging.Serilog` | Logging, Hosting |
| `Logging.NLog` | Logging |
| `Hosting` | Core |
| `Auditing` | Core |
| `Data` | Core |
| `MultiTenancy` | Core |
| `MultiTenancy.Authentication` | MultiTenancy |
| `MultiTenancy.EventBus` | EventBus, MultiTenancy |
| `MultiTenancy.EntityFrameworkCore` | EntityFrameworkCore, MultiTenancy |
| `MultiTenancy.MongoDB` | MongoDB, MultiTenancy |
| `Ddd.Domain` | Auditing, Data |
| `Ddd.Application` | Ddd.Domain |
| `Authentication` | Core |
| `Authorization` | Core |
| `AspNetCore` | Core |
| `AspNetCore.WebApi` | AspNetCore |
| `AspNetCore.Swashbuckle` | AspNetCore |
| `AspNetCore.SwaggerGateway` | Swashbuckle |
| `EventBus` | Core |
| `EventBus.RabbitMQ`, `EventBus.Kafka` | EventBus |
| `EventBus.EntityFrameworkCore` | EventBus, EntityFrameworkCore |
| `EventBus.MongoDB` | EventBus, MongoDB |
| `Caching.StackExchangeRedis` | Core |
| `EntityFrameworkCore` | Ddd.Domain |
| `EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer`, `EntityFrameworkCore.MySql` | EntityFrameworkCore |
| `BlobStoring` | Core |
| `BlobStoring.Minio`, `BlobStoring.Azure`, `BlobStoring.Aws` | BlobStoring |
| `MongoDB` | Ddd.Domain |
| `HsnSoft.Base` | Core (a name holder, nothing else) |

- **No cycles, and no package depends on a provider.** `WebApi` does not know Swagger; `Logging` does not know Serilog; `EventBus` does not know a broker.
- `HsnSoft.Base` (the bare name) only references Core, so a package id with the family name exists; it has no tests because there is nothing to run.

## Consequences

- Adding a dependency between packages is a decision: it changes this table and the root README, and usually needs an ADR.
- A package that wants something from a sibling it does not reference is a sign that the type belongs in Core (or in a new shared abstraction).
- The `Logging.Serilog` -> `Hosting` edge exists because the Graylog sink is refreshed by a hosted service.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0003](0003-abstractions-separate-from-providers.md), root `README.md`, `CONTRIBUTING.md`.

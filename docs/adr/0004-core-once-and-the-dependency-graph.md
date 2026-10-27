# ADR-0004: Packages reference `HsnSoft.Base.Core` once, and form this dependency graph

- **Status:** Accepted
- **Date:** 2026-10-22

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
| `Hosting` | Core |
| `Authentication` | Core |
| `AspNetCore` | Core |
| `AspNetCore.WebApi` | AspNetCore |
| `AspNetCore.Swashbuckle` | AspNetCore |
| `AspNetCore.SwaggerGateway` | Swashbuckle |
| `EventBus` | Core |
| `EventBus.RabbitMQ`, `EventBus.Kafka` | EventBus |
| `EventBus.EntityFrameworkCore` | EventBus, EntityFrameworkCore |
| `EventBus.MongoDB` | EventBus, MongoDB |
| `Caching.StackExchangeRedis` | Core |
| `EntityFrameworkCore` | Core |
| `EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer` | EntityFrameworkCore |
| `BlobStoring` | Core |
| `BlobStoring.Minio` | BlobStoring |
| `MongoDB` | Core |
| `HsnSoft.Base` | Core (a name holder, nothing else) |

- **No cycles, and no package depends on a provider.** `WebApi` does not know Swagger; `Logging` does not know Serilog; `EventBus` does not know a broker.
- `HsnSoft.Base` (the bare name) only references Core, so a package id with the family name exists; it has no tests because there is nothing to run.

## Consequences

- Adding a dependency between packages is a decision: it changes this table and the root README, and usually needs an ADR.
- A package that wants something from a sibling it does not reference is a sign that the type belongs in Core (or in a new shared abstraction).
- The `Logging.Serilog` -> `Hosting` edge exists because the Graylog sink is refreshed by a hosted service.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0003](0003-abstractions-separate-from-providers.md), root `README.md`, `CONTRIBUTING.md`.

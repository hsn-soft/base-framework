# ADR-0005: Every microservice has the same four projects; only the persistence project varies

- **Status:** Accepted (the domain project depends on Core and `Ddd.Domain`, the application project on `Ddd.Application`: [framework ADR-0042](../../../docs/adr/0042-ddd-domain-and-application-packages.md)) (the host no longer picks the database provider: the persistence project registers it, [ADR-0010](0010-the-persistence-project-registers-its-database.md))
- **Date:** 2026-10-25

## Context

The production microservices are all built from the same layers, and the one thing that differs between them is where they keep their data (a relational database through Entity Framework, or MongoDB). When each service is shaped by its author, a reader
cannot find things, the dependencies between layers drift, and a new service starts from copying another. The Abc system has five services (identity, order, product, shipment, and the auth server of [ADR-0004](0004-one-auth-server-issues-the-token.md)).

## Decision

A microservice `X` lives in `samples/abc/service/<x>/` and has **four projects**:

| Project | Is | References |
|---|---|---|
| `Abc.XService.Domain` | the entities, the rules, the repository interfaces (`<Name>Domain/{Consts,Entities,Exceptions,Repositories}`) | the framework's Core only |
| `Abc.XService.Application` | the use cases: DTOs and service interfaces (`Contracts/<Name>Domain/{Dtos,Services}`), their implementations (`Services/`), `ApplicationServiceCollectionExtensions` | Domain |
| `Abc.XService.EntityFrameworkCore` **or** `Abc.XService.MongoDB` | the persistence: the `DbContext` and mappings (`Context/`, `Configurations/`, `Repositories/`) or the collections; `Add<X>Persistence()` | Domain, and `HsnSoft.Base.EntityFrameworkCore` or `HsnSoft.Base.MongoDB` |
| `Abc.XService.Http.Host` | the web api: `Program.cs`, the controllers (`Controllers/Base/BaseServiceController`), settings | `Abc.Shared.Hosting.Microservices`, Application, the persistence project, `Logging.Serilog` (and `EntityFrameworkCore.PostgreSql` for a relational service) |

- **The persistence project is the only thing that varies** between services: `EntityFrameworkCore` (identity, order, with PostgreSQL) or `MongoDB` (product, shipment), as in the Xyz sample. A service that keeps data in both has both.
- **Dependencies go one way:** Host -> Application and persistence -> Domain; **Application and persistence never reference each other**, and nothing references the Host. The Host picks the database provider (`AddBasePostgres`, `AddBaseMongo`); the persistence project does not.
- **The host is thin** ([ADR-0003](0003-shared-hosting-projects.md)): `AddAbcMicroservice`, the service's persistence, its application, then `MapControllers`. Its **base controller** carries the area, JWT and routes pattern ([framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md)).
- The projects of a service are in a folder named after it, with no extra level for the host; the names are `Abc.<Service>Service.<Layer>` (the auth server, which has no layers yet, is `Abc.AuthServer.Host` in `app/`).
- Ports of the sample: identity 5400, auth server 5410, order 5420, product 5430, shipment 5440.

## Consequences

- A new service is the same four projects, in the same places, with the same registrations; reviews check the dependency directions.
- The order, product and shipment services were **skeletons** (layers wired, no domain); they have their domains and endpoints now, [ADR-0008](0008-the-abc-services-crud-pattern.md).
- The namespace of a MongoDB project (`Abc.ProductService.MongoDB`) hides the driver's `MongoDB` namespace for fully qualified names inside it; use `using MongoDB.Driver;` and simple names there.
- The solution file lists the four projects of each service in the `abc` solution folder ([framework ADR-0006](../../../docs/adr/0006-repository-folders-and-the-solution-file.md)).

## Related

[framework ADR-0005](../../../docs/adr/0005-where-code-lives.md), [framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md), [ADR-0003](0003-shared-hosting-projects.md), [ADR-0004](0004-one-auth-server-issues-the-token.md), `samples/abc/service`.

# ADR-0035: The persistence project registers its database provider

- **Status:** Accepted
- **Date:** 2026-10-25 (replaces the part of [ADR-0030](0030-microservice-project-pattern.md) that says the host picks the database provider)

## Context

ADR-0030 had the host call `AddBasePostgres<XDbContext>` or `AddBaseMongo` and the persistence project only add the repositories. The persistence project is the one that knows which database the service keeps its data in, and the host had to know it too (its
project reference to the provider package and its `Program.cs`), so a change of database was a change in two projects, and a second host of the same service (the auth server reads the identity service's database) had to repeat it.

## Decision

- **`AddXPersistence(configuration)` registers everything the service needs of its database:** for PostgreSQL the `DbContext` with the provider (`AddBasePostgres`, `ConnectionStrings:Postgres`) and the repositories; for MongoDB the connection (`AddBaseMongo`, `ConnectionStrings:Mongo`),
  the mappings and the repositories. Both add their readiness check.
- **The persistence project references the provider package** (`HsnSoft.Base.EntityFrameworkCore.PostgreSql`; `HsnSoft.Base.MongoDB` it already had); **the host does not**: its `Program.cs` calls `AddAbcMicroservice`, `AddXPersistence(builder.Configuration)`, `AddXApplication`.
- A host that needs the database of another service (the auth server reads the identity service's) calls that service's `AddXPersistence` and gets the same registration.

## Consequences

- A change of database is a change of one project; the hosts stay the same.
- The persistence project is no longer free of a provider: it knows PostgreSQL or MongoDB. That was already true of its mappings.
- The dependency directions of ADR-0030 stay as they are (Host -> Application and persistence -> Domain).

## Related

[ADR-0030](0030-microservice-project-pattern.md), [ADR-0032](0032-the-auth-server-reads-the-users-of-the-identity-service.md), [ADR-0033](0033-the-abc-services-crud-pattern.md).

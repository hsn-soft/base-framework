# ADR-0051: The REST host of a service owns its databases

- **Status:** Accepted
- **Date:** 2026-10-31 (uses framework ADR-0118, which links to `../../../docs/adr`)

## Context

A service has one host today and may have more (the firm invoice integration of Edx already has a REST, a gRPC and a WCF host on one MongoDB database, and a service of Abc could get a WCF host beside its REST host). The pods of a host start together too: in the test of a two-pod deployment of the Edx identity
service one pod failed on the seed of the other and restarted.

## Decision

- **Each host says which databases it prepares, in its `appsettings.json` (`SeedStartupConnections`)**: the REST host of a service owns the databases of the service (the identity service: `EfCore-IdentityService` and `EfCore-HistoryService`; the shipment service: `Mongo-ShipmentService` and `EfCore-ShipmentHistoryService`; the order service,
  the product service, the Edx invoice service and the two integrations: theirs), the auth servers and the gRPC and WCF hosts own none (`""`). Absent would have meant "all the ones it has".
- **The steps are in the registration of the persistence, not in `Program.cs`**: the schema of a context is `AddSchemaStartupStep<TContext>(...)`, the Mongo collections and indexes and the seed are `AddDatabaseStartupStep(<connection name>, ...)`, all called by the `Add...Persistence` method of the service (the seed tenants of the product and the identity services
  are parameters of that method). So every host of a service has the same code (a REST, a gRPC and a WCF host call the same `AddFirmInvoicePersistence`) and only its `SeedStartupConnections` says which of them does the work. The steps run when the host starts, before any hosted service. The code of what is seeded did not change.
- **A host that does not own a database leaves it to the owner** and is not ready before it is there (the readiness check of the database).
- If a service gets a second host that should prepare one of its databases (the WCF host of the identity service owning SQL Server while the REST host keeps PostgreSQL), it is two settings and no code: the REST host drops the name from its list, the new host has it in its own.

## Consequences

- Two pods, or two hosts, of a service can start on empty databases at once: each database is made and seeded once.
- A host that was deployed with the old `appsettings.json` (no `Startup` section) still prepares everything it has.

## Related

Framework ADR-0118, [ADR-0047](0047-the-abc-system-runs-in-docker-compose-and-in-kubernetes.md), [ADR-0050](0050-edx-has-the-same-environments-with-a-broker-bucket-and-build-for-each.md).

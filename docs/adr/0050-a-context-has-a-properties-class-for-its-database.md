# ADR-0050: A context has a properties class that says where and on what its data is

- **Status:** Accepted
- **Date:** 2026-10-25 (adds to [ADR-0047](0047-relational-provider-chosen-by-a-setting.md); [ADR-0049](0049-abc-databases-and-provider-references.md) is changed by it for the identity service)

## Context

A service can have more than one context, each on its own database and, as the sample of the identity service shows, its own provider: the users on SQL Server, the history of what happened to them on PostgreSQL. One setting `Database:Provider` cannot name two providers, and a table prefix, a schema and the name of a connection string
belong to the context, not to the host that runs it. The old system had a static class for this (`EfCoreIdentityDbProperties`: `DbTablePrefix`, `DbSchema`, `ConnectionStringName`).

## Decision

- **Each context of a service has a static properties class in its persistence project** (`EfCoreIdentityDbProperties`, `EfCoreHistoryDbProperties`): `DbTablePrefix`, `DbSchema`, `Provider` (one of `EfCoreProviderNames`) and the constant `ConnectionStringName` (`EfCore-IdentityService`, `EfCore-HistoryService`). The mappings read the prefix
  and the schema (`ToTable(prefix + name, schema)`); the registration reads the provider and the connection name. They are in the code of the service, **not in its settings**.
- **`AddBaseEfCore<TContext>(configuration, providers, provider, connectionName)`** registers a context on the provider and the connection string the code names. The overload of ADR-0047 (the provider by the setting `Database:Provider`) stays for a service that wants the setting.
- **One registration method for each context** (`AddIdentityPersistence`, `AddIdentityHistoryPersistence`), so a host registers the contexts it needs: the auth server reads the users and has no history.
- **The persistence project references the provider packages of every context it has:** the identity project, SQL Server and PostgreSQL.

## Consequences

- The identity service has two databases, `abc_identity` (SQL Server) and `abc_history` (PostgreSQL), and the connection strings `ConnectionStrings:EfCore-IdentityService` and `ConnectionStrings:EfCore-HistoryService` replace `ConnectionStrings:SqlServer` and the setting `Database:Provider` in the settings of the identity service and the auth server.
- There is no transaction over two databases: the application service writes the history after the change, and a failure between the two leaves the change made and the history without it (a transactional outbox is the way if it must not).

## Related

[ADR-0047](0047-relational-provider-chosen-by-a-setting.md), [ADR-0049](0049-abc-databases-and-provider-references.md), [ADR-0045](0045-repository-levels-relational-and-document.md).

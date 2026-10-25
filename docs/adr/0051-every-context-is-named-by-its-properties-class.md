# ADR-0051: Every context of a sample is named by its properties class; the provider setting is gone

- **Status:** Accepted
- **Date:** 2026-10-25 (changes [ADR-0047](0047-relational-provider-chosen-by-a-setting.md); completes [ADR-0050](0050-a-context-has-a-properties-class-for-its-database.md))

## Context

ADR-0050 gave the contexts of the identity service a properties class and left the setting `Database:Provider` for the other services, so two ways to say the same thing lived side by side. The maintainer's rule: what is done in one sample is done in all of them; no sample is left behind.

## Decision

- **The setting `Database:Provider` and the overload of `AddBaseEfCore` that read it are removed.** There is one `AddBaseEfCore<TContext>(configuration, providers, provider, connectionName)`: the provider and the connection name are said by the code. `AddBasePostgres` and `AddBaseSqlServer` stay as the call of a
  service with one database, with a connection name of their own. `IEfCoreProvider` no longer has a connection name.
- **Every persistence project has the properties class of its store:** `EfCoreIdentityDbProperties`, `EfCoreHistoryDbProperties`, `EfCoreOrderDbProperties` (table prefix, schema, provider, connection name: `EfCore-OrderService`) for the relational ones, and `MongoProductDbProperties`, `MongoShipmentDbProperties` (collection prefix,
  connection name: `Mongo-ProductService`, `Mongo-ShipmentService`) for MongoDB. The mappings, the collection names and the seeds read the prefixes.
- **The settings of a service hold only the connection strings, under these names.**

## Consequences

- A service that moves a context to another provider changes the `Provider` of its properties class (and offers the provider in its persistence project, and gives its connection string); no setting is read to choose.
- The connection string keys `Postgres`, `SqlServer` and `Mongo` of the Abc services are replaced by the names above; a deployment that set them by environment variable renames them (`ConnectionStrings__EfCore-OrderService`).

## Related

[ADR-0050](0050-a-context-has-a-properties-class-for-its-database.md), [ADR-0047](0047-relational-provider-chosen-by-a-setting.md), [ADR-0035](0035-the-persistence-project-registers-its-database.md).

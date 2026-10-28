# ADR-0014: More than one store in a service: a second MongoDB under a key, a relational project beside a MongoDB one

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [framework ADR-0045](../../../docs/adr/0045-repository-levels-relational-and-document.md) and [ADR-0012](0012-every-context-is-named-by-its-properties-class.md))

## Context

The purpose of the samples is to try different contexts, different providers and different infrastructure libraries together: the identity service on SQL Server and PostgreSQL (two EF Core contexts), the order service on PostgreSQL, the product service on two MongoDB databases (the products, and the history of their stock), the
shipment service on MongoDB (the shipments) and PostgreSQL with EF Core (the history of its deliveries). `AddBaseMongo` used `TryAdd`, so a second MongoDB would have been silently the first; and `AddMongoRepository` could only use the one `IMongoDatabase`.

## Decision

- **A second MongoDB is registered under a key.** `AddBaseMongo(configuration, connectionName, serviceKey)` registers the client and the database as keyed singletons (`GetRequiredKeyedService<IMongoDatabase>(key)`) and a readiness check `mongo-<key>`; without a key it is the unkeyed database of the service, as before.
  `AddMongoRepository<TEntity, TKey>(collection, serviceKey)` makes the repository of the collection in that database. The key is a constant of the properties class of the store (`MongoStockDbProperties.ServiceKey`).
- **A service with more than one kind of store has one project for each**, named by the store (framework ADR-0045): the shipment service has `Abc.ShipmentService.MongoDB` (the shipments) and `Abc.ShipmentService.EntityFrameworkCore` (the shipment history, `EfCoreShipmentHistoryDbProperties`, PostgreSQL); the domain, the application and the host are shared and
  register what each project offers (`AddShipmentPersistence`, `AddShipmentHistoryPersistence`).
- **Every history is a CRUD like the entity it follows** (domain, application service, controller, DTOs read by projection) and is **written by the service when the entity changes**: the product stock history (`product-stock-histories`) when a product is created and when an update changes its stock; the shipment history
  (`shipment-histories`) when a shipment is delivered (`delivered`) or returned (`failed`). The stores are different from the entity's: there is no transaction over them yet, so a failure of the history store fails the call after the change is made.

## Consequences

- The samples now use, together: EF Core on SQL Server and PostgreSQL, MongoDB on two databases, in one solution; a `Mongo-StockService` connection string is another cluster as easily as another database.
- The settings hold only connection strings: `Mongo-StockService`, `EfCore-ShipmentHistoryService` (and the ones of ADR-0012).

## Related

[framework ADR-0045](../../../docs/adr/0045-repository-levels-relational-and-document.md), [ADR-0012](0012-every-context-is-named-by-its-properties-class.md), [ADR-0013](0013-the-history-of-the-users.md).

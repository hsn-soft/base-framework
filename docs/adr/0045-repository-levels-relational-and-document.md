# ADR-0045: The repository has levels: a common contract, one interface and one implementation for each kind of store

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [ADR-0042](0042-ddd-domain-and-application-packages.md))

## Context

ADR-0042 had one `IRepository` and options with an `Include` that a document store silently ignored. A relational store and a document store are not the same thing: one loads related rows with joins, saves the changes of a context, and moves between providers (PostgreSQL, SQL Server, MySQL)
with the same code; the other keeps nested documents, writes a whole document or one field atomically, and has sessions instead of a context. The old framework had the same split in its interfaces (`IEfCoreGenericRepository`, `IMongoGenericRepository`) but let `DbSet`, collections and sessions leak
into them, and a method could work on one store and be ignored by the other.

## Decision

- **A common contract in `Ddd.Domain`, which every store implements:** `IReadOnlyRepository<TEntity, TKey>` (find, any, count, list, page, and the DTO reads of ADR-0046) and `IRepository<TEntity, TKey>` (add, update, delete). It has no `IQueryable`, no `Include`, no context or session.
- **One interface for each kind of store, extending the common one:** `IEfCoreRepository<TEntity, TKey>` (in `EntityFrameworkCore`: the reads with an `include`; more follows for set based changes and bulk) and, later, `IMongoRepository<TEntity, TKey>` (in `MongoDB`: replace the whole document, set one field, upsert,
  bulk write, session). What only one kind of store can do is on its interface; it is never added to the common one and ignored by the other.
- **Nothing is joined unless it is asked for.** The relational reads of an entity with an `include` (`query => query.Include(order => order.Lines)`) join what it says; without one, nothing; a DTO read joins only what the DTO reaches.
- **Where the code lives.** The common contract and the options are in `Ddd.Domain`. Every kind of store is a project of the service: all the relational ones are `<Service>.EntityFrameworkCore` (the provider is a setting, not another project); the others are named after the product (`<Service>.MongoDb`,
  `<Service>.Elasticsearch`). The domain is shared; when a service has more than one store, the interfaces and the entities of each store are in a folder of the domain named for it (`Domain/EntityFramework`, `Domain/Mongo`).
- **The services that follow:** a service's own repository interface extends the interface of its store when it needs something of it (`IOrderRepository : IEfCoreRepository<Order, Guid>`), and the common one when it does not.

## Consequences

- `ListQueryOptions.Include` is gone; the application code that needs a join uses the relational interface (a service on a relational store injects `IEfCoreRepository<,>`; the rest of the code keeps `IRepository<,>`).
- Swapping a relational provider changes configuration, not application code; swapping a relational store for a document store changes the project and what the service asks of its interface.
- Following: the choice of the provider by setting, set based changes and bulk, the safety of a context in a scope, the same for MongoDB (each as its own ADR).

## Related

[ADR-0042](0042-ddd-domain-and-application-packages.md), [ADR-0046](0046-dto-projection-without-a-mapper.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md).

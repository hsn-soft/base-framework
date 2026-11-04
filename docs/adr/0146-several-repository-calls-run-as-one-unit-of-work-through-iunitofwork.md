# ADR-0146: Several repository calls run as one unit of work through `IUnitOfWork`, never through a context

- **Status:** Accepted
- **Date:** 2026-11-04

## Context

A repository of the framework saves with every call. An operation that writes several entities and must be all or nothing (a domain service that sells a product and links the roles to the sale; one that makes a tenant with its administrator role and its first user) needs one transaction. The only way to have one was `DbContext.ExecuteInTransactionAsync`, which asks the code for a context: the domain and the application layers of a service would then know the persistence, and a developer would write the queries with `DbSet`, joins and foreign keys there. The rule of the HHS services is that **no code outside the seeders and the repositories touches a context**, and that a repository itself is written with the repository API (filters, includes, projections, `UpdateWhereAsync`).

## Decision

`HsnSoft.Base.Ddd.Domain` has `IUnitOfWork` (`ExecuteAsync` of a unit of work that returns a value, or nothing): everything the work saved through the repositories is committed together, or none of it when the work throws. The EF Core provider has `EfCoreUnitOfWork<TContext>` over `ExecuteInTransactionAsync` (so it works with a retrying strategy: the work may run again from its start, so it reads what it needs inside, and the tracker is cleared when it begins) and `AddEfCoreUnitOfWork<TContext>()`. A domain or application service depends on the repositories and on `IUnitOfWork`, and on no context.

The unit of work is **a callback, not a scope** (`ExecuteAsync(work)`, not `using var uow = Begin()`). A scope cannot be run again, and the retrying strategy of the context runs the whole unit again when the connection fails in a way that may pass; a transaction that the code begins itself is refused by that strategy. Nor does it defer the saves into one `SaveChanges`: a sale must write the end of the old subscription before the new one, because of the unique index of the customer and the product, so the repositories save when they are called, inside the transaction.

## Consequences

An all-or-nothing operation is written without a context. The cost: the work runs again after a retry, so it must be safe to run again; and the repositories of one context share its unit of work, so the runner covers repositories of the same context only.

## Related

[ADR-0136](0136-an-entity-is-protected-from-a-lost-update-by-an-opt-in-stamp.md), [DddBuildingBlocks](../DddBuildingBlocks.md), `src/HsnSoft.Base.Ddd.Domain/HsnSoft/Base/Domain/Repositories/IUnitOfWork.cs`.

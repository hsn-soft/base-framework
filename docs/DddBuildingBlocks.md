# Entities, DTOs and the repository

What a service does not write itself ([ADR-0042](adr/0042-ddd-domain-and-application-packages.md)): the base classes of its entities, the DTOs of paging, and the repository.

## Entities (`HsnSoft.Base.Ddd.Domain`)

```csharp
public sealed class Product : FullAuditedEntity<Guid>, IMultiTenant       // audit + soft delete from the base; the tenant is explicit (only a service with tenants: HsnSoft.Base.MultiTenancy)
{
    private Product() { }                                                 // for the store
    public string Name { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }                            // the persistence writes it: no method for it

    public static Product Create(string name) => new() { Id = Guid.CreateVersion7(), Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), 128) };
}
```

`Entity<TKey>` (the id), `CreationAuditedEntity<TKey>` (+ `CreatedAt`, `CreatedBy`), `AuditedEntity<TKey>` (+ `UpdatedAt`, `UpdatedBy`), `FullAuditedEntity<TKey>` (+ soft delete). An entity that needs none of it derives from `Entity<TKey>`.
An entity that people edit at the same time adds `IHasConcurrencyStamp`: a change made after the caller read it is a 409 instead of a silent overwrite ([ADR-0136](adr/0136-an-entity-is-protected-from-a-lost-update-by-an-opt-in-stamp.md)).
See [EntityConcepts.md](EntityConcepts.md) for what the persistence does with the interfaces.

## DTOs (`HsnSoft.Base.Ddd.Application`)

| For | Derive from | Has |
|---|---|---|
| every response | `IBaseDto` (marker) | what a service sends, found by its type (ADR-0147): the entity DTOs and the list results have it; a request does not have to |
| a response of an entity | `EntityDto<TKey>`, `AuditedEntityDto<TKey>`, `FullAuditedEntityDto<TKey>` | `Id`, `CreatedAt/By`, `UpdatedAt/By`, the deletion |
| a list with at most N | `LimitedDataRequestDto` | `MaxResultCount` (10; at most `Paging:MaxResultCountLimit`, 100) |
| a sorted list | `SortedAndLimitedDataRequestDto` | + `SortingText` (`"name asc, price desc"`) |
| a search | `SearchDataRequestDto` | + `SearchText` |
| a page | `PagedDataRequestDto` | + `PageNumber` (from 1); `MaxResultCount` is the size of the page |
| the answer | `ListDataResultDto<T>`, `PagedDataResultDto<T>` | `Items`; `DataTotalCount`, `PageTotalCount`, `ResultDataStartNumber`, `ResultDataEndNumber` |

```csharp
public sealed class GetProductsPaged : PagedDataRequestDto { public string? Name { get; set; } public bool? IsActive { get; set; } }   // only what is the service's own
```

## The repository (`IRepository<TEntity, TKey>`)

`IReadOnlyRepository` (`FindAsync(id | predicate)`, `AnyAsync(predicate, ignoreFilters)`, `CountAsync`, `GetListAsync(ListQueryOptions)`, `GetPagedListAsync(PagedQueryOptions)`, and the same for a DTO: `FindAsync<TDto>`, `GetListAsync<TDto>`, `GetPagedListAsync<TDto>`) and `IRepository` (`AddAsync` and `UpdateAsync` **give something back**, `DeleteAsync`, the same for many: `AddManyAsync`, `UpdateManyAsync`, `DeleteManyAsync`, and `DeleteWhereAsync(filter)`, which deletes every match without loading it and writes the soft delete and the audit fields as one by one would, [ADR-0048](adr/0048-bulk-and-set-based-changes.md)); what only one kind of store does is on its own interface (`IEfCoreRepository`, [ADR-0045](adr/0045-repository-levels-relational-and-document.md)). Registered with `AddEfCoreRepository<TContext, TEntity, TKey>()` or

**What a write gives back, and how it fails** ([ADR-0162](adr/0162-a-repository-write-says-what-it-did-and-fails-with-one-exception-that-names-the-records.md)):

| Call | Gives | Fails with |
|---|---|---|
| `AddAsync(entity)` | the **same instance**, with what the store wrote (key, audit fields, stamp, tenant): no second read is needed to answer | |
| `AddManyAsync(entities)` | nothing; all are added or none | |
| `UpdateAsync(entity)` | `WriteOutcome.Modified`, or `NotModified` when nothing in the entity differs from what `FindAsync` read (nothing is written, the stamp stays) | `RepositoryException` |
| `UpdateManyAsync(entities)` | `WriteResult(Requested, Modified)` (`NotModified` is the difference) | `RepositoryException` listing **every** record that failed |
| `DeleteAsync(entity)`, `DeleteManyAsync(entities)` | nothing | `RepositoryException` |
| `DeleteWhereAsync(filter)`, `UpdateWhereAsync` (per store) | how many records | |

A key comes from the caller or from the store: an empty `Guid` key is given a version 7 `Guid` (`EntityKeys`), a key that was given is kept. `RepositoryException` is one `BaseHttpException` for the three ways a write fails: `NotFound` (404, `entity_not_found`: the record is not there, or was soft deleted), `ConcurrencyConflict` (409, `concurrency_conflict`: it is not the one the caller read) and `DuplicateKey` (409, `duplicate_key`: a unique key or index would be broken; `Constraint` names it, the duplicated value is never told). It names the entity and the records (`Ids`, also in the response as `entity` and `ids`), in the language of the request. An application service catches it to answer with a business exception of its own. To answer a create or an update it makes the DTO of the entity it has: `EntityProjection.ToDto<TEntity, TDto>(entity)` (the same rules as the projection; `IsFlat<TEntity, TDto>()` says whether the DTO needs nothing but the entity's own properties: a DTO that reaches another object, like `TenantName` from `Tenant.Name`, or a list, is read with `FindAsync<TDto>` instead). Only an entity that `FindAsync` returned is followed (`NotModified` is known for it); one that came from a list is saved whole. A relational store writes **all or nothing** (one `SaveChanges` is one transaction, rolled back on a failure); MongoDB is [below](#mongodb-transactions).

**Several calls as one transaction.** A repository saves with every call. An operation that writes several entities and must be all or nothing runs in `IUnitOfWork.ExecuteAsync(work)` (EF Core: `AddEfCoreUnitOfWork<TContext>()`): what the work saved through the repositories is committed together, or none of it when it throws. The work may run again after a retry of the persistence, so it reads what it needs inside it. A domain service depends on the repositories and on this, never on a context ([ADR-0146](adr/0146-several-repository-calls-run-as-one-unit-of-work-through-iunitofwork.md)).
`AddMongoRepository<TEntity, TKey>("collection")`; soft-deleted and other tenants' entities are never found, except by `AnyAsync(..., ignoreFilters: true)` (a value that is unique in the whole store). Each method that changes saves.

An application service builds the filter and asks:

```csharp
Expression<Func<Product, bool>> filter = new FilterBuilder<Product>()                       // a part that is null is left out
    .And(name is null ? null : p => p.NormalizedName.Contains(name))
    .And(isActive is null ? null : p => p.IsActive == isActive.Value)
    .Build();

PagedQueryResult<Product> page = await products.GetPagedListAsync(
    input.ToPagedOptions(filter, ProductSorting.Fields, ProductSorting.Default), cancellationToken);   // checks the sorting text against the fields offered
return page.ToPagedDto(input, ToDto);
```

### Sorting is a whitelist

```csharp
public static readonly SortFields<Product> Fields = new SortFields<Product>().Add("name", p => p.NormalizedName).Add("price", p => p.Price);
```

A client sends `"price desc, name"`; a field that is not in `Fields` is refused (`Validation:UnknownSortField`, told in its language, with the fields it may use). The text is never part of a query.

### Stores

- **EF Core:** `EfCoreRepository<TContext, TEntity, TKey>`; derive it for a method of your own (`class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository<IdentityDbContext, User, Guid>(db), IUserRepository`). `IEfCoreRepository<,>` adds `UpdateWhereAsync(filter, set => set.SetProperty(...))` (one statement, audit fields written) and the reads with an `include` (`query => query.Include(order => order.Lines)`); nothing is joined unless one says so.

The provider of a relational context is said by the code of the service, not by a setting: `services.AddBaseEfCore<OrdersDbContext>(configuration, providers => providers.UseSqlServer(), EfCoreOrdersDbProperties.Provider, EfCoreOrdersDbProperties.ConnectionStringName)`, where the properties class of the context also has its table prefix and schema ([ADR-0050](adr/0050-a-context-has-a-properties-class-for-its-database.md)). The application code does not change when a context moves to another provider.

### Reading a DTO without a mapper

`GetPagedListAsync<ProductListDto>(options)` reads only the columns of `ProductListDto` (and joins only what it reaches): its properties are filled from the entity's of the same name, `CustomerName` from `Customer.Name`, lists and objects member by member; `[NotMapped]` leaves one out; a property nothing answers is an error that names it. A `selector` argument says what a name cannot ([ADR-0046](adr/0046-dto-projection-without-a-mapper.md)).
- **MongoDB:** `MongoRepository<TEntity, TKey>`; call `MongoEntityMaps.RegisterBases<TKey>()` before the class maps of the entities (the driver wants the members of a base class mapped in its own map), and map only what is the entity's own
  (`map.MapProperty(p => p.Name)`, `TenantId`). `AddMongoRepository` calls it too. `IMongoRepository<,>` adds `UpdateWhereAsync(filter, set => set.Inc(p => p.Stock, -3))`: the check of the filter and the change are one statement of the database, audit fields written.

### The change of a counter

Two requests, or two copies of one message, can reach a service at once, and a broker may run several handlers of an event at the same time (`fetchCount`). A value that the code reads, checks and writes back (a stock) is then wrong; the service changes it in one step of the store
(`UpdateWhereAsync` with the check in the filter, as a `TryTakeStockAsync` of a product repository would).

### MongoDB transactions

MongoDB has transactions only on a replica set (or a sharded cluster). `AddMongoUnitOfWork()` registers `MongoUnitOfWork` as `IUnitOfWork`: the work runs in one transaction and every repository call in it (reads too) goes through it; a unit of work inside one joins it ([ADR-0163](adr/0163-mongodb-transactions-are-a-unit-of-work-and-a-write-of-many-without-one-is-all-or-none-as-far-as-a-document-store-can.md)). A service on a standalone server does not register it. `IMongoRepository` also has `AddManyInTransactionAsync`, `UpdateManyInTransactionAsync` and `DeleteManyInTransactionAsync` (all or none, whatever fails). Without a transaction the plain many-methods do what a document store can: `AddManyAsync` removes the documents it had added when one fails, `DeleteManyAsync` of documents removed for good reads the keys first and fails before it removes any, and `UpdateManyAsync` writes each document atomically, so when one is not there the others were written and the exception names the one that was not.

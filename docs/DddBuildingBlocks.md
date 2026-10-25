# Entities, DTOs and the repository

What a service does not write itself ([ADR-0042](adr/0042-ddd-domain-and-application-packages.md)): the base classes of its entities, the DTOs of paging, and the repository.

## Entities (`HsnSoft.Base.Ddd.Domain`)

```csharp
public sealed class Product : FullAuditedEntity<Guid>, IMultiTenant       // audit + soft delete from the base; the tenant is explicit
{
    private Product() { }                                                 // for the store
    public string Name { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }                            // the persistence writes it: no method for it

    public static Product Create(string name) => new() { Id = Guid.CreateVersion7(), Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), 128) };
}
```

`Entity<TKey>` (the id), `CreationAuditedEntity<TKey>` (+ `CreatedAt`, `CreatedBy`), `AuditedEntity<TKey>` (+ `UpdatedAt`, `UpdatedBy`), `FullAuditedEntity<TKey>` (+ soft delete). An entity that needs none of it derives from `Entity<TKey>`.
See [EntityConcepts.md](EntityConcepts.md) for what the persistence does with the interfaces.

## DTOs (`HsnSoft.Base.Ddd.Application`)

| For | Derive from | Has |
|---|---|---|
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

`IReadOnlyRepository` (`FindAsync(id | predicate)`, `AnyAsync(predicate, ignoreFilters)`, `CountAsync`, `GetListAsync(ListQueryOptions)`, `GetPagedListAsync(PagedQueryOptions)`, and the same for a DTO: `FindAsync<TDto>`, `GetListAsync<TDto>`, `GetPagedListAsync<TDto>`) and `IRepository` (`AddAsync`, `UpdateAsync`, `DeleteAsync`); what only one kind of store does is on its own interface (`IEfCoreRepository`, [ADR-0045](adr/0045-repository-levels-relational-and-document.md)). Registered with `AddEfCoreRepository<TContext, TEntity, TKey>()` or
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

- **EF Core:** `EfCoreRepository<TContext, TEntity, TKey>`; derive it for a method of your own (`class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository<IdentityDbContext, User, Guid>(db), IUserRepository`). `IEfCoreRepository<,>` adds the reads with an `include` (`query => query.Include(order => order.Lines)`); nothing is joined unless one says so.

### Reading a DTO without a mapper

`GetPagedListAsync<ProductListDto>(options)` reads only the columns of `ProductListDto` (and joins only what it reaches): its properties are filled from the entity's of the same name, `CustomerName` from `Customer.Name`, lists and objects member by member; `[NotMapped]` leaves one out; a property nothing answers is an error that names it. A `selector` argument says what a name cannot ([ADR-0046](adr/0046-dto-projection-without-a-mapper.md)).
- **MongoDB:** `MongoRepository<TEntity, TKey>`; call `MongoEntityMaps.RegisterBases<TKey>()` before the class maps of the entities (the driver wants the members of a base class mapped in its own map), and map only what is the entity's own
  (`map.MapProperty(p => p.Name)`, `TenantId`). `AddMongoRepository` calls it too.

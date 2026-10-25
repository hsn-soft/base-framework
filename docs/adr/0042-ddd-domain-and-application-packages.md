# ADR-0042: `Ddd.Domain` and `Ddd.Application`: entity bases, DTO bases, and the generic repository

- **Status:** Accepted (the repository part is extended by [ADR-0045](0045-repository-levels-relational-and-document.md) and [ADR-0046](0046-dto-projection-without-a-mapper.md): `Include` left the common options)
- **Date:** 2026-10-25 (adds to [ADR-0030](0030-microservice-project-pattern.md), [ADR-0033](0033-the-abc-services-crud-pattern.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md))

## Context

Every Abc service wrote the same things again: the audit and soft delete members of its entity (about 25 lines), the four DTO classes of paging, a repository with `GetList`, `Count`, `Search` and a filter record, and an `if` chain that built the filter. The system before the rewrite (hhs, on the `HsnSoft.Base.Ddd.Domain` and
`HsnSoft.Base.Ddd.Application` packages) had them once in the framework: `AuditedEntity<Guid>`, `PagedDataRequestDto`, `PagedDataResultDto`, `FilterBuilder`, the repository with query options. They are taken back, smaller and with the faults left out.

## Decision

- **Two packages, each referenced where its layer is:** `HsnSoft.Base.Ddd.Domain` (referenced by the domain project of a service, which no longer depends on Core alone) and `HsnSoft.Base.Ddd.Application` (by the application project). `Ddd.Application` references `Ddd.Domain` (it turns a request into repository options).
- **`Ddd.Domain`:** `Entity<TKey>`, `CreationAuditedEntity<TKey>`, `AuditedEntity<TKey>` and `FullAuditedEntity<TKey>` (audit, and soft delete), as in hhs. **Tenants stay an explicit interface**: the entity implements `IMultiTenant` with `TenantId { get; private set; }`, and **`AssignTenant` is gone**: the persistence writes the
  property (EF Core through the change tracker, MongoDB through `TenantAssigner`). The repository contract `IRepository<TEntity, TKey>` and its options (below).
- **`Ddd.Application`:** the DTOs of an entity (`EntityDto`, `AuditedEntityDto`, ...), of a request (`LimitedDataRequestDto` → `SortedAndLimitedDataRequestDto` → `SearchDataRequestDto` → `PagedDataRequestDto`) and of a result (`ListDataResultDto`, `PagedDataResultDto` with the total, the page count and the numbers of the first and the last item), and
  the conversions between a request, repository options and a result (`ToPagedOptions`, `ToListOptions`, `ToPagedDto`). **The paging model is hhs's: `PageNumber` from 1 and `MaxResultCount` (the page size, 10 by default).** The most a client may ask for is the `Paging:MaxResultCountLimit` setting (100), not a static field.
- **The generic repository:** `IRepository<TEntity, TKey>` (`FindAsync`, `AnyAsync(..., ignoreFilters)`, `CountAsync`, `GetListAsync`, `GetPagedListAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`), implemented once for each store (`EfCoreRepository` in `EntityFrameworkCore`, `MongoRepository` in `MongoDB`), registered with
  `AddEfCoreRepository` / `AddMongoRepository`. The filters of the concepts (soft delete, tenants) apply to every method; each change is saved by its method. A service with a need of its own derives (`EfCoreUserRepository : EfCoreRepository<..>, IUserRepository`). The filter is an expression built in the application service
  with `FilterBuilder<T>` (a part left out is not applied); the text fields have a `Normalized...` twin (`StringHelper.Normalize`) to look for part of them.
- **Sorting is a whitelist, not a text in a query:** a client sends `"name asc, price desc"`; `SortFields<TEntity>` (declared once for the entity: `.Add("name", p => p.NormalizedName)`) turns it into a `SortSpec`, and a field that is not offered is refused with `Validation:UnknownSortField` (a column that was not offered, a password hash, cannot be
  sorted by, and nothing the client sent is part of a query). A repository applies the `SortSpec` through a visitor (a queryable for EF Core, a sort definition for MongoDB). Without a sorting the key orders a page.
- **MongoDB maps the base classes once:** the driver wants the members of a base class mapped in its own class map, so `MongoEntityMaps.RegisterBases<TKey>()` registers those of the entity bases and the map of an entity has only what is its own.
- **Mapping an entity to its DTO stays by hand** (a `ToDto` in the application service); a mapper is a decision for later.

## Consequences

- An entity is `class Product : FullAuditedEntity<Guid>, IMultiTenant` with its business members; a service's repository, filter record and four DTO classes are gone, and an application service builds a filter and calls the repository.
- The responses of the lists changed: a page has `dataTotalCount`, `pageTotalCount`, `resultDataStartNumber`, `resultDataEndNumber`; a request has `pageNumber`, `maxResultCount`, `sortingText`, `searchText`.
- The domain project depends on `Ddd.Domain` (and, through it, Core); ADR-0030's "nothing but Core" is replaced by "Core and Ddd.Domain".
- A relational store loads what the query says (`Include`); a document store ignores `Include`.

## Related

[ADR-0030](0030-microservice-project-pattern.md), [ADR-0033](0033-the-abc-services-crud-pattern.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0041](0041-application-service-base-in-the-framework.md), [`docs/DddBuildingBlocks.md`](../DddBuildingBlocks.md).

# Audit, soft delete and tenants of an entity

Three concepts an entity can have, each one by an interface of `HsnSoft.Base.Core` that the entity implements. The persistence does the rest; the application code writes none of it
([ADR-0037](adr/0037-audit-soft-delete-and-tenants.md)).

| An entity that implements | Has | Done by the persistence |
|---|---|---|
| `ICreationAudited` | `CreatedAt`, `CreatedBy` | when it is added |
| `IModificationAudited` | `UpdatedAt`, `UpdatedBy` | when it is added and every time it is changed |
| `ISoftDelete` | `IsDeleted`, `DeletedAt`, `DeletedBy` | a delete marks it; the lists do not show it |
| `IMultiTenant` | `TenantId` | a new one gets the current tenant; a caller sees only the tenants it may |

The entity keeps the fields with private setters and implements the `Mark...` methods (the base classes of [DddBuildingBlocks.md](DddBuildingBlocks.md) do it for the audit and the soft delete); the tenant is just `TenantId { get; private set; }`, which the persistence writes (`public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);`).
**`CreatedBy`, `UpdatedBy` and `DeletedBy` are the id of the user** (`user_id` claim, a guid), `null` when nobody signed in did (a seed, a worker).

## Who and which tenant: `ICurrentUser` and `ICurrentTenant`

`ICurrentUser` (Core; `HsnSoft.Base.Authentication` answers it from the token, `AddBaseJwtAuthentication` registers it) has `Id` (`user_id`), `UserName` (`sub`), `IsAuthenticated`, `TenantId` (`tenant_id`),
`IsSystemTenant` (`is_system_tenant` = `true`) and `AllowedTenantIds` (`allowed_tenant_id`, one claim each). `ICurrentTenant` is the tenant of the request (the user's) unless code acts as another one:

```csharp
using (currentTenant.Change(tenantId))            // a seed, a worker: work as this tenant; the previous one comes back
{
    ...
}
```

**The system tenant sees every tenant; anyone else sees its own and the ones in `allowed_tenant_id`; with no tenant nothing of a tenant is seen.**

## The filters, and switching them off: `IDataFilter`

Both filters (`ISoftDelete`, `IMultiTenant`) are on. Code that must see more turns one off for a while, and the scope ends with the `using` (it follows the flow of the code, across `await`):

```csharp
using (dataFilter.Disable<ISoftDelete>()) { ... }     // the deleted are seen too
```

## EF Core

Derive the context from `BaseDbContext` (mappings in `ConfigureModel`, not `OnModelCreating`) and register it with `AddBasePostgres` / `AddBaseSqlServer`, which add the interceptor:

```csharp
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureOrderDomain();
}
```

`BaseDbContext` puts the query filters on the entities that have the concepts. `BaseEntityInterceptor` fills the audit fields and the tenant on save and turns the delete of an `ISoftDelete` entity into an update
(what the entity owns is kept with it). `IgnoreQueryFilters()` shows everything (add `!IsDeleted` when the deleted must stay hidden).

## MongoDB

MongoDB has no change tracking, so the repository asks `MongoEntityConcepts` (registered by `AddBaseMongo`): `Visible(filter)` adds the filters to a query, `Created(entity)`, `Modified(entity)` before a write, and
`SoftDeleted(entity)` before a delete (`true`: replace the document, it was marked; `false`: delete it). The filters read the elements `IsDeleted` and `TenantId`, so map the properties under their names.

## Not done

A unique index still counts the deleted rows and the other tenants' rows (a user name or a sku is unique in the whole system); the application checks it without the filters to answer a business error. Cascades
to what an entity references are not soft.

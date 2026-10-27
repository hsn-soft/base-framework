# Audit, soft delete and tenants of an entity

Three concepts an entity can have, each one by interfaces that the entity implements: the audit contracts are in `HsnSoft.Base.Auditing`, soft delete in `HsnSoft.Base.Core`, the switch of the filters in `HsnSoft.Base.Data` and the tenants in `HsnSoft.Base.MultiTenancy` (and four packages that put them on the token, the messages, EF Core and MongoDB: a service that has no tenants has none of them, [ADR-0074](adr/0074-multitenancy-is-a-package-of-its-own.md)).
The persistence does the rest; the application code writes none of it ([ADR-0037](adr/0037-audit-soft-delete-and-tenants.md), [ADR-0073](adr/0073-auditing-and-data-are-packages-of-their-own.md)).

| An entity that implements | Has | Done by the persistence |
|---|---|---|
| `ICreationAuditedObject` (`IHasCreationTime`, `IMayHaveCreator`) and `IMarkCreated` | `CreatedAt`, `CreatedBy` | when it is added |
| `IModificationAuditedObject` (`IHasModificationTime`) and `IMarkModified` | `UpdatedAt`, `UpdatedBy` | when it is added and every time it is changed |
| `ISoftDelete`, `IDeletionAuditedObject` (`IHasDeletionTime`) and `IMarkDeleted` | `IsDeleted`, `DeletedAt`, `DeletedBy` | a delete marks it; the lists do not show it |
| `IMultiTenant` (`HsnSoft.Base.MultiTenancy`) | `TenantId` | a new one gets the current tenant; a caller sees only the tenants it may |

`IAuditedObject` is the creation and the modification together, `IFullAuditedObject` all three. These are **shapes that only show the fields**: the DTO of an entity (`CreationAuditedEntityDto<TKey>`, `AuditedEntityDto<TKey>`, `FullAuditedEntityDto<TKey>`) implements the same
ones as the entity, so one rule can read the audit fields of either. What only an entity takes are the marks (`IMarkCreated`, `IMarkModified`, `IMarkDeleted`): the entity keeps the fields with private setters and implements the `Mark...` methods (the base classes of
[DddBuildingBlocks.md](DddBuildingBlocks.md) do it for the audit and the soft delete); the tenant is just `TenantId { get; private set; }`, which the persistence writes itself.
**`CreatedBy`, `UpdatedBy` and `DeletedBy` are the id of the user** (`user_id` claim, a guid), `null` when nobody signed in did (a seed, a worker).

## Who: `ICurrentUser`

`ICurrentUser` (Core; `HsnSoft.Base.Authentication` answers it from the token, `AddBaseJwtAuthentication` registers it) has `Id` (`user_id`), `UserName` (`sub`), `IsAuthenticated` and `Roles`. Where nobody is signed in (a message handler, a worker) it answers from `ActingUser`.

## Tenants: a package of their own

A service chooses whether it has tenants; the framework carries no tenant unless the service references the packages, and each places one thing:

| Package | What it does | Registration |
|---|---|---|
| `HsnSoft.Base.MultiTenancy` | `IMultiTenant` (the entity), `ICurrentTenant` and `ITenantSource` (where the tenant comes from) | `AddBaseMultiTenancy()` (the others call it) |
| `HsnSoft.Base.MultiTenancy.Authentication` | the tenant of a request is the claims `tenant_id`, `is_system_tenant` and `allowed_tenant_id` (`TenantClaimTypes`; the auth server writes them) | `AddBaseTenantClaims()` |
| `HsnSoft.Base.MultiTenancy.EventBus` | the tenant is written into the headers of every message and a handler runs in the tenant of its message | `AddBaseTenantEventBus()` |
| `HsnSoft.Base.MultiTenancy.EntityFrameworkCore` | `MultiTenantDbContext` (the tenant filter), and the interceptor that gives a new entity the tenant | `AddBaseMultiTenancyEntityFramework()` |
| `HsnSoft.Base.MultiTenancy.MongoDB` | the tenant filter and the tenant of a new document | `AddBaseMultiTenancyMongo()` |

`ICurrentTenant` is the tenant of the request (the first `ITenantSource` that knows one) unless code acts as another one:

```csharp
using (currentTenant.Change(tenantId))            // a seed, a worker: work as this tenant; the previous one comes back
{
    ...
}
```

**The system tenant sees every tenant; anyone else sees its own and the ones in `allowed_tenant_id`; with no tenant nothing of a tenant is seen.** A service without tenants does not reference any of this: its entities have no `TenantId`, its context derives from
`BaseDbContext`, and a message that crosses it keeps the tenant headers for the services after it (the Abc shipment service is such a service).

## The filters, and switching them off: `IDataFilter`

The filters (`ISoftDelete`, and `IMultiTenant` for a service with tenants) are on. Code that must see more turns one off for a while, and the scope ends with the `using` (it follows the flow of the code, across `await`):

```csharp
using (dataFilter.Disable<ISoftDelete>()) { ... }     // the deleted are seen too
```

## EF Core

Derive the context from `BaseDbContext` (mappings in `ConfigureModel`, not `OnModelCreating`) and register it with `AddBasePostgres` / `AddBaseSqlServer`, which add the interceptor:

```csharp
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IDataFilter? dataFilter = null)
    : BaseDbContext(options, dataFilter)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureOrderDomain();
}
```

A service with tenants derives from `MultiTenantDbContext` (`HsnSoft.Base.MultiTenancy.EntityFrameworkCore`) instead, which takes the `ICurrentTenant` too (`: MultiTenantDbContext(options, dataFilter, currentTenant)`) and registers `AddBaseMultiTenancyEntityFramework()`.
The filters are named (`SoftDelete`, `MultiTenant`), so each can be ignored on its own.

`BaseDbContext` puts the query filter on the entities that have the concept. `BaseEntityInterceptor` fills the audit fields on save and turns the delete of an `ISoftDelete` entity into an update
(what the entity owns is kept with it). `IgnoreQueryFilters()` shows everything (add `!IsDeleted` when the deleted must stay hidden).

## MongoDB

MongoDB has no change tracking, so the repository asks `MongoEntityConcepts` (registered by `AddBaseMongo`): `Visible(filter)` adds the filters to a query, `Created(entity)`, `Modified(entity)` before a write, and
`SoftDeleted(entity)` before a delete (`true`: replace the document, it was marked; `false`: delete it). The filters read the elements `IsDeleted` and `TenantId`, so map the properties under their names. What a package adds (the tenants of `MultiTenancy.MongoDB`) is an `IMongoEntityConcept` that `MongoEntityConcepts` asks on every query and insert.

## Not done

A unique index still counts the deleted rows and the other tenants' rows (a user name or a sku is unique in the whole system); the application checks it without the filters to answer a business error. Cascades
to what an entity references are not soft.

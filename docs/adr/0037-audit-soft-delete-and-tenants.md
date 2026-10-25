# ADR-0037: Audit, soft delete and tenants of an entity

- **Status:** Accepted (`IMultiTenant` has no `AssignTenant` now: the persistence writes `TenantId`; the entity bases are in `Ddd.Domain`, [ADR-0042](0042-ddd-domain-and-application-packages.md))
- **Date:** 2026-10-25 (replaces the hand-written `CreatedBy`/`UpdatedBy` of [ADR-0033](0033-the-abc-services-crud-pattern.md) and the shape of `ICurrentUser` of [ADR-0036](0036-icurrentuser-in-the-framework.md))

## Context

The services of the rewrite wrote their audit fields by hand (the application service passed the time and the user name into the entity), soft delete was an unused interface and there was no multitenancy. The framework before the rewrite
had all three (`IAuditPropertySetter` filled on the change tracker, `ISoftDelete` with a global query filter, `IMultiTenant` with tenant claims and filters, `IDataFilter` to switch them off) and every product used them. They are taken back, smaller:
what depended on the old dependency-injection conventions, on `IScopeSubscription` (a product's rule) and on the customer subscription filters is left out.

## Decision

- **Interfaces in `HsnSoft.Base.Core`:** `ICreationAudited`, `IModificationAudited` (`HsnSoft.Base.Auditing`), `ISoftDelete` (`HsnSoft.Base`, with `DeletedAt`, `DeletedBy` and `MarkDeleted`), `IMultiTenant` (`TenantId`, `AssignTenant`), `ICurrentTenant`, `IDataFilter`, `IAuditPropertySetter`.
  An entity keeps the fields with private setters and implements the `Mark...` methods: no reflection, and the entity decides what the call does. **The application and the domain no longer pass the time or the user into an entity**: `Create` and `Update` have only the business values.
- **The user is the id (a guid) in the audit fields** (`user_id` claim), not the user name: a renamed user does not change the history. `ICurrentUser` is now `Id`, `UserName` (`sub`), `IsAuthenticated`, `TenantId`, `IsSystemTenant`, `AllowedTenantIds`.
- **Tenants come from the token:** `tenant_id`, `is_system_tenant` (`true`) and `allowed_tenant_id` (one claim per tenant). `CurrentTenant` is the user's tenant unless `Change(...)` acts as another (a seed, a worker; it follows the flow of the code). A tenant always allows itself.
  **The system tenant sees every tenant; anyone else sees its own and the allowed ones; without a tenant nothing of a tenant is seen.** The new entity gets the current tenant when it has none.
- **`IDataFilter`** switches the filters of `ISoftDelete` and `IMultiTenant` off per flow of the code (`using (filter.Disable<ISoftDelete>())`); both are on by default.
- **EF Core:** `BaseDbContext` (mappings in `ConfigureModel`) puts the query filters on the entities that have the concepts; `BaseEntityInterceptor` fills the audit fields and the tenant on save and turns the delete of an `ISoftDelete` entity into an update (its owned
  entities are kept). `AddBasePostgres` / `AddBaseSqlServer` register the interceptor.
- **MongoDB:** no change tracking, so `MongoEntityConcepts` (registered by `AddBaseMongo`) is called by the repository: `Visible` for queries, `Created` / `Modified` before a write, `SoftDeleted` before a delete.
- **Where they live:** the interfaces and the default implementations are in `HsnSoft.Base.Core` (registered by `AddBaseEntityConcepts`, which the database providers call); `ICurrentUser` is implemented by `HsnSoft.Base.Authentication`.
- **In the Abc system:** the four entities (`User`, `Product`, `Order`, `Shipment`) have all four concepts. There are two tenants (`AbcTenants.System`, `AbcTenants.Acme`): `admin` is of the system tenant and `alice` of Acme; the auth server puts the claims in the token; the seeded products are Acme's.
  A unique value (a user name, a sku) is unique in the whole system, so the application checks it without the filters, and the auth server finds a user in every tenant (but not a deleted one).

## Consequences

- A service gets audit, soft delete and tenants by implementing the interfaces and deriving its context from `BaseDbContext`; the repositories of a MongoDB service call `MongoEntityConcepts`.
- Databases made before this change have to be dropped (new columns); the Mongo documents of the earlier version lack the fields.
- A soft-deleted row stays under a unique index, so its value cannot be used again; the application says the value is taken.
- The tenant filter hides the entities of other tenants everywhere a query goes through the context; raw SQL and `IgnoreQueryFilters()` do not have it.
- Who may do what (an `admin` policy) is still not decided.

## Related

[ADR-0033](0033-the-abc-services-crud-pattern.md), [ADR-0035](0035-the-persistence-project-registers-its-database.md), [ADR-0036](0036-icurrentuser-in-the-framework.md), [ADR-0032](0032-the-auth-server-reads-the-users-of-the-identity-service.md), [`docs/EntityConcepts.md`](../EntityConcepts.md).

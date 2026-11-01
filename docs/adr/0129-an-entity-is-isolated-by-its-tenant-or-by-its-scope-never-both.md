# ADR-0129: An entity is isolated by its tenant or by its scope, never both

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0128](0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md))

## Context

Besides the data of a tenant, a product has data that belongs to a **subscription**: a company has customers, and a customer is not under a tenant. One tenant sold the customer a product and another tenant sold it another, so each must manage the data of the product it sold and of no
other, though the customer is the same. A tree of tenants cannot say that (the customer is under no tenant), so the old system isolated this data by a **scope key**: a text made for a subscription (in the old product, the customer and the product, `{customerId:N}:{product}`) kept in the row, with the keys a user may
see in its token (`allowed_scope_key`). The old framework had both isolations, `IMultiTenant` and `IScopeSubscription`, in the same classes; the new one had only the tenant one.

## Decision

- **`IScopeSubscription { string ScopeKey }`** (`HsnSoft.Base.MultiTenancy`). The same packages carry it as the tenant: `ICurrentTenant.AllowedScopeKeys` (and `Change(..., allowedScopeKeys)`), `TenantContext.AllowedScopeKeys`, the claim `allowed_scope_key` (`TenantClaimTypes`, one claim per key, read by
  `AddBaseTenantClaims`), the EF Core filter of `MultiTenantDbContext` (`AllowedScopeKeys.Contains(ScopeKey)`) and the MongoDB concept (`ScopeKey in (...)`).
- **The system tenant is under no isolation** and sees every scope as every tenant; **without scope keys nothing of a scope is seen**; `IDataFilter.Disable<IScopeSubscription>()` switches the filter off apart from the tenant filter.
- **One isolation for an entity, never both:** an entity that is `IMultiTenant` and `IScopeSubscription` is refused (when the EF model is built, when a MongoDB document is queried): it would be hidden from whoever does not satisfy both, and the two are different isolations of different data. The users and roles of a context are
  isolated by tenant and its content by scope, in different tables.
- **The product makes the key and sets it** (the framework does not fill `ScopeKey` in, as it fills `TenantId` in: only the product knows the subscription of a new row), and puts the keys into the token (from the subscriptions of the roles of a user, in hhs).
- **Events carry the tenant mechanism only** (the headers `TenantId`, `IsSystemTenant`): a message that needs a scope key has it in its body.

## Consequences

- A service has tenant-isolated and scope-isolated tables in one context, and a token can say both what tenants and what scopes its user sees.
- The filter on a scope is a `Contains` over the keys of the token, so the token is as long as the keys it lists (the same limit of the size of a token as the list of tenants, [ADR-0128](0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md)).
- A product that needs a customer's data seen by the tenants that sold it something needs the subscriptions in its own identity data; the framework gives the isolation, not the subscriptions.
- Not here: a sample that shows it (a product's own data is needed for the subscriptions), and the scope keys in the headers of the events.

## Related

[docs/MultiTenancy.md](../MultiTenancy.md), [ADR-0128](0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md).

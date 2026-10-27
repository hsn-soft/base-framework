# ADR-0074: Multitenancy is a package of its own, and a service that does not want tenants does not get them

- **Status:** Accepted
- **Date:** 2026-10-27 (changes where [ADR-0037](0037-audit-soft-delete-and-tenants.md) put the tenants and what [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md) put in the envelope; follows [ADR-0002](0002-one-package-one-capability.md), [ADR-0003](0003-abstractions-separate-from-providers.md) and [ADR-0073](0073-auditing-and-data-are-packages-of-their-own.md))

## Context

Tenants were everywhere in the framework although a service may not want them: `IMultiTenant` and `ICurrentTenant` were in `Core`, `ICurrentUser` had `TenantId`, `IsSystemTenant` and `AllowedTenantIds`, `ApplicationService` and `ApiControllerBase` had a `CurrentTenant`,
`BaseDbContext` and `MongoEntityConcepts` took an `ICurrentTenant`, the envelope of a message had `TenantId` and `IsSystemTenant`, `ActingUser` carried a tenant, and `Authentication` read the tenant claims. A service without tenants could leave `IMultiTenant` off its entities
and not write the claims, and it worked, but it still carried the types, the constructor parameters and the registrations. The point of the framework is that **a host chooses the capabilities it uses** ([ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md)); tenants are one more.

## Decision

Everything about tenants moves out of the packages that do not need it into packages that do, and the packages that stay behave as before for a service without tenants.

- **`HsnSoft.Base.MultiTenancy`** (on nothing but the DI abstractions) holds `IMultiTenant`, `ICurrentTenant` and `CurrentTenant`, `TenantAssigner`, `TenantContext` and `ITenantSource`, and `AddBaseMultiTenancy()`.
  `ICurrentTenant` no longer reads `ICurrentUser`: it reads the first **`ITenantSource`** that knows a tenant (a source answers `TenantContext?`), unless code acts as another tenant with `Change(...)`. Where the tenant comes from is a source, so the token and the message are two packages, not two cases in one.
- **`HsnSoft.Base.MultiTenancy.Authentication`** (on `MultiTenancy`) is the source of a request: the claims `tenant_id`, `is_system_tenant` and `allowed_tenant_id` (`TenantClaimTypes`, which the auth server writes with), `AddBaseTenantClaims()`.
- **`HsnSoft.Base.MultiTenancy.EventBus`** (on `EventBus` and `MultiTenancy`): the tenant travels with the messages as **headers** (`TenantId`, `IsSystemTenant`), written by an `IEventHeaderProvider` and read by an `IEventHandlerScope` that runs the handler in the tenant of the message
  (`ICurrentTenant.Change`) and puts `TenantId` in the logs of the handling. `AddBaseTenantEventBus()`. `EventBus` got `IEventHandlerScope` for it: a scope that a package opens around a handler, registered as a scoped service, begun before the handler and disposed after, each attempt.
  Because the tenant is a header, **a service without tenants carries it along without knowing it**: the headers of the message that caused a new one are inherited, so a tenantless service in the middle of a chain does not lose the tenant of the chain.
- **`HsnSoft.Base.MultiTenancy.EntityFrameworkCore`** (on `EntityFrameworkCore` and `MultiTenancy`): `MultiTenantDbContext`, a `BaseDbContext` that adds the tenant filter, and `TenantEntityInterceptor`, which gives an added entity the current tenant; `AddBaseMultiTenancyEntityFramework()` registers
  the interceptor as an `IInterceptor`, which `UseBaseEntityConcepts` puts on the context with the framework's own. `BaseDbContext` keeps only the soft-delete filter and has `ConfigureEntityFilters<TEntity>` for a derived context to add its own; the filters are **named** (EF Core 10), so the soft-delete
  and the tenant filter of one entity are two filters, and `IgnoreQueryFilters` can still switch each by name.
- **`HsnSoft.Base.MultiTenancy.MongoDB`** (on `MongoDB` and `MultiTenancy`): the tenant filter and the tenant of a new document, as an `IMongoEntityConcept` that `MongoEntityConcepts` asks on every query and insert. `AddBaseMultiTenancyMongo()`.
- **`ICurrentUser`** is the user only: `IsAuthenticated`, `Id`, `UserName`, `Roles`. **`UserContext`** (`ActingUser`) is `Id`, `UserName`, `Roles`. `ApplicationService` and `ApiControllerBase` have no `CurrentTenant`: a class that needs the tenant asks `ICurrentTenant` in its constructor.
  `AddBaseEntityConcepts()` no longer registers the tenant. `Authentication` has no tenant claims. The envelope has no `TenantId` and `IsSystemTenant` properties (the headers say it, and the wire has the same names).
- **A host registers what it has:** a service with tenants adds `AddBaseTenantClaims()`, `AddBaseTenantEventBus()` and, in its persistence, `AddBaseMultiTenancyEntityFramework()` or `AddBaseMultiTenancyMongo()`; a service without them adds nothing and has no reference to any of the five packages.
- **In the samples:** the Abc identity, order and product services and the auth server keep their tenants; **the Abc shipment service has none** (no `IMultiTenant`, no `TenantId` in its entities and DTOs, no tenant index, its context derives from `BaseDbContext`): it is the proof that a service takes the tenants it wants.
  It still passes the tenant through (the order is shipped for a tenant and the order service gets the answer in the same tenant). The Xyz system has tenants in all its services, which `Xyz.Shared` registers once.
- The tests of each package are in its own test project; the tests that exercised the tenant through `BaseDbContext`, `MongoEntityConcepts` and the envelope moved with the code.

## Consequences

- A service without tenants has no tenant type, parameter or registration; the unique indexes and the DTOs are the service's own (the shipment's customer user name is unique in the whole service).
- The graph of [ADR-0004](0004-core-once-and-the-dependency-graph.md) has five more rows. `Core` has no tenant, and `MultiTenancy` is the first package that does not reference `Core`: it needs nothing from it (the rule of ADR-0004 is about the types that several packages share, and nothing of `Core` is one of them for the tenants).
- The envelope of a message from a service without tenants has no `IsSystemTenant` (it used to carry `false`); readers treat a missing one as `false`, as before.
- A service with tenants adds three or four registrations it did not need to write before, and a reference per package: that is the price of choosing.
- `ICurrentUser` implementations of a product (a test, a host) lose the three tenant members; the tenant of a test is an `ITenantSource`.
- The tenant is no longer in `ActingUser`: a worker or an auth server that acts for a user in a tenant uses `ActingUser.Use(...)` for the user and `ICurrentTenant.Change(...)` for the tenant.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md), [ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md), [ADR-0073](0073-auditing-and-data-are-packages-of-their-own.md), [`docs/EntityConcepts.md`](../EntityConcepts.md).

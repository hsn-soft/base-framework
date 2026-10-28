# ADR-0029: The identity service manages the permissions; the sample roles get their starting grants

- **Status:** Accepted
- **Date:** 2026-10-27

## Context

The framework has a permission package (`HsnSoft.Base.Authorization`, [framework ADR-0081](../../../docs/adr/0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md)): the services ask who may do what and read the grants from a distributed cache. Somebody has to make the grants and write the cache. The old system had a service of its own for it (administration, beside identity); the Edx sample needs it in one service,
the identity service, and holds the system tenant to the same rules as the others.

## Decision

- **The identity service fills its `Permission` table from the constants of the catalogue at every start** (a new code is added, one that left the code is made inactive, nothing is deleted). A code is a public string constant of a class nested by service and by controller (`EdxEndpointPermissions.Intake.Files.Get` = `endpoint.intake.files.get`); the entity derives the type from the first part of the code.
- **Grants go to roles and to users**: `RolePermission` and `UserPermission` (both `IMultiTenant`, with the value of a constraint). A grant of a user adds to the roles' and its value of a constraint wins. There is **no denial and no dependency between permissions**, and **no copy table**: the entries of the cache are written from the grant tables.
- **The identity service writes the cache, the services read it.** A change of the grants of a role or user writes its entry at once; `PermissionCacheSyncService` writes every entry again at start and every `Permissions:SyncMinutes` (5), which makes right a cache that was lost and fills it the first time. The scope of an entry is the tenant (`IPermissionScopeProvider`), because the roles of a tenant are its own.
- **The identity service manages them**: the catalogue (read only), the grants of a role and of a user (replaced as a whole, in one transaction), and what the caller itself can do (`me/permissions`, which needs no permission). A tenant's administrator changes the grants of its own roles (the tenant filter makes any other role not found); the system tenant's, any tenant's. **The system tenant has no bypass**: its administrator is granted
  everything by the seed, as any role is granted anything. The sample roles get their starting grants once, on a new database (`DefaultPermissionGrants`), and are changed through the endpoints after that.
- **Where the rest lives:** the codes and the format of the scope are in `Edx.Shared.Helper`; the scope provider that reads the tenant and `AddEdxPermissions` are in `Edx.Shared.Hosting.Microservices` (the auth server and the gateway have no permissions: the services decide).

## Consequences

- A service that wants permissions has Redis, `AddBaseJwtAuthentication` and a call to `AddEdxPermissions`; one that does not want them pays nothing.
- A permission an endpoint asks for that is not in the catalogue is a grant nobody can make: the constants are the only list.
- Entries of a deleted role stay in the cache (roles cannot be deleted yet); a role that is deleted later has to remove its entry.

## Related

[framework ADR-0081](../../../docs/adr/0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [ADR-0002](0002-gateways-per-audience.md), [ADR-0030](0030-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md), [ADR-0031](0031-the-menu-is-data-in-the-identity-service.md), [../Authorization.md](../Authorization.md).

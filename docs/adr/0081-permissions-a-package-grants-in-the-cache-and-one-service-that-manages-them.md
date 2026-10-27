# ADR-0081: Permissions: one framework package, the grants kept in the distributed cache, one service that manages them

- **Status:** Accepted
- **Date:** 2026-10-27

## Context

Roles alone say too little about what a caller may do: an endpoint, a step inside it, a limit on a value. The old system had a permission model in a service of its own (administration, beside identity): a table of permissions, links to roles, a flattened copy of every grant that the services read from Redis through a store they refilled every hour or on an event, `[PermissionAuthorize]` on controllers, and codes in a `Shared.Helper` file. It worked, and it had
costs: the same grant was kept twice (the link and the copy), a change took up to an hour to be seen, the type of a permission was a string prefix checked by hand, and identity and administration were two services for one subject. The Edx sample needs it, in one service (the identity service), and the system tenant is held to the same rules as the others.

## Decision

- **A framework package, `HsnSoft.Base.Authorization`** (on `Core`, [ADR-0005](0005-where-code-lives.md): any product would use it unchanged): `[PermissionAuthorize(code)]` (policy `permission:{code}`, made on demand by `PermissionPolicyProvider`, so no policy is registered per permission), the handler, `IPermissionChecker` (is it granted, and the value of a constraint),
  `IPermissionStore` and its implementation over `IDistributedCache` (the host chooses Redis; the package asks for no provider), `IPermissionScopeProvider`, and `PermissionCatalog` (the codes of a class's nested constants). 401 for a caller not signed in, 403 for one not granted (gRPC `PermissionDenied`; a WCF operation asks the checker itself).
- **The catalogue is the code.** A permission is a public string constant of a class nested by service and by controller (`EdxEndpointPermissions.Intake.Files.Get` = `endpoint.intake.files.get`); the attribute names it, and the identity service fills its `Permission` table from the same constants at every start (a new code is added, one that left the code is made inactive, nothing is deleted).
  **Three types, told by the first part of the code, which the entity derives itself** (so they cannot disagree): `endpoint` (an action, asked by the attribute), `operation` (something inside an action, asked with `IPermissionChecker.IsGrantedAsync`) and `constraint` (a limit on a value, asked with `GetValueAsync` / `GetLongAsync`; a grant of it **carries the value**, and the
  setting of the service is the limit of whoever has none).
- **Grants go to roles and to users**: `RolePermission` and `UserPermission` (both `IMultiTenant`, with the value). A grant of a user adds to the roles' and its value of a constraint wins. There is **no denial and no dependency between permissions** (the old system's `PermissionDependency` is not carried over). A grant is made for the tenant of the role or the user, whoever sets it.
  There is **no copy table**: the entries of the cache are written from the two grant tables.
- **The services read the grants from the cache, the identity service writes them.** One entry for each subject (a role by its name, a user by its id) under a **scope**, which is the tenant here (`IPermissionScopeProvider`; the framework knows no tenant, the product says what a scope is), because the roles of a tenant are its own. A service keeps what it read in memory for `Permissions:CacheSeconds` (30), so a change is seen
  everywhere within that time and a check costs no round trip. A change of the grants of a role or user writes its entry at once; `PermissionCacheSyncService` writes every entry again at start and every `Permissions:SyncMinutes` (5), which makes right a cache that was lost and fills it the first time. Entries do not expire.
- **The identity service manages them**: the catalogue (read only), the grants of a role and of a user (replaced as a whole, in one transaction), and what the caller itself can do (`me/permissions`, which needs no permission). A tenant's administrator changes the grants of its own roles (the tenant filter makes any other role not found); the system tenant's, any tenant's. **The system tenant has no bypass**: its administrator is
  granted everything by the seed, as any role is granted anything. The sample roles get their starting grants once, on a new database (`DefaultPermissionGrants`), and are changed through the endpoints after that.
- **Where the rest lives:** the codes and the format of the scope are `Edx.Shared.Helper`; the scope provider that reads the tenant and `AddEdxPermissions` are `Edx.Shared.Hosting.Microservices` (the auth server and the gateway have no permissions: the services decide, [ADR-0027](0027-gateways-per-audience.md)).

## Consequences

- A service that wants permissions has Redis, `AddBaseJwtAuthentication` (`ICurrentUser`) and a call to `AddBasePermissions` (Edx: `AddEdxPermissions`); a service that does not want them pays nothing.
- A permission an endpoint asks for that is not in the catalogue is a grant nobody can make (the endpoint is closed to everyone): the constants are the only list, so this cannot happen with a constant, and a test of the sample's endpoints would check it.
- Entries of a deleted role stay in the cache (roles cannot be deleted yet); a role that is deleted later has to remove its entry.
- The old model's menu mapping (permissions of the pages of a UI) is not here; `me/permissions` is what a client uses.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0036](0036-icurrentuser-in-the-framework.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md), [ADR-0078](0078-the-edx-sample-a-file-intake-system.md), [ADR-0080](0080-edx-identity-tenants-roles-and-signing-in-by-tenant.md), [docs/Authorization.md](../Authorization.md).

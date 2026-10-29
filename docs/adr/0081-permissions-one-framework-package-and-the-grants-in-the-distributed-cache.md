# ADR-0081: Permissions: one framework package and the grants kept in the distributed cache

- **Status:** Accepted
- **Note:** [ADR-0083](0083-four-permission-types-and-constraints-that-must-be-given.md): the types are four (a page type was added) and a constraint must be given (no more "no value is no limit").
- **Date:** 2026-10-27

## Context

Roles alone say too little about what a caller may do: an endpoint, a step inside it, a limit on a value. A product that wants it needs a catalogue of permissions, grants to roles and users, and a cheap way for every service to ask. An earlier system kept a table of permissions, links to roles and a flattened copy of every grant that the services read from Redis through a store they refilled every hour or on an event;
it worked, and it had costs: the same grant was kept twice (the link and the copy), a change took up to an hour to be seen, and the type of a permission was a string prefix checked by hand.

## Decision

- **A framework package, `HsnSoft.Base.Authorization`** (on `Core`, [ADR-0005](0005-where-code-lives.md): any product would use it unchanged): `[PermissionAuthorize(code)]` (policy `permission:{code}`, made on demand by `PermissionPolicyProvider`, so no policy is registered per permission), the handler, `IPermissionChecker` (is it granted, and the value of a constraint),
  `IPermissionStore` and its implementation over `IDistributedCache` (the host chooses Redis; the package asks for no provider), `IPermissionScopeProvider`, and `PermissionCatalog` (the codes of a class's nested constants). 401 for a caller not signed in, 403 for one not granted (gRPC `PermissionDenied`; a WCF operation asks the checker itself).
- **The catalogue is the code.** A permission is a public string constant of a class nested by service and by controller (`OrdersEndpointPermissions.Orders.Create` = `endpoint.orders.create`); the attribute names it. **Types are told by the first part of the code, which the entity derives itself** (so they cannot disagree): `endpoint` (an action, asked by the attribute), `operation` (something inside an action,
  asked with `IPermissionChecker.IsGrantedAsync`) and `constraint` (a limit on a value, asked with `GetValueAsync` / `GetLongAsync`; a grant of it **carries the value**, and the setting of the service is the limit of whoever has none).
- **Grants go to roles and to users** (a subject is a role by its name or a user by its id): a grant of a user adds to the roles' and its value of a constraint wins. There is **no denial and no dependency between permissions**. There is **no copy table**: the entries of the cache are written from the grant tables of the service that manages them.
- **The services read the grants from the cache, the service that manages them writes them.** One entry for each subject under a **scope** (`IPermissionScopeProvider`; the framework knows no tenant, the product says what a scope is: a tenant, say, because the roles of a tenant are its own). A service keeps what it read in memory for `Permissions:CacheSeconds` (30), so a change is seen everywhere within that time and a check costs no round trip.
  Entries do not expire; the writer writes them at once on a change and writes every entry again from time to time, which makes right a cache that was lost.
- **The framework knows no product:** the codes and the format of the scope are the product's; the host registers the package with `AddBasePermissions` and a scope provider.

## Consequences

- A service that wants permissions has Redis, `AddBaseJwtAuthentication` (`ICurrentUser`) and a call to `AddBasePermissions`; a service that does not want them pays nothing.
- A permission an endpoint asks for that is not in the catalogue is a grant nobody can make (the endpoint is closed to everyone): the constants are the only list, so this cannot happen with a constant.
- Entries of a deleted role stay in the cache; whoever deletes a role has to remove its entry.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0036](0036-icurrentuser-in-the-framework.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md), [ADR-0083](0083-four-permission-types-and-constraints-that-must-be-given.md), [docs/Authorization.md](../Authorization.md).

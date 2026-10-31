# Authorization

`HsnSoft.Base.Authorization` lets an endpoint ask for a **permission** and the code ask what the caller may do. Why it is this way: [ADR-0081](adr/0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [ADR-0083](adr/0083-four-permission-types-and-constraints-that-must-be-given.md).

## Who the caller is: the token

`AddBaseJwtAuthentication` checks the bearer token; `AddBaseAuthorization` adds the standard policies. A host that reads tokens but authorizes nothing (a gateway) calls the first only; a host that is open to its callers calls neither ([ADR-0122](adr/0122-a-service-has-its-own-audiences-and-chooses-authentication-and-authorization.md)).

```json
"Jwt": { "Issuer": "abc-auth", "Audience": "abc-order", "PublicKeyFile": "public_key.xml", "ValidateIssuer": null, "ClockSkewSeconds": 30 }
```

- **`Audience`** is the name of *this host*: the `aud` claim of a token may be a list, and the token is accepted when the list has this audience. Every service has its own, so a client whose token does not name a service cannot call it (the
  gateways multiply; a client reaches the services whose audience its token has). The host that issues tokens lists what it puts into the `aud` of a token in **`IssuedAudiences`** (and may be told fewer for a client that reaches fewer services).
- **The issuer** is checked in every environment except Development; `ValidateIssuer` (true/false) says it for an environment.
- **`ClockSkewSeconds`** (default 30, at most 300) is the difference of two clocks that is forgiven when the lifetime of a token is checked.

## In a service

```csharp
builder.Services
    .AddBaseRedis(configuration)                       // an IDistributedCache: where the grants are read from
    .AddBaseJwtAuthentication(configuration)           // ICurrentUser: the user and its roles
    .AddBaseAuthorization()                            // the standard policies ([Authorize], BasePolicies.Authenticated)
    .AddBasePermissions(configuration)                 // the attribute, the handler, the checker, the store
    .AddPermissionScope<TenantPermissionScope>();      // only when the product has scopes (the tenant)
```

```csharp
[PermissionAuthorize(ShopEndpointPermissions.Order.Orders.Read)]    // 401 when not signed in, 403 when not granted
[HttpPost("paged-list")]
public Task<PagedDataResultDto<OrderDto>> GetPagedListAsync(...) => ...;
```

```csharp
// inside an action: a constraint that must be given (403 before anything is read when it is not), and an operation
decimal limit = await permissions.RequireDecimalAsync(ShopConstraintPermissions.Order.MaxViewAmount);
bool price = await permissions.IsGrantedAsync(ShopOperationPermissions.Order.PriceView);
```

gRPC takes the attribute on a method as a controller does (`PermissionDenied` for a refusal); a WCF operation is not an endpoint of the pipeline, so it asks `IPermissionChecker` itself.

## The permissions

A permission is a code, `{type}.{resource}.{action}` (three parts at least), in small letters, digits and dashes, and is a public string constant of a class nested by resource; the constants are the only list (`PermissionCatalog.CodesOf(typeof(ShopEndpointPermissions))` reads them).
**The first part is the type** (`PermissionType`: `Page = 1`, `Endpoint = 2`, `Operation = 3`, `Constraint = 4`, in the `Permission` table; the entity derives it from the code).

| Type | Code | Asked by | What it decides |
|---|---|---|---|
| Page | `page.order.view` | the client (menu, route) | the item is in the menu and the page opens; the client only |
| Endpoint | `endpoint.order.read`, `endpoint.order.create` | `[PermissionAuthorize]` | the call is let in (403 otherwise): a page reads with one and has a button for each of the others |
| Operation | `operation.order.price.view` | `IPermissionChecker.IsGrantedAsync` | inside an action: a field is open or masked before it leaves the service |
| Constraint | `constraint.order.max-view-amount` | `IPermissionChecker.Require...Async` | inside an action: the limit or the filter of the business, **with the value the role was given; a caller with none is refused (403) before anything is done** |

The constraints of the framework: `GetValueAsync` (and `GetIntAsync`, `GetLongAsync`, `GetDecimalAsync`, `GetBooleanAsync`) answer `null` when there is none; `RequireValueAsync` (and `RequireIntAsync`, `RequireLongAsync`, `RequireDecimalAsync`) refuse.

## Who has what

A grant is for a **role** (by its name), or for a **user** (by its id; the framework supports it, a system that wants it keeps the grants of its users), under a **scope** (the tenant: `IPermissionScopeProvider`, so a role of one tenant is not the role of the same name in another). The checker looks at the user first, then each role of
the token; any grant is a grant (there is no denial), and for a value the first one found is the answer.

Where the grants are kept: **`IPermissionStore`**, over `IDistributedCache` as JSON, one entry for each subject, key `{Permissions:KeyPrefix}:{scope}:{role|user}:{name or id}`, in small letters. A service reads and keeps what it read in memory for `Permissions:CacheSeconds` (30). The service that
manages the grants writes with `SetAsync`.

| Setting (section `Permissions`) | Default | Means |
|---|---|---|
| `KeyPrefix` | `permission` | the prefix of the keys, so the grants have a place of their own in the cache |
| `CacheSeconds` | 30 | how long a service keeps what it read (0: every time); the longest a change takes to be seen |

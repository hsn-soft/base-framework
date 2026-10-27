# Authorization

`HsnSoft.Base.Authorization` lets an endpoint ask for a **permission** and the code ask what the caller may do. Why it is this way: [ADR-0081](adr/0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md).

## In a service

```csharp
builder.Services
    .AddBaseRedis(configuration)                       // an IDistributedCache: where the grants are read from
    .AddBaseJwtAuthentication(configuration)           // ICurrentUser: the user and its roles
    .AddBasePermissions(configuration)                 // the attribute, the handler, the checker, the store
    .AddPermissionScope<TenantPermissionScope>();      // only when the product has scopes (the tenant)
```

```csharp
[PermissionAuthorize(EdxEndpointPermissions.Intake.Files.Get)]      // 401 when not signed in, 403 when not granted
[HttpPost("get/{id:guid}")]
public Task<StoredFileDto> GetAsync(Guid id, CancellationToken cancellationToken) => ...;
```

```csharp
// inside an action: an operation, and a limit
if (await permissions.IsGrantedAsync(EdxOperationPermissions.Intake.FileStorageLocationView)) { ... }
long limit = Math.Min(setting, await permissions.GetLongAsync(EdxConstraintPermissions.Intake.UploadMaxBytes) ?? long.MaxValue);
```

gRPC takes the attribute on a method as a controller does (`PermissionDenied` for a refusal); a WCF operation is not an endpoint of the pipeline, so it asks `IPermissionChecker` itself.

## The permissions

A permission is a code, `{type}.{service}.{subject}.{action}`, in small letters, digits and dashes, and is a public string constant of a class nested by service and by controller; the constants are the only list. `PermissionCatalog.CodesOf(typeof(EdxEndpointPermissions))` reads them.

| Type | First part | Asked by | Carries |
|---|---|---|---|
| Endpoint | `endpoint.` | `[PermissionAuthorize]` | |
| Operation | `operation.` | `IPermissionChecker.IsGrantedAsync` | |
| Constraint | `constraint.` | `IPermissionChecker.GetValueAsync` (`GetIntAsync`, `GetLongAsync`, `GetDecimalAsync`, `GetBooleanAsync`) | a value, given by the grant |

## Who has what

A grant is for a **role** (by its name) or a **user** (by its id), under a **scope** (the tenant: `IPermissionScopeProvider`, so a role of one tenant is not the role of the same name in another). The checker looks at the user first, then each role of the token; any grant is a grant (there is no denial), and for a value the first one found
is the answer (the user's own, then the roles' in the order of the token).

Where the grants are kept: **`IPermissionStore`**, over `IDistributedCache` as JSON, one entry for each subject, key `{Permissions:KeyPrefix}:{scope}:{role|user}:{name or id}`, in small letters. A service reads and keeps what it read in memory for `Permissions:CacheSeconds` (30). The service that manages the grants (Edx: the identity service) writes with `SetAsync`.

| Setting (section `Permissions`) | Default | Means |
|---|---|---|
| `KeyPrefix` | `permission` | the prefix of the keys, so the grants have a place of their own in the cache |
| `CacheSeconds` | 30 | how long a service keeps what it read (0: every time); the longest a change takes to be seen |

## The Edx identity service

It holds the catalogue (`Permission`: code, type, name, active), the grants (`RolePermission`, `UserPermission`, each with the optional value) and writes the entries to the store when a grant changes, at start and every `Permissions:SyncMinutes` (5).

| Endpoint (`api/identity-service/v1/commercial/...`) | Permission | Does |
|---|---|---|
| `POST permissions/filter-list` | `endpoint.identity.permissions.filter-list` | the catalogue |
| `POST roles/{roleId}/permissions/get` | `endpoint.identity.role-permissions.read` | what a role was granted |
| `PUT roles/{roleId}/permissions` | `endpoint.identity.role-permissions.update` | replaces it: `{ "grants": [ { "code": "...", "value": "..." } ] }` (a constraint needs a value, any other permission takes none) |
| `POST users/{userId}/permissions/get`, `PUT users/{userId}/permissions` | `endpoint.identity.user-permissions.read` / `.update` | the same for a user |
| `POST me/permissions/get` | none (any signed-in user) | what the caller can do: its own grants and its roles' |

A caller reaches the roles and users of its own tenant; a caller of the system tenant reaches every tenant's. The grants a new database starts with are in `DefaultPermissionGrants`.

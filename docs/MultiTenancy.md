# Tenants that see other tenants

A tenant sees its own data. When tenants are in a tree (a company and its customers: Techsummus has Veriban and Bilgera), the tenant at the top also sees the data of the tenants under it, at any depth, and they do not see each other or the one above.
([ADR-0128](adr/0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md))

```
Techsummus            /{T}/             sees Techsummus, Veriban, Bilgera
├── Veriban           /{T}/{V}/         sees Veriban
└── Bilgera           /{T}/{B}/         sees Bilgera
System tenant         (no path needed)  sees every tenant
```

## How it works

- **The filter is one list.** `ICurrentTenant.AllowedTenantIds` is the tenant of the caller and the ones its token names with `allowed_tenant_id` claims. The query filter of an `IMultiTenant` entity is `AllowedTenantIds.Contains(TenantId)` (EF Core), `TenantId in (...)` (MongoDB);
  the system tenant has no filter; with no tenant nothing of a tenant is seen. A new entity always gets the tenant of the caller. The filter is switched off with `IDataFilter` where a service must (a sign-in, a seed).
- **The tree is the product's data** (its tenant table has `ParentId` and a path), and the auth server writes the list into the token when it issues it: the tenants under the tenant of the user, not the tenant itself.
- **`TenantAccessPath`** is the path: the ids of the tenants from the root down, each in 32 small hex digits (`Guid.ToString("N")`) and each followed by `/`: `/{D}/{C}/{B}/` is B under C under D. The tenants a tenant may see are the ones whose path starts with its own: one indexed range query, no recursion.
  - Ids, not names: a tenant that is renamed keeps its path, and a tenant that is moved is a change of the start of the paths of everything under it (`TenantAccessPath.Move`).
  - Every id ends with `/`: `/A/CC/` does not start `/A/CCD/` (with names and `StartsWith("/A/CC")` the tenant `CCD` is found too, which is somebody else's data).
- **The list is in the token, so it is short on purpose:** a token that names a thousand tenants is thirty-six thousand characters, more than a host accepts in its headers (`Hosting:Kestrel:MaxRequestHeadersTotalSize`, 32 KB). The auth server of a product has a limit (the Edx sample: `Tenants:MaxAllowedPerToken`, 50) and refuses to sign in a tenant that has more under it, with the reason. A tenant that has
  more is not seen by a list of ids but by the scope of its data (the scope key).
- **A tenant created after a token is issued** is seen by the next token (the list is a snapshot of the sign-in).

```csharp
string path = TenantAccessPath.Root(parent) ;                               // /{parent}/
string child = TenantAccessPath.Child(path, childId);                       // /{parent}/{child}/
bool sees = TenantAccessPath.IsSelfOrDescendantOf(child, path);             // true: the parent sees the child
ids = db.Tenants.Where(t => t.AccessPath.StartsWith(path) && t.Id != id);   // what goes into the token
```

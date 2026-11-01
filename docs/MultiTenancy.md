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
  the system tenant has no filter; with no tenant nothing of a tenant is seen. A new entity always gets the tenant of the caller. (A different isolation, by the scope of a subscription, is at the end of this page.) The filter is switched off with `IDataFilter` where a service must (a sign-in, a seed).
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

## Entities isolated by a scope (a subscription), not by a tenant

Some data does not belong to a tenant: it belongs to a **subscription** (a customer's product), and several tenants may manage it, each the part it sold. A company has customers; a customer is not under a tenant; tenant T1 sold the customer the product X and tenant T2 the product Y, so T1 sees the data of
(customer, X) and T2 the data of (customer, Y), though it is one customer. A tree of tenants cannot say that. A **scope key** can: a text the product makes for a subscription (hhs: `{customerId:N}:{product}`), written in the entity and in the token.
([ADR-0129](adr/0129-an-entity-is-isolated-by-its-tenant-or-by-its-scope-never-both.md))

```csharp
public sealed class Content : IScopeSubscription          // the data of a subscription
{
    public string ScopeKey { get; private set; }          // the product sets it: "…abc…:video"
}
public sealed class Account : IMultiTenant { … }          // the data of a tenant, in the same context, in another table
```

- **The filter** is `AllowedScopeKeys.Contains(ScopeKey)` (EF Core), `ScopeKey in (...)` (MongoDB); `ICurrentTenant.AllowedScopeKeys` is the claims `allowed_scope_key` of the token (one claim per key); the system tenant is under no isolation and sees everything; with no keys nothing of a scope is seen.
  `IDataFilter` switches it off with `Disable<IScopeSubscription>()`, apart from the tenant filter (`Disable<IMultiTenant>()`).
- **An entity has one isolation, never both.** `MultiTenantDbContext` refuses an entity that is `IMultiTenant` and `IScopeSubscription` when the model is built (MongoDB: when it is queried), because it would be hidden from whoever does not satisfy both. A context has both kinds, in different tables: users and roles by tenant, content by scope.
- **The product sets the key.** The persistence does not fill `ScopeKey` in (it fills `TenantId` in from the caller): only the product knows which subscription a new row belongs to. How a key is made, and which keys go into a token (from the subscriptions of the roles of the user), is the product's.
- **Events** carry the tenant of the caller (the headers of `HsnSoft.Base.MultiTenancy.EventBus`), not the scope keys: a message that needs a scope has it in its body.

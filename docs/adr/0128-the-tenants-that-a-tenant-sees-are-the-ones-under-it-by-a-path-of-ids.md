# ADR-0128: The tenants that a tenant sees are the ones under it, by a path of ids

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md); the filter itself, `AllowedTenantIds`, exists since the tenant packages)

## Context

A system of companies and their customers needs a tenant that sees its own data **and** the data of the tenants under it (a company and its customers), where the customers do not see each other or the company. The framework already had the filter for it: `ICurrentTenant.AllowedTenantIds`
from the claims `allowed_tenant_id`, an EF Core filter `Contains(TenantId)` and a MongoDB filter `In`, the system tenant seeing every tenant. What no product had was the tree and the way to fill the list. The old system kept `ParentId` and a path of tenant **names**
(`/A/CC`) and found the tenants under a tenant with `path.StartsWith(ownPath)`; that has two faults: it also finds `/A/CCD` (a tenant whose name begins like another's: its data is shown to somebody else), and a tenant that is renamed or moved has a wrong path (and the
paths of everything under it) with nothing to correct them. The list is in the token, and a token that names a thousand tenants is about thirty-six thousand characters, more than a host accepts in its headers.

## Decision

- **`TenantAccessPath`** (`HsnSoft.Base.MultiTenancy`): the path of a tenant is the ids from the root down to it, each in `Guid.ToString("N")` (32 small hex digits) and each followed by `/`: `/{D}/{C}/{B}/`. `Root`, `Child`, `IsValid`, `IsSelfOrDescendantOf`, `Ids`, `Parent` and
  `Move` (the start of the paths of a tenant that moves and of everything under it). The ids never change and every id ends with `/`, so a rename changes nothing and a path never starts another that merely begins like it.
- **The tree is the product's data** (`ParentId` and the path in its tenant table, indexed), the auth server writes `allowed_tenant_id` for the tenants under the tenant of the user, **not** the tenant itself (the framework adds it), and none for the system tenant (it has no filter).
- **The list has a limit the product sets** (the Edx sample: `Tenants:MaxAllowedPerToken`, 50): a tenant with more under it is not signed in, with the reason (it is not cut, a list that hides tenants without saying so is worse). A tenant with many under it is seen by the scope of its data (the scope key), not
  by a list of ids.
- **A new entity gets the tenant of the caller**, never one of the allowed ones; the filter decides what is seen, so a tenant that sees a tenant under it can also change what it sees (a company manages its customers).

## Consequences

- A company sees its customers, at any depth, with the filters the framework had; the customers see only themselves.
- A tenant can be renamed and moved without its paths being wrong, and a path cannot show somebody else's tenant.
- A token carries a short list; a system with a tenant that has hundreds of tenants under it needs the scope key.
- Not here: the events (a message carries the tenant of the caller and not the list: a handler acts in that tenant; whether it should carry the list is not decided), the scope key.

## Related

[docs/MultiTenancy.md](../MultiTenancy.md), [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md).

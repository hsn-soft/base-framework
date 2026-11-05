using HsnSoft.Base.Authorization;
using HsnSoft.Base.MultiTenancy;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Hosting.Microservices;

/// <summary>The scope of the grants of a request: the tenant of the caller. Roles are a tenant's own, so the grants of a role are looked for under the tenant it belongs to; a call with no tenant has no scope and nothing is granted.</summary>
public sealed class TenantPermissionScope(ICurrentTenant tenant) : IPermissionScopeProvider
{
    public string? GetScope() => tenant.Id is { } id ? HsxPermissionScopes.ForTenant(id) : null;
}

using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.MultiTenancy;

namespace Edx.Shared.Hosting.Microservices;

/// <summary>The scope of the grants of the caller is its tenant: the roles of a tenant are its own, so the grants are looked up under the tenant of the request (or of the message being handled). No tenant, no scope.</summary>
public sealed class TenantPermissionScope(ICurrentTenant tenant) : IPermissionScopeProvider
{
    public string? GetScope() => tenant.Id is { } id ? EdxPermissionScopes.ForTenant(id) : null;
}

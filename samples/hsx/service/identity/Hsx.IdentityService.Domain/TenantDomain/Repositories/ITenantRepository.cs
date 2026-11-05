using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Domain.TenantDomain.Repositories;

/// <summary>What is asked of the tree of tenants.</summary>
public interface ITenantRepository : IRepository<Tenant, Guid>
{
    /// <summary>
    /// The tenants under this one, at any depth, <b>without</b> the tenant itself (its own id is always allowed): the ones whose path starts with the path of the tenant. They are what the token of a user of the tenant lets it see besides its own
    /// (<c>allowed_tenant_id</c>). The tenants that are not active are left out.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Whether any tenant is under this one.</summary>
    Task<bool> HasChildrenAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Moves a tenant under another (or to the root) and the tenants under it with it, in one step: the paths of all of them are changed together.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.TenantDomain.Exceptions.TenantLoopException">The new parent is the tenant itself or one of the tenants under it.</exception>
    Task MoveAsync(Tenant tenant, Tenant? newParent, CancellationToken cancellationToken = default);
}

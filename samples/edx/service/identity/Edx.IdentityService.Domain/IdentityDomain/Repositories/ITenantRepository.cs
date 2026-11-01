using Edx.IdentityService.Domain.IdentityDomain.Entities;

namespace Edx.IdentityService.Domain.IdentityDomain.Repositories;

/// <summary>What is asked of the tree of tenants.</summary>
public interface ITenantRepository
{
    /// <summary>
    /// The tenants under this one, at any depth, <b>without</b> the tenant itself (its own id is always allowed): the ones whose path starts with the path of the tenant. They are what the token of a user of the tenant lets it see besides its own
    /// (<c>allowed_tenant_id</c>). The tenants that are not active are left out.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Moves a tenant under another (or to the root) and the tenants under it with it, in one step: the paths of all of them are changed together.</summary>
    /// <exception cref="InvalidOperationException">The new parent is the tenant itself or one of the tenants under it (the tree would have a loop).</exception>
    Task MoveAsync(Tenant tenant, Tenant? newParent, CancellationToken cancellationToken = default);
}

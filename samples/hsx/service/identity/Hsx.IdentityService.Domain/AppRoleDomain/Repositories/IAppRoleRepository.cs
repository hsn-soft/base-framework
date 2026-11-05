using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;

namespace Hsx.IdentityService.Domain.AppRoleDomain.Repositories;

/// <summary>The repository of the roles of the tenants. The reads and the writes of a role are in the generic repository; this adds what the rules of a role ask.</summary>
public interface IAppRoleRepository : IRepository<AppRole, Guid>
{
    /// <summary>Whether the tenant has a role with this name (compared without regard to case); the role <paramref name="exceptId"/> is left out.</summary>
    Task<bool> NameExistsAsync(Guid tenantId, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default);

    /// <summary>The role with this id, whichever tenant it is of (the system tenant works across tenants); not a deleted one.</summary>
    Task<AppRole?> FindOfAnyTenantAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The roles of the tenant that sold the subscription whose type covers it: every Tenant role, the Company roles of its company, the Customer roles of its customer.</summary>
    Task<IReadOnlyList<AppRole>> GetCoveringAsync(Subscription subscription, CancellationToken cancellationToken = default);
}

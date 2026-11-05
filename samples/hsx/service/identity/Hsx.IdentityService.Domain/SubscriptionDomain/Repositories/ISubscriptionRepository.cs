using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;

/// <summary>
/// The repository of the subscriptions (the sales). The system tenant sells for a reseller and works across tenants, so the methods that say <c>OfAnyTenant</c> or <c>Active</c> look at every tenant's rows. The rules of a sale (who may sell, the links to the
/// roles, the transfer) are in the application service of the subscriptions, which uses this.
/// </summary>
public interface ISubscriptionRepository : IRepository<Subscription, Guid>
{
    /// <summary>The subscription with this id, whichever tenant sold it (not a deleted one); nothing when there is none.</summary>
    Task<Subscription?> FindOfAnyTenantAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Whether the customer has this product that is not ended, whoever sells it: a customer has a product once.</summary>
    Task<bool> HasActiveAsync(Guid customerId, ProductTypes product, CancellationToken cancellationToken = default);

    /// <summary>The ids of the subscriptions of a tenant that are not ended, for one company or one customer of it when they are given.</summary>
    Task<IReadOnlyList<Guid>> GetActiveIdsOfTenantAsync(Guid tenantId, Guid? companyId = null, Guid? customerId = null, CancellationToken cancellationToken = default);
}

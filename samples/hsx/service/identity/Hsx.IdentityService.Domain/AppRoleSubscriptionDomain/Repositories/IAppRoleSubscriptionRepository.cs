using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Repositories;

/// <summary>The repository of the links of the roles to the subscriptions (a role has the subscriptions its users see). It works across tenants: the system tenant links the roles of a reseller.</summary>
public interface IAppRoleSubscriptionRepository : IRepository<AppRoleSubscription, Guid>
{
    /// <summary>The ids of the subscriptions a role has.</summary>
    Task<IReadOnlyList<Guid>> GetSubscriptionIdsOfRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>The users that have a role with this subscription: they lose it when it ends.</summary>
    Task<IReadOnlyList<Guid>> GetUserIdsOfSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Takes the subscription from every role.</summary>
    Task DeleteForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}

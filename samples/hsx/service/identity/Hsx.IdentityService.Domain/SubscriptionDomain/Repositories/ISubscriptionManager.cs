using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;

/// <summary>A subscription that was made or changed, and the users whose roles lost a subscription by it (their sessions are ended by the caller).</summary>
public sealed record SubscriptionChange(Subscription Subscription, IReadOnlyList<Guid> UsersToSignOut);

/// <summary>
/// What is done to a sale, with the links of the roles to it, in one transaction: either both are there or neither. The system tenant is the caller, and it works across tenants (it sells for a reseller and the roles it updates are the reseller's), so what is read is
/// read without the filter of the tenant. The links follow the type of the role (<see cref="Hsx.IdentityService.Domain.AppRoleDomain.Entities.AppRoleType"/>): a Tenant role has every subscription of its tenant, a Company role those for its company, a Customer role those for its customer.
/// </summary>
public interface ISubscriptionManager
{
    /// <summary>Records that a reseller sells a product to a customer (the company is the customer's), and gives the subscription to the roles of the reseller that it matches.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    /// <exception cref="Exceptions.SubscriptionTenantInvalidException">The tenant is not a reseller.</exception>
    /// <exception cref="Exceptions.SubscriptionAlreadyActiveException">The customer has the product already.</exception>
    Task<Subscription> SellAsync(Guid tenantId, Guid customerId, ProductTypes product, DateTimeOffset? validTo, string? settingsJson, CancellationToken cancellationToken = default);

    /// <summary>Ends a sale: it gets its end and is taken from every role.</summary>
    /// <exception cref="Exceptions.SubscriptionEndedException">It has ended already.</exception>
    Task<SubscriptionChange> EndAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Passes a sale to another reseller: the old subscription ends and is taken from every role, a new one starts now for the other reseller (the same customer, product and settings; the scope key does not change) and is given to the roles of that reseller it matches.</summary>
    /// <exception cref="Exceptions.SubscriptionEndedException">It has ended already.</exception>
    /// <exception cref="Exceptions.SubscriptionTenantInvalidException">The other tenant is not a reseller, or it is the one that sells it.</exception>
    Task<SubscriptionChange> TransferAsync(Guid subscriptionId, Guid toTenantId, CancellationToken cancellationToken = default);

    /// <summary>Gives a role the subscriptions of its tenant that its type matches (a new role): the ones that are not ended.</summary>
    Task LinkRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

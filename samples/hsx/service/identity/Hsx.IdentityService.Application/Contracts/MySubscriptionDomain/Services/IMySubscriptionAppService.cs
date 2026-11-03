using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;

public interface IMySubscriptionAppService
{
    /// <summary>
    /// The subscriptions whose data the caller may see: the ones whose scope key is in its token (the system tenant, which is under no isolation, sees every one). What a subscription names (the company, the customer, the reseller) is read whoever made it:
    /// the token is what lets the caller see it.
    /// </summary>
    Task<List<MySubscriptionDto>> GetMineAsync(CancellationToken cancellationToken = default);
}

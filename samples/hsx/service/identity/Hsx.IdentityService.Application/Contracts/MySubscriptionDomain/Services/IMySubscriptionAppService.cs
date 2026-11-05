using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;

public interface IMySubscriptionAppService
{
    /// <summary>
    /// The subscriptions whose data the caller may see: the ones whose scope key is in its token (the system tenant, which is under no isolation, sees every one). What a subscription names (the company, the customer, the reseller) is read for the caller
    /// whichever reseller sold it: the token is what lets the caller see it.
    /// </summary>
    Task<ListDataResultDto<MySubscriptionDto>> GetMineAsync(CancellationToken cancellationToken = default);
}

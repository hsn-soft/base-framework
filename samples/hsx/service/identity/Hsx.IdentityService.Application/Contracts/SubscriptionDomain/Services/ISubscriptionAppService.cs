using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;

/// <summary>What can be done with the sales. A subscription belongs to the reseller that sells it: a reseller sees its own and the ones of the tenants under it, but only the system tenant records, changes, ends and passes them on.</summary>
public interface ISubscriptionAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPagedDto pagedInput, CancellationToken cancellationToken = default);

    /// <summary>Records that a reseller sells a product to a customer; the roles of the reseller that the sale matches are given it.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller is not the system tenant (403).</exception>
    /// <exception cref="Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions.SubscriptionAlreadyActiveException">The customer has the product already.</exception>
    Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions.SubscriptionEndedException">The sale has ended.</exception>
    Task<SubscriptionDto> UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>Ends the sale (a sale is never erased: it stays as the history of its reseller): it is taken from every role and the users of those roles are signed out. Answers the sale as it is now.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task<SubscriptionDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Passes the sale to another reseller: the old subscription ends and a new one starts now for it, with the same customer, product and settings. Answers the new one.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task<SubscriptionDto> TransferAsync(SubscriptionTransferDto input, CancellationToken cancellationToken = default);

    /// <summary>The companies, the customers and the products in the subscriptions the caller sees, to filter them by.</summary>
    Task<SubscriptionFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default);
}

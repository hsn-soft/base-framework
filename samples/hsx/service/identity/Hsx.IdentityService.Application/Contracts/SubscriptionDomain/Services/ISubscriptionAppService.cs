using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;

/// <summary>What can be done with the subscriptions. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ISubscriptionAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>Records that a reseller sells a product to a customer. Only the system tenant; the roles of the reseller that the sale matches are given it.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller is not the system tenant (403).</exception>
    /// <exception cref="Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions.SubscriptionAlreadyActiveException">The customer has the product already, from any reseller.</exception>
    Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>Ends the sale (a sale is never erased: it stays as the history of its reseller): it is taken from every role and the users of those roles have to sign in again. Only the system tenant.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Passes the sale to another reseller: the old subscription ends and a new one starts now for it, with the same customer, product and settings (so the same scope key). Only the system tenant.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task<SubscriptionDto> TransferAsync(SubscriptionTransferDto input, CancellationToken cancellationToken = default);

    /// <summary>The companies, the customers and the products in the subscriptions the caller sees, to filter them by.</summary>
    Task<SubscriptionFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default);
}

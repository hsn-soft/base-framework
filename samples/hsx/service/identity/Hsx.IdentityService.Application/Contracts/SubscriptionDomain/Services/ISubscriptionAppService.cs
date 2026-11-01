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

    Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such subscription.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

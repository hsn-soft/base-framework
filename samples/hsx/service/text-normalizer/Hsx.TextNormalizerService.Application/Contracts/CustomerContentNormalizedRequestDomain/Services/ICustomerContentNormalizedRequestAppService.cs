using HsnSoft.Base.Application.Dtos;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Filters;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Submits;

namespace Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Services;

/// <summary>What can be done with the customer content normalized requests. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerContentNormalizedRequestAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content normalized request.</exception>
    Task<CustomerContentNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerContentNormalizedRequestDto>> GetPagedListAsync(GetCustomerContentNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerContentNormalizedRequestDto> CreateAsync(CustomerContentNormalizedRequestCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content normalized request.</exception>
    Task UpdateAsync(CustomerContentNormalizedRequestUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content normalized request.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

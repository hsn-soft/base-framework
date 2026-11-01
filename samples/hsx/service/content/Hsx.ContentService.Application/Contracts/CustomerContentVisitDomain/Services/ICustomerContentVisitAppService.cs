using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Services;

/// <summary>What can be done with the customer content visits. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerContentVisitAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content visit.</exception>
    Task<CustomerContentVisitDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerContentVisitDto>> GetPagedListAsync(GetCustomerContentVisitsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerContentVisitDto> CreateAsync(CustomerContentVisitCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content visit.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

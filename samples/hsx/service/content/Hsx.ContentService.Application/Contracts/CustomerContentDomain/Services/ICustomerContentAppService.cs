using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.CustomerContentDomain.Services;

/// <summary>What can be done with the customer contents. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerContentAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content.</exception>
    Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync(GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerContentDto> CreateAsync(CustomerContentCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content.</exception>
    Task UpdateAsync(CustomerContentUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer content.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

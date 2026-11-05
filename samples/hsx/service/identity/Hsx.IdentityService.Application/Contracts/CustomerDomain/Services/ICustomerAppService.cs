using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.CustomerDomain.Services;

/// <summary>What can be done with the customers. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync(GetCustomersPagedDto pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CustomerCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    Task<CustomerDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

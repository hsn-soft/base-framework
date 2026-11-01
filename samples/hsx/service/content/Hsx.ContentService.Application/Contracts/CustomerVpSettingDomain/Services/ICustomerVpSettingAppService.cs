using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Services;

/// <summary>What can be done with the customer vp settings. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerVpSettingAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task<CustomerVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerVpSettingDto>> GetPagedListAsync(GetCustomerVpSettingsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerVpSettingDto> CreateAsync(CustomerVpSettingCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task UpdateAsync(CustomerVpSettingUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

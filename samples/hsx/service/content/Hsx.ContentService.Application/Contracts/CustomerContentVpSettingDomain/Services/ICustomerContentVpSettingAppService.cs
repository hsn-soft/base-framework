using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Services;

/// <summary>What can be done with the customer vp settings. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerContentVpSettingAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task<CustomerContentVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerContentVpSettingDto>> GetPagedListAsync(GetCustomerContentVpSettingsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CustomerContentVpSettingDto> CreateAsync(CustomerContentVpSettingCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task UpdateAsync(CustomerContentVpSettingUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer vp setting.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Interfaces;

public interface ICustomerContentSettingAppService : IEventApplicationService
{
    Task<CustomerContentSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<CustomerContentSettingDto>> GetPagedListAsync(GetCustomerContentSettingsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<CustomerContentSettingDto>> GetFilterListAsync(GetCustomerContentSettingsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<CustomerContentSettingSearchDto>> GetSearchListAsync(GetCustomerContentSettingsSearch searchInput, CancellationToken cancellationToken = default);

    Task<CustomerContentSettingDto> CreateAsync(CustomerContentSettingCreateDto input);

    Task UpdateAsync(CustomerContentSettingUpdateDto input);

    Task DeleteAsync(Guid id);
}
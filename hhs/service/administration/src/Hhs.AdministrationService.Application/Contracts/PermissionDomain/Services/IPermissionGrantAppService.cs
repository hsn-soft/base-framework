using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;

public interface IPermissionGrantAppService
{
    Task<PermissionGrantDto> GetAsync(Guid id);

    Task<PagedDataResultDto<PermissionGrantDto>> GetPagedListAsync(GetPermissionGrantsPaged pagedInput);
    Task<List<PermissionGrantDto>> GetFilterListAsync(GetPermissionGrantsFilter filterInput);
    Task<List<PermissionGrantSearchDto>> GetSearchListAsync(GetPermissionGrantsSearch searchInput);

    Task<PermissionGrantDto> CreateAsync(PermissionGrantCreateDto input);

    Task UpdateAsync(PermissionGrantUpdateDto input);

    Task DeleteAsync(Guid id);
}
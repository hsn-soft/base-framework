using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;

public interface IAppRoleAppService
{
    Task<AppRoleDto> GetAsync(Guid id);

    Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput);
    Task<List<AppRoleDto>> GetFilterListAsync(GetAppRolesFilter filterInput);
    Task<List<AppRoleDto>> GetSearchListAsync(GetAppRolesSearch searchInput);

    Task<AppRoleDto> CreateAsync(AppRoleCreateDto input);

    Task UpdateAsync(AppRoleUpdateDto input);

    Task DeleteAsync(Guid id);
}
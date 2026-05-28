using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;

public interface IAppRoleAppService
{
    Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<AppRoleDto>> GetFilterListAsync(GetAppRolesFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<AppRoleSearchDto>> GetSearchListAsync(GetAppRolesSearch searchInput, CancellationToken cancellationToken = default);

    Task<AppRoleDto> CreateAsync(AppRoleCreateDto input);

    Task UpdateAsync(AppRoleUpdateDto input);

    Task DeleteAsync(Guid id);
}
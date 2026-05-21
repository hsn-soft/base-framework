using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;

public interface IAppUserAppService
{
    Task<AppUserDto> GetAsync(Guid id);

    Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<AppUserDto>> GetFilterListAsync(GetAppUsersFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<AppUserDto>> GetSearchListAsync(GetAppUsersSearch searchInput);

    Task<AppUserDto> CreateAsync(AppUserCreateDto input);

    Task UpdateAsync(AppUserUpdateDto input);

    Task DeleteAsync(Guid id);
}
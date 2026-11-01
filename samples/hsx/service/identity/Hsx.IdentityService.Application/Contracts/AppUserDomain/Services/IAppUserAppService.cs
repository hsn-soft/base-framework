using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;

/// <summary>What can be done with the users. A user of a tenant sees the users of its own tenant and of the tenants under it (the tenants of its token).</summary>
public interface IAppUserAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user, or the caller may not see it.</exception>
    Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppUserDomain.Exceptions.AppUserEmailDuplicateException">There is a user with this email.</exception>
    Task<AppUserDto> CreateAsync(AppUserCreateDto input, CancellationToken cancellationToken = default);

    Task UpdateAsync(AppUserUpdateDto input, CancellationToken cancellationToken = default);

    Task SetPasswordAsync(AppUserSetPasswordDto input, CancellationToken cancellationToken = default);

    /// <summary>The ids of the roles of a user.</summary>
    Task<List<Guid>> GetRoleIdsAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetRolesAsync(AppUserSetRolesDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

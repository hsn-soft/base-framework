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

    Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPagedDto pagedInput, CancellationToken cancellationToken = default);

    /// <summary>Makes a user with a temporary password the system makes; the user is told by the notification service.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.AppUserDomain.Exceptions.AppUserEmailDuplicateException">There is a user with this email.</exception>
    Task<AppUserDto> CreateAsync(AppUserCreateDto input, CancellationToken cancellationToken = default);

    Task<AppUserDto> UpdateAsync(AppUserUpdateDto input, CancellationToken cancellationToken = default);

    Task<AppUserRolesDto> GetRolesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUserRolesDto> SetRolesAsync(AppUserSetRolesDto input, CancellationToken cancellationToken = default);

    /// <summary>A new temporary password for a user who lost its own; its sessions end.</summary>
    Task<TemporaryPasswordIssuedDto> IssueTemporaryPasswordAsync(IssueTemporaryPasswordDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user and answers it as it was.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.AppUserDomain.Exceptions.AppUserNotDeletableException">The user is static.</exception>
    Task<AppUserDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

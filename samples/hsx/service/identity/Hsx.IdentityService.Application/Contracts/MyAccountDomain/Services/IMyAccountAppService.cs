using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;

/// <summary>What the caller does with its own account.</summary>
public interface IMyAccountAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">The caller is nobody.</exception>
    Task<AppUserDto> GetAsync(CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppUserDomain.Exceptions.AppUserWrongPasswordException">The current password is wrong.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Exceptions.PasswordPolicyViolationException">The new password breaks the rules of the tenant.</exception>
    Task<PasswordChangedDto> ChangePasswordAsync(ChangePasswordDto input, CancellationToken cancellationToken = default);
}

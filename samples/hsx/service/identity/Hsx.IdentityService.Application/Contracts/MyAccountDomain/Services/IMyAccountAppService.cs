using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;

/// <summary>What a user does with its own account.</summary>
public interface IMyAccountAppService
{
    /// <summary>Changes the password of the caller, who has to give the current one.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.AppUserDomain.Exceptions.AppUserWrongPasswordException">The current password is not the caller's.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.PasswordPolicyViolationException">The new password does not keep the rules of the tenant.</exception>
    Task ChangePasswordAsync(ChangePasswordDto input, CancellationToken cancellationToken = default);
}

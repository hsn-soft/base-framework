using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Security;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Exceptions;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// What the caller does with its own account. A user changes its own password (the temporary one it was sent included, which is what the sign-in forces at the first time): the current one is checked, the new one keeps the rules of the tenant, and the new one
/// replaces the old at once. The sessions of the user end in the same unit of work (its refresh tokens are revoked): it signs in again with the new password.
/// </summary>
public sealed class MyAccountAppService(
    IAppUserRepository users,
    IAuthPasswordPolicyRepository policies,
    IAuthRefreshTokenRepository refreshTokens,
    TimeProvider time,
    IUnitOfWork unitOfWork,
    IServiceProvider services) : ApplicationServiceBase(services), IMyAccountAppService
{
    public async Task<AppUserDto> GetAsync(CancellationToken cancellationToken = default)
    {
        Guid userId = CurrentUser.Id ?? throw AppUserNotFound.For(Guid.Empty);
        return await users.FindAsync<AppUserDto>(userId, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(userId);
    }

    public async Task<PasswordChangedDto> ChangePasswordAsync(ChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid userId = CurrentUser.Id ?? throw AppUserNotFound.For(Guid.Empty);
        AppUser user = await users.FindAsync(userId, cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(userId);
        if (!PasswordHasher.Verify(user.PasswordHash, input.CurrentPassword!))
        {
            throw new AppUserWrongPasswordException();
        }

        IReadOnlyList<string> broken = (await policies.GetRulesAsync(user.TenantId, cancellationToken).ConfigureAwait(false)).Check(input.NewPassword!);
        if (broken.Count > 0)
        {
            throw new PasswordPolicyViolationException(broken);
        }

        user.SetPassword(input.NewPassword!);
        await unitOfWork.ExecuteAsync(async token =>
        {
            await users.UpdateAsync(user, token).ConfigureAwait(false);
            await refreshTokens.RevokeForUsersAsync([user.Id], token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

        return new PasswordChangedDto { ChangedAt = time.GetUtcNow() };
    }
}

using HsnSoft.Base;
using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Security;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class MyAccountAppService(IRepository<AppUser, Guid> users, IPasswordPolicyProvider policies, IServiceProvider services) : ApplicationServiceBase(services), IMyAccountAppService
{
    public async Task ChangePasswordAsync(ChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid id = CurrentUser.Id ?? throw new BaseHttpException(401, "Nobody is signed in.").WithKey("not_signed_in");
        AppUser user = await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(id);
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
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }
}

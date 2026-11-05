using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Security;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.Shared.Contracts.Events.Identity;
using Hsx.Shared.Helper;
using Microsoft.Extensions.Options;

namespace Hsx.IdentityService.Application.Services;

/// <summary>A user that was made, and the event that tells it to the notification service; the caller publishes the event after its unit of work has ended.</summary>
public sealed record MadeUser(AppUser User, UserCreatedEto Event);

/// <summary>
/// Makes a user the way every use case does: what is known is checked (the email is the sign-in of the whole system, the user name is the tenant's, the way to tell the user is there), the system makes the first password by the rules of the tenant, the user is
/// saved with it as a temporary one, and the event that carries it is made. The password is in the event and nowhere else: not in the result, not in a log.
/// </summary>
public sealed class NewUserFactory(IAppUserRepository users, IAuthPasswordPolicyRepository policies, IOptions<TemporaryPasswordOptions> options, TimeProvider time)
{
    /// <summary>Saves the user in the tenant that is current (the persistence gives it its tenant) and returns it with its event.</summary>
    /// <exception cref="AppUserEmailDuplicateException">The email is taken, by a deleted user too.</exception>
    /// <exception cref="AppUserUsernameDuplicateException">The user name is taken in the tenant.</exception>
    /// <exception cref="AppUserContactMissingException">The user is to be told by a text message and has no phone number.</exception>
    public async Task<MadeUser> MakeAsync(Guid tenantId, NewUserInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.NotificationType == NotificationTypes.Sms && string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            throw new AppUserContactMissingException("phone number");
        }

        string email = input.Email.Trim();
        if (await users.EmailExistsAsync(tenantId, StringHelper.Normalize(email), null, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        string userName = input.UserName.Trim();
        if (await users.UserNameExistsAsync(tenantId, StringHelper.Normalize(userName), null, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserUsernameDuplicateException(userName);
        }

        PasswordRules rules = await policies.GetRulesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        string password = options.Value.SamplePassword ?? TemporaryPasswordGenerator.Generate(rules);
        DateTimeOffset expiresAt = time.GetUtcNow().Add(options.Value.Lifetime);

        AppUser user = AppUser.CreateWithTemporaryPassword(userName, email, input.DisplayName, password, expiresAt, input.LanguageCode, input.PhoneNumber);
        await users.AddAsync(user, cancellationToken).ConfigureAwait(false);

        return new MadeUser(user, new UserCreatedEto(user.Id, user.DisplayName, input.NotificationType, user.Email, user.PhoneNumber, user.LanguageCode, password, expiresAt));
    }
}

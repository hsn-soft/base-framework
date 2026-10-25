using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.Domain.UserDomain.Security;
using Abc.Shared.Hosting;

namespace Abc.AuthServer.Host.Security;

/// <summary>Who signed in: what goes into the claims of the token.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, string Email, IReadOnlyList<string> Roles, Guid TenantId, bool IsSystemTenant);

/// <summary>Checks a user name and a password against the users of the identity service (the same database; the auth server only reads it).</summary>
public sealed class UserAuthenticator(IUserRepository users)
{
    // checked when the user does not exist, so a wrong name takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the name is unknown, the user is not active or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindByUserNameAsync(userName, cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(user?.PasswordHash ?? s_unknownUserHash, password);
        return user is { IsActive: true } && passwordMatches
            ? new AuthenticatedUser(user.Id, user.UserName, user.DisplayName, user.Email, user.Roles, user.TenantId, user.TenantId == AbcTenants.System)
            : null;
    }
}

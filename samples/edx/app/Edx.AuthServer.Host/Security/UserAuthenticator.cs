using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.Domain.IdentityDomain.Security;
using Edx.Shared.Helper;

namespace Edx.AuthServer.Host.Security;

/// <summary>Who signed in: what goes into the claims of the token.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, string Email, IReadOnlyList<string> Roles, Guid TenantId, bool IsSystemTenant);

/// <summary>Checks a tenant, a user name and a password against the users of the identity service (the same database; the auth server only reads it).</summary>
public sealed class UserAuthenticator(IUserRepository users)
{
    // checked when the user does not exist, so a wrong name takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the tenant or the name is unknown, the tenant or the user is not active or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string tenantCode, string userName, string password, CancellationToken cancellationToken = default)
    {
        SignInInfo? found = await users.FindForSignInAsync(tenantCode, userName, cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(found?.User.PasswordHash ?? s_unknownUserHash, password);
        return found is { Tenant.IsActive: true, User.IsActive: true } && passwordMatches
            ? new AuthenticatedUser(found.User.Id, found.User.UserName, found.User.DisplayName, found.User.Email, found.Roles, found.Tenant.Id, found.Tenant.Id == EdxTenants.System)
            : null;
    }
}

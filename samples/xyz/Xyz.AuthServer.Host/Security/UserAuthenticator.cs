using Xyz.AuthServer.Domain.UserDomain.Entities;
using Xyz.AuthServer.Domain.UserDomain.Repositories;
using Xyz.AuthServer.Domain.UserDomain.Security;
using Xyz.Shared.Hosting;

namespace Xyz.AuthServer.Security;

/// <summary>Who signed in: what goes into the claims of the token.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, string Email, IReadOnlyList<string> Roles, Guid TenantId, bool IsSystemTenant, string Language);

/// <summary>Checks a user name and a password against the users of the auth server (its own database).</summary>
public sealed class UserAuthenticator(IUserRepository users)
{
    // checked when the user does not exist, so a wrong name takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the name is unknown, the user is not active or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindByUserNameAsync(userName, cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(user?.PasswordHash ?? s_unknownUserHash, password);
        return user is { IsActive: true } && passwordMatches ? ToAuthenticated(user) : null;
    }

    /// <summary>The active user with this id (for a refresh token), or <see langword="null"/>.</summary>
    public async Task<AuthenticatedUser?> FindActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindByIdAcrossTenantsAsync(id, cancellationToken).ConfigureAwait(false);
        return user is { IsActive: true } ? ToAuthenticated(user) : null;
    }

    private static AuthenticatedUser ToAuthenticated(User user)
        => new(user.Id, user.UserName, user.DisplayName, user.Email, user.Roles, user.TenantId, user.TenantId == XyzTenants.System, user.Language);
}

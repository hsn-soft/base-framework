using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserDomain.Repositories;

/// <summary>What the auth server needs to sign someone in: the user, the tenant it belongs to, the names of its roles and the scope keys of the subscriptions of those roles.</summary>
public sealed record SignInInfo(AppUser User, Tenant Tenant, IReadOnlyList<string> Roles, IReadOnlyList<string> ScopeKeys);

/// <summary>The lookup the auth server signs in with. Nobody is signed in yet, so the filters of the tenants and of the scopes are not what decides: the email the caller gave names the user.</summary>
public interface ISignInRepository
{
    /// <summary>The user with this email (compared without regard to case) with its tenant, roles and scope keys; nothing when there is no such user.</summary>
    Task<SignInInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>The same for a user whose id is known (a refresh token names its user).</summary>
    Task<SignInInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Counts a failed sign-in in one command: the count goes up by one and, when it reaches <paramref name="maxFailedLoginCount"/>, the user is locked out for <paramref name="lockoutMinutes"/> and the count starts again.</summary>
    Task RecordFailureAsync(Guid userId, int maxFailedLoginCount, int lockoutMinutes, CancellationToken cancellationToken = default);

    /// <summary>Notes a sign-in that worked: the count of failures and the lockout are cleared and the time is kept.</summary>
    Task RecordSuccessAsync(Guid userId, CancellationToken cancellationToken = default);
}

using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserDomain.Repositories;

/// <summary>What the auth server needs to sign someone in: the user, the tenant it belongs to, the names of its roles and the scope keys of the subscriptions of those roles.</summary>
public sealed record SignInInfo(AppUser User, Tenant Tenant, IReadOnlyList<string> Roles, IReadOnlyList<string> ScopeKeys);

/// <summary>The repository of the users, with the lookup the auth server signs in with. Nobody is signed in yet, so the filters of the tenants and of the scopes are not what decides for the sign-in: the tenant and the email the caller gave name the user.</summary>
public interface IAppUserRepository : IRepository<AppUser, Guid>
{
    /// <summary>Whether the tenant has a user with this normalized email (the pair is unique); the user <paramref name="exceptId"/> is left out.</summary>
    Task<bool> EmailExistsAsync(Guid tenantId, string normalizedEmail, Guid? exceptId = null, CancellationToken cancellationToken = default);

    /// <summary>Whether the tenant has a user with this user name; the user <paramref name="exceptId"/> is left out.</summary>
    Task<bool> UserNameExistsAsync(Guid tenantId, string normalizedUserName, Guid? exceptId = null, CancellationToken cancellationToken = default);

    /// <summary>The user of the tenant with this normalized email, with its tenant, roles and scope keys; nothing when there is no such user. The caller normalizes what was typed (<c>StringHelper.Normalize</c>) before it asks: the repository only compares. The same email may be a user of another tenant, so the sign-in names the tenant.</summary>
    Task<SignInInfo?> FindByEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>The same for the normalized user name (unique in the tenant).</summary>
    Task<SignInInfo?> FindByUserNameAsync(Guid tenantId, string normalizedUserName, CancellationToken cancellationToken = default);

    /// <summary>The same for a user whose id is known (a refresh token names its user).</summary>
    Task<SignInInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Counts a failed sign-in in one command: the count goes up by one and, when it reaches <paramref name="maxFailedLoginCount"/>, the user is locked out for <paramref name="lockoutMinutes"/> and the count starts again.</summary>
    Task RecordFailureAsync(Guid userId, int maxFailedLoginCount, int lockoutMinutes, CancellationToken cancellationToken = default);

    /// <summary>Notes a sign-in that worked: the count of failures and the lockout are cleared and the time is kept.</summary>
    Task RecordSuccessAsync(Guid userId, CancellationToken cancellationToken = default);
}

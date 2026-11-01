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
}

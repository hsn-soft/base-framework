using Edx.IdentityService.Domain.IdentityDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.IdentityService.Domain.IdentityDomain.Repositories;

/// <summary>What the auth server needs to sign someone in: the user, with the tenant it belongs to and its roles.</summary>
public sealed record SignInInfo(User User, Tenant Tenant, IReadOnlyList<string> Roles);

/// <summary>Where the users are kept: what every repository does, the roles of users, and the lookup the auth server signs in with.</summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// The user of the tenant that has this code with this name (compared without regard to case), with the tenant and the names of its roles; nothing when the tenant or the user is not there. Nobody is signed in
    /// yet, so the tenant filters are not what decides: the code the caller gave names the tenant, and only its users are looked at.
    /// </summary>
    Task<SignInInfo?> FindForSignInAsync(string tenantCode, string userName, CancellationToken cancellationToken = default);

    /// <summary>Gives the user exactly these roles (the roles of the user's own tenant: the caller checked them).</summary>
    Task SetRolesAsync(User user, IReadOnlyCollection<Role> roles, CancellationToken cancellationToken = default);

    /// <summary>Whether any user has the role.</summary>
    Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>The names of the roles of each of these users (a user without roles has an empty list).</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRoleNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
}

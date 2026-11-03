using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Repositories;

/// <summary>A permission a role was granted: its code and its type.</summary>
public sealed record GrantedPermission(string Code, PermissionType Type);

/// <summary>What a role was granted, with who it is: the tenant, and the name of the role the services find it by.</summary>
public sealed record SubjectGrants(Guid TenantId, string Key, IReadOnlyList<GrantedPermission> Grants);

/// <summary>
/// Where the grants of the roles are kept: read and replaced as a whole. A change of the grants of a role replaces them all in one transaction, so nobody is left with half of what was meant. The query that serves the services' store
/// (<see cref="GetAllRoleSubjectsAsync"/>) looks at every tenant on purpose: the store is the system's, and no caller is signed in when it is written.
/// </summary>
public interface IPermissionGrantRepository
{
    /// <summary>The active permissions with these codes.</summary>
    Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>What the user has altogether: the grants of its roles.</summary>
    Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gives a role that was just made what its kind starts with (a Company or a Customer role reads the data of its products; any other role starts with nothing and is granted through the grants endpoint).</summary>
    Task GrantStartingSetAsync(AppRole role, CancellationToken cancellationToken = default);

    Task ReplaceRoleGrantsAsync(AppRole role, IReadOnlyList<Permission> grants, CancellationToken cancellationToken = default);

    /// <summary>Every role of every tenant, with what it was granted (nothing for a role that has no grants).</summary>
    Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default);
}

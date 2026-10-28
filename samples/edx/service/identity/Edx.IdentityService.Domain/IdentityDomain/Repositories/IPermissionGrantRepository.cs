using Edx.IdentityService.Domain.IdentityDomain.Entities;

namespace Edx.IdentityService.Domain.IdentityDomain.Repositories;

/// <summary>A permission a role was granted: its code, its type and the value it carries (a constraint's).</summary>
public sealed record GrantedPermission(string Code, PermissionType Type, string? Value);

/// <summary>What a role was granted, with who it is: the tenant, and the name of the role the services find it by.</summary>
public sealed record SubjectGrants(Guid TenantId, string Key, IReadOnlyList<GrantedPermission> Grants);

/// <summary>
/// Where the grants of the roles are kept: the permissions that carry no value (<c>RolePermission</c>) and the constraints with their values (<c>RolePermissionConstraint</c>), read and replaced together. A change of the grants of a role replaces them all in one step, so
/// nobody is left with half of what was meant. The queries that serve the services' cache (<see cref="GetAllRoleSubjectsAsync"/>) look at every tenant on purpose: the cache is the system's, and no caller is signed in when it is written.
/// </summary>
public interface IPermissionGrantRepository
{
    /// <summary>The active permissions with these codes.</summary>
    Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>What the user has altogether: the grants of its roles (the highest value of a constraint that more than one role gives, when it is a number; otherwise the first).</summary>
    Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ReplaceRoleGrantsAsync(Role role, IReadOnlyList<(Permission Permission, string? Value)> grants, CancellationToken cancellationToken = default);

    /// <summary>Every role of every tenant, with what it was granted (nothing for a role that has no grants).</summary>
    Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default);
}

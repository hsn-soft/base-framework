using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Entities;

/// <summary>
/// A role was granted a constraint with its value (<c>constraint.audit-logs.max-days</c>, <c>30</c>), in the tenant of the role. A role without the row is not granted the constraint at all, and a caller is refused before anything is done. It is kept apart from
/// <see cref="AppRolePermission"/> so a value never sits in the row of a grant that has none (framework ADR-0083). The tenant is the role's.
/// </summary>
public sealed class AppRolePermissionConstraint : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRolePermissionConstraint()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public AppRole? Role { get; }

    public Permission? Permission { get; }

    public static AppRolePermissionConstraint Grant(AppRole role, Permission permission, string? value, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);
        return new()
        {
            Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(),
            RoleId = role.Id,
            PermissionId = permission.Id,
            Value = permission.CheckValue(value) ?? string.Empty,
            TenantId = role.TenantId,
        };
    }
}

using HsnSoft.Base.MultiTenancy;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A role was granted a constraint with its value (<c>constraint.invoice.max-view-amount</c>, <c>20000</c>), in the tenant of the role. A role without the row is not granted the constraint at all, and a caller is refused before anything is done. The tenant is the role's.</summary>
public sealed class RolePermissionConstraint : IMultiTenant
{
    // for EF Core
    private RolePermissionConstraint()
    {
    }

    public Guid Id { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static RolePermissionConstraint Grant(Role role, Permission permission, string? value)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);

        return new() { Id = Guid.CreateVersion7(), RoleId = role.Id, PermissionId = permission.Id, Value = permission.CheckValue(value) ?? string.Empty, TenantId = role.TenantId };
    }
}

using HsnSoft.Base.MultiTenancy;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A role was granted a permission, in the tenant of the role (a role of one tenant can never be granted in another). A constraint carries its value (<see cref="Value"/>). The tenant is kept here and is the role's.</summary>
public sealed class RolePermission : IMultiTenant
{
    // for EF Core
    private RolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    /// <summary>The value of a constraint (a number of bytes); none for any other permission.</summary>
    public string? Value { get; private set; }

    public Guid TenantId { get; private set; }

    public static RolePermission Grant(Role role, Permission permission, string? value)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);

        return new() { RoleId = role.Id, PermissionId = permission.Id, Value = permission.CheckValue(value), TenantId = role.TenantId };
    }
}

using HsnSoft.Base.MultiTenancy;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>
/// A role was granted a permission that carries no value (a page, an endpoint, an operation), in the tenant of the role (a role of one tenant can never be granted in another). A constraint is granted with its value by <see cref="RolePermissionConstraint"/>.
/// The tenant is kept here and is the role's. A grant to a user is not built in the sample (the framework supports it: <c>PermissionProviders.User</c>); a system that wants it adds the table and writes the entries of its users.
/// </summary>
public sealed class RolePermission : IMultiTenant
{
    // for EF Core
    private RolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Guid TenantId { get; private set; }

    public static RolePermission Grant(Role role, Permission permission)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);

        _ = permission.CheckValue(null);
        return new() { RoleId = role.Id, PermissionId = permission.Id, TenantId = role.TenantId };
    }
}

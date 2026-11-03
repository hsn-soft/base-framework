using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Entities;

/// <summary>
/// A role was granted a permission, in the tenant of the role (a role of one tenant can never be granted in another). The tenant is kept here and is the role's. A grant to a user is not built (the framework supports it: <c>PermissionProviders.User</c>); a system that
/// wants it adds the table and writes the entries of its users.
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

    public static RolePermission Grant(AppRole role, Permission permission)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);
        return new() { RoleId = role.Id, PermissionId = permission.Id, TenantId = role.TenantId };
    }
}

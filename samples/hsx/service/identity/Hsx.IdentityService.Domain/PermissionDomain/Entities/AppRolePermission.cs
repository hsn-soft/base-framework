using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Entities;

/// <summary>
/// A role was granted a permission, in the tenant of the role (a role of one tenant can never be granted in another). The tenant is kept here and is the role's. A grant to a user is not built (the framework supports it: <c>PermissionProviders.User</c>); a system that
/// wants it adds the table and writes the entries of its users.
/// </summary>
public sealed class AppRolePermission : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Guid TenantId { get; private set; }

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public AppRole? Role { get; }
    public Permission? Permission { get; }

    public static AppRolePermission Grant(AppRole role, Permission permission, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);
        return new() { Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(), RoleId = role.Id, PermissionId = permission.Id, TenantId = role.TenantId };
    }
}

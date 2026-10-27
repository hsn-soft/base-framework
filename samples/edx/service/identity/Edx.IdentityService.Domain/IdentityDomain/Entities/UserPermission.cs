using HsnSoft.Base.MultiTenancy;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A user was granted a permission of its own, besides what its roles give, in the tenant of the user. A constraint carries its value (<see cref="Value"/>), which wins over the ones of the roles. The tenant is kept here and is the user's.</summary>
public sealed class UserPermission : IMultiTenant
{
    // for EF Core
    private UserPermission()
    {
    }

    public Guid UserId { get; private set; }

    public Guid PermissionId { get; private set; }

    /// <summary>The value of a constraint (a number of bytes); none for any other permission.</summary>
    public string? Value { get; private set; }

    public Guid TenantId { get; private set; }

    public static UserPermission Grant(User user, Permission permission, string? value)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permission);

        return new() { UserId = user.Id, PermissionId = permission.Id, Value = permission.CheckValue(value), TenantId = user.TenantId };
    }
}

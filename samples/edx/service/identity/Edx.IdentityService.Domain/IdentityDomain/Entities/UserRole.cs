using HsnSoft.Base;
using HsnSoft.Base.MultiTenancy;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A user has a role: the link between them, in the tenant both belong to (a user cannot have a role of another tenant). The tenant is kept here and given by the persistence.</summary>
public sealed class UserRole : IMultiTenant
{
    // for EF Core
    private UserRole()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid TenantId { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
        => new() { UserId = Guard.NotDefault<Guid>(userId, nameof(UserId)), RoleId = Guard.NotDefault<Guid>(roleId, nameof(RoleId)) };
}

using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;

/// <summary>A user has a role (both are of the same tenant). The tenant is kept here and given by the persistence.</summary>
public sealed class AppUserRole : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppUserRole()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public static AppUserRole Create(Guid userId, Guid roleId) => new() { Id = Guid.CreateVersion7(), UserId = userId, RoleId = roleId };
}

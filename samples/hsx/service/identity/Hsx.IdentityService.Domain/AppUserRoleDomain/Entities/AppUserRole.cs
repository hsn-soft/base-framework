using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;

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

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public AppRole? Role { get; }

    public static AppUserRole Create(Guid userId, Guid roleId, Guid? id = null) => new() { Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(), UserId = userId, RoleId = roleId };
}

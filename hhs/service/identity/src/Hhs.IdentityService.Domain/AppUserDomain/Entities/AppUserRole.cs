using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AppUserDomain.Entities;

public sealed class AppUserRole : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;


    private AppUserRole()
    {
    }

    internal AppUserRole(Guid tenantId, Guid userId, Guid roleId) : this(Guid.CreateVersion7(), tenantId, userId, roleId)
    {
    }

    internal AppUserRole(Guid id, Guid tenantId, Guid userId, Guid roleId) : this()
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
    }
}
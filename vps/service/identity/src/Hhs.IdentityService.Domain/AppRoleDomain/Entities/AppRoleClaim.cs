using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Entities;

public sealed class AppRoleClaim : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AppUserDomain.Entities;

public sealed class AppUserClaim : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}
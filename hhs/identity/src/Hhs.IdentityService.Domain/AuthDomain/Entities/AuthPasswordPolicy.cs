using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthPasswordPolicy: Entity<Guid>,  IMultiTenant
{
    public Guid TenantId { get;  set; }
    public AuthTenant Tenant { get; set; } = null!;

    public int MinLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = false;

    public int MaxFailedLoginCount { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
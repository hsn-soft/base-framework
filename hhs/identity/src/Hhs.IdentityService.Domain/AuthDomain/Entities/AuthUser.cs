using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthUser: AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get;  set; }
    public AuthTenant Tenant { get; set; } = null!;

    public string UserName { get; set; } = null!;
    public string NormalizedUserName { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public bool IsStatic { get; set; }  // can't delete

    public bool EmailConfirmed { get; set; }

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<AuthUserRole> UserRoles { get; set; } = [];
    public ICollection<AuthUserClaim> Claims { get; set; } = [];
    public ICollection<AuthRefreshToken> RefreshTokens { get; set; } = [];
}
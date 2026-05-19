namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public AuthTenant Tenant { get; set; } = null!;

    public string UserName { get; set; } = null!;
    public string NormalizedUserName { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<AuthUserRole> UserRoles { get; set; } = [];
    public ICollection<AuthUserClaim> Claims { get; set; } = [];
    public ICollection<AuthRefreshToken> RefreshTokens { get; set; } = [];
}
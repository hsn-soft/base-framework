namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthPasswordPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public AuthTenant Tenant { get; set; } = null!;

    public int MinLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = false;

    public int MaxFailedLoginCount { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
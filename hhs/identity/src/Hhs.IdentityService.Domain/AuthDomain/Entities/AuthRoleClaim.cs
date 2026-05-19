namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthRoleClaim
{
    public long Id { get; set; }

    public Guid RoleId { get; set; }
    public AuthRole Role { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}
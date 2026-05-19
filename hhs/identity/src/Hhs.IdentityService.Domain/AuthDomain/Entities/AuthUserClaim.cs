namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthUserClaim
{
    public long Id { get; set; }

    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}
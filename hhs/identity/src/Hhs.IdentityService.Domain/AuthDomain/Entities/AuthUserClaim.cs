using HsnSoft.Base.Domain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthUserClaim: Entity<Guid>
{
    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}
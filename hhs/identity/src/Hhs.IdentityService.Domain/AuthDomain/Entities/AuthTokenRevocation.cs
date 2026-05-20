using HsnSoft.Base.Domain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthTokenRevocation: Entity<Guid>
{
    public Guid UserId { get; set; }

    public string Jti { get; set; } = null!;
    public string Reason { get; set; } = null!;

    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
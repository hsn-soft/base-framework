namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthTokenRevocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Jti { get; set; } = null!;
    public string Reason { get; set; } = null!;

    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
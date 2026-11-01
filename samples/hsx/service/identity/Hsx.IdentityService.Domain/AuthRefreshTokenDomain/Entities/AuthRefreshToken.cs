using HsnSoft.Base.Domain.Entities;

namespace Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;

/// <summary>
/// A refresh token of a user: the hash of the token (the token itself is given once, to the client, and kept nowhere), when it ends, when it was revoked and the hash of the token that took its place (a refresh token is used once and replaced).
/// It is not isolated by the tenant: the client that asks for a new token has nothing but the token.
/// </summary>
public sealed class AuthRefreshToken : Entity<Guid>
{
    // for EF Core
    private AuthRefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public static AuthRefreshToken Create(Guid userId, Guid tenantId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
        => new() { Id = Guid.CreateVersion7(), UserId = userId, TenantId = tenantId, TokenHash = tokenHash, CreatedAt = now, ExpiresAt = now + lifetime };

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt ??= now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

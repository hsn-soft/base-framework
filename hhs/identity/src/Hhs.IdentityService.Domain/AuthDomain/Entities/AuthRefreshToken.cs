using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthRefreshToken: CreationAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
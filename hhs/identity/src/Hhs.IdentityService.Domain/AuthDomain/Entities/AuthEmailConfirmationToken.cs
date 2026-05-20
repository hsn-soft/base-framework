using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthEmailConfirmationToken: CreationAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
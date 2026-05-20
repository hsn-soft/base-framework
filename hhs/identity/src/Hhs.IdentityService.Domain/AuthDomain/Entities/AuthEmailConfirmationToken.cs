using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthEmailConfirmationToken: CreationAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

   [NotNull] public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
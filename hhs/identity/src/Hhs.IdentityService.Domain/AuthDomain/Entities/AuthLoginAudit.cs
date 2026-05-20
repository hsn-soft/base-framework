using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthLoginAudit : CreationAuditedEntity<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    [NotNull] public string UserNameOrEmail { get; set; } = null!;
    public bool IsSuccess { get; set; }

    [CanBeNull] public string FailureReason { get; set; }
    [CanBeNull] public string IpAddress { get; set; }
    [CanBeNull] public string UserAgent { get; set; }
}
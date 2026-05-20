using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthLoginAudit: CreationAuditedEntity<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    public string UserNameOrEmail { get; set; } = null!;
    public bool IsSuccess { get; set; }

    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

}
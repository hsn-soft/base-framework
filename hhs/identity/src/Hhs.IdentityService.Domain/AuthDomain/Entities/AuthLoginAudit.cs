namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthLoginAudit
{
    public long Id { get; set; }

    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    public string UserNameOrEmail { get; set; } = null!;
    public bool IsSuccess { get; set; }

    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
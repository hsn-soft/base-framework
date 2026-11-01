using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;

namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;

/// <summary>A sign-in that was tried, and how it ended: who (the email the caller gave, and the user and its tenant when there is one), whether it worked, why not, and where from. It is written for every attempt and never changed.</summary>
public sealed class AuthLoginAudit : Entity<Guid>
{
    // for EF Core
    private AuthLoginAudit()
    {
    }

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public bool IsSuccess { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuthLoginAudit Record(string email, Guid? tenantId, Guid? userId, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent, DateTimeOffset now)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            Email = Guard.NotNullOrWhiteSpace(email, nameof(Email), AuthLoginAuditConsts.EmailMaxLength).Trim(),
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            IpAddress = Cut(ipAddress, AuthLoginAuditConsts.IpAddressMaxLength),
            UserAgent = Cut(userAgent, AuthLoginAuditConsts.UserAgentMaxLength),
            CreatedAt = now,
        };

    // what comes from the caller is cut, not refused: an audit that cannot be written because of a long header is a hole in the audit
    private static string? Cut(string? value, int length) => value is null || value.Length <= length ? value : value[..length];
}
